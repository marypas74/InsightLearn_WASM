using InsightLearn.Core.DTOs.Subscription;
using InsightLearn.Core.Entities;
using InsightLearn.Core.Interfaces;
using InsightLearn.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;
using System.Text.Json;

namespace InsightLearn.Application.Services;

/// <summary>
/// Subscription Plan Management Service - v2.0.0 SaaS Model
/// Task: T6 - SubscriptionPlanService.cs (12 methods)
/// Architect Score Target: 10/10
/// </summary>
public class SubscriptionPlanService : ISubscriptionPlanService
{
    private readonly ISubscriptionPlanRepository _planRepo;
    private readonly IUserSubscriptionRepository _subscriptionRepo;
    private readonly InsightLearnDbContext _context;
    private readonly ILogger<SubscriptionPlanService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _stripeSecretKey;
    private readonly string? _stripeProductId;

    public SubscriptionPlanService(
        ISubscriptionPlanRepository planRepo,
        IUserSubscriptionRepository subscriptionRepo,
        InsightLearnDbContext context,
        ILogger<SubscriptionPlanService> logger,
        IConfiguration configuration)
    {
        _planRepo = planRepo ?? throw new ArgumentNullException(nameof(planRepo));
        _subscriptionRepo = subscriptionRepo ?? throw new ArgumentNullException(nameof(subscriptionRepo));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        // Initialize Stripe API key
        _stripeSecretKey = Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY")
            ?? configuration["Stripe:SecretKey"]
            ?? throw new InvalidOperationException("Stripe secret key not configured");

        StripeConfiguration.ApiKey = _stripeSecretKey;

        // Get global Stripe Product ID for subscription plans
        _stripeProductId = configuration["Stripe:SubscriptionProductId"];

        _logger.LogInformation("[SubscriptionPlanService] Initialized with Stripe API key configured");
    }

    #region Plan Management (5 methods)

