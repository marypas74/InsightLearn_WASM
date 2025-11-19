using System;

namespace InsightLearn.Core.DTOs.StudentNotes
{
    /// <summary>
    /// Student note response DTO.
    /// Used for returning note data from API endpoints.
    /// Part of Student Learning Space v2.1.0.
    /// </summary>
    public class StudentNoteDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid LessonId { get; set; }

        /// <summary>
        /// Video timestamp in seconds where the note was created.
        /// </summary>
        public int VideoTimestamp { get; set; }

        /// <summary>
        /// Note content (Markdown formatted).
        /// </summary>
        public string NoteText { get; set; } = string.Empty;

        /// <summary>
        /// Whether the note is shared with the community.
        /// </summary>
        public bool IsShared { get; set; }

        /// <summary>
        /// Whether the note is bookmarked by the user.
        /// </summary>
        public bool IsBookmarked { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Optional: User display info (for shared notes)
        public string? UserFirstName { get; set; }
        public string? UserLastName { get; set; }
    }
}
