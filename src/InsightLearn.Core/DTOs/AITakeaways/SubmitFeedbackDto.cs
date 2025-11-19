using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Core.DTOs.AITakeaways
{
    /// <summary>
    /// DTO for submitting feedback on AI takeaways.
    /// Part of Student Learning Space v2.1.0.
    /// </summary>
    public class SubmitFeedbackDto
    {
        [Required(ErrorMessage = "Takeaway ID is required")]
        [StringLength(100, ErrorMessage = "Takeaway ID must be 100 characters or less")]
        public string TakeawayId { get; set; } = string.Empty;

        /// <summary>
        /// Feedback value:
        /// 1 = Thumbs up (helpful)
        /// -1 = Thumbs down (not helpful)
        /// 0 = Neutral / Clear feedback
        /// </summary>
        [Required(ErrorMessage = "Feedback is required")]
        [Range(-1, 1, ErrorMessage = "Feedback must be -1 (down), 0 (neutral), or 1 (up)")]
        public int Feedback { get; set; }
    }
}
