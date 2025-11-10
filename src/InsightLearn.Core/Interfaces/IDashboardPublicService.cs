using System.Threading.Tasks;
using InsightLearn.Core.DTOs.Admin;

namespace InsightLearn.Core.Interfaces
{
    /// <summary>
    /// Public-facing service for dashboard statistics with security filtering
    /// </summary>
    public interface IDashboardPublicService
    {
        /// <summary>
        /// Gets public (sanitized) dashboard statistics
        /// Excludes sensitive financial and user data
        /// </summary>
        Task<object> GetPublicStatsAsync();

        /// <summary>
        /// Gets full enhanced dashboard statistics (Admin only)
        /// Includes all financial metrics and detailed data
        /// </summary>
        Task<EnhancedDashboardStatsDto> GetAdminStatsAsync();
    }
}
