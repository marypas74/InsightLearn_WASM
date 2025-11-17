namespace InsightLearn.Core.DTOs.Engagement;

/// <summary>
/// User engagement statistics summary
/// </summary>
public class UserEngagementStatsDto
{
    public Guid UserId { get; set; }

    public int TotalTimeSeconds { get; set; }

    public int CoursesEngaged { get; set; }

    public int LessonsCompleted { get; set; }

    public int QuizzesTaken { get; set; }

    public int AssignmentsSubmitted { get; set; }

    /// <summary>
    /// Breakdown of engagement time by type (video_watch: 3600s, quiz_attempt: 1200s, etc.)
    /// </summary>
    public Dictionary<string, int> EngagementBreakdown { get; set; } = new();

    public DateTime? FirstEngagement { get; set; }

    public DateTime? LastEngagement { get; set; }

    /// <summary>
    /// Average validation score (0.00-1.00) indicating engagement quality
    /// </summary>
    public decimal? AverageValidationScore { get; set; }
}
