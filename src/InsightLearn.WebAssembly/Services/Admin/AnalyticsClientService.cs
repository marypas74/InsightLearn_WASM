using System.Text;
using InsightLearn.Core.DTOs.Admin;
using InsightLearn.WebAssembly.Models.Admin;
using InsightLearn.WebAssembly.Services.Http;
using Microsoft.Extensions.Logging;

namespace InsightLearn.WebAssembly.Services.Admin
{
    /// <summary>
    /// Implementation of IAnalyticsClientService for fetching analytics data from backend API
    /// </summary>
    public class AnalyticsClientService : IAnalyticsClientService
    {
        private readonly IApiClient _apiClient;
        private readonly ILogger<AnalyticsClientService> _logger;

        public AnalyticsClientService(IApiClient apiClient, ILogger<AnalyticsClientService> logger)
        {
            _apiClient = apiClient;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<AnalyticsOverviewResponse?> GetOverviewAsync(int days = 30)
        {
            try
            {
                _logger.LogInformation("Fetching analytics overview for {Days} days", days);
                var response = await _apiClient.GetAsync<AnalyticsOverviewResponse>($"/api/admin/analytics/overview?days={days}");

                if (response.Success && response.Data != null)
                {
                    _logger.LogInformation("Successfully fetched analytics overview");
                    return response.Data;
                }
                else
                {
                    _logger.LogWarning("Failed to fetch analytics overview: {Message}", response.Message);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching analytics overview");
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<ChartDataDto?> GetUserGrowthAsync(int days = 30)
        {
            try
            {
                _logger.LogInformation("Fetching user growth data for {Days} days", days);
                var response = await _apiClient.GetAsync<ChartDataDto>($"/api/admin/analytics/user-growth?days={days}");

                if (response.Success && response.Data != null)
                {
                    _logger.LogInformation("Successfully fetched user growth data with {Count} data points",
                        response.Data.DataPoints?.Count ?? 0);
                    return response.Data;
                }
                else
                {
                    _logger.LogWarning("Failed to fetch user growth data: {Message}", response.Message);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user growth data");
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<ChartDataDto?> GetRevenueTrendsAsync(int days = 30)
        {
            try
            {
                _logger.LogInformation("Fetching revenue trends for {Days} days", days);
                var response = await _apiClient.GetAsync<ChartDataDto>($"/api/admin/analytics/revenue-trends?days={days}");

                if (response.Success && response.Data != null)
                {
                    _logger.LogInformation("Successfully fetched revenue trends with {Count} data points",
                        response.Data.DataPoints?.Count ?? 0);
                    return response.Data;
                }
                else
                {
                    _logger.LogWarning("Failed to fetch revenue trends: {Message}", response.Message);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching revenue trends");
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<List<TopCourseItem>> GetTopCoursesAsync(int count = 10)
        {
            try
            {
                _logger.LogInformation("Fetching top {Count} courses", count);
                var response = await _apiClient.GetAsync<TopCoursesResponse>($"/api/admin/analytics/top-courses?count={count}");

                if (response.Success && response.Data?.Courses != null)
                {
                    _logger.LogInformation("Successfully fetched {Count} top courses", response.Data.Courses.Count);
                    return response.Data.Courses;
                }
                else
                {
                    _logger.LogWarning("Failed to fetch top courses: {Message}", response.Message);
                    return new List<TopCourseItem>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching top courses");
                return new List<TopCourseItem>();
            }
        }

        /// <inheritdoc />
        public async Task<ChartDataDto?> GetEnrollmentTrendsAsync(int days = 30)
        {
            try
            {
                _logger.LogInformation("Fetching enrollment trends for {Days} days", days);
                var response = await _apiClient.GetAsync<ChartDataDto>($"/api/admin/analytics/enrollment-trends?days={days}");

                if (response.Success && response.Data != null)
                {
                    _logger.LogInformation("Successfully fetched enrollment trends with {Count} data points",
                        response.Data.DataPoints?.Count ?? 0);
                    return response.Data;
                }
                else
                {
                    _logger.LogWarning("Failed to fetch enrollment trends: {Message}", response.Message);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching enrollment trends");
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<string?> ExportToCsvAsync(int days = 30)
        {
            try
            {
                _logger.LogInformation("Exporting analytics data to CSV for {Days} days", days);

                // Fetch all data in parallel
                var overviewTask = GetOverviewAsync(days);
                var userGrowthTask = GetUserGrowthAsync(days);
                var revenueTask = GetRevenueTrendsAsync(days);
                var enrollmentTask = GetEnrollmentTrendsAsync(days);
                var topCoursesTask = GetTopCoursesAsync(10);

                await Task.WhenAll(overviewTask, userGrowthTask, revenueTask, enrollmentTask, topCoursesTask);

                var overview = await overviewTask;
                var userGrowth = await userGrowthTask;
                var revenue = await revenueTask;
                var enrollment = await enrollmentTask;
                var topCourses = await topCoursesTask;

                var csv = new StringBuilder();
                csv.AppendLine($"InsightLearn Analytics Report - Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                csv.AppendLine($"Period: Last {days} days");
                csv.AppendLine();

                // Overview Section
                csv.AppendLine("=== OVERVIEW ===");
                if (overview != null)
                {
                    csv.AppendLine($"Total Users,{overview.TotalUsers}");
                    csv.AppendLine($"User Growth %,{overview.UserGrowthPercent:F2}");
                    csv.AppendLine($"Total Courses,{overview.TotalCourses}");
                    csv.AppendLine($"Course Growth %,{overview.CourseGrowthPercent:F2}");
                    csv.AppendLine($"Total Enrollments,{overview.TotalEnrollments}");
                    csv.AppendLine($"Enrollment Growth %,{overview.EnrollmentGrowthPercent:F2}");
                    csv.AppendLine($"Total Revenue,{overview.TotalRevenue:C2}");
                    csv.AppendLine($"Revenue Growth %,{overview.RevenueGrowthPercent:F2}");
                }
                csv.AppendLine();

                // User Growth Section
                csv.AppendLine("=== USER GROWTH ===");
                csv.AppendLine("Date,New Users");
                if (userGrowth?.DataPoints != null)
                {
                    foreach (var dp in userGrowth.DataPoints)
                    {
                        csv.AppendLine($"{dp.Label},{dp.Value}");
                    }
                }
                csv.AppendLine();

                // Revenue Trends Section
                csv.AppendLine("=== REVENUE TRENDS ===");
                csv.AppendLine("Date,Revenue");
                if (revenue?.DataPoints != null)
                {
                    foreach (var dp in revenue.DataPoints)
                    {
                        csv.AppendLine($"{dp.Label},{dp.Value:F2}");
                    }
                }
                csv.AppendLine();

                // Enrollment Trends Section
                csv.AppendLine("=== ENROLLMENT TRENDS ===");
                csv.AppendLine("Date,Enrollments");
                if (enrollment?.DataPoints != null)
                {
                    foreach (var dp in enrollment.DataPoints)
                    {
                        csv.AppendLine($"{dp.Label},{dp.Value}");
                    }
                }
                csv.AppendLine();

                // Top Courses Section
                csv.AppendLine("=== TOP COURSES ===");
                csv.AppendLine("Rank,Title,Instructor,Category,Enrollments,Revenue,Rating");
                if (topCourses != null)
                {
                    int rank = 1;
                    foreach (var course in topCourses)
                    {
                        csv.AppendLine($"{rank},{EscapeCsv(course.Title)},{EscapeCsv(course.InstructorName)},{EscapeCsv(course.CategoryName)},{course.EnrollmentCount},{course.Revenue:F2},{course.Rating:F1}");
                        rank++;
                    }
                }

                _logger.LogInformation("Successfully generated CSV export");
                return csv.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting analytics to CSV");
                return null;
            }
        }

        /// <summary>
        /// Escapes a string value for safe CSV inclusion
        /// </summary>
        private static string EscapeCsv(string? value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }
            return value;
        }
    }
}
