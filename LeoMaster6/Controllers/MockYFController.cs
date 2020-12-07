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


        [HttpGet]
        [Route("shared/randomCode")]
        public object Get_()
        {
            //return Newtonsoft.Json.JsonConvert.SerializeObject(new
            //{
            //    code = 123456
            //});
            return WrapResultJson("123456");
        }


        [HttpGet] //should be POST
        [Route("customer/login")]
        public IHttpActionResult Get__()
        {
            var user = MockUser.GetOne();
            return WrapResultJson(new { USER = user });
        }

        [HttpGet]
        [Route("account/getCurrentUser")]
        public IHttpActionResult Get___()
        {
            var user = MockUser.GetOne();
            return WrapResultJson(user);
        }


       

        [HttpPost]
        [Route("account/getBalance")]
        public IHttpActionResult Get____()
        {
            return WrapResultJson(MockUser.GetBalance());
        }

        [HttpGet]
        [Route("account/record")]
        public IHttpActionResult Get_____()
        {
            return WrapResultJson("demo transaction list", false, null, true);
        }

        #endregion

        private class MockUser
        {
            internal readonly static double avaiBalance = 200;  //can withdraw to bank card
            internal readonly static double unavaiBalance = 100; //can not withdraw to bank card


            public static object GetOne()
            {
                return new
                {
                    identity = 100,
                    name = "刘立想",
                    gender = "man",
                    tel = "18612341234",
                    // iconUrl = "https://leoskywork.com/img/seagull480.jpg",
                    iconUrl = "image/seagull480.jpg",
                    availableBalance = avaiBalance,
                    unavailableBalance = unavaiBalance,
                    regTime = 1577808000000,
                    source = "testing",
                    balance = GetBalance(),
                    deleted = false
                };
            }

            public static double GetBalance()
            {
                return avaiBalance + unavaiBalance;
            }
        }


        //class ApiResult<T>
        public IHttpActionResult WrapResultJson<T>(T data, bool hasError = false, string message = null, bool simpleVersion = false)
        {
            return Json(WrapResult(data, hasError, message, simpleVersion));
        }

        public static object WrapResult<T>(T data, bool hasError = false, string message = null, bool simpleVersion = false)
        {
            if (simpleVersion) return new { data, message };

            return new
            {
                data,
                success = !hasError,
                message,
                map = data//bad design, just for compatible
            };
        }
    }
}