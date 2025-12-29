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

    public async Task<ApiResponse<VideoTranscriptDto>> AutoGenerateTranscriptAsync(AutoGenerateTranscriptRequest request)
    {
        // POST /api/transcripts/{lessonId}/generate
        // Phase 1 Task 1.3 (v2.3.23-dev): Handle HTTP 202 Accepted with polling
        // LinkedIn Learning approach: Queue job, poll status, return when complete

        // Step 1: Queue the transcript generation job
        var generateResponse = await _apiClient.PostAsync<object>(
            $"{BaseEndpoint}/{request.LessonId}/generate",
            new { request.LessonTitle, request.DurationSeconds, request.Language });

        // Step 2: If HTTP 200, transcript already exists - return it directly
        if (generateResponse.StatusCode == 200 && generateResponse.Data != null)
        {
            // Cast the response to VideoTranscriptDto
            return await GetTranscriptAsync(request.LessonId);
        }

        // Step 3: If HTTP 202, job queued - start polling
        if (generateResponse.StatusCode == 202)
        {
            // Poll status every 2 seconds, max 60 attempts (2 minutes)
            const int maxAttempts = 60;
            const int intervalMs = 2000;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                await Task.Delay(intervalMs);

                var statusResponse = await GetStatusAsync(request.LessonId);

                if (!statusResponse.IsSuccess || statusResponse.Data == null)
                {
                    // Status check failed, continue polling
                    continue;
                }

                var status = statusResponse.Data.Status?.ToLowerInvariant();

                // Step 4: Check if completed
                if (status == "completed" || status == "success")
                {
                    // Fetch and return the final transcript
                    return await GetTranscriptAsync(request.LessonId);
                }

                // Step 5: Check if failed
                if (status == "failed" || status == "error")
                {
                    return new ApiResponse<VideoTranscriptDto>
                    {
                        IsSuccess = false,
                        StatusCode = 500,
                        ErrorMessage = statusResponse.Data.ErrorMessage ?? "Transcript generation failed"
                    };
                }

                // Still processing, continue polling...
            }

            // Timeout: Max polling attempts reached
            return new ApiResponse<VideoTranscriptDto>
            {
                IsSuccess = false,
                StatusCode = 408,
                ErrorMessage = "Transcript generation timeout. The job is still processing in the background."
            };
        }

        // Step 6: Unexpected response status
        return new ApiResponse<VideoTranscriptDto>
        {
            IsSuccess = false,
            StatusCode = generateResponse.StatusCode,
            ErrorMessage = generateResponse.ErrorMessage ?? "Unexpected response from transcript generation endpoint"
        };
    }

    public async Task<ApiResponse<TranslationResponseDto>> GetTranslationAsync(Guid lessonId, string targetLanguage)
    {
        // GET /api/transcripts/{lessonId}/translations/{targetLanguage}
        // Phase 8.5: Multi-Language Subtitle Support - Frontend language selector
        return await _apiClient.GetAsync<TranslationResponseDto>(
            $"{BaseEndpoint}/{lessonId}/translations/{targetLanguage}");
    }
}
