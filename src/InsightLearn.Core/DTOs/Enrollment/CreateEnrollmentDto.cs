using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Core.DTOs.Enrollment;

/// <summary>
/// DTO for creating a new enrollment
/// </summary>
public class CreateEnrollmentDto
{
    [Required(ErrorMessage = "User ID is required")]
    public Guid UserId { get; set; }

    [Required(ErrorMessage = "Course ID is required")]
    public Guid CourseId { get; set; }

    [Required(ErrorMessage = "Amount paid is required")]
    [Range(0, double.MaxValue, ErrorMessage = "Amount must be a positive number")]
    public decimal AmountPaid { get; set; }

    public Guid? PaymentId { get; set; } // Reference to payment transaction

    public string? CouponCode { get; set; }
}
