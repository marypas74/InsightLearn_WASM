using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using InsightLearn.Core.DTOs.StudentNotes;
using InsightLearn.Core.Entities;
using InsightLearn.Core.Interfaces;

namespace InsightLearn.Application.Services
{
    /// <summary>
    /// Service for student note management.
    /// Provides authorization and business logic over repository layer.
    /// Part of Student Learning Space v2.1.0.
    /// </summary>
    public class StudentNoteService : IStudentNoteService
    {
        private readonly IStudentNoteRepository _repository;
        private readonly ILogger<StudentNoteService> _logger;

        public StudentNoteService(
            IStudentNoteRepository repository,
            ILogger<StudentNoteService> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<StudentNote>> GetNotesByUserAndLessonAsync(Guid userId, Guid lessonId, CancellationToken ct = default)
        {
            _logger.LogDebug("Getting notes for user {UserId} in lesson {LessonId}", userId, lessonId);
            return await _repository.GetNotesByUserAndLessonAsync(userId, lessonId, ct);
        }

        public async Task<List<StudentNote>> GetSharedNotesByLessonAsync(Guid lessonId, int limit = 100, CancellationToken ct = default)
        {
            _logger.LogDebug("Getting shared notes for lesson {LessonId}, limit {Limit}", lessonId, limit);
            return await _repository.GetSharedNotesByLessonAsync(lessonId, limit, ct);
        }

        public async Task<List<StudentNote>> GetBookmarkedNotesByUserAsync(Guid userId, CancellationToken ct = default)
        {
            _logger.LogDebug("Getting bookmarked notes for user {UserId}", userId);
            return await _repository.GetBookmarkedNotesByUserAsync(userId, ct);
        }

        public async Task<StudentNote?> GetByIdAsync(Guid noteId, Guid requestingUserId, CancellationToken ct = default)
        {
            var note = await _repository.GetByIdAsync(noteId, ct);

            if (note == null)
                return null;

            // Authorization: user can access their own notes or shared notes
            if (note.UserId != requestingUserId && !note.IsShared)
            {
                _logger.LogWarning("User {UserId} attempted to access unauthorized note {NoteId}", requestingUserId, noteId);
                return null;
            }

            return note;
        }

        public async Task<StudentNote> CreateNoteAsync(Guid userId, CreateStudentNoteDto dto, CancellationToken ct = default)
        {
            _logger.LogInformation("Creating note for user {UserId} in lesson {LessonId} at timestamp {Timestamp}",
                userId, dto.LessonId, dto.VideoTimestamp);

            var note = new StudentNote
            {
                UserId = userId,
                LessonId = dto.LessonId,
                VideoTimestamp = dto.VideoTimestamp,
                NoteText = dto.NoteText,
                IsShared = dto.IsShared,
                IsBookmarked = dto.IsBookmarked
            };

            return await _repository.CreateAsync(note, ct);
        }

        public async Task<StudentNote> UpdateNoteAsync(Guid noteId, Guid requestingUserId, UpdateStudentNoteDto dto, CancellationToken ct = default)
        {
            var note = await _repository.GetByIdAsync(noteId, ct);

            if (note == null)
                throw new KeyNotFoundException($"Note {noteId} not found");

            // Authorization: only owner can update
            if (note.UserId != requestingUserId)
                throw new UnauthorizedAccessException($"User {requestingUserId} cannot update note {noteId}");

            _logger.LogInformation("Updating note {NoteId} for user {UserId}", noteId, requestingUserId);

            // Update fields
            if (dto.NoteText != null)
                note.NoteText = dto.NoteText;

            if (dto.IsShared.HasValue)
                note.IsShared = dto.IsShared.Value;

            if (dto.IsBookmarked.HasValue)
                note.IsBookmarked = dto.IsBookmarked.Value;

            return await _repository.UpdateAsync(note, ct);
        }

        public async Task DeleteNoteAsync(Guid noteId, Guid requestingUserId, CancellationToken ct = default)
        {
            var note = await _repository.GetByIdAsync(noteId, ct);

            if (note == null)
                throw new KeyNotFoundException($"Note {noteId} not found");

            // Authorization: only owner can delete
            if (note.UserId != requestingUserId)
                throw new UnauthorizedAccessException($"User {requestingUserId} cannot delete note {noteId}");

            _logger.LogInformation("Deleting note {NoteId} for user {UserId}", noteId, requestingUserId);

            await _repository.DeleteAsync(noteId, ct);
        }

        public async Task<StudentNote> ToggleBookmarkAsync(Guid noteId, Guid requestingUserId, CancellationToken ct = default)
        {
            var note = await _repository.GetByIdAsync(noteId, ct);

            if (note == null)
                throw new KeyNotFoundException($"Note {noteId} not found");

            // Authorization: only owner can toggle bookmark
            if (note.UserId != requestingUserId)
                throw new UnauthorizedAccessException($"User {requestingUserId} cannot modify note {noteId}");

            _logger.LogInformation("Toggling bookmark for note {NoteId}", noteId);

            return await _repository.ToggleBookmarkAsync(noteId, ct);
        }

        public async Task<StudentNote> ToggleShareAsync(Guid noteId, Guid requestingUserId, CancellationToken ct = default)
        {
            var note = await _repository.GetByIdAsync(noteId, ct);

            if (note == null)
                throw new KeyNotFoundException($"Note {noteId} not found");

            // Authorization: only owner can toggle share
            if (note.UserId != requestingUserId)
                throw new UnauthorizedAccessException($"User {requestingUserId} cannot modify note {noteId}");

            _logger.LogInformation("Toggling share for note {NoteId}", noteId);

            return await _repository.ToggleShareAsync(noteId, ct);
        }
    }
}
