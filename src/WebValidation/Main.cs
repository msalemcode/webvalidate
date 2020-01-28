using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using WebValidationTest;

namespace WebValidation
{
    // integration test for testing any REST API or web site
    public partial class Test : IDisposable
    {
        private readonly List<Request> _requestList;
        private HttpClient _client;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0044:Add readonly modifier", Justification = "can't be readonly - json serialization")]
        private Dictionary<string, PerfTarget> Targets = new Dictionary<string, PerfTarget>();
        private Config _config = null;
        private readonly string _baseUrl;

        /// <summary>
        /// CTOR
        /// </summary>
        /// <param name="fileList">list of files to load</param>
        /// <param name="baseUrl">server URL (i.e. https://www.microsoft.com)</param>
        public Test(List<string> fileList, string baseUrl)
        {
            if (fileList == null)
            {
                throw new ArgumentNullException(nameof(fileList));
            }

            if (string.IsNullOrEmpty(baseUrl))
            {
                throw new ArgumentNullException(nameof(baseUrl));
            }

            _baseUrl = baseUrl;
            _client = OpenHttpClient();

            // load the performance targets
            Targets = LoadPerfTargets();

            // load the requests
            _requestList = LoadRequests(fileList);
        }

        HttpClient OpenHttpClient()
        {
            return new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
            {
                Timeout = new TimeSpan(0, 0, 30),
                BaseAddress = new Uri(_baseUrl)
            };
        }

        /// <summary>
        /// Run the validation test one time
        /// </summary>
        /// <returns>bool</returns>
        public async Task<bool> RunOnce()
        {
            bool isError = false;

            // send each request
            foreach (Request r in _requestList)
            {
                try
                {
                    await ExecuteRequest(r).ConfigureAwait(false);
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
        /// Run the validation tests in a loop
        /// </summary>
        /// <param name="id">thread id</param>
        /// <param name="config">Config</param>
        /// <param name="ct">CancellationToken</param>
        /// <returns></returns>
        public async Task RunLoop(int id, Config config, CancellationToken ct)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));

            if (ct == null)
            {
                throw new ArgumentNullException(nameof(ct));
            }

            DateTime dt;
            DateTime nextPrune = DateTime.UtcNow.AddMinutes(1);
            DateTime dtMax = DateTime.MaxValue;
            PerfLog perfLog;

            int i;
            Request r;

            Random rand = new Random(DateTime.UtcNow.Millisecond);

            // only run for duration (seconds)
            if (config.Duration > 0)
            {
                dtMax = DateTime.UtcNow.AddSeconds(config.Duration);
            }

            if (ct.IsCancellationRequested)
            {
                return;
            }

            bool isError = false;

            // loop for duration or forever
            while (DateTime.UtcNow < dtMax)
            {
                i = 0;

                // send each request
                while (i < _requestList.Count && DateTime.UtcNow < dtMax)
                {
                    if (ct.IsCancellationRequested)
                    {
                        return;
                    }

                    if (config.Random)
                    {
                        i = rand.Next(0, _requestList.Count - 1);
                    }

                    r = _requestList[i];
                    dt = DateTime.UtcNow;

                    try
                    {
                        // create the request
                        perfLog = await ExecuteRequest(r).ConfigureAwait(false);
                    }
                    catch (System.Threading.Tasks.TaskCanceledException tce)
                    {
                        // request timeout error
                        string message = tce.Message;

                        if (tce.InnerException != null)
                        {
                            message = tce.InnerException.Message;
                        }

                        Console.WriteLine($"{id}\t500\t{Math.Round(DateTime.UtcNow.Subtract(dt).TotalMilliseconds, 0)}\t0\t{r.Url}\tTaskCancelledException\t{message}");
                        if (tce.InnerException != null)
                        {
                            Console.WriteLine(tce.InnerException);
                        }
                        else
                        {
                            Console.WriteLine(tce);
                        }

                        isError = true;
                    }

                    catch (Exception ex)
                    {
                        // ignore any error and keep processing
                        Console.WriteLine($"{id}\t500\t{Math.Round(DateTime.UtcNow.Subtract(dt).TotalMilliseconds, 0)}\t0\t{r.Url}\tWebvException\t{ex.Message}\n{ex}");

                        isError = true;
                    }

                    // TODO - smoker is hanging here
                    if (isError)
                    {
                        Console.WriteLine("Creating new HttpClient ...");
                        _client.Dispose();
                        _client = null;
                        await Task.Delay(500).ConfigureAwait(false);

                        _client = OpenHttpClient();
                        isError = false;
                    }

                    // increment the index
                    if (!config.Random)
                    {
                        i++;
                    }

                    if (ct.IsCancellationRequested)
                    {
                        return;
                    }

                    // sleep between each request
                    await Task.Delay(config.SleepMs).ConfigureAwait(false);

                    if (ct.IsCancellationRequested)
                    {
                        return;
                    }

                    if (DateTime.UtcNow > nextPrune)
                    {
                        App.Metrics.Prune();
                        nextPrune = DateTime.UtcNow.AddMinutes(1);
                    }
                }
            }
        }

