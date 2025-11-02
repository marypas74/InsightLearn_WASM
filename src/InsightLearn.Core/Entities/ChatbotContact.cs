using System;

namespace InsightLearn.Core.Entities;

/// <summary>
/// Represents a contact collected via the chatbot
/// </summary>
public class ChatbotContact
{
    public int Id { get; set; }

    /// <summary>
    /// Email address provided by the user
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// First message sent by the user (optional)
    /// </summary>
    public string? InitialMessage { get; set; }

    /// <summary>
    /// When the contact was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User's IP address (for security and analytics)
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent string
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Conversation session ID (for grouping messages)
    /// </summary>
    public string SessionId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Total number of messages in this session
    /// </summary>
    public int MessageCount { get; set; } = 0;

    /// <summary>
    /// Last interaction timestamp
    /// </summary>
    public DateTime? LastInteractionAt { get; set; }

    /// <summary>
    /// Whether this contact has been followed up
    /// </summary>
    public bool IsFollowedUp { get; set; } = false;

    /// <summary>
    /// Associated user ID if the user was logged in
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Notes for follow-up
    /// </summary>
    public string? Notes { get; set; }
}
