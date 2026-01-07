using InsightLearn.WebAssembly.Models;
using InsightLearn.WebAssembly.Services.Http;
using InsightLearn.Core.DTOs.VideoTranscript;
using InsightLearn.Core.DTOs.VideoTranscripts;
using Microsoft.Extensions.Logging;

namespace InsightLearn.WebAssembly.Services.LearningSpace;

/// <summary>
/// Implementation of IVideoTranscriptClientService.
/// Part of Student Learning Space v2.1.0.
/// </summary>
public class VideoTranscriptClientService : IVideoTranscriptClientService
{
    private readonly IApiClient _apiClient;
    private readonly ILogger<VideoTranscriptClientService> _logger;
    // v2.1.0-dev: Fixed endpoint to match backend (/api/transcripts instead of /api/video-transcripts)
    private const string BaseEndpoint = "/api/transcripts";

    public VideoTranscriptClientService(IApiClient apiClient, ILogger<VideoTranscriptClientService> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logger = logger;
    }

    public async Task<ApiResponse<VideoTranscriptDto>> GetTranscriptAsync(Guid lessonId)
    {
        _logger.LogDebug("Fetching transcript for lesson: {LessonId}", lessonId);
        var response = await _apiClient.GetAsync<VideoTranscriptDto>($"{BaseEndpoint}/{lessonId}");

        if (response.Success && response.Data != null)
        {
            _logger.LogInformation("Retrieved transcript for lesson {LessonId} ({SegmentCount} segments)",
                lessonId, response.Data.Segments?.Count ?? 0);
        }
        else
        {
            _logger.LogWarning("Failed to retrieve transcript for lesson {LessonId}: {ErrorMessage}",
                lessonId, response.Message ?? "Unknown error");
        }

        return response;
    }

    public async Task<ApiResponse<object>> QueueGenerationAsync(QueueTranscriptDto dto)
    {
        _logger.LogInformation("Queueing transcript generation for lesson {LessonId} (language: {Language})",
            dto.LessonId, dto.Language);
        // Backend uses /api/transcripts/{lessonId}/generate
        var response = await _apiClient.PostAsync<object>($"{BaseEndpoint}/{dto.LessonId}/generate", dto);

        if (response.Success)
        {
            _logger.LogInformation("Transcript generation queued successfully for lesson {LessonId}", dto.LessonId);
        }
        else
        {
            _logger.LogError("Failed to queue transcript generation for lesson {LessonId}: {ErrorMessage}",
                dto.LessonId, response.Message ?? "Unknown error");
        }

        return response;
    }

    public async Task<ApiResponse<TranscriptStatusDto>> GetStatusAsync(Guid lessonId)
    {
        _logger.LogDebug("Fetching transcript status for lesson: {LessonId}", lessonId);
        var response = await _apiClient.GetAsync<TranscriptStatusDto>($"{BaseEndpoint}/{lessonId}/status");

        if (response.Success && response.Data != null)
        {
            _logger.LogDebug("Transcript status for lesson {LessonId}: {Status}",
                lessonId, response.Data.Status);
        }
        else
        {
            _logger.LogWarning("Failed to retrieve transcript status for lesson {LessonId}: {ErrorMessage}",
                lessonId, response.Message ?? "Unknown error");
        }

        return response;
    }

    public async Task<ApiResponse<List<TranscriptSearchResultDto>>> SearchTranscriptAsync(Guid lessonId, string searchText)
    {
        _logger.LogDebug("Searching transcript for lesson {LessonId} with query: {SearchText}",
            lessonId, searchText);
        var response = await _apiClient.GetAsync<List<TranscriptSearchResultDto>>(
            $"{BaseEndpoint}/{lessonId}/search?searchText={Uri.EscapeDataString(searchText)}");

        if (response.Success && response.Data != null)
        {
            _logger.LogInformation("Found {ResultCount} matches in transcript for lesson {LessonId}",
                response.Data.Count, lessonId);
        }
        else
        {
            _logger.LogWarning("Failed to search transcript for lesson {LessonId}: {ErrorMessage}",
                lessonId, response.Message ?? "Unknown error");
        }

        return response;
    }

    public async Task<ApiResponse<object>> DeleteTranscriptAsync(Guid lessonId)
    {
        _logger.LogWarning("Deleting transcript for lesson: {LessonId}", lessonId);
        var response = await _apiClient.DeleteAsync<object>($"{BaseEndpoint}/{lessonId}");

        if (response.Success)
        {
            _logger.LogInformation("Transcript deleted successfully for lesson {LessonId}", lessonId);
        }
        else
        {
            _logger.LogError("Failed to delete transcript for lesson {LessonId}: {ErrorMessage}",
                lessonId, response.Message ?? "Unknown error");
        }

        return response;
    }

