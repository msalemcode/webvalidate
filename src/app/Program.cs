using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CSE.WebValidate
{
    public sealed class App
    {
        // public properties
        public static CancellationTokenSource TokenSource { get; set; } = new CancellationTokenSource();

        /// <summary>
        /// Main entry point
        /// </summary>
        /// <param name="args">Command Line Parameters</param>
        public static async Task<int> Main(string[] args)
        {
            // add ctl-c handler
            AddControlCHandler();

            // build the System.CommandLine.RootCommand
            RootCommand root = BuildRootCommand();
            root.Handler = CommandHandler.Create((Config cfg) => App.Run(cfg));

            if (args == null) args = Array.Empty<string>();

            return await root.InvokeAsync(args).ConfigureAwait(false);
        }

        /// <summary>
        /// Build the RootCommand for parsing
        /// </summary>
        /// <returns>RootCommand</returns>
        public static RootCommand BuildRootCommand()
        {
            RootCommand root = new RootCommand
            {
                Name = "WebValidate",
                Description = "Validate API responses",
                TreatUnmatchedTokensAsErrors = true
            };

            root.AddOption(new Option<string>(new string[] { "-s", "--server" }, ParseString, true, "Server to test"));
            root.AddOption(new Option<List<string>>(new string[] { "-f", "--files" }, ParseStringList, true, "List of files to test"));
            root.AddOption(new Option<int>(new string[] { "-l", "--sleep" }, ParseInt, true, "Sleep (ms) between each request"));
            root.AddOption(new Option<string>(new string[] { "-u", "--base-url" }, ParseString, true, "Base url for files"));
            root.AddOption(new Option<bool>(new string[] { "-v", "--verbose" }, ParseBool, true, "Display verbose results"));
            root.AddOption(new Option<bool>(new string[] { "-r", "--run-loop" }, ParseBool, true, "Run test in an infinite loop"));
            root.AddOption(new Option<bool>(new string[] { "--random" }, ParseBool, true, "Run requests randomly (requires --run-loop)"));
            root.AddOption(new Option<int>(new string[] { "--duration" }, ParseInt, true, "Test duration (seconds)  (requires --run-loop)"));
            root.AddOption(new Option<int>(new string[] { "-t", "--timeout" }, ParseInt, true, "Request timeout (seconds)"));
            root.AddOption(new Option<int>(new string[] { "--max-concurrent" }, ParseInt, true, "Max concurrent requests"));
            root.AddOption(new Option<int>(new string[] { "--max-errors" }, ParseInt, true, "Max validation errors"));
            root.AddOption(new Option<string>(new string[] { "--telemetry-name" }, ParseString, true, "App Insights application name"));
            root.AddOption(new Option<string>(new string[] { "--telemetry-key" }, ParseString, true, "App Insights key"));
            root.AddOption(new Option<bool>(new string[] { "-d", "--dry-run" }, "Validates configuration"));

            // these require access to --run-loop so are added at the root level
            root.AddValidator(ValidateRunLoopDependencies);
            root.AddValidator(ValidateTelemetry);

            return root;
        }

        static string ValidateTelemetry(CommandResult result)
        {
            const string nameMessage = "--telemetry-name must be 3 - 24 characters [a-z][0-9]";
            const string keyMessage = "--telemetry-key must be 36 characters 8[hex]-4[hex]-4[hex]-12[hex]";
            const string bothMessage = "--telemetry-name and --telemetry-key must both be specified or omitted";

            OptionResult name = result.Children.FirstOrDefault(c => c.Symbol.Name == "telemetry-name") as OptionResult;
            OptionResult key = result.Children.FirstOrDefault(c => c.Symbol.Name == "telemetry-key") as OptionResult;

            // neither name or key is set
            if (name == null && key == null)
            {
                return string.Empty;
            }

            // both name and key must be set
            if (name == null || key == null)
            {
                return bothMessage;
            }

            // get the values
            string n = name.GetValueOrDefault<string>();
            string k = key.GetValueOrDefault<string>();

            // both are null is OK
            if (n == null && k == null)
            {
                return string.Empty;
            }

            // both must be set
            if (n == null || k == null)
            {
                return bothMessage;
            }

            // name must be 3-24 characters in length
            if (n.Length < 3 || n.Length > 24)
            {
                return nameMessage;
            }

            // key must be 36 characters
            if (k.Length != 36)
            {
                return keyMessage;
            }

            return string.Empty;
        }

        // validate --duration and --random based on --run-loop
        static string ValidateRunLoopDependencies(CommandResult result)
        {
            OptionResult runLoopRes = result.Children.FirstOrDefault(c => c.Symbol.Name == "run-loop") as OptionResult;
            OptionResult durationRes = result.Children.FirstOrDefault(c => c.Symbol.Name == "duration") as OptionResult;
            OptionResult randomRes = result.Children.FirstOrDefault(c => c.Symbol.Name == "random") as OptionResult;

            bool runLoop = runLoopRes.GetValueOrDefault<bool>();
            int? duration = durationRes.GetValueOrDefault<int?>();
            bool random = randomRes.GetValueOrDefault<bool>();

            if (duration != null && (int)duration > 0 && !runLoop)
            {
                return "--run-loop must be true to use --duration";
            }

            if (random && !runLoop)
            {
                return "--run-loop must be true to use --random";
            }

            return string.Empty;
        }

        static string ParseString(ArgumentResult result)
        {
            string name = result.Parent?.Symbol.Name.ToUpperInvariant().Replace('-', '_');
            if (string.IsNullOrEmpty(name))
            {
                result.ErrorMessage = "result.Parent is null";
                return null;
            }

            string val;

            if (result.Tokens.Count == 0)
            {
                string env = Environment.GetEnvironmentVariable(name);

                if (string.IsNullOrWhiteSpace(env))
                {
                    if (name == "SERVER")
                    {
                        result.ErrorMessage = $"--{result.Parent.Symbol.Name} is required";
                    }

                    return null;
                }
                else
                {
                    val = env.Trim();
                }
            }
            else
            {
                val = result.Tokens[0].Value.Trim();
            }

            if (string.IsNullOrWhiteSpace(val))
            {
                if (name == "SERVER")
                {
                    result.ErrorMessage = $"--{result.Parent.Symbol.Name} is required";
                }

                return null;
            }
            else if (val.Length < 3)
            {
                result.ErrorMessage = $"--{result.Parent.Symbol.Name} must be at least 3 characters";
                return null;
            }
            else if (val.Length > 100)
            {
                result.ErrorMessage = $"--{result.Parent.Symbol.Name} must be 100 characters or less";
            }

            return val;
        }

        static List<string> ParseStringList(ArgumentResult result)
        {
            string name = result.Parent?.Symbol.Name.ToUpperInvariant().Replace('-', '_');
            if (string.IsNullOrEmpty(name))
            {
                result.ErrorMessage = "result.Parent is null";
                return null;
            }

            List<string> val = new List<string>();

            if (result.Tokens.Count == 0)
            {
                string env = Environment.GetEnvironmentVariable(name);

                if (string.IsNullOrWhiteSpace(env))
                {
                    result.ErrorMessage = "--files is a required parameter";
                    return null;
                }

                string[] files = env.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                foreach (string f in files)
                {
                    val.Add(f.Trim());
                }
            }
            else
            {
                for (int i = 0; i < result.Tokens.Count; i++)
                {
                    val.Add(result.Tokens[i].Value.Trim());
                }
            }

            return val;
        }

        static bool ParseBool(ArgumentResult result)
        {
            string name = result.Parent?.Symbol.Name.ToUpperInvariant().Replace('-', '_');
            if (string.IsNullOrEmpty(name))
            {
                result.ErrorMessage = "result.Parent is null";
                return false;
            }

            string errorMessage = $"--{result.Parent.Symbol.Name} must true or false";
            bool val;

            // bool options default to true if value not specified (ie -r and -r true)

            if (result.Parent.Parent.Children.FirstOrDefault(c => c.Symbol.Name == result.Parent.Symbol.Name) is OptionResult res &&
                !res.IsImplicit &&
                result.Tokens.Count == 0)
            {
                return true;
            }

            // nothing to validate
            if (result.Tokens.Count == 0)
            {
                string env = Environment.GetEnvironmentVariable(name);

                if (!string.IsNullOrWhiteSpace(env))
                {
                    if (bool.TryParse(env, out val))
                    {
                        return val;
                    }
                    else
                    {
                        result.ErrorMessage = errorMessage;
                        return false;
                    }
                }

                if (result.Parent.Symbol.Name == "verbose" &&
                    result.Parent.Parent.Children.FirstOrDefault(c => c.Symbol.Name == "run-loop") is OptionResult resRunLoop &&
                    !resRunLoop.GetValueOrDefault<bool>())
                {
                    return true;
                }

                return false;
            }

            if (!bool.TryParse(result.Tokens[0].Value, out val))
            {
                result.ErrorMessage = errorMessage;
                return false;
            }

            return val;
        }

        /// <summary>
        /// CommandLine validator for integer >= 0
        /// </summary>
        /// <param name="result">OptionResult</param>
        /// <returns>error message</returns>
        static int ParseInt(ArgumentResult result)
        {
            string name = result.Parent?.Symbol.Name.ToUpperInvariant().Replace('-', '_');
            if (string.IsNullOrEmpty(name))
            {
                result.ErrorMessage = "result.Parent is null";
                return -1;
            }

            string errorMessage = name + " must be an integer >= 0";
            int val;

            // nothing to validate
            if (result.Tokens.Count == 0)
            {
                string env = Environment.GetEnvironmentVariable(name);

                if (string.IsNullOrWhiteSpace(env))
                {
                    return GetCommandDefaultValues(result);
                }
                else
                {
                    if (!int.TryParse(env, out val) || val < 0)
                    {
                        result.ErrorMessage = errorMessage;
                        return -1;
                    }

                    return val;
                }
            }

            if (!int.TryParse(result.Tokens[0].Value, out val) || val < 0)
            {
                result.ErrorMessage = errorMessage;
                return -1;
            }

            return val;
        }

        static int GetCommandDefaultValues(ArgumentResult result)
        {
            switch (result.Parent.Symbol.Name)
            {
                case "max-errors":
                    return 10;
                case "max-concurrent":
                    return 100;
                case "sleep":
                    // check run-loop
                    if (result.Parent.Parent.Children.FirstOrDefault(c => c.Symbol.Name == "run-loop") is OptionResult res && res.GetValueOrDefault<bool>())
                    {
                        return 1000;
                    }

                    return 0;
                case "timeout":
                    return 30;
                default:
                    return 0;
            }
        }

        static int DoDryRun(Config config)
        {
            // display the config
            Console.WriteLine("dry run");
            Console.WriteLine($"   Server          {config.Server}");
            Console.WriteLine($"   Files (count)   {config.Files.Count}");
            Console.WriteLine($"   Run Loop        {config.RunLoop}");
            Console.WriteLine($"   Sleep           {config.Sleep}");
            Console.WriteLine($"   Duration        {config.Duration}");
            Console.WriteLine($"   Max Concurrent  {config.MaxConcurrent}");
            Console.WriteLine($"   Max Errors      {config.MaxErrors}");
            Console.WriteLine($"   Random          {config.Random}");
            Console.WriteLine($"   Timeout         {config.Timeout}");
            Console.WriteLine($"   Verbose         {config.Verbose}");
            Console.WriteLine($"   Telemetry App   {config.TelemetryName}");
            Console.WriteLine($"   Telemetry Key   {config.TelemetryKey}");

            return 0;
        }

        public static async Task<int> Run(Config config)
        {
            if (config == null)
            {
                Console.WriteLine("CommandOptions is null");
                return -1;
            }

            // set any missing values
            config.SetDefaultValues();

            // don't run the test on a dry run
            if (config.DryRun)
            {
                return DoDryRun(config);
            }

            // create the test
            try
            {
                using WebV webv = new CSE.WebValidate.WebV(config);

                if (config.RunLoop)
                {
                    // run in a loop
                    return webv.RunLoop(config, TokenSource.Token);
                }
                else
                {
                    // run one iteration
                    return await webv.RunOnce(config, TokenSource.Token).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n{ex}\n\nWebV:Exception:{ex.Message}");
                return 1;
            }
        }

        /// <summary>
        /// Add a ctl-c handler
        /// </summary>
        private static void AddControlCHandler()
        {
            Console.CancelKeyPress += delegate (object sender, ConsoleCancelEventArgs e)
            {
                const string ControlCMessage = "Ctl-C Pressed - Starting shutdown ...";

                e.Cancel = true;
                TokenSource.Cancel();

                Console.WriteLine(ControlCMessage);
            };
        }

        /// <summary>
        /// Check to see if the file exists in the current directory
        /// </summary>
        /// <param name="name">file name</param>
        /// <returns>bool</returns>
        public static bool CheckFileExists(string name)
        {
            return !string.IsNullOrWhiteSpace(name) && System.IO.File.Exists(name.Trim());
        }
    }
}
