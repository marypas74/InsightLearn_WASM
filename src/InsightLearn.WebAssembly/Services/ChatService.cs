using InsightLearn.WebAssembly.Models;
using InsightLearn.WebAssembly.Models.Config;
using InsightLearn.WebAssembly.Services.Http;

namespace InsightLearn.WebAssembly.Services;

public class ChatService : IChatService
{
    private readonly IApiClient _apiClient;
    private readonly EndpointsConfig _endpoints;

    public ChatService(IApiClient apiClient, EndpointsConfig endpoints)
    {
        _apiClient = apiClient;
        _endpoints = endpoints;
    }

    public async Task<ApiResponse<ChatResponse>> SendMessageAsync(string message, string? email = null, string? sessionId = null, Guid? courseId = null)
    {
        return await _apiClient.PostAsync<ChatResponse>(_endpoints.Chat.SendMessage, new
        {
            Message = message,
            Email = email,
            SessionId = sessionId,
            CourseId = courseId
        });
    }

    public async Task<ApiResponse<List<ChatMessage>>> GetChatHistoryAsync()
    {
        return await _apiClient.GetAsync<List<ChatMessage>>(_endpoints.Chat.GetHistory);
    }
}
