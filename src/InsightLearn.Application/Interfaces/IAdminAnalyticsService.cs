using InsightLearn.Core.DTOs.Admin;

namespace InsightLearn.Application.Interfaces;

/// <summary>
/// Interface for Admin Analytics Service operations
/// Provides comprehensive analytics data for the Admin Analytics page
/// </summary>
public interface IAdminAnalyticsService
{
    /// <summary>
    /// Get analytics overview with KPIs and trends
    /// </summary>
    /// <param name="range">Time range: "7days", "30days", "90days", "12months"</param>
    Task<AnalyticsOverviewDto> GetOverviewAsync(string range = "30days");

    /// <summary>
    /// Get user growth data for charts
    /// </summary>
    /// <param name="days">Number of days to include</param>
    Task<AnalyticsChartDataDto> GetUserGrowthAsync(int days = 30);

    /// <summary>
    /// Get monthly revenue trends for charts
    /// </summary>
    /// <param name="months">Number of months to include</param>
    Task<AnalyticsChartDataDto> GetRevenueTrendsAsync(int months = 12);

    /// <summary>
    /// Get top performing courses by enrollments
    /// </summary>
    /// <param name="limit">Number of courses to return</param>
    /// <param name="range">Time range filter: "all", "7days", "30days", "90days"</param>
    Task<TopCoursesResponseDto> GetTopCoursesAsync(int limit = 10, string range = "all");

    /// <summary>
    /// Get daily enrollment trends for charts
    /// </summary>
    /// <param name="days">Number of days to include</param>
    Task<AnalyticsChartDataDto> GetEnrollmentTrendsAsync(int days = 90);
}
