using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using InsightLearn.Application.Interfaces;
using InsightLearn.Core.DTOs.Admin;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace InsightLearn.Application.Services
{
    public interface IPrometheusService
    {
        Task<PrometheusQueryResult> QueryAsync(string query);
        Task<PrometheusRangeQueryResult> QueryRangeAsync(string query, DateTime start, DateTime end, string step);
        Task<List<PrometheusMetricDto>> GetInfrastructureMetricsAsync();
        Task<List<PrometheusMetricDto>> GetApiMetricsAsync();
        Task<Dictionary<string, double>> GetPodMetricsAsync();
    }

    public class PrometheusService : IPrometheusService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PrometheusService> _logger;

        public PrometheusService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<PrometheusService> logger)
        {
            _httpClient = httpClientFactory.CreateClient("Prometheus");
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<PrometheusQueryResult> QueryAsync(string query)
        {
            try
            {
                var url = $"/api/v1/query?query={Uri.EscapeDataString(query)}";
                _logger.LogInformation("[PROMETHEUS] Executing query: {Query}", query);

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<PrometheusQueryResult>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                _logger.LogInformation("[PROMETHEUS] Query successful, returned {Count} results",
                    result?.Data?.Result?.Count ?? 0);

                return result ?? new PrometheusQueryResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PROMETHEUS] Query failed: {Query}", query);
                return new PrometheusQueryResult { Status = "error" };
            }
        }

        public async Task<PrometheusRangeQueryResult> QueryRangeAsync(string query, DateTime start, DateTime end, string step)
        {
            try
            {
                var startUnix = ((DateTimeOffset)start.ToUniversalTime()).ToUnixTimeSeconds();
                var endUnix = ((DateTimeOffset)end.ToUniversalTime()).ToUnixTimeSeconds();

                var url = $"/api/v1/query_range?query={Uri.EscapeDataString(query)}&start={startUnix}&end={endUnix}&step={step}";
                _logger.LogInformation("[PROMETHEUS] Executing range query: {Query} from {Start} to {End}", query, start, end);

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<PrometheusRangeQueryResult>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result ?? new PrometheusRangeQueryResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PROMETHEUS] Range query failed: {Query}", query);
                return new PrometheusRangeQueryResult { Status = "error" };
            }
        }

        public async Task<List<PrometheusMetricDto>> GetInfrastructureMetricsAsync()
        {
            var metrics = new List<PrometheusMetricDto>();

            try
            {
                // CPU Usage by pod
                var cpuQuery = await QueryAsync("sum(rate(container_cpu_usage_seconds_total{namespace=\"insightlearn\"}[5m])) by (pod)");
                if (cpuQuery.Status == "success" && cpuQuery.Data?.Result != null)
                {
                    foreach (var result in cpuQuery.Data.Result)
                    {
                        var podName = result.Metric?.GetValueOrDefault("pod", "unknown") ?? "unknown";
                        var value = ParseValue(result.Value);

                        metrics.Add(new PrometheusMetricDto
                        {
                            Name = "cpu_usage",
                            Label = $"CPU Usage - {podName}",
                            Value = Math.Round(value * 100, 2), // Convert to percentage
                            Unit = "percent",
                            Timestamp = DateTime.UtcNow,
                            Labels = result.Metric ?? new Dictionary<string, string>()
                        });
                    }
                }

                // Memory Usage by pod
                var memQuery = await QueryAsync("sum(container_memory_usage_bytes{namespace=\"insightlearn\"}) by (pod)");
                if (memQuery.Status == "success" && memQuery.Data?.Result != null)
                {
                    foreach (var result in memQuery.Data.Result)
                    {
                        var podName = result.Metric?.GetValueOrDefault("pod", "unknown") ?? "unknown";
                        var value = ParseValue(result.Value);

                        metrics.Add(new PrometheusMetricDto
                        {
                            Name = "memory_usage",
                            Label = $"Memory Usage - {podName}",
                            Value = value,
                            Unit = "bytes",
                            Timestamp = DateTime.UtcNow,
                            Labels = result.Metric ?? new Dictionary<string, string>()
                        });
                    }
                }

                // Pod Status
                var podStatusQuery = await QueryAsync("up{namespace=\"insightlearn\"}");
                if (podStatusQuery.Status == "success" && podStatusQuery.Data?.Result != null)
                {
                    var upCount = podStatusQuery.Data.Result.Count(r => ParseValue(r.Value) == 1);
                    var totalCount = podStatusQuery.Data.Result.Count;

                    metrics.Add(new PrometheusMetricDto
                    {
                        Name = "pods_healthy",
                        Label = "Healthy Pods",
                        Value = upCount,
                        Unit = "count",
                        Timestamp = DateTime.UtcNow,
                        Labels = new Dictionary<string, string> { ["total"] = totalCount.ToString() }
                    });
                }

                // Disk Usage (if available)
                var diskQuery = await QueryAsync("sum(container_fs_usage_bytes{namespace=\"insightlearn\"}) by (pod)");
                if (diskQuery.Status == "success" && diskQuery.Data?.Result != null)
                {
                    foreach (var result in diskQuery.Data.Result)
                    {
                        var podName = result.Metric?.GetValueOrDefault("pod", "unknown") ?? "unknown";
                        var value = ParseValue(result.Value);

                        metrics.Add(new PrometheusMetricDto
                        {
                            Name = "disk_usage",
                            Label = $"Disk Usage - {podName}",
                            Value = value,
                            Unit = "bytes",
                            Timestamp = DateTime.UtcNow,
                            Labels = result.Metric ?? new Dictionary<string, string>()
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PROMETHEUS] Error fetching infrastructure metrics");
            }

            return metrics;
        }

        public async Task<List<PrometheusMetricDto>> GetApiMetricsAsync()
        {
            var metrics = new List<PrometheusMetricDto>();

            try
            {
                // Request rate
                var requestRateQuery = await QueryAsync("sum(rate(http_requests_total{job=\"insightlearn-api\"}[5m]))");
                if (requestRateQuery.Status == "success" && requestRateQuery.Data?.Result?.Any() == true)
                {
                    var value = ParseValue(requestRateQuery.Data.Result.First().Value);
                    metrics.Add(new PrometheusMetricDto
                    {
                        Name = "request_rate",
                        Label = "Request Rate",
                        Value = Math.Round(value, 2),
                        Unit = "requests/s",
                        Timestamp = DateTime.UtcNow
                    });
                }

                // Response time percentiles
                var p50Query = await QueryAsync("histogram_quantile(0.5, rate(http_request_duration_seconds_bucket{job=\"insightlearn-api\"}[5m]))");
                if (p50Query.Status == "success" && p50Query.Data?.Result?.Any() == true)
                {
                    var value = ParseValue(p50Query.Data.Result.First().Value);
                    metrics.Add(new PrometheusMetricDto
                    {
                        Name = "response_time_p50",
                        Label = "Response Time (p50)",
                        Value = Math.Round(value * 1000, 2), // Convert to milliseconds
                        Unit = "milliseconds",
                        Timestamp = DateTime.UtcNow
                    });
                }

                var p95Query = await QueryAsync("histogram_quantile(0.95, rate(http_request_duration_seconds_bucket{job=\"insightlearn-api\"}[5m]))");
                if (p95Query.Status == "success" && p95Query.Data?.Result?.Any() == true)
                {
                    var value = ParseValue(p95Query.Data.Result.First().Value);
                    metrics.Add(new PrometheusMetricDto
                    {
                        Name = "response_time_p95",
                        Label = "Response Time (p95)",
                        Value = Math.Round(value * 1000, 2),
                        Unit = "milliseconds",
                        Timestamp = DateTime.UtcNow
                    });
                }

                var p99Query = await QueryAsync("histogram_quantile(0.99, rate(http_request_duration_seconds_bucket{job=\"insightlearn-api\"}[5m]))");
                if (p99Query.Status == "success" && p99Query.Data?.Result?.Any() == true)
                {
                    var value = ParseValue(p99Query.Data.Result.First().Value);
                    metrics.Add(new PrometheusMetricDto
                    {
                        Name = "response_time_p99",
                        Label = "Response Time (p99)",
                        Value = Math.Round(value * 1000, 2),
                        Unit = "milliseconds",
                        Timestamp = DateTime.UtcNow
                    });
                }

                // Error rate
                var errorRateQuery = await QueryAsync("sum(rate(http_requests_total{job=\"insightlearn-api\",status=~\"5..\"}[5m]))");
                if (errorRateQuery.Status == "success")
                {
                    var value = errorRateQuery.Data?.Result?.Any() == true
                        ? ParseValue(errorRateQuery.Data.Result.First().Value)
                        : 0;

                    metrics.Add(new PrometheusMetricDto
                    {
                        Name = "error_rate",
                        Label = "Error Rate (5xx)",
                        Value = Math.Round(value, 4),
                        Unit = "errors/s",
                        Timestamp = DateTime.UtcNow
                    });
                }

                // Active connections
                var connectionsQuery = await QueryAsync("http_connections_active{job=\"insightlearn-api\"}");
                if (connectionsQuery.Status == "success" && connectionsQuery.Data?.Result?.Any() == true)
                {
                    var value = ParseValue(connectionsQuery.Data.Result.First().Value);
                    metrics.Add(new PrometheusMetricDto
                    {
                        Name = "active_connections",
                        Label = "Active Connections",
                        Value = value,
                        Unit = "connections",
                        Timestamp = DateTime.UtcNow
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PROMETHEUS] Error fetching API metrics");
            }

            return metrics;
        }

        public async Task<Dictionary<string, double>> GetPodMetricsAsync()
        {
            var metrics = new Dictionary<string, double>();

            try
            {
                // Get pod CPU usage
                var cpuQuery = await QueryAsync("sum(rate(container_cpu_usage_seconds_total{namespace=\"insightlearn\"}[5m])) by (pod)");
                if (cpuQuery.Status == "success" && cpuQuery.Data?.Result != null)
                {
                    foreach (var result in cpuQuery.Data.Result)
                    {
                        var podName = result.Metric?.GetValueOrDefault("pod", "unknown") ?? "unknown";
                        var value = ParseValue(result.Value);
                        metrics[$"{podName}_cpu"] = Math.Round(value * 100, 2);
                    }
                }

                // Get pod memory usage
                var memQuery = await QueryAsync("sum(container_memory_usage_bytes{namespace=\"insightlearn\"}) by (pod) / 1024 / 1024");
                if (memQuery.Status == "success" && memQuery.Data?.Result != null)
                {
                    foreach (var result in memQuery.Data.Result)
                    {
                        var podName = result.Metric?.GetValueOrDefault("pod", "unknown") ?? "unknown";
                        var value = ParseValue(result.Value);
                        metrics[$"{podName}_memory_mb"] = Math.Round(value, 2);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PROMETHEUS] Error fetching pod metrics");
            }

            return metrics;
        }

        private double ParseValue(object[]? value)
        {
            if (value == null || value.Length < 2)
                return 0;

            if (double.TryParse(value[1]?.ToString(), out var result))
                return result;

            return 0;
        }
    }
}