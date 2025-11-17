namespace InsightLearn.Core.DTOs.Revenue;

/// <summary>
/// Revenue summary for a single month
/// </summary>
public class MonthlyRevenueDto
{
    /// <summary>
    /// Year (e.g., 2025)
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Month (1-12)
    /// </summary>
    public int Month { get; set; }

    /// <summary>
    /// Month name (e.g., "January")
    /// </summary>
    public string MonthName => new DateTime(Year, Month, 1).ToString("MMMM");

    /// <summary>
    /// Total revenue for the month
    /// </summary>
    public decimal TotalRevenue { get; set; }

    /// <summary>
    /// Number of successful transactions
    /// </summary>
    public int TransactionCount { get; set; }

    /// <summary>
    /// Average transaction value (TotalRevenue / TransactionCount)
    /// </summary>
    public decimal AverageTransactionValue { get; set; }

    /// <summary>
    /// Number of new subscriptions created in this month
    /// </summary>
    public int NewSubscriptions { get; set; }

    /// <summary>
    /// Number of cancelled subscriptions in this month
    /// </summary>
    public int CancelledSubscriptions { get; set; }

    /// <summary>
    /// Number of failed payments in this month
    /// </summary>
    public int FailedPayments { get; set; }

    /// <summary>
    /// MRR at end of month
    /// </summary>
    public decimal MRR { get; set; }
}
