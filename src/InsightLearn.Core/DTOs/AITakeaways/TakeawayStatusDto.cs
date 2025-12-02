using System;

namespace InsightLearn.Core.DTOs.AITakeaways
{
    /// <summary>
    /// DTO for AI takeaway processing status.
    /// Part of Student Learning Space v2.1.0.
    /// </summary>
    public class TakeawayStatusDto
    {
        public Guid LessonId { get; set; }

        /// <summary>
        /// Processing status: NotStarted, Queued, Processing, Completed, Failed.
        /// </summary>
        public string Status { get; set; } = "NotStarted";

        /// <summary>
        /// Error message if processing failed.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// When the processing was last updated.
        /// </summary>
        public DateTime? LastUpdated { get; set; }

        /// <summary>
        /// When the takeaways were generated (if completed).
        /// </summary>
        public DateTime? GeneratedAt { get; set; }

        /// <summary>
        /// Number of takeaways generated.
        /// </summary>
        public int? TakeawayCount { get; set; }
    }
}
