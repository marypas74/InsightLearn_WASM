namespace InsightLearn.WebAssembly.Models.Auth;

public class AuthResponse
{
    public bool IsSuccess { get; set; }  // FIXED: Aligned with API AuthResultDto
    public string? Token { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? SessionId { get; set; }  // ADDED: SessionId from API
    public UserInfo? User { get; set; }
    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }  // ADDED: ErrorMessage from API
    public List<string> Errors { get; set; } = new();

    // Backward compatibility: Success can be set/get but internally uses IsSuccess
    public bool Success
    {
        get => IsSuccess;
        set => IsSuccess = value;
    }
}

public class UserInfo
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? ProfileImageUrl { get; set; }
    public string? Bio { get; set; }
    public DateTime DateJoined { get; set; }
    public DateTime? LastLoginDate { get; set; }
    public bool IsInstructor { get; set; }
    public bool IsVerified { get; set; }
    public decimal WalletBalance { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public List<string> Roles { get; set; } = new();
    public string? SessionId { get; set; }

    // Computed property for backward compatibility
    public string Role => Roles.FirstOrDefault() ?? "Student";
}
