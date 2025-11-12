using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InsightLearn.Core.Entities;

/// <summary>
/// User's active or past subscription
/// </summary>
public class UserSubscription
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid PlanId { get; set; }

    /// <summary>
    /// Subscription status: "active", "cancelled", "past_due", "paused", "expired"
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "active";

    /// <summary>
    /// Billing interval: "month" or "year"
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string BillingInterval { get; set; } = "month";

    /// <summary>
    /// Stripe subscription ID
    /// </summary>
    [MaxLength(255)]
    public string? StripeSubscriptionId { get; set; }

    /// <summary>
    /// Stripe customer ID
    /// </summary>
    [MaxLength(255)]
    public string? StripeCustomerId { get; set; }

    /// <summary>
    /// Current billing period start date
    /// </summary>
    [Required]
    public DateTime CurrentPeriodStart { get; set; }

    /// <summary>
    /// Current billing period end date
    /// </summary>
    [Required]
    public DateTime CurrentPeriodEnd { get; set; }

    /// <summary>
    /// Date when subscription was cancelled (null if not cancelled)
    /// </summary>
    public DateTime? CancelledAt { get; set; }

    /// <summary>
    /// Date when subscription will end (after cancellation)
    /// </summary>
    public DateTime? EndsAt { get; set; }

    /// <summary>
    /// Whether subscription is set to auto-renew
    /// </summary>
    public bool AutoRenew { get; set; } = true;

    /// <summary>
    /// Trial end date (null if not in trial)
    /// </summary>
    public DateTime? TrialEndsAt { get; set; }

    /// <summary>
    /// Reason for cancellation
    /// </summary>
    [MaxLength(500)]
    public string? CancellationReason { get; set; }

    /// <summary>
    /// User feedback on cancellation
    /// </summary>
    public string? CancellationFeedback { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;

    [ForeignKey(nameof(PlanId))]
    public virtual SubscriptionPlan Plan { get; set; } = null!;

    public virtual ICollection<SubscriptionRevenue> SubscriptionRevenues { get; set; } = new List<SubscriptionRevenue>();
    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();

    // Computed properties
    [NotMapped]
    public bool IsActive => Status == "active" && CurrentPeriodEnd > DateTime.UtcNow;

    [NotMapped]
    public bool IsInTrial => TrialEndsAt.HasValue && TrialEndsAt > DateTime.UtcNow;

    [NotMapped]
    public int DaysUntilRenewal => IsActive ? (CurrentPeriodEnd - DateTime.UtcNow).Days : 0;
}
