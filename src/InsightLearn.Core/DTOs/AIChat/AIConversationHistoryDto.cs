using System;
using System.Collections.Generic;

namespace InsightLearn.Core.DTOs.AIChat
{
    /// <summary>
    /// AI conversation history response DTO.
    /// Part of Student Learning Space v2.1.0.
    /// </summary>
    public class AIConversationHistoryDto
    {
        public Guid SessionId { get; set; }
        public Guid UserId { get; set; }
        public Guid? LessonId { get; set; }
        public int MessageCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastMessageAt { get; set; }

        /// <summary>
        /// List of conversation messages.
        /// </summary>
        public List<ConversationMessageDto> Messages { get; set; } = new();
    }

    /// <summary>
    /// Single conversation message.
    /// </summary>
    public class ConversationMessageDto
    {
        public string MessageId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Role: user, assistant, or system.
        /// </summary>
        public string Role { get; set; } = "user";

        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Optional video timestamp context.
        /// </summary>
        public int? VideoTimestamp { get; set; }
    }
}
