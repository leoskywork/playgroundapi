using LeoMaster6.ErrorHandling;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Cors;
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

            ConfigGlobalCors(config);

            // Web API routes 
            //   - enable attribute routing(new in web api 2)
            config.MapHttpAttributeRoutes();
            //   - convention-based routing
            //     - match URI to a route template
            //     - selecting a controller
            //     - selecting an action
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
        }

        private static void ConfigGlobalCors(HttpConfiguration config)
        {
            // allow cross origin resource sharing
            //   - ref https://www.c-sharpcorner.com/article/enable-cors-in-asp-net-webapi-2/
            // need install package
            //   - Install-Package Microsoft.AspNet.WebApi.Cors
            // can enable CORS at global, controller or action level

            // parameter 'origins' - allow those sites to access to resources of current site
            // var cors = new EnableCorsAttribute("http://localhost:4200,http://leoskywork.com:84", "*", "GET,POST");
            var cors = new EnableCorsAttribute("*", "*", "*");
            config.EnableCors(cors);
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
