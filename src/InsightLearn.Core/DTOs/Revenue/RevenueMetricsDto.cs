namespace InsightLearn.Core.DTOs.Revenue;

/// <summary>
/// Comprehensive revenue metrics for a specific date range
/// </summary>
public class RevenueMetricsDto
{
    /// <summary>
    /// Total revenue (sum of all paid invoices)
    /// </summary>
    public decimal TotalRevenue { get; set; }

    /// <summary>
    /// Monthly Recurring Revenue (normalized)
    /// </summary>
    public decimal MRR { get; set; }

    /// <summary>
    /// Annual Recurring Revenue (MRR * 12)
    /// </summary>
    public decimal ARR { get; set; }

    /// <summary>
    /// Revenue growth rate compared to previous period (percentage)
    /// </summary>
    public decimal GrowthRate { get; set; }

    /// <summary>
    /// Churn rate (percentage of cancellations)
    /// </summary>
    public decimal ChurnRate { get; set; }

    /// <summary>
    /// Revenue breakdown by subscription plan
    /// Key: Plan name (e.g., "Basic", "Pro", "Premium")
    /// Value: Revenue amount
    /// </summary>
    public Dictionary<string, decimal> RevenueByPlan { get; set; } = new();

    /// <summary>
    /// Total number of successful payments
    /// </summary>
    public int TotalTransactions { get; set; }

    /// <summary>
    /// Total number of failed payments
    /// </summary>
    public int FailedTransactions { get; set; }

    /// <summary>
    /// Total number of refunds
    /// </summary>
    public int RefundedTransactions { get; set; }

    /// <summary>
    /// Average transaction value
    /// </summary>
    public decimal AverageTransactionValue { get; set; }

    /// <summary>
    /// Date range start
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Date range end
    /// </summary>
    public DateTime EndDate { get; set; }
}
