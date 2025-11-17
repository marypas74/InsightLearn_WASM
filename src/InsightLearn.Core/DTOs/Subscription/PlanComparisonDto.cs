namespace InsightLearn.Core.DTOs.Subscription;

/// <summary>
/// DTO for comparing all active subscription plans in a table format
/// </summary>
public class PlanComparisonDto
{
    public List<SubscriptionPlanDto> Plans { get; set; } = new();
    public List<string> AllFeatures { get; set; } = new();
    public Dictionary<Guid, Dictionary<string, bool>> PlanFeatureMatrix { get; set; } = new();
}
