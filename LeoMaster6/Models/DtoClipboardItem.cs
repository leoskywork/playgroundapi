using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LeoMaster6.Models
{
    public class DtoClipboardItem
    {
        //no easy way to track this without file/db
        //public int Id { get; set; }

        public Guid Uid { get; set; } = Guid.Empty;
        //public string SessionId { get; set; }
        public string UserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Data { get; set; }
    }
}