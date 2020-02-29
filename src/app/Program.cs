using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using WebValidation;

namespace WebValidationApp
{
    public sealed class App : IDisposable
    {
        // public properties
        public static CancellationTokenSource TokenSource { get; set; } = new CancellationTokenSource();
        public Config Config { get; } = new Config();

        /// <summary>
        /// Main entry point
        /// </summary>
        /// <param name="args">Command Line Parms</param>
        public static int Main(string[] args)
        {
            // should never be null
            if (args == null)
            {
                args = Array.Empty<string>();
            }

            // display help
            if (CheckCommandLineHelp(args))
            {
                Usage();
                return 0;
            }

            // add ctl-c handler
            AddControlCHandler();

            // run the app
            using App app = new App();
            return app.Run(args);
        }

        public int Run(string[] args)
        {
            // validate parameters
            if (!ProcessEnvironmentVariables() ||
                !ProcessCommandArgs(args) ||
                !ValidateParameters())
            {
                Usage();
                return -1;
            }

            // create the test
            using WebV webv = new WebValidation.WebV(Config);

            // run in a loop
            if (Config.RunLoop)
            {
                webv.RunLoop(Config, TokenSource.Token);
            }
            else
            {
                // run one iteration
                return webv.RunOnce(Config).Result ? 0 : -1;
            }

            return 0;
        }

        /// <summary>
        /// Add a ctl-c handler
        /// </summary>
        private static void AddControlCHandler()
        {
            Console.CancelKeyPress += delegate (object sender, ConsoleCancelEventArgs e)
            {
                e.Cancel = true;
                TokenSource.Cancel();

                Console.WriteLine(Constants.ControlCMessage);

                // give tasks a chance to shutdown
                Task.Delay(500);

                // end the app
                Environment.Exit(0);
            };
        }

