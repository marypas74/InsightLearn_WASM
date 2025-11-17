using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Core.DTOs.Subscription;

/// <summary>
/// DTO for updating an existing subscription plan
/// </summary>
public class UpdateSubscriptionPlanDto
{
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Plan name must be between 3 and 100 characters")]
    public string? Name { get; set; }

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }

    [Range(0.01, 1000.00, ErrorMessage = "Monthly price must be between 0.01 and 1000.00")]
    public decimal? PriceMonthly { get; set; }

    [Range(0.01, 10000.00, ErrorMessage = "Yearly price must be between 0.01 and 10000.00")]
    public decimal? PriceYearly { get; set; }

    public List<string>? Features { get; set; }

    [Range(1, 10, ErrorMessage = "Max devices must be between 1 and 10")]
    public int? MaxDevices { get; set; }

    [StringLength(20)]
    public string? MaxVideoQuality { get; set; }

    public bool? AllowOfflineDownload { get; set; }

    public int? DisplayOrder { get; set; }

    public bool? IsFeatured { get; set; }
}
