using System;
using System.Collections.Generic;

namespace InsightLearn.WebAssembly.Models.Admin
{
    public class PrometheusMetricDto
    {
        public string Name { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public double Value { get; set; }
        public string Unit { get; set; } = string.Empty; // "percent", "bytes", "seconds", "milliseconds", "requests/s", "count", "connections", "errors/s"
        public DateTime Timestamp { get; set; }
        public Dictionary<string, string> Labels { get; set; } = new();
    }

    public class PrometheusQueryRequest
    {
        public string Query { get; set; } = string.Empty;
        public bool IsRangeQuery { get; set; }
        public DateTime? Start { get; set; }
        public DateTime? End { get; set; }
        public string? Step { get; set; }
    }

    public class PrometheusQueryResult
    {
        public string Status { get; set; } = string.Empty;
        public PrometheusData? Data { get; set; }
    }

    public class PrometheusRangeQueryResult
    {
        public string Status { get; set; } = string.Empty;
        public PrometheusRangeData? Data { get; set; }
    }

    public class PrometheusData
    {
        public string ResultType { get; set; } = string.Empty;
        public List<PrometheusResult> Result { get; set; } = new();
    }

    public class PrometheusRangeData
    {
        public string ResultType { get; set; } = string.Empty;
        public List<PrometheusRangeResult> Result { get; set; } = new();
    }

    public class PrometheusResult
    {
        public Dictionary<string, string> Metric { get; set; } = new();
        public object[]? Value { get; set; } // [timestamp, value]
    }

    public class PrometheusRangeResult
    {
        public Dictionary<string, string> Metric { get; set; } = new();
        public List<object[]> Values { get; set; } = new(); // [[timestamp, value], ...]
    }
}