using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Core.DTOs.Payment;

/// <summary>
/// DTO for PayPal checkout order response
/// </summary>
public class PayPalCheckoutDto
{
    [Required(ErrorMessage = "PayPal order ID is required")]
    [StringLength(200, ErrorMessage = "Order ID cannot exceed 200 characters")]
    public string OrderId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Approval URL is required")]
    [StringLength(2000, ErrorMessage = "Approval URL cannot exceed 2000 characters")]
    [Url(ErrorMessage = "Invalid approval URL format")]
    public string ApprovalUrl { get; set; } = string.Empty;

    [Required(ErrorMessage = "Payment ID is required")]
    public Guid PaymentId { get; set; }
}
