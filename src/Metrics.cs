using System;
using System.Collections.Generic;
using System.Linq;

namespace WebValidationTest
{
    /// <summary>
    /// Test metrics
    /// </summary>
    public sealed class Metrics
    {
        public List<Metric> Requests { get; } = new List<Metric>();

        /// <summary>
        /// Get the metric aggregates
        /// </summary>
        /// <returns>List of MetricAggregate</returns>
        public List<MetricAggregate> GetMetricList(int maxAge)
        {
            // Build the list of expected results
            List<MetricAggregate> res = new List<MetricAggregate>();

            DateTime minDate = DateTime.UtcNow.AddMinutes(-1 * maxAge);

            List<dynamic> query;

            // run the aggregate query
            lock (Requests)
            {
                query = Requests.Where(r => r.Time >= minDate)
                    .GroupBy(r => r.Category,
                    (cat, reqs) => new
                    {
                        Category = cat,
                        Count = reqs.Count(),
                        Failures = reqs.Count(d => d.StatusCode >= 400),
                        ValidationErrors = reqs.Count(d => !d.Validated),
                        Q1 = reqs.Count(d => d.PerfLevel == 1),
                        Q2 = reqs.Count(d => d.PerfLevel == 2),
                        Q3 = reqs.Count(d => d.PerfLevel == 3),
                        Q4 = reqs.Count(d => d.PerfLevel == 4),
                        Duration = reqs.Sum(d => d.Duration),
                        Min = reqs.Min(d => d.Duration),
                        Max = reqs.Max(d => d.Duration)
                    }).OrderBy(d => d.Category).ToList<dynamic>();
            }

            MetricAggregate m3;

            // update the result list based on the aggregate
            foreach (dynamic r in query)
            {
                m3 = new MetricAggregate();
                res.Add(m3);

                m3.Category = r.Category;
                m3.Count = r.Count;
                m3.Failures = r.Failures;
                m3.ValidationErrors = r.ValidationErrors;
                m3.Q1 = r.Q1;
                m3.Q2 = r.Q2;
                m3.Q3 = r.Q3;
                m3.Q4 = r.Q4;
                m3.Duration = r.Duration;
                m3.Min = r.Min;
                m3.Max = r.Max;
                m3.Average = m3.Count > 0 ? m3.Duration / m3.Count : 0;
            }

            return res;
        }

        /// <summary>
        /// Get the metric key from the status code
        /// </summary>
        /// <param name="status"></param>
        /// <returns>2xx, 3xx, etc.</returns>
        public static string GetKeyFromStatus(int status)
        {
            switch (status / 100)
            {
                case 0:
                    return "Validation Errors";
                case 2:
                case 3:
                case 4:
                case 5:
                    return string.Format(System.Globalization.CultureInfo.InvariantCulture, $"{status / 100}xx");

                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Add a metric to the list
        /// </summary>
        /// <param name="status">http status code (or 0 for validation error)</param>
        /// <param name="duration">duration of request in ms</param>
        /// <param name="category">category of request</param>
        /// <param name="perfLevel">perf level (quartile)</param>
        /// <param name="validated">validated successfully</param>
        public void Add(int status, double duration, string category, bool validated, int perfLevel)
        {
            // validate status
            if (status >= 200 && status < 600)
            {
                // create outside the lock for concurrency
                var metric = new Metric { StatusCode = status, Duration = duration, Category = category, Validated = validated, PerfLevel = perfLevel };

                lock (Requests)
                {
                    Requests.Add(metric);
                }
            }
        }
    }

    /// <summary>
    /// Represents one request
    /// </summary>
    public class Metric
    {
        public DateTime Time { get; set; } = DateTime.UtcNow;
        public string Key { get; set; } = string.Empty;
        public int StatusCode { get; set; } = 0;
        public double Duration { get; set; } = 0;
        public string Category { get; set; } = string.Empty;
        public int PerfLevel { get; set; } = 0;
        public bool Validated { get; set; } = true;
    }

    /// <summary>
    /// Metric aggregation by Category
    /// </summary>
    public class MetricAggregate
    {
        public string Category { get; set; }
        public long Count { get; set; } = 0;
        public int Failures { get; set; }
        public int ValidationErrors { get; set; }
        public int Q1 { get; set; }
        public int Q2 { get; set; }
        public int Q3 { get; set; }
        public int Q4 { get; set; }
        public double Duration { get; set; } = 0;
        public double Average { get; set; } = 0;
        public double Min { get; set; } = 0;
        public double Max { get; set; } = 0;

        // Quartile percentages
        public double Q1p => ToPercent(Q1);
        public double Q2p => ToPercent(Q2);
        public double Q3p => ToPercent(Q3);
        public double Q4p => ToPercent(Q4);

        /// <summary>
        /// Compute Quartile percentage - rounded to 3 decimal places
        /// </summary>
        /// <param name="value">double</param>
        /// <returns>double</returns>
        private double ToPercent(double value)
        {
            double d = 0;

            if (Count > 0 && value > 0)
            {
                d = value / Count * 100;

                // don't report 0 if count > 1
                if (d < .001)
                {
                    d = .001;
                }
            }

            return Math.Round(d, 3);
        }
    }
}
