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
    /// Repository implementation for StudentNote entity.
    /// Part of Student Learning Space v2.1.0.
    /// </summary>
    public class StudentNoteRepository : IStudentNoteRepository
    {
        private readonly InsightLearnDbContext _context;

        public StudentNoteRepository(InsightLearnDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<List<StudentNote>> GetNotesByUserAndLessonAsync(Guid userId, Guid lessonId, CancellationToken ct = default)
        {
            return await _context.StudentNotes
                .Where(n => n.UserId == userId && n.LessonId == lessonId)
                .OrderBy(n => n.VideoTimestamp)
                .ToListAsync(ct);
        }

        public async Task<List<StudentNote>> GetSharedNotesByLessonAsync(Guid lessonId, int limit = 100, CancellationToken ct = default)
        {
            return await _context.StudentNotes
                .Include(n => n.User) // Include user info for display
                .Where(n => n.LessonId == lessonId && n.IsShared)
                .OrderBy(n => n.VideoTimestamp)
                .Take(limit)
                .ToListAsync(ct);
        }

        public async Task<List<StudentNote>> GetBookmarkedNotesByUserAsync(Guid userId, CancellationToken ct = default)
        {
            return await _context.StudentNotes
                .Where(n => n.UserId == userId && n.IsBookmarked)
                .OrderByDescending(n => n.UpdatedAt)
                .ToListAsync(ct);
        }

        public async Task<StudentNote?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return await _context.StudentNotes
                .Include(n => n.User)
                .Include(n => n.Lesson)
                .FirstOrDefaultAsync(n => n.Id == id, ct);
        }

        public async Task<StudentNote> CreateAsync(StudentNote note, CancellationToken ct = default)
        {
            note.Id = Guid.NewGuid();
            note.CreatedAt = DateTime.UtcNow;
            note.UpdatedAt = DateTime.UtcNow;

            _context.StudentNotes.Add(note);
            await _context.SaveChangesAsync(ct);

            return note;
        }

        public async Task<StudentNote> UpdateAsync(StudentNote note, CancellationToken ct = default)
        {
            note.UpdatedAt = DateTime.UtcNow;

            _context.StudentNotes.Update(note);
            await _context.SaveChangesAsync(ct);

            return note;
        }

        public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var note = await _context.StudentNotes.FindAsync(new object[] { id }, ct);
            if (note != null)
            {
                _context.StudentNotes.Remove(note);
                await _context.SaveChangesAsync(ct);
            }
        }

        public async Task<StudentNote> ToggleBookmarkAsync(Guid noteId, CancellationToken ct = default)
        {
            var note = await _context.StudentNotes.FindAsync(new object[] { noteId }, ct);
            if (note == null)
                throw new KeyNotFoundException($"Note with ID {noteId} not found");

            note.IsBookmarked = !note.IsBookmarked;
            note.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);

            return note;
        }

        public async Task<StudentNote> ToggleShareAsync(Guid noteId, CancellationToken ct = default)
        {
            var note = await _context.StudentNotes.FindAsync(new object[] { noteId }, ct);
            if (note == null)
                throw new KeyNotFoundException($"Note with ID {noteId} not found");

            note.IsShared = !note.IsShared;
            note.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);

            return note;
        }
    }
}
