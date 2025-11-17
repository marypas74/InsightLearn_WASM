namespace InsightLearn.Core.DTOs.Engagement;

/// <summary>
/// Instructor engagement statistics summary (across all instructor's courses)
/// </summary>
public class InstructorEngagementStatsDto
{
    public Guid InstructorId { get; set; }

    public string InstructorName { get; set; } = string.Empty;

    public int TotalTimeSeconds { get; set; }

    public int CoursesCount { get; set; }

    public int UniqueStudents { get; set; }

    /// <summary>
    /// Engagement breakdown by course (CourseId -> TotalSeconds)
    /// </summary>
    public Dictionary<Guid, int> EngagementByCourse { get; set; } = new();

    /// <summary>
    /// Engagement breakdown by type (video_watch, quiz_attempt, etc.)
    /// </summary>
    public Dictionary<string, int> EngagementByType { get; set; } = new();

    public DateTime? FirstEngagement { get; set; }

    public DateTime? LastEngagement { get; set; }

    /// <summary>
    /// Percentage of total platform engagement (for payout calculation)
    /// </summary>
    public decimal EngagementSharePercentage { get; set; }
}
