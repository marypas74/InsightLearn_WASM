using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using InsightLearn.WebAssembly.Models.Admin;
using InsightLearn.WebAssembly.Services.Http;

namespace InsightLearn.WebAssembly.Services.Admin
{
    public interface IPrometheusMetricsService
    {
        Task<List<PrometheusMetricDto>> GetInfrastructureMetricsAsync();
        Task<List<PrometheusMetricDto>> GetApiMetricsAsync();
        Task<Dictionary<string, double>> GetPodMetricsAsync();
        Task<PrometheusQueryResult> QueryAsync(string query);
        Task<PrometheusRangeQueryResult> QueryRangeAsync(string query, DateTime start, DateTime end, string step = "15s");
    }

    public class PrometheusMetricsService : IPrometheusMetricsService
    {
        private readonly IApiClient _apiClient;
        private readonly ILogger<PrometheusMetricsService> _logger;

        public PrometheusMetricsService(IApiClient apiClient, ILogger<PrometheusMetricsService> logger)
        {
            _apiClient = apiClient;
            _logger = logger;
        }

        public async Task<List<PrometheusMetricDto>> GetInfrastructureMetricsAsync()
        {
            try
            {
                _logger.LogInformation("Fetching infrastructure metrics from Prometheus");
                var response = await _apiClient.GetAsync<List<PrometheusMetricDto>>("/api/admin/metrics/infrastructure");

                if (response.Success && response.Data != null)
                {
                    _logger.LogInformation("Retrieved {Count} infrastructure metrics", response.Data.Count);
                    return response.Data;
                }

                _logger.LogWarning("Failed to get infrastructure metrics");
                return new List<PrometheusMetricDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching infrastructure metrics");
                return new List<PrometheusMetricDto>();
            }
        }

        public async Task<List<PrometheusMetricDto>> GetApiMetricsAsync()
        {
            try
            {
                _logger.LogInformation("Fetching API metrics from Prometheus");
                var response = await _apiClient.GetAsync<List<PrometheusMetricDto>>("/api/admin/metrics/api");

                if (response.Success && response.Data != null)
                {
                    _logger.LogInformation("Retrieved {Count} API metrics", response.Data.Count);
                    return response.Data;
                }

                _logger.LogWarning("Failed to get API metrics: {Error}", "error");
                return new List<PrometheusMetricDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching API metrics");
                return new List<PrometheusMetricDto>();
            }
        }

        public async Task<Dictionary<string, double>> GetPodMetricsAsync()
        {
            try
            {
                _logger.LogInformation("Fetching pod metrics from Prometheus");
                var response = await _apiClient.GetAsync<Dictionary<string, double>>("/api/admin/metrics/pods");

                if (response.Success && response.Data != null)
                {
                    _logger.LogInformation("Retrieved metrics for {Count} pods", response.Data.Count);
                    return response.Data;
                }

                _logger.LogWarning("Failed to get pod metrics: {Error}", "error");
                return new Dictionary<string, double>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching pod metrics");
                return new Dictionary<string, double>();
            }
        }

        public async Task<PrometheusQueryResult> QueryAsync(string query)
        {
            try
            {
                _logger.LogInformation("Executing Prometheus query: {Query}", query);
                var request = new PrometheusQueryRequest
                {
                    Query = query,
                    IsRangeQuery = false
                };

                var response = await _apiClient.PostAsync<PrometheusQueryResult>("/api/admin/metrics/query", request);

                if (response.Success && response.Data != null)
                {
                    _logger.LogInformation("Query executed successfully");
                    return response.Data;
                }

                _logger.LogWarning("Query failed: {Error}", "error");
                return new PrometheusQueryResult { Status = "error" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing Prometheus query");
                return new PrometheusQueryResult { Status = "error" };
            }
        }

        public async Task<PrometheusRangeQueryResult> QueryRangeAsync(string query, DateTime start, DateTime end, string step = "15s")
        {
            try
            {
                _logger.LogInformation("Executing Prometheus range query: {Query} from {Start} to {End}", query, start, end);
                var request = new PrometheusQueryRequest
                {
                    Query = query,
                    IsRangeQuery = true,
                    Start = start,
                    End = end,
                    Step = step
                };

                var response = await _apiClient.PostAsync<PrometheusRangeQueryResult>("/api/admin/metrics/query", request);

                if (response.Success && response.Data != null)
                {
                    _logger.LogInformation("Range query executed successfully");
                    return response.Data;
                }

                _logger.LogWarning("Range query failed: {Error}", "error");
                return new PrometheusRangeQueryResult { Status = "error" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing Prometheus range query");
                return new PrometheusRangeQueryResult { Status = "error" };
            }
        }
    }
}