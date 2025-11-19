using System;

namespace InsightLearn.Core.DTOs.AIChat
{
    /// <summary>
    /// AI chatbot response DTO.
    /// Part of Student Learning Space v2.1.0.
    /// </summary>
    public class AIChatResponseDto
    {
        public Guid SessionId { get; set; }
        public string Response { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when response was generated.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Context used for generating the response.
        /// </summary>
        public AIContextInfoDto? Context { get; set; }
    }

    /// <summary>
    /// Context information used by AI for response generation.
    /// </summary>
    public class AIContextInfoDto
    {
        public Guid? LessonId { get; set; }
        public int? VideoTimestamp { get; set; }
        public bool TranscriptUsed { get; set; }
        public bool NotesUsed { get; set; }
        public int TokensUsed { get; set; }
    }
}