        /// <summary>
        /// Validate env vars and command line
        /// </summary>
        public bool ValidateParameters()
        {
            // host is required
            if (string.IsNullOrWhiteSpace(Config.Host))
            {
                Console.WriteLine(Constants.HostMissingMessage);
                return false;
            }

            // make it easier to pass host
            if (!Config.Host.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                if (Config.Host.StartsWith("localhost", StringComparison.OrdinalIgnoreCase))
                {
                    Config.Host = "http://" + Config.Host;
                }
                else
                {
                    Config.Host = string.Format(CultureInfo.InvariantCulture, $"https://{Config.Host}.azurewebsites.net");
                }
            }

            // validate additional parameters
            if (!ValidateRunOnceParameters())
            {
                return false;
            }

            if (!ValidateRunLoopParameters())
            {
                return false;
            }

            if (!ValidateFileList())
            {
                return false;
            }

            if (!ValidateSharedParameters())
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validate at least one test file exists
        /// </summary>
        public bool ValidateFileList()
        {
            // Add the default file if none specified
            if (Config.FileList.Count == 0)
            {
                Config.FileList.Add(Constants.DefaultTestFile);
            }

            string f;

            // check each file in reverse index order
            for (int i = Config.FileList.Count - 1; i >= 0; i--)
            {
                f = Config.FileList[i];

                if (TestFileExists(f.Trim('\'', '\"', ' '), out string file))
                {
                    Config.FileList[i] = file;
                }
                else
                {
                    Console.WriteLine(Constants.FileNotFoundError, f);
                    Config.FileList.RemoveAt(i);
                }
            }

            // exit if no files found
            if (Config.FileList.Count == 0)
            {
                Console.WriteLine(Constants.NoFilesFoundMessage);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validate shared parameters (RunOnce and RunLoop)
        /// </summary>
        public bool ValidateSharedParameters()
        {
            // validate request timeout
            if (Config.RequestTimeout < 1)
            {
                Console.WriteLine(Constants.RequestTimeoutParameterError, Config.RequestTimeout);
                return false;
            }

            // validate telemetry
            if (!string.IsNullOrEmpty(Config.TelemetryKey) || !string.IsNullOrEmpty(Config.TelemetryApp))
            {
                // both or neither have to be specified
                if (string.IsNullOrEmpty(Config.TelemetryKey) || string.IsNullOrEmpty(Config.TelemetryApp))
                {
                    Console.WriteLine(Constants.TelemetryParameterError, Config.TelemetryApp, Config.TelemetryKey);
                    return false;
                }
            }

            return true;
        }


        /// <summary>
        /// Validate RunOnce parameters
        /// </summary>
        public bool ValidateRunOnceParameters()
        {
            if (!Config.RunLoop)
            {
                // default verbose to true
                if (Config.Verbose == null)
                {
                    Config.Verbose = true;
                }

                // these params require --runloop
                if (Config.Duration > 0)
                {
                    Console.WriteLine(Constants.RunLoopMessage, "duration");
                    return false;
                }

                if (Config.Random)
                {
                    Console.WriteLine(Constants.RunLoopMessage, "random");
                    return false;
                }

                // -1 means was not specified
                if (Config.SleepMs == -1)
                {
                    Config.SleepMs = 0;
                }
            }

            // validate request timeout
            if (Config.RequestTimeout < 1)
            {
                Console.WriteLine(Constants.RequestTimeoutParameterError, Config.RequestTimeout);
                return false;
            }

            // validate telemetry
            if (!string.IsNullOrEmpty(Config.TelemetryKey) || !string.IsNullOrEmpty(Config.TelemetryApp))
            {
                // both or neither have to be specified
                if (string.IsNullOrEmpty(Config.TelemetryKey) || string.IsNullOrEmpty(Config.TelemetryApp))
                {
                    Console.WriteLine(Constants.TelemetryParameterError, Config.TelemetryApp, Config.TelemetryKey);
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Validate parameters if --runloop specified
        /// </summary>
        public bool ValidateRunLoopParameters()
        {
            if (Config.RunLoop)
            {
                // default verbose to false
                if (Config.Verbose == null)
                {
                    Config.Verbose = false;
                }

                // -1 means was not specified
                if (Config.SleepMs == -1)
                {
                    Config.SleepMs = 1000;
                }

                // must be > 0
                if (Config.MaxConcurrentRequests < 1)
                {
                    Console.WriteLine(Constants.MaxConcurrentParameterError, Config.MaxConcurrentRequests);
                    return false;
                }

                // must be >= 0
                if (Config.SleepMs < 0)
                {
                    Console.WriteLine(Constants.SleepParameterError, Config.SleepMs);
                    return false;
                }

                // can't be less than 0
                if (Config.Duration < 0)
                {
                    Console.WriteLine(Constants.DurationParameterError, Config.Duration);
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Check command line for --help
        /// </summary>
        /// <param name="args">string[]</param>
        public static bool CheckCommandLineHelp(string[] args)
        {
            // show usage
            if (args == null || args.Length == 0 || args[0] == ArgKeys.Help || args[0] == ArgKeys.HelpShort)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Process command line params
        /// </summary>
        /// <param name="args">string[]</param>
        public bool ProcessCommandArgs(string[] args)
        {
            if (args == null)
            {
                return false;
            }

            const string invalidArgsMessage = "\nInvalid argument: {0}\n";
            int i = 0;

            // process the args
            while (i < args.Length)
            {
                // all args start with --
                if (!args[i].StartsWith("--", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine(invalidArgsMessage, args[i]);
                    return false;
                }

                // handle run loop (--runloop)
                if (args[i] == ArgKeys.RunLoop)
                {
                    Config.RunLoop = true;
                }

                // handle --random
                else if (args[i] == ArgKeys.Random)
                {
                    Config.Random = true;
                }

                // handle --verbose
                else if (args[i] == ArgKeys.Verbose)
                {
                    Config.Verbose = true;
                }

                // process all other args in pairs
                else if (i < args.Length - 1)
                {
                    // handle host
                    if (args[i] == ArgKeys.Host)
                    {
                        Config.Host = args[i + 1].Trim();
                        i++;
                    }

                    // handle input files (--files inputFile.json input2.json input3.json)
                    else if (i < args.Length - 1 && (args[i] == ArgKeys.Files))
                    {
                        // command line overrides env var
                        Config.FileList.Clear();

                        // process all file names
                        while (i + 1 < args.Length && !args[i + 1].StartsWith("-", StringComparison.OrdinalIgnoreCase))
                        {
                            if (!string.IsNullOrEmpty(args[i + 1]))
                            {
                                Config.FileList.Add(args[i + 1].Trim());
                            }

                            i++;
                        }
                    }

                    // handle telemetry app name and key (--telemetry appName key)
                    // checking for < args.Length - 2 will miss the usage error
                    //    where the command line ends with -- telemetry onlyOneArg
                    else if (i < args.Length - 1 && (args[i] == ArgKeys.Telemetry))
                    {
                        // get app name
                        if (i + 1 < args.Length && !args[i + 1].StartsWith("-", StringComparison.OrdinalIgnoreCase))
                        {
                            Config.TelemetryApp = args[i + 1];
                            i++;
                        }

                        // get key
                        if (i + 1 < args.Length && !args[i + 1].StartsWith("-", StringComparison.OrdinalIgnoreCase))
                        {
                            Config.TelemetryKey = args[i + 1];
                            i++;
                        }
                    }

                    // handle sleep (--sleep config.SleepMs)
                    else if (args[i] == ArgKeys.Sleep)
                    {
                        if (int.TryParse(args[i + 1], out int v))
                        {
                            Config.SleepMs = v;
                            i++;
                        }

                        else
                        {
                            // exit on error
                            Console.WriteLine(Constants.SleepParameterError, args[i + 1]);
                            return false;
                        }
                    }

                    // handle config.MaxConcurrentRequests (--maxconncurrent)
                    else if (args[i] == ArgKeys.MaxConcurrent)
                    {
                        if (int.TryParse(args[i + 1], out int v))
                        {
                            Config.MaxConcurrentRequests = v;
                            i++;
                        }
                        else
                        {
                            // exit on error
                            Console.WriteLine(Constants.MaxConcurrentParameterError, args[i + 1]);
                            return false;
                        }
                    }

                    // handle config.RequestTimeout (--timeout)
                    else if (args[i] == ArgKeys.RequestTimeout)
                    {
                        if (int.TryParse(args[i + 1], out int v))
                        {
                            Config.RequestTimeout = v;
                            i++;
                        }
                        else
                        {
                            // exit on error
                            Console.WriteLine(Constants.RequestTimeoutParameterError, args[i + 1]);
                            return false;
                        }
                    }

                    // handle duration (--duration config.Duration (seconds))
                    else if (args[i] == ArgKeys.Duration)
                    {
                        if (int.TryParse(args[i + 1], out int v))
                        {
                            Config.Duration = v;
                            i++;
                        }
                        else
                        {
                            // exit on error
                            Console.WriteLine(Constants.DurationParameterError, args[i + 1]);
                            return false;
                        }
                    }
                }

                i++;
            }

            return true;
        }

        /// <summary>
        /// Process environment variables
        /// </summary>
        public bool ProcessEnvironmentVariables()
        {
            string env = Environment.GetEnvironmentVariable(EnvKeys.RunLoop);
            if (!string.IsNullOrEmpty(env))
            {
                if (bool.TryParse(env, out bool b))
                {
                    Config.RunLoop = b;
                }
            }

            env = Environment.GetEnvironmentVariable(EnvKeys.Random);
            if (!string.IsNullOrEmpty(env))
            {
                if (bool.TryParse(env, out bool b))
                {
                    Config.Random = b;
                }
            }

            env = Environment.GetEnvironmentVariable(EnvKeys.Verbose);
            if (!string.IsNullOrEmpty(env))
            {
                if (bool.TryParse(env, out bool b))
                {
                    Config.Verbose = b;
                }
            }

            env = Environment.GetEnvironmentVariable(EnvKeys.Host);
            if (!string.IsNullOrEmpty(env))
            {
                Config.Host = env;
            }

            env = Environment.GetEnvironmentVariable(EnvKeys.Files);
            if (!string.IsNullOrEmpty(env))
            {
                Config.FileList.AddRange(env.Split(' ', StringSplitOptions.RemoveEmptyEntries));
            }

            env = Environment.GetEnvironmentVariable(EnvKeys.Sleep);
            if (!string.IsNullOrEmpty(env))
            {
                if (int.TryParse(env, out int v))
                {
                    Config.SleepMs = v;
                }
                else
                {
                    // exit on error
                    Console.WriteLine(Constants.SleepParameterError, env);
                    return false;
                }
            }

            env = Environment.GetEnvironmentVariable(EnvKeys.MaxConcurrent);
            if (!string.IsNullOrEmpty(env))
            {
                if (int.TryParse(env, out int v))
                {
                    Config.MaxConcurrentRequests = v;
                }
                else
                {
                    // exit on error
                    Console.WriteLine(Constants.MaxConcurrentParameterError, env);
                    return false;
                }
            }

            env = Environment.GetEnvironmentVariable(EnvKeys.RequestTimeout);
            if (!string.IsNullOrEmpty(env))
            {
                if (int.TryParse(env, out int v))
                {
                    Config.RequestTimeout = v;
                }
                else
                {
                    // exit on error
                    Console.WriteLine(Constants.RequestTimeoutParameterError, env);
                    return false;
                }
            }

            env = Environment.GetEnvironmentVariable(EnvKeys.Duration);
            if (!string.IsNullOrEmpty(env))
            {
                if (int.TryParse(env, out int v))
                {
                    Config.Duration = v;
                }
                else
                {
                    // exit on error
                    Console.WriteLine(Constants.DurationParameterError, env);
                    return false;
                }
            }

            env = Environment.GetEnvironmentVariable(EnvKeys.TelemetryAppName);
            if (!string.IsNullOrEmpty(env))
            {
                Config.TelemetryApp = env;
            }

            env = Environment.GetEnvironmentVariable(EnvKeys.TelemetryKey);
            if (!string.IsNullOrEmpty(env))
            {
                Config.TelemetryKey = env;
            }

            return true;
        }

        /// <summary>
        /// Test to see if the file exists
        /// </summary>
        /// <param name="name">file name or path</param>
        /// <returns>full path to file or string.empty</returns>
        public static bool TestFileExists(string name, out string file)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                file = string.Empty;
                return false;
            }

            file = name.Trim();

            // add TestFiles directory if not specified (default location)
            if (!file.StartsWith(Constants.TestFilePath, StringComparison.OrdinalIgnoreCase))
            {
                file = Constants.TestFilePath + file;
            }

            if (System.IO.File.Exists(file))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Display command line usage
        /// </summary>
        public static void Usage()
        {
            Console.WriteLine($"Version: {WebValidationApp.Version.AssemblyVersion}");
            Console.WriteLine();
            Console.WriteLine("Usage: dotnet run -- ...");
            Console.WriteLine("\t[--help] [-h] help (must be first parameter)");
            Console.WriteLine("\t--host host base Url (i.e. https://www.microsoft.com) (required)");
            Console.WriteLine("\t[--files file1 [file2 file3 ...]] one or more test json files (default baseline.json)");
            Console.WriteLine("\t[--timeout] HTTP request timeout in seconds (default 30 sec)");
            Console.WriteLine("\t[--sleep] number of milliseconds to sleep between requests (default 0)");
            Console.WriteLine("\t[--duration] duration in seconds (default forever)");
            Console.WriteLine("\t[--verbose] turn on verbose logging (default true)");
            Console.WriteLine("\t[--telemetry appName key] App Insights information (default null)");
            Console.WriteLine("\t[--runloop] runs the test in a continuous loop");
            Console.WriteLine("\tLoop Mode Parameters");
            Console.WriteLine("\t\t[--sleep] number of milliseconds to sleep between requests (default 1000)");
            Console.WriteLine("\t\t[--maxconcurrent] max concurrent requests (default 100)");
            Console.WriteLine("\t\t[--random] randomize requests (default false)");
            Console.WriteLine("\t\t[--verbose] turn on verbose logging (default false)");
        }

        // IDispose implementation
        private bool disposedValue = false;

        /// <summary>
        /// Private Dispose
        /// </summary>
        /// <param name="disposing">bool</param>
        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (Config != null)
                    {
                        Config.Dispose();
                    }
                }

                disposedValue = true;
            }
        }

        /// <summary>
        /// IDispose::Dispose
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
