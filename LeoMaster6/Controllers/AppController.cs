using LeoMaster6.Common;
using LeoMaster6.ErrorHandling;
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
        [System.Web.Mvc.Route("age")]
        public IHttpActionResult GetLiveTime()
        {
            _logger.Debug("enter GetLiveTime() '/age'");
            return Json((int)Math.Ceiling(DateTime.Now.Subtract(new DateTime(2018, 12, 10)).TotalDays));
        }

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

            var machineKey = "lsk-env-" + Environment.MachineName;
            var envKey = ConfigurationManager.AppSettings.AllKeys.First(k => k.Equals(machineKey, StringComparison.OrdinalIgnoreCase));
            var dir = ConfigurationManager.AppSettings["lsk-dir-" + ConfigurationManager.AppSettings[envKey]];
            var datapoolEntry = ConfigurationManager.AppSettings["lsk-dir-data-pool-entry"];
            var path = Path.Combine(dir, datapoolEntry, "msg-" + DateTime.Now.ToString("yyyy-MM-dd") + ".txt");
            var alternate = Path.Combine(dir, datapoolEntry, "msg-" + DateTime.Now.ToString("yyyy-MM-dd_HH:mm:ss") + ".txt");

            AppendToFile(string.Join(Environment.NewLine, content), path, alternate);

            return Json($"Your post{(string.IsNullOrWhiteSpace(message.Title) ? "" : " (" + message.Title.Trim() + ")")} is saved.");

            //await the async method to finish?? to ensure message saved??
            //actual should return: Created("...")
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
                catch (Exception)
                {
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

    }


}
