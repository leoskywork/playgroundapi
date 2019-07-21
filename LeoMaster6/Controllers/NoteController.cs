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
        [HttpPost]
        public IHttpActionResult Clipboard([FromBody]string data)
        {
            if (string.IsNullOrEmpty(data)) throw new ArgumentNullException(nameof(data));

            //todo improve this, hard coded as dev session id for now
            if (!CheckHeaderSession())
            {
                //return Unauthorized();
            }

            var item = new DtoClipboardItem()
            {
                Uid = Guid.NewGuid(),
                CreatedBy = MapToUserId(Request.Headers.GetValues(Constants.HeaderSessionId).First()),
                CreatedAt = DateTime.Now,
                Data = data
            };

            string path = GetFullClipboardDataPath(DateTime.Now);
            AppendNoteToFile(path, item);

            return DtoResult.Success($"{data.Length} characters saved to clipboard.").To(Json);
        }

        public class PutBody
        {
            public string data { get; set; }
            public Guid uid { get; set; }
            public DateTime createdAt { get; set; }
        }

        [HttpPut]
        public IHttpActionResult Clipboard([FromBody]PutBody putBody)
        {
            if (putBody == null) throw new ArgumentNullException(nameof(putBody));

            Guid uid = putBody.uid;
            string data = putBody.data;
            DateTime createdAt = putBody.createdAt;

            //todo improve this, hard coded as dev session id for now
            if (!CheckHeaderSession())
            {
                //return Unauthorized();
            }

            string path = GetFullClipboardDataPath(createdAt); //ensure orig item and updated item in the same file
            if (!File.Exists(path)) return DtoResultV5.Success(this.Json, "no data");

            //perf - O(n) is 2n here, can be optimized to n
            var notes = ReadLskjson(path, (line) => Newtonsoft.Json.JsonConvert.DeserializeObject<DtoClipboardItem>(line), i => i.HasDeleted != true);
            var foundNote = notes.FirstOrDefault(n => n.Uid == uid);
            var foundChild = notes.FirstOrDefault(n => n.ParentUid == uid);

            //ensure the relation is a chain, not a tree
            if (foundChild != null)
            {
                throw new InvalidOperationException("Data already changed, please reload to latest then edit");
            }

            if (foundNote != null)
            {
                var newNote = Utility.DeepClone(foundNote);

                newNote.Uid = Guid.NewGuid(); //reset
                newNote.Data = data;

                //todo - replace with real UserId(get by session id)
                newNote.HasUpdated = true;
                newNote.LastUpdatedBy = Constants.DevUpdateUserId + DateTime.Now.ToString("dd");
                newNote.LastUpdatedAt = DateTime.Now;
                newNote.ParentUid = foundNote.Uid;

                AppendNoteToFile(path, newNote);

                return DtoResultV5.Success(Json, MapToJSNameConvention(newNote), "Data updated");
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

            var items = ReadLskjson(path, (line) => Newtonsoft.Json.JsonConvert.DeserializeObject<DtoClipboardItem>(line), i => i.HasDeleted != true, pageIndex, pageSize);


            var jsonObjects = MapToJSNameConvention(items);
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

        public class DeleteBody
        {
            public Guid uid { get; set; }
            public DateTime createdAt { get; set; }
        }

        //soft delete
        [HttpDelete]
        public IHttpActionResult Clipboard([FromBody]DeleteBody body)
        {
            if (body == null) throw new ArgumentNullException(nameof(body));

            Guid uid = body.uid;
            DateTime createdAt = body.createdAt;


            string path = GetFullClipboardDataPath(createdAt); //ensure orig item and (soft)deleted item in the same file

            if (!File.Exists(path)) return DtoResultV5.Success(Json, "no data");

            var notes = ReadLskjson(path, line => Newtonsoft.Json.JsonConvert.DeserializeObject<DtoClipboardItem>(line), i => i.HasDeleted != true);
            var foundNote = notes.FirstOrDefault(n => n.Uid == uid);

            if (foundNote == null) return DtoResultV5.Success(Json, "already deleted");

            foundNote.HasDeleted = true;
            //todo - replace with real UserId(get by session id)
            foundNote.DeletedBy = Constants.DevDeleteUserId + DateTime.Now.ToString("dd");
            foundNote.DeletedAt = DateTime.Now;

            //need replace a line in the file - seems there is no way to just rewrite one line, have to re-write entire file
            // - https://stackoverflow.com/questions/1971008/edit-a-specific-line-of-a-text-file-in-c-sharp
            // - https://stackoverflow.com/questions/13509532/how-to-find-and-replace-text-in-a-file-with-c-sharp
            var backupPath = path.Replace(Constants.LskjsonPrefix, Constants.LskjsonPrefix + DateTime.Now.ToString("dd-HHmmss-"));
            File.Move(path, backupPath);
            var success = AppendNoteToFile(path, notes.ToArray());

            if (success)
            {
                File.Delete(backupPath);
                return DtoResultV5.Success(Json, "Data deleted");
            }
            else
            {
                File.Move(backupPath, path);
                return DtoResultV5.Fail(Json, "Failed to delete data");
            }
        }


        #region helpers

        /// <summary>
        /// Read all lines if pageIndex and pageSize not assigned
        /// </summary>
        private static List<T> ReadLskjson<T>(string path, Func<string, T> mapper, Predicate<T> valid = null, int pageIndex = 0, int pageSize = int.MaxValue)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));

            //can't read entire file one time, the entire file is not in valid json format(for array)
            //hover every line is a valid json object
            var items = new List<T>();
            var validLineNumber = 0;
            var pageStartIndex = pageSize * pageIndex + 1;

            using (var reader = File.OpenText(path))
            {
                do
                {
                    //fixme - poor paging mechanism(have to read every line from top), but this is a small project
                    //?? top x lines or top x items? line can be empty, which means no item
                    var line = reader.ReadLine();

                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        validLineNumber++;

                        if (validLineNumber >= pageStartIndex)
                        {
                            var item = mapper(line);

                            if (valid == null || valid(item))
                            {
                                items.Add(mapper(line));
                            }
                            else
                            {
                                validLineNumber--;
                            }
                        }
                    }
                }
                while (!reader.EndOfStream && items.Count < pageSize);
            }

            return items;
        }

        private static IEnumerable<object> MapToJSNameConvention(IEnumerable<DtoClipboardItem> items)
        {
            if (items?.Count() > 0)
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

        private bool CheckHeaderSession()
        {
            return Request.Headers.TryGetValues(Constants.HeaderSessionId, out IEnumerable<string> sessionIds) && sessionIds.FirstOrDefault() == Constants.DevSessionId;
        }

        private static string MapToUserId(string sessionId)
        {
            if (sessionId == Constants.DevSessionId)
            {
                return Constants.DevUserId;
            }

            throw new NotImplementedException();
        }

        private static string GetFullClipboardDataPath(DateTime time)
        {
            //not the normal week of year, this is too complex to determinate the date boundaries of every portion(file)
            //return Path.Combine(GetBaseDirectory(), GetDatapoolEntry(), "clip-" + (time.DayOfYear / 7 + 1).ToString("D2") + time.ToString("-yyyyMM") + ".json");

            return Path.Combine(GetBaseDirectory(), GetDatapoolEntry(), $"{Constants.LskjsonPrefix}note-" + time.ToString("yyyyMM") + ".txt");
        }

        private bool AppendNoteToFile(string path, params DtoClipboardItem[] notes)
        {
            var builder = new StringBuilder();

            foreach (var note in notes)
            {
                dynamic trimedNote = new ExpandoObject();
                trimedNote.Uid = note.Uid;
                trimedNote.CreatedBy = note.CreatedBy;
                trimedNote.CreatedAt = note.CreatedAt;
                trimedNote.Data = note.Data;

                if (note.HasUpdated == true)
                {
                    trimedNote.HasUpdated = note.HasUpdated;
                    trimedNote.LastUpdatedBy = note.LastUpdatedBy;
                    trimedNote.LastUpdatedAt = note.LastUpdatedAt;
                    trimedNote.ParentUid = note.ParentUid;
                }

                if (note.HasDeleted == true)
                {
                    trimedNote.HasDeleted = note.HasDeleted;
                    trimedNote.DeletedBy = note.DeletedBy;
                    trimedNote.DeletedAt = note.DeletedAt;
                }

                builder.Append(Newtonsoft.Json.JsonConvert.SerializeObject(trimedNote));
                builder.Append(Environment.NewLine);
            }

            return AppendToFile(path, builder.ToString());
        }

        #endregion
    }
}
