using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Core.DTOs.Payment;

/// <summary>
/// DTO for applying a coupon/discount code to a course purchase
/// </summary>
public class ApplyCouponDto
{
    [Required(ErrorMessage = "Coupon code is required")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Coupon code must be between 3 and 50 characters")]
    [RegularExpression(@"^[A-Z0-9-]+$", ErrorMessage = "Coupon code must contain only uppercase letters, numbers, and hyphens")]
    public string CouponCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Course ID is required")]
    public Guid CourseId { get; set; }

    [Required(ErrorMessage = "Original amount is required")]
    [Range(0.01, 50000.00, ErrorMessage = "Original amount must be between $0.01 and $50,000")]
    public decimal OriginalAmount { get; set; }
}
