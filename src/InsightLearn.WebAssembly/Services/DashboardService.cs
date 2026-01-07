using InsightLearn.WebAssembly.Models;
using InsightLearn.WebAssembly.Models.Config;
using InsightLearn.WebAssembly.Services.Http;
using Microsoft.Extensions.Logging;

namespace InsightLearn.WebAssembly.Services;

public class DashboardService : IDashboardService
{
    private readonly IApiClient _apiClient;
    private readonly EndpointsConfig _endpoints;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(IApiClient apiClient, EndpointsConfig endpoints, ILogger<DashboardService> logger)
    {
        _apiClient = apiClient;
        _endpoints = endpoints;
        _logger = logger;
    }

    public async Task<ApiResponse<DashboardData>> GetStudentDashboardAsync()
    {
        _logger.LogDebug("Fetching student dashboard data");
        var response = await _apiClient.GetAsync<DashboardData>("api/dashboard/student");

        if (response.Success && response.Data != null)
        {
            _logger.LogInformation("Student dashboard data retrieved successfully");
        }
        else
        {
            _logger.LogWarning("Failed to retrieve student dashboard: {ErrorMessage}",
                response.Message ?? "Unknown error");
        }

        return response;
    }

    public async Task<ApiResponse<DashboardData>> GetInstructorDashboardAsync()
    {
        _logger.LogDebug("Fetching instructor dashboard data");
        var response = await _apiClient.GetAsync<DashboardData>("api/dashboard/instructor");

        if (response.Success && response.Data != null)
        {
            _logger.LogInformation("Instructor dashboard data retrieved successfully");
        }
        else
        {
            _logger.LogWarning("Failed to retrieve instructor dashboard: {ErrorMessage}",
                response.Message ?? "Unknown error");
        }

        return response;
    }

    public async Task<ApiResponse<DashboardData>> GetAdminDashboardAsync()
    {
        _logger.LogDebug("Fetching admin dashboard data");
        var response = await _apiClient.GetAsync<DashboardData>("api/dashboard/admin");

        if (response.Success && response.Data != null)
        {
            _logger.LogInformation("Admin dashboard data retrieved successfully");
        }
        else
        {
            _logger.LogWarning("Failed to retrieve admin dashboard: {ErrorMessage}",
                response.Message ?? "Unknown error");
        }

        return response;
    }
}
