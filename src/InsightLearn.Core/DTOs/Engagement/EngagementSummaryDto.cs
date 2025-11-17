namespace InsightLearn.Core.DTOs.Engagement;

/// <summary>
/// Platform-wide engagement summary for a specific period
/// </summary>
public class EngagementSummaryDto
{
    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public long TotalEngagementTimeSeconds { get; set; }

    public int UniqueUsers { get; set; }

    public int CoursesEngaged { get; set; }

    public decimal AverageEngagementPerUser { get; set; }

    /// <summary>
    /// Engagement breakdown by type
    /// </summary>
    public Dictionary<string, long> EngagementByType { get; set; } = new();

    /// <summary>
    /// Top 10 instructors by engagement time
    /// </summary>
    public List<InstructorEngagementSummary> TopInstructors { get; set; } = new();

    /// <summary>
    /// Top 10 courses by engagement time
    /// </summary>
    public List<CourseEngagementSummary> TopCourses { get; set; } = new();
}

/// <summary>
/// Simplified instructor engagement summary for dashboard
/// </summary>
public class InstructorEngagementSummary
{
    public Guid InstructorId { get; set; }
    public string InstructorName { get; set; } = string.Empty;
    public long TotalTimeSeconds { get; set; }
    public decimal SharePercentage { get; set; }
}

/// <summary>
/// Simplified course engagement summary for dashboard
/// </summary>
public class CourseEngagementSummary
{
    public Guid CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public long TotalTimeSeconds { get; set; }
    public int UniqueUsers { get; set; }
}
