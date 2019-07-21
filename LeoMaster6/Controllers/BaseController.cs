using LeoMaster6.ErrorHandling;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace LeoMaster6.Controllers
{
    [LskExceptionFilter]
    public class BaseController : ApiController
    {
        //make it static member?? no, need pass in the log source for different sub classes
        protected log4net.ILog _logger;

        public BaseController()
        {
            _logger = log4net.LogManager.GetLogger(this.GetType());
        }


        #region helpers

        public static string GetDatapoolEntry()
        {
            return ConfigurationManager.AppSettings["lsk-dir-data-pool-entry"];
        }

        public static string GetBaseDirectory()
        {
            var machineKey = "lsk-env-" + Environment.MachineName;
            var envKey = ConfigurationManager.AppSettings.AllKeys.First(k => k.Equals(machineKey, StringComparison.OrdinalIgnoreCase));
            var dir = ConfigurationManager.AppSettings["lsk-dir-" + ConfigurationManager.AppSettings[envKey]];
            return dir;
        }

        protected bool AppendToFile(string path, string content, string alternatePath = null)
        {
            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }

            if (!File.Exists(path))
            {
                using (var fileWriter = new StreamWriter(File.Create(path)))
                {
                    //fileWriter.WriteAsync(content); //?? async, seems no need to do so
                    //the block is on server side, not on client side, and we need the return value reliable here
                    fileWriter.Write(content);
                    return true;
                }
            }
            else
            {
                try
                {
                    using (var fileWriter = new StreamWriter(path, true))
                    {
                        //fileWriter.WriteAsync(content);
                        fileWriter.Write(content);
                        return true;
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

                    return false;
                }
            }
        }

        #endregion
    }
}
