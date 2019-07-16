using LeoMaster6.ErrorHandling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace LeoMaster6.Controllers
{
    [LskExceptionFilter]
    public class BaseController : ApiController
    {
        protected log4net.ILog _logger;

        public BaseController()
        {
            _logger = log4net.LogManager.GetLogger(this.GetType());
        }
    }
}
