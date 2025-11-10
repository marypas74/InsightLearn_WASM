namespace InsightLearn.Core.DTOs.Enrollment;

/// <summary>
/// Individual lesson progress data
/// </summary>
public class LessonProgressDto
{
    public Guid Id { get; set; }
    public Guid LessonId { get; set; }
    public string LessonTitle { get; set; } = string.Empty;
    public Guid UserId { get; set; }

    public bool IsCompleted { get; set; }
    public int WatchedMinutes { get; set; }
    public int LessonDurationMinutes { get; set; }
    public double ProgressPercentage { get; set; }

    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
