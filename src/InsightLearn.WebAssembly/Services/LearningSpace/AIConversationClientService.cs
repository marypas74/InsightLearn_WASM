using InsightLearn.WebAssembly.Models;
using InsightLearn.WebAssembly.Services.Http;
using InsightLearn.Core.DTOs.AIChat;
using Microsoft.Extensions.Logging;

namespace InsightLearn.WebAssembly.Services.LearningSpace;

/// <summary>
/// Implementation of AI Conversation and Chat client service.
/// Part of Student Learning Space v2.1.0.
/// Extended for AIChatPanel component support.
/// </summary>
public class AIConversationClientService : IAIConversationClientService
{
    private readonly IApiClient _apiClient;
    private readonly ILogger<AIConversationClientService> _logger;

    public AIConversationClientService(IApiClient apiClient, ILogger<AIConversationClientService> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logger = logger;
    }

    /// <summary>
    /// Send a message to the AI assistant and get a response.
    /// </summary>
    public async Task<ApiResponse<AIChatResponseDto>> SendMessageAsync(AIChatMessageDto message)
    {
        if (message == null || string.IsNullOrWhiteSpace(message.Message))
        {
            _logger.LogWarning("SendMessageAsync called with null or empty message");
            return new ApiResponse<AIChatResponseDto>
            {
                Success = false,
                Message = "Message content is required"
            };
        }

        _logger.LogDebug("Sending message to AI assistant (session: {SessionId})", message.SessionId);
        try
        {
            var response = await _apiClient.PostAsync<AIChatResponseDto>("api/ai-chat/message", message);

            if (response.Success)
            {
                _logger.LogInformation("AI message sent successfully (session: {SessionId})", message.SessionId);
            }
            else
            {
                _logger.LogWarning("Failed to send AI message (session: {SessionId}): {ErrorMessage}",
                    message.SessionId, response.Message ?? "Unknown error");
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to AI (session: {SessionId}): {ErrorMessage}",
                message.SessionId, ex.Message);
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
            _logger.LogWarning("GetConversationHistoryAsync called with null or empty sessionId");
            return new ApiResponse<AIConversationHistoryDto>
            {
                Success = false,
                Message = "Session ID is required"
            };
        }

        _logger.LogDebug("Fetching conversation history for session: {SessionId}", sessionId);
        try
        {
            var response = await _apiClient.GetAsync<AIConversationHistoryDto>($"api/ai-chat/history?sessionId={sessionId}");

            if (response.Success && response.Data != null)
            {
                _logger.LogInformation("Retrieved conversation history for session {SessionId} ({MessageCount} messages)",
                    sessionId, response.Data.Messages?.Count ?? 0);
            }
            else
            {
                _logger.LogWarning("Failed to retrieve conversation history for session {SessionId}: {ErrorMessage}",
                    sessionId, response.Message ?? "Unknown error");
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving conversation history for session {SessionId}: {ErrorMessage}",
                sessionId, ex.Message);
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
            _logger.LogWarning("GetSessionsForLessonAsync called with empty lessonId");
            return new ApiResponse<List<AIConversationHistoryDto>>
            {
                Success = false,
                Message = "Lesson ID is required"
            };
        }

        _logger.LogDebug("Fetching AI chat sessions for lesson: {LessonId}", lessonId);
        try
        {
            var response = await _apiClient.GetAsync<List<AIConversationHistoryDto>>($"api/ai-chat/sessions?lessonId={lessonId}");

            if (response.Success && response.Data != null)
            {
                _logger.LogInformation("Retrieved {SessionCount} AI chat sessions for lesson {LessonId}",
                    response.Data.Count, lessonId);
            }
            else
            {
                _logger.LogWarning("Failed to retrieve AI chat sessions for lesson {LessonId}: {ErrorMessage}",
                    lessonId, response.Message ?? "Unknown error");
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving AI chat sessions for lesson {LessonId}: {ErrorMessage}",
                lessonId, ex.Message);
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
            _logger.LogWarning("DeleteConversationAsync called with null or empty sessionId");
            return new ApiResponse<object>
            {
                Success = false,
                Message = "Session ID is required"
            };
        }

        _logger.LogWarning("Deleting AI conversation for session: {SessionId}", sessionId);
        try
        {
            var response = await _apiClient.PostAsync<object>($"api/ai-chat/sessions/{sessionId}/end", new { });

            if (response.Success)
            {
                _logger.LogInformation("AI conversation deleted successfully for session {SessionId}", sessionId);
            }
            else
            {
                _logger.LogError("Failed to delete AI conversation for session {SessionId}: {ErrorMessage}",
                    sessionId, response.Message ?? "Unknown error");
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ending AI conversation for session {SessionId}: {ErrorMessage}",
                sessionId, ex.Message);
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
        _logger.LogDebug("Checking AI chat service availability");
        try
        {
            var response = await _apiClient.GetAsync<object>("api/chat/health");

            if (response.Success)
            {
                _logger.LogDebug("AI chat service is available");
            }
            else
            {
                _logger.LogWarning("AI chat service health check failed: {ErrorMessage}",
                    response.Message ?? "Unknown error");
            }

            return response.Success;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI chat service availability check failed: {ErrorMessage}", ex.Message);
            return false;
        }
    }
}
