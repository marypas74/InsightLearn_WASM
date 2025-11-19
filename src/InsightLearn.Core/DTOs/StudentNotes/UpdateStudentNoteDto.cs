using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Core.DTOs.StudentNotes
{
    /// <summary>
    /// DTO for updating an existing student note.
    /// All fields are optional (partial update support).
    /// Part of Student Learning Space v2.1.0.
    /// </summary>
    public class UpdateStudentNoteDto
    {
        /// <summary>
        /// Updated note content (Markdown supported).
        /// If null, note text won't be updated.
        /// </summary>
        [StringLength(4000, MinimumLength = 1, ErrorMessage = "Note text must be between 1 and 4000 characters")]
        public string? NoteText { get; set; }

        /// <summary>
        /// Update share status.
        /// If null, share status won't be updated.
        /// </summary>
        public bool? IsShared { get; set; }

        /// <summary>
        /// Update bookmark status.
        /// If null, bookmark status won't be updated.
        /// </summary>
        public bool? IsBookmarked { get; set; }
    }
}
