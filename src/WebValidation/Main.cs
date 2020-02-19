using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using WebValidationTest;

namespace WebValidation
{
    /// <summary>
    /// Web Validation Test
    /// </summary>
    public partial class Test : IDisposable
    {
        private static List<Request> _requestList;
        private static HttpClient _client;
        private static Semaphore LoopController;

        private Config _config = null;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0044:Add readonly modifier", Justification = "can't be readonly - json serialization")]
        private Dictionary<string, PerfTarget> Targets = new Dictionary<string, PerfTarget>();

        /// <summary>
        /// CTOR
        /// </summary>
        /// <param name="config">Config</param>
        public Test(Config config)
        {
            if (config == null || config.FileList == null || string.IsNullOrEmpty(config.Host))
            {
                throw new ArgumentNullException(nameof(config));
            }

            _config = config;

            // setup the HttpClient
            _client = OpenHttpClient(config.Host);

            // setup the semaphore
            LoopController = new Semaphore(_config.MaxConcurrentRequests, _config.MaxConcurrentRequests);

            // load the performance targets
            Targets = LoadPerfTargets();

            // load the requests from json files
            _requestList = LoadValidateRequests(config.FileList);

            if (_requestList == null || _requestList.Count == 0)
            {
                Environment.Exit(-1);
            }
        }

        /// <summary>
        /// Opens and configures the shared HttpClient
        /// 
        /// Make sure to dispose via using or in IDispose
        /// </summary>
        /// <returns>HttpClient</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "handled in IDispose")]
        HttpClient OpenHttpClient(string host)
        {
            return new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
            {
                Timeout = new TimeSpan(0, 0, _config.RequestTimeout),
                BaseAddress = new Uri(host)
            };
        }

