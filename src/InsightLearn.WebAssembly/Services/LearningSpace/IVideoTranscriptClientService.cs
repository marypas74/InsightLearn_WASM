using InsightLearn.WebAssembly.Models;
using InsightLearn.Core.DTOs.VideoTranscript;
using InsightLearn.Core.DTOs.VideoTranscripts;

namespace InsightLearn.WebAssembly.Services.LearningSpace;

/// <summary>
/// Frontend service for Video Transcripts API.
/// Part of Student Learning Space v2.1.0.
/// </summary>
public interface IVideoTranscriptClientService
{
    /// <summary>
    /// Get complete transcript for a lesson.
    /// </summary>
    Task<ApiResponse<VideoTranscriptDto>> GetTranscriptAsync(Guid lessonId);

    /// <summary>
    /// Queue transcript generation (background job).
    /// </summary>
    Task<ApiResponse<object>> QueueGenerationAsync(QueueTranscriptDto dto);

    /// <summary>
    /// Get transcript processing status.
    /// </summary>
    Task<ApiResponse<TranscriptStatusDto>> GetStatusAsync(Guid lessonId);

    /// <summary>
    /// Search transcript for a keyword.
    /// </summary>
    Task<ApiResponse<List<TranscriptSearchResultDto>>> SearchTranscriptAsync(Guid lessonId, string searchText);

    /// <summary>
    /// Delete transcript.
    /// </summary>
    Task<ApiResponse<object>> DeleteTranscriptAsync(Guid lessonId);
}

/// <summary>
/// Transcript processing status DTO.
/// </summary>
public class TranscriptStatusDto
{
    public string Status { get; set; } = "Pending";
    public string? ErrorMessage { get; set; }
    public double? Progress { get; set; }
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// Transcript search result DTO.
/// </summary>
public class TranscriptSearchResultDto
{
    public double StartTime { get; set; }
    public double EndTime { get; set; }
    public string Text { get; set; } = string.Empty;
    public string? Speaker { get; set; }
    public int MatchIndex { get; set; }
}
