using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InsightLearn.Core.Entities
{
    /// <summary>
    /// Student note entity for timestamped notes during video playback.
    /// Part of Student Learning Space v2.1.0 feature.
    /// </summary>
    [Table("StudentNotes")]
    public class StudentNote
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public Guid LessonId { get; set; }

        /// <summary>
        /// Video timestamp in seconds where the note was created.
        /// </summary>
        [Required]
        public int VideoTimestamp { get; set; }

        /// <summary>
        /// Note content (supports Markdown formatting).
        /// Max length: 4000 characters.
        /// </summary>
        [Required]
        [MaxLength(4000)]
        public string NoteText { get; set; } = string.Empty;

        /// <summary>
        /// Whether the note is shared with the community.
        /// Default: false (private).
        /// </summary>
        public bool IsShared { get; set; } = false;

        /// <summary>
        /// Whether the note is bookmarked by the user.
        /// Default: false.
        /// </summary>
        public bool IsBookmarked { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey(nameof(UserId))]
        public virtual User? User { get; set; }

        [ForeignKey(nameof(LessonId))]
        public virtual Lesson? Lesson { get; set; }
    }
}
