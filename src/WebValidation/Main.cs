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
        private readonly HttpClient _client;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0044:Add readonly modifier", Justification = "can't be readonly - json serialization")]
        private Dictionary<string, PerfTarget> Targets = new Dictionary<string, PerfTarget>();
        private Config _config = null;
        private readonly string _baseUrl;

        /// <summary>
        /// CTOR
        /// </summary>
        /// <param name="fileList">list of files to load</param>
        /// <param name="baseUrl">server URL (i.e. https://www.microsoft.com)</param>
        public Test(string baseUrl, List<string> fileList)
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

        /// <summary>
        /// Opens and configures the shared HttpClient
        /// 
        /// Make sure to dispose via using or in IDispose
        /// </summary>
        /// <returns>HttpClient</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "handled in IDispose")]
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
        public async Task<bool> RunOnce(Config config)
        {
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
        /// Run the validation tests in a loop
        /// </summary>
        /// <param name="id">thread id</param>
        /// <param name="config">Config</param>
        /// <param name="ct">CancellationToken</param>
        /// <returns></returns>
        public async Task RunLoop(Config config, CancellationToken ct)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));

            if (ct == null)
            {
                throw new ArgumentNullException(nameof(ct));
            }

            DateTime dt;
            DateTime dtMax = DateTime.MaxValue;
            DateTime dtLog = DateTime.UtcNow;
            int duration;
            PerfLog perfLog;
            int i;
            int count = 0;
            Request r;
            Random rand = new Random(DateTime.UtcNow.Millisecond);

            dtLog = new DateTime(dtLog.Year, dtLog.Month, dtLog.Day, dtLog.Hour, 0, 0).AddHours(1);

            // only run for duration (seconds)
            if (config.Duration > 0)
            {
                dtMax = DateTime.UtcNow.AddSeconds(config.Duration);
            }

            // loop for duration or forever
            while (DateTime.UtcNow < dtMax)
            {
                i = 0;

                // send each request
                while (i < _requestList.Count && DateTime.UtcNow < dtMax)
                {
                    // log requests in last hour
                    if (DateTime.UtcNow > dtLog)
                    {
                        Console.WriteLine($"{dtLog.AddHours(-1).ToString("MM/dd HH:mm:ss", CultureInfo.InvariantCulture)}\tRequests\t{count}");
                        dtLog = dtLog.AddHours(1);
                        count = 0;
                    }
                    if (ct.IsCancellationRequested)
                    {
                        return;
                    }

                    count++;

                    // randomize request
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
                    catch (OperationCanceledException oce)
                    {
                        // ignore any error and keep processing
                        Console.WriteLine($"{DateTime.UtcNow.ToString("MM/dd hh:mm:ss", CultureInfo.InvariantCulture)}\t500\t{Math.Round(DateTime.UtcNow.Subtract(dt).TotalMilliseconds, 0)}\t0\t{r.Url}\tTaskCancelledException\t{oce.Message}");
                    }

                    catch (Exception ex)
                    {
                        // ignore any error and keep processing
                        Console.WriteLine($"{DateTime.UtcNow.ToString("MM/dd hh:mm:ss", CultureInfo.InvariantCulture)}\t500\t{Math.Round(DateTime.UtcNow.Subtract(dt).TotalMilliseconds, 0)}\t0\t{r.Url}\tWebvException\t{ex.Message}");
                    }

                    // increment the index if not random
                    if (!config.Random)
                    {
                        i++;
                    }

                    // compute the target sleep time
                    duration = config.SleepMs - (int)DateTime.UtcNow.Subtract(dt).TotalMilliseconds;

                    // sleep between each request
                    if (duration > 0)
                    {
                        await Task.Delay(duration, ct).ConfigureAwait(false);
                    }
                }
            }
        }

        /// <summary>
        /// Execute a single validation test
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
                using HttpResponseMessage resp = await _client.SendAsync(req).ConfigureAwait(false);
                string body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

                double duration = Math.Round(DateTime.UtcNow.Subtract(dt).TotalMilliseconds, 0);

                // validate the response
                string res = ValidateAll(r, resp, body);

                // check the performance
                perfLog = CreatePerfLog(r, res, duration, body, (long)resp.Content.Headers.ContentLength, (int)resp.StatusCode);
            }

            // log the test
            LogToConsole(r, perfLog);

            // add the metrics
            // TODO - change this to use App Insights
            //App.Metrics.Add(perfLog.StatusCode, perfLog.Duration, perfLog.Category, perfLog.Validated, perfLog.PerfLevel);

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
            string log = string.Format(CultureInfo.InvariantCulture, $"{DateTime.UtcNow.ToString("MM/dd hh:mm:ss", CultureInfo.InvariantCulture)}\t{perfLog.StatusCode}\t{perfLog.Duration}\t{perfLog.Category.PadRight(13)}\t{perfLog.PerfLevel}\t{perfLog.Validated}\t{perfLog.ContentLength}\t{r.Url}{perfLog.ValidationResults.Replace("\n", string.Empty, StringComparison.OrdinalIgnoreCase)}");

            // only log 4XX and 5XX status codes unless verbose is true
            if (_config == null || _config.Verbose || perfLog.StatusCode > 399 || !string.IsNullOrEmpty(perfLog.ValidationResults))
            {
                Console.WriteLine(log);
            }
        }
    }
}
