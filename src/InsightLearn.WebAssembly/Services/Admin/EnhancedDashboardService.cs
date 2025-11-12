using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using InsightLearn.Core.DTOs.Admin;
using InsightLearn.WebAssembly.Services.Http;
using Microsoft.Extensions.Logging;

namespace InsightLearn.WebAssembly.Services.Admin
{
    public class EnhancedDashboardService : IEnhancedDashboardService
    {
        private readonly AuthHttpClient _authHttpClient;
        private readonly ILogger<EnhancedDashboardService> _logger;

        public EnhancedDashboardService(AuthHttpClient authHttpClient, ILogger<EnhancedDashboardService> logger)
        {
            _authHttpClient = authHttpClient;
            _logger = logger;
        }

        public async Task<EnhancedDashboardStatsDto?> GetEnhancedStatsAsync()
        {
            try
            {
                _logger.LogInformation("Fetching enhanced dashboard stats");
                var stats = await _authHttpClient.GetAsync<EnhancedDashboardStatsDto>("/api/admin/dashboard/enhanced-stats");

                if (stats != null)
                {
                    _logger.LogInformation("Successfully fetched enhanced dashboard stats");
                }
                else
                {
                    _logger.LogWarning("Failed to fetch enhanced stats");
                }

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching enhanced dashboard stats");
                return null;
            }
        }

        public async Task<ChartDataDto?> GetChartDataAsync(string chartType, int days = 30)
        {
            try
            {
                _logger.LogInformation($"Fetching chart data for {chartType} (last {days} days)");
                var chartData = await _authHttpClient.GetAsync<ChartDataDto>($"/api/admin/dashboard/charts/{chartType}?days={days}");

                if (chartData != null)
                {
                    _logger.LogInformation($"Successfully fetched chart data for {chartType}");
                }
                else
                {
                    _logger.LogWarning($"Failed to fetch chart data for {chartType}");
                }

                return chartData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching chart data for {chartType}");
                return null;
            }
        }

        public async Task<PagedResult<ActivityItemDto>?> GetRecentActivityAsync(int limit = 20, int offset = 0)
        {
            try
            {
                _logger.LogInformation($"Fetching recent activity (limit: {limit}, offset: {offset})");
                var activities = await _authHttpClient.GetAsync<PagedResult<ActivityItemDto>>($"/api/admin/dashboard/activity?limit={limit}&offset={offset}");

                if (activities != null)
                {
                    _logger.LogInformation($"Successfully fetched {activities.Items?.Count ?? 0} activity items");
                }
                else
                {
                    _logger.LogWarning("Failed to fetch activity");
                }

                return activities;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching recent activity");
                return null;
            }
        }

        public async Task<RealTimeMetricsDto?> GetRealTimeMetricsAsync()
        {
            try
            {
                _logger.LogInformation("Fetching real-time metrics");
                var metrics = await _authHttpClient.GetAsync<RealTimeMetricsDto>("/api/admin/dashboard/realtime-metrics");

                if (metrics != null)
                {
                    _logger.LogInformation("Successfully fetched real-time metrics");
                }
                else
                {
                    _logger.LogWarning("Failed to fetch real-time metrics");
                }

                return metrics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching real-time metrics");
                return null;
            }
        }
    }
}