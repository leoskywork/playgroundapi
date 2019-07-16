using System;

namespace LeoMaster6.ErrorHandling
{
    public class LskExcepiton : Exception
    {
        public string TechnicalInfo { get; set; }

        public LskExcepiton(string message) : base(message)
        {

        }

        public LskExcepiton(string message, Exception innerEx) : base(message, innerEx)
        {

        }

        public override string ToString()
        {
            return (string.IsNullOrEmpty(this.TechnicalInfo) ? "" : "Tech info: " + this.TechnicalInfo + ", ") + base.ToString();
        }
    }
}