    public async Task<ApiResponse<VideoTranscriptDto>> AutoGenerateTranscriptAsync(AutoGenerateTranscriptRequest request)
    {
        _logger.LogInformation("Auto-generating transcript for lesson {LessonId} (language: {Language}, duration: {Duration}s)",
            request.LessonId, request.Language, request.DurationSeconds);

        // POST /api/transcripts/{lessonId}/generate
        // Phase 1 Task 1.3 (v2.3.23-dev): Handle HTTP 202 Accepted with polling
        // LinkedIn Learning approach: Queue job, poll status, return when complete

        // Step 1: Queue the transcript generation job
        var generateResponse = await _apiClient.PostAsync<object>(
            $"{BaseEndpoint}/{request.LessonId}/generate",
            new { request.LessonTitle, request.DurationSeconds, request.Language });

        // Step 2: Check if transcript generation was successful (HTTP 200 - already exists)
        // Note: ApiResponse doesn't expose HTTP status codes, so we check Success flag
        if (generateResponse.Success && generateResponse.Data != null)
        {
            _logger.LogInformation("Transcript already exists for lesson {LessonId}, returning cached version",
                request.LessonId);
            // Transcript already exists - return it directly
            return await GetTranscriptAsync(request.LessonId);
        }

        // Step 3: Job likely queued (HTTP 202) or needs polling - start status checking
        // Poll status every 2 seconds, max 60 attempts (2 minutes)
        const int maxAttempts = 60;
        const int intervalMs = 2000;

        _logger.LogDebug("Starting transcript generation polling for lesson {LessonId} (max {MaxAttempts} attempts, {IntervalMs}ms interval)",
            request.LessonId, maxAttempts, intervalMs);

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            await Task.Delay(intervalMs);

            var statusResponse = await GetStatusAsync(request.LessonId);

            if (!statusResponse.Success || statusResponse.Data == null)
            {
                _logger.LogDebug("Status check failed for lesson {LessonId} (attempt {Attempt}/{MaxAttempts}), continuing polling",
                    request.LessonId, attempt + 1, maxAttempts);
                // Status check failed, continue polling
                continue;
            }

            var status = statusResponse.Data.Status?.ToLowerInvariant();

            // Step 4: Check if completed
            if (status == "completed" || status == "success")
            {
                _logger.LogInformation("Transcript generation completed for lesson {LessonId} after {Attempts} polling attempts",
                    request.LessonId, attempt + 1);
                // Fetch and return the final transcript
                return await GetTranscriptAsync(request.LessonId);
            }

            // Step 5: Check if failed
            if (status == "failed" || status == "error")
            {
                _logger.LogError("Transcript generation failed for lesson {LessonId} with status '{Status}': {ErrorMessage}",
                    request.LessonId, status, statusResponse.Data.ErrorMessage ?? "Unknown error");
                return new ApiResponse<VideoTranscriptDto>
                {
                    Success = false,
                    Message = statusResponse.Data.ErrorMessage ?? "Transcript generation failed"
                };
            }

            _logger.LogDebug("Transcript generation still processing for lesson {LessonId} (status: {Status}, attempt {Attempt}/{MaxAttempts})",
                request.LessonId, status, attempt + 1, maxAttempts);
            // Still processing, continue polling...
        }

        // Timeout: Max polling attempts reached
        _logger.LogWarning("Transcript generation timeout for lesson {LessonId} after {MaxAttempts} polling attempts ({TotalSeconds}s total)",
            request.LessonId, maxAttempts, (maxAttempts * intervalMs) / 1000);
        return new ApiResponse<VideoTranscriptDto>
        {
            Success = false,
            Message = "Transcript generation timeout. The job is still processing in the background."
        };
    }

    public async Task<ApiResponse<TranslationResponseDto>> GetTranslationAsync(Guid lessonId, string targetLanguage)
    {
        _logger.LogDebug("Fetching translation for lesson {LessonId} (target language: {TargetLanguage})",
            lessonId, targetLanguage);
        // GET /api/transcripts/{lessonId}/translations/{targetLanguage}
        // Phase 8.5: Multi-Language Subtitle Support - Frontend language selector
        var response = await _apiClient.GetAsync<TranslationResponseDto>(
            $"{BaseEndpoint}/{lessonId}/translations/{targetLanguage}");

        if (response.Success && response.Data != null)
        {
            _logger.LogInformation("Retrieved translation for lesson {LessonId} to {TargetLanguage}",
                lessonId, targetLanguage);
        }
        else
        {
            _logger.LogWarning("Failed to retrieve translation for lesson {LessonId} to {TargetLanguage}: {ErrorMessage}",
                lessonId, targetLanguage, response.Message ?? "Unknown error");
        }

        return response;
    }
}
