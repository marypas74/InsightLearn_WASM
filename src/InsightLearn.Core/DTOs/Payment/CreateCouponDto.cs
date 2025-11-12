using System.ComponentModel.DataAnnotations;
using InsightLearn.Core.Constants;
using InsightLearn.Core.Validation; // For DateGreaterThan

namespace InsightLearn.Core.DTOs.Payment;

/// <summary>
/// Input DTO for coupon creation (prevents over-posting attacks)
/// Separates user input from system-managed fields (Id, IsActive, UsedCount)
/// </summary>
public class CreateCouponDto
{
    [Required(ErrorMessage = "Coupon code is required")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Coupon code must be between 3 and 50 characters")]
    [RegularExpression(@"^[A-Z0-9-]+$", ErrorMessage = "Coupon code must contain only uppercase letters, numbers, and hyphens")]
    public string Code { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Coupon type is required")]
    [RegularExpression(@"^(Percentage|FixedAmount)$", ErrorMessage = "Type must be 'Percentage' or 'FixedAmount'")]
    public string Type { get; set; } = string.Empty;

    [Required(ErrorMessage = "Coupon value is required")]
    [Range(0.01, 50000.00,
        ErrorMessage = "Value must be between $0.01 and $50,000")]
    public decimal Value { get; set; }

    [Range(0, 50000.00,
        ErrorMessage = "Minimum amount must be between $0 and $50,000")]
    public decimal? MinimumAmount { get; set; }

    [Range(0, 50000.00,
        ErrorMessage = "Maximum discount must be between $0 and $50,000")]
    public decimal? MaximumDiscount { get; set; }

    [Range(1, ValidationConstants.Payment.MaxCouponUsageLimit,
        ErrorMessage = "Usage limit must be between 1 and 100,000")]
    public int? UsageLimit { get; set; }

    [Required(ErrorMessage = "Valid from date is required")]
    public DateTime ValidFrom { get; set; }

    [Required(ErrorMessage = "Valid until date is required")]
    [DateGreaterThan(nameof(ValidFrom), ErrorMessage = "Valid until must be after valid from date")]
    public DateTime ValidUntil { get; set; }
}