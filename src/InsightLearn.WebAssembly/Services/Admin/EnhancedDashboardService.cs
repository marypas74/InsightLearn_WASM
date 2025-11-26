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
        private readonly IApiClient _apiClient;
        private readonly ILogger<EnhancedDashboardService> _logger;

        public EnhancedDashboardService(IApiClient apiClient, ILogger<EnhancedDashboardService> logger)
        {
            _apiClient = apiClient;
            _logger = logger;
        }

        public async Task<EnhancedDashboardStatsDto?> GetEnhancedStatsAsync()
        {
            try
            {
                _logger.LogInformation("Fetching enhanced dashboard stats");
                var response = await _apiClient.GetAsync<EnhancedDashboardStatsDto>("/api/admin/dashboard/enhanced-stats");

                if (response.Success && response.Data != null)
                {
                    _logger.LogInformation("Successfully fetched enhanced dashboard stats");
                    return response.Data;
                }
                else
                {
                    _logger.LogWarning("Failed to fetch enhanced stats: {Message}", response.Message);
                    return null;
                }
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
                var response = await _apiClient.GetAsync<ChartDataDto>($"/api/admin/dashboard/charts/{chartType}?days={days}");

                if (response.Success && response.Data != null)
                {
                    _logger.LogInformation($"Successfully fetched chart data for {chartType}");
                    return response.Data;
                }
                else
                {
                    _logger.LogWarning($"Failed to fetch chart data for {chartType}: {response.Message}");
                    return null;
                }
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
                var response = await _apiClient.GetAsync<PagedResult<ActivityItemDto>>($"/api/admin/dashboard/activity?limit={limit}&offset={offset}");

                if (response.Success && response.Data != null)
                {
                    _logger.LogInformation($"Successfully fetched {response.Data.Items?.Count ?? 0} activity items");
                    return response.Data;
                }
                else
                {
                    _logger.LogWarning("Failed to fetch activity: {Message}", response.Message);
                    return null;
                }
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
                var response = await _apiClient.GetAsync<RealTimeMetricsDto>("/api/admin/dashboard/realtime-metrics");

                if (response.Success && response.Data != null)
                {
                    _logger.LogInformation("Successfully fetched real-time metrics");
                    return response.Data;
                }
                else
                {
                    _logger.LogWarning("Failed to fetch real-time metrics: {Message}", response.Message);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching real-time metrics");
                return null;
            }
        }
    }
}