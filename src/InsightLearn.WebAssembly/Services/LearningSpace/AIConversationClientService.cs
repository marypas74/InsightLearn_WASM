using InsightLearn.WebAssembly.Models;
using InsightLearn.WebAssembly.Services.Http;
using InsightLearn.Core.DTOs.AIChat;

namespace InsightLearn.WebAssembly.Services.LearningSpace;

/// <summary>
/// Implementation of AI Conversation History client service.
/// Part of Student Learning Space v2.1.0.
/// </summary>
public class AIConversationClientService : IAIConversationClientService
{
    private readonly ApiClient _apiClient;

    public AIConversationClientService(ApiClient apiClient)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
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
            return await _apiClient.GetAsync<AIConversationHistoryDto>($"api/ai-conversations/{sessionId}");
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
            return await _apiClient.DeleteAsync<object>($"api/ai-conversations/{sessionId}");
        }
        catch (Exception ex)
        {
            return new ApiResponse<object>
            {
                Success = false,
                Message = $"Error deleting conversation: {ex.Message}"
            };
        }
    }
}
