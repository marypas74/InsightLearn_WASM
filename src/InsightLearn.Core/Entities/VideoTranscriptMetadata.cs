using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InsightLearn.Core.Entities
{
    /// <summary>
    /// Video transcript metadata stored in SQL Server.
    /// Full transcript data (large text) is stored in MongoDB for performance.
    /// Part of Student Learning Space v2.1.0 feature.
    /// </summary>
    [Table("VideoTranscriptMetadata")]
    public class VideoTranscriptMetadata
    {
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// Lesson ID (one transcript per lesson).
        /// Unique constraint enforced.
        /// </summary>
        [Required]
        public Guid LessonId { get; set; }

        /// <summary>
        /// MongoDB document _id (ObjectId as string).
        /// Used to retrieve full transcript from MongoDB.
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string MongoDocumentId { get; set; } = string.Empty;

        /// <summary>
        /// Language code (e.g., "en-US", "it-IT").
        /// Default: en-US.
        /// </summary>
        [Required]
        [MaxLength(10)]
        public string Language { get; set; } = "en-US";

        /// <summary>
        /// Processing status: Pending, Processing, Completed, Failed.
        /// Maps to 'Status' column in database.
        /// </summary>
        [Required]
        [MaxLength(50)]
        [Column("Status")]
        public string Status { get; set; } = "Pending";

        /// <summary>
        /// Number of segments in transcript.
        /// </summary>
        public int? SegmentCount { get; set; }

        /// <summary>
        /// Video duration in seconds.
        /// </summary>
        public int? DurationSeconds { get; set; }

        /// <summary>
        /// Timestamp when transcript was generated.
        /// Maps to 'GeneratedAt' column in database.
        /// </summary>
        [Column("GeneratedAt")]
        public DateTime? GeneratedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey(nameof(LessonId))]
        public virtual Lesson? Lesson { get; set; }
    }
}
