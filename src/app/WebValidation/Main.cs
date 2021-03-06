using CSE.WebValidate.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Text;

namespace CSE.WebValidate
{
    /// <summary>
    /// Web Validation Test
    /// </summary>
    public partial class WebV : IDisposable
    {
        private static List<Request> requestList;
        private static HttpClient client;
        private static Semaphore LoopController;

        private Config config = null;

        private readonly Dictionary<string, PerfTarget> Targets = new Dictionary<string, PerfTarget>();

        /// <summary>
        /// CTOR
        /// </summary>
        /// <param name="config">Config</param>
        public WebV(Config config)
        {
            if (config == null || config.Files == null || string.IsNullOrEmpty(config.Server))
            {
                throw new ArgumentNullException(nameof(config));
            }

            this.config = config;

            // setup the HttpClient
            client = OpenHttpClient(config.Server);

            // setup the semaphore
            LoopController = new Semaphore(this.config.MaxConcurrent, this.config.MaxConcurrent);

            // load the performance targets
            Targets = LoadPerfTargets();

            // load the requests from json files
            requestList = LoadValidateRequests(config.Files);

            if (requestList == null || requestList.Count == 0)
            {
                throw new ArgumentException("RequestList is empty");
            }
        }

        /// <summary>
        /// Opens and configures the shared HttpClient
        /// 
        /// Disposed in IDispose
        /// </summary>
        /// <returns>HttpClient</returns>
        HttpClient OpenHttpClient(string host)
        {
            var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
            {
                Timeout = new TimeSpan(0, 0, config.Timeout),
                BaseAddress = new Uri(host)
            };
            client.DefaultRequestHeaders.Add("User-Agent", "webValidate");

            return client;
        }

