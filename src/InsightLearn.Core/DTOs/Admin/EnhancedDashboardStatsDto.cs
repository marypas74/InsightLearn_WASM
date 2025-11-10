using System;

namespace InsightLearn.Core.DTOs.Admin
{
    public class EnhancedDashboardStatsDto
    {
        // User Metrics
        public int TotalUsers { get; set; }
        public TrendData UsersTrend { get; set; } = new();
        public int ActiveUsersNow { get; set; }
        public int NewUsersToday { get; set; }
        public int NewUsersThisWeek { get; set; }
        public int NewUsersThisMonth { get; set; }

        // Instructor Metrics
        public int TotalInstructors { get; set; }
        public TrendData InstructorsTrend { get; set; } = new();
        public int PendingInstructorApplications { get; set; }
        public int ActiveInstructors { get; set; }

        // Course Metrics
        public int TotalCourses { get; set; }
        public TrendData CoursesTrend { get; set; } = new();
        public int PublishedCourses { get; set; }
        public int DraftCourses { get; set; }
        public int ArchivedCourses { get; set; }

        // Enrollment Metrics
        public int TotalEnrollments { get; set; }
        public TrendData EnrollmentsTrend { get; set; } = new();
        public int ActiveEnrollments { get; set; }
        public int CompletedEnrollments { get; set; }
        public int DroppedEnrollments { get; set; }

        // Revenue Metrics
        public decimal TotalRevenue { get; set; }
        public TrendData RevenueTrend { get; set; } = new();
        public decimal RevenueToday { get; set; }
        public decimal RevenueThisWeek { get; set; }
        public decimal RevenueThisMonth { get; set; }
        public decimal AverageOrderValue { get; set; }

        // Platform Health
        public string PlatformStatus { get; set; } = "healthy";
        public double AverageResponseTime { get; set; } // milliseconds
        public double Uptime { get; set; } // percentage
        public int ErrorsLast24Hours { get; set; }

        // Storage Metrics
        public StorageMetrics Storage { get; set; } = new();

        // Last Update
        public DateTime LastUpdated { get; set; }
    }

    public class TrendData
    {
        public decimal Value { get; set; }
        public decimal PreviousValue { get; set; }
        public decimal Change { get; set; }
        public double ChangePercentage { get; set; }
        public string Period { get; set; } = "week";
        public bool IsIncrease => Change > 0;
        public string Direction => IsIncrease ? "up" : Change < 0 ? "down" : "stable";
    }

    public class StorageMetrics
    {
        public long TotalUsedBytes { get; set; }
        public long TotalAvailableBytes { get; set; }
        public double UsagePercentage => TotalAvailableBytes > 0
            ? (double)TotalUsedBytes / TotalAvailableBytes * 100
            : 0;
        public long SqlServerUsedBytes { get; set; }
        public long MongoDbUsedBytes { get; set; }
        public int TotalVideos { get; set; }
        public int TotalDocuments { get; set; }

        public string TotalUsedFormatted => FormatBytes(TotalUsedBytes);
        public string TotalAvailableFormatted => FormatBytes(TotalAvailableBytes);

        private static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double value = bytes;
            while (value >= 1024 && order < sizes.Length - 1)
            {
                order++;
                value /= 1024;
            }
            return $"{value:0.##} {sizes[order]}";
        }
    }
}