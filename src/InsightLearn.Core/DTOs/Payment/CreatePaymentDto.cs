using System.ComponentModel.DataAnnotations;
using InsightLearn.Core.Constants;
using InsightLearn.Core.Validation;

namespace InsightLearn.Core.DTOs.Payment;

/// <summary>
/// DTO for creating a new payment transaction
/// </summary>
public class CreatePaymentDto
{
    [Required(ErrorMessage = "User ID is required")]
    public Guid UserId { get; set; }

    [Required(ErrorMessage = "Course ID is required")]
    public Guid CourseId { get; set; }

    [Required(ErrorMessage = "Amount is required")]
    [Range(0.01, 50000.00,
        ErrorMessage = "Amount must be between $0.01 and $50,000")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Payment method is required")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Payment method must be between 3 and 50 characters")]
    [RegularExpression(@"^[a-zA-Z_]+$", ErrorMessage = "Payment method must contain only letters and underscores (e.g., stripe, paypal, credit_card)")]
    public string PaymentMethod { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "Coupon code cannot exceed 50 characters")]
    [RegularExpression(@"^[A-Z0-9-]+$", ErrorMessage = "Coupon code must contain only uppercase letters, numbers, and hyphens")]
    public string? CouponCode { get; set; }

    [Required(ErrorMessage = "Currency is required")]
    [ValidCurrency]  // Use custom validator instead of regex
    public string Currency { get; set; } = "USD";

    [StringLength(500, ErrorMessage = "Billing address cannot exceed 500 characters")]
    public string? BillingAddress { get; set; }
}
