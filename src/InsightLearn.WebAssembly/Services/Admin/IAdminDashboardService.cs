using InsightLearn.WebAssembly.Models.Admin;

namespace InsightLearn.WebAssembly.Services.Admin;

public interface IAdminDashboardService
{
    Task<DashboardStats?> GetStatsAsync();
    Task<List<RecentActivity>?> GetRecentActivityAsync(int limit = 10);
}
