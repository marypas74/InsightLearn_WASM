using InsightLearn.WebAssembly.Models;
using InsightLearn.Core.DTOs.AITakeaways;

namespace InsightLearn.WebAssembly.Services.LearningSpace;

/// <summary>
/// Frontend service for AI Takeaways API.
/// Part of Student Learning Space v2.1.0.
/// </summary>
public interface IAITakeawayClientService
{
    /// <summary>
    /// Get AI-generated key takeaways for a lesson.
    /// </summary>
    Task<ApiResponse<VideoKeyTakeawaysDto>> GetTakeawaysAsync(Guid lessonId);

    /// <summary>
    /// Queue AI takeaway generation (background job).
    /// </summary>
    Task<ApiResponse<object>> QueueGenerationAsync(QueueTakeawayDto dto);

    /// <summary>
    /// Get takeaway processing status.
    /// </summary>
    Task<ApiResponse<TakeawayStatusDto>> GetStatusAsync(Guid lessonId);

    /// <summary>
    /// Submit user feedback (thumbs up/down) on a takeaway.
    /// </summary>
    Task<ApiResponse<object>> SubmitFeedbackAsync(Guid lessonId, SubmitFeedbackDto dto);

    /// <summary>
    /// Delete AI takeaways.
    /// </summary>
    Task<ApiResponse<object>> DeleteTakeawaysAsync(Guid lessonId);

    /// <summary>
    /// Invalidate cache and force re-fetch from database.
    /// </summary>
    Task<ApiResponse<object>> InvalidateCacheAsync(Guid lessonId);
}

/// <summary>
/// AI takeaway processing status DTO.
/// </summary>
public class TakeawayStatusDto
{
    public string Status { get; set; } = "Pending";
    public string? ErrorMessage { get; set; }
    public int? TakeawayCount { get; set; }
    public DateTime? CompletedAt { get; set; }
}
