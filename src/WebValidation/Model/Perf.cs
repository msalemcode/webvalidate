using System;
using System.Collections.Generic;

namespace WebValidation
{
    public class PerfLog
    {
        public DateTime Date { get; set; } = DateTime.UtcNow;
        public string Category { get; set; }
        public int PerfLevel { get; set; }
        public bool Validated { get; set; } = true;
        public string Body { get; set; } = string.Empty;
        public string ValidationResults { get; set; } = string.Empty;
        public double Duration { get; set; }
        public int StatusCode { get; set; }
        public long ContentLength { get; set; }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "can't be read-only - json serialization")]
    public class PerfTarget
    {
        public string Category { get; set; }
        public List<double> Targets { get; set; }
    }
}
