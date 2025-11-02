using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Core.Entities;

public class SecurityEvent
{
    public Guid Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string EventType { get; set; } = string.Empty; // BruteForce, SuspiciousLogin, AccountTakeover, etc.
    
    [Required]
    [StringLength(20)]
    public string Severity { get; set; } = "Medium"; // Low, Medium, High, Critical
    
    public Guid? UserId { get; set; }
    
    [StringLength(256)]
    public string? Email { get; set; }
    
    [Required]
    [StringLength(45)]
    public string IpAddress { get; set; } = string.Empty;
    
    [StringLength(1000)]
    public string? UserAgent { get; set; }
    
    [Required]
    public string EventDetails { get; set; } = string.Empty; // JSON with event-specific details
    
    public decimal RiskScore { get; set; } = 0.00m;
    
    public bool IsResolved { get; set; } = false;
    
    public DateTime? ResolvedAt { get; set; }
    
    public Guid? ResolvedByUserId { get; set; }
    
    [StringLength(1000)]
    public string? ResolutionNotes { get; set; }
    
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    [StringLength(1000)]
    public string? GeolocationData { get; set; }
    
    [StringLength(100)]
    public string? RelatedSessionId { get; set; }
    
    public Guid? RelatedLoginAttemptId { get; set; }
    
    public bool AutoBlocked { get; set; } = false;
    
    public DateTime? BlockedUntil { get; set; }
    
    public bool NotificationSent { get; set; } = false;
    
    [StringLength(100)]
    public string? CorrelationId { get; set; }
    
    // Navigation properties removed to prevent EF cascade conflicts
    // Use UserId, ResolvedByUserId, and RelatedLoginAttemptId as simple foreign key fields
}