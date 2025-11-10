namespace InsightLearn.Core.DTOs.User;

public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public DateTime DateJoined { get; set; }
    public DateTime? LastLoginDate { get; set; }
    public bool IsInstructor { get; set; }
    public bool IsVerified { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool IsLocked { get; set; }
    public DateTime? LockoutEnd { get; set; }
    public decimal WalletBalance { get; set; }
    public List<string> Roles { get; set; } = new();
    public string FullName => $"{FirstName} {LastName}";
    public string AccountStatus => IsLocked ? "Suspended" : "Active";
}
