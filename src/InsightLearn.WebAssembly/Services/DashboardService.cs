using InsightLearn.WebAssembly.Models;
using InsightLearn.WebAssembly.Models.Config;
using InsightLearn.WebAssembly.Services.Http;

namespace InsightLearn.WebAssembly.Services;

public class DashboardService : IDashboardService
{
    private readonly IApiClient _apiClient;
    private readonly EndpointsConfig _endpoints;

    public DashboardService(IApiClient apiClient, EndpointsConfig endpoints)
    {
        _apiClient = apiClient;
        _endpoints = endpoints;
    }

    public async Task<ApiResponse<DashboardData>> GetStudentDashboardAsync()
    {
        return await _apiClient.GetAsync<DashboardData>("api/dashboard/student");
    }

    public async Task<ApiResponse<DashboardData>> GetInstructorDashboardAsync()
    {
        return await _apiClient.GetAsync<DashboardData>("api/dashboard/instructor");
    }

    public async Task<ApiResponse<DashboardData>> GetAdminDashboardAsync()
    {
        return await _apiClient.GetAsync<DashboardData>("api/dashboard/admin");
    }
}
