using System.Collections.Generic;
using System.Web.Http;

namespace LeoMaster6.Controllers
{
    [RoutePrefix("mock-yf")]
    public class MockYFController : ApiController
    {

        #region default CRUD

        [Route("")]
        // GET api/<controller>
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        [Route("{id:int}")]
        // GET api/<controller>/5
        public string Get(int id)
        {
            return "value";
        }


        // POST api/<controller>
        public void Post([FromBody]string value)
        {
        }

        // PUT api/<controller>/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/<controller>/5
        public void Delete(int id)
        {
        }

        #endregion


        #region Mocking

        [HttpGet] //should be POST
        [Route("customer/login")]
        public IHttpActionResult Get_()
        {

            var user = new
            {
                identity = 100,
                name = "刘立想",
                gender = "man",
                tel = "18612341234",
                iconUrl = "https://leoskywork.com/img/seagull480.jpg",
                availableBalance = 200,
                unavailableBalance = 0,
                regTime = 1577808000000,
                source = "testing",
                balance = 300,
                deleted = false
            };

            return Json(new { USER = user });
        }

        [HttpGet]
        [Route("shared/randomCode")]
        public object Get__()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(new
            {
                code = 123456
            });
        }


        #endregion
    }
}