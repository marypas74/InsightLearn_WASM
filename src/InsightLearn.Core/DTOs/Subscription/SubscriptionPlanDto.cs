namespace InsightLearn.Core.DTOs.Subscription;

/// <summary>
/// DTO for subscription plan response
/// </summary>
public class SubscriptionPlanDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal PriceMonthly { get; set; }
    public decimal? PriceYearly { get; set; }
    public List<string> Features { get; set; } = new();
    public int? MaxDevices { get; set; }
    public string? MaxVideoQuality { get; set; }
    public bool AllowOfflineDownload { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public bool IsFeatured { get; set; }
    public string? StripePriceMonthlyId { get; set; }
    public string? StripePriceYearlyId { get; set; }
    public int ActiveSubscriptionCount { get; set; }
    public decimal? YearlySavings { get; set; }
    public int? YearlySavingsPercentage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
