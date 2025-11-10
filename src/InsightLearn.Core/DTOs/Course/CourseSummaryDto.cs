namespace InsightLearn.Core.DTOs.Course;

/// <summary>
/// Course statistics and summary information
/// </summary>
public class CourseSummaryDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;

    public int TotalSections { get; set; }
    public int TotalLessons { get; set; }
    public int TotalDurationMinutes { get; set; }

    public int EnrollmentCount { get; set; }
    public int ActiveEnrollments { get; set; }
    public int CompletedEnrollments { get; set; }

    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }

    public int ViewCount { get; set; }
    public decimal TotalRevenue { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
}
