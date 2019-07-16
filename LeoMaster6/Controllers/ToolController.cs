using LeoMaster6.ErrorHandling;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Web.Http;

namespace LeoMaster6.Controllers
{
    [LskExceptionFilter]
    public class ToolController : ApiController
    {
        public IHttpActionResult GetVer()
        {
            var assembly = MethodInfo.GetCurrentMethod().DeclaringType.Assembly;
            var fileInfo = new FileInfo(assembly.Location);

            return Json($"Asp.NET Api; {string.Join(";", assembly.FullName.Split(',').Take(2))}; Build At {fileInfo.CreationTime.ToString("yyyy-MM-dd H:mm:ss")}");
        }
    }
}