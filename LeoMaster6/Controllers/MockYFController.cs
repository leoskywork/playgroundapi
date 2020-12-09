using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Http;

namespace LeoMaster6.Controllers
{
    [RoutePrefix("mock-yf")]
    public class MockYFController : MockController
    {
        private static TempCache _temp = new TempCache();

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
            return WrapResultJson("18612345678");
        }

        [HttpPost]
        [Route("setting/zone/getZoneIdByCityName")]
        public IHttpActionResult Get__103()
        {
            return WrapResultJson(MockData.GetZoneId());
        }

        [HttpPost]
        [Route("driveOrder/create")]
        public IHttpActionResult Get__104([FromBody] OrderBody body)
        {
            System.Threading.Thread.Sleep(100);

            _temp.CircleDriverCount = 1;
            _temp.OrderStatus = 1;

            _temp.UseHack = true;
            _temp.HackOrderStartAddress = body.planStartLoc.addr;
            _temp.HackOrderStartTimeSince1970 = _temp.GetMSSince1970(DateTime.Now);
            _temp.HackOrderStartLat = body.planStartLoc.lat;
            _temp.HackOrderStartLng = body.planStartLoc.lng;
            _temp.HackOrderEndAddress = body.planEndLoc?.addr ?? null;

            var fileObject = ReadMockingFileAsObject(100);

            //var temp = ReadMockingFileAsObject(100);
            //var temp2 = (Newtonsoft.Json.Linq.JObject)temp;

            DoHackLocations(fileObject);

            return WrapResultJson(fileObject);
        }

        [HttpPost]
        [Route("driveOrder/{id:int}/cancel")]
        public IHttpActionResult Get__105(int id)
        {
            System.Threading.Thread.Sleep(100);

            //string order = ReadMockingPath(100);
            _temp.CircleDriverCount = 2;
            _temp.OrderStatus = 110;
            _temp.ShowOrderHistory = true;

            var fileObject = ReadMockingFileAsObject(200);
            if (_temp.UseHack) { DoHackLocations(fileObject); }
            _temp.AddHistory(fileObject);


            return WrapResultJson(new { total = 0, id, cancelReason = "demo" });
        }

        #endregion

        #region Mocking - return null data

        public class OrderBody
        {
            public Location planStartLoc { get; set; }
            public Location planEndLoc { get; set; }
            public Location startWaitNode { get; set; }
        }

        public class Location
        {
            public string addr { get; set; }
            public float lng { get; set; }
            public float lat { get; set; }
            public int identity { get; set; }
            public long locDate { get; set; }
        }


        private T ReadMockingFileAs<T>(int id)
        {
            string temp = ReadMockingFile(id);
            //return Ok(order); //contains '\n' and '\t' when return this way

            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(temp);
        }

        private object ReadMockingFileAsObject(int id)
        {
            string temp = ReadMockingFile(id);
            //return Ok(order); //contains '\n' and '\t' when return this way

            return Newtonsoft.Json.JsonConvert.DeserializeObject(temp);
        }

        private string ReadMockingFile(int id)
        {
            string path = GetMockingPath(id);

            if (string.IsNullOrEmpty(path)) throw new InvalidOperationException("no mocking file for " + id);

            return File.ReadAllText(path);
        }

        private string GetMockingPath(int id)
        {
            string dir = GetMockingDir("YF");

            string path = Directory.GetFiles(dir).FirstOrDefault(f => f.Replace('/', '@').Replace('\\', '@').Contains("@YF@" + id + "-"));

            return path;
        }

        [HttpGet]
        [Route("pos/circle")]
        public IHttpActionResult Get__1(double longitude, double latitude)
        {
            double allowOffset = 0.15;

            if (Math.Abs(longitude - 120.2) > allowOffset || Math.Abs(latitude - 30.2) > allowOffset)
            {
                return WrapResultJson(new object[] { });
            }

            var dis = (DateTime.Now.Second % 30) * 0.2; //diff * 100;

            var driver = new
            {
                content = MockData.GetDriver(),
                distance = MockData.GetGeoDistance(2.0 + dis)
            };

            var driver2 = new
            {
                content = MockData.GetDriver2(),
                distance = MockData.GetGeoDistance(1.5)
            };

            var drivers = new object[] { driver, driver2 };

            if (_temp.CircleDriverCount == 1)
            {
                drivers = new object[] { driver };
            }

            return WrapResultJson(new { content = drivers });
        }

        [HttpPost]
        [Route("driveOrder/realtime")]
        public IHttpActionResult Get__2()
        {
            System.Threading.Thread.Sleep(500);

            if (_temp.OrderStatus == 1)
            {
                var fileObject = ReadMockingFileAsObject(100);
                if (_temp.UseHack) { DoHackLocations(fileObject); }

                return WrapResultJson(new object[] { fileObject });
            }

            return WrapResultEmptyArray();
        }

