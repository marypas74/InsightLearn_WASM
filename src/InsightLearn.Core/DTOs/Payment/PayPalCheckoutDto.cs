namespace InsightLearn.Core.DTOs.Payment;

public class PayPalCheckoutDto
{
    public string OrderId { get; set; } = string.Empty;
    public string ApprovalUrl { get; set; } = string.Empty;
    public Guid PaymentId { get; set; }
}
