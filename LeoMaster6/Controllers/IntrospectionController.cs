﻿using LeoMaster6.Common;
using LeoMaster6.ErrorHandling;
using LeoMaster6.Models;
using LeoMaster6.Models.Helpers;
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
    public class IntrospectionController: BaseController
    {
        public class PutBody
        {
            public string Name { get; set; }
            public DateTime LastFulfil { get; set; }
        }

        public IntrospectionController()
        {

            var routinePath = GetFullIntrospectionDataPath(DateTime.Now, IntrospectionDataType.Routines);
            var passcodePath = GetFullIntrospectionDataPath(DateTime.Now, IntrospectionDataType.Config);
            var fulfillmentPath = GetFullIntrospectionDataPath(DateTime.Now, IntrospectionDataType.Fulfillments);

            var selfCheckingResult = PassSelfCheckingSequences(routinePath, passcodePath, fulfillmentPath);

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
            var antiSpamResult = PassAntiSpamDefender(fulfillmentPath, Constants.LskMaxDBFileSizeKB);

            if (!antiSpamResult.Valid) return DtoResultV5.Fail(BadRequest, antiSpamResult.Message);
            perf.Check("anti spam end");

            SyncRoutine(fulfillmentPath);
            perf.Check("sync routine end");

            var fulfillments = ReadLskjson<Guid, DtoRoutine>(fulfillmentPath, CollectLskjsonLineDefault);
            perf.End("read fulfillment end", true);

            return DtoResultV5.Success(Json, fulfillments);
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

            var fulfillments = ReadLskjson<Guid, DtoRoutine>(fulfillmentPath, CollectLskjsonLineDefault);
            perf.Check("read fulfillment end");

            var fulfill = fulfillments.FirstOrDefault(f => f.Uid == inputUid);
            if (fulfill == null) return DtoResultV5.Fail(BadRequest, "expired data found, please reload page first.");

            var offsetDays = int.Parse(antiSpamResult.Message);

            if (fulfill.LastFulfil.HasValue)
            {
                if (fulfill.HistoryFulfilments == null || !fulfill.HistoryFulfilments.Any())
                {
                    fulfill.HistoryFulfilments = new[] { fulfill.LastFulfil.Value };
                }
                else
                {
                    var history = fulfill.HistoryFulfilments.ToList();
                    history.Add(fulfill.LastFulfil.Value);
                    fulfill.HistoryFulfilments = history.ToArray();
                }
            }

            fulfill.LastFulfil = DateTime.Now.AddDays(-1 * Math.Abs(offsetDays));
            fulfill.UpdateBy = "fixme-update";
            fulfill.UpdateAt = DateTime.Now;

            WriteToFile(fulfillmentPath, fulfillments);
            perf.End("override fulfill end", true);
            return DtoResultV5.Success(Json, fulfill);
        }

        //----- helpers
        private ValidationResult PassSelfCheckingSequences(string routinePath, string passcodePath, string fulfillmentPath)
        {
            if (!File.Exists(routinePath)) return ValidationResult.Fail("missing file: " + routinePath);
            if (!File.Exists(passcodePath)) return ValidationResult.Fail("missing file: " + passcodePath);
            if (FileSizeGreaterThan(fulfillmentPath, Constants.LskMaxDBFileSizeKB)) return ValidationResult.Fail("file too large, file: " + fulfillmentPath);

            var lines = File.ReadAllLines(passcodePath);
            if (!lines.Any(l => !string.IsNullOrEmpty(l))) return ValidationResult.Fail("missing preset config in file: " + passcodePath);

            lines = File.ReadAllLines(routinePath);
            if (!lines.Any(l => !string.IsNullOrEmpty(l))) return ValidationResult.Fail("missing routine items in file: " + routinePath);

            return ValidationResult.Success();
        }

        private ValidationResult PassAntiSpamDefender(string fulfilmentPath, int maxSizeInKB)
        {
            var passcodePath = GetFullIntrospectionDataPath(DateTime.Now, IntrospectionDataType.Config);
            return PassAntiSpamDefender(fulfilmentPath, maxSizeInKB, passcodePath);
        }

        private ValidationResult PassAntiSpamDefender(string fulfilmentPath, int maxSizeInKB, string passcodePath)
        {
            if (Request.Headers.TryGetValues("lsk-introspection-god", out IEnumerable<string> passcodes))
            {
                var firstPass = passcodes?.FirstOrDefault() ?? string.Empty;

                if (firstPass.Length < 6) return ValidationResult.Fail("config missing or incorrect");

                var offsetLength = 2;
                var offsetString = firstPass.Substring(firstPass.Length - offsetLength, offsetLength);
                var passcode = firstPass.Substring(0, firstPass.Length - offsetLength);

                if (!int.TryParse(offsetString, out int _)) return ValidationResult.Fail("fail to parse offset value");

                if (FileSizeGreaterThan(fulfilmentPath, maxSizeInKB)) return ValidationResult.Fail("insufficient fulfillment storage");

                if (!File.Exists(passcodePath)) return ValidationResult.Fail("internal server error - not fully initialized");

                var lines = File.ReadAllLines(passcodePath);
                var passcodeLine = lines.FirstOrDefault(l => !string.IsNullOrEmpty(l));
                if (string.IsNullOrEmpty(passcodeLine)) return ValidationResult.Fail("internal server error - not fully initialized");

                if (passcode != passcodeLine) return ValidationResult.Fail("!spam!");

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

            var fulfillments = ReadLskjson<Guid, DtoRoutine>(fulfillmentPath, CollectLskjsonLineDefault);
            var foundFulfillments = new HashSet<string>();

            //remove obsoleted
            foreach(var fulfill in fulfillments)
            {
                if (string.IsNullOrWhiteSpace(fulfill.Name))
                {
                    fulfill.IsDeleted = true;
                    continue;
                }

                if (loweredRoutineSet.ContainsKey(fulfill.Name.ToLowerInvariant()))
                {
                    foundFulfillments.Add(fulfill.Name.ToLowerInvariant());
                }
                else
                {
                    fulfill.IsDeleted = true; //set flag to remove obsoleted
                }
            }

            //add new
            foreach(var kvp in loweredRoutineSet)
            {
                if (!foundFulfillments.Contains(kvp.Key))
                {
                    fulfillments.Add(new DtoRoutine()
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