using InsightLearn.Core.DTOs.Payment;
using InsightLearn.Core.Entities;

namespace InsightLearn.Core.Interfaces;

/// <summary>
/// Enhanced payment processing service interface with Stripe and PayPal support
/// </summary>
public interface IPaymentService
{
    // ===== Checkout Methods =====
    /// <summary>
    /// Creates a Stripe checkout session for course purchase
    /// </summary>
    Task<StripeCheckoutDto> CreateStripeCheckoutAsync(CreatePaymentDto dto);

    /// <summary>
    /// Creates a PayPal checkout for course purchase
    /// </summary>
    Task<PayPalCheckoutDto> CreatePayPalCheckoutAsync(CreatePaymentDto dto);

    /// <summary>
    /// Creates a payment intent for direct payment processing
    /// </summary>
    Task<PaymentIntentDto> CreatePaymentIntentAsync(CreatePaymentDto dto);

    // ===== Payment Processing =====
    /// <summary>
    /// Processes Stripe webhook events
    /// </summary>
    Task<PaymentDto?> ProcessStripeWebhookAsync(string payload, string signature);

    /// <summary>
    /// Processes PayPal webhook events
    /// </summary>
    Task<PaymentDto?> ProcessPayPalWebhookAsync(string payload);

    /// <summary>
    /// Completes a payment and creates enrollment
    /// </summary>
    Task<PaymentDto?> CompletePaymentAsync(Guid paymentId, string transactionId);

    /// <summary>
    /// Cancels a pending payment
    /// </summary>
    Task<bool> CancelPaymentAsync(Guid paymentId);

    // ===== Refunds =====
    /// <summary>
    /// Processes a refund for a completed payment
    /// </summary>
    Task<bool> ProcessRefundAsync(Guid paymentId, decimal refundAmount, string reason);

    // ===== Coupon Validation =====
    /// <summary>
    /// Validates a coupon code and calculates discount
    /// </summary>
    Task<CouponValidationDto> ValidateCouponAsync(ApplyCouponDto dto);

    /// <summary>
    /// Applies a coupon to an existing payment
    /// </summary>
    Task<bool> ApplyCouponToPaymentAsync(Guid paymentId, string couponCode);

    // ===== Query Methods =====
    /// <summary>
    /// Gets a payment by its unique identifier
    /// </summary>
    Task<PaymentDto?> GetPaymentByIdAsync(Guid id);

    /// <summary>
    /// Gets a payment by transaction ID from payment gateway
    /// </summary>
    Task<PaymentDto?> GetPaymentByTransactionIdAsync(string transactionId);

    /// <summary>
    /// Gets all transactions for a specific user
    /// </summary>
    Task<List<TransactionDto>> GetUserTransactionsAsync(Guid userId);

    /// <summary>
    /// Gets all transactions with pagination and filtering
    /// </summary>
    Task<TransactionListDto> GetAllTransactionsAsync(int page = 1, int pageSize = 10, PaymentStatus? status = null);

    // ===== Revenue Reporting =====
    /// <summary>
    /// Gets total revenue within a date range
    /// </summary>
    Task<decimal> GetTotalRevenueAsync(DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// Gets revenue breakdown by month for a specific year
    /// </summary>
    Task<Dictionary<string, decimal>> GetRevenueByMonthAsync(int year);
}