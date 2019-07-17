using LeoMaster6.ErrorHandling;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Http;
using System.Web.Http.ExceptionHandling;

namespace LeoMaster6
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            ConfigLog4Net();

            // Web API configuration and services
            config.Filters.Add(new LskExceptionFilterAttribute());
            config.Services.Add(typeof(IExceptionLogger), new ErrorLogger());

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
        }

        private static void ConfigLog4Net()
        {
            // NOTE! change property of 'log4net.confg' to 'Copy Always'?? - no, no need to do this
            // doc - https://logging.apache.org/log4net/release/faq.html

            // Gets directory path of the calling application  
            // RelativeSearchPath is null if the executing assembly i.e. calling assembly is a  
            // stand alone exe file (Console, WinForm, etc).   
            // RelativeSearchPath is not null if the calling assembly is a web hosted application i.e. a web site  

            //NOOOOO! should use base dir if log4net has its own config file in its own folder
            //var log4NetConfigDirectory = AppDomain.CurrentDomain.RelativeSearchPath ?? AppDomain.CurrentDomain.BaseDirectory;
            var log4NetConfigDirectory = AppDomain.CurrentDomain.BaseDirectory;


            var log4NetConfigFilePath = Path.Combine(log4NetConfigDirectory, "ErrorHandling\\log4net.config");
            log4net.Config.XmlConfigurator.ConfigureAndWatch(new FileInfo(log4NetConfigFilePath));
        }
    }
}