        private static void DoHackLocations(object file)
        {
            var fileObject = file as Newtonsoft.Json.Linq.JObject;

            fileObject["planStartLoc"]["addr"] = _temp.HackOrderStartAddress;
            fileObject["planStartLoc"]["locDate"] = _temp.HackOrderStartTimeSince1970;
            fileObject["planStartLoc"]["lat"] = _temp.HackOrderStartLat;
            fileObject["planStartLoc"]["lng"] = _temp.HackOrderStartLng;

            if (_temp.HackOrderEndAddress == null)
            {
                fileObject["planEndLoc"] = null;
            }
            else
            {
                fileObject["planEndLoc"]["identity"] = DateTime.Now.Millisecond;
                fileObject["planEndLoc"]["addr"] = _temp.HackOrderEndAddress;
            }

            if (fileObject["pickOrderNode"] != null)
            {
                fileObject["pickOrderNode"]["locDate"] = _temp.HackOrderWaitingStartTimeSince1970;
            }

            if (fileObject["createOrderNode"] != null)
            {
                fileObject["createOrderNode"]["locDate"] = _temp.HackOrderStartTimeSince1970;
            }

            fileObject["waitMinutes"] = (int) (_temp.GetMSSince1970(DateTime.Now) - _temp.HackOrderStartTimeSince1970) / 1000 / 60  + 1;
            
        }

        [HttpPost] //should be GET, return order by id
        [Route("driveOrder/{id:int}")]
        public IHttpActionResult Get__202(int id)
        {
            System.Threading.Thread.Sleep(1500);

            if (_temp.IdList.Contains(id))
            {
                var fileObject = ReadMockingFileAsObject(id);
                if (_temp.UseHack) { DoHackLocations(fileObject); }

                return WrapResultJson(fileObject);
            }

            return WrapResultJson(default(object));
        }

        [HttpPost]
        [Route("driveOrder/history")]
        public IHttpActionResult Get__3()
        {
            //System.Threading.Thread.Sleep(1000);

            if (_temp.OrderStatus == 110 || _temp.ShowOrderHistory)
            {
                return WrapResultJson(_temp.HistoryOrders.ToArray());
            }

            return WrapResultEmptyArray();
        }

        [HttpGet]
        [Route("account/logout")]
        public IHttpActionResult Get__4()
        {
            _temp.OrderStatus = 0;
           // _temp.ShowOrderHistory = false;
            return WrapResultJson(default(object));
        }

        #endregion

        private class TempCache
        {
            public int CircleDriverCount { get; set; } = 2;
            //
            // 1 - created
            // 110 - canceled
            //
            public int OrderStatus { get; set; } = 0;
            public bool ShowOrderHistory { get; set; }

            public int[] IdList { get; } = new int[] {
                100, //order, after creating, driver waiting

                200, //order, canceled
                201, //order, paid
                299  //order template
            };

            public bool UseHack { get; set; }
            public string HackOrderStartAddress { get; set; }
            public float HackOrderStartLat { get; set; }
            public float HackOrderStartLng { get; set; }
            public string HackOrderEndAddress { get; set; }
            public long HackOrderStartTimeSince1970 { get; set; }
            public long HackOrderWaitingStartTimeSince1970
            {
                get
                {
                    return this.HackOrderStartTimeSince1970 + 1000 * 60;
                }
            }


            public List<object> HistoryOrders { get; } = new List<object>();

            public void AddHistory(object history)
            {
                var jsonHistory = (Newtonsoft.Json.Linq.JObject)history;

                jsonHistory["identity"] = this.GetMSSince1970(DateTime.Now) / 1000;
                jsonHistory["orderNo"] = "1708" + this.GetMSSince1970(DateTime.Now).ToString();


                if (HistoryOrders.Count >= 10)
                {
                    HistoryOrders.RemoveAt(0);
                }

                HistoryOrders.Add(history);
            }

            public long GetMSSince1970(DateTime time)
            {
                return (long)(time - new DateTime(1970, 1, 1)).TotalMilliseconds;
            }
        }

        private class MockData
        {
            internal static readonly double avaiBalance = 2000;  //can withdraw to bank card
            internal static readonly double unavaiBalance = 100; //can not withdraw to bank card


            public static object GetUser()
            {
                return new
                {
                    identity = 100,
                    name = "刘立想",
                    gender = "man",
                    tel = "18612341234",
                    iconUrl = "demo-user",
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
                    gender = "woman",
                    tel = "17600040004",
                    iconUrl = default(object), //show the default avatar
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
                    gender = "man",
                    tel = "17600050005",
                    iconUrl = "demo-driver",
                    drivingLicenseStartDate = 1396281600000, //2014-5-1
                    jobNum = default(string), //serial number, current working task(order number)
                    level = 4, //rating

                    point = new { x = 120.17, y = 30.27 }, //west lake, HZ
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
                System.Threading.Thread.Sleep(1500);

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

            public static int GetZoneId()
            {
                return 100;
            }

        }


        public IHttpActionResult WrapResultEmptyArray()
        {
            return WrapResultJson(new object[] { });
        }

        //class ApiResult
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
                map = data//bad design, just for compatible here 
            };
        }
    }
}