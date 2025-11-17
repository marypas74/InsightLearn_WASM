namespace InsightLearn.Core.DTOs.Instructor;

/// <summary>
/// DTO for Instructor Connect Account details (output only)
/// </summary>
public class InstructorConnectAccountDto
{
    public Guid Id { get; set; }
    public Guid InstructorId { get; set; }
    public string StripeAccountId { get; set; } = string.Empty;

    /// <summary>
    /// Onboarding status: pending, incomplete, complete, restricted, disabled
    /// </summary>
    public string Status { get; set; } = "pending";

    public bool ChargesEnabled { get; set; }
    public bool PayoutsEnabled { get; set; }
    public string Country { get; set; } = "US";
    public string Currency { get; set; } = "USD";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
