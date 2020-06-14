﻿using LeoMaster6.Common;
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
            public string LastRemark { get; set; }
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
            var fulfillments = ReadFulfillmentsOfThisYear(AuthMode.Simple);
            var dtoList = fulfillments.Select(f => DtoRoutine.From(f, false));

            return DtoResultV5.Success(Json, dtoList);
        }


        [HttpGet]
        [Route("{id:guid}")]
        public IHttpActionResult Get(string id, string history = null)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return DtoResultV5.Fail(BadRequest, "id is empty");
            }

            if (Constants.LskArchived.Equals(history, StringComparison.OrdinalIgnoreCase))
            {
                var archivePath = GetFullIntrospectionDataPath(DateTime.Now, IntrospectionDataType.Archives);
                var archivedRecords = ReadLskjson<Guid, FulfillmentArchive>(archivePath, CollectLskjsonLineDefault);
                var recordsByParentUid = archivedRecords.Where(a => id.Equals(a.ParentUid.ToString(), StringComparison.OrdinalIgnoreCase));
                return DtoResultV5.Success(Json, recordsByParentUid.Select(r => DtoFulfillmentArchive.From(r)));
            }
            else
            {
                var fulfillments = ReadFulfillmentsOfThisYear(AuthMode.None);
                var fulfillment = fulfillments.FirstOrDefault(f => id.Equals(f.Uid.ToString(), StringComparison.OrdinalIgnoreCase));
                var dto = fulfillment == null ? null : DtoRoutine.From(fulfillment, true);
                return DtoResultV5.Success(Json, dto);
            }
        }

        [HttpGet]
        public IHttpActionResult HeartBeat(string user)
        {
            _logger.Info("enter heart beat, user: " + user);
            ReadFulfillmentsOfThisYear(AuthMode.None);
            _logger.Info("leave heart beat");

            return DtoResultV5.Success(Json, "heart beat");
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
            var antiSpamResult = PassAntiSpamDefender(fulfillmentPath, Constants.LskMaxDBFileSizeKB, AuthMode.None);

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
                if (!fulfill.HasMigrated)
                {
                    fulfill.HasMigrated = true;

                    if (fulfill.HistoryFulfillments?.Length > 0)
                    {
                        var archiveUnit = fulfill.HistoryFulfillments.Select(h => new FulfillmentArchive(fulfill.Uid, null, h));
                        var archivePath = GetFullIntrospectionDataPath(DateTime.Now, IntrospectionDataType.Archives);
                        AppendObjectToFile(archivePath, archiveUnit.ToArray());
                    }
                }

                if (fulfill.StagedArchives?.Length > 0)
                {
                    var staged = fulfill.StagedArchives.ToList();
                    staged.Add(FulfillmentArchive.FromLast(fulfill));

                    if (staged.Count >= Constants.LskFulfillmentActiveRecords + Constants.LskFulfillmentArchiveUnit)
                    {
                        var archiveUnit = staged.GetRange(Constants.LskFulfillmentActiveRecords, Constants.LskFulfillmentArchiveUnit);
                        var archivePath = GetFullIntrospectionDataPath(DateTime.Now, IntrospectionDataType.Archives);
                        AppendObjectToFile(archivePath, archiveUnit.ToArray());
                        staged.RemoveRange(Constants.LskFulfillmentActiveRecords, Constants.LskFulfillmentArchiveUnit);
                        fulfill.HasArchived = true;
                    }

                    fulfill.StagedArchives = staged.ToArray();
                }
                else
                {
                    fulfill.StagedArchives = new[] { FulfillmentArchive.FromLast(fulfill) };
                }
            }

            fulfill.LastFulfill = DateTime.Now.AddDays(-1 * Math.Abs(offsetDays));
            fulfill.LastRemark = body.LastRemark;
            fulfill.UpdateBy = "fixme-update";
            fulfill.UpdateAt = DateTime.Now;

            WriteToFile(fulfillmentPath, fulfillments);
            perf.End("override fulfill end", true);
            return DtoResultV5.Success(Json, DtoRoutine.From(fulfill, false));
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

        private ValidationResult PassAntiSpamDefender(string fulfilmentPath, int maxSizeInKB, AuthMode mode)
        {
            var passcodePath = GetFullIntrospectionDataPath(DateTime.Now, IntrospectionDataType.Config);
            return PassAntiSpamDefender(fulfilmentPath, maxSizeInKB, passcodePath, mode);
        }

        private ValidationResult PassAntiSpamDefender(string fulfilmentPath, int maxSizeInKB, string configPath, AuthMode mode)
        {
            Request.Headers.TryGetValues("lsk-introspection-god", out IEnumerable<string> passcodes);
            var inputPass = passcodes?.FirstOrDefault() ?? string.Empty;

            if (mode == AuthMode.None)
            {
                return ValidationResult.Success(inputPass);
            }

            if(!string.IsNullOrEmpty(inputPass))
            {
                var inputMaxLength = 12;

                if (inputPass.Length > inputMaxLength) return ValidationResult.Fail("input too long");
                if (!int.TryParse(inputPass, out int _)) return ValidationResult.Fail("fail to parse value");


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

                if (mode == AuthMode.Simple)
                {
                    var pass = config.Passcode.ToString();

                    if (!inputPass.Contains(pass)) return ValidationResult.Fail("spam. 0xe10");
                }
                else if (mode == AuthMode.Standard)
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
            if (type == IntrospectionDataType.Fulfillments || type == IntrospectionDataType.Archives)
            {
                return Path.Combine(GetBaseDirectory(), GetDatapoolEntry(), $"{Constants.LsktextPrefix}introspection-{type.ToString().ToLower()}-{time.ToString("yyyy")}.txt");
            }

            return Path.Combine(GetBaseDirectory(), GetDatapoolEntry(), $"{Constants.LsktextPrefix}introspection-{type.ToString().ToLower()}.txt");
        }

        private List<RoutineFulfillment> ReadFulfillmentsOfThisYear(AuthMode authMode)
        {
            var perf = PerfCounter.NewThenCheck(this.ToString() + "." + MethodBase.GetCurrentMethod().Name);
            var fulfillmentPath = GetFullIntrospectionDataPath(DateTime.Now, IntrospectionDataType.Fulfillments);
            var antiSpamResult = PassAntiSpamDefender(fulfillmentPath, Constants.LskMaxDBFileSizeKB, authMode);

            if (!antiSpamResult.Valid) //return DtoResultV5.Fail(BadRequest, antiSpamResult.Message);
            {
                throw new LskExcepiton("Bad request, " + antiSpamResult.Message);
            }
            perf.Check("anti spam end");

            SyncRoutine(fulfillmentPath);
            perf.Check("sync routine end");

            var fulfillments = ReadLskjson<Guid, RoutineFulfillment>(fulfillmentPath, CollectLskjsonLineDefault);
            perf.End("read fulfillment end", true);

            return fulfillments;
        }

        private enum AuthMode
        {
            None,
            Simple,
            Standard
        }
    }

    public enum IntrospectionDataType
    {
        Config,
        Routines,
        Fulfillments,
        Archives
    }
}