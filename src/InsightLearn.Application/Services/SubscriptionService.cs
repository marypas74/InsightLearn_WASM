using InsightLearn.Core.Entities;
using InsightLearn.Core.Interfaces;
using InsightLearn.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;

namespace InsightLearn.Application.Services;

/// <summary>
/// SaaS Subscription Service - Implements complete subscription lifecycle management
/// Version: v2.0.0
/// Task: T1 - SubscriptionService.cs (17 methods)
/// </summary>
public class SubscriptionService : ISubscriptionService
{
    private readonly IUserSubscriptionRepository _subscriptionRepo;
    private readonly ISubscriptionPlanRepository _planRepo;
    private readonly IEnrollmentRepository _enrollmentRepo;
    private readonly ICourseRepository _courseRepo;
    private readonly InsightLearnDbContext _context;
    private readonly ILogger<SubscriptionService> _logger;
    private readonly IConfiguration _configuration;

    public SubscriptionService(
        IUserSubscriptionRepository subscriptionRepo,
        ISubscriptionPlanRepository planRepo,
        IEnrollmentRepository enrollmentRepo,
        ICourseRepository courseRepo,
        InsightLearnDbContext context,
        ILogger<SubscriptionService> logger,
        IConfiguration configuration)
    {
        _subscriptionRepo = subscriptionRepo ?? throw new ArgumentNullException(nameof(subscriptionRepo));
        _planRepo = planRepo ?? throw new ArgumentNullException(nameof(planRepo));
        _enrollmentRepo = enrollmentRepo ?? throw new ArgumentNullException(nameof(enrollmentRepo));
        _courseRepo = courseRepo ?? throw new ArgumentNullException(nameof(courseRepo));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        // Initialize Stripe API key
        var stripeSecretKey = Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY")
            ?? configuration["Stripe:SecretKey"];

        if (!string.IsNullOrEmpty(stripeSecretKey))
        {
            StripeConfiguration.ApiKey = stripeSecretKey;
            _logger.LogInformation("[SubscriptionService] Stripe API key configured successfully");
        }
        else
        {
            _logger.LogWarning("[SubscriptionService] Stripe API key not configured - payment operations will fail");
        }
    }

    #region Core Subscription Management (7 methods)

