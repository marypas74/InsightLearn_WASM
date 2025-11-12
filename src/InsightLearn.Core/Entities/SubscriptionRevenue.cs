using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InsightLearn.Core.Entities;

/// <summary>
/// Tracks individual subscription payments (invoices)
/// </summary>
public class SubscriptionRevenue
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid SubscriptionId { get; set; }

    /// <summary>
    /// Payment amount
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Currency code (ISO 4217)
    /// </summary>
    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "EUR";

    /// <summary>
    /// Stripe invoice ID
    /// </summary>
    [MaxLength(255)]
    public string? StripeInvoiceId { get; set; }

    /// <summary>
    /// Stripe payment intent ID
    /// </summary>
    [MaxLength(255)]
    public string? StripePaymentIntentId { get; set; }

    /// <summary>
    /// Billing period start date
    /// </summary>
    [Required]
    public DateTime BillingPeriodStart { get; set; }

    /// <summary>
    /// Billing period end date
    /// </summary>
    [Required]
    public DateTime BillingPeriodEnd { get; set; }

    /// <summary>
    /// Payment status: "paid", "pending", "failed", "refunded", "partially_refunded"
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "pending";

    /// <summary>
    /// Date when payment was successfully received
    /// </summary>
    public DateTime? PaidAt { get; set; }

    /// <summary>
    /// Date when payment failed
    /// </summary>
    public DateTime? FailedAt { get; set; }

    /// <summary>
    /// Reason for payment failure
    /// </summary>
    [MaxLength(500)]
    public string? FailureReason { get; set; }

    /// <summary>
    /// Date when refund was issued
    /// </summary>
    public DateTime? RefundedAt { get; set; }

    /// <summary>
    /// Refund amount (if partially refunded)
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal? RefundAmount { get; set; }

    /// <summary>
    /// Reason for refund
    /// </summary>
    [MaxLength(500)]
    public string? RefundReason { get; set; }

    /// <summary>
    /// Payment method used (e.g., "card", "sepa_debit", "paypal")
    /// </summary>
    [MaxLength(50)]
    public string? PaymentMethod { get; set; }

    /// <summary>
    /// Last 4 digits of card (for display purposes)
    /// </summary>
    [MaxLength(4)]
    public string? CardLast4 { get; set; }

    /// <summary>
    /// Card brand (e.g., "visa", "mastercard")
    /// </summary>
    [MaxLength(20)]
    public string? CardBrand { get; set; }

    /// <summary>
    /// Invoice PDF URL from Stripe
    /// </summary>
    [MaxLength(500)]
    public string? InvoiceUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(SubscriptionId))]
    public virtual UserSubscription Subscription { get; set; } = null!;
}
