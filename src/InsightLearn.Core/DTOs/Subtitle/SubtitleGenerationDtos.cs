using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Core.DTOs.Subtitle;

/// <summary>
/// Request DTO for queuing automatic subtitle generation.
/// </summary>
public class SubtitleGenerationRequestDto
{
    /// <summary>
    /// Lesson ID to generate subtitles for.
    /// </summary>
    [Required]
    public Guid LessonId { get; set; }

    /// <summary>
    /// MongoDB GridFS file ID of the video.
    /// </summary>
    [Required]
    [StringLength(50)]
    public string VideoFileId { get; set; } = string.Empty;

    /// <summary>
    /// BCP-47 language code for transcription (e.g., "it-IT", "en-US").
    /// Default is "it-IT" (Italian).
    /// </summary>
    [StringLength(10)]
    public string Language { get; set; } = "it-IT";
}

/// <summary>
/// Response DTO for subtitle generation queue operation.
/// </summary>
public class SubtitleGenerationQueuedDto
{
    /// <summary>
    /// Success message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Hangfire job ID for tracking.
    /// </summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>
    /// Lesson ID being processed.
    /// </summary>
    public string LessonId { get; set; } = string.Empty;

    /// <summary>
    /// Target language code.
    /// </summary>
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// Initial status (always "Queued").
    /// </summary>
    public string Status { get; set; } = "Queued";
}

/// <summary>
/// Batch subtitle generation request for multiple lessons.
/// </summary>
public class BatchSubtitleGenerationRequestDto
{
    /// <summary>
    /// List of lesson/video pairs to process.
    /// </summary>
    [Required]
    [MinLength(1)]
    public List<LessonVideoDto> Lessons { get; set; } = new();

    /// <summary>
    /// BCP-47 language code for transcription.
    /// </summary>
    [StringLength(10)]
    public string Language { get; set; } = "it-IT";
}

/// <summary>
/// Lesson and video file ID pair for batch processing.
/// </summary>
public class LessonVideoDto
{
    /// <summary>
    /// Lesson ID.
    /// </summary>
    [Required]
    public Guid LessonId { get; set; }

    /// <summary>
    /// MongoDB GridFS file ID of the video.
    /// </summary>
    [Required]
    [StringLength(50)]
    public string VideoFileId { get; set; } = string.Empty;
}