        /// <summary>
        /// Run the validation test one time
        /// </summary>
        /// <returns>bool</returns>
        public async Task<bool> RunOnce(Config config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            bool isError = false;
            int duration;
            DateTime dt;
            PerfLog pl;

            // send each request
            foreach (Request r in _requestList)
            {
                try
                {
                    dt = DateTime.UtcNow;

                    pl = await ExecuteRequest(r).ConfigureAwait(false);

                    if (config.SleepMs > 0)
                    {
                        duration = config.SleepMs - (int)pl.Duration;

                        if (duration > 0)
                        {
                            await Task.Delay(duration).ConfigureAwait(false);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // ignore any error and keep processing
                    Console.WriteLine($"{DateTime.UtcNow.ToString("MM/dd hh:mm:ss", CultureInfo.InvariantCulture)}\tException: {ex.Message}");
                    isError = true;
                }
            }

            return isError;
        }

        /// <summary>
        /// Summarize the requests for the hour
        /// </summary>
        /// <param name="timerState">TimerState</param>
        private static void SummaryLogTask(object timerState)
        {
            if (!(timerState is TimerRequestState state))
            {
                Console.WriteLine($"{DateTime.UtcNow.ToString("MM/dd hh:mm:ss", CultureInfo.InvariantCulture)}\tError\tTimerState is null");
                return;
            }

            // get count and reset to zero
            long count = Interlocked.Exchange(ref state.Count, 0);

            // log the count
            Console.WriteLine($"{state.CurrentLogTime.ToString("MM/dd HH:mm:ss", CultureInfo.InvariantCulture)}\tTotal Requests\t{count}");

            // set next log time
            state.CurrentLogTime = state.CurrentLogTime.AddHours(1);
        }

        /// <summary>
        /// Submit a request from the timer event
        /// </summary>
        /// <param name="timerState">TimerState</param>
        private static void SubmitRequestTask(object timerState)
        {
            // get a semaphore slot - rate limit the requests
            if (!LoopController.WaitOne(10))
            {
                return;
            }

            int index = 0;

            // cast to TimerState
            if (!(timerState is TimerRequestState state))
            {
                Console.WriteLine($"{DateTime.UtcNow.ToString("MM/dd hh:mm:ss", CultureInfo.InvariantCulture)}\tError\tTimerState is null");
                return;
            }

            // lock the state for updates
            lock (state.Lock)
            {
                index = state.Index;

                // increment
                state.Count++;
                state.Index++;

                // keep the index in range
                if (state.Index >= state.MaxIndex)
                {
                    state.Index = 0;
                }
            }

            // randomize request index
            if (state.Random != null)
            {
                index = state.Random.Next(0, state.MaxIndex);
            }

            Request req = _requestList[index];
            DateTime dt = DateTime.UtcNow;

            try
            {
                // Execute the request
                state.Test.ExecuteRequest(req).Wait();
            }

            catch (OperationCanceledException oce)
            {
                // log and ignore any error
                Console.WriteLine($"{DateTime.UtcNow.ToString("MM/dd hh:mm:ss", CultureInfo.InvariantCulture)}\t500\t{Math.Round(DateTime.UtcNow.Subtract(dt).TotalMilliseconds, 0)}\t0\t{req.Url}\tTaskCancelledException\t{oce.Message}");
            }

            catch (Exception ex)
            {
                // log and ignore any error
                Console.WriteLine($"{DateTime.UtcNow.ToString("MM/dd hh:mm:ss", CultureInfo.InvariantCulture)}\t500\t{Math.Round(DateTime.UtcNow.Subtract(dt).TotalMilliseconds, 0)}\t0\t{req.Url}\tWebvException\t{ex.Message}");
            }

            // make sure to release the semaphore
            LoopController.Release();
        }

        /// <summary>
        /// Display the startup message for RunLoop
        /// </summary>
        private static void DisplayStartupMessage(Config config)
        {
            string msg = string.Format(CultureInfo.InvariantCulture, $"{DateTime.UtcNow.ToString("MM/dd HH:mm:ss", CultureInfo.InvariantCulture)}\tStarting Web Validation Test Loop on {config.Host}\n\t\t");

            msg += "Files: ";
            if (config.FileList.Count > 1)
            {
                foreach (string f in config.FileList)
                {
                    msg += f.Replace("TestFiles/", string.Empty, StringComparison.OrdinalIgnoreCase) + " ";
                }
                msg = msg.Trim() + "\n\t\t";
            }
            else
            {
                msg += config.FileList[0].Replace("TestFiles/", string.Empty, StringComparison.OrdinalIgnoreCase);
            }

            msg += "; Sleep:" + config.SleepMs.ToString(CultureInfo.InvariantCulture);
            msg += "; MaxConcurrent: " + config.MaxConcurrentRequests.ToString(CultureInfo.InvariantCulture);
            if (config.Duration > 0)
            {
                msg += "; " + config.Duration.ToString(CultureInfo.InvariantCulture);
            }
            msg += config.Random ? "; Random" : string.Empty;
            msg += (bool)config.Verbose ? "; Verbose" : string.Empty;

            msg += string.IsNullOrEmpty(config.TelemetryApp) ? string.Empty : string.Format(CultureInfo.InvariantCulture, $"\n\t\tTelemetry: {config.TelemetryApp} {config.TelemetryKey}");

            Console.WriteLine(msg);
        }

        /// <summary>
        /// Run the validation tests in a loop
        /// </summary>
        /// <param name="id">thread id</param>
        /// <param name="config">Config</param>
        /// <param name="token">CancellationToken</param>
        /// <returns></returns>
        public void RunLoop(Config config, CancellationToken token)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));

            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            DateTime dtMax = DateTime.MaxValue;

            // only run for duration (seconds)
            if (config.Duration > 0)
            {
                dtMax = DateTime.UtcNow.AddSeconds(config.Duration);
            }

            // create the shared state
            TimerRequestState state = new TimerRequestState
            {
                MaxIndex = _requestList.Count,
                Test = this,

                // current hour
                CurrentLogTime = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0)
            };

            if (config.Random)
            {
                state.Random = new Random(DateTime.UtcNow.Millisecond);
            }

            if (config.SleepMs < 1)
            {
                config.SleepMs = 1;
            }

            DisplayStartupMessage(config);

            // start the timers
            Timer timer = new Timer(new TimerCallback(SubmitRequestTask), state, 0, config.SleepMs);
            Timer logTimer = new Timer(new TimerCallback(SummaryLogTask), state, (int)state.CurrentLogTime.AddHours(1).Subtract(DateTime.UtcNow).TotalMilliseconds, 60 * 60 * 1000);

