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
    // Use camelCase to match backend JSON (backend serializes in camelCase)
    [System.Text.Json.Serialization.JsonPropertyName("response")]
    public string Response { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("responseTimeMs")]
    public int ResponseTimeMs { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("aiModel")]
    public string? AiModel { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("hasError")]
    public bool HasError { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }
}

public interface IChatService
{
    Task<ApiResponse<ChatResponse>> SendMessageAsync(string message, string? email = null, string? sessionId = null, Guid? courseId = null);
    Task<ApiResponse<List<ChatMessage>>> GetChatHistoryAsync();
}
