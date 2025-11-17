using System.ComponentModel.DataAnnotations;
using InsightLearn.Core.Validation;

namespace InsightLearn.Core.DTOs.Revenue;

/// <summary>
/// DTO for recording subscription revenue from Stripe webhook events
/// </summary>
public class RecordRevenueDto
{
    /// <summary>
    /// User ID associated with the subscription
    /// </summary>
    [Required(ErrorMessage = "User ID is required")]
    public Guid UserId { get; set; }

    /// <summary>
    /// Subscription ID
    /// </summary>
    [Required(ErrorMessage = "Subscription ID is required")]
    public Guid SubscriptionId { get; set; }

    /// <summary>
    /// Payment amount
    /// </summary>
    [Required(ErrorMessage = "Amount is required")]
    [Range(0.01, 100000, ErrorMessage = "Amount must be between 0.01 and 100,000")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Currency code (ISO 4217 validated)
    /// </summary>
    [ValidCurrency]
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Billing period start date
    /// </summary>
    [Required(ErrorMessage = "Billing period start is required")]
    public DateTime BillingPeriodStart { get; set; }

    /// <summary>
    /// Billing period end date
    /// </summary>
    [Required(ErrorMessage = "Billing period end is required")]
    public DateTime BillingPeriodEnd { get; set; }

    /// <summary>
    /// Stripe invoice ID (used for idempotency)
    /// </summary>
    [Required(ErrorMessage = "Stripe invoice ID is required")]
    [StringLength(255, ErrorMessage = "Stripe invoice ID cannot exceed 255 characters")]
    public string StripeInvoiceId { get; set; } = string.Empty;

    /// <summary>
    /// Stripe payment intent ID (optional)
    /// </summary>
    [StringLength(255, ErrorMessage = "Stripe payment intent ID cannot exceed 255 characters")]
    public string? StripePaymentIntentId { get; set; }

    /// <summary>
    /// Payment method type (e.g., "card", "sepa_debit")
    /// </summary>
    [StringLength(50, ErrorMessage = "Payment method cannot exceed 50 characters")]
    public string? PaymentMethod { get; set; }

    /// <summary>
    /// Last 4 digits of card (for display purposes)
    /// </summary>
    [StringLength(4, MinimumLength = 4, ErrorMessage = "Card last 4 must be exactly 4 digits")]
    [RegularExpression(@"^\d{4}$", ErrorMessage = "Card last 4 must be numeric")]
    public string? CardLast4 { get; set; }

    /// <summary>
    /// Card brand (e.g., "visa", "mastercard")
    /// </summary>
    [StringLength(20, ErrorMessage = "Card brand cannot exceed 20 characters")]
    public string? CardBrand { get; set; }

    /// <summary>
    /// Invoice PDF URL from Stripe
    /// </summary>
    [Url(ErrorMessage = "Invoice URL must be a valid URL")]
    [StringLength(500, ErrorMessage = "Invoice URL cannot exceed 500 characters")]
    public string? InvoiceUrl { get; set; }
}
