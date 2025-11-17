using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Core.DTOs.Subscription;

/// <summary>
/// DTO for creating a new subscription plan
/// </summary>
public class CreateSubscriptionPlanDto
{
    [Required(ErrorMessage = "Plan name is required")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Plan name must be between 3 and 100 characters")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Monthly price is required")]
    [Range(0.01, 1000.00, ErrorMessage = "Monthly price must be between 0.01 and 1000.00")]
    public decimal PriceMonthly { get; set; }

    [Range(0.01, 10000.00, ErrorMessage = "Yearly price must be between 0.01 and 10000.00")]
    public decimal? PriceYearly { get; set; }

    public List<string> Features { get; set; } = new();

    [Range(1, 10, ErrorMessage = "Max devices must be between 1 and 10")]
    public int? MaxDevices { get; set; }

    [StringLength(20)]
    public string? MaxVideoQuality { get; set; }

    public bool AllowOfflineDownload { get; set; } = false;

    public int DisplayOrder { get; set; } = 0;

    public bool IsFeatured { get; set; } = false;

    [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency must be 3-letter code (e.g., USD, EUR)")]
    public string Currency { get; set; } = "USD";

    [Required(ErrorMessage = "Billing cycle is required")]
    [Range(1, 12, ErrorMessage = "Billing cycle months must be between 1 and 12")]
    public int BillingCycleMonths { get; set; } = 1;
}
