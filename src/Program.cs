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

        /// <summary>
        /// Main entry point
        /// </summary>
        /// <param name="args">Command Line Parms</param>
        public static void Main(string[] args)
        {
            // should never be null
            if (args == null)
            {
                args = Array.Empty<string>();
            }

            ProcessEnvironmentVariables();

            ProcessCommandArgs(args);

            ValidateParameters();

            // create the test
            WebV = new WebValidation.Test(Config.Host, Config.FileList);

            // run one test iteration
            if (!Config.RunLoop)
            {
                if (!WebV.RunOnce(Config).Result)
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

            // run in a loop
            RunLoop(TokenSource.Token);
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
                Console.WriteLine($"{DateTime.Now.ToString("MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}\tStarting Task\t{i}");
                Tasks.Add(WebV.RunLoop(Config, token));
            }

            try
            {
                // wait for all tasks to complete or ctl-c
                Task.WaitAll(Tasks.ToArray());
            }
            catch
            {
                // this will throw an exception if all the tasks are cancelled, so just ignore it
            }
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
                if (Config.Threads != -1)
                {
                    Console.WriteLine("Must specify --runloop to use --threads\n");
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
            Console.WriteLine("Usage: dotnet run -- [-h] [--help] --host hostUrl [--files file1.json [file2.json] [file3.json] ...]\n[--runloop] [--sleep sleepMs] [--threads numberOfThreads] [--duration durationSeconds] [--random][--verbose]");
            Console.WriteLine("\t--host host name or host Url");
            Console.WriteLine("\t--files file1 [file2 file3 ...] (default baseline.json)");
            Console.WriteLine("\t--runloop");
            Console.WriteLine("\tLoop Mode Parameters");
            Console.WriteLine("\t\t--sleep number of milliseconds to sleep between requests (default 1000)");
            Console.WriteLine("\t\t--duration duration in seconds (default forever");
            Console.WriteLine("\t\t--random randomize requests");
            Console.WriteLine("\t\t--verbose turn on verbose logging");
        }
    }
}
