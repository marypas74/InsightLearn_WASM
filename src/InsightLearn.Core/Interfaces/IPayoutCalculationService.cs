using InsightLearn.Core.Entities;

namespace InsightLearn.Core.Interfaces;

public interface IPayoutCalculationService
{
    /// <summary>
    /// Calculates monthly payout for a specific instructor
    /// Formula: (total_revenue * 0.80) * (instructor_engagement / total_platform_engagement)
    /// </summary>
    Task<InstructorPayout?> CalculateMonthlyPayoutAsync(Guid instructorId, int month, int year);

    /// <summary>
    /// Calculates payouts for ALL instructors for a given month/year
    /// Creates pending InstructorPayout records
    /// </summary>
    Task<List<InstructorPayout>> CalculateAllPayoutsForPeriodAsync(int month, int year);

    /// <summary>
    /// Processes pending payouts (changes status from pending to processing)
    /// </summary>
    Task<int> ProcessPendingPayoutsAsync();

    /// <summary>
    /// Executes a payout to an instructor's Stripe Connect account
    /// Requires StripeConnectService (optional for Week 2)
    /// </summary>
    Task<bool> ExecutePayoutAsync(Guid payoutId);

    /// <summary>
    /// Gets payout summary for a specific period
    /// </summary>
    Task<PayoutPeriodSummary> GetPayoutSummaryAsync(int month, int year);

    /// <summary>
    /// Gets instructor's payout history
    /// </summary>
    Task<List<InstructorPayout>> GetInstructorPayoutHistoryAsync(Guid instructorId, int page = 1, int pageSize = 12);

    /// <summary>
    /// Gets instructor's total earned (lifetime)
    /// </summary>
    Task<decimal> GetInstructorTotalEarnedAsync(Guid instructorId);

    /// <summary>
    /// Gets top earning instructors for a period
    /// </summary>
    Task<List<InstructorEarningsSummary>> GetTopEarningInstructorsAsync(int month, int year, int topN = 10);

    /// <summary>
    /// Recalculates an existing payout (admin feature)
    /// </summary>
    Task<InstructorPayout?> RecalculatePayoutAsync(Guid payoutId);

    /// <summary>
    /// Marks a payout as failed with error message
    /// </summary>
    Task<bool> MarkPayoutAsFailedAsync(Guid payoutId, string errorMessage);
}

/// <summary>
/// Summary of payouts for a specific period
/// </summary>
public class PayoutPeriodSummary
{
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal PlatformCommission { get; set; }
    public decimal TotalInstructorPayouts { get; set; }
    public long TotalPlatformEngagementMinutes { get; set; }
    public int TotalInstructors { get; set; }
    public int PayoutsPending { get; set; }
    public int PayoutsProcessing { get; set; }
    public int PayoutsPaid { get; set; }
    public int PayoutsFailed { get; set; }
}

/// <summary>
/// Instructor earnings summary for leaderboard
/// </summary>
public class InstructorEarningsSummary
{
    public Guid InstructorId { get; set; }
    public string InstructorName { get; set; } = string.Empty;
    public string InstructorEmail { get; set; } = string.Empty;
    public decimal PayoutAmount { get; set; }
    public long EngagementMinutes { get; set; }
    public decimal EngagementPercentage { get; set; }
    public int UniqueStudents { get; set; }
    public int CoursesWithEngagement { get; set; }
}
