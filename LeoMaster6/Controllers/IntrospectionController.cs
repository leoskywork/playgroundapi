using LeoMaster6.Common;
using LeoMaster6.ErrorHandling;
using LeoMaster6.Models;
using LeoMaster6.Models.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Http;

namespace LeoMaster6.Controllers
{

    [RoutePrefix("introspection")]
    public class IntrospectionController : BaseController
    {
        public class PutBody
        {
            public string Name { get; set; }
            public DateTime LastFulfill { get; set; }
        }

        private class IntrospectionConfig
        {
            public int Passcode { get; set; }
            public string AntiSpamToken { get; set; }
        }

        public IntrospectionController()
        {

            var routinePath = GetFullIntrospectionDataPath(DateTime.Now, IntrospectionDataType.Routines);
            var passcodePath = GetFullIntrospectionDataPath(DateTime.Now, IntrospectionDataType.Config);
            var fulfillmentPath = GetFullIntrospectionDataPath(DateTime.Now, IntrospectionDataType.Fulfillments);

            var selfCheckingResult = PassSelfCheckingSequences(routinePath, passcodePath, fulfillmentPath, true);

            if (!selfCheckingResult.Valid)
            {
                throw new LskSelfCheckingException(selfCheckingResult.Message, nameof(IntrospectionController));
            }
        }


        [HttpGet]
        public IHttpActionResult Get()
        {
            var perf = PerfCounter.NewThenCheck(this.ToString() + "." + MethodBase.GetCurrentMethod().Name);
            var fulfillmentPath = GetFullIntrospectionDataPath(DateTime.Now, IntrospectionDataType.Fulfillments);
            var antiSpamResult = PassAntiSpamDefender(fulfillmentPath, Constants.LskMaxDBFileSizeKB, true);

            if (!antiSpamResult.Valid) return DtoResultV5.Fail(BadRequest, antiSpamResult.Message);
            perf.Check("anti spam end");

            SyncRoutine(fulfillmentPath);
            perf.Check("sync routine end");

            var fulfillments = ReadLskjson<Guid, RoutineFulfillment>(fulfillmentPath, CollectLskjsonLineDefault);
            perf.End("read fulfillment end", true);

            return DtoResultV5.Success(Json, fulfillments.Select(f => DtoRoutine.From(f)));
        }

        [HttpPut]
        [Route("{id:guid}")]
        public IHttpActionResult Put(string id, [FromBody]PutBody body)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            if (body == null) throw new ArgumentNullException(nameof(body));

            var inputUid = Guid.Parse(id);

            var perf = PerfCounter.NewThenCheck(this.ToString() + "." + MethodBase.GetCurrentMethod().Name);
            var fulfillmentPath = GetFullIntrospectionDataPath(DateTime.Now, IntrospectionDataType.Fulfillments);
            var antiSpamResult = PassAntiSpamDefender(fulfillmentPath, Constants.LskMaxDBFileSizeKB);

            if (!antiSpamResult.Valid) return DtoResultV5.Fail(BadRequest, antiSpamResult.Message);
            perf.Check("anti spam end");

            SyncRoutine(fulfillmentPath);
            perf.Check("sync routine end");

            var fulfillments = ReadLskjson<Guid, RoutineFulfillment>(fulfillmentPath, CollectLskjsonLineDefault);
            perf.Check("read fulfillment end");

            var fulfill = fulfillments.FirstOrDefault(f => f.Uid == inputUid);
            if (fulfill == null) return DtoResultV5.Fail(BadRequest, "expired data found, please reload page first.");

            var offsetDays = int.Parse(antiSpamResult.Message);

            if (fulfill.LastFulfill.HasValue)
            {
                if (fulfill.HistoryFulfillments == null || !fulfill.HistoryFulfillments.Any())
                {
                    fulfill.HistoryFulfillments = new[] { fulfill.LastFulfill.Value };
                }
                else
                {
                    var history = fulfill.HistoryFulfillments.ToList();
                    history.Add(fulfill.LastFulfill.Value);
                    fulfill.HistoryFulfillments = history.ToArray();
                }
            }

            fulfill.LastFulfill = DateTime.Now.AddDays(-1 * Math.Abs(offsetDays));
            fulfill.UpdateBy = "fixme-update";
            fulfill.UpdateAt = DateTime.Now;

