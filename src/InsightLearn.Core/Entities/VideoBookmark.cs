using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InsightLearn.Core.Entities
{
    /// <summary>
    /// Video bookmark entity for marking important timestamps in video lessons.
    /// Part of Student Learning Space v2.1.0 feature.
    /// </summary>
    [Table("VideoBookmarks")]
    public class VideoBookmark
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public Guid LessonId { get; set; }

        /// <summary>
        /// Video timestamp in seconds where the bookmark is placed.
        /// </summary>
        [Required]
        public int VideoTimestamp { get; set; }

        /// <summary>
        /// Optional custom label for the bookmark.
        /// Max length: 200 characters.
        /// </summary>
        [MaxLength(200)]
        public string? Label { get; set; }

        /// <summary>
        /// Type of bookmark: Manual (user-created) or Auto (AI-generated from key takeaways).
        /// Default: Manual.
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string BookmarkType { get; set; } = "Manual";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey(nameof(UserId))]
        public virtual User? User { get; set; }

        [ForeignKey(nameof(LessonId))]
        public virtual Lesson? Lesson { get; set; }
    }
}
