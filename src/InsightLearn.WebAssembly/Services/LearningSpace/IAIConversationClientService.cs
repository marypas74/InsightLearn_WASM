using InsightLearn.WebAssembly.Models;
using InsightLearn.Core.DTOs.AIChat;

namespace InsightLearn.WebAssembly.Services.LearningSpace;

/// <summary>
/// Frontend service for AI Conversation and Chat API.
/// Part of Student Learning Space v2.1.0.
/// Extended for AIChatPanel component support.
/// </summary>
public interface IAIConversationClientService
{
    /// <summary>
    /// Send a message to the AI assistant and get a response.
    /// </summary>
    /// <param name="message">The message DTO with content and context.</param>
    /// <returns>AI response with context information.</returns>
    Task<ApiResponse<AIChatResponseDto>> SendMessageAsync(AIChatMessageDto message);

    /// <summary>
    /// Get chat history for a specific session.
    /// </summary>
    Task<ApiResponse<AIConversationHistoryDto>> GetConversationHistoryAsync(string sessionId);

    /// <summary>
    /// Get all sessions for a specific lesson.
    /// </summary>
    /// <param name="lessonId">The lesson ID.</param>
    /// <returns>List of sessions for the lesson.</returns>
    Task<ApiResponse<List<AIConversationHistoryDto>>> GetSessionsForLessonAsync(Guid lessonId);

    /// <summary>
    /// Delete conversation history for a specific session.
    /// </summary>
    Task<ApiResponse<object>> DeleteConversationAsync(string sessionId);

    /// <summary>
    /// Check if AI chat service is available.
    /// </summary>
    Task<bool> IsAvailableAsync();
}
