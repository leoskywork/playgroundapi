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
    public class NoteController : BaseController
    {

        public class PostBody
        {
            public string Data { get; set; }
            public DateTime? CreatedAt { get; set; }
        }

        public class PutBody
        {
            //public Guid Uid { get; set; }
            public string Uid { get; set; }
            public string Data { get; set; }
        }

        [HttpPost]
        public IHttpActionResult Clipboard([FromBody]PostBody body)
        {
            if (body == null) throw new ArgumentNullException(nameof(body));
            if (string.IsNullOrEmpty(body.Data)) throw new ArgumentNullException(nameof(body.Data));

            //todo improve this, hard coded as dev session id for now
            if (!CheckHeaderSession())
            {
                //return Unauthorized();
            }

            var time = body.CreatedAt ?? DateTime.Now;
            var item = new DtoClipboardItem()
            {
                Uid = Guid.NewGuid(),
                CreatedBy = MapToUserId(Request.Headers.GetValues(Constants.HeaderSessionId).First()),
                CreatedAt = time,
                Data = body.Data
            };

            string path = GetFullClipboardDataPath(time);
            AppendNoteToFile(path, item);

            string indexPath = GetFullClipboardIndexPath(time);
            AppendObjectToFile(indexPath, DtoLskjsonIndex.From(item));

            return DtoResultV5.Success(Json, $"{body.Data.Length} characters saved to clipboard.");
        }


        //working on localhost but not on prod???
        [HttpPut]
        public IHttpActionResult Clipboard([FromBody]PutBody putBody)
        {
            if (putBody == null) throw new ArgumentNullException(nameof(putBody));
            if (putBody.Uid == null) throw new ArgumentNullException(nameof(putBody.Uid));

            var inputUid = Guid.Parse(putBody.Uid);

            //todo improve this, hard coded as dev session id for now
            if (!CheckHeaderSession())
            {
                //return Unauthorized();
            }

            var createdAt = GetOriginCreatedAt(inputUid);

            if (!createdAt.HasValue)
            {
                throw new InvalidOperationException("Data already deleted, please reload to latest then edit");
            }

            var path = GetFullClipboardDataPath(createdAt.Value); //ensure orig item and updated item in the same file

            if (!File.Exists(path)) return DtoResultV5.Success(this.Json, "no data");

            //perf - O(n) is 2n here, can be optimized to n
            var notes = ReadLskjson<Guid, DtoClipboardItem>(path, CollectLskjsonLineClipboard);
            var foundNote = notes.FirstOrDefault(n => n.Uid == inputUid);
            var foundChild = notes.FirstOrDefault(n => n.ParentUid == inputUid);

            //ensure the relation is a chain, not a tree
            if (foundChild != null)
            {
                throw new InvalidOperationException("Data already changed, please reload to latest then edit");
            }

            if (foundNote != null)
            {
                var newNote = Utility.DeepClone(foundNote);

                newNote.Uid = Guid.NewGuid(); //reset
                newNote.Data = putBody.Data;

                //todo - replace with real UserId(get by session id)
                newNote.HasUpdated = true;
                newNote.LastUpdatedBy = Constants.DevUpdateUserId + DateTime.Now.ToString("dd");
                newNote.LastUpdatedAt = DateTime.Now;
                newNote.ParentUid = foundNote.Uid;

                AppendNoteToFile(path, newNote);
                AppendObjectToFile(GetFullClipboardIndexPath(newNote.CreatedAt), DtoLskjsonIndex.From(newNote));

                //return DtoResultV5.Success(Json, MapToJSNameConvention(newNote), "Data updated");
                return DtoResultV5.Success(Json, newNote, "Data updated"); //do the map on client side
            }
            else
            {
                return DtoResultV5.Fail(Json, "The data you want to update may have been deleted");
            }
        }

        //todo pass in the DateTime, page size, page index
        [HttpGet]
        public IHttpActionResult Clipboard()
        {
            int pageSize = 50;
            int pageIndex = 0;
            DateTime time = DateTime.Now;

            if (!CheckHeaderSession())
            {
                //return Unauthorized();
            }

            string path = GetFullClipboardDataPath(time);

            if (!File.Exists(path))
            {
                return DtoResultV5.Success(Json, "no data");
            }

            var items = ReadLskjson<Guid, DtoClipboardItem>(path, CollectLskjsonLineClipboard, pageIndex, pageSize);

            //read all files created this year and till the month of passed in time
            var oldFileTime = new DateTime(time.Year, time.Month, 1);
            var endFileTime = new DateTime(time.Year, 1, 1);

            while (oldFileTime > endFileTime)
            {
                oldFileTime = oldFileTime.AddMonths(-1);
                var oldFilePath = GetFullClipboardDataPath(oldFileTime);

                if (File.Exists(oldFilePath))
                {
                    var oldData = ReadLskjson<Guid, DtoClipboardItem>(oldFilePath, CollectLskjsonLineClipboard, 0, int.MaxValue);
                    var numberOfDataToAdd = Constants.LskMaxReturnNoteCount - items.Count;
                    items.AddRange(oldData.Take(Math.Min(numberOfDataToAdd, oldData.Count)));

                    if (items.Count >= Constants.LskMaxReturnNoteCount)
                    {
                        break;
                    }
                }
            }


            //todo - filter items by lskjson index file??

            //var jsonObjects = MapToJSNameConvention(items);
            var jsonObjects = items; //do the mapping on client side
            return DtoResultV5.Success(Json, jsonObjects, "v5");

            //following code works only when the entire file is in valid format - which is not the case here
            //since we just append new line to the file(a proper json array string should quote by '[]' and item separated by ',')
            //  - a workaround for saving is deserialize entire file into a collection, add the new item then save the collection to the file again which is poor performance
            //using (var reader = File.OpenText(GetFullClipboardDataPath(DateTime.MinValue)))     
            //using (var jsonReader = new Newtonsoft.Json.JsonTextReader(reader))
            //{
            //    var serializer = new Newtonsoft.Json.JsonSerializer();
            //    var items = serializer.Deserialize<DtoClipboardItem[]>(jsonReader);
            //    return Json(items);
            //}
        }


        //soft delete
        //[HttpDelete] // move to RestfulNoteController
       


        #region helpers

        private static IEnumerable<object> MapToJSNameConvention(IEnumerable<DtoClipboardItem> items)
        {
            if (items != null && items.Any())
            {
                return items.Select(i => MapToJSNameConvention(i));
            }

            return items;
        }

        private static object MapToJSNameConvention(DtoClipboardItem item)
        {
            dynamic trimedObject = new ExpandoObject();

            trimedObject.uid = item.Uid;
            trimedObject.createdBy = item.CreatedBy;
            trimedObject.createdAt = item.CreatedAt;
            trimedObject.data = item.Data;

            if (item.HasUpdated == true)
            {
                trimedObject.hasUpdated = item.HasUpdated;
                trimedObject.lastUpdatedBy = item.LastUpdatedBy;
                trimedObject.lastUpdatedAt = item.LastUpdatedAt;
                trimedObject.parentUid = item.ParentUid;
            }

            return trimedObject;
        }

        private static string MapToUserId(string sessionId)
        {
            if (sessionId == Constants.DevSessionId)
            {
                return Constants.DevUserId;
            }

            throw new NotImplementedException();
        }

      

        #endregion
    }
}
