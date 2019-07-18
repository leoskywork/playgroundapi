using LeoMaster6.Common;
using LeoMaster6.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web.Http;

namespace LeoMaster6.Controllers
{
    //[LskExceptionFilter] //move to base class
    public class AppController : BaseController
    {
        //[System.Web.Mvc.Route("age")]
        //public IHttpActionResult GetLiveTime()
        //{
        //    _logger.Debug("enter GetLiveTime() '/age'");
        //    return Json((int)Math.Ceiling(DateTime.Now.Subtract(new DateTime(2018, 12, 10)).TotalDays));
        //}

        /*
        [HttpPost]
        public IHttpActionResult PostMessage([FromBody]DtoPost message)
        {
            if (message == null || string.IsNullOrWhiteSpace(message.Message))
            {
                return Unauthorized();
            }

            if (message.Message == "1/0")
            {
                throw new DivideByZeroException(message.Message);
            }

            if (message.Message == "0/0")
            {
                throw new LskExcepiton("Divide by zero: " + message.Message);
            }

            //a test message
            //moon //lsk// game of throne ended //lsk// admin
            //var weekDay = DateTime.Now.DayOfWeek.ToString().Select(c => c.ToString()).ToArray();
            var messageParts = message.Message.Split(new[] { Constants.MessageSeparator }, StringSplitOptions.RemoveEmptyEntries);

            if (messageParts.Length < 3)
            {
                return Unauthorized();
            }

            //if (!messageParts.First().StartsWith(weekDay[0], StringComparison.OrdinalIgnoreCase))
            //{
            //    return Unauthorized();
            //}

            //if (!messageParts.Last().StartsWith(weekDay[1], StringComparison.OrdinalIgnoreCase))
            //{
            //    return Unauthorized();
            //}

            var content = new List<string>();

            if (!string.IsNullOrWhiteSpace(message.Title))
            {
                content.Add("//title//" + message.Title);
            }

            content.AddRange(messageParts.Skip(1).Take(messageParts.Length - 2));

            if (!string.IsNullOrWhiteSpace(message.Title))
            {
                content.Add("".PadLeft(128, '-') + Environment.NewLine);
            }

            var path = GetFullTitledMessagePath();
            var alternate = GetFullTitledMessageAlterPath();

            AppendToFile(string.Join(Environment.NewLine, content), path, alternate);

            return Json($"Your post{(string.IsNullOrWhiteSpace(message.Title) ? "" : " (" + message.Title.Trim() + ")")} is saved.");

            //await the async method to finish?? to ensure message saved??
            //actual should return: Created("...")
        }
        */

        //[HttpGet]
        //public IHttpActionResult Message()
        //{
        //    var path = GetFullTitledMessagePath();

        //    return Json(File.Exists(path) ? File.ReadAllText(path) : string.Empty);
        //}

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
                return Unauthorized();
            }

            var item = new DtoClipboardItem()
            {
                Uid = Guid.NewGuid(),
                SessionId = Request.Headers.GetValues(Constants.HeaderSessionId).First(),
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
                return Unauthorized();
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
            using(var reader = File.OpenText(path))
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


        #region Helpers

        private static string GetFullTitledMessagePath(DateTime time)
        {
            return Path.Combine(GetBaseDirectory(), GetDatapoolEntry(), "msg-" + time.ToString("yyyy-MM-dd") + ".txt");
        }

        private static string GetFullTitledMessageAlterPath(DateTime time)
        {
            return Path.Combine(GetBaseDirectory(), GetDatapoolEntry(), "msg-" + time.ToString("yyyy-MM-dd_HH:mm:ss") + ".txt");
        }

        private static string GetFullClipboardDataPath(DateTime time)
        {
            //not the normal week of year, this is too complex to determinate the date boundaries of every portion(file)
            //return Path.Combine(GetBaseDirectory(), GetDatapoolEntry(), "clip-" + (time.DayOfYear / 7 + 1).ToString("D2") + time.ToString("-yyyy-MM") + ".json");

            return Path.Combine(GetBaseDirectory(), GetDatapoolEntry(), "clip-" + time.ToString("yyyy-MM") + ".json");
        }

        private static string GetDatapoolEntry()
        {
            return ConfigurationManager.AppSettings["lsk-dir-data-pool-entry"];
        }

        private static string GetBaseDirectory()
        {
            var machineKey = "lsk-env-" + Environment.MachineName;
            var envKey = ConfigurationManager.AppSettings.AllKeys.First(k => k.Equals(machineKey, StringComparison.OrdinalIgnoreCase));
            var dir = ConfigurationManager.AppSettings["lsk-dir-" + ConfigurationManager.AppSettings[envKey]];
            return dir;
        }

        private void AppendToFile(string content, string path, string alternatePath = null)
        {
            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }

            if (!File.Exists(path))
            {
                using (var fileWriter = new StreamWriter(File.Create(path)))
                {
                    fileWriter.WriteAsync(content);
                }
            }
            else
            {
                try
                {
                    using (var fileWriter = new StreamWriter(path, true))
                    {
                        fileWriter.WriteAsync(content);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"Got an error while writing to {path}, going to save to alter file {alternatePath}.", ex);

                    //shouldn't have done this, just over thinking here and waste time
                    if (!string.IsNullOrEmpty(alternatePath) && !File.Exists(alternatePath))
                    {
                        using (var fileWriter = new StreamWriter(File.Create(alternatePath)))
                        {
                            fileWriter.WriteAsync(content);
                        }
                    }
                }
            }
        }

        private bool CheckHeaderSession()
        {
            return Request.Headers.TryGetValues(Constants.HeaderSessionId, out IEnumerable<string> sessionIds) && sessionIds.FirstOrDefault() == Constants.DevSessionId;
        }

        #endregion
    }


}
