using InsightLearn.WebAssembly.Models;

namespace InsightLearn.WebAssembly.Services;

public class PaymentIntent
{
    public Guid PaymentId { get; set; }
    public string ClientSecret { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "usd";
}

public interface IPaymentService
{
    Task<ApiResponse<PaymentIntent>> CreatePaymentIntentAsync(Guid courseId, decimal amount);
    Task<ApiResponse> ConfirmPaymentAsync(string paymentIntentId);
    Task<ApiResponse<List<PaymentHistory>>> GetPaymentHistoryAsync();
}

public class PaymentHistory
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "usd";
    public DateTime CreatedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? CourseTitle { get; set; }
}
