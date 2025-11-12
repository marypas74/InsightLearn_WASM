using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InsightLearn.Core.Entities;

/// <summary>
/// User subscription entity - manages recurring subscription plans
/// TODO: Implement full subscription logic with Stripe integration
/// TODO: Add subscription plan features, billing cycles, payment processing
/// </summary>
public class Subscription
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid PlanId { get; set; }

    [Required]
    [StringLength(50)]
    public string Status { get; set; } = "Active"; // Active, Cancelled, Expired, PastDue

    [Required]
    [StringLength(20)]
    public string BillingInterval { get; set; } = "Monthly"; // Monthly, Yearly

    [Column(TypeName = "decimal(10,2)")]
    public decimal Price { get; set; }

    public DateTime StartDate { get; set; } = DateTime.UtcNow;

    public DateTime CurrentPeriodStart { get; set; } = DateTime.UtcNow;

    public DateTime CurrentPeriodEnd { get; set; }

    public DateTime? CancelledAt { get; set; }

    public DateTime? EndedAt { get; set; }

    [StringLength(255)]
    public string? StripeSubscriptionId { get; set; }

    [StringLength(255)]
    public string? StripeCustomerId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual User User { get; set; } = null!;

    public virtual SubscriptionPlan Plan { get; set; } = null!;

    public virtual ICollection<SubscriptionRevenue> SubscriptionRevenues { get; set; } = new List<SubscriptionRevenue>();
}
