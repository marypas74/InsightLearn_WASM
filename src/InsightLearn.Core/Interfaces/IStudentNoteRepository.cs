using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using InsightLearn.Core.Entities;

namespace InsightLearn.Core.Interfaces
{
    /// <summary>
    /// Repository interface for StudentNote entity.
    /// Part of Student Learning Space v2.1.0.
    /// </summary>
    public interface IStudentNoteRepository
    {
        /// <summary>
        /// Get all notes for a specific user and lesson.
        /// </summary>
        Task<List<StudentNote>> GetNotesByUserAndLessonAsync(Guid userId, Guid lessonId, CancellationToken ct = default);

        /// <summary>
        /// Get shared notes for a lesson (community notes).
        /// </summary>
        Task<List<StudentNote>> GetSharedNotesByLessonAsync(Guid lessonId, int limit = 100, CancellationToken ct = default);

        /// <summary>
        /// Get bookmarked notes for a user.
        /// </summary>
        Task<List<StudentNote>> GetBookmarkedNotesByUserAsync(Guid userId, CancellationToken ct = default);

        /// <summary>
        /// Get a single note by ID.
        /// </summary>
        Task<StudentNote?> GetByIdAsync(Guid id, CancellationToken ct = default);

        /// <summary>
        /// Create a new note.
        /// </summary>
        Task<StudentNote> CreateAsync(StudentNote note, CancellationToken ct = default);

        /// <summary>
        /// Update an existing note.
        /// </summary>
        Task<StudentNote> UpdateAsync(StudentNote note, CancellationToken ct = default);

        /// <summary>
        /// Delete a note.
        /// </summary>
        Task DeleteAsync(Guid id, CancellationToken ct = default);

        /// <summary>
        /// Toggle bookmark status for a note.
        /// </summary>
        Task<StudentNote> ToggleBookmarkAsync(Guid noteId, CancellationToken ct = default);

        /// <summary>
        /// Toggle share status for a note.
        /// </summary>
        Task<StudentNote> ToggleShareAsync(Guid noteId, CancellationToken ct = default);
    }
}
