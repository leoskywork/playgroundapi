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
            var user = MockData.GetUser();
            return WrapResultJson(new { USER = user });
        }

        [HttpGet]
        [Route("account/getCurrentUser")]
        public IHttpActionResult Get___()
        {
            var user = MockData.GetUser();
            return WrapResultJson(user);
        }

        [HttpPost]
        [Route("account/getBalance")]
        public IHttpActionResult Get____()
        {
            return WrapResultJson(MockData.GetBalance());
        }

        [HttpGet]
        [Route("account/record")]
        public IHttpActionResult Get_____()
        {
            return WrapResultJson("demo transaction list", false, null, true);
        }

        [HttpPost]
        [Route("coupon/searchCurrent")]
        public IHttpActionResult Get__100()
        {
            return WrapResultJson(MockData.GetCouponList());
        }

        [HttpGet]
        [Route("shared/listence")]
        public IHttpActionResult Get__101()
        {
            return WrapResultJson("demo service agreement", false, null, true);
        }

        [HttpGet]
        [Route("setting/system/getCustomServiceTel")]
        public IHttpActionResult Get__102()
        {
            return WrapResultJson("18612341234");
        }

        #endregion

        #region Mocking - return null data

        [HttpGet]
        [Route("pos/circle")]
        public IHttpActionResult Get__1()
        {
            // return WrapResultJson(new object[] { });
            return WrapResultJson(default(object));
        }

        [HttpPost]
        [Route("driveOrder/realtime")]
        public IHttpActionResult Get__2()
        {
            System.Threading.Thread.Sleep(2000);
            return WrapResultJson(new object[] { });
        }

        [HttpPost]
        [Route("driveOrder/history")]
        public IHttpActionResult Get__3()
        {
            System.Threading.Thread.Sleep(2000);
            return WrapResultJson(new object[] { });
        }

        [HttpGet]
        [Route("account/logout")]
        public IHttpActionResult Get__4()
        {
            return WrapResultJson(default(object));
        }

        #endregion

        private class MockData
        {
            internal readonly static double avaiBalance = 200;  //can withdraw to bank card
            internal readonly static double unavaiBalance = 100; //can not withdraw to bank card


            public static object GetUser()
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

            public static object GetCouponList()
            {
                var one = new
                {
                    identity = 100,
                    type = "fixed",
                    coupon = 10,
                    startTime = 1577808000000, //2020-1-1
                    endTime = default(object),
                    user = default(object),
                    beUsed = false
                };

                var two = new
                {
                    identity = 102,
                    type = "fixed",
                    coupon = 20,
                    startTime = 1585843200000, //2020-5-1
                    endTime = default(object),
                    user = default(object),
                    beUsed = false
                };

                var three = new
                {
                    identity = 103,
                    type = "fixed",
                    coupon = 199,
                    startTime = 1585843200000, //2020-4-3
                    endTime = 1588435200000, //2020-5-3
                    user = default(object),
                    beUsed = false
                };

                var four = new
                {
                    identity = 104,
                    type = "fixed",
                    coupon = 99,
                    startTime = 1585843200000, //2020-4-3
                    endTime = 1617379200000, //2021-4-3
                    user = default(object),
                    beUsed = false
                };

                return new List<object>() { one, two, three, four };
            }
        }


        //class ApiResult<T>
        public IHttpActionResult WrapResultJson<T>(T data, bool hasError = false, string message = null, bool simpleVersion = false)
        {
            return Json(WrapResult(data, hasError, message, simpleVersion));
        }

        public static object WrapResult<T>(T data, bool hasError = false, string message = null, bool simpleVersion = false)
        {
            if (simpleVersion) return new { data };

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