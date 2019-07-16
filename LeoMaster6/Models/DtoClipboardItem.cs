using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LeoMaster6.Models
{
    public class DtoClipboardItem
    {
        public string SessionId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Data { get; set; }
    }
}