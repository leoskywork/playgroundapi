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

        [HttpPost]
        [Route("setting/fee/getPriceByCityAndDistance")]
        public IHttpActionResult Get__________()
        {
            return WrapResultJson(30.0 + System.DateTime.Now.Second % 30);
        }

        [HttpGet]
        [Route("account/record")]
        public IHttpActionResult Get_____()
        {
            System.Threading.Thread.Sleep(1500);
            return WrapResultJson("demo transaction list", false, null, true);
        }

        [HttpGet]
        [Route("setting/fee/feeDescribeByCityName")]
        public IHttpActionResult Get______()
        {
            System.Threading.Thread.Sleep(1500);
            return WrapResultJson("demo fee rules", false, null, true);
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
            System.Threading.Thread.Sleep(1500);
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

            var driver = new
            {
                content = MockData.GetDriver(),
                distance = MockData.GetGeoDistance(1.5)
            };

            var driver2 = new
            {
                content = MockData.GetDriver2(),
                distance = MockData.GetGeoDistance(1.0)
            };

            var drivers = new object[] { driver, driver2 };

            return WrapResultJson(new { content = drivers});
        }

        [HttpPost]
        [Route("driveOrder/realtime")]
        public IHttpActionResult Get__2()
        {
            System.Threading.Thread.Sleep(500);
            return WrapResultJson(new object[] { });
        }

        [HttpPost]
        [Route("driveOrder/history")]
        public IHttpActionResult Get__3()
        {
            //System.Threading.Thread.Sleep(1000);
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
                    iconUrl = "image/seagull240.jpg",
                    availableBalance = avaiBalance,
                    unavailableBalance = unavaiBalance,
                    regTime = 1577808000000,
                    source = "testing",
                    balance = GetBalance(),
                    deleted = false
                };
            }

            public static object GetDriver()
            {
                return new
                {
                    identity = 500,
                    name = "张三",
                    gender = "man",
                    tel = "17600040004",
                    iconUrl = "demo-driver",
                    drivingLicenseStartDate = 1272643200000, //2010-5-1
                    jobNum = default(string), //serial number, current working task(order number)
                    level = 5, //rating

                    point = new { x = 120.1867, y = 30.2483 }, //west lake, HZ
                    status = "FREE",
                    serviceTimes = 567 //order count
                };
            }

            public static object GetDriver2()
            {
                return new
                {
                    identity = 501,
                    name = "李四",
                    gender = "woman",
                    tel = "17600050005",
                    iconUrl = default(object), //"demo-driver2",
                    drivingLicenseStartDate = 1396281600000, //2014-5-1
                    jobNum = default(string), //serial number, current working task(order number)
                    level = 4, //rating

                    point = new { x = 120.17, y = 30.26}, //west lake, HZ
                    status = "FREE",
                    serviceTimes = 1585 //order count
                };
            }

            public static object GetGeoDistance(double distance)
            {
                return new
                {
                   value = distance, //direct distance ??
                   metric = "km", //??
                   unit = "km",
                   normalizedValue = distance + 0.4 //??
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