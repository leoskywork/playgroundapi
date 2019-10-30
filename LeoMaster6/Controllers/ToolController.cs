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
    //[LskExceptionFilter]
    public class ToolController : BaseController
    {
        public IHttpActionResult GetVer()
        {
            _logger.Debug("enter GetVer()");

            var assembly = MethodInfo.GetCurrentMethod().DeclaringType.Assembly;
            var fileInfo = new FileInfo(assembly.Location);
            var format = "yyyy-MM-dd H:mm:ss";

            return Json($"Asp.NET Api; {string.Join(";", assembly.FullName.Split(',').Take(2))}; Initial Build At {fileInfo.CreationTime.ToString(format)}({fileInfo.CreationTime.Kind}); Last Build At {fileInfo.LastWriteTime.ToString(format)}");
        }
    }
}