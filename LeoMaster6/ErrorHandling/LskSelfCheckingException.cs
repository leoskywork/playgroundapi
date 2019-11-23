using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LeoMaster6.ErrorHandling
{
    public class LskSelfCheckingException : LskExcepiton
    {
        public string Subsystem { get; set; }

        public LskSelfCheckingException(string message, string subsystem = "Self checking",  string techInfo = null) : base(message, techInfo)
        {
        }

        public LskSelfCheckingException(string message, Exception innerEx, string subsystem = "Self checking", string techInfo = null) : base(message, innerEx, techInfo)
        {
        }

        public override string ToString()
        {
            return $"Subsystem: ${Subsystem}, ${ base.ToString()}";
        }
    }
}