using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InsightLearn.Core.Entities
{
    /// <summary>
    /// AI conversation session metadata stored in SQL Server.
    /// Full conversation history (messages) is stored in MongoDB.
    /// Part of Student Learning Space v2.1.0 feature.
    /// </summary>
    [Table("AIConversations")]
    public class AIConversation
    {
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// Unique session ID for this conversation.
        /// Used to group related messages.
        /// </summary>
        [Required]
        public Guid SessionId { get; set; }

        /// <summary>
        /// User ID if authenticated, null for anonymous users (free lessons).
        /// Anonymous users are tracked by SessionId instead.
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// Optional lesson context for the conversation.
        /// If null, this is a general conversation.
        /// </summary>
        public Guid? LessonId { get; set; }

        /// <summary>
        /// Current video timestamp when conversation started (in seconds).
        /// Used for contextual AI responses.
        /// </summary>
        public int? CurrentVideoTimestamp { get; set; }

        /// <summary>
        /// MongoDB document _id (ObjectId as string).
        /// Used to retrieve full conversation history from MongoDB.
        /// </summary>
        [MaxLength(100)]
        public string? MongoDocumentId { get; set; }

        /// <summary>
        /// Total number of messages in this conversation.
        /// </summary>
        public int MessageCount { get; set; } = 0;

        /// <summary>
        /// Whether the conversation is still active.
        /// Inactive conversations can be cleaned up after 90 days.
        /// </summary>
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp of the last message in this conversation.
        /// Updated on each new message.
        /// </summary>
        public DateTime? LastMessageAt { get; set; }

        // Navigation properties
        [ForeignKey(nameof(UserId))]
        public virtual User? User { get; set; }

        [ForeignKey(nameof(LessonId))]
        public virtual Lesson? Lesson { get; set; }
    }
}
