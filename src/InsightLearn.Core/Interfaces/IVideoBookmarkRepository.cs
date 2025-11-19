using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using InsightLearn.Core.Entities;

namespace InsightLearn.Core.Interfaces
{
    /// <summary>
    /// Repository interface for VideoBookmark entity.
    /// Part of Student Learning Space v2.1.0.
    /// </summary>
    public interface IVideoBookmarkRepository
    {
        /// <summary>
        /// Get all bookmarks for a specific user and lesson.
        /// </summary>
        Task<List<VideoBookmark>> GetBookmarksByUserAndLessonAsync(Guid userId, Guid lessonId, CancellationToken ct = default);

        /// <summary>
        /// Get a single bookmark by ID.
        /// </summary>
        Task<VideoBookmark?> GetByIdAsync(Guid id, CancellationToken ct = default);

        /// <summary>
        /// Create a new bookmark.
        /// </summary>
        Task<VideoBookmark> CreateAsync(VideoBookmark bookmark, CancellationToken ct = default);

        /// <summary>
        /// Update an existing bookmark.
        /// </summary>
        Task<VideoBookmark> UpdateAsync(VideoBookmark bookmark, CancellationToken ct = default);

        /// <summary>
        /// Delete a bookmark.
        /// </summary>
        Task DeleteAsync(Guid id, CancellationToken ct = default);

        /// <summary>
        /// Check if a bookmark already exists at a specific timestamp.
        /// </summary>
        Task<bool> ExistsAtTimestampAsync(Guid userId, Guid lessonId, int videoTimestamp, CancellationToken ct = default);
    }
}
