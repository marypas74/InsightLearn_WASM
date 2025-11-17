using InsightLearn.Core.DTOs.Subscription;
using InsightLearn.Core.Entities;

namespace InsightLearn.Core.Interfaces;

/// <summary>
/// Service interface for subscription plan management
/// </summary>
public interface ISubscriptionPlanService
{
    // Plan Management (5 methods)
    Task<SubscriptionPlanDto> CreatePlanAsync(CreateSubscriptionPlanDto dto);
    Task<SubscriptionPlanDto> UpdatePlanAsync(Guid planId, UpdateSubscriptionPlanDto dto);
    Task<bool> DeletePlanAsync(Guid planId);
    Task<SubscriptionPlanDto?> GetPlanByIdAsync(Guid planId);
    Task<List<SubscriptionPlanDto>> GetAllPlansAsync(bool includeInactive = false);

    // Stripe Integration (3 methods)
    Task<string> CreateStripePriceAsync(SubscriptionPlan plan);
    Task UpdateStripePriceAsync(Guid planId, decimal newPrice);
    Task SyncWithStripeAsync(Guid planId);

    // Feature Management (2 methods)
    Task<bool> ActivatePlanAsync(Guid planId);
    Task<bool> DeactivatePlanAsync(Guid planId);

    // Analytics & Reporting (2 methods)
    Task<int> GetActiveSubscriptionCountAsync(Guid planId);
    Task<PlanComparisonDto> GetPlanComparisonAsync();
}
