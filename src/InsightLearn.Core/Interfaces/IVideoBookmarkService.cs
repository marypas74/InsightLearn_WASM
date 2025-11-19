using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using InsightLearn.Core.DTOs.VideoBookmarks;
using InsightLearn.Core.Entities;

namespace InsightLearn.Core.Interfaces
{
    /// <summary>
    /// Service interface for video bookmark management.
    /// Provides authorization and business logic over repository layer.
    /// Part of Student Learning Space v2.1.0.
    /// </summary>
    public interface IVideoBookmarkService
    {
        /// <summary>
        /// Get all bookmarks for a specific user and lesson.
        /// </summary>
        Task<List<VideoBookmark>> GetBookmarksByUserAndLessonAsync(Guid userId, Guid lessonId, CancellationToken ct = default);

        /// <summary>
        /// Get all auto-generated bookmarks (AI chapter markers) for a lesson.
        /// Visible to all users enrolled in the course.
        /// </summary>
        Task<List<VideoBookmark>> GetAutoBookmarksByLessonAsync(Guid lessonId, CancellationToken ct = default);

        /// <summary>
        /// Get a single bookmark by ID.
        /// Authorization: User can access their own bookmarks or auto-generated ones.
        /// </summary>
        Task<VideoBookmark?> GetByIdAsync(Guid bookmarkId, Guid requestingUserId, CancellationToken ct = default);

        /// <summary>
        /// Create a new bookmark.
        /// </summary>
        Task<VideoBookmark> CreateBookmarkAsync(Guid userId, CreateVideoBookmarkDto dto, CancellationToken ct = default);

        /// <summary>
        /// Update a bookmark's label.
        /// Authorization: Only owner can update manual bookmarks.
        /// </summary>
        Task<VideoBookmark> UpdateBookmarkAsync(Guid bookmarkId, Guid requestingUserId, UpdateVideoBookmarkDto dto, CancellationToken ct = default);

        /// <summary>
        /// Delete a bookmark.
        /// Authorization: Only owner can delete manual bookmarks. Auto bookmarks cannot be deleted by users.
        /// </summary>
        Task DeleteBookmarkAsync(Guid bookmarkId, Guid requestingUserId, CancellationToken ct = default);

        /// <summary>
        /// Check if a bookmark already exists at a specific timestamp for a user.
        /// Useful to prevent duplicate bookmarks at the same second.
        /// </summary>
        Task<bool> CheckBookmarkExistsAsync(Guid userId, Guid lessonId, int videoTimestamp, CancellationToken ct = default);
    }
}
