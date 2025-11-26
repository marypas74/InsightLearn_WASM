using System;
using System.Collections.Generic;

namespace InsightLearn.WebAssembly.Models.Admin
{
    /// <summary>
    /// Analytics overview with KPIs and trend data for the Analytics admin page
    /// </summary>
    public class AnalyticsOverviewModel
    {
        // User Metrics
        public int TotalUsers { get; set; }
        public TrendDataModel UsersTrend { get; set; } = new();

        // Course Metrics
        public int TotalCourses { get; set; }
        public int PublishedCourses { get; set; }
        public int DraftCourses { get; set; }
        public TrendDataModel CoursesTrend { get; set; } = new();

        // Enrollment Metrics
        public int TotalEnrollments { get; set; }
        public int ActiveEnrollments { get; set; }
        public int CompletedEnrollments { get; set; }
        public TrendDataModel EnrollmentsTrend { get; set; } = new();

        // Revenue Metrics
        public decimal TotalRevenue { get; set; }
        public TrendDataModel RevenueTrend { get; set; } = new();

        // Metadata
        public DateTime GeneratedAt { get; set; }
        public string DateRange { get; set; } = string.Empty;
    }

    /// <summary>
    /// Trend data with percentage change and direction indicator
    /// </summary>
    public class TrendDataModel
    {
        public decimal PercentageChange { get; set; } // e.g., 12.5 for +12.5%
        public bool IsPositive { get; set; }
        public string Description { get; set; } = "vs previous period";
    }

    /// <summary>
    /// User growth data point for chart
    /// </summary>
    public class UserGrowthDataPoint
    {
        public DateTime Date { get; set; }
        public string Label { get; set; } = string.Empty; // e.g., "Nov 1"
        public int NewUsers { get; set; }
        public int TotalUsers { get; set; }
    }

    /// <summary>
    /// Revenue data point for chart
    /// </summary>
    public class RevenueDataPoint
    {
        public DateTime Date { get; set; }
        public string Label { get; set; } = string.Empty; // e.g., "Nov 2024"
        public decimal Revenue { get; set; }
        public int Transactions { get; set; }
    }

    /// <summary>
    /// Top performing course data
    /// </summary>
    public class TopCourseModel
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

    /// <summary>
    /// Category distribution data for pie/bar chart
    /// </summary>
    public class CategoryDistributionModel
    {
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int CourseCount { get; set; }
        public int EnrollmentCount { get; set; }
        public decimal Revenue { get; set; }
        public decimal Percentage { get; set; } // Percentage of total
        public string Color { get; set; } = string.Empty; // Hex color for chart
    }

    /// <summary>
    /// Date range options for analytics filtering
    /// </summary>
    public enum DateRangeOption
    {
        SevenDays,
        ThirtyDays,
        NinetyDays,
        OneYear,
        ThisMonth,
        Last3Months,
        Last6Months,
        ThisYear,
        Custom
    }

    /// <summary>
    /// Request model for analytics with date range
    /// </summary>
    public class AnalyticsRequest
    {
        public DateRangeOption RangeOption { get; set; } = DateRangeOption.ThirtyDays;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    /// <summary>
    /// Analytics overview response from API
    /// </summary>
    public class AnalyticsOverviewResponse
    {
        public int TotalUsers { get; set; }
        public int TotalCourses { get; set; }
        public int TotalEnrollments { get; set; }
        public decimal TotalRevenue { get; set; }
        public double UserGrowthPercent { get; set; }
        public double CourseGrowthPercent { get; set; }
        public double EnrollmentGrowthPercent { get; set; }
        public double RevenueGrowthPercent { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    /// <summary>
    /// User growth chart data response
    /// </summary>
    public class UserGrowthDataResponse
    {
        public List<ChartDataPointModel> Data { get; set; } = new();
        public int TotalNewUsers { get; set; }
        public double AverageDaily { get; set; }
    }

    /// <summary>
    /// Revenue trends chart data response
    /// </summary>
    public class RevenueTrendResponse
    {
        public List<ChartDataPointModel> Data { get; set; } = new();
        public decimal TotalPeriodRevenue { get; set; }
        public decimal AverageDaily { get; set; }
    }

    /// <summary>
    /// Enrollment trends chart data response
    /// </summary>
    public class EnrollmentTrendResponse
    {
        public List<ChartDataPointModel> Data { get; set; } = new();
        public int TotalPeriodEnrollments { get; set; }
        public double AverageDaily { get; set; }
    }

    /// <summary>
    /// Top courses response from API
    /// </summary>
    public class TopCoursesResponse
    {
        public List<TopCourseItem> Courses { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
    }

    /// <summary>
    /// Individual top course item
    /// </summary>
    public class TopCourseItem
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string InstructorName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int EnrollmentCount { get; set; }
        public decimal Revenue { get; set; }
        public double Rating { get; set; }
        public int ReviewCount { get; set; }
    }

    /// <summary>
    /// Generic chart data point for all chart types
    /// </summary>
    public class ChartDataPointModel
    {
        public string Label { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public DateTime? Date { get; set; }
    }
}
