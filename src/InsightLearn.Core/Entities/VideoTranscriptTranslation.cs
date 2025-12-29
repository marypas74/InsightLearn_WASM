using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InsightLearn.Core.Entities
{
    /// <summary>
    /// Video transcript translation metadata stored in SQL Server.
    /// Full translated subtitle content (WebVTT) is stored in MongoDB for performance.
    /// Part of Phase 8: Multi-Language Subtitle Support - LinkedIn Learning parity.
    /// </summary>
    [Table("VideoTranscriptTranslations")]
    public class VideoTranscriptTranslation
    {
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// Lesson ID (one lesson can have multiple translations).
        /// </summary>
        [Required]
        public Guid LessonId { get; set; }

        /// <summary>
        /// Source language code (ISO 639-1, e.g., "en", "it").
        /// Default: "en" (English).
        /// </summary>
        [Required]
        [MaxLength(10)]
        public string SourceLanguage { get; set; } = "en";

        /// <summary>
        /// Target language code (ISO 639-1, e.g., "es", "fr", "de").
        /// </summary>
        [Required]
        [MaxLength(10)]
        public string TargetLanguage { get; set; } = string.Empty;

        /// <summary>
        /// MongoDB document _id (ObjectId as string).
        /// Used to retrieve full translated subtitle content from MongoDB.
        /// Collection: AzureTranslatedSubtitles or OllamaTranslatedSubtitles.
        /// </summary>
        [MaxLength(100)]
        public string? MongoDocumentId { get; set; }

        /// <summary>
        /// Translation status: Pending, Processing, Completed, Failed.
        /// Maps to 'Status' column in database.
        /// </summary>
        [Required]
        [MaxLength(50)]
        [Column("Status")]
        public string Status { get; set; } = "Pending";

        /// <summary>
        /// Translation quality tier: Auto/Ollama, Auto/Azure, Professional, Human-Verified.
        /// Determines which translation service was used.
        /// Maps to 'QualityTier' column in database.
        /// </summary>
        [Required]
        [MaxLength(50)]
        [Column("QualityTier")]
        public string QualityTier { get; set; } = "Auto/Ollama";

        /// <summary>
        /// Total number of segments translated.
        /// </summary>
        public int? SegmentCount { get; set; }

        /// <summary>
        /// Total characters translated (for cost calculation).
        /// Azure charges $10 per 1M characters.
        /// </summary>
        public int? TotalCharacters { get; set; }

        /// <summary>
        /// Estimated cost in USD (for Azure translations).
        /// Ollama is free, so cost is 0.
        /// </summary>
        [Column(TypeName = "decimal(10,4)")]
        public decimal EstimatedCost { get; set; } = 0.0m;

        /// <summary>
        /// Error message if translation failed.
        /// </summary>
        [MaxLength(1000)]
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Timestamp when translation was completed.
        /// Maps to 'CompletedAt' column in database.
        /// </summary>
        [Column("CompletedAt")]
        public DateTime? CompletedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey(nameof(LessonId))]
        public virtual Lesson? Lesson { get; set; }
    }
}
