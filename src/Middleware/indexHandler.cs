using Microsoft.AspNetCore.Builder;
using Newtonsoft.Json;
using Smoker;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace Helium
{
    public static class HomePageMiddlewareExtensions
    {
        static readonly HashSet<string> validPaths = new HashSet<string> { "/", "/INDEX.HTML", "/INDEX.HTM", "/DEFAULT.HTML", "/DEFAULT.HTM" };

        /// <summary>
        /// Middleware extension method to handle home page request
        /// </summary>
        /// <param name="builder">this IApplicationBuilder</param>
        /// <returns></returns>
        public static IApplicationBuilder UseHomePage(this IApplicationBuilder builder)
        {
            const string queryKey = "maxage";

            // create the middleware
            builder.Use(async (context, next) =>
            {
                // matches / or index.htm[l] or default.htm[l]
                if (validPaths.Contains(context.Request.Path.Value.ToUpperInvariant()))
                {
                    int maxAge = App.Metrics.MaxAge;

                    if (context.Request.Query.ContainsKey(queryKey))
                    {
                        if (int.TryParse(context.Request.Query[queryKey], out int val))
                        {
                            if (val > 0 && val < App.Metrics.MaxAge)
                            {
                                maxAge = val;
                            }
                        }
                    }
                    // run healthz check
                    string healthz = GetHealthz().GetAwaiter().GetResult();

                    // build the response
                    string html = string.Format(CultureInfo.InvariantCulture, $"Helium Integration Test\nV {Helium.Version.AssemblyVersion}\n\n");
                    html += GetConfig();
                    html += GetRunningTime();
                    html += GetMetrics(maxAge);
                    html += healthz;

                    byte[] responseBytes = System.Text.Encoding.UTF8.GetBytes(html);

                    // return the content
                    context.Response.ContentType = "text/plain";
                    await context.Response.Body.WriteAsync(responseBytes, 0, responseBytes.Length).ConfigureAwait(false);
                }
                else
                {
                    // not a match, so call next middleware handler
                    await next().ConfigureAwait(false);
                }
            });

            return builder;
        }

        /// <summary>
        /// call the /healtz/ietf endpoint
        /// </summary>
        /// <returns>string</returns>
        static async Task<string> GetHealthz()
        {
            string html = string.Format(CultureInfo.InvariantCulture, $"Healthz: {App.Config.Host}\n");

            try
            {
                // create the PerfTarget
                Smoker.Request req = new Smoker.Request
                {
                    Url = "/healthz/ietf",
                    PerfTarget = new Smoker.PerfTarget
                    {
                        Category = "healthz",
                        Targets = new List<double> { 1000, 1500, 2000 }
                    }
                };

                // create the validation rule
                req.Validation = new Smoker.Validation
                {
                    ContentType = "application/health+json",
                    JsonObject = new List<Smoker.JsonProperty>
                    {
                        new Smoker.JsonProperty { Field = "status", Value = "up" }
                    }
                };

                // run the health check
                PerfLog perfLog = await App.Smoker.ExecuteRequest(req).ConfigureAwait(false);

                // add results to metrics
                App.Metrics.Add(perfLog.StatusCode, perfLog.Duration, perfLog.Category, perfLog.Validated, perfLog.PerfLevel);

                // make the json pretty
                dynamic d = JsonConvert.DeserializeObject<dynamic>(perfLog.Body);
                string content = JsonConvert.SerializeObject(d, Formatting.Indented);

                html += content;
            }
#pragma warning disable CA1031 // implemented by design
            catch (Exception ex)
#pragma warning restore CA1031
            {
                html += ex.Message;
            }

            return html;
        }

        /// <summary>
        /// Compute the running time in minutes / hours / days
        /// </summary>
        /// <returns>string - Running for xx minutes</returns>
        static string GetRunningTime()
        {
            const string running = "Running for ";

            TimeSpan ts = DateTime.UtcNow.Subtract(App.StartTime);

            // 1 minute
            if (ts.TotalSeconds <= 90)
            {
                return running + "1 minute\n\n";
            }

            // xx minutes
            else if (ts.TotalMinutes <= 90)
            {
                return string.Format(CultureInfo.InvariantCulture, $"{running}{Math.Round(ts.TotalMinutes, 0)} minutes\n\n");
            }

            // xx hours
            else if (ts.TotalHours <= 36)
            {
                return string.Format(CultureInfo.InvariantCulture, $"{running}{Math.Round(ts.TotalHours, 0)} hours\n\n");
            }

            // xx.x days
            else
            {
                return string.Format(CultureInfo.InvariantCulture, $"{running}{Math.Round(ts.TotalDays, 1)} days\n\n");
            }
        }

        /// <summary>
        /// Get the metrics from the last 4 hours
        /// </summary>
        /// <returns>string</returns>
        static string GetMetrics(int maxAge)
        {
            // don't display metrics
            if (maxAge <= 0)
            {
                return string.Empty;
            }

            string html = "Metrics";

            if (DateTime.UtcNow.Subtract(App.StartTime).TotalMinutes > maxAge)
            {
                // show age of metrics
                html += string.Format(CultureInfo.InvariantCulture, $" (prior {maxAge} minutes)");
            }

            html += "\n";

            // add column headers
            html += ("Count").PadLeft(37) + "    Failures   Validation   Quartile 1     Quartile 2     Quartile 3     Quartile 4         Avg         Min         Max\n";

            // display each line
            List<MetricAggregate> list = App.Metrics.GetMetricList(maxAge);

            foreach (MetricAggregate r in list)
            {
                html += string.Format(CultureInfo.InvariantCulture, $"{r.Category.PadLeft(r.Category.Length + 4).PadRight(24).Substring(0, 24)} {r.Count.ToString(CultureInfo.InvariantCulture).PadLeft(12)} {r.Failures.ToString(CultureInfo.InvariantCulture).PadLeft(11)}  {r.ValidationErrors.ToString(CultureInfo.InvariantCulture).PadLeft(11)} {r.Q1p.ToString("0.000", CultureInfo.InvariantCulture).PadLeft(11)}% {r.Q2p.ToString("0.000", CultureInfo.InvariantCulture).PadLeft(13)}% {r.Q3p.ToString("0.000", CultureInfo.InvariantCulture).PadLeft(13)}% {r.Q4p.ToString("0.000", CultureInfo.InvariantCulture).PadLeft(13)}% {Math.Round(r.Average, 0).ToString(CultureInfo.InvariantCulture).PadLeft(11)} {r.Min.ToString(CultureInfo.InvariantCulture).PadLeft(11)} {r.Max.ToString(CultureInfo.InvariantCulture).PadLeft(11)} \n");
            }

            return html + "\n";
        }

        /// <summary>
        /// Get the current configuration
        /// </summary>
        /// <returns>string</returns>
        static string GetConfig()
        {
            string html = "Current Configuration: \n";

            html += string.Format(CultureInfo.InvariantCulture, $"\tThreads: {App.Config.Threads}\n\tSleep: {App.Config.SleepMs}\n");

            if (App.Config.Random)
            {
                html += string.Format(CultureInfo.InvariantCulture, $"\tRandomize\n");
            }

            if (App.Config.Verbose)
            {
                html += string.Format(CultureInfo.InvariantCulture, $"\tVerbose\n");
            }

            return html + "\n";
        }
    }
}