        /// <summary>
        /// Run the validation test one time
        /// </summary>
        /// <returns>bool</returns>
        public async Task<int> RunOnce(Config config, CancellationToken token)
        {
            if (config == null)
            {
                Console.WriteLine("RunOnce:Config is null");
                return -1;
            }

            int duration;
            PerfLog pl;
            int errorCount = 0;
            int validationFailureCount = 0;

            // send each request
            foreach (Request r in requestList)
            {
                try
                {
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    // stop after MaxErrors errors
                    if ((errorCount + validationFailureCount) > config.MaxErrors)
                    {
                        break;
                    }

                    // execute the request
                    pl = await ExecuteRequest(r).ConfigureAwait(false);

                    if (pl.Failed)
                    {
                        errorCount++;
                    }

                    if (!pl.Validated)
                    {
                        validationFailureCount++;
                    }

                    // sleep if configured
                    if (config.Sleep > 0)
                    {
                        duration = config.Sleep - (int)pl.Duration;

                        if (duration > 0)
                        {
                            await Task.Delay(duration, token).ConfigureAwait(false);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // ignore any error and keep processing
                    Console.WriteLine($"{DateTime.UtcNow.ToString("MM/dd hh:mm:ss", CultureInfo.InvariantCulture)}\tException: {ex.Message}");
                    errorCount++;
                }
            }

            // display validation failure count
            if (validationFailureCount > 0)
            {
                Console.WriteLine($"Validation Errors: {validationFailureCount}");
            }

            // display error count
            if (errorCount > 0)
            {
                Console.WriteLine($"Failed: {errorCount} Errors");
            }

            // fail if MaxErrors exceeded
            else if (validationFailureCount > config.MaxErrors)
            {
                Console.Write($"Failed: Validation Errors({validationFailureCount}) exceeded MaxErrors ({config.MaxErrors})");
                errorCount += validationFailureCount;
            }

            // return non-zero exit code on failure
            return errorCount;
        }

        /// <summary>
        /// Summarize the requests for the hour
        /// </summary>
        /// <param name="timerState">TimerState</param>
        private static void SummaryLogTask(object timerState)
        {
            if (timerState is TimerRequestState state)
            {
                // exit if cancelled
                if (state.Token.IsCancellationRequested)
                {
                    return;
                }

                // get count and reset to zero
                long count = Interlocked.Exchange(ref state.Count, 0);

                // log the count
                Console.WriteLine($"{state.CurrentLogTime.ToString("MM/dd HH:mm:ss", CultureInfo.InvariantCulture)}\tTotal Requests\t{count}");

                // set next log time
                state.CurrentLogTime = state.CurrentLogTime.AddHours(1);
            }
        }

        /// <summary>
        /// Submit a request from the timer event
        /// </summary>
        /// <param name="timerState">TimerState</param>
        private static void SubmitRequestTask(object timerState)
        {
            int index = 0;

            // cast to TimerState
            if (!(timerState is TimerRequestState state))
            {
                Console.WriteLine($"{DateTime.UtcNow.ToString("MM/dd hh:mm:ss", CultureInfo.InvariantCulture)}\tError\tTimerState is null");
                return;
            }

            // exit if cancelled
            if (state.Token.IsCancellationRequested)
            {
                return;
            }

            // get a semaphore slot - rate limit the requests
            if (!LoopController.WaitOne(10))
            {
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

            Request req = requestList[index];
            DateTime dt = DateTime.UtcNow;

            try
            {
                // Execute the request
                state.Test.ExecuteRequest(req).Wait();
            }

            catch (OperationCanceledException oce)
            {
                // log and ignore any error
                Console.WriteLine($"{DateTime.UtcNow.ToString("MM/dd hh:mm:ss", CultureInfo.InvariantCulture)}\t500\t{Math.Round(DateTime.UtcNow.Subtract(dt).TotalMilliseconds, 0)}\t0\t{req.Path}\tTaskCancelledException\t{oce.Message}");
            }

            catch (Exception ex)
            {
                // log and ignore any error
                Console.WriteLine($"{DateTime.UtcNow.ToString("MM/dd hh:mm:ss", CultureInfo.InvariantCulture)}\t500\t{Math.Round(DateTime.UtcNow.Subtract(dt).TotalMilliseconds, 0)}\t0\t{req.Path}\tWebvException\t{ex.Message}");
            }

            // make sure to release the semaphore
            LoopController.Release();
        }

        /// <summary>
        /// Display the startup message for RunLoop
        /// </summary>
        private static void DisplayStartupMessage(Config config)
        {
            string msg = $"{DateTime.UtcNow.ToString("MM/dd HH:mm:ss", CultureInfo.InvariantCulture)}\tStarting Web Validation Test";
            msg += $"\n\t\tVersion: {CSE.WebValidate.Version.AssemblyVersion}";
            msg += $"\n\t\tHost: {config.Server}";

            if (!string.IsNullOrEmpty(config.BaseUrl))
            {
                msg += $"\n\t\tBaseUrl: {config.BaseUrl}";
            }

            msg += $"\n\t\tFiles: {string.Join(' ', config.Files)}";
            msg += $"\n\t\tSleep: {config.Sleep}";
            msg += $"\n\t\tMaxConcurrent: {config.MaxConcurrent}";

            if (config.Duration > 0)
            {
                msg += $"\n\t\tDuration: {config.Duration}";
            }

            msg += config.Random ? "\n\t\tRandom" : string.Empty;
            msg += config.Verbose ? "\n\t\tVerbose" : string.Empty;

            msg += string.IsNullOrEmpty(config.TelemetryName) ? string.Empty : $"\n\t\tTelemetry: {config.TelemetryName} {config.TelemetryKey}";

            Console.WriteLine(msg + "\n");
        }

        /// <summary>
        /// Run the validation tests in a loop
        /// </summary>
        /// <param name="id">thread id</param>
        /// <param name="config">Config</param>
        /// <param name="token">CancellationToken</param>
        /// <returns></returns>
        public int RunLoop(Config config, CancellationToken token)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));

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
                MaxIndex = requestList.Count,
                Test = this,

                // current hour
                CurrentLogTime = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0),

                Token = token
            };

            if (config.Random)
            {
                state.Random = new Random(DateTime.UtcNow.Millisecond);
            }

            if (config.Sleep < 1)
            {
                config.Sleep = 1;
            }

            DisplayStartupMessage(config);

            // start the timers
            using Timer timer = new Timer(new TimerCallback(SubmitRequestTask), state, 0, config.Sleep);
            using Timer logTimer = new Timer(new TimerCallback(SummaryLogTask), state, (int)state.CurrentLogTime.AddHours(1).Subtract(DateTime.UtcNow).TotalMilliseconds, 60 * 60 * 1000);

            try
            {
                // run the wait loop
                if (dtMax == DateTime.MaxValue)
                {
                    Task.Delay(-1).Wait(token);
                }
                else
                {
                    // wait one hour to keep total milliseconds from overflowing
                    while (dtMax.Subtract(DateTime.UtcNow).TotalHours > 1)
                    {
                        Task.Delay(60 * 60 * 1000).Wait(token);
                    }

                    int delay = (int)dtMax.Subtract(DateTime.UtcNow).TotalMilliseconds;

                    if (delay > 0)
                    {
                        Task.Delay(delay).Wait(token);
                    }
                }
            }
            catch (OperationCanceledException ex)
            {
                // safe to ignore
                Console.WriteLine(ex.Message);
            }