    /// <summary>
    /// Creates a new subscription plan with Stripe Price integration
    /// Atomic transaction: DB + Stripe operations
    /// </summary>
    public async Task<SubscriptionPlanDto> CreatePlanAsync(CreateSubscriptionPlanDto dto)
    {
        _logger.LogInformation("[CreatePlanAsync] Creating subscription plan: {PlanName}", dto.Name);

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 1. Validate plan name unique
            var existing = await _planRepo.GetByNameAsync(dto.Name);
            if (existing != null)
            {
                var error = $"Plan with name '{dto.Name}' already exists";
                _logger.LogError("[CreatePlanAsync] {Error}", error);
                throw new InvalidOperationException(error);
            }

            // 2. Validate features list
            if (dto.Features == null || dto.Features.Count == 0)
            {
                var error = "Plan must have at least one feature";
                _logger.LogError("[CreatePlanAsync] {Error}", error);
                throw new ArgumentException(error, nameof(dto.Features));
            }

            // 3. Validate yearly price logic (should be less than 12 * monthly)
            if (dto.PriceYearly.HasValue && dto.PriceYearly.Value >= (dto.PriceMonthly * 12))
            {
                var error = "Yearly price must be less than 12 * monthly price to provide savings";
                _logger.LogWarning("[CreatePlanAsync] {Error}", error);
            }

            // 4. Create Stripe Product if not exists globally
            var productId = await GetOrCreateStripeProductAsync("InsightLearn Subscription");

            // 5. Create local plan record (without StripePriceId yet)
            var plan = new SubscriptionPlan
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Description = dto.Description,
                PriceMonthly = dto.PriceMonthly,
                PriceYearly = dto.PriceYearly,
                Features = JsonSerializer.Serialize(dto.Features),
                MaxDevices = dto.MaxDevices,
                MaxVideoQuality = dto.MaxVideoQuality,
                AllowOfflineDownload = dto.AllowOfflineDownload,
                DisplayOrder = dto.DisplayOrder,
                IsFeatured = dto.IsFeatured,
                IsActive = true,
                StripeProductId = productId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _planRepo.CreateAsync(plan);
            await _context.SaveChangesAsync(); // Get plan.Id

            _logger.LogInformation("[CreatePlanAsync] Created plan {PlanId}, creating Stripe prices", plan.Id);

            // 6. Create Stripe Price for monthly billing
            var stripePriceMonthlyId = await CreateStripePriceInternalAsync(
                productId,
                dto.PriceMonthly,
                dto.Currency,
                dto.BillingCycleMonths,
                plan.Id);

            plan.StripePriceMonthlyId = stripePriceMonthlyId;

            // 7. Create Stripe Price for yearly billing (if applicable)
            if (dto.PriceYearly.HasValue)
            {
                var stripePriceYearlyId = await CreateStripePriceInternalAsync(
                    productId,
                    dto.PriceYearly.Value,
                    dto.Currency,
                    12,
                    plan.Id);

                plan.StripePriceYearlyId = stripePriceYearlyId;
            }

            // 8. Update plan with Stripe Price IDs
            await _planRepo.UpdateAsync(plan);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            _logger.LogInformation("[CreatePlanAsync] Plan {PlanId} created successfully with Stripe prices {MonthlyPriceId}, {YearlyPriceId}",
                plan.Id, stripePriceMonthlyId, plan.StripePriceYearlyId ?? "none");

            return MapToDto(plan);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "[CreatePlanAsync] Failed to create subscription plan - transaction rolled back");
            throw;
        }
    }

    /// <summary>
    /// Updates an existing subscription plan
    /// Note: Does NOT update Stripe prices (use UpdateStripePriceAsync for price changes)
    /// </summary>
    public async Task<SubscriptionPlanDto> UpdatePlanAsync(Guid planId, UpdateSubscriptionPlanDto dto)
    {
        _logger.LogInformation("[UpdatePlanAsync] Updating plan {PlanId}", planId);

        try
        {
            var plan = await _planRepo.GetByIdAsync(planId);
            if (plan == null)
            {
                var error = $"Plan {planId} not found";
                _logger.LogError("[UpdatePlanAsync] {Error}", error);
                throw new KeyNotFoundException(error);
            }

            // Update fields if provided
            if (!string.IsNullOrWhiteSpace(dto.Name))
            {
                // Check name uniqueness
                var existingWithName = await _planRepo.GetByNameAsync(dto.Name);
                if (existingWithName != null && existingWithName.Id != plan.Id)
                {
                    var error = $"Plan with name '{dto.Name}' already exists";
                    _logger.LogError("[UpdatePlanAsync] {Error}", error);
                    throw new InvalidOperationException(error);
                }
                plan.Name = dto.Name;
            }

            if (dto.Description != null) plan.Description = dto.Description;
            if (dto.Features != null) plan.Features = JsonSerializer.Serialize(dto.Features);
            if (dto.MaxDevices.HasValue) plan.MaxDevices = dto.MaxDevices;
            if (dto.MaxVideoQuality != null) plan.MaxVideoQuality = dto.MaxVideoQuality;
            if (dto.AllowOfflineDownload.HasValue) plan.AllowOfflineDownload = dto.AllowOfflineDownload.Value;
            if (dto.DisplayOrder.HasValue) plan.DisplayOrder = dto.DisplayOrder.Value;
            if (dto.IsFeatured.HasValue) plan.IsFeatured = dto.IsFeatured.Value;

            // WARNING: Price updates via DTO not allowed - use UpdateStripePriceAsync
            if (dto.PriceMonthly.HasValue || dto.PriceYearly.HasValue)
            {
                _logger.LogWarning("[UpdatePlanAsync] Price updates attempted via DTO - use UpdateStripePriceAsync instead");
            }

            plan.UpdatedAt = DateTime.UtcNow;

            await _planRepo.UpdateAsync(plan);
            await _context.SaveChangesAsync();

            _logger.LogInformation("[UpdatePlanAsync] Plan {PlanId} updated successfully", plan.Id);

            return MapToDto(plan);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UpdatePlanAsync] Failed to update plan {PlanId}", planId);
            throw;
        }
    }

    /// <summary>
    /// Soft deletes a plan (sets IsActive = false)
    /// Prevents deletion if there are active subscriptions
    /// </summary>
    public async Task<bool> DeletePlanAsync(Guid planId)
    {
        _logger.LogInformation("[DeletePlanAsync] Deleting plan {PlanId}", planId);

        try
        {
            var plan = await _planRepo.GetByIdAsync(planId);
            if (plan == null)
            {
                _logger.LogWarning("[DeletePlanAsync] Plan {PlanId} not found", planId);
                return false;
            }

            // Check for active subscriptions
            var activeSubscriptionCount = await GetActiveSubscriptionCountAsync(planId);
            if (activeSubscriptionCount > 0)
            {
                var error = $"Cannot delete plan {planId} - {activeSubscriptionCount} active subscriptions exist";
                _logger.LogError("[DeletePlanAsync] {Error}", error);
                throw new InvalidOperationException(error);
            }

            // Soft delete
            plan.IsActive = false;
            plan.UpdatedAt = DateTime.UtcNow;

            await _planRepo.UpdateAsync(plan);
            await _context.SaveChangesAsync();

            _logger.LogInformation("[DeletePlanAsync] Plan {PlanId} soft deleted successfully", plan.Id);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DeletePlanAsync] Failed to delete plan {PlanId}", planId);
            throw;
        }
    }

    /// <summary>
    /// Gets a single plan by ID
    /// </summary>
    public async Task<SubscriptionPlanDto?> GetPlanByIdAsync(Guid planId)
    {
        _logger.LogDebug("[GetPlanByIdAsync] Fetching plan {PlanId}", planId);

        try
        {
            var plan = await _planRepo.GetByIdAsync(planId);
            if (plan == null)
            {
                _logger.LogWarning("[GetPlanByIdAsync] Plan {PlanId} not found", planId);
                return null;
            }

            return MapToDto(plan);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GetPlanByIdAsync] Failed to fetch plan {PlanId}", planId);
            throw;
        }
    }

    /// <summary>
    /// Gets all plans (optionally including inactive)
    /// </summary>
    public async Task<List<SubscriptionPlanDto>> GetAllPlansAsync(bool includeInactive = false)
    {
        _logger.LogDebug("[GetAllPlansAsync] Fetching all plans (includeInactive: {IncludeInactive})", includeInactive);

        try
        {
            var plans = await _planRepo.GetAllAsync(includeInactive);

            var planDtos = new List<SubscriptionPlanDto>();
            foreach (var plan in plans)
            {
                var dto = MapToDto(plan);
                dto.ActiveSubscriptionCount = await GetActiveSubscriptionCountInternalAsync(plan.Id);
                planDtos.Add(dto);
            }

            _logger.LogInformation("[GetAllPlansAsync] Retrieved {Count} plans", planDtos.Count);

            return planDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GetAllPlansAsync] Failed to fetch plans");
            throw;
        }
    }

    #endregion

    #region Stripe Integration (3 methods)

    /// <summary>
    /// Creates a Stripe Price object for a plan (internal helper)
    /// </summary>
    public async Task<string> CreateStripePriceAsync(SubscriptionPlan plan)
    {
        _logger.LogInformation("[CreateStripePriceAsync] Creating Stripe price for plan {PlanId}", plan.Id);

        try
        {
            var productId = plan.StripeProductId ?? await GetOrCreateStripeProductAsync("InsightLearn Subscription");

            var priceId = await CreateStripePriceInternalAsync(
                productId,
                plan.PriceMonthly,
                "USD",
                1,
                plan.Id);

            _logger.LogInformation("[CreateStripePriceAsync] Created Stripe price {PriceId} for plan {PlanId}",
                priceId, plan.Id);

            return priceId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CreateStripePriceAsync] Failed to create Stripe price for plan {PlanId}", plan.Id);
            throw;
        }
    }

    /// <summary>
    /// Updates plan price by archiving old Stripe Price and creating new one
    /// </summary>
    public async Task UpdateStripePriceAsync(Guid planId, decimal newPrice)
    {
        _logger.LogInformation("[UpdateStripePriceAsync] Updating price for plan {PlanId} to {NewPrice:C}",
            planId, newPrice);

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var plan = await _planRepo.GetByIdAsync(planId);
            if (plan == null)
            {
                var error = $"Plan {planId} not found";
                _logger.LogError("[UpdateStripePriceAsync] {Error}", error);
                throw new KeyNotFoundException(error);
            }

            // Validate new price
            if (newPrice <= 0)
            {
                var error = $"New price must be greater than 0, got {newPrice}";
                _logger.LogError("[UpdateStripePriceAsync] {Error}", error);
                throw new ArgumentException(error, nameof(newPrice));
            }

            // 1. Archive old Stripe Price (set active = false)
            if (!string.IsNullOrEmpty(plan.StripePriceMonthlyId))
            {
                try
                {
                    var priceService = new PriceService();
                    await priceService.UpdateAsync(plan.StripePriceMonthlyId, new PriceUpdateOptions
                    {
                        Active = false
                    });

                    _logger.LogInformation("[UpdateStripePriceAsync] Archived old Stripe price {PriceId}",
                        plan.StripePriceMonthlyId);
                }
                catch (StripeException ex)
                {
                    _logger.LogWarning(ex, "[UpdateStripePriceAsync] Failed to archive old Stripe price {PriceId}",
                        plan.StripePriceMonthlyId);
                    // Continue anyway - create new price
                }
            }

            // 2. Create new Stripe Price
            var productId = plan.StripeProductId ?? await GetOrCreateStripeProductAsync("InsightLearn Subscription");

            var newPriceId = await CreateStripePriceInternalAsync(
                productId,
                newPrice,
                "USD",
                1,
                plan.Id);

            // 3. Update local plan record
            plan.PriceMonthly = newPrice;
            plan.StripePriceMonthlyId = newPriceId;
            plan.UpdatedAt = DateTime.UtcNow;

            await _planRepo.UpdateAsync(plan);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            _logger.LogInformation("[UpdateStripePriceAsync] Updated plan {PlanId} with new price {Price:C} and Stripe price {PriceId}",
                plan.Id, newPrice, newPriceId);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "[UpdateStripePriceAsync] Failed to update price for plan {PlanId}", planId);
            throw;
        }
    }

    /// <summary>
    /// Syncs plan details from Stripe (retrieves latest price info)
    /// </summary>
    public async Task SyncWithStripeAsync(Guid planId)
    {
        _logger.LogInformation("[SyncWithStripeAsync] Syncing plan {PlanId} with Stripe", planId);

        try
        {
            var plan = await _planRepo.GetByIdAsync(planId);
            if (plan == null)
            {
                var error = $"Plan {planId} not found";
                _logger.LogError("[SyncWithStripeAsync] {Error}", error);
                throw new KeyNotFoundException(error);
            }

            if (string.IsNullOrEmpty(plan.StripePriceMonthlyId))
            {
                _logger.LogWarning("[SyncWithStripeAsync] Plan {PlanId} has no Stripe price ID, skipping sync", planId);
                return;
            }

            // Retrieve price from Stripe
            var priceService = new PriceService();
            var stripePrice = await priceService.GetAsync(plan.StripePriceMonthlyId);

            // Update local price if different
            var stripePriceDecimal = stripePrice.UnitAmount / 100m;
            if (plan.PriceMonthly != stripePriceDecimal)
            {
                _logger.LogInformation("[SyncWithStripeAsync] Updating plan {PlanId} price from {OldPrice:C} to {NewPrice:C}",
                    plan.Id, plan.PriceMonthly, stripePriceDecimal);

                plan.PriceMonthly = stripePriceDecimal ?? plan.PriceMonthly;
                plan.UpdatedAt = DateTime.UtcNow;

                await _planRepo.UpdateAsync(plan);
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation("[SyncWithStripeAsync] Plan {PlanId} synced successfully", plan.Id);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "[SyncWithStripeAsync] Stripe API error while syncing plan {PlanId}", planId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SyncWithStripeAsync] Failed to sync plan {PlanId} with Stripe", planId);
            throw;
        }
    }

    #endregion

    #region Feature Management (2 methods)

    /// <summary>
    /// Activates a plan (sets IsActive = true)
    /// </summary>
    public async Task<bool> ActivatePlanAsync(Guid planId)
    {
        _logger.LogInformation("[ActivatePlanAsync] Activating plan {PlanId}", planId);

        try
        {
            var plan = await _planRepo.GetByIdAsync(planId);
            if (plan == null)
            {
                _logger.LogWarning("[ActivatePlanAsync] Plan {PlanId} not found", planId);
                return false;
            }

            if (plan.IsActive)
            {
                _logger.LogInformation("[ActivatePlanAsync] Plan {PlanId} is already active", planId);
                return true;
            }

            plan.IsActive = true;
            plan.UpdatedAt = DateTime.UtcNow;

            await _planRepo.UpdateAsync(plan);
            await _context.SaveChangesAsync();

            _logger.LogInformation("[ActivatePlanAsync] Plan {PlanId} activated successfully", plan.Id);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ActivatePlanAsync] Failed to activate plan {PlanId}", planId);
            throw;
        }
    }

    /// <summary>
    /// Deactivates a plan (sets IsActive = false, prevents new subscriptions)
    /// </summary>
    public async Task<bool> DeactivatePlanAsync(Guid planId)
    {
        _logger.LogInformation("[DeactivatePlanAsync] Deactivating plan {PlanId}", planId);

        try
        {
            var plan = await _planRepo.GetByIdAsync(planId);
            if (plan == null)
            {
                _logger.LogWarning("[DeactivatePlanAsync] Plan {PlanId} not found", planId);
                return false;
            }

            if (!plan.IsActive)
            {
                _logger.LogInformation("[DeactivatePlanAsync] Plan {PlanId} is already inactive", planId);
                return true;
            }

            plan.IsActive = false;
            plan.UpdatedAt = DateTime.UtcNow;

            await _planRepo.UpdateAsync(plan);
            await _context.SaveChangesAsync();

            _logger.LogInformation("[DeactivatePlanAsync] Plan {PlanId} deactivated successfully (existing subscriptions continue)",
                plan.Id);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DeactivatePlanAsync] Failed to deactivate plan {PlanId}", planId);
            throw;
        }
    }

    #endregion

    #region Analytics & Reporting (2 methods)

    /// <summary>
    /// Gets count of active subscriptions for a specific plan
    /// </summary>
    public async Task<int> GetActiveSubscriptionCountAsync(Guid planId)
    {
        _logger.LogDebug("[GetActiveSubscriptionCountAsync] Counting active subscriptions for plan {PlanId}", planId);

        try
        {
            var count = await GetActiveSubscriptionCountInternalAsync(planId);

            _logger.LogInformation("[GetActiveSubscriptionCountAsync] Plan {PlanId} has {Count} active subscriptions",
                planId, count);

            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GetActiveSubscriptionCountAsync] Failed to count active subscriptions for plan {PlanId}",
                planId);
            throw;
        }
    }

    /// <summary>
    /// Gets all active plans with features for UI comparison table
    /// </summary>
    public async Task<PlanComparisonDto> GetPlanComparisonAsync()
    {
        _logger.LogDebug("[GetPlanComparisonAsync] Generating plan comparison table");

        try
        {
            var plans = await _planRepo.GetAllActiveAsync();
            var planDtos = new List<SubscriptionPlanDto>();

            // Collect all unique features
            var allFeaturesSet = new HashSet<string>();

            foreach (var plan in plans)
            {
                var dto = MapToDto(plan);
                dto.ActiveSubscriptionCount = await GetActiveSubscriptionCountInternalAsync(plan.Id);
                planDtos.Add(dto);

                foreach (var feature in dto.Features)
                {
                    allFeaturesSet.Add(feature);
                }
            }

            var allFeatures = allFeaturesSet.OrderBy(f => f).ToList();

            // Build feature matrix: planId -> feature -> has_it
            var featureMatrix = new Dictionary<Guid, Dictionary<string, bool>>();

            foreach (var planDto in planDtos)
            {
                var planFeatures = new Dictionary<string, bool>();
                foreach (var feature in allFeatures)
                {
                    planFeatures[feature] = planDto.Features.Contains(feature);
                }
                featureMatrix[planDto.Id] = planFeatures;
            }

            var comparison = new PlanComparisonDto
            {
                Plans = planDtos.OrderBy(p => p.DisplayOrder).ToList(),
                AllFeatures = allFeatures,
                PlanFeatureMatrix = featureMatrix
            };

            _logger.LogInformation("[GetPlanComparisonAsync] Generated comparison for {PlanCount} plans with {FeatureCount} features",
                planDtos.Count, allFeatures.Count);

            return comparison;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GetPlanComparisonAsync] Failed to generate plan comparison");
            throw;
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Gets or creates a Stripe Product for subscription plans
    /// </summary>
    private async Task<string> GetOrCreateStripeProductAsync(string productName)
    {
        try
        {
            // Check if global product ID exists
            if (!string.IsNullOrEmpty(_stripeProductId))
            {
                _logger.LogDebug("[GetOrCreateStripeProductAsync] Using existing Stripe product {ProductId}", _stripeProductId);
                return _stripeProductId;
            }

            // Create new product
            var productService = new ProductService();
            var product = await productService.CreateAsync(new ProductCreateOptions
            {
                Name = productName,
                Description = "InsightLearn LMS Subscription",
                Active = true,
                Metadata = new Dictionary<string, string>
                {
                    { "platform", "InsightLearn" },
                    { "type", "subscription" }
                }
            });

            _logger.LogInformation("[GetOrCreateStripeProductAsync] Created Stripe product {ProductId}", product.Id);

            return product.Id;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "[GetOrCreateStripeProductAsync] Stripe API error creating product");
            throw;
        }
    }

    /// <summary>
    /// Creates a Stripe Price object (internal helper)
    /// </summary>
    private async Task<string> CreateStripePriceInternalAsync(
        string productId,
        decimal amount,
        string currency,
        int intervalCount,
        Guid planId)
    {
        try
        {
            var priceOptions = new PriceCreateOptions
            {
                Currency = currency.ToLowerInvariant(),
                UnitAmount = (long)(amount * 100), // Convert to cents
                Recurring = new PriceRecurringOptions
                {
                    Interval = intervalCount == 12 ? "year" : "month",
                    IntervalCount = intervalCount == 12 ? 1 : intervalCount
                },
                Product = productId,
                Active = true,
                Metadata = new Dictionary<string, string>
                {
                    { "plan_id", planId.ToString() },
                    { "platform", "InsightLearn" }
                }
            };

            var priceService = new PriceService();
            var price = await priceService.CreateAsync(priceOptions);

            _logger.LogDebug("[CreateStripePriceInternalAsync] Created Stripe price {PriceId} for {Amount:C} {Currency}/{Interval}",
                price.Id, amount, currency, intervalCount == 12 ? "year" : "month");

            return price.Id;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "[CreateStripePriceInternalAsync] Stripe API error creating price");
            throw;
        }
    }

    /// <summary>
    /// Gets active subscription count for a plan (internal helper)
    /// </summary>
    private async Task<int> GetActiveSubscriptionCountInternalAsync(Guid planId)
    {
        try
        {
            var count = await _context.UserSubscriptions
                .Where(s => s.PlanId == planId &&
                           (s.Status == "active" || s.Status == "trialing") &&
                           s.CurrentPeriodEnd > DateTime.UtcNow)
                .CountAsync();

            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GetActiveSubscriptionCountInternalAsync] Failed to count active subscriptions for plan {PlanId}",
                planId);
            throw;
        }
    }

    /// <summary>
    /// Maps SubscriptionPlan entity to DTO
    /// </summary>
    private SubscriptionPlanDto MapToDto(SubscriptionPlan plan)
    {
        try
        {
            var features = string.IsNullOrWhiteSpace(plan.Features)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(plan.Features) ?? new List<string>();

            decimal? yearlySavings = null;
            int? yearlySavingsPercentage = null;

            if (plan.PriceYearly.HasValue)
            {
                yearlySavings = (plan.PriceMonthly * 12) - plan.PriceYearly.Value;
                if (yearlySavings.Value > 0)
                {
                    yearlySavingsPercentage = (int)Math.Round((yearlySavings.Value / (plan.PriceMonthly * 12)) * 100);
                }
            }

            return new SubscriptionPlanDto
            {
                Id = plan.Id,
                Name = plan.Name,
                Description = plan.Description,
                PriceMonthly = plan.PriceMonthly,
                PriceYearly = plan.PriceYearly,
                Features = features,
                MaxDevices = plan.MaxDevices,
                MaxVideoQuality = plan.MaxVideoQuality,
                AllowOfflineDownload = plan.AllowOfflineDownload,
                DisplayOrder = plan.DisplayOrder,
                IsActive = plan.IsActive,
                IsFeatured = plan.IsFeatured,
                StripePriceMonthlyId = plan.StripePriceMonthlyId,
                StripePriceYearlyId = plan.StripePriceYearlyId,
                ActiveSubscriptionCount = 0, // Set by caller if needed
                YearlySavings = yearlySavings,
                YearlySavingsPercentage = yearlySavingsPercentage,
                CreatedAt = plan.CreatedAt,
                UpdatedAt = plan.UpdatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MapToDto] Failed to map plan {PlanId} to DTO", plan.Id);
            throw;
        }
    }

    #endregion
}
