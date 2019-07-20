using LeoMaster6.Common;
using LeoMaster6.Models;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Web.Http;

namespace LeoMaster6.Controllers
{
    public class NoteController : BaseController
    {
        [HttpPost]
        public IHttpActionResult Clipboard([FromBody]string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentNullException(nameof(message));
            }

            //todo improve this, hard coded as dev session id for now
            if (!CheckHeaderSession())
            {
                //return Unauthorized();
            }

            var item = new DtoClipboardItem()
            {
                Uid = Guid.NewGuid(),
                UserId = MapToUserId(Request.Headers.GetValues(Constants.HeaderSessionId).First()),
                CreatedAt = DateTime.Now,
                Data = message
            };

            string path = GetFullClipboardDataPath(DateTime.Now);
            AppendToFile(Newtonsoft.Json.JsonConvert.SerializeObject(item) + Environment.NewLine, path);

            return Json($"{message.Length} characters saved to clipboard.");
        }

        [HttpPut]
        public IHttpActionResult Clipboard([FromBody]string message, [FromBody]string uid, [FromBody]DateTime date)
        {
            if (string.IsNullOrEmpty(uid))
            {
                throw new ArgumentNullException(nameof(uid));
            }

            var parsedUid = Guid.Parse(uid);

            //todo improve this, hard coded as dev session id for now
            if (!CheckHeaderSession())
            {
                //return Unauthorized();
            }

            //todo - allow update history notes, only allow update current month notes now
            date = DateTime.Now;
            string path = GetFullClipboardDataPath(date);
            var notes = ReadLskJson(path, int.MaxValue, 1, (line) => Newtonsoft.Json.JsonConvert.DeserializeObject<DtoClipboardItem>(line));
            //perf - 2n here, can be optimal to n
            var foundNote = notes.FirstOrDefault(n => n.Uid == parsedUid);
            var foundChild = notes.FirstOrDefault(n => n.ParentUid == parsedUid);

            if (foundChild != null)
            {
                throw new InvalidOperationException("Data already changed, please reload to latest then edit");
            }

            if (foundNote != null)
            {
                var newNote = Utility.DeepClone(foundNote);

                newNote.Uid = Guid.NewGuid(); //reset
                newNote.Data = message;

                newNote.HasUpdated = true;
                //todo - replace with real UserId(get by session id)
                newNote.LastUpdatedBy = foundNote.UserId.Substring(0, foundNote.UserId.Length - 2) + DateTime.Now.ToString("dd");
                newNote.LastUpdatedAt = DateTime.Now;
                newNote.ParentUid = foundNote.Uid;

                AppendToFile(Newtonsoft.Json.JsonConvert.SerializeObject(newNote) + Environment.NewLine, path);

                return Json(new { found = true, message = "Data updated", data = newNote });
            }
            else
            {
                return Json(new { found = false, message = "The data you try to update may have been deleted" });
            }
        }

