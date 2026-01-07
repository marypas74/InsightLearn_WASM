using InsightLearn.WebAssembly.Models;
using InsightLearn.WebAssembly.Models.Config;
using InsightLearn.WebAssembly.Services.Http;
using Microsoft.Extensions.Logging;

namespace InsightLearn.WebAssembly.Services;

public class ChatService : IChatService
{
    private readonly IApiClient _apiClient;
    private readonly EndpointsConfig _endpoints;
    private readonly ILogger<ChatService> _logger;

    public ChatService(IApiClient apiClient, EndpointsConfig endpoints, ILogger<ChatService> logger)
    {
        _apiClient = apiClient;
        _endpoints = endpoints;
        _logger = logger;
    }

    public async Task<ApiResponse<ChatResponse>> SendMessageAsync(string message, string? email = null, string? sessionId = null, Guid? courseId = null)
    {
        _logger.LogInformation("Sending chat message (length: {MessageLength}, session: {SessionId}, course: {CourseId})",
            message?.Length ?? 0, sessionId ?? "none", courseId?.ToString() ?? "none");

        // IMPORTANT: Use PascalCase property names to match backend DTO (ChatMessageRequest)
        var request = new
        {
            Message = message,      // Backend expects: Message (PascalCase)
            Email = email,          // Backend expects: Email (PascalCase)
            SessionId = sessionId,  // Backend expects: SessionId (PascalCase)
            CourseId = courseId     // Backend expects: CourseId (PascalCase)
        };

        var response = await _apiClient.PostAsync<ChatResponse>(_endpoints.Chat.SendMessage, request);

        if (response.Success && response.Data != null)
        {
            _logger.LogInformation("Chat message sent successfully, received response (length: {ResponseLength})",
                response.Data.Response?.Length ?? 0);
        }
        else
        {
            _logger.LogWarning("Failed to send chat message: {ErrorMessage}",
                response.Message ?? "Unknown error");
        }

        return response;
    }

    public async Task<ApiResponse<List<ChatMessage>>> GetChatHistoryAsync()
    {
        _logger.LogDebug("Fetching chat history");
        var response = await _apiClient.GetAsync<List<ChatMessage>>(_endpoints.Chat.GetHistory);

        if (response.Success && response.Data != null)
        {
            _logger.LogInformation("Retrieved {MessageCount} chat messages", response.Data.Count);
        }
        else
        {
            _logger.LogWarning("Failed to retrieve chat history: {ErrorMessage}",
                response.Message ?? "Unknown error");
        }

        return response;
    }
}
