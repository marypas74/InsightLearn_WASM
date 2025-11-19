using System;

namespace InsightLearn.Core.DTOs.AITakeaways
{
    /// <summary>
    /// Request DTO for queuing AI takeaway generation.
    /// Part of Student Learning Space v2.1.0.
    /// </summary>
    public class GenerateTakeawayRequestDto
    {
        /// <summary>
        /// Optional: Force regeneration even if takeaways already exist.
        /// </summary>
        public bool ForceRegenerate { get; set; } = false;
    }

    /// <summary>
    /// Takeaway processing status response.
    /// </summary>
    public class TakeawayProcessingStatusDto
    {
        public Guid LessonId { get; set; }
        public string ProcessingStatus { get; set; } = "Pending";
        public int? ProgressPercentage { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime? ProcessedAt { get; set; }
    }
}
