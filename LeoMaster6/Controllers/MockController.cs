using LeoMaster6.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;

namespace LeoMaster6.Controllers
{
    [RoutePrefix("mock")]
    public class MockController: BaseController
    {
        [HttpGet]
        [Route("todos")]
        public IHttpActionResult GetTodoList()
        {
            return DtoResultV5.Success(Json, new string[] { });
        }
    }
}