using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LeoMaster6.Models
{
    public class DtoLskjsonIndex
    {
        public Guid Uid { get; set; }
        public DateTime OriginCreatedAt { get; set; }
        public DateTime CreatedAt { get; set; }

        public DtoLskjsonIndex()
        {

        }

        public DtoLskjsonIndex(Guid uid, DateTime originCreatedAt, DateTime? createdAt = null)
        {
            this.Uid = uid;
            this.OriginCreatedAt = originCreatedAt;
            this.CreatedAt = createdAt ?? DateTime.Now;
        }

        public static DtoLskjsonIndex From(DtoClipboardItem item)
        {
            return new DtoLskjsonIndex(item.Uid, item.CreatedAt);
        }

        public override string ToString()
        {
            return $"{this.Uid} {this.OriginCreatedAt} {this.CreatedAt}";
        }
    }
}