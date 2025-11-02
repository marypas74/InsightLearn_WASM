using System;

namespace InsightLearn.Core.Entities;

/// <summary>
/// Represents a message exchanged in the chatbot
/// </summary>
public class ChatbotMessage
{
    public int Id { get; set; }

    /// <summary>
    /// Session ID linking to ChatbotContact
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// Message text
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Whether this is a user message or bot response
    /// </summary>
    public bool IsUserMessage { get; set; }

    /// <summary>
    /// When the message was sent
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// AI model used for response (if bot message)
    /// </summary>
    public string? AiModel { get; set; }

    /// <summary>
    /// Response time in milliseconds (if bot message)
    /// </summary>
    public int? ResponseTimeMs { get; set; }
}
