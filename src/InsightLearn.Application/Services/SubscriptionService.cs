using InsightLearn.Core.Entities;
using InsightLearn.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace InsightLearn.Application.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly ISubscriptionPlanRepository _planRepository;
    private readonly IUserSubscriptionRepository _subscriptionRepository;
    private readonly ISubscriptionRevenueRepository _revenueRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly ILogger<SubscriptionService> _logger;

    public SubscriptionService(
        ISubscriptionPlanRepository planRepository,
        IUserSubscriptionRepository subscriptionRepository,
        ISubscriptionRevenueRepository revenueRepository,
        IEnrollmentRepository enrollmentRepository,
        ICourseRepository courseRepository,
        ILogger<SubscriptionService> logger)
    {
        _planRepository = planRepository;
        _subscriptionRepository = subscriptionRepository;
        _revenueRepository = revenueRepository;
        _enrollmentRepository = enrollmentRepository;
        _courseRepository = courseRepository;
        _logger = logger;
    }

    #region Subscription Plans

    public async Task<List<SubscriptionPlan>> GetActiveSubscriptionPlansAsync()
    {
        return await _planRepository.GetAllActiveAsync();
    }

    public async Task<SubscriptionPlan?> GetSubscriptionPlanByIdAsync(Guid planId)
    {
        return await _planRepository.GetByIdAsync(planId);
    }

    #endregion

    #region User Subscriptions

    public async Task<UserSubscription?> GetActiveSubscriptionAsync(Guid userId)
    {
        return await _subscriptionRepository.GetActiveByUserIdAsync(userId);
    }

    public async Task<UserSubscription?> CreateSubscriptionAsync(
        Guid userId,
        Guid planId,
        string billingInterval,
        string? stripeSubscriptionId = null)
    {
        try
        {
            var plan = await _planRepository.GetByIdAsync(planId);
            if (plan == null)
            {
                _logger.LogError($"Subscription plan {planId} not found");
                return null;
            }

            // Check if user already has an active subscription
            var existing = await _subscriptionRepository.GetActiveByUserIdAsync(userId);
            if (existing != null)
            {
                _logger.LogWarning($"User {userId} already has an active subscription {existing.Id}");
                return existing;
            }

            var now = DateTime.UtcNow;
            var periodEnd = billingInterval == "year" ? now.AddYears(1) : now.AddMonths(1);

            var subscription = new UserSubscription
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                PlanId = planId,
                Status = "active",
                BillingInterval = billingInterval,
                StripeSubscriptionId = stripeSubscriptionId,
                CurrentPeriodStart = now,
                CurrentPeriodEnd = periodEnd,
                AutoRenew = true,
                CreatedAt = now,
                UpdatedAt = now
            };

            var created = await _subscriptionRepository.CreateAsync(subscription);

            // Auto-enroll user in all subscription-only courses
            await AutoEnrollSubscriberAsync(userId, created.Id);

            _logger.LogInformation($"Created subscription {created.Id} for user {userId} on plan {planId}");
            return created;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error creating subscription for user {userId}");
            return null;
        }
    }

    public async Task<UserSubscription?> UpdateSubscriptionStatusAsync(string stripeSubscriptionId, string status)
    {
        try
        {
            var subscription = await _subscriptionRepository.GetByStripeSubscriptionIdAsync(stripeSubscriptionId);
            if (subscription == null)
            {
                _logger.LogWarning($"Subscription with Stripe ID {stripeSubscriptionId} not found");
                return null;
            }

            subscription.Status = status;
            subscription.UpdatedAt = DateTime.UtcNow;

            if (status == "cancelled")
            {
                subscription.CancelledAt = DateTime.UtcNow;
            }

            return await _subscriptionRepository.UpdateAsync(subscription);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating subscription status for Stripe ID {stripeSubscriptionId}");
            return null;
        }
    }

    public async Task<bool> CancelSubscriptionAsync(Guid subscriptionId, string? reason = null, string? feedback = null)
    {
        try
        {
            return await _subscriptionRepository.CancelAsync(subscriptionId, reason, feedback);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error cancelling subscription {subscriptionId}");
            return false;
        }
    }

    public async Task<bool> ReactivateSubscriptionAsync(Guid subscriptionId)
    {
        try
        {
            var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId);
            if (subscription == null)
                return false;

            subscription.Status = "active";
            subscription.CancelledAt = null;
            subscription.AutoRenew = true;
            subscription.UpdatedAt = DateTime.UtcNow;

            await _subscriptionRepository.UpdateAsync(subscription);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error reactivating subscription {subscriptionId}");
            return false;
        }
    }

    #endregion

    #region Access Control

    public async Task<bool> HasActiveSubscriptionAsync(Guid userId)
    {
        var subscription = await _subscriptionRepository.GetActiveByUserIdAsync(userId);
        return subscription != null && subscription.IsActive;
    }

    public async Task<bool> CanAccessCourseAsync(Guid userId, Guid courseId)
    {
        // Check if course is subscription-only
        var course = await _courseRepository.GetByIdAsync(courseId);
        if (course == null)
            return false;

        // If course is not subscription-only, check enrollment or payment
        if (!course.IsSubscriptionOnly)
        {
            // Check if user has paid enrollment
            var enrollment = await _enrollmentRepository.GetActiveEnrollmentAsync(userId, courseId);
            return enrollment != null;
        }

        // For subscription-only courses, check active subscription
        return await HasActiveSubscriptionAsync(userId);
    }

    public async Task AutoEnrollSubscriberAsync(Guid userId, Guid subscriptionId)
    {
        try
        {
            // Get all subscription-only courses
            var allCourses = await _courseRepository.GetAllAsync();
            var subscriptionCourses = allCourses.Where(c => c.IsSubscriptionOnly && c.Status == CourseStatus.Published).ToList();

            _logger.LogInformation($"Auto-enrolling user {userId} in {subscriptionCourses.Count} subscription courses");

            foreach (var course in subscriptionCourses)
            {
                // Check if already enrolled
                var existing = await _enrollmentRepository.GetActiveEnrollmentAsync(userId, course.Id);
                if (existing != null)
                    continue;

                // Create enrollment
                var enrollment = new Enrollment
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    CourseId = course.Id,
                    SubscriptionId = subscriptionId,
                    Status = EnrollmentStatus.Active,
                    AmountPaid = 0, // Subscription-based, no individual payment
                    EnrolledAt = DateTime.UtcNow,
                    LastAccessedAt = DateTime.UtcNow
                };

                await _enrollmentRepository.CreateAsync(enrollment);
            }

            _logger.LogInformation($"Auto-enrollment completed for user {userId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error auto-enrolling user {userId}");
        }
    }

    #endregion

    #region Analytics

    public async Task<decimal> GetMonthlyRecurringRevenueAsync()
    {
        return await _subscriptionRepository.GetMonthlyRecurringRevenueAsync();
    }

    public async Task<int> GetActiveSubscriptionCountAsync()
    {
        return await _subscriptionRepository.GetActiveSubscriptionCountAsync();
    }

    public async Task<int> GetChurnRateAsync(int month, int year)
    {
        return await _subscriptionRepository.GetChurnCountAsync(month, year);
    }

    public async Task<List<UserSubscription>> GetExpiringSubscriptionsAsync(int daysBeforeExpiry)
    {
        return await _subscriptionRepository.GetExpiringSubscriptionsAsync(daysBeforeExpiry);
    }

    #endregion

    #region Stripe Webhooks

    public async Task HandleSubscriptionCreatedAsync(string stripeSubscriptionId, Guid userId, Guid planId)
    {
        try
        {
            // Check if subscription already exists
            var existing = await _subscriptionRepository.GetByStripeSubscriptionIdAsync(stripeSubscriptionId);
            if (existing != null)
            {
                _logger.LogWarning($"Subscription {stripeSubscriptionId} already exists");
                return;
            }

            await CreateSubscriptionAsync(userId, planId, "month", stripeSubscriptionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error handling subscription created webhook for {stripeSubscriptionId}");
        }
    }

    public async Task HandleSubscriptionUpdatedAsync(string stripeSubscriptionId, DateTime currentPeriodEnd, string status)
    {
        try
        {
            var subscription = await _subscriptionRepository.GetByStripeSubscriptionIdAsync(stripeSubscriptionId);
            if (subscription == null)
            {
                _logger.LogWarning($"Subscription {stripeSubscriptionId} not found");
                return;
            }

            subscription.CurrentPeriodEnd = currentPeriodEnd;
            subscription.Status = status;
            subscription.UpdatedAt = DateTime.UtcNow;

            await _subscriptionRepository.UpdateAsync(subscription);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error handling subscription updated webhook for {stripeSubscriptionId}");
        }
    }

    public async Task HandleSubscriptionCancelledAsync(string stripeSubscriptionId)
    {
        try
        {
            var subscription = await _subscriptionRepository.GetByStripeSubscriptionIdAsync(stripeSubscriptionId);
            if (subscription == null)
            {
                _logger.LogWarning($"Subscription {stripeSubscriptionId} not found");
                return;
            }

            await CancelSubscriptionAsync(subscription.Id, "Cancelled via Stripe");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error handling subscription cancelled webhook for {stripeSubscriptionId}");
        }
    }

    public async Task HandleInvoicePaidAsync(string stripeInvoiceId, string stripeSubscriptionId, decimal amount)
    {
        try
        {
            var subscription = await _subscriptionRepository.GetByStripeSubscriptionIdAsync(stripeSubscriptionId);
            if (subscription == null)
            {
                _logger.LogWarning($"Subscription {stripeSubscriptionId} not found");
                return;
            }

            // Create revenue record
            var revenue = new SubscriptionRevenue
            {
                Id = Guid.NewGuid(),
                SubscriptionId = subscription.Id,
                Amount = amount,
                Currency = "EUR",
                StripeInvoiceId = stripeInvoiceId,
                BillingPeriodStart = subscription.CurrentPeriodStart,
                BillingPeriodEnd = subscription.CurrentPeriodEnd,
                Status = "paid",
                PaidAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            await _revenueRepository.CreateAsync(revenue);

            _logger.LogInformation($"Created revenue record for invoice {stripeInvoiceId}, amount: {amount}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error handling invoice paid webhook for {stripeInvoiceId}");
        }
    }

    public async Task HandleInvoicePaymentFailedAsync(string stripeInvoiceId, string stripeSubscriptionId, string failureReason)
    {
        try
        {
            var subscription = await _subscriptionRepository.GetByStripeSubscriptionIdAsync(stripeSubscriptionId);
            if (subscription == null)
            {
                _logger.LogWarning($"Subscription {stripeSubscriptionId} not found");
                return;
            }

            // Update subscription status
            subscription.Status = "past_due";
            subscription.UpdatedAt = DateTime.UtcNow;
            await _subscriptionRepository.UpdateAsync(subscription);

            _logger.LogWarning($"Invoice {stripeInvoiceId} payment failed: {failureReason}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error handling invoice payment failed webhook for {stripeInvoiceId}");
        }
    }

    #endregion
}
