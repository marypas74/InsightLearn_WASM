namespace InsightLearn.Core.DTOs.Instructor;

/// <summary>
/// DTO for Stripe payout schedule configuration
/// </summary>
public class PayoutScheduleDto
{
    /// <summary>
    /// Payout frequency: daily, weekly, monthly
    /// </summary>
    public string Interval { get; set; } = "monthly";

    /// <summary>
    /// Number of days delay before payout (Stripe default is 7)
    /// </summary>
    public int DelayDays { get; set; }

    /// <summary>
    /// Currency for payouts
    /// </summary>
    public string Currency { get; set; } = "USD";
}
