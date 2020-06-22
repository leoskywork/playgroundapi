using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;
using System.Xml.Serialization;

namespace LeoMaster6.Models
{
    [Serializable]
    public class DtoRoutine
    {
        public Guid Uid { get; set; }
        public string Name { get; set; }
        public DateTime? LastFulfill { get; set; }
        public string LastRemark { get; set; }
        public DtoFulfillmentArchive[] HistoryFulfillments { get; set; }
        public bool HasArchived { get; set; }
        public bool EnableSchedule { get; set; }
        public int? RecursiveIntervalDays { get; set; }

        //optional fields
        public string CreateBy { get; set; }
        public DateTime? CreateAt { get; set; } = DateTime.Now;
        public bool? IsDeleted { get; set; }
        public string DeleteReason { get; set; }


        public override string ToString()
        {
            return $"{Name}_{LastFulfill?.ToString()}_{Uid}_{CreateBy}";
        }

        public static DtoRoutine From(RoutineFulfillment fulfill, bool includeHistory)
        {
            var history = includeHistory ? fulfill.StagedArchives?.Select(h => DtoFulfillmentArchive.From(h))?.ToArray() : null;
            FulfillmentArchive lastFulfill = null;

            if (fulfill.StagedArchives?.Length > 0)
            {
                for (int i = fulfill.StagedArchives.Length - 1; i >= 0; i--)
                {
                    if (fulfill.StagedArchives[i].IsDeleted == null || !fulfill.StagedArchives[i].IsDeleted.Value)
                    {
                        lastFulfill = fulfill.StagedArchives[i];
                        break;
                    }
                }
            }

            if (lastFulfill == null && fulfill.HasArchived)
            {
                //fixme, read from archive file, do this outside this method
                //also change front end logic once do it, get lastFulfill directly instead of search among staged items?
                //  - exception when user just delete a history item and we want to repaint UI
                //    may also return last fulfill id ? so we can tall if last fulfill still valid after the delete
            }

            return new DtoRoutine()
            {
                Uid = fulfill.Uid,
                Name = fulfill.Name,
                LastFulfill = lastFulfill?.Time,
                LastRemark = lastFulfill?.Remark,
                HistoryFulfillments = history,
                HasArchived = fulfill.HasArchived,
                EnableSchedule = fulfill.EnableSchedule,
                RecursiveIntervalDays = fulfill.RecursiveIntervalDays,
                CreateAt = fulfill.CreateAt,
                CreateBy = fulfill.CreateBy,
                IsDeleted = fulfill.IsDeleted,
                DeleteReason = fulfill.DeleteReason
            };
        }
    }

    [Serializable]
    public class DtoFulfillmentArchive
    {
        public Guid ParentUid { get; set; }
        public Guid Uid { get; set; }
        public string Remark { get; set; }
        public DateTime Time { get; set; }
        public bool? IsDeleted { get; set; }
        public string DeleteReason { get; set; }

        public static DtoFulfillmentArchive From(FulfillmentArchive archive)
        {
            return new DtoFulfillmentArchive()
            {
                ParentUid = archive.ParentUid,
                Uid = archive.Uid,
                Remark = archive.Remark,
                Time = archive.Time,
                IsDeleted = archive.IsDeleted,
                DeleteReason = archive.DeleteReason
            };
        }
    }


    public class RoutineFulfillment : ILskjsonLine
    {
        public Guid Uid { get; set; }
        public string Name { get; set; }

        /// <summary>
        /// deprecated - replaced by staged archives
        /// </summary>
        public DateTime? LastFulfill { get; set; }
        /// <summary>
        /// deprecated - replaced by staged archives
        /// </summary>
        public string LastRemark { get; set; }

        /// <summary>
        /// deprecated - replaced by StagedArchives
        /// </summary>
        public DateTime[] HistoryFulfillments { get; set; }
        public bool HasMigrated { get; set; }
        public bool HasArchived { get; set; }
        /// <summary>
        /// used to replace HistoryFulfillments
        /// </summary>
        public FulfillmentArchive[] StagedArchives { get; set; }


        public bool EnableSchedule { get; set; }
        public int? RecursiveIntervalDays { get; set; }


        //optional fields
        public string CreateBy { get; set; }
        public DateTime? CreateAt { get; set; } = DateTime.Now;

        //can't do this, since we also relay on json serializer when save to file
        //[XmlIgnore]
        //[ScriptIgnore]
        //[JsonIgnore]
        public string UpdateBy { get; set; }
        //[XmlIgnore]
        //[ScriptIgnore]
        //[JsonIgnore]
        public DateTime? UpdateAt { get; set; }

        //[XmlIgnore]
        //[ScriptIgnore]
        //[JsonIgnore]
        public bool? IsDeleted { get; set; }

        //[XmlIgnore]
        //[ScriptIgnore]
        //[JsonIgnore]
        public string DeletedBy { get; set; }
        //[XmlIgnore]
        //[ScriptIgnore]
        //[JsonIgnore]
        public DateTime? DeleteAt { get; set; }

        public string DeleteReason { get; set; }


        public override string ToString()
        {
            return $"{Name}_{IsDeleted?.ToString()}_{Uid}_{CreateBy}";
        }
    }

    public class FulfillmentArchive : ILskjsonLine
    {
        public Guid ParentUid { get; set; }
        public Guid Uid { get; set; }
        public DateTime Time { get; set; }
        public string Remark { get; set; }

        //optional fields
        public string CreateBy { get; set; }
        public DateTime? CreateAt { get; set; } = DateTime.Now;

        public bool? IsDeleted { get; set; }
        public string DeletedBy { get; set; }
        public DateTime? DeleteAt { get; set; }
        public string DeleteReason { get; set; }

        public FulfillmentArchive()
        {

        }

        public FulfillmentArchive(Guid parent, string remark,  DateTime? time = null, string createBy = null, DateTime? createAt = null)
        {
            this.ParentUid = parent;
            this.Uid = Guid.NewGuid();
            this.Time = time ?? DateTime.Now;
            this.Remark = remark;
            this.CreateBy = createBy;

            if (createAt.HasValue)
            {
                this.CreateAt = createAt;
            }

        }

    }

}