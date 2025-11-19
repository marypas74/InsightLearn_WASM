using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using InsightLearn.Core.DTOs.VideoBookmarks;
using InsightLearn.Core.Entities;
using InsightLearn.Core.Interfaces;

namespace InsightLearn.Application.Services
{
    /// <summary>
    /// Service for video bookmark management.
    /// Provides authorization and business logic over repository layer.
    /// Part of Student Learning Space v2.1.0.
    /// </summary>
    public class VideoBookmarkService : IVideoBookmarkService
    {
        private readonly IVideoBookmarkRepository _repository;
        private readonly ILogger<VideoBookmarkService> _logger;

        public VideoBookmarkService(
            IVideoBookmarkRepository repository,
            ILogger<VideoBookmarkService> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<VideoBookmark>> GetBookmarksByUserAndLessonAsync(Guid userId, Guid lessonId, CancellationToken ct = default)
        {
            _logger.LogDebug("Getting bookmarks for user {UserId} in lesson {LessonId}", userId, lessonId);
            return await _repository.GetBookmarksByUserAndLessonAsync(userId, lessonId, ct);
        }

        public async Task<List<VideoBookmark>> GetAutoBookmarksByLessonAsync(Guid lessonId, CancellationToken ct = default)
        {
            _logger.LogDebug("Getting auto-generated bookmarks for lesson {LessonId}", lessonId);

            // Auto bookmarks are created by the system (AI chapter markers)
            // We need to get all bookmarks for the lesson and filter by BookmarkType = "Auto"
            // Since repository doesn't have a method for this, we'll need to implement it differently

            // For now, we'll use a workaround: get bookmarks for a system user (Guid.Empty)
            // In production, consider adding a GetAutoBookmarksByLessonAsync method to repository
            // or filtering in the database layer for better performance

            // NOTE: This is a placeholder implementation. In a real scenario, you should:
            // 1. Add a repository method: GetBookmarksByLessonAndTypeAsync(lessonId, "Auto")
            // 2. Or use a dedicated system user ID for auto bookmarks

            var allBookmarks = await _repository.GetBookmarksByUserAndLessonAsync(Guid.Empty, lessonId, ct);
            return allBookmarks.Where(b => b.BookmarkType == "Auto").ToList();
        }

        public async Task<VideoBookmark?> GetByIdAsync(Guid bookmarkId, Guid requestingUserId, CancellationToken ct = default)
        {
            var bookmark = await _repository.GetByIdAsync(bookmarkId, ct);

            if (bookmark == null)
                return null;

            // Authorization: user can access their own bookmarks or auto-generated ones
            if (bookmark.UserId != requestingUserId && bookmark.BookmarkType != "Auto")
            {
                _logger.LogWarning("User {UserId} attempted to access unauthorized bookmark {BookmarkId}",
                    requestingUserId, bookmarkId);
                return null;
            }

            return bookmark;
        }

        public async Task<VideoBookmark> CreateBookmarkAsync(Guid userId, CreateVideoBookmarkDto dto, CancellationToken ct = default)
        {
            _logger.LogInformation("Creating bookmark for user {UserId} in lesson {LessonId} at timestamp {Timestamp}",
                userId, dto.LessonId, dto.VideoTimestamp);

            // Check if bookmark already exists at this timestamp
            var exists = await _repository.ExistsAtTimestampAsync(userId, dto.LessonId, dto.VideoTimestamp, ct);
            if (exists)
            {
                _logger.LogWarning("Bookmark already exists for user {UserId} at timestamp {Timestamp} in lesson {LessonId}",
                    userId, dto.VideoTimestamp, dto.LessonId);
                throw new InvalidOperationException($"Bookmark already exists at timestamp {dto.VideoTimestamp}");
            }

            var bookmark = new VideoBookmark
            {
                UserId = userId,
                LessonId = dto.LessonId,
                VideoTimestamp = dto.VideoTimestamp,
                Label = dto.Label,
                BookmarkType = dto.BookmarkType
            };

            return await _repository.CreateAsync(bookmark, ct);
        }

        public async Task<VideoBookmark> UpdateBookmarkAsync(Guid bookmarkId, Guid requestingUserId, UpdateVideoBookmarkDto dto, CancellationToken ct = default)
        {
            var bookmark = await _repository.GetByIdAsync(bookmarkId, ct);

            if (bookmark == null)
                throw new KeyNotFoundException($"Bookmark {bookmarkId} not found");

            // Authorization: only owner can update manual bookmarks
            if (bookmark.UserId != requestingUserId)
                throw new UnauthorizedAccessException($"User {requestingUserId} cannot update bookmark {bookmarkId}");

            // Auto bookmarks cannot be modified by users
            if (bookmark.BookmarkType == "Auto")
                throw new InvalidOperationException("Auto-generated bookmarks cannot be modified");

            _logger.LogInformation("Updating bookmark {BookmarkId} for user {UserId}", bookmarkId, requestingUserId);

            // Update label if provided
            if (dto.Label != null)
                bookmark.Label = dto.Label;

            return await _repository.UpdateAsync(bookmark, ct);
        }

        public async Task DeleteBookmarkAsync(Guid bookmarkId, Guid requestingUserId, CancellationToken ct = default)
        {
            var bookmark = await _repository.GetByIdAsync(bookmarkId, ct);

            if (bookmark == null)
                throw new KeyNotFoundException($"Bookmark {bookmarkId} not found");

            // Authorization: only owner can delete manual bookmarks
            if (bookmark.UserId != requestingUserId)
                throw new UnauthorizedAccessException($"User {requestingUserId} cannot delete bookmark {bookmarkId}");

            // Auto bookmarks cannot be deleted by users
            if (bookmark.BookmarkType == "Auto")
            {
                _logger.LogWarning("User {UserId} attempted to delete auto bookmark {BookmarkId}",
                    requestingUserId, bookmarkId);
                throw new InvalidOperationException("Auto-generated bookmarks cannot be deleted by users");
            }

            _logger.LogInformation("Deleting bookmark {BookmarkId} for user {UserId}", bookmarkId, requestingUserId);

            await _repository.DeleteAsync(bookmarkId, ct);
        }

        public async Task<bool> CheckBookmarkExistsAsync(Guid userId, Guid lessonId, int videoTimestamp, CancellationToken ct = default)
        {
            _logger.LogDebug("Checking if bookmark exists for user {UserId} at timestamp {Timestamp} in lesson {LessonId}",
                userId, videoTimestamp, lessonId);

            return await _repository.ExistsAtTimestampAsync(userId, lessonId, videoTimestamp, ct);
        }
    }
}
