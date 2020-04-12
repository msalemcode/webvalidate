using System;

namespace WebValidation.Model
{
    public class PerfLog
    {
        public DateTime Date { get; set; } = DateTime.UtcNow;
        public string Category { get; set; }
        public int PerfLevel { get; set; }
        public bool Validated { get; set; } = true;
        public string ValidationResults { get; set; } = string.Empty;
        public double Duration { get; set; }
        public int StatusCode { get; set; }
        public long ContentLength { get; set; }
    }
}
