using InsightLearn.Core.DTOs.Revenue;
using InsightLearn.Core.Entities;

namespace InsightLearn.Core.Interfaces;

/// <summary>
/// Service interface for subscription revenue tracking and financial reporting
/// </summary>
public interface ISubscriptionRevenueService
{
    #region Revenue Recording (3 methods)

    /// <summary>
    /// Records a subscription payment from Stripe webhook (idempotent via StripeInvoiceId)
    /// </summary>
    Task RecordRevenueAsync(RecordRevenueDto dto);

    /// <summary>
    /// Records a refund for a revenue record
    /// </summary>
    Task RecordRefundAsync(Guid revenueId, decimal refundAmount, string reason);

    /// <summary>
    /// Records a chargeback for a revenue record
    /// </summary>
    Task RecordChargebackAsync(Guid revenueId, string reason);

    #endregion

    #region Revenue Reporting (5 methods)

    /// <summary>
    /// Gets total revenue for a specific month (used by PayoutCalculationService)
    /// </summary>
    Task<decimal> GetMonthlyRevenueAsync(int year, int month);

    /// <summary>
    /// Calculates Monthly Recurring Revenue (MRR) as of specific date
    /// </summary>
    Task<decimal> GetMRRAsync(DateTime? asOfDate = null);

    /// <summary>
    /// Calculates Annual Recurring Revenue (ARR = MRR * 12)
    /// </summary>
    Task<decimal> GetARRAsync(DateTime? asOfDate = null);

    /// <summary>
    /// Gets comprehensive revenue metrics for a date range
    /// </summary>
    Task<RevenueMetricsDto> GetRevenueMetricsAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// Gets monthly revenue breakdown for a full year (useful for charts)
    /// </summary>
    Task<List<MonthlyRevenueDto>> GetMonthlyRevenueBreakdownAsync(int year);

    #endregion
}
