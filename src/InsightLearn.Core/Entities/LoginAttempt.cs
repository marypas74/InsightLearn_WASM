using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Core.Entities;

public class LoginAttempt
{
    public Guid Id { get; set; }
    
    [Required]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;
    
    public Guid? UserId { get; set; }
    
    [Required]
    [StringLength(50)]
    public string LoginMethod { get; set; } = "EmailPassword"; // EmailPassword, GoogleOAuth, AppleSSO, etc.
    
    public bool IsSuccess { get; set; }
    
    [StringLength(500)]
    public string? FailureReason { get; set; } // InvalidCredentials, AccountLocked, etc.
    
    [Required]
    [StringLength(45)]
    public string IpAddress { get; set; } = string.Empty;
    
    [StringLength(1000)]
    public string? UserAgent { get; set; }
    
    [StringLength(500)]
    public string? DeviceFingerprint { get; set; }
    
    [StringLength(100)]
    public string? SessionId { get; set; }
    
    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;
    
    [StringLength(1000)]
    public string? GeolocationData { get; set; } // JSON: {"country": "US", "city": "New York", "lat": 40.7128, "lng": -74.0060}
    
    public decimal? RiskScore { get; set; } // 0.00 to 1.00 risk assessment
    
    [StringLength(500)]
    public string? BrowserInfo { get; set; } // JSON: {"browser": "Chrome", "version": "115.0.0.0", "os": "Windows"}
    
    [StringLength(100)]
    public string? AuthProvider { get; set; } // Google, Microsoft, Facebook for OAuth
    
    [StringLength(200)]
    public string? ProviderUserId { get; set; } // External provider user ID
    
    [StringLength(100)]
    public string? CorrelationId { get; set; }
    
    // Navigation properties removed to prevent EF cascade conflicts
    // Use UserId as simple foreign key field
}