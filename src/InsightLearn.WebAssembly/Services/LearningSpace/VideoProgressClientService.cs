using InsightLearn.WebAssembly.Models;
using InsightLearn.WebAssembly.Services.Http;

namespace InsightLearn.WebAssembly.Services.LearningSpace;

public class VideoProgressClientService : IVideoProgressClientService
{
    private readonly IApiClient _apiClient;
    // v2.1.0-dev: Fixed endpoint path to match backend (/api/engagement instead of /api/video-progress)
    private const string BaseEndpoint = "/api/engagement";

    public VideoProgressClientService(IApiClient apiClient)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
    }

    public async Task<ApiResponse<VideoProgressResponseDto>> TrackProgressAsync(TrackVideoProgressDto dto)
    {
        // Backend uses /api/engagement/video-progress (not /track)
        return await _apiClient.PostAsync<VideoProgressResponseDto>($"{BaseEndpoint}/video-progress", dto);
    }

    public async Task<ApiResponse<LastPositionDto>> GetLastPositionAsync(Guid lessonId)
    {
        return await _apiClient.GetAsync<LastPositionDto>($"{BaseEndpoint}/lesson/{lessonId}/position");
    }

    public async Task<ApiResponse<LessonProgressDto>> GetLessonProgressAsync(Guid lessonId)
    {
        return await _apiClient.GetAsync<LessonProgressDto>($"{BaseEndpoint}/lesson/{lessonId}");
    }

    public async Task<ApiResponse<List<LessonProgressDto>>> GetCourseProgressAsync(Guid courseId)
    {
        return await _apiClient.GetAsync<List<LessonProgressDto>>($"{BaseEndpoint}/course/{courseId}");
    }
}
