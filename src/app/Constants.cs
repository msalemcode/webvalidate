using System.Collections.Generic;

namespace CSE.WebValidate
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
    }

    /// <summary>
    /// Environment Variable Keys
    /// </summary>
    public sealed class EnvKeys
    {
        public const string RunLoop = "RUN_LOOP";
        public const string MaxConcurrent = "MAX_CONCURRENT";
        public const string MaxErrors = "MAX_ERRORS";
        public const string Sleep = "SLEEP";
        public const string Verbose = "VERBOSE";
        public const string Files = "FILES";
        public const string Random = "RANDOM";
        public const string Server = "SERVER";
        public const string Duration = "DURATION";
        public const string RequestTimeout = "TIMEOUT";
        public const string TelemetryKey = "TELEMETRY_KEY";
        public const string TelemetryName = "TELEMETRY_NAME";

        public static Dictionary<string, string> EnvVarToCommandLineDictionary()
        {
            return new Dictionary<string, string>
            {
                { Server, "--server -s" },
                { Sleep, "--sleep -l" },
                { Verbose, "--verbose -v" },
                { RunLoop, "--run-loop -r" },
                { Random, "--random" },
                { Duration, "--duration" },
                { RequestTimeout, "--timeout -t" },
                { MaxConcurrent, "--max-concurrent" },
                { MaxErrors, "--max-errors" },
                { TelemetryKey, "--telemetry-key" },
                { TelemetryName, "--telemetry-name" }
            };
        }
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
