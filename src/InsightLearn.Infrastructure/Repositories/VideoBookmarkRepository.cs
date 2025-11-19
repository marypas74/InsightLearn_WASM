using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using InsightLearn.Core.Entities;
using InsightLearn.Core.Interfaces;
using InsightLearn.Infrastructure.Data;

namespace InsightLearn.Infrastructure.Repositories
{
    /// <summary>
    /// Repository implementation for VideoBookmark entity.
    /// Part of Student Learning Space v2.1.0.
    /// </summary>
    public class VideoBookmarkRepository : IVideoBookmarkRepository
    {
        private readonly InsightLearnDbContext _context;

        public VideoBookmarkRepository(InsightLearnDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<List<VideoBookmark>> GetBookmarksByUserAndLessonAsync(Guid userId, Guid lessonId, CancellationToken ct = default)
        {
            return await _context.VideoBookmarks
                .Where(b => b.UserId == userId && b.LessonId == lessonId)
                .OrderBy(b => b.VideoTimestamp)
                .ToListAsync(ct);
        }

        public async Task<VideoBookmark?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return await _context.VideoBookmarks
                .Include(b => b.User)
                .Include(b => b.Lesson)
                .FirstOrDefaultAsync(b => b.Id == id, ct);
        }

        public async Task<VideoBookmark> CreateAsync(VideoBookmark bookmark, CancellationToken ct = default)
        {
            bookmark.Id = Guid.NewGuid();
            bookmark.CreatedAt = DateTime.UtcNow;

            _context.VideoBookmarks.Add(bookmark);
            await _context.SaveChangesAsync(ct);

            return bookmark;
        }

        public async Task<VideoBookmark> UpdateAsync(VideoBookmark bookmark, CancellationToken ct = default)
        {
            _context.VideoBookmarks.Update(bookmark);
            await _context.SaveChangesAsync(ct);

            return bookmark;
        }

        public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var bookmark = await _context.VideoBookmarks.FindAsync(new object[] { id }, ct);
            if (bookmark != null)
            {
                _context.VideoBookmarks.Remove(bookmark);
                await _context.SaveChangesAsync(ct);
            }
        }

        public async Task<bool> ExistsAtTimestampAsync(Guid userId, Guid lessonId, int videoTimestamp, CancellationToken ct = default)
        {
            return await _context.VideoBookmarks
                .AnyAsync(b => b.UserId == userId &&
                              b.LessonId == lessonId &&
                              b.VideoTimestamp == videoTimestamp, ct);
        }
    }
}
