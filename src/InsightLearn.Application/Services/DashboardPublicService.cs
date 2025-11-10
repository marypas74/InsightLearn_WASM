using System;
using System.Threading.Tasks;
using InsightLearn.Core.DTOs.Admin;
using InsightLearn.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace InsightLearn.Application.Services
{
    /// <summary>
    /// Public-facing dashboard service that provides sanitized statistics
    /// Wraps EnhancedDashboardService with security filtering
    /// </summary>
    public class DashboardPublicService : IDashboardPublicService
    {
        private readonly IEnhancedDashboardService _enhancedDashboard;
        private readonly ILogger<DashboardPublicService> _logger;

        public DashboardPublicService(
            IEnhancedDashboardService enhancedDashboard,
            ILogger<DashboardPublicService> logger)
        {
            _enhancedDashboard = enhancedDashboard;
            _logger = logger;
        }

        /// <summary>
        /// Gets public statistics without sensitive data
        /// </summary>
        public async Task<object> GetPublicStatsAsync()
        {
            try
            {
                _logger.LogInformation("[DashboardPublic] Getting public stats");

                var fullStats = await _enhancedDashboard.GetEnhancedStatsAsync();

                // Return sanitized version - only public-facing metrics
                var publicStats = new
                {
                    // User metrics (aggregate only, no personal data)
                    TotalUsers = fullStats.TotalUsers,
                    TotalInstructors = fullStats.ActiveInstructors,

                    // Course metrics (published only)
                    TotalCourses = fullStats.PublishedCourses,
                    CoursesAvailable = fullStats.PublishedCourses,

                    // Platform health (generic status)
                    PlatformStatus = fullStats.PlatformStatus,
                    Uptime = fullStats.Uptime,

                    // Storage (general info only)
                    TotalVideos = fullStats.Storage?.TotalVideos ?? 0,

                    // Timestamp
                    LastUpdated = fullStats.LastUpdated
                };

                _logger.LogInformation("[DashboardPublic] Public stats returned successfully");
                return publicStats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DashboardPublic] Error getting public stats");
                return new
                {
                    Error = "Unable to retrieve statistics",
                    LastUpdated = DateTime.UtcNow
                };
            }
        }

        /// <summary>
        /// Gets full enhanced dashboard statistics (Admin only)
        /// </summary>
        public async Task<EnhancedDashboardStatsDto> GetAdminStatsAsync()
        {
            try
            {
                _logger.LogInformation("[DashboardPublic] Getting admin stats");
                return await _enhancedDashboard.GetEnhancedStatsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DashboardPublic] Error getting admin stats");
                return new EnhancedDashboardStatsDto
                {
                    LastUpdated = DateTime.UtcNow,
                    PlatformStatus = "error"
                };
            }
        }
    }
}
