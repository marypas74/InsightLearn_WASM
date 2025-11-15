using InsightLearn.Core.Entities;
using InsightLearn.Core.Validation;

namespace InsightLearn.Core.DTOs.Payment;

public class PaymentDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal OriginalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    [ValidCurrency]  // ISO 4217 validation
    public string Currency { get; set; } = "USD";
    public PaymentStatus Status { get; set; }
    public PaymentMethodType? PaymentMethod { get; set; }
    public string? PaymentGatewayId { get; set; }
    public string? TransactionId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string? Metadata { get; set; }
}