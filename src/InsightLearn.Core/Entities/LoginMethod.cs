using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Core.Entities;

public class LoginMethod
{
    public Guid Id { get; set; }
    
    public Guid UserId { get; set; }
    
    [Required]
    [StringLength(50)]
    public string MethodType { get; set; } = string.Empty; // EmailPassword, GoogleOAuth, MicrosoftSSO, etc.
    
    public bool IsEnabled { get; set; } = true;
    
    public DateTime FirstUsedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime LastUsedAt { get; set; } = DateTime.UtcNow;
    
    public int SuccessfulLogins { get; set; } = 0;
    
    public int FailedAttempts { get; set; } = 0;
    
    [StringLength(200)]
    public string? ProviderUserId { get; set; } // External provider user ID
    
    [StringLength(100)]
    public string? ProviderName { get; set; } // Google, Microsoft, Facebook
    
    [StringLength(256)]
    public string? ProviderAccountEmail { get; set; }
    
    public string? MetadataJson { get; set; } // Additional provider-specific data
    
    public bool IsVerified { get; set; } = false;
    
    public DateTime? VerifiedAt { get; set; }
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
}