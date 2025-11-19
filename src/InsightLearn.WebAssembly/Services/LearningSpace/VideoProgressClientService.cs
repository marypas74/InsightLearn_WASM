using InsightLearn.WebAssembly.Models;
using InsightLearn.WebAssembly.Services.Http;

namespace InsightLearn.WebAssembly.Services.LearningSpace;

public class VideoProgressClientService : IVideoProgressClientService
{
    private readonly IApiClient _apiClient;
    private const string BaseEndpoint = "/api/video-progress";

    public VideoProgressClientService(IApiClient apiClient)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
    }

    public async Task<ApiResponse<VideoProgressResponseDto>> TrackProgressAsync(TrackVideoProgressDto dto)
    {
        return await _apiClient.PostAsync<VideoProgressResponseDto>($"{BaseEndpoint}/track", dto);
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
