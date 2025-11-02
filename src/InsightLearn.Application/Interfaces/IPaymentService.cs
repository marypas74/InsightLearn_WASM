using InsightLearn.Application.DTOs;

namespace InsightLearn.Application.Interfaces;

public interface IPaymentService
{
    Task<PaymentIntentDto> CreatePaymentIntentAsync(CreatePaymentDto createPaymentDto);
    Task<bool> ConfirmPaymentAsync(string paymentIntentId);
    Task<PaymentDto?> GetPaymentAsync(Guid paymentId);
    Task<List<PaymentDto>> GetUserPaymentsAsync(Guid userId, int page = 1, int pageSize = 10);
    Task<RefundResultDto> ProcessRefundAsync(Guid paymentId, decimal? amount = null, string? reason = null);
    Task<CouponValidationDto> ValidateCouponAsync(string couponCode, Guid? courseId = null);
}