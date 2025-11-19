using InsightLearn.WebAssembly.Models;
using InsightLearn.Core.DTOs.AIChat;

namespace InsightLearn.WebAssembly.Services.LearningSpace;

/// <summary>
/// Frontend service for AI Conversation History API.
/// Part of Student Learning Space v2.1.0.
/// </summary>
public interface IAIConversationClientService
{
    /// <summary>
    /// Get chat history for a specific session.
    /// </summary>
    Task<ApiResponse<AIConversationHistoryDto>> GetConversationHistoryAsync(string sessionId);

    /// <summary>
    /// Delete conversation history for a specific session.
    /// </summary>
    Task<ApiResponse<object>> DeleteConversationAsync(string sessionId);
}
