using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Core.DTOs.User;

public class UpdateUserDto
{
    [StringLength(100)] public string? FirstName { get; set; }
    [StringLength(100)] public string? LastName { get; set; }
    [Phone] public string? PhoneNumber { get; set; }
    public string? Bio { get; set; }
    public bool? IsInstructor { get; set; }
    public bool? IsVerified { get; set; }
    public string? StreetAddress { get; set; }
    public string? City { get; set; }
    public string? StateProvince { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? UserType { get; set; }
}
