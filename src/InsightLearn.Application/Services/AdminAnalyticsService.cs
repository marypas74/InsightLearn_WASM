using InsightLearn.Application.Interfaces;
using InsightLearn.Core.DTOs.Admin;
using InsightLearn.Core.Entities;
using InsightLearn.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InsightLearn.Application.Services;

/// <summary>
/// Service implementation for Analytics admin page backend operations
/// Provides comprehensive analytics data for user growth, revenue trends, top courses, and enrollments
/// </summary>
public class AdminAnalyticsService : IAdminAnalyticsService
{
    private readonly IDbContextFactory<InsightLearnDbContext> _contextFactory;
    private readonly ILogger<AdminAnalyticsService> _logger;

    public AdminAnalyticsService(
        IDbContextFactory<InsightLearnDbContext> contextFactory,
        ILogger<AdminAnalyticsService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    /// <summary>
    /// Get analytics overview with KPIs and trends
    /// </summary>
    public async Task<AnalyticsOverviewDto> GetOverviewAsync(string range = "30days")
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var days = ParseRangeToDays(range);
            var currentStartDate = DateTime.UtcNow.AddDays(-days);
            var previousStartDate = currentStartDate.AddDays(-days);

            _logger.LogInformation("[ANALYTICS] Getting overview for range: {Range} ({Days} days)", range, days);

            // Current period metrics
            var totalUsers = await context.Users.CountAsync();
            var totalCourses = await context.Courses.CountAsync();
            var totalEnrollments = await context.Enrollments.CountAsync();
            var totalRevenue = await context.Payments
                .Where(p => p.Status == PaymentStatus.Completed)
                .SumAsync(p => (decimal?)p.Amount) ?? 0;

            // Published and draft courses
            var publishedCourses = await context.Courses
                .CountAsync(c => c.Status == CourseStatus.Published);
            var draftCourses = await context.Courses
                .CountAsync(c => c.Status == CourseStatus.Draft);

            // Active and completed enrollments
            var activeEnrollments = await context.Enrollments
                .CountAsync(e => e.Status == EnrollmentStatus.Active);
            var completedEnrollments = await context.Enrollments
                .CountAsync(e => e.Status == EnrollmentStatus.Completed);

            // Previous period for comparison
            var previousUsers = await context.Users
                .CountAsync(u => u.DateJoined < previousStartDate);
            var previousRevenue = await context.Payments
                .Where(p => p.Status == PaymentStatus.Completed && p.CreatedAt < previousStartDate)
                .SumAsync(p => (decimal?)p.Amount) ?? 0;
            var previousEnrollments = await context.Enrollments
                .CountAsync(e => e.EnrolledAt < previousStartDate);
            var previousCourses = await context.Courses
                .CountAsync(c => c.CreatedAt < previousStartDate);

            // Calculate growth percentages
            var userGrowthPercent = CalculateGrowthPercentage(totalUsers, previousUsers);
            var courseGrowthPercent = CalculateGrowthPercentage(totalCourses, previousCourses);
            var enrollmentGrowthPercent = CalculateGrowthPercentage(totalEnrollments, previousEnrollments);
            var revenueGrowthPercent = CalculateGrowthPercentage((double)totalRevenue, (double)previousRevenue);

            var result = new AnalyticsOverviewDto
            {
                TotalUsers = totalUsers,
                TotalCourses = totalCourses,
                PublishedCourses = publishedCourses,
                DraftCourses = draftCourses,
                TotalEnrollments = totalEnrollments,
                ActiveEnrollments = activeEnrollments,
                CompletedEnrollments = completedEnrollments,
                TotalRevenue = totalRevenue,
                UsersTrend = new TrendDataDto
                {
                    PercentageChange = (decimal)userGrowthPercent,
                    IsPositive = userGrowthPercent >= 0
                },
                CoursesTrend = new TrendDataDto
                {
                    PercentageChange = (decimal)courseGrowthPercent,
                    IsPositive = courseGrowthPercent >= 0
                },
                EnrollmentsTrend = new TrendDataDto
                {
                    PercentageChange = (decimal)enrollmentGrowthPercent,
                    IsPositive = enrollmentGrowthPercent >= 0
                },
                RevenueTrend = new TrendDataDto
                {
                    PercentageChange = (decimal)revenueGrowthPercent,
                    IsPositive = revenueGrowthPercent >= 0
                },
                GeneratedAt = DateTime.UtcNow,
                DateRange = range
            };

            _logger.LogInformation("[ANALYTICS] Overview generated: {Users} users (+{UserGrowth}%), {Revenue} revenue (+{RevenueGrowth}%)",
                totalUsers, userGrowthPercent.ToString("F1"), totalRevenue, revenueGrowthPercent.ToString("F1"));

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ANALYTICS] Error getting analytics overview for range: {Range}", range);
            throw;
        }
    }

    /// <summary>
    /// Get user growth data for charts
    /// </summary>
    public async Task<AnalyticsChartDataDto> GetUserGrowthAsync(int days = 30)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var startDate = DateTime.UtcNow.AddDays(-days).Date;
            var endDate = DateTime.UtcNow.Date;

            _logger.LogInformation("[ANALYTICS] Getting user growth for {Days} days (from {StartDate} to {EndDate})",
                days, startDate, endDate);

            // Get all users registered in the period
            var users = await context.Users
                .Where(u => u.DateJoined >= startDate && u.DateJoined <= endDate.AddDays(1))
                .Select(u => new { u.DateJoined })
                .ToListAsync();

            // Group by day
            var dailyData = users
                .GroupBy(u => u.DateJoined.Date)
                .Select(g => new ChartDataPointDto
                {
                    Label = g.Key.ToString("MMM dd"),
                    Value = g.Count()
                })
                .OrderBy(d => d.Label)
                .ToList();

            // Fill missing days with zero
            var filledData = FillMissingDays(dailyData, startDate, endDate);

            return new AnalyticsChartDataDto
            {
                Title = $"User Growth (Last {days} Days)",
                DataPoints = filledData,
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ANALYTICS] Error getting user growth for {Days} days", days);
            throw;
        }
    }

    /// <summary>
    /// Get monthly revenue trends for charts
    /// </summary>
    public async Task<AnalyticsChartDataDto> GetRevenueTrendsAsync(int months = 12)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var startDate = DateTime.UtcNow.AddMonths(-months).Date;
            var endDate = DateTime.UtcNow.Date;

            _logger.LogInformation("[ANALYTICS] Getting revenue trends for {Months} months", months);

            // Get all completed payments in the period
            var payments = await context.Payments
                .Where(p => p.Status == PaymentStatus.Completed
                    && p.CreatedAt >= startDate
                    && p.CreatedAt <= endDate.AddDays(1))
                .Select(p => new { p.CreatedAt, p.Amount })
                .ToListAsync();

            // Group by month
            var monthlyData = payments
                .GroupBy(p => new DateTime(p.CreatedAt.Year, p.CreatedAt.Month, 1))
                .Select(g => new ChartDataPointDto
                {
                    Label = g.Key.ToString("MMM yyyy"),
                    Value = g.Sum(p => p.Amount)
                })
                .OrderBy(d => d.Label)
                .ToList();

            // Fill missing months with zero
            var filledData = FillMissingMonths(monthlyData, startDate, endDate);

            return new AnalyticsChartDataDto
            {
                Title = $"Revenue Trends (Last {months} Months)",
                DataPoints = filledData,
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ANALYTICS] Error getting revenue trends for {Months} months", months);
            throw;
        }
    }

    /// <summary>
    /// Get top performing courses by enrollments
    /// </summary>
    public async Task<TopCoursesResponseDto> GetTopCoursesAsync(int limit = 10, string range = "all")
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            _logger.LogInformation("[ANALYTICS] Getting top {Limit} courses for range: {Range}", limit, range);

            DateTime? startDate = null;
            if (range != "all")
            {
                var days = ParseRangeToDays(range);
                startDate = DateTime.UtcNow.AddDays(-days);
            }

            // Build query with optional date filter
            var query = context.Courses
                .Include(c => c.Category)
                .Include(c => c.Enrollments)
                .Include(c => c.Reviews)
                .Where(c => c.Status == CourseStatus.Published);

            var courses = await query.ToListAsync();

            // Get payments separately (Course doesn't have Payments navigation property)
            var courseIds = courses.Select(c => c.Id).ToList();
            var paymentsQuery = context.Payments
                .Where(p => courseIds.Contains(p.CourseId) && p.Status == PaymentStatus.Completed);

            if (startDate.HasValue)
            {
                paymentsQuery = paymentsQuery.Where(p => p.CreatedAt >= startDate);
            }

            var paymentsByCourse = await paymentsQuery
                .GroupBy(p => p.CourseId)
                .Select(g => new { CourseId = g.Key, Revenue = g.Sum(p => p.Amount) })
                .ToDictionaryAsync(x => x.CourseId, x => x.Revenue);

            // Calculate metrics and rank
            var topCourses = courses
                .Select(c => new TopCourseDto
                {
                    CourseId = c.Id,
                    CourseName = c.Title,
                    CategoryName = c.Category?.Name ?? "Uncategorized",
                    TotalEnrollments = startDate.HasValue
                        ? c.Enrollments.Count(e => e.EnrolledAt >= startDate)
                        : c.Enrollments.Count,
                    TotalRevenue = paymentsByCourse.TryGetValue(c.Id, out var revenue) ? revenue : 0m,
                    AverageRating = (decimal)(c.Reviews.Any() ? c.Reviews.Average(r => r.Rating) : 0),
                    ReviewCount = c.Reviews.Count
                })
                .OrderByDescending(c => c.TotalEnrollments)
                .ThenByDescending(c => c.TotalRevenue)
                .Take(limit)
                .Select((c, index) => new TopCourseDto
                {
                    Rank = index + 1,
                    CourseId = c.CourseId,
                    CourseName = c.CourseName,
                    CategoryName = c.CategoryName,
                    TotalEnrollments = c.TotalEnrollments,
                    TotalRevenue = c.TotalRevenue,
                    AverageRating = c.AverageRating,
                    ReviewCount = c.ReviewCount
                })
                .ToList();

            return new TopCoursesResponseDto
            {
                Courses = topCourses,
                TotalCount = topCourses.Count,
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ANALYTICS] Error getting top courses (limit: {Limit}, range: {Range})", limit, range);
            throw;
        }
    }

    /// <summary>
    /// Get daily enrollment trends for charts
    /// </summary>
    public async Task<AnalyticsChartDataDto> GetEnrollmentTrendsAsync(int days = 90)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var startDate = DateTime.UtcNow.AddDays(-days).Date;
            var endDate = DateTime.UtcNow.Date;

            _logger.LogInformation("[ANALYTICS] Getting enrollment trends for {Days} days", days);

            // Get all enrollments in the period
            var enrollments = await context.Enrollments
                .Where(e => e.EnrolledAt >= startDate && e.EnrolledAt <= endDate.AddDays(1))
                .Select(e => new { e.EnrolledAt })
                .ToListAsync();

            // Group by day
            var dailyData = enrollments
                .GroupBy(e => e.EnrolledAt.Date)
                .Select(g => new ChartDataPointDto
                {
                    Label = g.Key.ToString("MMM dd"),
                    Value = g.Count()
                })
                .OrderBy(d => d.Label)
                .ToList();

            // Fill missing days with zero
            var filledData = FillMissingDays(dailyData, startDate, endDate);

            return new AnalyticsChartDataDto
            {
                Title = $"Enrollment Trends (Last {days} Days)",
                DataPoints = filledData,
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ANALYTICS] Error getting enrollment trends for {Days} days", days);
            throw;
        }
    }

    #region Helper Methods

    /// <summary>
    /// Parse range string to number of days
    /// </summary>
    private int ParseRangeToDays(string range)
    {
        return range.ToLowerInvariant() switch
        {
            "7days" => 7,
            "30days" => 30,
            "90days" => 90,
            "12months" => 365,
            _ => 30 // Default
        };
    }

    /// <summary>
    /// Calculate growth percentage between current and previous values
    /// </summary>
    private double CalculateGrowthPercentage(double current, double previous)
    {
        if (previous == 0)
            return current > 0 ? 100.0 : 0.0;

        return ((current - previous) / previous) * 100.0;
    }

    private double CalculateGrowthPercentage(int current, int previous)
    {
        return CalculateGrowthPercentage((double)current, (double)previous);
    }

    /// <summary>
    /// Fill missing days with zero values for continuous chart data
    /// </summary>
    private List<ChartDataPointDto> FillMissingDays(List<ChartDataPointDto> data, DateTime startDate, DateTime endDate)
    {
        var result = new List<ChartDataPointDto>();
        var dataDict = data.ToDictionary(d => d.Label, d => d.Value);

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            var label = date.ToString("MMM dd");
            result.Add(new ChartDataPointDto
            {
                Label = label,
                Value = dataDict.ContainsKey(label) ? dataDict[label] : 0
            });
        }

        return result;
    }

    /// <summary>
    /// Fill missing months with zero values for continuous chart data
    /// </summary>
    private List<ChartDataPointDto> FillMissingMonths(List<ChartDataPointDto> data, DateTime startDate, DateTime endDate)
    {
        var result = new List<ChartDataPointDto>();
        var dataDict = data.ToDictionary(d => d.Label, d => d.Value);

        var current = new DateTime(startDate.Year, startDate.Month, 1);
        var end = new DateTime(endDate.Year, endDate.Month, 1);

        while (current <= end)
        {
            var label = current.ToString("MMM yyyy");
            result.Add(new ChartDataPointDto
            {
                Label = label,
                Value = dataDict.ContainsKey(label) ? dataDict[label] : 0
            });
            current = current.AddMonths(1);
        }

        return result;
    }

    #endregion
}
