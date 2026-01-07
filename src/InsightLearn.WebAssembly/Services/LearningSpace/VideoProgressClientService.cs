using InsightLearn.WebAssembly.Models;
using InsightLearn.WebAssembly.Services.Http;
using Microsoft.Extensions.Logging;

namespace InsightLearn.WebAssembly.Services.LearningSpace;

public class VideoProgressClientService : IVideoProgressClientService
{
    private readonly IApiClient _apiClient;
    private readonly ILogger<VideoProgressClientService> _logger;
    // v2.1.0-dev: Fixed endpoint path to match backend (/api/progress)
    private const string BaseEndpoint = "/api/progress";

    public VideoProgressClientService(IApiClient apiClient, ILogger<VideoProgressClientService> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logger = logger;
    }

    public async Task<ApiResponse<VideoProgressResponseDto>> TrackProgressAsync(TrackVideoProgressDto dto)
    {
        _logger.LogDebug("Tracking video progress for lesson {LessonId} at position {Position}s",
            dto.LessonId, dto.CurrentTimestampSeconds);
        // Backend uses /api/progress/sync
        var response = await _apiClient.PostAsync<VideoProgressResponseDto>($"{BaseEndpoint}/sync", dto);

        if (response.Success && response.Data != null)
        {
            _logger.LogInformation("Video progress tracked successfully for lesson {LessonId} ({PercentComplete}% complete)",
                dto.LessonId, response.Data.CompletionPercentage);
        }
        else
        {
            _logger.LogWarning("Failed to track video progress for lesson {LessonId}: {ErrorMessage}",
                dto.LessonId, response.Message ?? "Unknown error");
        }

        return response;
    }

    public async Task<ApiResponse<LastPositionDto>> GetLastPositionAsync(Guid lessonId)
    {
        _logger.LogDebug("Fetching last video position for lesson: {LessonId}", lessonId);
        // Backend uses /api/progress/resume?lessonId={id}
        var response = await _apiClient.GetAsync<LastPositionDto>($"{BaseEndpoint}/resume?lessonId={lessonId}");

        if (response.Success && response.Data != null)
        {
            _logger.LogInformation("Retrieved last position for lesson {LessonId}: {Position}s",
                lessonId, response.Data.LastPosition);
        }
        else
        {
            _logger.LogWarning("Failed to retrieve last position for lesson {LessonId}: {ErrorMessage}",
                lessonId, response.Message ?? "Unknown error");
        }

        return response;
    }

    public async Task<ApiResponse<LessonProgressDto>> GetLessonProgressAsync(Guid lessonId)
    {
        _logger.LogDebug("Fetching lesson progress for: {LessonId}", lessonId);
        // Using resume endpoint - returns lastPosition
        var response = await _apiClient.GetAsync<LessonProgressDto>($"{BaseEndpoint}/resume?lessonId={lessonId}");

        if (response.Success && response.Data != null)
        {
            _logger.LogInformation("Retrieved lesson progress for {LessonId}", lessonId);
        }
        else
        {
            _logger.LogWarning("Failed to retrieve lesson progress for {LessonId}: {ErrorMessage}",
                lessonId, response.Message ?? "Unknown error");
        }

        return response;
    }

    public async Task<ApiResponse<List<LessonProgressDto>>> GetCourseProgressAsync(Guid courseId)
    {
        _logger.LogDebug("Fetching course progress for: {CourseId}", courseId);
        // Backend doesn't have course-level progress endpoint yet
        _logger.LogWarning("Course-level progress endpoint not yet implemented in backend for course {CourseId} - returning empty list",
            courseId);
        return await Task.FromResult(new ApiResponse<List<LessonProgressDto>> { Success = true, Data = new List<LessonProgressDto>() });
    }
}
