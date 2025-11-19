using System;
using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Core.DTOs.AITakeaways
{
    /// <summary>
    /// DTO for queueing AI takeaway generation.
    /// Part of Student Learning Space v2.1.0.
    /// </summary>
    public class QueueTakeawayDto
    {
        [Required(ErrorMessage = "Lesson ID is required")]
        public Guid LessonId { get; set; }

        /// <summary>
        /// Optional transcript text.
        /// If null, transcript will be fetched from database.
        /// </summary>
        [StringLength(100000, ErrorMessage = "Transcript text must be 100,000 characters or less")]
        public string? TranscriptText { get; set; }
    }
}
