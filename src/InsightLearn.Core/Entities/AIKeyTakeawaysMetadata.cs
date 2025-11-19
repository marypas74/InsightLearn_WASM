using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InsightLearn.Core.Entities
{
    /// <summary>
    /// AI-generated key takeaways metadata stored in SQL Server.
    /// Full takeaway data is stored in MongoDB for flexibility.
    /// Part of Student Learning Space v2.1.0 feature.
    /// </summary>
    [Table("AIKeyTakeawaysMetadata")]
    public class AIKeyTakeawaysMetadata
    {
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// Lesson ID (one set of takeaways per lesson).
        /// Unique constraint enforced.
        /// </summary>
        [Required]
        public Guid LessonId { get; set; }

        /// <summary>
        /// MongoDB document _id (ObjectId as string).
        /// Used to retrieve full takeaways from MongoDB.
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string MongoDocumentId { get; set; } = string.Empty;

        /// <summary>
        /// Total number of takeaways extracted.
        /// </summary>
        [Required]
        public int TakeawayCount { get; set; } = 0;

        /// <summary>
        /// Processing status: Pending, Processing, Completed, Failed.
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string ProcessingStatus { get; set; } = "Pending";

        /// <summary>
        /// Timestamp when AI processing completed.
        /// </summary>
        public DateTime? ProcessedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey(nameof(LessonId))]
        public virtual Lesson? Lesson { get; set; }
    }
}
