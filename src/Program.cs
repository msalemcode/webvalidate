using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace WebValidationTest
{
    public sealed class App
    {
        // public properties
        public static WebValidation.Test WebV { get; set; } = null;
        public static Config Config { get; } = new Config();
        public static DateTime StartTime { get; } = DateTime.UtcNow;
        public static Metrics Metrics { get; } = new Metrics();
        public static List<Task> Tasks { get; } = new List<Task>();
        public static CancellationTokenSource TokenSource { get; set; } = new CancellationTokenSource();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "null is valid")]
        public static void Main(string[] args)
        {
            ProcessEnvironmentVariables();

            ProcessCommandArgs(args);

            ValidateParameters();

            // create the test
            WebV = new WebValidation.Test(Config.FileList, Config.Host);

            // run one test iteration
            if (!Config.RunLoop)
            {
                if (!WebV.RunOnce().Result)
                {
                    Environment.Exit(-1);
                }

                return;
            }

            // setup ctl c handler
            Console.CancelKeyPress += delegate (object sender, ConsoleCancelEventArgs e)
            {
                e.Cancel = true;
                TokenSource.Cancel();

                Console.WriteLine(Constants.ControlCMessage);

                // give threads a chance to shutdown
                Task.Delay(500);

                // end the app
                Environment.Exit(0);
            };

            if (Config.RunWeb)
            {
                RunWeb(args, TokenSource.Token);
            }
            else if (Config.RunLoop)
            {
                RunLoop(TokenSource.Token);
            }
        }

        /// <summary>
        /// Run as a web server
        /// </summary>
        /// <param name="token">CancellationTokenSource</param>
        private static void RunWeb(string[] args, CancellationToken token)
        {
            IWebHost host;

            // use the default web host builder + startup
            IWebHostBuilder builder = WebHost.CreateDefaultBuilder(args)
                .UseKestrel()
                .UseStartup<Startup>()
                .UseUrls("http://*:4122/");

            // build the host
            host = builder.Build();

            // start the test threads
            for (int i = 0; i < Config.Threads; i++)
            {
                Tasks.Add(WebV.RunLoop(i, Config, token));
            }

            // run the web server
            try
            {
                Console.WriteLine($"Version: {WebValidationTest.Version.AssemblyVersion}\nThreads: {Config.Threads}\nSleep: {Config.SleepMs}\nRandomize: {Config.Random}");
                host.Run();
                Console.WriteLine("Web server shutdown");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Web Server Exception\n{ex}");
            }
        }

        /// <summary>
        /// Run tests in a loop
        /// </summary>
        /// <param name="ctCancel">CancellationTokenSource</param>
        private static void RunLoop(CancellationToken token)
        {
            // start the tests on separate threads
            for (int i = 0; i < Config.Threads; i++)
            {
                Console.WriteLine($"Starting task {i}");
                Tasks.Add(WebV.RunLoop(i, Config, token));
            }

            // wait for all tasks to complete or ctl-c
            Task.WaitAll(Tasks.ToArray());
        }

        /// <summary>
        /// Validate env vars and command line have valid values
        /// </summary>
        private static void ValidateParameters()
        {
            // host is required
            if (string.IsNullOrWhiteSpace(Config.Host))
            {
                Console.WriteLine(Constants.HostMissingMessage);
                Usage();
                Environment.Exit(-1);
            }

            // invalid parameter
            if (Metrics.MaxAge < 0)
            {
                Console.Write(Constants.MaxAgeParameterError, Metrics.MaxAge);
                Usage();
                Environment.Exit(-1);
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
            ValidateNonRunloopParameters();
            ValidateRunloopParameters();
            ValidateFileList();
        }

        /// <summary>
        /// Validate at least one test file exists
        /// </summary>
        private static void ValidateFileList()
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

                string file = TestFileExists(f.Trim('\'', '\"', ' '));

                if (System.IO.File.Exists(file))
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
                Environment.Exit(-1);
            }
        }

        /// <summary>
        /// Validate the parameters if --runloop not specified
        /// </summary>
        private static void ValidateNonRunloopParameters()
        {
            if (!Config.RunLoop)
            {
                // these params require --runloop
                if (Config.RunWeb)
                {
                    Console.WriteLine("Must specify --runloop to use --runweb\n");
                    Usage();
                    Environment.Exit(-1);
                }

                if (Config.Threads != -1)
                {
                    Console.WriteLine("Must specify --runloop to use --threads\n");
                    Usage();
                    Environment.Exit(-1);
                }

                if (Config.SleepMs != -1)
                {
                    Console.WriteLine("Must specify --runloop to use --sleep\n");
                    Usage();
                    Environment.Exit(-1);
                }

                if (Config.Duration > 0)
                {
                    Console.WriteLine("Must specify --runloop to use --duration\n");
                    Usage();
                    Environment.Exit(-1);
                }

                if (Config.Random)
                {
                    Console.WriteLine("Must specify --runloop to use --random\n");
                    Usage();
                    Environment.Exit(-1);
                }
            }

            // invalid combo
            if (Config.RunWeb && Config.Duration > 0)
            {
                Console.WriteLine("Cannot use --duration with --runweb\n");
                Usage();
                Environment.Exit(-1);
            }

            // limit metrics to 12 hours as it's stored in memory
            if (Metrics.MaxAge > 12 * 60 * 60)
            {
                Metrics.MaxAge = 12 * 60 * 60;
            }

            // can't be less than 0
            if (Config.Duration < 0)
            {
                Console.WriteLine(Constants.DurationParameterError, Config.Duration);
                Usage();
                Environment.Exit(-1);
            }
        }

        /// <summary>
        /// Validate parameters if --runloop specified
        /// </summary>
        private static void ValidateRunloopParameters()
        {
            if (Config.RunLoop)
            {
                // -1 means was not specified
                if (Config.SleepMs == -1)
                {
                    Config.SleepMs = 1000;
                }

                if (Config.Threads == -1)
                {
                    Config.Threads = 1;
                }

                // let's not get too crazy
                if (Config.Threads > 10)
                {
                    Config.Threads = 10;
                }

                // must be > 0
                if (Config.Threads <= 0)
                {
                    Console.WriteLine(Constants.ThreadsParameterError, Config.Threads);
                    Usage();
                    Environment.Exit(-1);
                }

                // must be >= 0
                if (Config.SleepMs < 0)
                {
                    Console.WriteLine(Constants.SleepParameterError, Config.SleepMs);
                    Usage();
                    Environment.Exit(-1);
                }
            }
        }

        /// <summary>
        /// Validate the command line params
        /// </summary>
        /// <param name="args">string[]</param>
        private static void ProcessCommandArgs(string[] args)
        {
            // show usage
            if (args == null || args.Length == 0 || args[0] == ArgKeys.Help || args[0] == ArgKeys.HelpShort)
            {
                Usage();
                Environment.Exit(0);
            }

            int i = 0;

            while (i < args.Length)
            {
                if (!args[i].StartsWith("--", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"\nInvalid argument: {args[i]}\n");
                    Usage();
                    Environment.Exit(-1);
                }

                // handle run loop (--runloop)
                if (args[i] == ArgKeys.RunLoop)
                {
                    Config.RunLoop = true;
                }

                // handle run web (--runweb)
                else if (args[i] == ArgKeys.RunWeb)
                {
                    Config.RunWeb = true;
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

                    // handle input files (-i inputFile.json input2.json input3.json)
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
                            Usage();
                            Environment.Exit(-1);
                        }
                    }

                    // handle config.Threads (--threads config.Threads)
                    else if (args[i] == ArgKeys.Threads)
                    {
                        if (int.TryParse(args[i + 1], out int v))
                        {
                            Config.Threads = v;
                            i++;
                        }
                        else
                        {
                            // exit on error
                            Console.WriteLine(Constants.ThreadsParameterError, args[i + 1]);
                            Usage();
                            Environment.Exit(-1);
                        }
                    }
                    // handle duration (--maxage Metrics.MaxAge (minutes))
                    else if (args[i] == ArgKeys.MaxMetricsAge)
                    {
                        if (int.TryParse(args[i + 1], out int maxAge))
                        {
                            Metrics.MaxAge = maxAge;
                            i++;
                        }
                        else
                        {
                            // exit on error
                            Console.WriteLine(Constants.MaxAgeParameterError, args[i + 1]);
                            Usage();
                            Environment.Exit(-1);
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
                            Usage();
                            Environment.Exit(-1);
                        }
                    }
                }

                i++;
            }
        }

        /// <summary>
        /// Process environment variables
        /// </summary>
        private static void ProcessEnvironmentVariables()
        {
            // run as web app if running in App Service
            string env = Environment.GetEnvironmentVariable(EnvKeys.AppService);
            if (!string.IsNullOrEmpty(env))
            {
                Config.RunLoop = true;
                Config.RunWeb = true;
            }

            env = Environment.GetEnvironmentVariable(EnvKeys.RunLoop);
            if (!string.IsNullOrEmpty(env))
            {
                if (bool.TryParse(env, out bool b))
                {
                    Config.RunLoop = b;
                }
            }

            env = Environment.GetEnvironmentVariable(EnvKeys.RunWeb);
            if (!string.IsNullOrEmpty(env))
            {
                if (bool.TryParse(env, out bool b))
                {
                    Config.RunWeb = b;
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
                    Environment.Exit(-1);
                }
            }

            env = Environment.GetEnvironmentVariable(EnvKeys.Threads);
            if (!string.IsNullOrEmpty(env))
            {
                if (int.TryParse(env, out int v))
                {
                    Config.Threads = v;
                }
                else
                {
                    // exit on error
                    Console.WriteLine(Constants.ThreadsParameterError, env);
                    Environment.Exit(-1);
                }
            }

            env = Environment.GetEnvironmentVariable(EnvKeys.MaxMetricsAge);
            if (!string.IsNullOrEmpty(env))
            {
                if (int.TryParse(env, out int maxAge))
                {
                    Metrics.MaxAge = maxAge;
                }
                else
                {
                    // exit on error
                    Console.WriteLine(Constants.MaxAgeParameterError, env);
                    Environment.Exit(-1);
                }

            }
        }

        /// <summary>
        /// Test to see if the file exists
        /// </summary>
        /// <param name="name">file name or path</param>
        /// <returns>full path to file or string.empty</returns>
        private static string TestFileExists(string name)
        {
            string file = name.Trim();

            if (!string.IsNullOrEmpty(file))
            {
                // add TestFiles directory if not specified (default location)
                if (!file.StartsWith(Constants.TestFilePath, StringComparison.Ordinal))
                {
                    file = Constants.TestFilePath + file;
                }

                if (System.IO.File.Exists(file))
                {
                    return file;
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Display command line usage
        /// </summary>
        private static void Usage()
        {
            Console.WriteLine($"Version: {WebValidationTest.Version.AssemblyVersion}");
            Console.WriteLine();
            Console.WriteLine("Usage: dotnet run -- [-h] [--help] --host hostUrl [--files file1.json [file2.json] [file3.json] ...]\n[--runloop] [--sleep sleepMs] [--threads numberOfThreads] [--duration durationSeconds] [--random]\n[--runweb] [--verbose] [--maxage maxMinutes]");
            Console.WriteLine("\t--host host name or host Url");
            Console.WriteLine("\t--files file1 [file2 file3 ...] (default *.json)");
            Console.WriteLine("\t--runloop");
            Console.WriteLine("\tLoop Mode Parameters");
            Console.WriteLine("\t\t--sleep number of milliseconds to sleep between requests (default 1000)");
            Console.WriteLine("\t\t--threads number of concurrent threads (default 1) (max 10)");
            Console.WriteLine("\t\t--duration duration in seconds (default forever");
            Console.WriteLine("\t\t--random randomize requests");
            Console.WriteLine("\t\t--runweb run as web server (listens on port 4122)");
            Console.WriteLine("\t\t--verbose turn on verbose logging");
            Console.WriteLine("\t\t--maxage maximum minutes to track metrics (default 240)");
            Console.WriteLine("\t\t\t0 = do not track metrics");
            Console.WriteLine("\t\t\trequires --runweb");
        }
    }
}
