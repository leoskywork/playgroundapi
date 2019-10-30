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
            var createAt = fileInfo.CreationTime;
            var lastWrite = fileInfo.LastWriteTime;

            return Json($"Asp.NET api; {string.Join(";", assembly.FullName.Split(',').Take(2))}; Initial build at {createAt.ToString(format)}({createAt.Kind}); Last build at {lastWrite.ToString(format)}({lastWrite.Kind})");
        }
    }
}