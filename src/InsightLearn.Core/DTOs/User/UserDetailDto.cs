namespace InsightLearn.Core.DTOs.User;

public class UserDetailDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public string? PhoneNumber { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public string? Bio { get; set; }

    // Status
    public bool IsInstructor { get; set; }
    public bool IsVerified { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool IsLocked { get; set; }
    public DateTime? LockoutEnd { get; set; }

    // Dates
    public DateTime DateJoined { get; set; }
    public DateTime? LastLoginDate { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Profile Information
    public string? StreetAddress { get; set; }
    public string? City { get; set; }
    public string? StateProvince { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }

    // OAuth
    public bool IsGoogleUser { get; set; }
    public string? GoogleId { get; set; }

    // Preferences
    public string UserType { get; set; } = "Student";
    public string? PreferredPaymentMethod { get; set; }
    public decimal WalletBalance { get; set; }

    // Roles
    public List<string> Roles { get; set; } = new();

    // Statistics (backward compatibility + new detailed stats)
    public int TotalEnrollments { get; set; }
    public int TotalCoursesCreated { get; set; }
    public int TotalReviews { get; set; }
    public decimal TotalSpent { get; set; }
    public UserStatisticsDto? Statistics { get; set; }
}
