using System;
using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Core.DTOs.AIChat
{
    /// <summary>
    /// DTO for creating a new AI conversation session.
    /// Part of Student Learning Space v2.1.0.
    /// </summary>
    public class CreateAISessionDto
    {
        /// <summary>
        /// Optional lesson context for the session.
        /// </summary>
        public Guid? LessonId { get; set; }

        /// <summary>
        /// Optional initial video timestamp.
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "Video timestamp must be non-negative")]
        public int? CurrentVideoTimestamp { get; set; }
    }

    /// <summary>
    /// Response after creating AI session.
    /// </summary>
    public class AISessionCreatedDto
    {
        public Guid SessionId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
