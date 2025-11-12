using System.Threading.Tasks;
using InsightLearn.Core.DTOs.Admin;

namespace InsightLearn.WebAssembly.Services.Admin
{
    public interface IEnhancedDashboardService
    {
        Task<EnhancedDashboardStatsDto?> GetEnhancedStatsAsync();
        Task<ChartDataDto?> GetChartDataAsync(string chartType, int days = 30);
        Task<PagedResult<ActivityItemDto>?> GetRecentActivityAsync(int limit = 20, int offset = 0);
        Task<RealTimeMetricsDto?> GetRealTimeMetricsAsync();
    }
}