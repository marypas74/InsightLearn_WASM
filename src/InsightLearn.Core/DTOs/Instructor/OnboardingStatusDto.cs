namespace InsightLearn.Core.DTOs.Instructor;

/// <summary>
/// DTO for Stripe Connect onboarding status with requirements
/// </summary>
public class OnboardingStatusDto
{
    /// <summary>
    /// Current onboarding status
    /// </summary>
    public string Status { get; set; } = "pending";

    /// <summary>
    /// Requirements that must be completed now
    /// </summary>
    public List<string> CurrentlyDue { get; set; } = new();

    /// <summary>
    /// Requirements that will need to be completed in the future
    /// </summary>
    public List<string> EventuallyDue { get; set; } = new();

    /// <summary>
    /// Onboarding completion percentage (0-100)
    /// </summary>
    public int CompletionPercentage { get; set; }

    /// <summary>
    /// Whether onboarding is fully complete
    /// </summary>
    public bool IsComplete { get; set; }
}
