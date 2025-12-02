using System;
using System.ComponentModel.DataAnnotations;

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
    [StringLength(255)]
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// Message text (supports long AI responses)
    /// v2.1.0-dev: Changed from NVARCHAR(255) to NVARCHAR(MAX) to support AI responses
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
