using InsightLearn.Core.DTOs.Payout;

namespace InsightLearn.WebAssembly.Services.Payout;

/// <summary>
/// Service for managing instructor payouts (frontend)
/// </summary>
public interface IPayoutService
{
    /// <summary>
    /// Get payout history for current instructor
    /// </summary>
    Task<PayoutHistoryResponse?> GetMyPayoutHistoryAsync(int page = 1, int pageSize = 12);

    /// <summary>
    /// Get analytics for current instructor
    /// </summary>
    Task<InstructorAnalyticsDto?> GetMyAnalyticsAsync();

    /// <summary>
    /// Get lifetime earnings for current instructor
    /// </summary>
    Task<decimal> GetLifetimeEarningsAsync();

    /// <summary>
    /// Get pending payout for current instructor
    /// </summary>
    Task<decimal> GetPendingPayoutAsync();

    /// <summary>
    /// Get all payouts (Admin only)
    /// </summary>
    Task<PayoutHistoryResponse?> GetAllPayoutsAsync(string? status = null, int page = 1, int pageSize = 12);

    /// <summary>
    /// Get payout summary for a period (Admin only)
    /// </summary>
    Task<PayoutSummaryDto?> GetPayoutSummaryAsync(DateTime periodStart, DateTime periodEnd);

    /// <summary>
    /// Calculate payouts for a period (Admin only)
    /// </summary>
    Task<bool> CalculatePayoutsAsync(DateTime periodStart, DateTime periodEnd);

    /// <summary>
    /// Process pending payouts (Admin only)
    /// </summary>
    Task<bool> ProcessPendingPayoutsAsync();

    /// <summary>
    /// Execute payout payment (Admin only)
    /// </summary>
    Task<bool> ExecutePayoutAsync(Guid payoutId);
}

/// <summary>
/// Response for payout history with pagination
/// </summary>
public class PayoutHistoryResponse
{
    public List<PayoutDto> Payouts { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
