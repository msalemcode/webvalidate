namespace WebValidation.Model
{
    public class Request
    {
        public string Verb { get; set; } = "GET";
        public string Path { get; set; }
        public PerfTarget PerfTarget { get; set; }
        public Validation Validation { get; set; }
    }
}
