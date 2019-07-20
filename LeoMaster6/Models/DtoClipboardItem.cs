using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LeoMaster6.Models
{
    [Serializable] // deep clone need this
    public class DtoClipboardItem
    {
        //no easy way to track this without file/db
        //public int Id { get; set; }

        public Guid Uid { get; set; } = Guid.Empty;
        public string UserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Data { get; set; }

        public bool? HasUpdated { get; set; }
        public string LastUpdatedBy { get; set; }
        public DateTime? LastUpdatedAt { get; set; }
        public Guid? ParentUid { get; set; }

        public bool? HasDeleted { get; set; }
        public string DeletedBy { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}