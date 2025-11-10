namespace InsightLearn.Core.DTOs.Payment;

public class StripeCheckoutDto
{
    public string SessionId { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
    public string CheckoutUrl { get; set; } = string.Empty;
    public Guid PaymentId { get; set; }
}
