namespace WebValidationTest
{
    public sealed class Constants
    {
        // error messages
        public const string DurationParameterError = "Invalid duration (seconds) parameter: {0}\n";
        public const string FileNotFoundError = "File not found: {0}";
        public const string SleepParameterError = "Invalid sleep (milliseconds) parameter: {0}\n";
        public const string ThreadsParameterError = "Invalid number of concurrent threads parameter: {0}\n";
        public const string MaxAgeParameterError = "Invalid maximum metrics age parameter: {0}\n";
        public const string HostMissingMessage = "Must specify --host parameter\n";
        public const string NoFilesFoundMessage = "No files found";

        public const string ControlCMessage = "Ctl-C Pressed - Starting shutdown ...";

        public const string DefaultTestFile = "TestFiles/baseline.json";

        public const string TestFilePath = "TestFiles/";

    }

    public sealed class EnvKeys
    {
        public const string AppService = "KUDU_APPPATH";

        public const string RunLoop = "RUNLOOP";
        public const string RunWeb = "RUNWEB";
        public const string Threads = "THREADS";
        public const string Sleep = "SLEEP";
        public const string Verbose = "VERBOSE";
        public const string Files = "FILES";
        public const string Random = "RANDOM";
        public const string Host = "HOST";
        public const string MaxMetricsAge = "MAXMETRICSAGE";
    }

    public sealed class ArgKeys
    {
        public const string RunLoop = "--runloop";
        public const string RunWeb = "--runweb";
        public const string Threads = "--threads";
        public const string Sleep = "--sleep";
        public const string Verbose = "--verbose";
        public const string Files = "--files";
        public const string Random = "--random";
        public const string Host = "--host";
        public const string MaxMetricsAge = "--maxage";
        public const string Duration = "--duration";
        public const string Help = "--help";
        public const string HelpShort = "-h";
    }
}
