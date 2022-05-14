using LeoMaster6.Common;
using LeoMaster6.Models;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Http;

namespace LeoMaster6.Controllers
{
    [RoutePrefix("public/todos")]
    public class TodoController : BaseController
    {

        //todo pass in the DateTime, page size, page index
        [Route("")]
        [HttpGet]
        public IHttpActionResult TodoList()
        {
            return DtoResultV5.Success(Json, "asp.net at " + DateTime.Now.ToString());
        }
      
    }
}
