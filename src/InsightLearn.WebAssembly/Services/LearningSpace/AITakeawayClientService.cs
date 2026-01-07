using InsightLearn.WebAssembly.Models;
using InsightLearn.WebAssembly.Services.Http;
using InsightLearn.Core.DTOs.AITakeaways;
using Microsoft.Extensions.Logging;

namespace InsightLearn.WebAssembly.Services.LearningSpace;

/// <summary>
/// Implementation of IAITakeawayClientService.
/// Part of Student Learning Space v2.1.0.
/// </summary>
public class AITakeawayClientService : IAITakeawayClientService
{
    private readonly IApiClient _apiClient;
    private readonly ILogger<AITakeawayClientService> _logger;
    // v2.1.0-dev: Fixed endpoint path to match backend (/api/takeaways instead of /api/ai-takeaways)
    private const string BaseEndpoint = "/api/takeaways";

    public AITakeawayClientService(IApiClient apiClient, ILogger<AITakeawayClientService> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logger = logger;
    }

    public async Task<ApiResponse<VideoKeyTakeawaysDto>> GetTakeawaysAsync(Guid lessonId)
    {
        _logger.LogDebug("Fetching AI takeaways for lesson: {LessonId}", lessonId);
        var response = await _apiClient.GetAsync<VideoKeyTakeawaysDto>($"{BaseEndpoint}/{lessonId}");

        if (response.Success && response.Data != null)
        {
            _logger.LogInformation("Retrieved AI takeaways for lesson {LessonId} ({TakeawayCount} takeaways)",
                lessonId, response.Data.Takeaways?.Count ?? 0);
        }
        else
        {
            _logger.LogWarning("Failed to retrieve AI takeaways for lesson {LessonId}: {ErrorMessage}",
                lessonId, response.Message ?? "Unknown error");
        }

        return response;
    }

    public async Task<ApiResponse<object>> QueueGenerationAsync(QueueTakeawayDto dto)
    {
        _logger.LogInformation("Queueing AI takeaway generation for lesson {LessonId}", dto.LessonId);
        // v2.1.0-dev: Fixed endpoint path to include lessonId in URL
        var response = await _apiClient.PostAsync<object>($"{BaseEndpoint}/{dto.LessonId}/generate", dto);

        if (response.Success)
        {
            _logger.LogInformation("AI takeaway generation queued successfully for lesson {LessonId}", dto.LessonId);
        }
        else
        {
            _logger.LogError("Failed to queue AI takeaway generation for lesson {LessonId}: {ErrorMessage}",
                dto.LessonId, response.Message ?? "Unknown error");
        }

        return response;
    }

    public async Task<ApiResponse<TakeawayStatusDto>> GetStatusAsync(Guid lessonId)
    {
        _logger.LogDebug("Fetching AI takeaway status for lesson: {LessonId}", lessonId);
        var response = await _apiClient.GetAsync<TakeawayStatusDto>($"{BaseEndpoint}/{lessonId}/status");

        if (response.Success && response.Data != null)
        {
            _logger.LogDebug("AI takeaway status for lesson {LessonId}: {Status}",
                lessonId, response.Data.Status);
        }
        else
        {
            _logger.LogWarning("Failed to retrieve AI takeaway status for lesson {LessonId}: {ErrorMessage}",
                lessonId, response.Message ?? "Unknown error");
        }

        return response;
    }

    public async Task<ApiResponse<object>> SubmitFeedbackAsync(Guid lessonId, SubmitFeedbackDto dto)
    {
        _logger.LogInformation("Submitting feedback for AI takeaway (lesson {LessonId}, takeawayId {TakeawayId}, feedback: {Feedback})",
            lessonId, dto.TakeawayId, dto.Feedback);
        var response = await _apiClient.PostAsync<object>($"{BaseEndpoint}/{lessonId}/feedback", dto);

        if (response.Success)
        {
            _logger.LogInformation("Feedback submitted successfully for AI takeaway {TakeawayId}", dto.TakeawayId);
        }
        else
        {
            _logger.LogWarning("Failed to submit feedback for AI takeaway {TakeawayId}: {ErrorMessage}",
                dto.TakeawayId, response.Message ?? "Unknown error");
        }

        return response;
    }

    public async Task<ApiResponse<object>> DeleteTakeawaysAsync(Guid lessonId)
    {
        _logger.LogWarning("Deleting AI takeaways for lesson: {LessonId}", lessonId);
        var response = await _apiClient.DeleteAsync<object>($"{BaseEndpoint}/{lessonId}");

        if (response.Success)
        {
            _logger.LogInformation("AI takeaways deleted successfully for lesson {LessonId}", lessonId);
        }
        else
        {
            _logger.LogError("Failed to delete AI takeaways for lesson {LessonId}: {ErrorMessage}",
                lessonId, response.Message ?? "Unknown error");
        }

        return response;
    }

    public async Task<ApiResponse<object>> InvalidateCacheAsync(Guid lessonId)
    {
        _logger.LogInformation("Invalidating AI takeaway cache for lesson: {LessonId}", lessonId);
        var response = await _apiClient.PostAsync<object>($"{BaseEndpoint}/{lessonId}/invalidate-cache", null);

        if (response.Success)
        {
            _logger.LogInformation("AI takeaway cache invalidated successfully for lesson {LessonId}", lessonId);
        }
        else
        {
            _logger.LogWarning("Failed to invalidate AI takeaway cache for lesson {LessonId}: {ErrorMessage}",
                lessonId, response.Message ?? "Unknown error");
        }

        return response;
    }
}
