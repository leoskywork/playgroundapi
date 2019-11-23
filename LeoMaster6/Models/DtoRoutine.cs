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
        public DateTime? LastFulfil { get; set; }
        public DateTime[] HistoryFulfilments { get; set; } 

        //optional fields
        public string CreateBy { get; set; }
        public DateTime? CreateAt { get; set; } = DateTime.Now;


        public override string ToString()
        {
            return $"{Name}_{LastFulfil?.ToString()}_{Uid}_{CreateBy}";
        }

        public static DtoRoutine From(RoutineFulfillment fulfill)
        {
            return new DtoRoutine()
            {
                Uid = fulfill.Uid,
                Name = fulfill.Name,
                LastFulfil = fulfill.LastFulfil,
                HistoryFulfilments = fulfill.HistoryFulfilments,
                CreateAt = fulfill.CreateAt,
                CreateBy = fulfill.CreateBy
            };
        }
    }


    public class RoutineFulfillment : ILskjsonLine
    {
        public Guid Uid { get; set; }
        public string Name { get; set; }
        public DateTime? LastFulfil { get; set; }
        public DateTime[] HistoryFulfilments { get; set; }

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
            return $"{Name}_{LastFulfil?.ToString()}_{Uid}_{CreateBy}";
        }
    }


}