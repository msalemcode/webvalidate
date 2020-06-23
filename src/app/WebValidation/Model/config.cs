using System;
using System.Collections.Generic;
using System.Globalization;

namespace CSE.WebValidate
{
    /// <summary>
    /// Web Validation Test Configuration
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "json serialization")]
    public class Config : IDisposable
    {
        private WebVMetrics metrics = null;

        public string Server { get; set; }

        public List<string> Files { get; set; } = new List<string>();
        public bool RunLoop { get; set; }
        public int Sleep { get; set; }
        public int Duration { get; set; }
        public bool Random { get; set; }
        public bool Verbose { get; set; }
        public int Timeout { get; set; }
        public string TelemetryKey { get; set; }
        public string TelemetryName { get; set; }
        public int MaxConcurrent { get; set; }
        public int MaxErrors { get; set; }
        public bool DryRun { get; set; }
        public string BaseUrl { get; set; }

        public WebVMetrics Metrics
        {
            get
            {
                if (metrics == null && !string.IsNullOrEmpty(TelemetryName) && !string.IsNullOrEmpty(TelemetryKey))
                {
                    metrics = new WebVMetrics(TelemetryName, TelemetryKey);
                }

                return metrics;
            }
        }

        public void SetDefaultValues()
        {
            // make it easier to pass server value
            if (!Server.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                if (Server.StartsWith("localhost", StringComparison.OrdinalIgnoreCase) || Server.StartsWith("127.0.0.1", StringComparison.OrdinalIgnoreCase))
                {
                    Server = "http://" + Server;
                }
                else
                {
                    Server = string.Format(CultureInfo.InvariantCulture, $"https://{Server}.azurewebsites.net");
                }
            }

            // add a trailing slash if necessary
            if (!string.IsNullOrEmpty(BaseUrl) && !BaseUrl.EndsWith('/'))
            {
                BaseUrl += "/";
            }
        }

        private bool disposed = false;

        // iDisposable::Dispose
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                if (metrics != null)
                {
                    metrics.Dispose();
                }
            }

            // Free any unmanaged objects
            disposed = true;
        }
    }
}
