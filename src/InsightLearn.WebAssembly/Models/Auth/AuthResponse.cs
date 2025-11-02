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
    public string? ProfileImageUrl { get; set; }
    public List<string> Roles { get; set; } = new();
    public bool IsProfileComplete { get; set; }
}