        /// <summary>
        /// Execute a validation test
        /// </summary>
        /// <param name="r">Request</param>
        /// <returns>PerfLog</returns>
        public async Task<PerfLog> ExecuteRequest(Request r)
        {
            if (r == null)
            {
                throw new ArgumentNullException(nameof(r));
            }

            PerfLog perfLog;

            using (HttpRequestMessage req = new HttpRequestMessage(new HttpMethod(r.Verb), r.Url))
            {
                DateTime dt = DateTime.UtcNow;

                // process the response
                using (HttpResponseMessage resp = await _client.SendAsync(req).ConfigureAwait(false))
                {
                    string body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

                    double duration = Math.Round(DateTime.UtcNow.Subtract(dt).TotalMilliseconds, 0);

                    // validate the response
                    string res = ValidateAll(r, resp, body);

                    // check the performance
                    perfLog = CreatePerfLog(r, res, duration, body, (long)resp.Content.Headers.ContentLength, (int)resp.StatusCode);
                }

                req.Dispose();
            }

            // log the test
            LogToConsole(r, perfLog);

            // add the metrics
            if ((bool)_config?.RunWeb)
            {
                App.Metrics.Add(perfLog.StatusCode, perfLog.Duration, perfLog.Category, perfLog.Validated, perfLog.PerfLevel);
            }

            return perfLog;
        }

        /// <summary>
        /// Create a PerfLog
        /// </summary>
        /// <param name="r">Request</param>
        /// <param name="res">validation errors</param>
        /// <param name="duration">duration</param>
        /// <param name="body">content body</param>
        /// <param name="contentLength">content length</param>
        /// <param name="statusCode">status code</param>
        /// <returns></returns>
        public PerfLog CreatePerfLog(Request r, string res, double duration, string body, long contentLength, int statusCode)
        {
            // map the parameters
            PerfLog log = new PerfLog
            {
                StatusCode = statusCode,
                Category = r?.PerfTarget?.Category ?? string.Empty,
                Validated = string.IsNullOrEmpty(res),
                ValidationResults = res,
                Body = body,
                Duration = duration,
                ContentLength = contentLength
            };

            // determine the Performance Level
            if (!string.IsNullOrEmpty(log.Category))
            {
                PerfTarget target = Targets[log.Category];

                if (target != null)
                {
                    log.PerfLevel = target.Targets.Count + 1;

                    for (int i = 0; i < target.Targets.Count; i++)
                    {
                        if (duration <= target.Targets[i])
                        {
                            log.PerfLevel = i + 1;
                            break;
                        }
                    }
                }
            }

            return log;
        }

        /// <summary>
        /// Log the test
        /// </summary>
        /// <param name="r">Request</param>
        /// <param name="perfLog">PerfLog</param>
        void LogToConsole(Request r, PerfLog perfLog)
        {
            string log = string.Empty;

            // date is redundant if running as a web server
            if (_config == null || !_config.RunWeb)
            {
                log = string.Format(CultureInfo.InvariantCulture, $"{DateTime.UtcNow.ToString("MM/dd hh:mm:ss", CultureInfo.InvariantCulture)}\t");
            }

            log += string.Format(CultureInfo.InvariantCulture, $"{perfLog.StatusCode}\t{perfLog.Duration}\t{perfLog.Category.PadRight(13)}\t{perfLog.PerfLevel}\t{perfLog.Validated}\t{perfLog.ContentLength}\t{r.Url}{perfLog.ValidationResults.Replace("\n", string.Empty, StringComparison.OrdinalIgnoreCase)}");

            // only log 4XX and 5XX status codes unless verbose is true
            if (_config == null || _config.Verbose || perfLog.StatusCode > 399 || !string.IsNullOrEmpty(perfLog.ValidationResults))
            {
                Console.WriteLine(log);
            }
        }
    }
}
