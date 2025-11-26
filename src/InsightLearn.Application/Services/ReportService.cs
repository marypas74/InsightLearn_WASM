using System.Text;
using InsightLearn.Application.Interfaces;
using InsightLearn.Core.Entities;
using InsightLearn.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InsightLearn.Application.Services;

/// <summary>
/// Service for generating platform reports from real database data.
/// Part of Admin Console v2.1.0-dev.
/// </summary>
public class ReportService : IReportService
{
    private readonly InsightLearnDbContext _context;
    private readonly ILogger<ReportService> _logger;

    public ReportService(InsightLearnDbContext context, ILogger<ReportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ReportResult> GenerateRevenueReportAsync(DateTime startDate, DateTime endDate)
    {
        _logger.LogInformation("Generating revenue report from {Start} to {End}", startDate, endDate);

        var payments = await _context.Payments
            .Where(p => p.CreatedAt >= startDate && p.CreatedAt <= endDate)
            .ToListAsync();

        var totalRevenue = payments.Where(p => p.Status == PaymentStatus.Completed).Sum(p => p.Amount);
        var totalTransactions = payments.Count(p => p.Status == PaymentStatus.Completed);
        var totalRefunds = payments.Where(p => p.Status == PaymentStatus.Refunded).Sum(p => p.Amount);
        var avgOrderValue = totalTransactions > 0 ? totalRevenue / totalTransactions : 0;

        // Calculate previous period for comparison
        var periodDays = (endDate - startDate).TotalDays;
        var prevStart = startDate.AddDays(-periodDays);
        var prevEnd = startDate.AddDays(-1);

        var prevPayments = await _context.Payments
            .Where(p => p.CreatedAt >= prevStart && p.CreatedAt <= prevEnd && p.Status == PaymentStatus.Completed)
            .ToListAsync();

        var prevRevenue = prevPayments.Sum(p => p.Amount);
        var revenueChange = prevRevenue > 0 ? ((totalRevenue - prevRevenue) / prevRevenue) * 100 : 0;

        var report = new ReportResult
        {
            Type = "revenue",
            Title = $"Revenue Report - {startDate:MMM dd} to {endDate:MMM dd, yyyy}",
            StartDate = startDate,
            EndDate = endDate,
            KeyMetrics = new List<ReportMetric>
            {
                new() { Label = "Total Revenue", Value = $"${totalRevenue:N2}", Icon = "fa-dollar-sign", Color = "#22c55e", Change = $"{revenueChange:+0.#;-0.#}%", IsPositive = revenueChange >= 0 },
                new() { Label = "Transactions", Value = totalTransactions.ToString("N0"), Icon = "fa-receipt", Color = "#3b82f6" },
                new() { Label = "Avg. Order Value", Value = $"${avgOrderValue:N2}", Icon = "fa-calculator", Color = "#8b5cf6" },
                new() { Label = "Refunds", Value = $"${totalRefunds:N2}", Icon = "fa-undo", Color = "#ef4444" }
            },
            Columns = new List<string> { "Date", "Transactions", "Revenue", "Refunds", "Net Revenue" }
        };

        // Group by date for rows
        var dailyData = payments
            .GroupBy(p => p.CreatedAt.Date)
            .OrderByDescending(g => g.Key)
            .Take(10)
            .Select(g => new List<string>
            {
                g.Key.ToString("MMM dd"),
                g.Count(p => p.Status == PaymentStatus.Completed).ToString(),
                $"${g.Where(p => p.Status == PaymentStatus.Completed).Sum(p => p.Amount):N2}",
                $"${g.Where(p => p.Status == PaymentStatus.Refunded).Sum(p => p.Amount):N2}",
                $"${g.Where(p => p.Status == PaymentStatus.Completed).Sum(p => p.Amount) - g.Where(p => p.Status == PaymentStatus.Refunded).Sum(p => p.Amount):N2}"
            })
            .ToList();

        report.Rows = dailyData;

        return report;
    }

    public async Task<ReportResult> GenerateUserGrowthReportAsync(DateTime startDate, DateTime endDate)
    {
        _logger.LogInformation("Generating user growth report from {Start} to {End}", startDate, endDate);

        var users = await _context.Users
            .Where(u => u.DateJoined >= startDate && u.DateJoined <= endDate)
            .ToListAsync();

        var totalVerifiedUsers = await _context.Users.CountAsync(u => u.IsVerified);
        var newUsers = users.Count;

        // Calculate previous period
        var periodDays = (endDate - startDate).TotalDays;
        var prevStart = startDate.AddDays(-periodDays);
        var prevEnd = startDate.AddDays(-1);
        var prevNewUsers = await _context.Users.CountAsync(u => u.DateJoined >= prevStart && u.DateJoined <= prevEnd);
        var userChange = prevNewUsers > 0 ? ((double)(newUsers - prevNewUsers) / prevNewUsers) * 100 : 0;

        var report = new ReportResult
        {
            Type = "users",
            Title = $"User Growth Report - {startDate:MMM dd} to {endDate:MMM dd, yyyy}",
            StartDate = startDate,
            EndDate = endDate,
            KeyMetrics = new List<ReportMetric>
            {
                new() { Label = "New Users", Value = newUsers.ToString("N0"), Icon = "fa-user-plus", Color = "#3b82f6", Change = $"{userChange:+0.#;-0.#}%", IsPositive = userChange >= 0 },
                new() { Label = "Verified Users", Value = totalVerifiedUsers.ToString("N0"), Icon = "fa-users", Color = "#22c55e" },
                new() { Label = "Students", Value = users.Count(u => u.UserType == "Student").ToString("N0"), Icon = "fa-user-graduate", Color = "#8b5cf6" },
                new() { Label = "Instructors", Value = users.Count(u => u.IsInstructor).ToString("N0"), Icon = "fa-chalkboard-teacher", Color = "#06b6d4" }
            },
            Columns = new List<string> { "Week", "New Signups", "Students", "Instructors", "Admins" }
        };

        // Group by week
        var weeklyData = users
            .GroupBy(u => GetWeekNumber(u.DateJoined))
            .OrderByDescending(g => g.Key)
            .Take(8)
            .Select(g => new List<string>
            {
                $"Week {g.Key}",
                g.Count().ToString(),
                g.Count(u => u.UserType == "Student").ToString(),
                g.Count(u => u.IsInstructor).ToString(),
                g.Count(u => u.UserType == "Admin").ToString()
            })
            .ToList();

        report.Rows = weeklyData;

        return report;
    }

    public async Task<ReportResult> GenerateCoursePerformanceReportAsync(DateTime startDate, DateTime endDate)
    {
        _logger.LogInformation("Generating course performance report from {Start} to {End}", startDate, endDate);

        var courses = await _context.Courses
            .Include(c => c.Reviews)
            .Include(c => c.Enrollments)
            .Where(c => c.Status == CourseStatus.Published || c.IsActive)
            .ToListAsync();

        var totalCourses = courses.Count;
        var coursesWithReviews = courses.Where(c => c.Reviews.Any()).ToList();
        var avgRating = coursesWithReviews.Any()
            ? coursesWithReviews.Average(c => c.Reviews.Average(r => r.Rating))
            : 0;
        var totalReviews = courses.Sum(c => c.Reviews.Count);

        var report = new ReportResult
        {
            Type = "courses",
            Title = $"Course Performance - {startDate:MMM dd} to {endDate:MMM dd, yyyy}",
            StartDate = startDate,
            EndDate = endDate,
            KeyMetrics = new List<ReportMetric>
            {
                new() { Label = "Active Courses", Value = totalCourses.ToString("N0"), Icon = "fa-book", Color = "#8b5cf6" },
                new() { Label = "Avg. Rating", Value = avgRating.ToString("F1"), Icon = "fa-star", Color = "#f59e0b" },
                new() { Label = "Total Reviews", Value = totalReviews.ToString("N0"), Icon = "fa-comment", Color = "#3b82f6" },
                new() { Label = "Total Enrollments", Value = courses.Sum(c => c.Enrollments.Count).ToString("N0"), Icon = "fa-user-plus", Color = "#22c55e" }
            },
            Columns = new List<string> { "Course", "Enrollments", "Rating", "Reviews", "Revenue" }
        };

        // Top courses by enrollment
        var topCourses = courses
            .OrderByDescending(c => c.Enrollments.Count)
            .Take(10)
            .Select(c => new List<string>
            {
                c.Title.Length > 40 ? c.Title.Substring(0, 37) + "..." : c.Title,
                c.Enrollments.Count.ToString(),
                c.Reviews.Any() ? c.Reviews.Average(r => r.Rating).ToString("F1") : "N/A",
                c.Reviews.Count.ToString(),
                $"${c.Enrollments.Count * c.Price:N2}"
            })
            .ToList();

        report.Rows = topCourses;

        return report;
    }

    public async Task<ReportResult> GenerateEnrollmentReportAsync(DateTime startDate, DateTime endDate)
    {
        _logger.LogInformation("Generating enrollment report from {Start} to {End}", startDate, endDate);

        var enrollments = await _context.Enrollments
            .Include(e => e.Course)
            .Where(e => e.EnrolledAt >= startDate && e.EnrolledAt <= endDate)
            .ToListAsync();

        var totalEnrollments = enrollments.Count;
        var uniqueStudents = enrollments.Select(e => e.UserId).Distinct().Count();
        var freeEnrollments = enrollments.Count(e => e.Course?.IsFree == true);
        var paidEnrollments = totalEnrollments - freeEnrollments;

        var report = new ReportResult
        {
            Type = "enrollments",
            Title = $"Enrollment Report - {startDate:MMM dd} to {endDate:MMM dd, yyyy}",
            StartDate = startDate,
            EndDate = endDate,
            KeyMetrics = new List<ReportMetric>
            {
                new() { Label = "New Enrollments", Value = totalEnrollments.ToString("N0"), Icon = "fa-user-plus", Color = "#f59e0b" },
                new() { Label = "Unique Students", Value = uniqueStudents.ToString("N0"), Icon = "fa-user-graduate", Color = "#3b82f6" },
                new() { Label = "Free Enrollments", Value = freeEnrollments.ToString("N0"), Icon = "fa-gift", Color = "#22c55e" },
                new() { Label = "Paid Enrollments", Value = paidEnrollments.ToString("N0"), Icon = "fa-credit-card", Color = "#8b5cf6" }
            },
            Columns = new List<string> { "Date", "Enrollments", "Free", "Paid", "Unique Students" }
        };

        // Group by date
        var dailyData = enrollments
            .GroupBy(e => e.EnrolledAt.Date)
            .OrderByDescending(g => g.Key)
            .Take(10)
            .Select(g => new List<string>
            {
                g.Key.ToString("MMM dd"),
                g.Count().ToString(),
                g.Count(e => e.Course?.IsFree == true).ToString(),
                g.Count(e => e.Course?.IsFree != true).ToString(),
                g.Select(e => e.UserId).Distinct().Count().ToString()
            })
            .ToList();

        report.Rows = dailyData;

        return report;
    }

    public async Task<ReportResult> GenerateEngagementReportAsync(DateTime startDate, DateTime endDate)
    {
        _logger.LogInformation("Generating engagement report from {Start} to {End}", startDate, endDate);

        // Get engagement data from CourseEngagement table
        var engagements = await _context.CourseEngagements
            .Where(e => e.CreatedAt >= startDate && e.CreatedAt <= endDate)
            .ToListAsync();

        var totalWatchTime = engagements.Sum(e => e.DurationMinutes);
        var totalSessions = engagements.Count;
        var avgSessionTime = totalSessions > 0 ? totalWatchTime / totalSessions : 0;

        var report = new ReportResult
        {
            Type = "engagement",
            Title = $"User Engagement - {startDate:MMM dd} to {endDate:MMM dd, yyyy}",
            StartDate = startDate,
            EndDate = endDate,
            KeyMetrics = new List<ReportMetric>
            {
                new() { Label = "Watch Time", Value = $"{totalWatchTime / 60:N0}h", Icon = "fa-clock", Color = "#ec4899" },
                new() { Label = "Sessions", Value = totalSessions.ToString("N0"), Icon = "fa-play-circle", Color = "#3b82f6" },
                new() { Label = "Avg. Session", Value = $"{avgSessionTime}min", Icon = "fa-hourglass-half", Color = "#8b5cf6" },
                new() { Label = "Active Users", Value = engagements.Select(e => e.UserId).Distinct().Count().ToString("N0"), Icon = "fa-users", Color = "#22c55e" }
            },
            Columns = new List<string> { "Date", "Sessions", "Watch Time", "Unique Users", "Avg. Session" }
        };

        // Group by date
        var dailyData = engagements
            .GroupBy(e => e.CreatedAt.Date)
            .OrderByDescending(g => g.Key)
            .Take(10)
            .Select(g => new List<string>
            {
                g.Key.ToString("MMM dd"),
                g.Count().ToString(),
                $"{g.Sum(e => e.DurationMinutes) / 60:N0}h",
                g.Select(e => e.UserId).Distinct().Count().ToString(),
                $"{(g.Any() ? g.Average(e => e.DurationMinutes) : 0):N0}min"
            })
            .ToList();

        report.Rows = dailyData;

        return report;
    }

    public async Task<ReportResult> GenerateInstructorEarningsReportAsync(DateTime startDate, DateTime endDate)
    {
        _logger.LogInformation("Generating instructor earnings report from {Start} to {End}", startDate, endDate);

        var instructors = await _context.Users
            .Include(u => u.CreatedCourses)
                .ThenInclude(c => c.Enrollments)
            .Where(u => u.IsInstructor)
            .ToListAsync();

        var activeInstructors = instructors.Count(i => i.CreatedCourses.Any());
        var totalRevenue = instructors.Sum(i => i.CreatedCourses.Sum(c => c.Enrollments.Count * c.Price));
        var totalPayout = totalRevenue * 0.8m; // 80% to instructors

        var report = new ReportResult
        {
            Type = "instructors",
            Title = $"Instructor Earnings - {startDate:MMM dd} to {endDate:MMM dd, yyyy}",
            StartDate = startDate,
            EndDate = endDate,
            KeyMetrics = new List<ReportMetric>
            {
                new() { Label = "Total Payouts", Value = $"${totalPayout:N2}", Icon = "fa-money-bill", Color = "#22c55e" },
                new() { Label = "Active Instructors", Value = activeInstructors.ToString("N0"), Icon = "fa-chalkboard-teacher", Color = "#06b6d4" },
                new() { Label = "Avg. Earnings", Value = activeInstructors > 0 ? $"${totalPayout / activeInstructors:N2}" : "$0", Icon = "fa-calculator", Color = "#8b5cf6" },
                new() { Label = "Total Instructors", Value = instructors.Count.ToString("N0"), Icon = "fa-users", Color = "#3b82f6" }
            },
            Columns = new List<string> { "Instructor", "Courses", "Students", "Revenue", "Payout (80%)" }
        };

        // Top instructors by revenue
        var topInstructors = instructors
            .OrderByDescending(i => i.CreatedCourses.Sum(c => c.Enrollments.Count * c.Price))
            .Take(10)
            .Select(i =>
            {
                var revenue = i.CreatedCourses.Sum(c => c.Enrollments.Count * c.Price);
                return new List<string>
                {
                    $"{i.FirstName} {i.LastName}".Trim(),
                    i.CreatedCourses.Count.ToString(),
                    i.CreatedCourses.Sum(c => c.Enrollments.Count).ToString(),
                    $"${revenue:N2}",
                    $"${revenue * 0.8m:N2}"
                };
            })
            .ToList();

        report.Rows = topInstructors;

        return report;
    }

    public Task<byte[]> ExportToCsvAsync(ReportResult report)
    {
        var csv = new StringBuilder();

        // Header
        csv.AppendLine(string.Join(",", report.Columns));

        // Data rows
        foreach (var row in report.Rows)
        {
            csv.AppendLine(string.Join(",", row.Select(c => $"\"{c.Replace("\"", "\"\"")}\"")));
        }

        return Task.FromResult(Encoding.UTF8.GetBytes(csv.ToString()));
    }

    public Task<byte[]> ExportToPdfAsync(ReportResult report)
    {
        // PDF generation requires a library like iTextSharp or QuestPDF
        // For now, return a placeholder
        _logger.LogWarning("PDF export not yet implemented - returning CSV format");
        return ExportToCsvAsync(report);
    }

    public Task<byte[]> ExportToExcelAsync(ReportResult report)
    {
        // Excel generation requires a library like EPPlus or ClosedXML
        // For now, return CSV format which Excel can open
        _logger.LogWarning("Excel export not yet implemented - returning CSV format");
        return ExportToCsvAsync(report);
    }

    private static int GetWeekNumber(DateTime date)
    {
        var cal = System.Globalization.CultureInfo.CurrentCulture.Calendar;
        return cal.GetWeekOfYear(date, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
    }
}
