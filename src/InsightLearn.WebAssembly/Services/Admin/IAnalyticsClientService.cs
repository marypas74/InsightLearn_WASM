using InsightLearn.Core.DTOs.Admin;
using InsightLearn.WebAssembly.Models.Admin;

namespace InsightLearn.WebAssembly.Services.Admin
{
    /// <summary>
    /// Client service for fetching analytics data from the backend API
    /// Provides methods for analytics overview, charts, and top courses
    /// </summary>
    public interface IAnalyticsClientService
    {
        /// <summary>
        /// Gets the analytics overview including KPIs and trend data
        /// </summary>
        /// <param name="days">Number of days for trend calculation (default: 30)</param>
        /// <returns>Analytics overview with user, course, enrollment, and revenue metrics</returns>
        Task<AnalyticsOverviewResponse?> GetOverviewAsync(int days = 30);

        /// <summary>
        /// Gets user growth data for charting
        /// </summary>
        /// <param name="days">Number of days of data to retrieve (default: 30)</param>
        /// <returns>User growth chart data with daily data points</returns>
        Task<ChartDataDto?> GetUserGrowthAsync(int days = 30);

        /// <summary>
        /// Gets revenue trend data for charting
        /// </summary>
        /// <param name="days">Number of days of data to retrieve (default: 30)</param>
        /// <returns>Revenue trend chart data with daily data points</returns>
        Task<ChartDataDto?> GetRevenueTrendsAsync(int days = 30);

        /// <summary>
        /// Gets top performing courses by enrollment count
        /// </summary>
        /// <param name="count">Number of top courses to retrieve (default: 10)</param>
        /// <returns>List of top courses with enrollment and revenue data</returns>
        Task<List<TopCourseItem>> GetTopCoursesAsync(int count = 10);

        /// <summary>
        /// Gets enrollment trend data for charting
        /// </summary>
        /// <param name="days">Number of days of data to retrieve (default: 30)</param>
        /// <returns>Enrollment trend chart data with daily data points</returns>
        Task<ChartDataDto?> GetEnrollmentTrendsAsync(int days = 30);

        /// <summary>
        /// Exports analytics data to CSV format
        /// </summary>
        /// <param name="days">Number of days of data to export</param>
        /// <returns>CSV content as a string</returns>
        Task<string?> ExportToCsvAsync(int days = 30);
    }
}
