using System.ComponentModel.DataAnnotations;

namespace InsightLearn.WebAssembly.Models.Auth;

public class RegisterRequest
{
    [Required(ErrorMessage = "First name is required")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password confirmation is required")]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
    public string? Address { get; set; }
    public string? PostalCode { get; set; }
    public string? ProfileImageUrl { get; set; }
    public string? Bio { get; set; }
    public string? Occupation { get; set; }
    public string? Company { get; set; }
    public string? LinkedInProfile { get; set; }
    public string? TwitterHandle { get; set; }
    public string? GitHubProfile { get; set; }
    public string Role { get; set; } = "Student";
}

public class CompleteRegistrationRequest
{
    public string? PhoneNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
    public string? Address { get; set; }
    public string? PostalCode { get; set; }
    public string? Bio { get; set; }
    public string? Occupation { get; set; }
    public string? Company { get; set; }
    public string? LinkedInProfile { get; set; }
    public string? TwitterHandle { get; set; }
    public string? GitHubProfile { get; set; }
}
