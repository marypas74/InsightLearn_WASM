using InsightLearn.WebAssembly.Models;
using InsightLearn.Core.DTOs.VideoBookmarks;

namespace InsightLearn.WebAssembly.Services.LearningSpace;

/// <summary>
/// Frontend service for Video Bookmarks API.
/// Part of Student Learning Space v2.1.0.
/// </summary>
public interface IVideoBookmarkClientService
{
    /// <summary>
    /// Get user's manual bookmarks for a lesson.
    /// </summary>
    Task<ApiResponse<List<VideoBookmarkDto>>> GetBookmarksByLessonAsync(Guid lessonId);

    /// <summary>
    /// Get AI-generated auto bookmarks (chapter markers) for a lesson.
    /// </summary>
    Task<ApiResponse<List<VideoBookmarkDto>>> GetAutoBookmarksAsync(Guid lessonId);

    /// <summary>
    /// Create a new bookmark.
    /// </summary>
    Task<ApiResponse<VideoBookmarkDto>> CreateBookmarkAsync(CreateVideoBookmarkDto dto);

    /// <summary>
    /// Update bookmark label.
    /// </summary>
    Task<ApiResponse<VideoBookmarkDto>> UpdateBookmarkAsync(Guid bookmarkId, UpdateVideoBookmarkDto dto);

    /// <summary>
    /// Delete a bookmark.
    /// </summary>
    Task<ApiResponse<object>> DeleteBookmarkAsync(Guid bookmarkId);

    /// <summary>
    /// Check if a bookmark exists at a specific timestamp.
    /// </summary>
    Task<ApiResponse<BookmarkExistsDto>> CheckExistsAsync(Guid lessonId, int videoTimestamp);
}

/// <summary>
/// Bookmark existence check result DTO.
/// </summary>
public class BookmarkExistsDto
{
    public bool Exists { get; set; }
    public Guid? BookmarkId { get; set; }
}
