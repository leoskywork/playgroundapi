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

        //optional fields
        public string CreateBy { get; set; }
        public DateTime? CreateAt { get; set; } = DateTime.Now;


        public override string ToString()
        {
            return $"{Name}_{LastFulfill?.ToString()}_{Uid}_{CreateBy}";
        }

        public static DtoRoutine From(RoutineFulfillment fulfill, bool includeHistory)
        {
            var history = includeHistory ? fulfill.StagedArchives?.Select(h => DtoFulfillmentArchive.From(h))?.ToArray() : null;

            return new DtoRoutine()
            {
                Uid = fulfill.Uid,
                Name = fulfill.Name,
                LastFulfill = fulfill.LastFulfill,
                LastRemark = fulfill.LastRemark,
                HistoryFulfillments = history,
                CreateAt = fulfill.CreateAt,
                CreateBy = fulfill.CreateBy
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

        public static DtoFulfillmentArchive From(FulfillmentArchive archive)
        {
            return new DtoFulfillmentArchive()
            {
                ParentUid = archive.ParentUid,
                Uid = archive.Uid,
                Remark = archive.Remark,
                Time = archive.FulfillmentTime
            };
        }
    }


    public class RoutineFulfillment : ILskjsonLine
    {
        public Guid Uid { get; set; }
        public string Name { get; set; }
        public DateTime? LastFulfill { get; set; }
        public string LastRemark { get; set; }
        /// <summary>
        /// deprecated - replaced by StagedArchives
        /// </summary>
        public DateTime[] HistoryFulfillments { get; set; }
        public bool HasMigrated { get; set; }

        public FulfillmentArchive[] StagedArchives { get; set; }

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



        public override string ToString()
        {
            return $"{Name}_{LastFulfill?.ToString()}_{Uid}_{CreateBy}";
        }
    }

    public class FulfillmentArchive : ILskjsonLine
    {
        public Guid ParentUid { get; set; }
        public Guid Uid { get; set; }
        public DateTime FulfillmentTime { get; set; }
        public string Remark { get; set; }

        //optional fields
        public string CreateBy { get; set; }
        public DateTime? CreateAt { get; set; } = DateTime.Now;

        public bool? IsDeleted { get; set; }
        public string DeletedBy { get; set; }
        public DateTime? DeleteAt { get; set; }

        public FulfillmentArchive()
        {

        }

        public FulfillmentArchive(Guid parent, string remark,  DateTime? time = null, string createBy = null, DateTime? createAt = null)
        {
            this.ParentUid = parent;
            this.Uid = Guid.NewGuid();
            this.FulfillmentTime = time ?? DateTime.Now;
            this.Remark = remark;
            this.CreateBy = createBy;

            if (createAt.HasValue)
            {
                this.CreateAt = createAt;
            }

        }

        public static FulfillmentArchive FromLast(RoutineFulfillment fulfill)
        {
            return new FulfillmentArchive(fulfill.Uid, fulfill.LastRemark, fulfill.LastFulfill, fulfill.UpdateBy, fulfill.UpdateAt);
        }
    }

}