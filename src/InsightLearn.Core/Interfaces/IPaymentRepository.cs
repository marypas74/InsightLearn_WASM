using InsightLearn.Core.Entities;

namespace InsightLearn.Core.Interfaces;

/// <summary>
/// Repository interface for Payment entity operations
/// </summary>
public interface IPaymentRepository
{
    /// <summary>
    /// Gets all payments with pagination
    /// </summary>
    Task<IEnumerable<Payment>> GetAllAsync(int page = 1, int pageSize = 10);

    /// <summary>
    /// Gets a payment by its unique identifier
    /// </summary>
    Task<Payment?> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets a payment by transaction ID (from payment gateway)
    /// </summary>
    Task<Payment?> GetByTransactionIdAsync(string transactionId);

    /// <summary>
    /// Gets all payments for a specific user
    /// </summary>
    Task<IEnumerable<Payment>> GetByUserIdAsync(Guid userId);

    /// <summary>
    /// Gets all payments for a specific course
    /// </summary>
    Task<IEnumerable<Payment>> GetByCourseIdAsync(Guid courseId);

    /// <summary>
    /// Creates a new payment record
    /// </summary>
    Task<Payment> CreateAsync(Payment payment);

    /// <summary>
    /// Updates an existing payment
    /// </summary>
    Task<Payment> UpdateAsync(Payment payment);

    /// <summary>
    /// Updates payment status
    /// </summary>
    Task UpdateStatusAsync(Guid paymentId, PaymentStatus status);

    /// <summary>
    /// Processes a refund for a payment
    /// </summary>
    Task ProcessRefundAsync(Guid paymentId, decimal refundAmount);

    /// <summary>
    /// Gets transactions within a date range and optional status filter
    /// </summary>
    Task<IEnumerable<Payment>> GetTransactionsAsync(DateTime? startDate = null, DateTime? endDate = null, PaymentStatus? status = null);

    /// <summary>
    /// Gets total revenue within a date range
    /// </summary>
    Task<decimal> GetTotalRevenueAsync(DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// Gets total count of payments
    /// </summary>
    Task<int> GetTotalCountAsync();
}
