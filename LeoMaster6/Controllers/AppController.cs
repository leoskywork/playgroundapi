using LeoMaster6.Common;
using LeoMaster6.ErrorHandling;
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
        [System.Web.Mvc.Route("age")]
        public IHttpActionResult GetLiveTime()
        {
            _logger.Debug("enter GetLiveTime() '/age'");
            return Json((int)Math.Ceiling(DateTime.Now.Subtract(new DateTime(2018, 12, 10)).TotalDays));
        }

        [HttpPost]
        public IHttpActionResult PostMessage([FromBody]DtoPost message)
        {
            //todo pass in time
            DateTime time = DateTime.Now;

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

            var path = GetFullTitledMessagePath(time);
            var alternate = GetFullTitledMessageAlterPath(time);

            AppendToFile(string.Join(Environment.NewLine, content), path, alternate);

            return Json($"Your post{(string.IsNullOrWhiteSpace(message.Title) ? "" : " (" + message.Title.Trim() + ")")} is saved.");

            //await the async method to finish?? to ensure message saved??
            //actual should return: Created("...")
        }
        
        //[HttpGet]
        //public IHttpActionResult Message()
        //{
        //    var path = GetFullTitledMessagePath();

        //    return Json(File.Exists(path) ? File.ReadAllText(path) : string.Empty);
        //}

        #region Helpers

        private static string GetFullTitledMessagePath(DateTime time)
        {
            return Path.Combine(GetBaseDirectory(), GetDatapoolEntry(), "msg-" + time.ToString("yyyy-MM-dd") + ".txt");
        }

        private static string GetFullTitledMessageAlterPath(DateTime time)
        {
            return Path.Combine(GetBaseDirectory(), GetDatapoolEntry(), "msg-" + time.ToString("yyyy-MM-dd_HH:mm:ss") + ".txt");
        }

        #endregion
    }


}
