using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InsightLearn.Core.Entities;

/// <summary>
/// Subscription plan (Basic, Pro, Premium)
/// </summary>
public class SubscriptionPlan
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty; // "Basic", "Pro", "Premium"

    [MaxLength(500)]
    public string? Description { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal PriceMonthly { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? PriceYearly { get; set; } // Optional annual pricing (with discount)

    /// <summary>
    /// JSON array of features (e.g., ["Unlimited courses", "HD videos", "Certificate"])
    /// </summary>
    public string? Features { get; set; }

    /// <summary>
    /// Maximum number of concurrent devices (null = unlimited)
    /// </summary>
    public int? MaxDevices { get; set; }

    /// <summary>
    /// Maximum video quality (e.g., "720p", "1080p", "4K")
    /// </summary>
    [MaxLength(20)]
    public string? MaxVideoQuality { get; set; }

    /// <summary>
    /// Whether users can download courses for offline viewing
    /// </summary>
    public bool AllowOfflineDownload { get; set; } = false;

    /// <summary>
    /// Display order in pricing page
    /// </summary>
    public int DisplayOrder { get; set; } = 0;

    /// <summary>
    /// Whether this plan is currently available for subscription
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether this is a featured plan (highlighted in UI)
    /// </summary>
    public bool IsFeatured { get; set; } = false;

    /// <summary>
    /// Stripe Product ID
    /// </summary>
    [MaxLength(255)]
    public string? StripeProductId { get; set; }

    /// <summary>
    /// Stripe Price ID for monthly billing
    /// </summary>
    [MaxLength(255)]
    public string? StripePriceMonthlyId { get; set; }

    /// <summary>
    /// Stripe Price ID for yearly billing
    /// </summary>
    [MaxLength(255)]
    public string? StripePriceYearlyId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<UserSubscription> UserSubscriptions { get; set; } = new List<UserSubscription>();
}
