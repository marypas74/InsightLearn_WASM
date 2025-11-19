using InsightLearn.WebAssembly.Models;
using InsightLearn.WebAssembly.Services.Http;
using InsightLearn.Core.DTOs.VideoBookmarks;

namespace InsightLearn.WebAssembly.Services.LearningSpace;

/// <summary>
/// Implementation of IVideoBookmarkClientService.
/// Part of Student Learning Space v2.1.0.
/// </summary>
public class VideoBookmarkClientService : IVideoBookmarkClientService
{
    private readonly IApiClient _apiClient;
    private const string BaseEndpoint = "/api/video-bookmarks";

    public VideoBookmarkClientService(IApiClient apiClient)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
    }

    public async Task<ApiResponse<List<VideoBookmarkDto>>> GetBookmarksByLessonAsync(Guid lessonId)
    {
        return await _apiClient.GetAsync<List<VideoBookmarkDto>>($"{BaseEndpoint}/lesson/{lessonId}");
    }

    public async Task<ApiResponse<List<VideoBookmarkDto>>> GetAutoBookmarksAsync(Guid lessonId)
    {
        return await _apiClient.GetAsync<List<VideoBookmarkDto>>($"{BaseEndpoint}/auto/{lessonId}");
    }

    public async Task<ApiResponse<VideoBookmarkDto>> CreateBookmarkAsync(CreateVideoBookmarkDto dto)
    {
        return await _apiClient.PostAsync<VideoBookmarkDto>($"{BaseEndpoint}", dto);
    }

    public async Task<ApiResponse<VideoBookmarkDto>> UpdateBookmarkAsync(Guid bookmarkId, UpdateVideoBookmarkDto dto)
    {
        return await _apiClient.PutAsync<VideoBookmarkDto>($"{BaseEndpoint}/{bookmarkId}", dto);
    }

    public async Task<ApiResponse<object>> DeleteBookmarkAsync(Guid bookmarkId)
    {
        return await _apiClient.DeleteAsync<object>($"{BaseEndpoint}/{bookmarkId}");
    }

    public async Task<ApiResponse<BookmarkExistsDto>> CheckExistsAsync(Guid lessonId, int videoTimestamp)
    {
        return await _apiClient.GetAsync<BookmarkExistsDto>(
            $"{BaseEndpoint}/check?lessonId={lessonId}&videoTimestamp={videoTimestamp}");
    }
}