            WriteToFile(fulfillmentPath, fulfillments);
            perf.End("override fulfill end", true);
            return DtoResultV5.Success(Json, DtoRoutine.From(fulfill));
        }

        //----- helpers
        private ValidationResult PassSelfCheckingSequences(string routinePath, string configPath, string fulfillmentPath, bool creatFileIfMissing)
        {
            if (creatFileIfMissing)
            {
                void ensureFileExists(string path)
                {
                    if (File.Exists(path)) return;

                    var dir = Path.GetDirectoryName(path);
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    using (File.Create(dir)) { }
                }

                ensureFileExists(routinePath);
                ensureFileExists(configPath);
            }

            if (!File.Exists(routinePath)) return ValidationResult.Fail("missing file: " + Path.GetFileName(routinePath));
            if (!File.Exists(configPath)) return ValidationResult.Fail("missing file: " + Path.GetFileName(configPath));
            if (FileSizeGreaterThan(fulfillmentPath, Constants.LskMaxDBFileSizeKB)) return ValidationResult.Fail("file too large, file: " + Path.GetFileName(fulfillmentPath));

            var lines = File.ReadAllLines(configPath);
            if (!lines.Any(l => !string.IsNullOrEmpty(l))) return ValidationResult.Fail("missing preset config in file: " + Path.GetFileName(configPath));

            try
            {
                var config = JsonConvert.DeserializeObject<IntrospectionConfig>(string.Join(string.Empty, lines));
                if (config == null || config.Passcode < 1000 || string.IsNullOrWhiteSpace(config.AntiSpamToken)) return ValidationResult.Fail("config file not set. 0xe01");
                if (config == null || config.AntiSpamToken == null || config.AntiSpamToken.Trim().Length < 4) return ValidationResult.Fail("config file not set. 0xe02");
            }
            catch (Exception e)
            {
                _logger.Error("fail to deserialize config file", e);
                return ValidationResult.Fail("config file error. 0xe03");
            }

            lines = File.ReadAllLines(routinePath);
            if (!lines.Any(l => !string.IsNullOrEmpty(l))) return ValidationResult.Fail("missing routine items in file: " + Path.GetFileName(routinePath));

            return ValidationResult.Success();
        }

        private ValidationResult PassAntiSpamDefender(string fulfilmentPath, int maxSizeInKB, bool simpleMode = false)
        {
            var passcodePath = GetFullIntrospectionDataPath(DateTime.Now, IntrospectionDataType.Config);
            return PassAntiSpamDefender(fulfilmentPath, maxSizeInKB, passcodePath, simpleMode);
        }

        private ValidationResult PassAntiSpamDefender(string fulfilmentPath, int maxSizeInKB, string configPath, bool simpleMode = false)
        {
            if (Request.Headers.TryGetValues("lsk-introspection-god", out IEnumerable<string> passcodes))
            {
                var inputPass = passcodes?.FirstOrDefault() ?? string.Empty;

                if (inputPass.Length < 6) return ValidationResult.Fail("config missing or incorrect");

                var offsetLength = 2;
                var offsetString = inputPass.Substring(4, offsetLength);

                if (!int.TryParse(offsetString, out int _)) return ValidationResult.Fail("fail to parse value");

                if (FileSizeGreaterThan(fulfilmentPath, maxSizeInKB)) return ValidationResult.Fail("insufficient fulfillment storage");

                if (!File.Exists(configPath)) return ValidationResult.Fail("internal server error - not fully initialized");

                var lines = File.ReadAllLines(configPath);
                var configLine = lines.FirstOrDefault(l => !string.IsNullOrEmpty(l));
                if (string.IsNullOrEmpty(configLine)) return ValidationResult.Fail("internal server error - not fully initialized");

                var config = JsonConvert.DeserializeObject<IntrospectionConfig>(string.Join(string.Empty, lines));

                if (simpleMode)
                {
                    var pass = config.Passcode.ToString();

                    if (!inputPass.Contains(pass)) return ValidationResult.Fail("spam. 0xe10");
                }
                else
                {
                    //var now = DateTime.Now.ToString("HHmm");

                    //if (Math.Abs(int.Parse(now[3].ToString()) - int.Parse(inputPass.Last().ToString())) > 2) return ValidationResult.Fail("spam. 0xe10");

                    //var inputPrefix = inputPass.Substring(0, 4);
                    var inputSuffix = inputPass.Substring(6, Math.Min(Constants.LskMaxPasscodeLength, inputPass.Length - 6));
                    //var hash = (int.Parse(now[1].ToString()) + int.Parse(now[2].ToString())) % 10;

                    //if (hash != int.Parse(inputPrefix[0].ToString())) return ValidationResult.Fail("spam. 0xe10");

                    config.AntiSpamToken = config.AntiSpamToken.Trim();

                    for (var i = 0; i < config.AntiSpamToken.Length; i += 2)
                    {
                        var unitLength = Math.Min(2, config.AntiSpamToken.Length - i);

                        if (!inputSuffix.Contains(config.AntiSpamToken.Substring(i, unitLength))) return ValidationResult.Fail("spam. 0xe10");
                    }
                }

                return ValidationResult.Success(offsetString);
            }

            return ValidationResult.Fail("config missing");
        }