            // run the wait loop
            if (dtMax == DateTime.MaxValue)
            {
                Task.Delay(-1).Wait(token);
            }
            else
            {
                // wait one hour to keep total milliseconds from overflowing
                while (dtMax.Subtract(DateTime.UtcNow).TotalHours > 0)
                {
                    Task.Delay(60 * 60 * 1000).Wait(token);
                }

                Task.Delay((int)dtMax.Subtract(DateTime.UtcNow).TotalMilliseconds).Wait(token);
            }

            // end and dispose of the timer
            if (timer != null)
            {
                timer.Dispose();
            }

            if (logTimer != null)
            {
                logTimer.Dispose();
            }
        }

        /// <summary>
        /// Execute a single validation test
        /// </summary>
        /// <param name="request">Request</param>
        /// <returns>PerfLog</returns>
        public async Task<PerfLog> ExecuteRequest(Request request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            PerfLog perfLog;

            // send the request
            using (HttpRequestMessage req = new HttpRequestMessage(new HttpMethod(request.Verb), request.Url))
            {
                DateTime dt = DateTime.UtcNow;

                // process the response
                using HttpResponseMessage resp = await _client.SendAsync(req).ConfigureAwait(false);
                string body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

                double duration = Math.Round(DateTime.UtcNow.Subtract(dt).TotalMilliseconds, 0);

                // validate the response
                string res = ValidateAll(request, resp, body);

                // check the performance
                perfLog = CreatePerfLog(request, res, duration, body, (long)resp.Content.Headers.ContentLength, (int)resp.StatusCode);
            }

            // log the test
            LogToConsole(request, perfLog);

            // add the metrics
            if (App.Metrics != null)
            {
                App.Metrics.Add(perfLog.StatusCode, perfLog.Duration, perfLog.Category, perfLog.Validated, perfLog.PerfLevel, perfLog.ContentLength, request.Url, perfLog.ValidationResults);
            }

            return perfLog;
        }

        /// <summary>
        /// Create a PerfLog
        /// </summary>
        /// <param name="request">Request</param>
        /// <param name="validationResults">validation errors</param>
        /// <param name="duration">duration</param>
        /// <param name="body">content body</param>
        /// <param name="contentLength">content length</param>
        /// <param name="statusCode">status code</param>
        /// <returns></returns>
        public PerfLog CreatePerfLog(Request request, string validationResults, double duration, string body, long contentLength, int statusCode)
        {
            // map the parameters
            PerfLog log = new PerfLog
            {
                StatusCode = statusCode,
                Category = request?.PerfTarget?.Category ?? string.Empty,
                Validated = string.IsNullOrEmpty(validationResults),
                ValidationResults = validationResults,
                Body = body,
                Duration = duration,
                ContentLength = contentLength,
                PerfLevel = 0
            };

            // determine the Performance Level based on category
            if (!string.IsNullOrEmpty(log.Category))
            {
                if (Targets.ContainsKey(log.Category))
                {
                    // lookup the target
                    PerfTarget target = Targets[log.Category];

                    if (target != null)
                    {
                        // set to max
                        log.PerfLevel = target.Targets.Count + 1;

                        for (int i = 0; i < target.Targets.Count; i++)
                        {
                            // find the lowest Perf Target achieved
                            if (duration <= target.Targets[i])
                            {
                                log.PerfLevel = i + 1;
                                break;
                            }
                        }
                    }
                }
            }

            return log;
        }

        /// <summary>
        /// Log the test
        /// </summary>
        /// <param name="request">Request</param>
        /// <param name="perfLog">PerfLog</param>
        void LogToConsole(Request request, PerfLog perfLog)
        {
            // only log 4XX and 5XX status codes unless verbose is true or config is null
            if (_config == null || (bool)_config.Verbose || perfLog.StatusCode > 399 || !string.IsNullOrEmpty(perfLog.ValidationResults))
            {
                Console.WriteLine($"{DateTime.UtcNow.ToString("MM/dd hh:mm:ss", CultureInfo.InvariantCulture)}\t{perfLog.StatusCode}\t{perfLog.Duration}\t{perfLog.Category.PadRight(13)}\t{perfLog.PerfLevel}\t{perfLog.Validated}\t{perfLog.ContentLength}\t{request.Url}{perfLog.ValidationResults.Replace("\n", string.Empty, StringComparison.OrdinalIgnoreCase)}", CultureInfo.InvariantCulture);
            }
        }
    }
}
