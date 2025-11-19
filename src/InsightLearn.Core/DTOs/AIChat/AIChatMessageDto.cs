using System;
using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Core.DTOs.AIChat
{
    /// <summary>
    /// DTO for sending a message to AI chatbot.
    /// Part of Student Learning Space v2.1.0.
    /// </summary>
    public class AIChatMessageDto
    {
        [Required(ErrorMessage = "Message content is required")]
        [StringLength(2000, MinimumLength = 1, ErrorMessage = "Message must be between 1 and 2000 characters")]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Session ID for conversation continuity.
        /// If null, a new session will be created.
        /// </summary>
        public Guid? SessionId { get; set; }

        /// <summary>
        /// Optional lesson context for AI responses.
        /// </summary>
        public Guid? LessonId { get; set; }

        /// <summary>
        /// Current video timestamp (in seconds) for contextual responses.
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "Video timestamp must be non-negative")]
        public int? VideoTimestamp { get; set; }
    }
}
