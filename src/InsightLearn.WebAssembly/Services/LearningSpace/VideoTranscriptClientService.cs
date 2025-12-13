using InsightLearn.WebAssembly.Models;
using InsightLearn.WebAssembly.Services.Http;
using InsightLearn.Core.DTOs.VideoTranscript;
using InsightLearn.Core.DTOs.VideoTranscripts;

namespace InsightLearn.WebAssembly.Services.LearningSpace;

/// <summary>
/// Implementation of IVideoTranscriptClientService.
/// Part of Student Learning Space v2.1.0.
/// </summary>
public class VideoTranscriptClientService : IVideoTranscriptClientService
{
    private readonly IApiClient _apiClient;
    // v2.1.0-dev: Fixed endpoint to match backend (/api/transcripts instead of /api/video-transcripts)
    private const string BaseEndpoint = "/api/transcripts";

    public VideoTranscriptClientService(IApiClient apiClient)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
    }

    public async Task<ApiResponse<VideoTranscriptDto>> GetTranscriptAsync(Guid lessonId)
    {
        return await _apiClient.GetAsync<VideoTranscriptDto>($"{BaseEndpoint}/{lessonId}");
    }

    public async Task<ApiResponse<object>> QueueGenerationAsync(QueueTranscriptDto dto)
    {
        // Backend uses /api/transcripts/{lessonId}/generate
        return await _apiClient.PostAsync<object>($"{BaseEndpoint}/{dto.LessonId}/generate", dto);
    }

    public async Task<ApiResponse<TranscriptStatusDto>> GetStatusAsync(Guid lessonId)
    {
        return await _apiClient.GetAsync<TranscriptStatusDto>($"{BaseEndpoint}/{lessonId}/status");
    }

    public async Task<ApiResponse<List<TranscriptSearchResultDto>>> SearchTranscriptAsync(Guid lessonId, string searchText)
    {
        return await _apiClient.GetAsync<List<TranscriptSearchResultDto>>(
            $"{BaseEndpoint}/{lessonId}/search?searchText={Uri.EscapeDataString(searchText)}");
    }

    public async Task<ApiResponse<object>> DeleteTranscriptAsync(Guid lessonId)
    {
        return await _apiClient.DeleteAsync<object>($"{BaseEndpoint}/{lessonId}");
    }
}
