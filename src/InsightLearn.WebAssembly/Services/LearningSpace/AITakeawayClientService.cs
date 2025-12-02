using InsightLearn.WebAssembly.Models;
using InsightLearn.WebAssembly.Services.Http;
using InsightLearn.Core.DTOs.AITakeaways;

namespace InsightLearn.WebAssembly.Services.LearningSpace;

/// <summary>
/// Implementation of IAITakeawayClientService.
/// Part of Student Learning Space v2.1.0.
/// </summary>
public class AITakeawayClientService : IAITakeawayClientService
{
    private readonly IApiClient _apiClient;
    // v2.1.0-dev: Fixed endpoint path to match backend (/api/takeaways instead of /api/ai-takeaways)
    private const string BaseEndpoint = "/api/takeaways";

    public AITakeawayClientService(IApiClient apiClient)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
    }

    public async Task<ApiResponse<VideoKeyTakeawaysDto>> GetTakeawaysAsync(Guid lessonId)
    {
        return await _apiClient.GetAsync<VideoKeyTakeawaysDto>($"{BaseEndpoint}/{lessonId}");
    }

    public async Task<ApiResponse<object>> QueueGenerationAsync(QueueTakeawayDto dto)
    {
        // v2.1.0-dev: Fixed endpoint path to include lessonId in URL
        return await _apiClient.PostAsync<object>($"{BaseEndpoint}/{dto.LessonId}/generate", dto);
    }

    public async Task<ApiResponse<TakeawayStatusDto>> GetStatusAsync(Guid lessonId)
    {
        return await _apiClient.GetAsync<TakeawayStatusDto>($"{BaseEndpoint}/{lessonId}/status");
    }

    public async Task<ApiResponse<object>> SubmitFeedbackAsync(Guid lessonId, SubmitFeedbackDto dto)
    {
        return await _apiClient.PostAsync<object>($"{BaseEndpoint}/{lessonId}/feedback", dto);
    }

    public async Task<ApiResponse<object>> DeleteTakeawaysAsync(Guid lessonId)
    {
        return await _apiClient.DeleteAsync<object>($"{BaseEndpoint}/{lessonId}");
    }

    public async Task<ApiResponse<object>> InvalidateCacheAsync(Guid lessonId)
    {
        return await _apiClient.PostAsync<object>($"{BaseEndpoint}/{lessonId}/invalidate-cache", null);
    }
}
