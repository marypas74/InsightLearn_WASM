using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Core.DTOs.Engagement;

/// <summary>
/// DTO for tracking video watch progress (current timestamp, total duration)
/// </summary>
public class TrackVideoProgressDto
{
    [Required(ErrorMessage = "Lesson ID is required")]
    public Guid LessonId { get; set; }

    [Required(ErrorMessage = "Current timestamp is required")]
    [Range(0, int.MaxValue, ErrorMessage = "Current timestamp must be non-negative")]
    public int CurrentTimestampSeconds { get; set; }

    [Required(ErrorMessage = "Total duration is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Total duration must be positive")]
    public int TotalDurationSeconds { get; set; }

    /// <summary>
    /// Optional session ID to update existing engagement record
    /// </summary>
    [StringLength(255)]
    public string? SessionId { get; set; }

    /// <summary>
    /// Playback speed (e.g., 1.0 = normal, 1.5 = 1.5x, 2.0 = 2x)
    /// </summary>
    [Range(0.1, 2.0, ErrorMessage = "Playback speed must be between 0.1 and 2.0")]
    public decimal? PlaybackSpeed { get; set; }

    /// <summary>
    /// Whether the browser tab was active during this tracking event
    /// </summary>
    public bool? TabActive { get; set; }
}
