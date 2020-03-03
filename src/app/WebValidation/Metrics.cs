using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using System;
using System.Collections.Generic;

namespace WebValidation
{
    /// <summary>
    /// Test metrics
    /// </summary>
    public sealed class WebVMetrics : IDisposable
    {
        public TelemetryClient TelemetryClient { get; set; }
        public string TelemetryAppName { get; set; }

        // needs to be member variable for IDispose::Dispose
        private readonly TelemetryConfiguration _telemetryConfig;

        /// <summary>
        /// CTOR
        /// </summary>
        /// <param name="appName">Application Name</param>
        /// <param name="telemetryKey">App Insights Key</param>
        public WebVMetrics(string appName, string telemetryKey)
        {
            if (!string.IsNullOrEmpty(telemetryKey) && !string.IsNullOrEmpty(appName))
            {
                TelemetryAppName = appName;

                // create telemetry config
                // needs to be member variable for IDisopse::Dispose
                _telemetryConfig = new TelemetryConfiguration { InstrumentationKey = telemetryKey };
                _telemetryConfig.TelemetryChannel.DeveloperMode = true;

                // create telemetry client
                TelemetryClient = new TelemetryClient(_telemetryConfig);
            }
        }

        /// <summary>
        /// Add a metric to App Insights
        /// </summary>
        /// <param name="status">http status code (or 0 for validation error)</param>
        /// <param name="duration">duration of request in ms</param>
        /// <param name="category">category of request</param>
        /// <param name="perfLevel">perf level (quartile)</param>
        /// <param name="validated">validated successfully</param>
        /// <param name="contentLength">content length of response</param>
        /// <param name="message">validation / error message (default string.empty)</param>
        /// <param name="path">request path (url)</param>
        public void Add(int status, double duration, string category, bool validated, int perfLevel, long contentLength, string path, string message)
        {
            const string durationLabel = "Duration";
            const string appLabel = "AppName";
            const string categoryLabel = "Category";
            const string pathLabel = "Path";
            const string messageLabel = "Message";
            const string statusLabel = "Status";
            const string quartileLabel = "Quartile";
            const string validatedLabel = "Validated";
            const string contentLengthLabel = "ContentLength";

            if (TelemetryClient != null)
            {
                try
                {
                    // track the custom event
                    TelemetryClient.TrackEvent(categoryLabel,
                        new Dictionary<string, string> {
                            { appLabel, TelemetryAppName },
                            { categoryLabel, category },
                            { pathLabel, path },
                            { messageLabel, message }
                        },
                        new Dictionary<string, double> {
                            { durationLabel, duration },
                            { statusLabel, status },
                            { quartileLabel, perfLevel },
                            { validatedLabel, validated ? 1 : 0 },
                            { contentLengthLabel, contentLength }
                        });
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        /// <summary>
        /// IDispose::Dispose
        /// </summary>
        public void Dispose()
        {
            if (_telemetryConfig != null)
            {
                _telemetryConfig.Dispose();
            }
        }
    }
}
