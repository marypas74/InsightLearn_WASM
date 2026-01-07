using InsightLearn.WebAssembly.Models;
using InsightLearn.WebAssembly.Services.Http;
using InsightLearn.Core.DTOs.VideoBookmarks;
using Microsoft.Extensions.Logging;

namespace InsightLearn.WebAssembly.Services.LearningSpace;

/// <summary>
/// Implementation of IVideoBookmarkClientService.
/// Part of Student Learning Space v2.1.0.
/// v2.1.0-dev: Fixed endpoint URLs to match backend (/api/bookmarks instead of /api/video-bookmarks)
/// </summary>
public class VideoBookmarkClientService : IVideoBookmarkClientService
{
    private readonly IApiClient _apiClient;
    private readonly ILogger<VideoBookmarkClientService> _logger;
    // v2.1.0-dev: Fixed endpoint to match backend (/api/bookmarks instead of /api/video-bookmarks)
    private const string BaseEndpoint = "/api/bookmarks";

    public VideoBookmarkClientService(IApiClient apiClient, ILogger<VideoBookmarkClientService> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logger = logger;
    }

    public async Task<ApiResponse<List<VideoBookmarkDto>>> GetBookmarksByLessonAsync(Guid lessonId)
    {
        _logger.LogDebug("Fetching bookmarks for lesson: {LessonId}", lessonId);
        // Backend uses query parameter: /api/bookmarks?lessonId={id}
        var response = await _apiClient.GetAsync<List<VideoBookmarkDto>>($"{BaseEndpoint}?lessonId={lessonId}");

        if (response.Success && response.Data != null)
        {
            _logger.LogInformation("Retrieved {BookmarkCount} bookmarks for lesson {LessonId}",
                response.Data.Count, lessonId);
        }
        else
        {
            _logger.LogWarning("Failed to retrieve bookmarks for lesson {LessonId}: {ErrorMessage}",
                lessonId, response.Message ?? "Unknown error");
        }

        return response;
    }

    public async Task<ApiResponse<List<VideoBookmarkDto>>> GetAutoBookmarksAsync(Guid lessonId)
    {
        _logger.LogDebug("Fetching auto-generated bookmarks for lesson: {LessonId}", lessonId);
        // Note: Auto bookmarks endpoint not implemented in backend yet
        // Return empty list as fallback
        _logger.LogWarning("Auto bookmarks endpoint not yet implemented in backend for lesson {LessonId} - returning empty list",
            lessonId);
        return await Task.FromResult(new ApiResponse<List<VideoBookmarkDto>>
        {
            Success = true,
            Data = new List<VideoBookmarkDto>()
        });
    }

    public async Task<ApiResponse<VideoBookmarkDto>> CreateBookmarkAsync(CreateVideoBookmarkDto dto)
    {
        _logger.LogInformation("Creating bookmark for lesson {LessonId} at timestamp {Timestamp}s",
            dto.LessonId, dto.VideoTimestamp);
        var response = await _apiClient.PostAsync<VideoBookmarkDto>($"{BaseEndpoint}", dto);

        if (response.Success && response.Data != null)
        {
            _logger.LogInformation("Bookmark created successfully (ID: {BookmarkId}) for lesson {LessonId}",
                response.Data.Id, dto.LessonId);
        }
        else
        {
            _logger.LogError("Failed to create bookmark for lesson {LessonId}: {ErrorMessage}",
                dto.LessonId, response.Message ?? "Unknown error");
        }

        return response;
    }

    public async Task<ApiResponse<VideoBookmarkDto>> UpdateBookmarkAsync(Guid bookmarkId, UpdateVideoBookmarkDto dto)
    {
        _logger.LogInformation("Updating bookmark {BookmarkId}", bookmarkId);
        var response = await _apiClient.PutAsync<VideoBookmarkDto>($"{BaseEndpoint}/{bookmarkId}", dto);

        if (response.Success)
        {
            _logger.LogInformation("Bookmark updated successfully: {BookmarkId}", bookmarkId);
        }
        else
        {
            _logger.LogError("Failed to update bookmark {BookmarkId}: {ErrorMessage}",
                bookmarkId, response.Message ?? "Unknown error");
        }

        return response;
    }

    public async Task<ApiResponse<object>> DeleteBookmarkAsync(Guid bookmarkId)
    {
        _logger.LogWarning("Deleting bookmark: {BookmarkId}", bookmarkId);
        var response = await _apiClient.DeleteAsync<object>($"{BaseEndpoint}/{bookmarkId}");

        if (response.Success)
        {
            _logger.LogInformation("Bookmark deleted successfully: {BookmarkId}", bookmarkId);
        }
        else
        {
            _logger.LogError("Failed to delete bookmark {BookmarkId}: {ErrorMessage}",
                bookmarkId, response.Message ?? "Unknown error");
        }

        return response;
    }

    public async Task<ApiResponse<BookmarkExistsDto>> CheckExistsAsync(Guid lessonId, int videoTimestamp)
    {
        _logger.LogDebug("Checking if bookmark exists for lesson {LessonId} at timestamp {Timestamp}s",
            lessonId, videoTimestamp);
        // Note: Check exists endpoint not implemented in backend yet
        // Return false as fallback
        _logger.LogWarning("Check bookmark exists endpoint not yet implemented in backend for lesson {LessonId} - returning false",
            lessonId);
        return await Task.FromResult(new ApiResponse<BookmarkExistsDto>
        {
            Success = true,
            Data = new BookmarkExistsDto { Exists = false }
        });
    }
}