            // graceful exit
            return 0;
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
            ValidationResult valid;

            // send the request
            using (HttpRequestMessage req = new HttpRequestMessage(new HttpMethod(request.Verb), request.Path))
            {
                DateTime dt = DateTime.UtcNow;

                // add the headers to the http request
                if (request.Headers != null && request.Headers.Count > 0)
                {
                    foreach (var key in request.Headers.Keys)
                    {
                        req.Headers.Add(key, request.Headers[key]);
                    }
                }

                // add the body to the http request
                if (!string.IsNullOrEmpty(request.Body))
                {
                    if (!string.IsNullOrEmpty(request.ContentMediaType))
                    {
                        req.Content = new StringContent(request.Body,Encoding.UTF8,request.ContentMediaType);
                    }
                    else
                    {
                        req.Content = new StringContent(request.Body);
                    }

                        
                }

                // process the response
                using HttpResponseMessage resp = await client.SendAsync(req).ConfigureAwait(false);
                string body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

                double duration = Math.Round(DateTime.UtcNow.Subtract(dt).TotalMilliseconds, 0);

                // validate the response
                valid = Response.Validator.Validate(request, resp, body);

                // check the performance
                perfLog = CreatePerfLog(request, valid, duration, (long)resp.Content.Headers.ContentLength, (int)resp.StatusCode);
            }

            // log the test
            LogToConsole(request, valid, perfLog);

            // add the metrics
            if (config.Metrics != null)
            {
                config.Metrics.Add(perfLog.StatusCode, perfLog.Duration, perfLog.Category, perfLog.Validated, perfLog.PerfLevel, perfLog.ContentLength, request.Path, perfLog.ValidationResults);
            }

            return perfLog;
        }

        /// <summary>
        /// Create a PerfLog
        /// </summary>
        /// <param name="request">Request</param>
        /// <param name="validationResult">validation errors</param>
        /// <param name="duration">duration</param>
        /// <param name="body">content body</param>
        /// <param name="contentLength">content length</param>
        /// <param name="statusCode">status code</param>
        /// <returns></returns>
        public PerfLog CreatePerfLog(Request request, ValidationResult validationResult, double duration, long contentLength, int statusCode)
        {
            if (validationResult == null)
            {
                throw new ArgumentNullException(nameof(validationResult));
            }

            // map the parameters
            PerfLog log = new PerfLog
            {
                StatusCode = statusCode,
                Category = request?.PerfTarget?.Category ?? string.Empty,
                Validated = !validationResult.Failed && validationResult.ValidationErrors.Count == 0,
                ValidationResults = string.Join('\t', validationResult.ValidationErrors),
                Duration = duration,
                ContentLength = contentLength,
                PerfLevel = 0,
                Failed = validationResult.Failed
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
                        log.PerfLevel = target.Quartiles.Count + 1;

                        for (int i = 0; i < target.Quartiles.Count; i++)
                        {
                            // find the lowest Perf Target achieved
                            if (duration <= target.Quartiles[i])
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
        void LogToConsole(Request request, ValidationResult valid, PerfLog perfLog)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (valid == null)
            {
                throw new ArgumentNullException(nameof(valid));
            }

            if (perfLog == null)
            {
                throw new ArgumentNullException(nameof(perfLog));
            }

            // only log 4XX and 5XX status codes unless verbose is true or there were validation errors
            if ((config.Verbose) || perfLog.StatusCode > 399 || valid.Failed || valid.ValidationErrors.Count > 0)
            {
                string log = $"{DateTime.UtcNow.ToString("MM/dd hh:mm:ss", CultureInfo.InvariantCulture)}\t";
                log += $"{perfLog.StatusCode}\t{perfLog.Duration}\t";
                log += $"{perfLog.Category.PadRight(12).Substring(0, 12)}\t";
                log += $"{(perfLog.PerfLevel > 0 && perfLog.PerfLevel <= 4 ? perfLog.PerfLevel.ToString(CultureInfo.InvariantCulture) : string.Empty)}\t";
                log += $"{perfLog.Validated}\t{perfLog.ContentLength}\t{request.Path}";

                if (valid.Failed)
                {
                    log += "\tFAILED";
                }

                if (valid.ValidationErrors.Count > 0)
                {
                    log += "\t" + string.Join('\t', valid.ValidationErrors);
                }

                Console.WriteLine(log);
            }
        }
    }
}