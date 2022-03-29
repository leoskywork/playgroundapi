using System;
using System.Collections.Generic;
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
            var user = ReadMockingFileAsObject(300, DoHackUserIfNeed);
            return WrapResultJson(new { USER = user });
        }

        [HttpGet]
        [Route("account/getCurrentUser")]
        public IHttpActionResult Get___()
        {
            var user = ReadMockingFileAsObject(300, DoHackUserIfNeed);
            return WrapResultJson(user);
        }

        [HttpPost]
        [Route("account/getBalance")]
        public IHttpActionResult Get____()
        {
            var user = ReadMockingFileAsObject(300, DoHackUserIfNeed);
            return WrapResultJson((float)user.AsJObject()["balance"]);
        }

        [HttpPost]
        [Route("setting/fee/getPriceByCityAndDistance")]
        public IHttpActionResult Get__________()
        {
            return WrapResultJson(30.0 + System.DateTime.UtcNow.Second % 30);
        }

        [HttpGet]
        [Route("account/record")]
        public IHttpActionResult Get_____()
        {
            System.Threading.Thread.Sleep(1000);
            return WrapResultJson("demo transaction list", false, null, true);
        }

        [HttpPost]
        [Route("account/changeMyInfo")]
        public IHttpActionResult Get__44([FromBody]Dictionary<string, string> kvp)
        {
            if (kvp["name"] == "reset")
            {
                _temp = new TempCache();
                return WrapResultJson(default(object), true, "reset done");
            }
            else
            {
                _temp.NeedHackUser = true;
                _temp.HackUserName = kvp["name"];
                _temp.HackUserGender = kvp["gender"];

                var newUserInfo = ReadMockingFileAsObject(300, DoHackUserIfNeed);
                return WrapResultJson(newUserInfo);
            }
        }

        [HttpGet]
        [Route("account/logout")]
        public IHttpActionResult Get__444()
        {
            _temp.OrderStatus = 0;
            // _temp.ShowOrderHistory = false;
            return WrapResultJson(default(object));
        }

        [HttpGet]
        [Route("setting/fee/feeDescribeByCityName")]
        public IHttpActionResult Get______()
        {
            System.Threading.Thread.Sleep(1000);
            return WrapResultJson("demo fee rules", false, null, true);
        }

        [HttpPost]
        [Route("coupon/searchCurrent")]
        public IHttpActionResult Get__100()
        {
            System.Threading.Thread.Sleep(1000);
            return WrapResultJson(ReadMockingFileAsObject(500));
        }

        [HttpGet]
        [Route("shared/listence")]
        public IHttpActionResult Get__101()
        {
            System.Threading.Thread.Sleep(1000);
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

            _temp.NeedHackOrder = true;
            _temp.HackOrderStartAddress = body.planStartLoc.addr;
            _temp.HackOrderStartTimeSince1970 = _temp.GetMSSince1970(DateTime.UtcNow);
            _temp.HackOrderStartLat = body.planStartLoc.lat;
            _temp.HackOrderStartLng = body.planStartLoc.lng;
            _temp.HackOrderEndAddress = body.planEndLoc?.addr ?? null;

            var config = ReadMockingFileAs<MockConfig>(200);
            var fileObject = ReadMockingFileAsObject(config.create_order_file);
            DoHackOrderIfNeed(fileObject);

            return WrapResultJson(fileObject);
        }

        [HttpPost]
        [Route("driveOrder/{id:int}/cancel")]
        public IHttpActionResult Get__105(int id, [FromBody]CancelOrderBody body)
        {
            System.Threading.Thread.Sleep(100);

            _temp.CircleDriverCount = 2;
            _temp.OrderStatus = 110;
            _temp.ShowOrderHistory = true;
            _temp.HackCancelReason = body.reason;

            var fileObject = ReadMockingFileAsObject(102, DoHackOrderIfNeed);
            _temp.AddHistory(fileObject);


            return WrapResultJson(new { total = 0, id, cancelReason = "demo" });
        }

        #endregion

        #region Mocking - hack return data

        private static T ReadMockingFileAs<T>(int id)
        {
            string txt = ReadMockingFile("YF", id);
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(txt);
        }

        private static object ReadMockingFileAsObject(int id, Action<object> postProcess = null)
        {
            var txt = ReadMockingFile("YF", id);
            var fileObject = Newtonsoft.Json.JsonConvert.DeserializeObject(txt);

            postProcess?.Invoke(fileObject);

            return fileObject;
        }

        private static void DoHackOrderIfNeed(object file)
        {
            if (!_temp.NeedHackOrder) return;

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
                fileObject["planEndLoc"]["identity"] = DateTime.UtcNow.Millisecond;
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

            fileObject["waitMinutes"] = (int)(_temp.GetMSSince1970(DateTime.UtcNow) - _temp.HackOrderStartTimeSince1970) / 1000 / 60 + 1;
            fileObject["canceledReason"] = _temp.HackCancelReason;
        }

        private static void DoHackUserIfNeed(object file)
        {
            if (!_temp.NeedHackUser) return;

            var fileObject = file as Newtonsoft.Json.Linq.JObject;

            fileObject["name"] = _temp.HackUserName;
            fileObject["gender"] = _temp.HackUserGender;
        }

        private static void DoHackDriverDistance(object file)
        {
            var fileObject = file as Newtonsoft.Json.Linq.JObject;
            var distance = _temp.HackDriverDistance + (DateTime.UtcNow.Millisecond % 10) * 0.1;

            fileObject["distance"]["value"] = distance;
            fileObject["distance"]["normalizedValue"] = distance + 0.4;
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

            _temp.HackDriverDistance = (DateTime.UtcNow.Second % 30) * 0.2 + 1;

            var driver = ReadMockingFileAsObject(400, DoHackDriverDistance);
            System.Threading.Thread.Sleep(DateTime.UtcNow.Millisecond % 100);
            var driver2 = ReadMockingFileAsObject(401, DoHackDriverDistance);
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
                var fileObject = ReadMockingFileAsObject(100, DoHackOrderIfNeed);

                return WrapResultJson(new object[] { fileObject });
            }

            return WrapResultEmptyArray();
        }

        [HttpPost] //should be GET, return order by id
        [Route("driveOrder/{id:int}")]
        public IHttpActionResult Get__202(int id)
        {
            System.Threading.Thread.Sleep(100);

            if (_temp.IdList.Contains(id))
            {
                var fileObject = ReadMockingFileAsObject(id, DoHackOrderIfNeed);

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

        #endregion

        #region wrap result

        public IHttpActionResult WrapResultEmptyArray()
        {
            return WrapResultJson(new object[] { });
        }

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

        #endregion

        #region inner classes

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
                101, //order, paid
                102, //order, canceled
                199  //order template
                ,200 //config
                ,201
                ,300
                ,400
                ,401
                ,500
            };

            public bool NeedHackOrder { get; set; }
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


            public string HackCancelReason { get; set; }
            public double HackDriverDistance { get; set; }
            public List<object> HistoryOrders { get; } = new List<object>();


            public bool NeedHackUser { get; set; }
            public string HackUserName { get; set; }
            public string HackUserGender { get; set; }


            public void AddHistory(object history)
            {
                var jsonHistory = (Newtonsoft.Json.Linq.JObject)history;

                jsonHistory["identity"] = this.GetMSSince1970(DateTime.UtcNow) / 1000;
                jsonHistory["orderNo"] = "1708" + this.GetMSSince1970(DateTime.UtcNow).ToString();


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
            internal static int GetZoneId()
            {
                var config = MockYFController.ReadMockingFileAs<MockConfig>(200);
                var zone = MockYFController.ReadMockingFileAsObject(config.zone_file);
                return (int)zone.AsJObject()["identity"];
            }
        }

        private class MockConfig
        {
            public int zone_file { get; set; }
            public int create_order_file { get; set; }
        }

        public class OrderBody
        {
            public Location planStartLoc { get; set; }
            public Location planEndLoc { get; set; }
        }

        public class CancelOrderBody
        {
            public string reason { get; set; }
        }

        public class Location
        {
            public string addr { get; set; }
            public float lng { get; set; }
            public float lat { get; set; }
            public int identity { get; set; }
            public long locDate { get; set; }
        }

        #endregion

    }
}