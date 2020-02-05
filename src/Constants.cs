namespace WebValidationTest
{
    /// <summary>
    /// Constants used in the app
    /// </summary>
    public sealed class Constants
    {
        // error messages
        public const string DurationParameterError = "Invalid duration (seconds) parameter: {0}\n";
        public const string FileNotFoundError = "File not found: {0}";
        public const string SleepParameterError = "Invalid sleep (milliseconds) parameter: {0}\n";
        public const string MaxConcurrentParameterError = "Invalid max concurrent requests parameter: {0}\n";
        public const string RequestTimeoutParameterError = "Invalid request timeout parameter: {0}\n";
        public const string HostMissingMessage = "Must specify --host parameter\n";
        public const string NoFilesFoundMessage = "No files found";
        public const string TelemetryParameterError = "Invalid telemetry parameter: {0} {1}\n";
        public const string RunLoopMessage = "Must specify --runloop to use --{0}\n";

        public const string ControlCMessage = "Ctl-C Pressed - Starting shutdown ...";

        public const string DefaultTestFile = "TestFiles/baseline.json";
        public const string TestFilePath = "TestFiles/";

    }

    /// <summary>
    /// Environment Variable Keys
    /// </summary>
    public sealed class EnvKeys
    {
        public const string AppService = "KUDU_APPPATH";

        public const string RunLoop = "RUNLOOP";
        public const string MaxConcurrent = "MAXCONCURRENT";
        public const string Sleep = "SLEEP";
        public const string Verbose = "VERBOSE";
        public const string Files = "FILES";
        public const string Random = "RANDOM";
        public const string Host = "HOST";
        public const string Duration = "DURATION";
        public const string RequestTimeout = "TIMEOUT";
        public const string TelemetryKey = "TELEMETRYKEY";
        public const string TelemetryAppName = "TELEMETRYAPPNAME";
    }

    /// <summary>
    /// Command Line parameter keys
    /// </summary>
    public sealed class ArgKeys
    {
        public const string RunLoop = "--runloop";
        public const string MaxConcurrent = "--maxconcurrent";
        public const string Sleep = "--sleep";
        public const string Verbose = "--verbose";
        public const string Files = "--files";
        public const string Random = "--random";
        public const string Host = "--host";
        public const string Duration = "--duration";
        public const string RequestTimeout = "--timeout";
        public const string Telemetry = "--telemetry";
        public const string Help = "--help";
        public const string HelpShort = "-h";
    }
}
