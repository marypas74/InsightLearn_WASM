using InsightLearn.WebAssembly.Models;

namespace InsightLearn.WebAssembly.Services;

public class ChatMessage
{
    public string Message { get; set; } = string.Empty;
    public bool IsUser { get; set; }
    public DateTime Timestamp { get; set; }
}

public class ChatResponse
{
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? SessionId { get; set; }
}

public interface IChatService
{
    Task<ApiResponse<ChatResponse>> SendMessageAsync(string message, string? email = null, string? sessionId = null, Guid? courseId = null);
    Task<ApiResponse<List<ChatMessage>>> GetChatHistoryAsync();
}
