using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Core.DTOs.Payment;

/// <summary>
/// DTO for Stripe payment intent response
/// </summary>
public class PaymentIntentDto
{
    [Required(ErrorMessage = "Payment ID is required")]
    public Guid PaymentId { get; set; }

    [Required(ErrorMessage = "Client secret is required")]
    [StringLength(500, ErrorMessage = "Client secret cannot exceed 500 characters")]
    public string ClientSecret { get; set; } = string.Empty;

    [Required(ErrorMessage = "Amount is required")]
    [Range(0.01, 50000.00, ErrorMessage = "Amount must be between $0.01 and $50,000")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Currency is required")]
    [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency must be a 3-letter ISO code")]
    [RegularExpression(@"^[A-Z]{3}$", ErrorMessage = "Currency must be a valid 3-letter ISO 4217 code")]
    public string Currency { get; set; } = "USD";
}
