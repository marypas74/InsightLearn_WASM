using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Core.DTOs.User;

/// <summary>
/// DTO for updating user profile information
/// </summary>
public class UpdateUserDto
{
    [StringLength(100, MinimumLength = 1, ErrorMessage = "First name must be between 1 and 100 characters")]
    [RegularExpression(@"^[a-zA-Z\s\-'\.]+$", ErrorMessage = "First name can only contain letters, spaces, hyphens, apostrophes, and periods")]
    public string? FirstName { get; set; }

    [StringLength(100, MinimumLength = 1, ErrorMessage = "Last name must be between 1 and 100 characters")]
    [RegularExpression(@"^[a-zA-Z\s\-'\.]+$", ErrorMessage = "Last name can only contain letters, spaces, hyphens, apostrophes, and periods")]
    public string? LastName { get; set; }

    [Phone(ErrorMessage = "Invalid phone number format")]
    [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
    public string? PhoneNumber { get; set; }

    [StringLength(2000, ErrorMessage = "Bio cannot exceed 2000 characters")]
    public string? Bio { get; set; }

    public bool? IsInstructor { get; set; }

    public bool? IsVerified { get; set; }

    [StringLength(200, ErrorMessage = "Street address cannot exceed 200 characters")]
    public string? StreetAddress { get; set; }

    [StringLength(100, ErrorMessage = "City cannot exceed 100 characters")]
    [RegularExpression(@"^[a-zA-Z\s\-'.]+$", ErrorMessage = "City can only contain letters, spaces, hyphens, apostrophes, and periods")]
    public string? City { get; set; }

    [StringLength(100, ErrorMessage = "State/Province cannot exceed 100 characters")]
    public string? StateProvince { get; set; }

    [StringLength(20, ErrorMessage = "Postal code cannot exceed 20 characters")]
    [RegularExpression(@"^[A-Z0-9\s\-]+$", ErrorMessage = "Postal code can only contain uppercase letters, numbers, spaces, and hyphens")]
    public string? PostalCode { get; set; }

    [StringLength(100, ErrorMessage = "Country cannot exceed 100 characters")]
    public string? Country { get; set; }

    [DataType(DataType.Date)]
    public DateTime? DateOfBirth { get; set; }

    [StringLength(50, ErrorMessage = "Gender cannot exceed 50 characters")]
    [RegularExpression(@"^(Male|Female|Other|PreferNotToSay)$", ErrorMessage = "Gender must be 'Male', 'Female', 'Other', or 'PreferNotToSay'")]
    public string? Gender { get; set; }

    [StringLength(50, ErrorMessage = "User type cannot exceed 50 characters")]
    [RegularExpression(@"^(Student|Instructor|Admin)$", ErrorMessage = "User type must be 'Student', 'Instructor', or 'Admin'")]
    public string? UserType { get; set; }
}
