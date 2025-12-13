using InsightLearn.WebAssembly.Models;
using InsightLearn.WebAssembly.Services.Http;

namespace InsightLearn.WebAssembly.Services.LearningSpace;

public class VideoProgressClientService : IVideoProgressClientService
{
    private readonly IApiClient _apiClient;
    // v2.1.0-dev: Fixed endpoint path to match backend (/api/progress)
    private const string BaseEndpoint = "/api/progress";

    public VideoProgressClientService(IApiClient apiClient)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
    }

    public async Task<ApiResponse<VideoProgressResponseDto>> TrackProgressAsync(TrackVideoProgressDto dto)
    {
        // Backend uses /api/progress/sync
        return await _apiClient.PostAsync<VideoProgressResponseDto>($"{BaseEndpoint}/sync", dto);
    }

    public async Task<ApiResponse<LastPositionDto>> GetLastPositionAsync(Guid lessonId)
    {
        // Backend uses /api/progress/resume?lessonId={id}
        return await _apiClient.GetAsync<LastPositionDto>($"{BaseEndpoint}/resume?lessonId={lessonId}");
    }

    public async Task<ApiResponse<LessonProgressDto>> GetLessonProgressAsync(Guid lessonId)
    {
        // Using resume endpoint - returns lastPosition
        return await _apiClient.GetAsync<LessonProgressDto>($"{BaseEndpoint}/resume?lessonId={lessonId}");
    }

    public async Task<ApiResponse<List<LessonProgressDto>>> GetCourseProgressAsync(Guid courseId)
    {
        // Backend doesn't have course-level progress endpoint yet
        return new ApiResponse<List<LessonProgressDto>> { Success = true, Data = new List<LessonProgressDto>() };
    }
}
