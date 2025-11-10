using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Core.DTOs.Payment;

public class CreatePaymentDto
{
    [Required] public Guid UserId { get; set; }
    [Required] public Guid CourseId { get; set; }
    [Required][Range(0, double.MaxValue)] public decimal Amount { get; set; }
    [Required] public string PaymentMethod { get; set; } = string.Empty;
    public string? CouponCode { get; set; }
    public string Currency { get; set; } = "USD";
    public string? BillingAddress { get; set; }
}
