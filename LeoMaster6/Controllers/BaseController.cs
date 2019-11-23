using LeoMaster6.Common;
using LeoMaster6.ErrorHandling;
using LeoMaster6.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;

namespace LeoMaster6.Controllers
{
    [LskExceptionFilter]
    public class BaseController : ApiController
    {
        //make it static member?? no, need pass in the log source for different sub classes
        protected log4net.ILog _logger;

        public BaseController()
        {
            _logger = log4net.LogManager.GetLogger(this.GetType());
        }


        #region helpers

        public static string GetDatapoolEntry()
        {
            return ConfigurationManager.AppSettings["lsk-dir-data-pool-entry"];
        }

        public static string GetBaseDirectory()
        {
            var machineKey = "lsk-env-" + Environment.MachineName;
            var envKey = ConfigurationManager.AppSettings.AllKeys.First(k => k.Equals(machineKey, StringComparison.OrdinalIgnoreCase));
            var dir = ConfigurationManager.AppSettings["lsk-dir-" + ConfigurationManager.AppSettings[envKey]];
            return dir;
        }

        protected bool AppendToFile(string path, string content, string alternatePath = null)
        {
            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }

            if (!File.Exists(path))
            {
                using (var fileWriter = new StreamWriter(File.Create(path)))
                {
                    //fileWriter.WriteAsync(content); //?? async, seems no need to do so
                    //the block is on server side, not on client side, and we need the return value reliable here
                    fileWriter.Write(content);
                    return true;
                }
            }
            else
            {
                try
                {
                    using (var fileWriter = new StreamWriter(path, true))
                    {
                        //fileWriter.WriteAsync(content);
                        fileWriter.Write(content);
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"Got an error while writing to {path}, going to save to alter file {alternatePath}.", ex);

                    //shouldn't have done this, just over thinking here and waste time
                    if (!string.IsNullOrEmpty(alternatePath) && !File.Exists(alternatePath))
                    {
                        using (var fileWriter = new StreamWriter(File.Create(alternatePath)))
                        {
                            fileWriter.WriteAsync(content);
                        }
                    }

                    return false;
                }
            }
        }

        protected bool AppendNoteToFile(string path, params DtoClipboardItem[] notes)
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

        /// <summary>
        /// Read all lines if pageIndex and pageSize not assigned
        /// </summary>
        protected static List<TValue> ReadLskjson<TKey, TValue>(string path, Action<Dictionary<TKey, TValue>, string> collector, int pageIndex = 0, int pageSize = int.MaxValue)
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
                //while (reader.Peek() >= 0 && items.Count < pageSize);
            }

            return items.Values.ToList();
        }

        protected static void CollectLskjsonLineClipboard(Dictionary<Guid, DtoClipboardItem> preItems, string currentLine)
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

        protected static void CollectLskjsonLineDefault<T>(Dictionary<Guid, T> preItems, string currentLine) where T: ILskjsonLine
        {
            if (string.IsNullOrWhiteSpace(currentLine)) return;

            var currentItem = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(currentLine);

            if (currentItem == null || currentItem.IsDeleted == true) return;

            preItems.Add(currentItem.Uid, currentItem);
        }


        protected static void CollectLskjsonIndex(Dictionary<Guid, DtoLskjsonIndex> preItems, string currentLine)
        {
            var currentItem = Newtonsoft.Json.JsonConvert.DeserializeObject<DtoLskjsonIndex>(currentLine);

            if (currentItem == null) return;

            preItems.Add(currentItem.Uid, currentItem);
        }

        protected DateTime? GetOriginCreatedAt(Guid uid)
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

        protected static string GetFullClipboardDataPath(DateTime time)
        {
            //not the normal week of year, this is too complex to determinate the date boundaries of every portion(file)
            //return Path.Combine(GetBaseDirectory(), GetDatapoolEntry(), "clip-" + (time.DayOfYear / 7 + 1).ToString("D2") + time.ToString("-yyyyMM") + ".json");

            return Path.Combine(GetBaseDirectory(), GetDatapoolEntry(), $"{ Constants.LskjsonPrefix }note-{ time.ToString("yyyyMM") }.txt");
        }

        protected static string GetFullClipboardIndexPath(DateTime time)
        {
            return Path.Combine(GetBaseDirectory(), GetDatapoolEntry(), $"{ Constants.LskjsonIndexFilePrefix }{ time.ToString("yyyy") }.txt");
        }

        protected bool CheckHeaderSession()
        {
            return Request.Headers.TryGetValues(Constants.HeaderSessionId, out IEnumerable<string> sessionIds) && sessionIds.FirstOrDefault() == Constants.DevSessionId;
        }

        protected bool AppendObjectToFile<T>(string path, params T[] items)
        {
            var builder = new StringBuilder();

            foreach (var item in items)
            {
                builder.Append(Newtonsoft.Json.JsonConvert.SerializeObject(item));
                builder.Append(Environment.NewLine);
            }

            return AppendToFile(path, builder.ToString());
        }

        protected void WriteToFile<T>(string path, IEnumerable<T> items)
        {
            var builder = new StringBuilder();

            foreach (var item in items)
            {
                builder.Append(Newtonsoft.Json.JsonConvert.SerializeObject(item));
                builder.Append(Environment.NewLine);
            }

            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }

            File.WriteAllText(path, builder.ToString());
        }

        #endregion
    }
}
