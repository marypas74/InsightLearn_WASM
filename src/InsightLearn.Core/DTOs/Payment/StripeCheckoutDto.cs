using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Core.DTOs.Payment;

/// <summary>
/// DTO for Stripe checkout session response
/// </summary>
public class StripeCheckoutDto
{
    [Required(ErrorMessage = "Stripe session ID is required")]
    [StringLength(500, ErrorMessage = "Session ID cannot exceed 500 characters")]
    public string SessionId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Stripe public key is required")]
    [StringLength(200, ErrorMessage = "Public key cannot exceed 200 characters")]
    [RegularExpression(@"^pk_(test|live)_[a-zA-Z0-9]+$", ErrorMessage = "Invalid Stripe public key format")]
    public string PublicKey { get; set; } = string.Empty;

    [Required(ErrorMessage = "Checkout URL is required")]
    [StringLength(2000, ErrorMessage = "Checkout URL cannot exceed 2000 characters")]
    [Url(ErrorMessage = "Invalid checkout URL format")]
    public string CheckoutUrl { get; set; } = string.Empty;

    [Required(ErrorMessage = "Payment ID is required")]
    public Guid PaymentId { get; set; }
}
