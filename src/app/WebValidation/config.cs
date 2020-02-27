using System;
using System.Collections.Generic;

namespace WebValidation
{
    /// <summary>
    /// Web Validation Test Configuration
    /// </summary>
    public class Config : IDisposable
    {
        private WebVMetrics _metrics = null;

        public string Host { get; set; } = string.Empty;
        public bool RunLoop { get; set; } = false;
        public int MaxConcurrentRequests { get; set; } = 100;
        public int SleepMs { get; set; } = -1;
        public int Duration { get; set; } = 0;
        public bool Random { get; set; } = false;
        public bool? Verbose { get; set; } = null;
        public int RequestTimeout { get; set; } = 30;
        public string TelemetryKey { get; set; }
        public string TelemetryApp { get; set; }
        public List<string> FileList { get; } = new List<string>();
        public int MaxErrors { get; set; } = 10;

        public WebVMetrics Metrics
        {
            get
            {
                if (_metrics == null && !string.IsNullOrEmpty(TelemetryApp) && !string.IsNullOrEmpty(TelemetryKey))
                {
                    _metrics = new WebVMetrics(TelemetryApp, TelemetryKey);
                }

                return _metrics;
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
                if (_metrics != null)
                {
                    _metrics.Dispose();
                }
            }

            // Free any unmanaged objects
            disposed = true;
        }
    }
}
