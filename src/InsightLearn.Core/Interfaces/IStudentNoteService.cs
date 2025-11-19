using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using InsightLearn.Core.DTOs.StudentNotes;
using InsightLearn.Core.Entities;

namespace InsightLearn.Core.Interfaces
{
    /// <summary>
    /// Service interface for student note management.
    /// Provides business logic layer over IStudentNoteRepository.
    /// Part of Student Learning Space v2.1.0.
    /// </summary>
    public interface IStudentNoteService
    {
        /// <summary>
        /// Get all notes for a user in a specific lesson.
        /// </summary>
        Task<List<StudentNote>> GetNotesByUserAndLessonAsync(Guid userId, Guid lessonId, CancellationToken ct = default);

        /// <summary>
        /// Get shared notes for a lesson (from all users).
        /// </summary>
        Task<List<StudentNote>> GetSharedNotesByLessonAsync(Guid lessonId, int limit = 100, CancellationToken ct = default);

        /// <summary>
        /// Get bookmarked notes for a user across all lessons.
        /// </summary>
        Task<List<StudentNote>> GetBookmarkedNotesByUserAsync(Guid userId, CancellationToken ct = default);

        /// <summary>
        /// Get note by ID with authorization check.
        /// Returns null if note doesn't exist or user doesn't have access.
        /// </summary>
        Task<StudentNote?> GetByIdAsync(Guid noteId, Guid requestingUserId, CancellationToken ct = default);

        /// <summary>
        /// Create a new note.
        /// </summary>
        Task<StudentNote> CreateNoteAsync(Guid userId, CreateStudentNoteDto dto, CancellationToken ct = default);

        /// <summary>
        /// Update an existing note.
        /// Authorization: only note owner can update.
        /// </summary>
        Task<StudentNote> UpdateNoteAsync(Guid noteId, Guid requestingUserId, UpdateStudentNoteDto dto, CancellationToken ct = default);

        /// <summary>
        /// Delete a note.
        /// Authorization: only note owner can delete.
        /// </summary>
        Task DeleteNoteAsync(Guid noteId, Guid requestingUserId, CancellationToken ct = default);

        /// <summary>
        /// Toggle bookmark status for a note.
        /// </summary>
        Task<StudentNote> ToggleBookmarkAsync(Guid noteId, Guid requestingUserId, CancellationToken ct = default);

        /// <summary>
        /// Toggle share status for a note.
        /// </summary>
        Task<StudentNote> ToggleShareAsync(Guid noteId, Guid requestingUserId, CancellationToken ct = default);
    }
}
