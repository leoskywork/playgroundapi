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
            public string LastRemark { get; set; }
        }

        public class DeleteBody
        {
            public string Name { get; set; }
            public string Reason { get; set; }
        }

        public class DeleteHistoryBody
        {
            public string Name { get; set; }
            public string Reason { get; set; }
            public string Kind { get; set; }
        }

        private class IntrospectionConfig
        {
            public int Passcode { get; set; }
            public string AntiSpamToken { get; set; }
            public string DeletionToken { get; set; }
            public int LskMaxDBFileSizeKB { get; set; }
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

            //for migrate
            if(fulfillments.Any(f => !f.HasMigrated || f.LastFulfill.HasValue))
            {
                foreach(var fulfill in fulfillments)
                {
                    if (!fulfill.HasMigrated)
                    {
                        fulfill.HasMigrated = true;

                        if (fulfill.HistoryFulfillments?.Length > 0)
                        {
                            var migrateUnit = fulfill.HistoryFulfillments.Select(h => new FulfillmentArchive(fulfill.Uid, null, h));
                            //var archivePath = GetFullIntrospectionDataPath(DateTime.Now, IntrospectionDataType.Archives);
                            //AppendObjectToFile(archivePath, migrateUnit.ToArray());

                            fulfill.StagedArchives = migrateUnit.ToArray();
                        }
                    }

                    if (fulfill.LastFulfill.HasValue)
                    {
                        var record = new FulfillmentArchive(fulfill.Uid, fulfill.LastRemark, fulfill.LastFulfill, fulfill.UpdateBy, fulfill.UpdateAt);

                        if (fulfill.StagedArchives?.Length > 0)
                        {
                            var staged = fulfill.StagedArchives.ToList();
                            staged.Add(record);
                            fulfill.StagedArchives = staged.ToArray();
                        }
                        else
                        {
                            fulfill.StagedArchives = new[] { record };
                        }

                        fulfill.LastFulfill = null;
                        fulfill.LastRemark = null;
                    }
                }

                var path = GetFullIntrospectionDataPath(DateTime.Now, IntrospectionDataType.Fulfillments);

                WriteToFile(path, fulfillments);
            }

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
                var archivedRecords = ReadLskjson<Guid, FulfillmentArchive>(archivePath, CollectLskjsonLineIncludeDeleted);
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
            var count = ReadFulfillmentsOfThisYear(AuthMode.None).Count;
            _logger.Info("leave heart beat");

            return DtoResultV5.Success(Json, $"heart heat. {count}");
        }



        [HttpPut]
        [Route("{id:guid}")]
        public IHttpActionResult Put(string id, [FromBody]PutBody body)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            if (body == null) throw new ArgumentNullException(nameof(body));

            var perf = PerfCounter.NewThenCheck(this.ToString() + "." + MethodBase.GetCurrentMethod().Name);
            var fulfillmentPath = GetFullIntrospectionDataPath(DateTime.Now, IntrospectionDataType.Fulfillments);
            var antiSpamResult = PassAntiSpamDefender(fulfillmentPath, AuthMode.None);

            if (!antiSpamResult.Valid) return DtoResultV5.Fail(BadRequest, antiSpamResult.Message);
            perf.Check("anti spam end");

            SyncRoutine(fulfillmentPath);
            perf.Check("sync routine end");

            var fulfillments = ReadLskjson<Guid, RoutineFulfillment>(fulfillmentPath, CollectLskjsonLineIncludeDeleted);
            perf.Check("read fulfillment end");

            var inputUid = Guid.Parse(id);
            var fulfill = fulfillments.FirstOrDefault(f => f.Uid == inputUid);
            if (fulfill == null) return DtoResultV5.Fail(BadRequest, "expired data found, please reload page first.");

            var offsetDays = int.Parse(antiSpamResult.Message);
            var date = DateTime.Now.AddDays(-1 * Math.Abs(offsetDays));
            var record = new FulfillmentArchive(fulfill.Uid, body.LastRemark, date, "fixme-put", DateTime.Now);

            if (fulfill.StagedArchives == null)
            {
                fulfill.StagedArchives = new[] { record };
            }
            else
            {
                var staged = fulfill.StagedArchives.ToList();
                staged.Add(record);

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

            //fulfill.StagedArchives = fulfill.StagedArchives.Concat(new[] { record }).ToArray();
            fulfill.UpdateBy = "fixme-update";
            fulfill.UpdateAt = DateTime.Now;

            WriteToFile(fulfillmentPath, fulfillments);
            perf.End("override fulfill end", true);
            return DtoResultV5.Success(Json, DtoRoutine.From(fulfill, false));
        }

        [HttpDelete]
        [Route("{id:guid}")]
        public IHttpActionResult Delete(string id, string reason)
        {
            return this.Delete2(id, new DeleteBody() { Reason = reason });
        }

        [HttpDelete]
        [Route("{id:guid}")]
        public IHttpActionResult Delete2(string id, [FromBody]DeleteBody body)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            if (body == null) throw new ArgumentNullException(nameof(body));

            var perf = PerfCounter.NewThenCheck(this.ToString() + "." + MethodBase.GetCurrentMethod().Name);
            var fulfillmentPath = GetFullIntrospectionDataPath(DateTime.Now, IntrospectionDataType.Fulfillments);
            var antiSpamResult = PassAntiSpamDefender(fulfillmentPath, AuthMode.SimpleDeletion);

            if (!antiSpamResult.Valid) return DtoResultV5.Fail(BadRequest, antiSpamResult.Message);
            perf.Check("anti spam end");

            SyncRoutine(fulfillmentPath);
            perf.Check("sync routine end");

            var fulfillments = ReadLskjson<Guid, RoutineFulfillment>(fulfillmentPath, CollectLskjsonLineIncludeDeleted);
            perf.Check("read fulfillment end");

            var inputUid = Guid.Parse(id);
            var fulfill = fulfillments.FirstOrDefault(f => f.Uid == inputUid);

            if (fulfill == null) return DtoResultV5.Fail(BadRequest, "Routine not found, may already deleted, please refresh page");

            fulfill.IsDeleted = true;
            fulfill.DeleteAt = DateTime.Now;
            fulfill.DeletedBy = "fixme-delete";
            fulfill.DeleteReason = body.Reason;

            WriteToFile(fulfillmentPath, fulfillments);
            perf.End("override fulfill end", true);
            return DtoResultV5.Success(Json, DtoRoutine.From(fulfill, false));
        }

        [HttpDelete]
        [Route("{parentId:guid}/history/{id:guid}")]
        public IHttpActionResult DeleteHistoryRecord(string parentId, string id, string reason, string kind)
        {
            return DeleteHistoryRecord2(parentId, id, new DeleteHistoryBody() { Reason = reason, Kind = kind });
        }

        [HttpDelete]
        [Route("{parentId:guid}/history/{id:guid}")]
        public IHttpActionResult DeleteHistoryRecord2(string parentId, string id, [FromBody]DeleteHistoryBody body)
        {
            if (string.IsNullOrEmpty(parentId)) throw new ArgumentNullException(nameof(parentId));
            if (body == null) throw new ArgumentNullException(nameof(body));
            if (body.Kind != Constants.LskArchived && body.Kind != Constants.LskStaged) return DtoResultV5.Fail(BadRequest, $"unsupported deletion: '{body.Kind}'");


            var perf = PerfCounter.NewThenCheck(this.ToString() + "." + MethodBase.GetCurrentMethod().Name);
            var fulfillmentPath = GetFullIntrospectionDataPath(DateTime.Now, IntrospectionDataType.Fulfillments);
            var antiSpamResult = PassAntiSpamDefender(fulfillmentPath, AuthMode.SimpleDeletion);

            if (!antiSpamResult.Valid) return DtoResultV5.Fail(BadRequest, antiSpamResult.Message);
            perf.Check("anti spam end");

            SyncRoutine(fulfillmentPath);
            perf.Check("sync routine end");

            var inputParentUid = Guid.Parse(parentId);
            var inputUid = Guid.Parse(id);

            string filePath = null;
            List<ILskjsonLine> allRecords = null;
            FulfillmentArchive history = null;

            if (body.Kind == Constants.LskStaged)
            {
                filePath = fulfillmentPath;
                var fulfillments = ReadLskjson<Guid, RoutineFulfillment>(filePath, CollectLskjsonLineIncludeDeleted);
                var fulfill = fulfillments.FirstOrDefault(f => f.Uid == inputParentUid);

                if (fulfill == null) return DtoResultV5.Fail(BadRequest, "Routine not found, may already deleted, please refresh page");
                if (!fulfill.HasMigrated) return DtoResultV5.Fail(BadRequest, "Migrate routine first");
                if (fulfill.StagedArchives == null || fulfill.StagedArchives.Length == 0) return DtoResultV5.Fail(BadRequest, "No staged history found, please refresh page");

                allRecords = fulfillments.ToList<ILskjsonLine>();
                history = fulfill.StagedArchives.FirstOrDefault(s => s.Uid == inputUid);
            }
            else if (body.Kind == Constants.LskArchived)
            {
                filePath = GetFullIntrospectionDataPath(DateTime.Now, IntrospectionDataType.Archives);
                var historyRecords = ReadLskjson<Guid, FulfillmentArchive>(filePath, CollectLskjsonLineIncludeDeleted);
                allRecords = historyRecords.ToList<ILskjsonLine>();
                history = historyRecords.FirstOrDefault(h => h.ParentUid == inputParentUid && h.Uid == inputUid);
            }

            if (history == null) return DtoResultV5.Fail(BadRequest, $"No {body.Kind} history matches with provided id, please refresh page");

            history.IsDeleted = true;
            history.DeleteAt = DateTime.Now;
            history.DeletedBy = "fixme-del";
            history.DeleteReason = body.Reason;

            WriteToFile(filePath, allRecords);
            perf.End($"delete {body.Kind} history end", true);
            return DtoResultV5.Success(Json, body.Kind);
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

            var lines = File.ReadAllLines(configPath);
            if (!lines.Any(l => !string.IsNullOrEmpty(l))) return ValidationResult.Fail("missing preset config in file: " + Path.GetFileName(configPath));

            try
            {
                var config = JsonConvert.DeserializeObject<IntrospectionConfig>(string.Join(string.Empty, lines));
                if (config == null || config.Passcode < 1000 || string.IsNullOrWhiteSpace(config.AntiSpamToken)) return ValidationResult.Fail("config file not set. 0xe01");
                if (config == null || config.AntiSpamToken == null || config.AntiSpamToken.Trim().Length < 4) return ValidationResult.Fail("config file not set. 0xe02");
                if (FileSizeGreaterThan(fulfillmentPath, config.LskMaxDBFileSizeKB)) return ValidationResult.Fail("file too large, file: " + Path.GetFileName(fulfillmentPath));
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

        private ValidationResult PassAntiSpamDefender(string fulfilmentPath, AuthMode mode)
        {
            Request.Headers.TryGetValues("lsk-introspection-god", out IEnumerable<string> passcodes);
            var inputPass = passcodes?.FirstOrDefault() ?? string.Empty;

            if (mode == AuthMode.None)
            {
                return ValidationResult.Success(inputPass);
            }

            if(!string.IsNullOrEmpty(inputPass))
            {
                var inputMaxLength = 24;

                if (inputPass.Length > inputMaxLength) return ValidationResult.Fail("input lsk too long");
                if (inputPass.Length < 6) return ValidationResult.Fail("input lsk to short");

                var offsetLength = 2;
                var offsetString = inputPass.Substring(4, offsetLength);
                var configPath = GetFullIntrospectionDataPath(DateTime.Now, IntrospectionDataType.Config);
                var lines = File.ReadAllLines(configPath);
                var configLine = lines.FirstOrDefault(l => !string.IsNullOrEmpty(l));

                if (string.IsNullOrEmpty(configLine)) return ValidationResult.Fail("internal server error - not fully initialized");
                var config = JsonConvert.DeserializeObject<IntrospectionConfig>(string.Join(string.Empty, lines));

                if (!int.TryParse(offsetString, out int _)) return ValidationResult.Fail("fail to parse value");
                if (FileSizeGreaterThan(fulfilmentPath, config.LskMaxDBFileSizeKB)) return ValidationResult.Fail("insufficient fulfillment storage");
                if (!File.Exists(configPath)) return ValidationResult.Fail("internal server error - not fully initialized");

                if (mode == AuthMode.Simple)
                {
                    if (!inputPass.Contains(config.Passcode.ToString())) return ValidationResult.Fail("spam. 0x10");
                }
                else if(mode == AuthMode.SimpleDeletion)
                {
                    if (!inputPass.Contains(config.DeletionToken)) return ValidationResult.Fail("spam. 0x11");
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

                        if (!inputSuffix.Contains(config.AntiSpamToken.Substring(i, unitLength))) return ValidationResult.Fail("spam. 0x20");
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

            var fulfillments = ReadLskjson<Guid, RoutineFulfillment>(fulfillmentPath, CollectLskjsonLineIncludeDeleted);
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
                        CreateBy = "fixme-new",
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
            var antiSpamResult = PassAntiSpamDefender(fulfillmentPath, authMode);

            if (!antiSpamResult.Valid) //return DtoResultV5.Fail(BadRequest, antiSpamResult.Message);
            {
                throw new LskExcepiton("Bad request, " + antiSpamResult.Message);
            }
            perf.Check("anti spam end");

            SyncRoutine(fulfillmentPath);
            perf.Check("sync routine end");

            var fulfillments = ReadLskjson<Guid, RoutineFulfillment>(fulfillmentPath, CollectLskjsonLineIncludeDeleted);
            perf.End("read fulfillment end", true);

            return fulfillments;
        }

        private enum AuthMode
        {
            None,
            Simple,
            SimpleDeletion,
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