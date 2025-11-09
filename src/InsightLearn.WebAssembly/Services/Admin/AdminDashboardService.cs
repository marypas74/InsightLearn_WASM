using InsightLearn.WebAssembly.Models.Admin;
using InsightLearn.WebAssembly.Services.Http;

namespace InsightLearn.WebAssembly.Services.Admin;

public class AdminDashboardService : IAdminDashboardService
{
    private readonly IApiClient _apiClient;
    private readonly ILogger<AdminDashboardService> _logger;

    public AdminDashboardService(IApiClient apiClient, ILogger<AdminDashboardService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<DashboardStats?> GetStatsAsync()
    {
        try
        {
            _logger.LogInformation("Fetching dashboard stats");
            var response = await _apiClient.GetAsync<DashboardStats>("/api/admin/dashboard/stats");

            if (response.Success && response.Data != null)
            {
                _logger.LogInformation("Dashboard stats fetched successfully");
                return response.Data;
            }

            _logger.LogWarning("Failed to fetch dashboard stats: {Message}", response.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching dashboard stats");
            return null;
        }
    }

    public async Task<List<RecentActivity>?> GetRecentActivityAsync(int limit = 10)
    {
        try
        {
            _logger.LogInformation("Fetching recent activity (limit: {Limit})", limit);
            var response = await _apiClient.GetAsync<List<RecentActivity>>($"/api/admin/dashboard/recent-activity?limit={limit}");

            if (response.Success && response.Data != null)
            {
                _logger.LogInformation("Recent activity fetched successfully: {Count} items", response.Data.Count);
                return response.Data;
            }

            _logger.LogWarning("Failed to fetch recent activity: {Message}", response.Message);
            return new List<RecentActivity>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching recent activity");
            return new List<RecentActivity>();
        }
    }
}
