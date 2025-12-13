using InsightLearn.WebAssembly.Models;
using InsightLearn.WebAssembly.Services.Http;
using InsightLearn.Core.DTOs.VideoBookmarks;

namespace InsightLearn.WebAssembly.Services.LearningSpace;

/// <summary>
/// Implementation of IVideoBookmarkClientService.
/// Part of Student Learning Space v2.1.0.
/// v2.1.0-dev: Fixed endpoint URLs to match backend (/api/bookmarks instead of /api/video-bookmarks)
/// </summary>
public class VideoBookmarkClientService : IVideoBookmarkClientService
{
    private readonly IApiClient _apiClient;
    // v2.1.0-dev: Fixed endpoint to match backend (/api/bookmarks instead of /api/video-bookmarks)
    private const string BaseEndpoint = "/api/bookmarks";

    public VideoBookmarkClientService(IApiClient apiClient)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
    }

    public async Task<ApiResponse<List<VideoBookmarkDto>>> GetBookmarksByLessonAsync(Guid lessonId)
    {
        // Backend uses query parameter: /api/bookmarks?lessonId={id}
        return await _apiClient.GetAsync<List<VideoBookmarkDto>>($"{BaseEndpoint}?lessonId={lessonId}");
    }

    public async Task<ApiResponse<List<VideoBookmarkDto>>> GetAutoBookmarksAsync(Guid lessonId)
    {
        // Note: Auto bookmarks endpoint not implemented in backend yet
        // Return empty list as fallback
        return new ApiResponse<List<VideoBookmarkDto>>
        {
            Success = true,
            Data = new List<VideoBookmarkDto>()
        };
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
        // Note: Check exists endpoint not implemented in backend yet
        // Return false as fallback
        return new ApiResponse<BookmarkExistsDto>
        {
            Success = true,
            Data = new BookmarkExistsDto { Exists = false }
        };
    }
}
