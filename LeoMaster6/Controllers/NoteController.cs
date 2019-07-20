using LeoMaster6.Common;
using LeoMaster6.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
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


            //can't read entire file one time, the entire file is not in valid json format(for array)
            //hover every line is a valid json object
            var items = new List<DtoClipboardItem>();
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
                        items.Add(Newtonsoft.Json.JsonConvert.DeserializeObject<DtoClipboardItem>(line));
                    }
                }
                while (!reader.EndOfStream && items.Count < pageSize);
            }

            return Json(items);
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