        private void SyncRoutine(string fulfillmentPath)
        {
            var routinePath = GetFullIntrospectionDataPath(DateTime.Now, IntrospectionDataType.Routines);
            SyncRoutine(fulfillmentPath, routinePath);
        }

        private void SyncRoutine(string fulfillmentPath, string routinePath)
        {
            var routineLines = File.ReadAllLines(routinePath);
            var routines = routineLines.Where(l => !string.IsNullOrWhiteSpace(l)).Select(l => l.Trim());
            var loweredRoutineSet = routines.GroupBy(r => r.ToLowerInvariant()).ToDictionary(g => g.Key, g => g.ToList());

            if (!File.Exists(fulfillmentPath)) using (File.Create(fulfillmentPath)) { };

            var fulfillments = ReadLskjson<Guid, RoutineFulfillment>(fulfillmentPath, CollectLskjsonLineDefault);
            var foundFulfillments = new HashSet<string>();

            //remove obsoleted
            foreach(var fulfill in fulfillments)
            {
                if (string.IsNullOrWhiteSpace(fulfill.Name))
                {
                    fulfill.IsDeleted = true;
                    fulfill.DeletedBy = "fixme-del";
                    fulfill.DeleteAt = DateTime.Now;
                    continue;
                }

                if (loweredRoutineSet.ContainsKey(fulfill.Name.ToLowerInvariant()))
                {
                    foundFulfillments.Add(fulfill.Name.ToLowerInvariant());
                }
                else
                {
                    fulfill.IsDeleted = true; //set flag to remove obsoleted
                    fulfill.DeletedBy = "fixme-del2";
                    fulfill.DeleteAt = DateTime.Now;
                }
            }

            //add new
            foreach(var kvp in loweredRoutineSet)
            {
                if (!foundFulfillments.Contains(kvp.Key))
                {
                    fulfillments.Add(new RoutineFulfillment()
                    {
                        Uid = Guid.NewGuid(),
                        Name = kvp.Value[0],
                        CreateBy = "<fixme>",
                        CreateAt = DateTime.Now
                    });
                }
            }

            WriteToFile(fulfillmentPath, fulfillments);

        }

        private static bool FileSizeGreaterThan(string path, int sizeInKB)
        {
            if (File.Exists(path))
            {
                FileInfo info = new FileInfo(path);

                if (info.Length > sizeInKB * 1024)
                {
                    return true;
                }
            }

            return false;
        }

        private static string GetFullIntrospectionDataPath(DateTime time, IntrospectionDataType type)
        {
            if (type == IntrospectionDataType.Fulfillments)
            {
                return Path.Combine(GetBaseDirectory(), GetDatapoolEntry(), $"{Constants.LsktextPrefix}introspection-{type.ToString().ToLower()}-{time.ToString("yyyy")}.txt");
            }

            return Path.Combine(GetBaseDirectory(), GetDatapoolEntry(), $"{Constants.LsktextPrefix}introspection-{type.ToString().ToLower()}.txt");
        }
    }

    public enum IntrospectionDataType
    {
        Routines,
        Fulfillments,
        Config
    }
}