namespace InsightLearn.Core.DTOs.Payment;

public class PaymentIntentDto
{
    public Guid PaymentId { get; set; }
    public string ClientSecret { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
}