    /// <summary>
    /// Gets all active subscription plans ordered by display order
    /// </summary>
    public async Task<List<SubscriptionPlan>> GetActiveSubscriptionPlansAsync()
    {
        try
        {
            _logger.LogDebug("[GetActiveSubscriptionPlansAsync] Fetching active subscription plans");

            var plans = await _planRepo.GetAllActiveAsync();

            _logger.LogInformation("[GetActiveSubscriptionPlansAsync] Retrieved {Count} active plans", plans.Count);
            return plans;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GetActiveSubscriptionPlansAsync] Failed to retrieve subscription plans");
            throw;
        }
    }

    /// <summary>
    /// Gets a subscription plan by ID
    /// </summary>
    public async Task<SubscriptionPlan?> GetSubscriptionPlanByIdAsync(Guid planId)
    {
        try
        {
            _logger.LogDebug("[GetSubscriptionPlanByIdAsync] Fetching plan {PlanId}", planId);

            var plan = await _planRepo.GetByIdAsync(planId);

            if (plan == null)
            {
                _logger.LogWarning("[GetSubscriptionPlanByIdAsync] Plan {PlanId} not found", planId);
                return null;
            }

            if (!plan.IsActive)
            {
                _logger.LogWarning("[GetSubscriptionPlanByIdAsync] Plan {PlanId} is inactive", planId);
                return null;
            }

            return plan;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GetSubscriptionPlanByIdAsync] Failed to retrieve plan {PlanId}", planId);
            throw;
        }
    }

    /// <summary>
    /// Gets the active subscription for a user
    /// </summary>
    public async Task<UserSubscription?> GetActiveSubscriptionAsync(Guid userId)
    {
        try
        {
            _logger.LogDebug("[GetActiveSubscriptionAsync] Fetching active subscription for user {UserId}", userId);

            var subscription = await _subscriptionRepo.GetActiveByUserIdAsync(userId);

            if (subscription != null)
            {
                _logger.LogInformation("[GetActiveSubscriptionAsync] Found active subscription {SubscriptionId} for user {UserId}",
                    subscription.Id, userId);
            }
            else
            {
                _logger.LogDebug("[GetActiveSubscriptionAsync] No active subscription for user {UserId}", userId);
            }

            return subscription;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GetActiveSubscriptionAsync] Failed to retrieve subscription for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Creates a new subscription for a user with atomic transaction and auto-enrollment
    /// </summary>
    public async Task<UserSubscription?> CreateSubscriptionAsync(
        Guid userId,
        Guid planId,
        string billingInterval,
        string? stripeSubscriptionId = null)
    {
        _logger.LogInformation("[CreateSubscriptionAsync] Creating subscription for user {UserId}, plan {PlanId}, interval {BillingInterval}",
            userId, planId, billingInterval);

        // Begin atomic transaction
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 1. Validate plan exists and is active
            var plan = await _planRepo.GetByIdAsync(planId);
            if (plan == null || !plan.IsActive)
            {
                var errorMsg = $"Subscription plan {planId} not found or inactive";
                _logger.LogError("[CreateSubscriptionAsync] {Error}", errorMsg);
                throw new InvalidOperationException(errorMsg);
            }

            // 2. Check user doesn't already have active subscription
            var existingSubscription = await _subscriptionRepo.GetActiveByUserIdAsync(userId);
            if (existingSubscription != null)
            {
                var errorMsg = $"User {userId} already has active subscription {existingSubscription.Id}";
                _logger.LogError("[CreateSubscriptionAsync] {Error}", errorMsg);
                throw new InvalidOperationException(errorMsg);
            }

            // 3. Calculate billing period
            var now = DateTime.UtcNow;
            var periodStart = now;
            var periodEnd = billingInterval.ToLowerInvariant() == "year"
                ? now.AddYears(1)
                : now.AddMonths(1);

            // 4. Determine if user gets trial (7 days for new users)
            var userHasPreviousSubscriptions = (await _subscriptionRepo.GetByUserIdAsync(userId, includeInactive: true)).Any();
            DateTime? trialEnd = userHasPreviousSubscriptions ? null : now.AddDays(7);

            // 5. Get Stripe customer ID or create new customer
            string? stripeCustomerId = null;
            if (!string.IsNullOrEmpty(stripeSubscriptionId))
            {
                // Extract customer ID from Stripe subscription if available
                try
                {
                    var stripeSubService = new Stripe.SubscriptionService();
                    var stripeSub = await stripeSubService.GetAsync(stripeSubscriptionId);
                    stripeCustomerId = stripeSub.CustomerId;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[CreateSubscriptionAsync] Failed to retrieve Stripe customer from subscription {StripeSubscriptionId}",
                        stripeSubscriptionId);
                }
            }

            // 6. Create subscription record
            var subscription = new UserSubscription
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                PlanId = planId,
                Status = trialEnd.HasValue ? "trialing" : "active",
                BillingInterval = billingInterval.ToLowerInvariant(),
                StripeSubscriptionId = stripeSubscriptionId,
                StripeCustomerId = stripeCustomerId,
                CurrentPeriodStart = periodStart,
                CurrentPeriodEnd = periodEnd,
                TrialEndsAt = trialEnd,
                AutoRenew = true,
                CreatedAt = now,
                UpdatedAt = now
            };

            await _subscriptionRepo.CreateAsync(subscription);
            await _context.SaveChangesAsync();

            _logger.LogInformation("[CreateSubscriptionAsync] Created subscription {SubscriptionId} for user {UserId}",
                subscription.Id, userId);

            // 7. Auto-enroll user to all subscription-only courses
            await AutoEnrollSubscriberAsync(userId, subscription.Id);

            // Commit transaction
            await transaction.CommitAsync();

            _logger.LogInformation("[CreateSubscriptionAsync] Successfully created subscription {SubscriptionId} with auto-enrollment",
                subscription.Id);

            return subscription;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "[CreateSubscriptionAsync] Failed to create subscription - transaction rolled back");
            throw;
        }
    }

    /// <summary>
    /// Updates subscription status from Stripe webhook events
    /// </summary>
    public async Task<UserSubscription?> UpdateSubscriptionStatusAsync(string stripeSubscriptionId, string status)
    {
        try
        {
            _logger.LogInformation("[UpdateSubscriptionStatusAsync] Updating subscription {StripeSubscriptionId} to status {Status}",
                stripeSubscriptionId, status);

            var subscription = await _subscriptionRepo.GetByStripeSubscriptionIdAsync(stripeSubscriptionId);
            if (subscription == null)
            {
                _logger.LogWarning("[UpdateSubscriptionStatusAsync] Subscription {StripeSubscriptionId} not found",
                    stripeSubscriptionId);
                return null;
            }

            subscription.Status = status.ToLowerInvariant();
            subscription.UpdatedAt = DateTime.UtcNow;

            // Update cancellation tracking
            if (status.ToLowerInvariant() == "cancelled")
            {
                subscription.CancelledAt = DateTime.UtcNow;
            }
            else if (status.ToLowerInvariant() == "active")
            {
                subscription.CancelledAt = null;
                subscription.AutoRenew = true;
            }

            await _subscriptionRepo.UpdateAsync(subscription);
            await _context.SaveChangesAsync();

            _logger.LogInformation("[UpdateSubscriptionStatusAsync] Updated subscription {SubscriptionId} to status {Status}",
                subscription.Id, status);

            return subscription;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UpdateSubscriptionStatusAsync] Failed to update subscription {StripeSubscriptionId}",
                stripeSubscriptionId);
            throw;
        }
    }

    /// <summary>
    /// Cancels a subscription (sets cancel_at_period_end = true, does not cancel immediately)
    /// </summary>
    public async Task<bool> CancelSubscriptionAsync(Guid subscriptionId, string? reason = null, string? feedback = null)
    {
        try
        {
            _logger.LogInformation("[CancelSubscriptionAsync] Cancelling subscription {SubscriptionId}, reason: {Reason}",
                subscriptionId, reason ?? "none");

            var subscription = await _subscriptionRepo.GetByIdAsync(subscriptionId);
            if (subscription == null)
            {
                _logger.LogWarning("[CancelSubscriptionAsync] Subscription {SubscriptionId} not found", subscriptionId);
                return false;
            }

            // Update local record
            subscription.AutoRenew = false;
            subscription.CancellationReason = reason;
            subscription.CancellationFeedback = feedback;
            subscription.UpdatedAt = DateTime.UtcNow;

            // Cancel in Stripe (at period end, not immediately)
            if (!string.IsNullOrEmpty(subscription.StripeSubscriptionId))
            {
                try
                {
                    var stripeSubService = new Stripe.SubscriptionService();
                    var options = new SubscriptionUpdateOptions
                    {
                        CancelAtPeriodEnd = true
                    };
                    await stripeSubService.UpdateAsync(subscription.StripeSubscriptionId, options);

                    _logger.LogInformation("[CancelSubscriptionAsync] Cancelled Stripe subscription {StripeSubscriptionId}",
                        subscription.StripeSubscriptionId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[CancelSubscriptionAsync] Failed to cancel Stripe subscription {StripeSubscriptionId}",
                        subscription.StripeSubscriptionId);
                    // Continue with local cancellation even if Stripe fails
                }
            }

            await _subscriptionRepo.UpdateAsync(subscription);
            await _context.SaveChangesAsync();

            _logger.LogInformation("[CancelSubscriptionAsync] Successfully cancelled subscription {SubscriptionId}",
                subscriptionId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CancelSubscriptionAsync] Failed to cancel subscription {SubscriptionId}", subscriptionId);
            throw;
        }
    }

    /// <summary>
    /// Reactivates a cancelled subscription (resumes auto-renewal)
    /// </summary>
    public async Task<bool> ReactivateSubscriptionAsync(Guid subscriptionId)
    {
        try
        {
            _logger.LogInformation("[ReactivateSubscriptionAsync] Reactivating subscription {SubscriptionId}", subscriptionId);

            var subscription = await _subscriptionRepo.GetByIdAsync(subscriptionId);
            if (subscription == null)
            {
                _logger.LogWarning("[ReactivateSubscriptionAsync] Subscription {SubscriptionId} not found", subscriptionId);
                return false;
            }

            // Reactivate in Stripe
            if (!string.IsNullOrEmpty(subscription.StripeSubscriptionId))
            {
                try
                {
                    var stripeSubService = new Stripe.SubscriptionService();
                    var options = new SubscriptionUpdateOptions
                    {
                        CancelAtPeriodEnd = false
                    };
                    await stripeSubService.UpdateAsync(subscription.StripeSubscriptionId, options);

                    _logger.LogInformation("[ReactivateSubscriptionAsync] Reactivated Stripe subscription {StripeSubscriptionId}",
                        subscription.StripeSubscriptionId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[ReactivateSubscriptionAsync] Failed to reactivate Stripe subscription {StripeSubscriptionId}",
                        subscription.StripeSubscriptionId);
                    throw;
                }
            }

            // Update local record
            subscription.AutoRenew = true;
            subscription.Status = "active";
            subscription.CancelledAt = null;
            subscription.UpdatedAt = DateTime.UtcNow;

            await _subscriptionRepo.UpdateAsync(subscription);
            await _context.SaveChangesAsync();

            // Re-enable enrollments (filter by subscription-based enrollments)
            var enrollments = await _enrollmentRepo.GetByUserIdAsync(subscription.UserId);
            foreach (var enrollment in enrollments.Where(e => e.SubscriptionId == subscriptionId))
            {
                // Re-activate enrollment logic would go here
                // For now, enrollments remain active
            }

            _logger.LogInformation("[ReactivateSubscriptionAsync] Successfully reactivated subscription {SubscriptionId}",
                subscriptionId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ReactivateSubscriptionAsync] Failed to reactivate subscription {SubscriptionId}",
                subscriptionId);
            throw;
        }
    }

    #endregion

    #region Subscription Access Control (3 methods)

    /// <summary>
    /// Checks if user has an active subscription (status = active/trialing and not expired)
    /// </summary>
    public async Task<bool> HasActiveSubscriptionAsync(Guid userId)
    {
        try
        {
            var subscription = await _subscriptionRepo.GetActiveByUserIdAsync(userId);

            if (subscription == null)
                return false;

            var isActive = (subscription.Status == "active" || subscription.Status == "trialing") &&
                          subscription.CurrentPeriodEnd > DateTime.UtcNow;

            _logger.LogDebug("[HasActiveSubscriptionAsync] User {UserId} active subscription: {IsActive}", userId, isActive);

            return isActive;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[HasActiveSubscriptionAsync] Failed to check subscription for user {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// Checks if user can access a course (subscription-only or paid enrollment)
    /// </summary>
    public async Task<bool> CanAccessCourseAsync(Guid userId, Guid courseId)
    {
        try
        {
            _logger.LogDebug("[CanAccessCourseAsync] Checking course {CourseId} access for user {UserId}",
                courseId, userId);

            var course = await _courseRepo.GetByIdAsync(courseId);
            if (course == null)
            {
                _logger.LogWarning("[CanAccessCourseAsync] Course {CourseId} not found", courseId);
                return false;
            }

            // If course is subscription-only, require active subscription
            if (course.IsSubscriptionOnly)
            {
                var hasSubscription = await HasActiveSubscriptionAsync(userId);
                _logger.LogDebug("[CanAccessCourseAsync] Course {CourseId} is subscription-only, user has subscription: {HasSubscription}",
                    courseId, hasSubscription);
                return hasSubscription;
            }

            // Otherwise, check if user has paid enrollment
            var isEnrolled = await _enrollmentRepo.IsUserEnrolledAsync(userId, courseId);
            _logger.LogDebug("[CanAccessCourseAsync] Course {CourseId} access via enrollment: {IsEnrolled}",
                courseId, isEnrolled);

            return isEnrolled;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CanAccessCourseAsync] Failed to check access for user {UserId}, course {CourseId}",
                userId, courseId);
            return false;
        }
    }

    /// <summary>
    /// Auto-enrolls a subscriber to all subscription-only courses (called on subscription creation)
    /// </summary>
    public async Task AutoEnrollSubscriberAsync(Guid userId, Guid subscriptionId)
    {
        _logger.LogInformation("[AutoEnrollSubscriberAsync] Auto-enrolling user {UserId} to subscription courses", userId);

        try
        {
            // Get all active courses where IsSubscriptionOnly = true
            var allCourses = await _courseRepo.GetAllAsync();
            var subscriptionCourses = allCourses.Where(c => c.IsSubscriptionOnly && c.IsActive).ToList();

            _logger.LogInformation("[AutoEnrollSubscriberAsync] Found {Count} subscription-only courses", subscriptionCourses.Count);

            var enrollmentCount = 0;
            var skipCount = 0;

            foreach (var course in subscriptionCourses)
            {
                try
                {
                    // Check if already enrolled
                    var existingEnrollment = await _enrollmentRepo.GetActiveEnrollmentAsync(userId, course.Id);
                    if (existingEnrollment != null)
                    {
                        skipCount++;
                        _logger.LogDebug("[AutoEnrollSubscriberAsync] User {UserId} already enrolled in course {CourseId}, skipping",
                            userId, course.Id);
                        continue;
                    }

                    // Create enrollment
                    var enrollment = new Enrollment
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        CourseId = course.Id,
                        Status = EnrollmentStatus.Active,
                        EnrolledAt = DateTime.UtcNow,
                        SubscriptionId = subscriptionId,
                        AmountPaid = 0, // Subscription-based, no individual payment
                        CompletedLessons = 0,
                        TotalWatchedMinutes = 0
                    };

                    await _enrollmentRepo.CreateAsync(enrollment);
                    enrollmentCount++;

                    _logger.LogDebug("[AutoEnrollSubscriberAsync] Created enrollment {EnrollmentId} for course {CourseId}",
                        enrollment.Id, course.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[AutoEnrollSubscriberAsync] Failed to enroll user {UserId} in course {CourseId}",
                        userId, course.Id);
                    // Continue with next course (don't fail entire operation)
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("[AutoEnrollSubscriberAsync] Auto-enrolled user {UserId} to {Count} courses ({SkipCount} skipped)",
                userId, enrollmentCount, skipCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AutoEnrollSubscriberAsync] Failed to auto-enroll user {UserId}", userId);
            throw;
        }
    }

    #endregion

    #region Analytics (4 methods)

    /// <summary>
    /// Calculates Monthly Recurring Revenue (MRR) from all active subscriptions
    /// </summary>
    public async Task<decimal> GetMonthlyRecurringRevenueAsync()
    {
        try
        {
            _logger.LogDebug("[GetMonthlyRecurringRevenueAsync] Calculating MRR");

            var mrr = await _subscriptionRepo.GetMonthlyRecurringRevenueAsync();

            _logger.LogInformation("[GetMonthlyRecurringRevenueAsync] Total MRR: {MRR:C}", mrr);

            return mrr;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GetMonthlyRecurringRevenueAsync] Failed to calculate MRR");
            throw;
        }
    }

    /// <summary>
    /// Gets count of active subscriptions (status = active/trialing and not expired)
    /// </summary>
    public async Task<int> GetActiveSubscriptionCountAsync()
    {
        try
        {
            _logger.LogDebug("[GetActiveSubscriptionCountAsync] Counting active subscriptions");

            var count = await _subscriptionRepo.GetActiveSubscriptionCountAsync();

            _logger.LogInformation("[GetActiveSubscriptionCountAsync] Active subscriptions: {Count}", count);

            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GetActiveSubscriptionCountAsync] Failed to count active subscriptions");
            throw;
        }
    }

    /// <summary>
    /// Calculates churn rate for a specific month/year
    /// Formula: (cancelled_subscriptions / active_at_start) * 100
    /// </summary>
    public async Task<int> GetChurnRateAsync(int month, int year)
    {
        try
        {
            _logger.LogInformation("[GetChurnRateAsync] Calculating churn rate for {Year}-{Month:D2}", year, month);

            // Get number of cancellations in this month
            var churnCount = await _subscriptionRepo.GetChurnCountAsync(month, year);

            // Get active subscriptions at start of month
            var startDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
            var activeAtStart = await _context.UserSubscriptions
                .Where(s => s.CreatedAt < startDate &&
                           (s.CancelledAt == null || s.CancelledAt >= startDate))
                .CountAsync();

            if (activeAtStart == 0)
            {
                _logger.LogWarning("[GetChurnRateAsync] No active subscriptions at start of {Year}-{Month:D2}, returning 0",
                    year, month);
                return 0;
            }

            var churnRate = (int)Math.Round((double)churnCount / activeAtStart * 100);

            _logger.LogInformation("[GetChurnRateAsync] Churn rate for {Year}-{Month:D2}: {ChurnRate}% ({ChurnCount} / {ActiveCount})",
                year, month, churnRate, churnCount, activeAtStart);

            return churnRate;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GetChurnRateAsync] Failed to calculate churn rate for {Year}-{Month:D2}", year, month);
            throw;
        }
    }

    /// <summary>
    /// Gets subscriptions expiring within the specified number of days
    /// </summary>
    public async Task<List<UserSubscription>> GetExpiringSubscriptionsAsync(int daysBeforeExpiry)
    {
        try
        {
            _logger.LogDebug("[GetExpiringSubscriptionsAsync] Fetching subscriptions expiring in {Days} days", daysBeforeExpiry);

            var expiringSubscriptions = await _subscriptionRepo.GetExpiringSubscriptionsAsync(daysBeforeExpiry);

            _logger.LogInformation("[GetExpiringSubscriptionsAsync] Found {Count} subscriptions expiring in {Days} days",
                expiringSubscriptions.Count, daysBeforeExpiry);

            return expiringSubscriptions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GetExpiringSubscriptionsAsync] Failed to fetch expiring subscriptions");
            throw;
        }
    }

    #endregion

    #region Stripe Webhook Handlers (5 methods)

    /// <summary>
    /// Handles subscription.created webhook from Stripe (idempotent)
    /// </summary>
    public async Task HandleSubscriptionCreatedAsync(string stripeSubscriptionId, Guid userId, Guid planId)
    {
        _logger.LogInformation("[HandleSubscriptionCreatedAsync] Processing subscription.created for {StripeSubscriptionId}",
            stripeSubscriptionId);

        try
        {
            // Check if subscription already exists (idempotency)
            var existingSubscription = await _subscriptionRepo.GetByStripeSubscriptionIdAsync(stripeSubscriptionId);
            if (existingSubscription != null)
            {
                _logger.LogWarning("[HandleSubscriptionCreatedAsync] Subscription {StripeSubscriptionId} already exists, skipping",
                    stripeSubscriptionId);
                return;
            }

            // Retrieve subscription details from Stripe
            var stripeSubService = new Stripe.SubscriptionService();
            var stripeSub = await stripeSubService.GetAsync(stripeSubscriptionId);

            var billingInterval = stripeSub.Items.Data.FirstOrDefault()?.Price.Recurring?.Interval ?? "month";

            // Create local subscription record
            await CreateSubscriptionAsync(userId, planId, billingInterval, stripeSubscriptionId);

            _logger.LogInformation("[HandleSubscriptionCreatedAsync] Successfully processed subscription.created for {StripeSubscriptionId}",
                stripeSubscriptionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[HandleSubscriptionCreatedAsync] Failed to handle subscription.created for {StripeSubscriptionId}",
                stripeSubscriptionId);
            throw;
        }
    }

    /// <summary>
    /// Handles subscription.updated webhook from Stripe
    /// </summary>
    public async Task HandleSubscriptionUpdatedAsync(string stripeSubscriptionId, DateTime currentPeriodEnd, string status)
    {
        _logger.LogInformation("[HandleSubscriptionUpdatedAsync] Processing subscription.updated for {StripeSubscriptionId}",
            stripeSubscriptionId);

        try
        {
            var subscription = await _subscriptionRepo.GetByStripeSubscriptionIdAsync(stripeSubscriptionId);
            if (subscription == null)
            {
                _logger.LogWarning("[HandleSubscriptionUpdatedAsync] Subscription {StripeSubscriptionId} not found",
                    stripeSubscriptionId);
                return;
            }

            subscription.CurrentPeriodEnd = currentPeriodEnd;
            subscription.Status = status.ToLowerInvariant();
            subscription.UpdatedAt = DateTime.UtcNow;

            await _subscriptionRepo.UpdateAsync(subscription);
            await _context.SaveChangesAsync();

            _logger.LogInformation("[HandleSubscriptionUpdatedAsync] Updated subscription {SubscriptionId}, period end: {PeriodEnd}",
                subscription.Id, currentPeriodEnd);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[HandleSubscriptionUpdatedAsync] Failed to handle subscription.updated for {StripeSubscriptionId}",
                stripeSubscriptionId);
            throw;
        }
    }

    /// <summary>
    /// Handles subscription.deleted webhook from Stripe
    /// </summary>
    public async Task HandleSubscriptionCancelledAsync(string stripeSubscriptionId)
    {
        _logger.LogInformation("[HandleSubscriptionCancelledAsync] Processing subscription.deleted for {StripeSubscriptionId}",
            stripeSubscriptionId);

        try
        {
            var subscription = await _subscriptionRepo.GetByStripeSubscriptionIdAsync(stripeSubscriptionId);
            if (subscription == null)
            {
                _logger.LogWarning("[HandleSubscriptionCancelledAsync] Subscription {StripeSubscriptionId} not found",
                    stripeSubscriptionId);
                return;
            }

            subscription.Status = "cancelled";
            subscription.CancelledAt = DateTime.UtcNow;
            subscription.UpdatedAt = DateTime.UtcNow;

            await _subscriptionRepo.UpdateAsync(subscription);
            await _context.SaveChangesAsync();

            _logger.LogInformation("[HandleSubscriptionCancelledAsync] Cancelled subscription {SubscriptionId}",
                subscription.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[HandleSubscriptionCancelledAsync] Failed to handle subscription.deleted for {StripeSubscriptionId}",
                stripeSubscriptionId);
            throw;
        }
    }

    /// <summary>
    /// Handles invoice.paid webhook from Stripe (records revenue)
    /// </summary>
    public async Task HandleInvoicePaidAsync(string stripeInvoiceId, string stripeSubscriptionId, decimal amount)
    {
        _logger.LogInformation("[HandleInvoicePaidAsync] Processing invoice.paid for invoice {InvoiceId}, amount {Amount:C}",
            stripeInvoiceId, amount);

        try
        {
            var subscription = await _subscriptionRepo.GetByStripeSubscriptionIdAsync(stripeSubscriptionId);
            if (subscription == null)
            {
                _logger.LogWarning("[HandleInvoicePaidAsync] Subscription {StripeSubscriptionId} not found", stripeSubscriptionId);
                return;
            }

            // Check if revenue record already exists (idempotency)
            var existingRevenue = await _context.SubscriptionRevenues
                .FirstOrDefaultAsync(r => r.StripeInvoiceId == stripeInvoiceId);

            if (existingRevenue != null)
            {
                _logger.LogWarning("[HandleInvoicePaidAsync] Revenue record for invoice {InvoiceId} already exists, skipping",
                    stripeInvoiceId);
                return;
            }

            // Create revenue record
            var revenue = new SubscriptionRevenue
            {
                Id = Guid.NewGuid(),
                SubscriptionId = subscription.Id,
                Amount = amount,
                Currency = subscription.Plan?.Name ?? "USD",
                Status = "paid",
                StripeInvoiceId = stripeInvoiceId,
                BillingPeriodStart = subscription.CurrentPeriodStart,
                BillingPeriodEnd = subscription.CurrentPeriodEnd,
                PaidAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _context.SubscriptionRevenues.Add(revenue);
            await _context.SaveChangesAsync();

            _logger.LogInformation("[HandleInvoicePaidAsync] Created revenue record {RevenueId} for {Amount:C}",
                revenue.Id, amount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[HandleInvoicePaidAsync] Failed to handle invoice.paid for {InvoiceId}",
                stripeInvoiceId);
            throw;
        }
    }

    /// <summary>
    /// Handles invoice.payment_failed webhook from Stripe
    /// </summary>
    public async Task HandleInvoicePaymentFailedAsync(string stripeInvoiceId, string stripeSubscriptionId, string failureReason)
    {
        _logger.LogWarning("[HandleInvoicePaymentFailedAsync] Processing invoice.payment_failed for invoice {InvoiceId}, reason: {Reason}",
            stripeInvoiceId, failureReason);

        try
        {
            var subscription = await _subscriptionRepo.GetByStripeSubscriptionIdAsync(stripeSubscriptionId);
            if (subscription == null)
            {
                _logger.LogWarning("[HandleInvoicePaymentFailedAsync] Subscription {StripeSubscriptionId} not found",
                    stripeSubscriptionId);
                return;
            }

            // Update subscription status to past_due
            subscription.Status = "past_due";
            subscription.UpdatedAt = DateTime.UtcNow;

            await _subscriptionRepo.UpdateAsync(subscription);

            // Create failed revenue record for tracking
            var revenue = new SubscriptionRevenue
            {
                Id = Guid.NewGuid(),
                SubscriptionId = subscription.Id,
                Amount = 0, // Amount not available in failure event
                Currency = "USD",
                Status = "failed",
                StripeInvoiceId = stripeInvoiceId,
                BillingPeriodStart = subscription.CurrentPeriodStart,
                BillingPeriodEnd = subscription.CurrentPeriodEnd,
                CreatedAt = DateTime.UtcNow
            };

            _context.SubscriptionRevenues.Add(revenue);
            await _context.SaveChangesAsync();

            _logger.LogInformation("[HandleInvoicePaymentFailedAsync] Updated subscription {SubscriptionId} to past_due, created failed revenue record",
                subscription.Id);

            // TODO: Send email notification to user (integrate with email service)
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[HandleInvoicePaymentFailedAsync] Failed to handle invoice.payment_failed for {InvoiceId}",
                stripeInvoiceId);
            throw;
        }
    }

    #endregion
}
