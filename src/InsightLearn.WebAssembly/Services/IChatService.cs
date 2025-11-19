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
    // FIXED: Removed JsonPropertyName attributes to allow PropertyNameCaseInsensitive to work
    // Backend returns PascalCase JSON, ApiClient has PropertyNameCaseInsensitive = true
    public string Response { get; set; } = string.Empty;

    public string SessionId { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; }

    public int ResponseTimeMs { get; set; }

    public string? AiModel { get; set; }

    public bool HasError { get; set; }

    public string? ErrorMessage { get; set; }
}

public interface IChatService
{
    Task<ApiResponse<ChatResponse>> SendMessageAsync(string message, string? email = null, string? sessionId = null, Guid? courseId = null);
    Task<ApiResponse<List<ChatMessage>>> GetChatHistoryAsync();
}
