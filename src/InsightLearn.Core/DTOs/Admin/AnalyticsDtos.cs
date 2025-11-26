using System;
using System.Collections.Generic;

namespace InsightLearn.Core.DTOs.Admin
{
    /// <summary>
    /// Analytics overview with KPIs and trend data for the Analytics admin page
    /// </summary>
    public class AnalyticsOverviewDto
    {
        // User Metrics
        public int TotalUsers { get; set; }
        public TrendDataDto UsersTrend { get; set; } = new();

        // Course Metrics
        public int TotalCourses { get; set; }
        public int PublishedCourses { get; set; }
        public int DraftCourses { get; set; }
        public TrendDataDto CoursesTrend { get; set; } = new();

        // Enrollment Metrics
        public int TotalEnrollments { get; set; }
        public int ActiveEnrollments { get; set; }
        public int CompletedEnrollments { get; set; }
        public TrendDataDto EnrollmentsTrend { get; set; } = new();

        // Revenue Metrics
        public decimal TotalRevenue { get; set; }
        public TrendDataDto RevenueTrend { get; set; } = new();

        // Metadata
        public DateTime GeneratedAt { get; set; }
        public string DateRange { get; set; } = string.Empty;
    }

    /// <summary>
    /// Trend data with percentage change and direction indicator
    /// </summary>
    public class TrendDataDto
    {
        public decimal PercentageChange { get; set; } // e.g., 12.5 for +12.5%
        public bool IsPositive { get; set; }
        public string Description { get; set; } = "vs previous period";
    }

    /// <summary>
    /// Chart data response for various analytics charts
    /// </summary>
    public class AnalyticsChartDataDto
    {
        public string Title { get; set; } = string.Empty;
        public List<ChartDataPointDto> DataPoints { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
    }

    /// <summary>
    /// Individual data point for charts
    /// </summary>
    public class ChartDataPointDto
    {
        public string Label { get; set; } = string.Empty; // "Nov 1", "Nov 2", etc.
        public decimal Value { get; set; } // Numeric value
    }

    /// <summary>
    /// Top performing courses by enrollments
    /// </summary>
    public class TopCoursesResponseDto
    {
        public List<TopCourseDto> Courses { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
        public int TotalCount { get; set; }
    }

    /// <summary>
    /// Individual top course data
    /// </summary>
    public class TopCourseDto
    {
        public int Rank { get; set; }
        public Guid CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int TotalEnrollments { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageRating { get; set; }
        public int ReviewCount { get; set; }
    }
}