        //todo pass in the DateTime, page size, page index
        [HttpGet]
        public IHttpActionResult Clipboard()
        {
            DateTime time = DateTime.Now;
            int pageSize = 50;
            int pageIndex = 1;

            if (!CheckHeaderSession())
            {
                //return Unauthorized();
            }

            string path = GetFullClipboardDataPath(time);

            /*
             //why there are escaped chars('\')?
             "{\"SessionId\":\"dev001abc\",\"CreatedAt\":\"2019-07-17T00:51:57.4275352+08:00\",\"Data\":\"friend//lsk// 222   //lsk//run\"}{\"SessionId\":\"dev001abc\",\"CreatedAt\":\"2019-07-17T00:54:09.7961408+08:00\",\"Data\":\"test333\"}{\"SessionId\":\"dev001abc\",\"CreatedAt\":\"2019-07-17T00:58:27.2650479+08:00\",\"Data\":\"test4\"}\r\n{\"SessionId\":\"dev001abc\",\"CreatedAt\":\"2019-07-17T00:58:43.5578953+08:00\",\"Data\":\"test5\"}\r\n{\"SessionId\":\"dev001abc\",\"CreatedAt\":\"2019-07-17T00:58:57.989691+08:00\",\"Data\":\"test6\"}\r\n"
             */
            //return Ok(File.Exists(path) ? File.ReadAllText(path) : string.Empty);

            if (!File.Exists(path))
            {
                return Ok();
            }

            //following code works only when the entire file is in valid format 
            //  - which is not the case here
            //since we just append new line to the file(a proper json array string should quote by '[]' and item separated by ',')
            //  - a workaround for saving is deserialize entire file into a collection, add the new item then save the collection to the file again which is poor performance
            //using (var reader = File.OpenText(GetFullClipboardDataPath(DateTime.MinValue)))     
            //using (var jsonReader = new Newtonsoft.Json.JsonTextReader(reader))
            //{
            //    var serializer = new Newtonsoft.Json.JsonSerializer();
            //    var items = serializer.Deserialize<DtoClipboardItem[]>(jsonReader);

            //    return Json(items);
            //}

            var items = ReadLskJson(path, pageSize, pageIndex, (line) =>
            {
                return Newtonsoft.Json.JsonConvert.DeserializeObject<DtoClipboardItem>(line);
            });


            return Json(ApplyJSNameConvention(items));
        }

        private static IEnumerable<T> ReadLskJson<T>(string path, int pageSize, int pageIndex, Func<string, T> mapper)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));

            //can't read entire file one time, the entire file is not in valid json format(for array)
            //hover every line is a valid json object
            var items = new List<T>();
            var lineNumber = 0;
            using (var reader = File.OpenText(path))
            {
                do
                {
                    //fixme - poor paging mechanism(have to read every line from top), but this is a small project
                    //?? top x lines or top x items? line can be empty, which means no item
                    lineNumber++;
                    var line = reader.ReadLine();
                    if (!string.IsNullOrWhiteSpace(line) && lineNumber > pageSize * (pageIndex - 1))
                    {
                        items.Add(mapper(line));
                    }
                }
                while (!reader.EndOfStream && items.Count < pageSize);
            }

            return items;
        }


        private static IEnumerable<object> ApplyJSNameConvention(IEnumerable<DtoClipboardItem> items)
        {
            if (items?.Count() > 0)
            {
                return items.Select(i =>
                {
                    dynamic jsonObject = new ExpandoObject();

                    jsonObject.uid = i.Uid;
                    jsonObject.userId = i.UserId;
                    jsonObject.createdAt = i.CreatedAt;
                    jsonObject.data = i.Data;

                    if (i.HasUpdated == true)
                    {
                        jsonObject.hasUpdated = i.HasUpdated;
                        jsonObject.lastUpdatedBy = i.LastUpdatedBy;
                        jsonObject.lastUpdatedAt = i.LastUpdatedAt;
                        jsonObject.parentUid = i.ParentUid;
                    }

                    return jsonObject;
                });
            }

            return items;
        }


        private bool CheckHeaderSession()
        {
            return Request.Headers.TryGetValues(Constants.HeaderSessionId, out IEnumerable<string> sessionIds) && sessionIds.FirstOrDefault() == Constants.DevSessionId;
        }

        private static string MapToUserId(string sessionId)
        {
            if (sessionId == Constants.DevSessionId)
            {
                return Constants.DevUserId;
            }

            throw new NotImplementedException();
        }

        private static string GetFullClipboardDataPath(DateTime time)
        {
            //not the normal week of year, this is too complex to determinate the date boundaries of every portion(file)
            //return Path.Combine(GetBaseDirectory(), GetDatapoolEntry(), "clip-" + (time.DayOfYear / 7 + 1).ToString("D2") + time.ToString("-yyyy-MM") + ".json");

            return Path.Combine(GetBaseDirectory(), GetDatapoolEntry(), "clip-" + time.ToString("yyyy-MM") + ".json");
        }
    }
}
