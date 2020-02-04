using System.Collections.Generic;

namespace WebValidationTest
{
    /// <summary>
    /// Web Validation Test Configuration
    /// </summary>
    public class Config
    {
        public string Host { get; set; } = string.Empty;
        public bool RunLoop { get; set; } = false;
        public int MaxConcurrentRequests { get; set; } = 100;
        public int SleepMs { get; set; } = -1;
        public int Duration { get; set; } = 0;
        public bool Random { get; set; } = false;
        public bool? Verbose { get; set; } = null;
        public int RequestTimeout { get; set; } = 30;
        public List<string> FileList { get; } = new List<string>();
    }
}
