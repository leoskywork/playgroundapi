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
        public class PutBody
        {
            public Guid uid { get; set; }
            public string data { get; set; }
        }

        public class DeleteBody
        {
            public Guid uid { get; set; }
        }

        public class PostBody
        {
            public string data { get; set; }
            public DateTime? createdAt { get; set; }
        }

        [HttpPost]
        public IHttpActionResult Clipboard([FromBody] PostBody body)
        {
            if (body == null) throw new ArgumentNullException(nameof(body));
            if (string.IsNullOrEmpty(body.data)) throw new ArgumentNullException(nameof(body.data));

            //todo improve this, hard coded as dev session id for now
            if (!CheckHeaderSession())
            {
                //return Unauthorized();
            }

            var time = body.createdAt ?? DateTime.Now;
            var item = new DtoClipboardItem()
            {
                Uid = Guid.NewGuid(),
                CreatedBy = MapToUserId(Request.Headers.GetValues(Constants.HeaderSessionId).First()),
                CreatedAt = time,
                Data = body.data
            };

            string path = GetFullClipboardDataPath(time);
            AppendNoteToFile(path, item);

            string indexPath = GetFullClipboardIndexPath(time);
            AppendObjectToFile(indexPath, DtoLskjsonIndex.From(item));

            return DtoResultV5.Success(Json, $"{body.data.Length} characters saved to clipboard.");
        }


        [HttpPut]
        public IHttpActionResult Clipboard([FromBody]PutBody putBody)
        {
            if (putBody == null) throw new ArgumentNullException(nameof(putBody));

            //todo improve this, hard coded as dev session id for now
            if (!CheckHeaderSession())
            {
                //return Unauthorized();
            }

            var createdAt = GetOriginCreatedAt(putBody.uid);

            if (!createdAt.HasValue)
            {
                throw new InvalidOperationException("Data already deleted, please reload to latest then edit");
            }

            var path = GetFullClipboardDataPath(createdAt.Value); //ensure orig item and updated item in the same file

            if (!File.Exists(path)) return DtoResultV5.Success(this.Json, "no data");

            //perf - O(n) is 2n here, can be optimized to n
            var notes = ReadLskjson<Guid, DtoClipboardItem>(path, CollectLskjsonLine);
            var foundNote = notes.FirstOrDefault(n => n.Uid == putBody.uid);
            var foundChild = notes.FirstOrDefault(n => n.ParentUid == putBody.uid);

            //ensure the relation is a chain, not a tree
            if (foundChild != null)
            {
                throw new InvalidOperationException("Data already changed, please reload to latest then edit");
            }

            if (foundNote != null)
            {
                var newNote = Utility.DeepClone(foundNote);

                newNote.Uid = Guid.NewGuid(); //reset
                newNote.Data = putBody.data;

                //todo - replace with real UserId(get by session id)
                newNote.HasUpdated = true;
                newNote.LastUpdatedBy = Constants.DevUpdateUserId + DateTime.Now.ToString("dd");
                newNote.LastUpdatedAt = DateTime.Now;
                newNote.ParentUid = foundNote.Uid;

                AppendNoteToFile(path, newNote);
                AppendObjectToFile(GetFullClipboardIndexPath(newNote.CreatedAt), DtoLskjsonIndex.From(newNote));

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

            var items = ReadLskjson<Guid, DtoClipboardItem>(path, CollectLskjsonLine, pageIndex, pageSize);

            //todo - filter items by lskjson index file??

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


        //soft delete
        [HttpDelete]
        public IHttpActionResult Clipboard([FromBody]DeleteBody body)
        {
            if (body == null) throw new ArgumentNullException(nameof(body));

            var createdAt = GetOriginCreatedAt(body.uid);

            if (!createdAt.HasValue)
            {
                return DtoResultV5.Success(Json, "Data already deleted");
            }

            string path = GetFullClipboardDataPath(createdAt.Value); //ensure orig item and (soft)deleted item in the same file

            if (!File.Exists(path)) return DtoResultV5.Success(Json, "no data");

            var notes = ReadLskjson<Guid, DtoClipboardItem>(path, CollectLskjsonLine);
            var foundNote = notes.FirstOrDefault(n => n.Uid == body.uid);

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

        private static void CollectLskjsonLine(Dictionary<Guid, DtoClipboardItem> preItems, string currentLine)
        {
            var currentItem = Newtonsoft.Json.JsonConvert.DeserializeObject<DtoClipboardItem>(currentLine);

            if (currentItem == null) return;

            if (currentItem.HasDeleted != true)
            {
                preItems.Add(currentItem.Uid, currentItem);
            }

            if (currentItem.ParentUid.HasValue && preItems.ContainsKey(currentItem.ParentUid.Value))
            {
                preItems.Remove(currentItem.ParentUid.Value);
            }
        }

        private static void CollectLskjsonIndex(Dictionary<Guid, DtoLskjsonIndex> preItems, string currentLine)
        {
            var currentItem = Newtonsoft.Json.JsonConvert.DeserializeObject<DtoLskjsonIndex>(currentLine);

            if (currentItem == null) return;

            preItems.Add(currentItem.Uid, currentItem);
        }

        /// <summary>
        /// Read all lines if pageIndex and pageSize not assigned
        /// </summary>
        private static List<TValue> ReadLskjson<TKey, TValue>(string path, Action<Dictionary<TKey, TValue>, string> collector, int pageIndex = 0, int pageSize = int.MaxValue)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            if (collector == null) throw new ArgumentNullException(nameof(collector));

            //can't read entire file one time, the entire file is not in valid json format(for array)
            //hover every line is a valid json object
            var notEmptyLineNumber = 0;
            var pageStartIndex = pageSize * pageIndex + 1;
            var items = new Dictionary<TKey, TValue>();

            using (var reader = File.OpenText(path))
            {
                do
                {
                    //fixme - poor paging mechanism(have to read every line from top), but this is a small project
                    //?? top x lines or top x items? line can be empty, which means no item
                    var line = reader.ReadLine();

                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        notEmptyLineNumber++;

                        if (notEmptyLineNumber < pageStartIndex)
                        {
                            continue;
                        }

                        collector?.Invoke(items, line);
                    }
                }
                while (!reader.EndOfStream && items.Count < pageSize);
            }

            return items.Values.ToList();
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

            return Path.Combine(GetBaseDirectory(), GetDatapoolEntry(), $"{ Constants.LskjsonPrefix }note-{ time.ToString("yyyyMM") }.txt");
        }

        private static string GetFullClipboardIndexPath(DateTime time)
        {
            return Path.Combine(GetBaseDirectory(), GetDatapoolEntry(), $"{ Constants.LskjsonIndexFilePrefix }{ time.ToString("yyyy") }.txt");
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

        private bool AppendObjectToFile<T>(string path, params T[] items)
        {
            var builder = new StringBuilder();

            foreach (var item in items)
            {
                builder.Append(Newtonsoft.Json.JsonConvert.SerializeObject(item));
                builder.Append(Environment.NewLine);
            }

            return AppendToFile(path, builder.ToString());
        }

        private DateTime? GetOriginCreatedAt(Guid uid)
        {
            var possibleTime = DateTime.Now;

            for (int i = 0; i < Constants.LskjsonIndexFileAgeInYears; i++)
            {
                var indexPath = GetFullClipboardIndexPath(possibleTime.AddYears(-1 * i));

                if (File.Exists(indexPath))
                {
                    var indexes = ReadLskjson<Guid, DtoLskjsonIndex>(indexPath, CollectLskjsonIndex);
                    var found = indexes.FirstOrDefault(lj => lj.Uid == uid);

                    if (found != null)
                    {
                        return found.OriginCreatedAt;
                    }
                }
            }

            return null;
        }

        #endregion
    }
}
