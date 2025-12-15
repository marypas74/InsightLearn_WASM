using InsightLearn.Core.DTOs.Course;
using InsightLearn.Core.DTOs.VideoTranscript;

namespace InsightLearn.Application.Services;

/// <summary>
/// Service interface for automatic subtitle generation using Whisper ASR.
/// Handles video transcription and WebVTT subtitle file creation.
/// Part of Student Learning Space v2.1.0.
/// </summary>
public interface ISubtitleGenerationService
{
    /// <summary>
    /// Queue a subtitle generation job for background processing via Hangfire.
    /// </summary>
    /// <param name="lessonId">The lesson ID to generate subtitles for.</param>
    /// <param name="videoFileId">MongoDB GridFS file ID of the video.</param>
    /// <param name="language">BCP-47 language code (e.g., "it-IT", "en-US").</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Job ID for tracking the generation status.</returns>
    Task<string> QueueSubtitleGenerationAsync(
        Guid lessonId,
        string videoFileId,
        string language = "it-IT",
        CancellationToken ct = default);

    /// <summary>
    /// Generate subtitles synchronously (called by Hangfire job).
    /// Transcribes video using Whisper API, converts to WebVTT, and saves to MongoDB.
    /// </summary>
    /// <param name="lessonId">The lesson ID.</param>
    /// <param name="videoFileId">MongoDB GridFS file ID of the video.</param>
    /// <param name="language">BCP-47 language code.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Created subtitle track DTO.</returns>
    Task<SubtitleTrackDto> GenerateSubtitlesAsync(
        Guid lessonId,
        string videoFileId,
        string language = "it-IT",
        CancellationToken ct = default);

    /// <summary>
    /// Get the current status of a subtitle generation job.
    /// </summary>
    /// <param name="lessonId">The lesson ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Status DTO with progress information.</returns>
    Task<SubtitleGenerationStatusDto> GetGenerationStatusAsync(
        Guid lessonId,
        CancellationToken ct = default);

    /// <summary>
    /// Convert transcript segments to WebVTT format.
    /// </summary>
    /// <param name="segments">List of transcript segments with timestamps.</param>
    /// <param name="language">Language code for the WEBVTT header.</param>
    /// <returns>WebVTT formatted string.</returns>
    string ConvertToWebVTT(List<TranscriptSegmentDto> segments, string language);

    /// <summary>
    /// Check if a lesson already has subtitles in a specific language.
    /// </summary>
    /// <param name="lessonId">The lesson ID.</param>
    /// <param name="language">Language code (e.g., "it", "en").</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if subtitles exist for that language.</returns>
    Task<bool> HasSubtitlesAsync(Guid lessonId, string language, CancellationToken ct = default);
}

/// <summary>
/// Status DTO for subtitle generation progress tracking.
/// </summary>
public class SubtitleGenerationStatusDto
{
    /// <summary>
    /// Lesson ID being processed.
    /// </summary>
    public Guid LessonId { get; set; }

    /// <summary>
    /// Current status: Queued, Processing, Completed, Failed.
    /// </summary>
    public string Status { get; set; } = "Unknown";

    /// <summary>
    /// Progress percentage (0-100).
    /// </summary>
    public int Progress { get; set; }

    /// <summary>
    /// Current step description (e.g., "Transcribing audio...", "Converting to WebVTT...").
    /// </summary>
    public string CurrentStep { get; set; } = string.Empty;

    /// <summary>
    /// Error message if Status is "Failed".
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Hangfire job ID for tracking.
    /// </summary>
    public string? JobId { get; set; }

    /// <summary>
    /// When the job was started.
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// When the job completed (success or failure).
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Language code being generated.
    /// </summary>
    public string Language { get; set; } = "it-IT";
}
