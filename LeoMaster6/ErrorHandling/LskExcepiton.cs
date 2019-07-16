using System;

namespace LeoMaster6.ErrorHandling
{
    public class LskExcepiton : Exception
    {
        public string TechnicalInfo { get; set; }

        public LskExcepiton(string message, string techInfo = null) : base(message)
        {
            this.TechnicalInfo = techInfo;
        }

        public LskExcepiton(string message, Exception innerEx, string techInfo = null) : base(message, innerEx)
        {
            this.TechnicalInfo = techInfo;
        }

        public override string ToString()
        {
            return (string.IsNullOrEmpty(this.TechnicalInfo) ? "" : "Tech info: " + this.TechnicalInfo + ", ") + base.ToString();
        }
    }
}