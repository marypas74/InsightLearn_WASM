using InsightLearn.WebAssembly.Models;
using InsightLearn.WebAssembly.Services.Http;
using InsightLearn.Core.DTOs.AIChat;

namespace InsightLearn.WebAssembly.Services.LearningSpace;

/// <summary>
/// Implementation of AI Conversation and Chat client service.
/// Part of Student Learning Space v2.1.0.
/// Extended for AIChatPanel component support.
/// </summary>
public class AIConversationClientService : IAIConversationClientService
{
    private readonly IApiClient _apiClient;

    public AIConversationClientService(IApiClient apiClient)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
    }

    /// <summary>
    /// Send a message to the AI assistant and get a response.
    /// </summary>
    public async Task<ApiResponse<AIChatResponseDto>> SendMessageAsync(AIChatMessageDto message)
    {
        if (message == null || string.IsNullOrWhiteSpace(message.Message))
        {
            return new ApiResponse<AIChatResponseDto>
            {
                Success = false,
                Message = "Message content is required"
            };
        }

        try
        {
            return await _apiClient.PostAsync<AIChatResponseDto>("api/ai-chat/message", message);
        }
        catch (Exception ex)
        {
            return new ApiResponse<AIChatResponseDto>
            {
                Success = false,
                Message = $"Error sending message to AI: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Get chat history for a specific session.
    /// </summary>
    public async Task<ApiResponse<AIConversationHistoryDto>> GetConversationHistoryAsync(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return new ApiResponse<AIConversationHistoryDto>
            {
                Success = false,
                Message = "Session ID is required"
            };
        }

        try
        {
            return await _apiClient.GetAsync<AIConversationHistoryDto>($"api/ai-chat/history?sessionId={sessionId}");
        }
        catch (Exception ex)
        {
            return new ApiResponse<AIConversationHistoryDto>
            {
                Success = false,
                Message = $"Error retrieving conversation history: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Get all sessions for a specific lesson.
    /// </summary>
    public async Task<ApiResponse<List<AIConversationHistoryDto>>> GetSessionsForLessonAsync(Guid lessonId)
    {
        if (lessonId == Guid.Empty)
        {
            return new ApiResponse<List<AIConversationHistoryDto>>
            {
                Success = false,
                Message = "Lesson ID is required"
            };
        }

        try
        {
            return await _apiClient.GetAsync<List<AIConversationHistoryDto>>($"api/ai-chat/sessions?lessonId={lessonId}");
        }
        catch (Exception ex)
        {
            return new ApiResponse<List<AIConversationHistoryDto>>
            {
                Success = false,
                Message = $"Error retrieving sessions: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Delete conversation history for a specific session.
    /// </summary>
    public async Task<ApiResponse<object>> DeleteConversationAsync(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return new ApiResponse<object>
            {
                Success = false,
                Message = "Session ID is required"
            };
        }

        try
        {
            return await _apiClient.PostAsync<object>($"api/ai-chat/sessions/{sessionId}/end", new { });
        }
        catch (Exception ex)
        {
            return new ApiResponse<object>
            {
                Success = false,
                Message = $"Error ending conversation: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Check if AI chat service is available.
    /// </summary>
    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            var response = await _apiClient.GetAsync<object>("api/chat/health");
            return response.Success;
        }
        catch
        {
            return false;
        }
    }
}
