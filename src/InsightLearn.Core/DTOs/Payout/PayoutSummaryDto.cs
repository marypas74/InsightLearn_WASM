using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Core.DTOs.Payout;

/// <summary>
/// Summary of payout information
/// </summary>
public class PayoutSummaryDto
{
    /// <summary>
    /// Instructor ID
    /// </summary>
    [Required(ErrorMessage = "Instructor ID is required")]
    public Guid InstructorId { get; set; }

    /// <summary>
    /// Total payouts made
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Total payouts must be a non-negative number")]
    public int TotalPayouts { get; set; }

    /// <summary>
    /// Total amount paid out
    /// </summary>
    [Range(0, 10000000.00, ErrorMessage = "Total paid must be between $0 and $10,000,000")]
    public decimal TotalPaid { get; set; }

    /// <summary>
    /// Pending payouts count
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Pending payouts must be a non-negative number")]
    public int PendingPayouts { get; set; }

    /// <summary>
    /// Pending amount
    /// </summary>
    [Range(0, 100000.00, ErrorMessage = "Pending amount must be between $0 and $100,000")]
    public decimal PendingAmount { get; set; }

    /// <summary>
    /// Average payout amount
    /// </summary>
    [Range(0, 100000.00, ErrorMessage = "Average payout must be between $0 and $100,000")]
    public decimal AveragePayoutAmount { get; set; }

    /// <summary>
    /// Largest payout amount
    /// </summary>
    [Range(0, 100000.00, ErrorMessage = "Largest payout must be between $0 and $100,000")]
    public decimal LargestPayout { get; set; }

    /// <summary>
    /// Last payout date
    /// </summary>
    public DateTime? LastPayoutDate { get; set; }

    /// <summary>
    /// Last payout amount
    /// </summary>
    [Range(0, 100000.00, ErrorMessage = "Last payout amount must be between $0 and $100,000")]
    public decimal? LastPayoutAmount { get; set; }

    /// <summary>
    /// Next scheduled payout date
    /// </summary>
    public DateTime? NextScheduledPayout { get; set; }

    /// <summary>
    /// List of recent payouts
    /// </summary>
    [Required(ErrorMessage = "Recent payouts list is required")]
    public List<PayoutDto> RecentPayouts { get; set; } = new();
}
