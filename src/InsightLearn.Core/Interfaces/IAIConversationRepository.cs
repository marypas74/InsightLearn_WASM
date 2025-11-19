using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using InsightLearn.Core.Entities;
using InsightLearn.Core.DTOs.AIChat;

namespace InsightLearn.Core.Interfaces
{
    /// <summary>
    /// Hybrid repository interface for AI Conversations (SQL Server metadata + MongoDB history).
    /// Part of Student Learning Space v2.1.0.
    /// </summary>
    public interface IAIConversationRepository
    {
        /// <summary>
        /// Get conversation with full message history (SQL metadata + MongoDB messages).
        /// </summary>
        Task<AIConversationHistoryDto?> GetConversationHistoryAsync(Guid sessionId, int limit = 50, CancellationToken ct = default);

        /// <summary>
        /// Get only metadata from SQL Server.
        /// </summary>
        Task<AIConversation?> GetConversationMetadataAsync(Guid sessionId, CancellationToken ct = default);

        /// <summary>
        /// List all conversations for a user.
        /// </summary>
        Task<List<AIConversation>> GetUserConversationsAsync(Guid userId, int limit = 50, CancellationToken ct = default);

        /// <summary>
        /// List all conversations for a specific lesson.
        /// </summary>
        Task<List<AIConversation>> GetLessonConversationsAsync(Guid lessonId, int limit = 50, CancellationToken ct = default);

        /// <summary>
        /// Create new conversation (SQL metadata + MongoDB collection).
        /// </summary>
        Task<AIConversation> CreateConversationAsync(Guid userId, Guid? lessonId = null, int? videoTimestamp = null, CancellationToken ct = default);

        /// <summary>
        /// Add message to conversation (MongoDB + update SQL metadata).
        /// </summary>
        Task AddMessageAsync(Guid sessionId, string role, string content, int? videoTimestamp = null, CancellationToken ct = default);

        /// <summary>
        /// End conversation (mark as inactive).
        /// </summary>
        Task EndConversationAsync(Guid sessionId, CancellationToken ct = default);

        /// <summary>
        /// Delete conversation (SQL metadata + MongoDB messages).
        /// </summary>
        Task DeleteConversationAsync(Guid sessionId, CancellationToken ct = default);

        /// <summary>
        /// Delete old inactive conversations (cleanup job).
        /// </summary>
        Task<int> DeleteOldConversationsAsync(int daysOld = 90, CancellationToken ct = default);
    }
}
