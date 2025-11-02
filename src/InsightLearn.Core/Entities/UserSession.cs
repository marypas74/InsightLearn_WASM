using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Core.Entities;

public class UserSession
{
    public Guid Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string SessionId { get; set; } = string.Empty;
    
    public Guid UserId { get; set; }
    
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? EndedAt { get; set; }
    
    [StringLength(100)]
    public string? EndReason { get; set; } // Logout, Timeout, Forced, TokenExpiry
    
    [Required]
    [StringLength(45)]
    public string IpAddress { get; set; } = string.Empty;
    
    [StringLength(1000)]
    public string? UserAgent { get; set; }
    
    [StringLength(50)]
    public string? DeviceType { get; set; } // Desktop, Mobile, Tablet
    
    [StringLength(50)]
    public string? Platform { get; set; } // Windows, MacOS, iOS, Android
    
    [StringLength(100)]
    public string? Browser { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public Guid? LoginAttemptId { get; set; } // Link to the login attempt that created this session
    
    [StringLength(100)]
    public string? JwtTokenId { get; set; } // JWT token identifier for revocation
    
    [StringLength(2000)]
    public string? JwtToken { get; set; } // Full JWT token for session authentication
    
    [StringLength(1000)]
    public string? GeolocationData { get; set; }
    
    public int ActivityCount { get; set; } = 0; // Number of requests in this session
    
    public long DataTransferred { get; set; } = 0; // Bytes transferred
    
    [StringLength(500)]
    public string? LastPageVisited { get; set; }
    
    [StringLength(100)]
    public string? TimeZone { get; set; }
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
    // LoginAttempt navigation property removed to prevent FK conflicts
}