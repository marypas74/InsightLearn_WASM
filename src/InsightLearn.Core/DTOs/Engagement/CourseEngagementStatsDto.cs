namespace InsightLearn.Core.DTOs.Engagement;

/// <summary>
/// Course engagement statistics summary
/// </summary>
public class CourseEngagementStatsDto
{
    public Guid CourseId { get; set; }

    public string CourseName { get; set; } = string.Empty;

    public int TotalTimeSeconds { get; set; }

    public int UniqueUsers { get; set; }

    public int TotalEngagements { get; set; }

    public decimal AverageTimePerUser { get; set; }

    public DateTime? FirstEngagement { get; set; }

    public DateTime? LastEngagement { get; set; }

    /// <summary>
    /// Engagement breakdown by type
    /// </summary>
    public Dictionary<string, int> EngagementByType { get; set; } = new();
}
