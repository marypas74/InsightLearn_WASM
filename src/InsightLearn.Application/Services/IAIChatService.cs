using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using InsightLearn.Core.DTOs.AIChat;
using InsightLearn.Core.Entities;

namespace InsightLearn.Application.Services
{
    /// <summary>
    /// Service interface for AI Chat functionality.
    /// Integrates Ollama LLM with conversation persistence and video context.
    /// Part of Student Learning Space v2.1.0.
    /// </summary>
    public interface IAIChatService
    {
        /// <summary>
        /// Send a message to the AI chatbot and receive a response.
        /// Creates a new session if SessionId is null.
        /// Enriches context with transcript data if LessonId is provided.
        /// </summary>
        Task<AIChatResponseDto> SendMessageAsync(Guid userId, AIChatMessageDto messageDto, CancellationToken ct = default);

        /// <summary>
        /// Get chat history for a session.
        /// </summary>
        Task<AIConversationHistoryDto?> GetHistoryAsync(Guid sessionId, int limit = 50, CancellationToken ct = default);

        /// <summary>
        /// End a chat session (mark as inactive).
        /// </summary>
        Task EndSessionAsync(Guid userId, Guid sessionId, CancellationToken ct = default);

        /// <summary>
        /// Get all sessions for a user, optionally filtered by lesson.
        /// </summary>
        Task<List<AISessionSummaryDto>> GetSessionsAsync(Guid userId, Guid? lessonId = null, int limit = 50, CancellationToken ct = default);

        /// <summary>
        /// Check if AI chat service is available (Ollama running).
        /// </summary>
        Task<bool> IsAvailableAsync();
    }

    /// <summary>
    /// Summary DTO for AI session listing.
    /// </summary>
    public class AISessionSummaryDto
    {
        public Guid SessionId { get; set; }
        public Guid? LessonId { get; set; }
        public string? LessonTitle { get; set; }
        public int MessageCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastMessageAt { get; set; }
        public bool IsActive { get; set; }
    }
}
