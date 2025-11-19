using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Core.DTOs.AITakeaways
{
    /// <summary>
    /// DTO for submitting feedback on AI takeaways (thumbs up/down).
    /// Part of Student Learning Space v2.1.0.
    /// </summary>
    public class CreateTakeawayFeedbackDto
    {
        [Required(ErrorMessage = "Takeaway ID is required")]
        public string TakeawayId { get; set; } = string.Empty;

        /// <summary>
        /// Feedback value: 1 for thumbs up, -1 for thumbs down.
        /// </summary>
        [Required(ErrorMessage = "Feedback value is required")]
        [Range(-1, 1, ErrorMessage = "Feedback must be -1 (thumbs down) or 1 (thumbs up)")]
        public int Feedback { get; set; }
    }
}
