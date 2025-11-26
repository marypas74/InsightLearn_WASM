using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Shared.DTOs;

public class UserDto
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
    public IList<string> Roles { get; set; } = new List<string>();
    public string? SessionId { get; set; }
}

/// <summary>
/// User registration DTO with comprehensive security validation.
/// Security: OWASP A03:2021 (Injection), OWASP A07:2021 (Authentication Failures)
/// </summary>
public class RegisterDto
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    [StringLength(256, ErrorMessage = "Email cannot exceed 256 characters")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "First name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 100 characters")]
    [RegularExpression(@"^[\p{L}\s\-'\.]+$", ErrorMessage = "First name can only contain letters, spaces, hyphens, apostrophes, and periods")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 100 characters")]
    [RegularExpression(@"^[\p{L}\s\-'\.]+$", ErrorMessage = "Last name can only contain letters, spaces, hyphens, apostrophes, and periods")]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Password must be 8-128 characters with at least: 1 uppercase, 1 lowercase, 1 digit, 1 special char.
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    [StringLength(128, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 128 characters")]
    [DataType(DataType.Password)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&\#\^\(\)\-_=+\[\]{}|;:',.<>\/\\`~])[A-Za-z\d@$!%*?&\#\^\(\)\-_=+\[\]{}|;:',.<>\/\\`~]{8,}$",
        ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one digit, and one special character")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your password")]
    [Compare("Password", ErrorMessage = "Passwords do not match")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Range(typeof(bool), "true", "true", ErrorMessage = "You must agree to the terms and conditions")]
    public bool AgreeToTerms { get; set; }

    public bool IsInstructor { get; set; }
}

/// <summary>
/// Login DTO with security validation.
/// Security: OWASP A07:2021 (Authentication Failures)
/// Note: Rate limiting is handled at the API level, not DTO validation.
/// </summary>
public class LoginDto
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    [StringLength(256, ErrorMessage = "Email cannot exceed 256 characters")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [StringLength(128, ErrorMessage = "Password cannot exceed 128 characters")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}

public class AuthResultDto
{
    public bool IsSuccess { get; set; }
    public string? Token { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? SessionId { get; set; }
    public UserDto? User { get; set; }
    public object? Data { get; set; }
    public IEnumerable<string> Errors { get; set; } = new List<string>();
    public string? ErrorMessage { get; set; }
}

public class UpdateProfileDto
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string LastName { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Bio { get; set; }

    public string? ProfileImageUrl { get; set; }
}

/// <summary>
/// Change password DTO with security validation.
/// Security: OWASP A07:2021 (Authentication Failures)
/// </summary>
public class ChangePasswordDto
{
    [Required(ErrorMessage = "Current password is required")]
    [StringLength(128, ErrorMessage = "Current password cannot exceed 128 characters")]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>
    /// New password must be 8-128 characters with at least: 1 uppercase, 1 lowercase, 1 digit, 1 special char.
    /// </summary>
    [Required(ErrorMessage = "New password is required")]
    [StringLength(128, MinimumLength = 8, ErrorMessage = "New password must be between 8 and 128 characters")]
    [DataType(DataType.Password)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&\#\^\(\)\-_=+\[\]{}|;:',.<>\/\\`~])[A-Za-z\d@$!%*?&\#\^\(\)\-_=+\[\]{}|;:',.<>\/\\`~]{8,}$",
        ErrorMessage = "New password must contain at least one uppercase letter, one lowercase letter, one digit, and one special character")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your new password")]
    [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ForgotPasswordDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Reset password DTO with security validation.
/// Security: OWASP A07:2021 (Authentication Failures)
/// </summary>
public class ResetPasswordDto
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    [StringLength(256, ErrorMessage = "Email cannot exceed 256 characters")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Reset token is required")]
    [StringLength(1024, ErrorMessage = "Token cannot exceed 1024 characters")]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Password must be 8-128 characters with at least: 1 uppercase, 1 lowercase, 1 digit, 1 special char.
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    [StringLength(128, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 128 characters")]
    [DataType(DataType.Password)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&\#\^\(\)\-_=+\[\]{}|;:',.<>\/\\`~])[A-Za-z\d@$!%*?&\#\^\(\)\-_=+\[\]{}|;:',.<>\/\\`~]{8,}$",
        ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one digit, and one special character")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your password")]
    [Compare("Password", ErrorMessage = "Passwords do not match")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class GoogleLoginDto
{
    [Required]
    public string AccessToken { get; set; } = string.Empty;
    
    public string? IdToken { get; set; }
}

public class GetTokenRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    public bool UseExistingSession { get; set; } = true;
}

public class GenerateUserTokenRequest
{
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    public bool GenerateForAuthenticatedUser { get; set; } = true;
}

public class UserSessionDto
{
    public string SessionId { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastAccessedAt { get; set; }
    public bool IsCurrentSession { get; set; }
    public string Location { get; set; } = string.Empty;
}