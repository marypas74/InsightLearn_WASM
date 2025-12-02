using System;
using System.Collections.Generic;

namespace InsightLearn.Core.DTOs.AITakeaways
{
    /// <summary>
    /// AI-generated key takeaways for a video lesson.
    /// Part of Student Learning Space v2.1.0.
    /// </summary>
    public class VideoKeyTakeawaysDto
    {
        public Guid LessonId { get; set; }

        /// <summary>
        /// List of AI-extracted key takeaways.
        /// </summary>
        public List<TakeawayDto> Takeaways { get; set; } = new();

        /// <summary>
        /// Processing metadata.
        /// </summary>
        public TakeawayMetadataDto? Metadata { get; set; }
    }

    /// <summary>
    /// Single key takeaway with category and relevance.
    /// </summary>
    public class TakeawayDto
    {
        public string TakeawayId { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Category: CoreConcept, BestPractice, Example, Warning, Summary.
        /// </summary>
        public string Category { get; set; } = "CoreConcept";

        /// <summary>
        /// AI relevance score (0.0 - 1.0).
        /// </summary>
        public double RelevanceScore { get; set; }

        public double? TimestampStart { get; set; }
        public double? TimestampEnd { get; set; }

        /// <summary>
        /// User feedback (thumbs up/down).
        /// </summary>
        public int? UserFeedback { get; set; }
    }

    /// <summary>
    /// Takeaway processing metadata.
    /// </summary>
    public class TakeawayMetadataDto
    {
        public int TotalTakeaways { get; set; }
        public string? ProcessingModel { get; set; }
        public DateTime? ProcessedAt { get; set; }

        /// <summary>
        /// Processing status: NotGenerated, Processing, Completed, Failed.
        /// v2.1.0-dev: Added for better frontend UX (return 200 instead of 404).
        /// </summary>
        public string ProcessingStatus { get; set; } = "NotGenerated";
    }
}
