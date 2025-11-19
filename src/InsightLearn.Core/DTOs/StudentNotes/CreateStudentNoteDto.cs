using System;
using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Core.DTOs.StudentNotes
{
    /// <summary>
    /// DTO for creating a new student note.
    /// Part of Student Learning Space v2.1.0.
    /// </summary>
    public class CreateStudentNoteDto
    {
        [Required(ErrorMessage = "Lesson ID is required")]
        public Guid LessonId { get; set; }

        /// <summary>
        /// Video timestamp in seconds where the note is created.
        /// Must be non-negative.
        /// </summary>
        [Required(ErrorMessage = "Video timestamp is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Video timestamp must be non-negative")]
        public int VideoTimestamp { get; set; }

        /// <summary>
        /// Note content (Markdown supported).
        /// Max length: 4000 characters.
        /// </summary>
        [Required(ErrorMessage = "Note text is required")]
        [StringLength(4000, MinimumLength = 1, ErrorMessage = "Note text must be between 1 and 4000 characters")]
        public string NoteText { get; set; } = string.Empty;

        /// <summary>
        /// Whether to share this note with the community.
        /// Default: false (private).
        /// </summary>
        public bool IsShared { get; set; } = false;

        /// <summary>
        /// Whether to bookmark this note.
        /// Default: false.
        /// </summary>
        public bool IsBookmarked { get; set; } = false;
    }
}
