using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Core.DTOs.Engagement;

/// <summary>
/// DTO for tracking engagement events (video watch, quiz, assignment, etc.)
/// </summary>
public class TrackEngagementDto
{
    [Required(ErrorMessage = "Lesson ID is required")]
    public Guid LessonId { get; set; }

    [Required(ErrorMessage = "Engagement type is required")]
    [StringLength(50, ErrorMessage = "Engagement type must be 50 characters or less")]
    [RegularExpression(@"^(video_watch|quiz_attempt|assignment_submit|reading|discussion_post)$",
        ErrorMessage = "Engagement type must be: video_watch, quiz_attempt, assignment_submit, reading, or discussion_post")]
    public string EngagementType { get; set; } = string.Empty;

    [Required(ErrorMessage = "Time spent is required")]
    [Range(1, 86400, ErrorMessage = "Time spent must be between 1 second and 24 hours (86400 seconds)")]
    public int TimeSpentSeconds { get; set; }

    /// <summary>
    /// Optional session ID for tracking continuous learning sessions
    /// </summary>
    [StringLength(255)]
    public string? SessionId { get; set; }

    /// <summary>
    /// Optional metadata (JSON): video progress, playback speed, tab visibility, etc.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}
