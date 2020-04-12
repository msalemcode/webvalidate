using System.Collections.Generic;

namespace WebValidation.Model
{
    public class Request
    {
        public string Verb { get; set; } = "GET";
        public string Path { get; set; }
        public string Body { get; set; }
        public Dictionary<string, string> Headers { get; } = new Dictionary<string, string>();
        public PerfTarget PerfTarget { get; set; }
        public Validation Validation { get; set; }
    }
}
