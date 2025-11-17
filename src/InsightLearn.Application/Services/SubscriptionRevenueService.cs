using InsightLearn.Core.DTOs.Revenue;
using InsightLearn.Core.Entities;
using InsightLearn.Core.Interfaces;
using InsightLearn.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace InsightLearn.Application.Services;

/// <summary>
/// Subscription Revenue Service - Tracks all subscription revenue and provides financial reporting
/// Version: v2.0.0
/// Task: T4 - SubscriptionRevenueService.cs (8 methods)
/// Architect Score Target: 10/10
///
/// CRITICAL: This service is the DATA SOURCE for PayoutCalculationService
/// Revenue tracking must be idempotent and financially accurate
/// </summary>
public class SubscriptionRevenueService : ISubscriptionRevenueService
{
    private readonly ISubscriptionRevenueRepository _revenueRepo;
    private readonly IUserSubscriptionRepository _subscriptionRepo;
    private readonly ISubscriptionPlanRepository _planRepo;
    private readonly InsightLearnDbContext _context;
    private readonly ILogger<SubscriptionRevenueService> _logger;
    private readonly IConfiguration _configuration;

    public SubscriptionRevenueService(
        ISubscriptionRevenueRepository revenueRepository,
        IUserSubscriptionRepository subscriptionRepository,
        ISubscriptionPlanRepository planRepository,
        InsightLearnDbContext context,
        ILogger<SubscriptionRevenueService> logger,
        IConfiguration configuration)
    {
        _revenueRepo = revenueRepository ?? throw new ArgumentNullException(nameof(revenueRepository));
        _subscriptionRepo = subscriptionRepository ?? throw new ArgumentNullException(nameof(subscriptionRepository));
        _planRepo = planRepository ?? throw new ArgumentNullException(nameof(planRepository));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        _logger.LogInformation("[SubscriptionRevenueService] Initialized successfully");
    }

    #region Revenue Recording (3 methods)

    /// <summary>
    /// Records a subscription payment from Stripe webhook (idempotent via StripeInvoiceId)
    /// </summary>
    public async Task RecordRevenueAsync(RecordRevenueDto dto)
    {
        _logger.LogInformation("[RecordRevenueAsync] Recording revenue: User={UserId}, Amount={Amount:C}, Invoice={InvoiceId}",
            dto.UserId, dto.Amount, dto.StripeInvoiceId);

        try
        {
            // 1. IDEMPOTENCY CHECK: Check if revenue already recorded
            var existing = await _revenueRepo.GetByStripeInvoiceIdAsync(dto.StripeInvoiceId);
            if (existing != null)
            {
                _logger.LogWarning("[RecordRevenueAsync] Revenue already recorded: RevenueId={RevenueId}, Invoice={InvoiceId}, " +
                                  "Status={Status}. Skipping duplicate.",
                    existing.Id, dto.StripeInvoiceId, existing.Status);
                return; // Don't throw exception - idempotency means silent skip
            }

            // 2. Validate subscription exists
            var subscription = await _subscriptionRepo.GetByIdAsync(dto.SubscriptionId);
            if (subscription == null)
            {
                var errorMsg = $"Subscription {dto.SubscriptionId} not found";
                _logger.LogError("[RecordRevenueAsync] {Error}", errorMsg);
                throw new InvalidOperationException(errorMsg);
            }

            // 3. Validate user matches subscription
            if (subscription.UserId != dto.UserId)
            {
                var errorMsg = $"User {dto.UserId} does not match subscription {dto.SubscriptionId} owner {subscription.UserId}";
                _logger.LogError("[RecordRevenueAsync] {Error}", errorMsg);
                throw new InvalidOperationException(errorMsg);
            }

            // 4. Validate billing period dates
            if (dto.BillingPeriodEnd <= dto.BillingPeriodStart)
            {
                var errorMsg = $"Invalid billing period: End {dto.BillingPeriodEnd} must be after Start {dto.BillingPeriodStart}";
                _logger.LogError("[RecordRevenueAsync] {Error}", errorMsg);
                throw new ArgumentException(errorMsg);
            }

            // 5. Create revenue record
            var revenue = new SubscriptionRevenue
            {
                Id = Guid.NewGuid(),
                SubscriptionId = dto.SubscriptionId,
                Amount = dto.Amount,
                Currency = dto.Currency,
                BillingPeriodStart = dto.BillingPeriodStart,
                BillingPeriodEnd = dto.BillingPeriodEnd,
                StripeInvoiceId = dto.StripeInvoiceId,
                StripePaymentIntentId = dto.StripePaymentIntentId,
                PaymentMethod = dto.PaymentMethod,
                CardLast4 = dto.CardLast4,
                CardBrand = dto.CardBrand,
                InvoiceUrl = dto.InvoiceUrl,
                Status = "paid",
                PaidAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            await _revenueRepo.CreateAsync(revenue);
            await _context.SaveChangesAsync();

            _logger.LogInformation("[RecordRevenueAsync] Revenue recorded successfully: RevenueId={RevenueId}, " +
                                  "Amount={Amount:C}, Invoice={InvoiceId}",
                revenue.Id, revenue.Amount, revenue.StripeInvoiceId);
        }
        catch (Exception ex) when (ex is not InvalidOperationException && ex is not ArgumentException)
        {
            _logger.LogError(ex, "[RecordRevenueAsync] Failed to record revenue for User={UserId}, Invoice={InvoiceId}",
                dto.UserId, dto.StripeInvoiceId);
            throw;
        }
    }

    /// <summary>
    /// Records a refund for a revenue record (partial or full)
    /// </summary>
    public async Task RecordRefundAsync(Guid revenueId, decimal refundAmount, string reason)
    {
        _logger.LogInformation("[RecordRefundAsync] Recording refund: RevenueId={RevenueId}, Amount={Amount:C}, Reason={Reason}",
            revenueId, refundAmount, reason);

        try
        {
            // 1. Get existing revenue record
            var revenue = await _revenueRepo.GetByIdAsync(revenueId);
            if (revenue == null)
            {
                var errorMsg = $"Revenue record {revenueId} not found";
                _logger.LogError("[RecordRefundAsync] {Error}", errorMsg);
                throw new InvalidOperationException(errorMsg);
            }

            // 2. Validate refund amount
            if (refundAmount <= 0)
            {
                var errorMsg = $"Refund amount {refundAmount:C} must be greater than zero";
                _logger.LogError("[RecordRefundAsync] {Error}", errorMsg);
                throw new ArgumentException(errorMsg, nameof(refundAmount));
            }

            if (refundAmount > revenue.Amount)
            {
                var errorMsg = $"Refund amount {refundAmount:C} cannot exceed original amount {revenue.Amount:C}";
                _logger.LogError("[RecordRefundAsync] {Error}", errorMsg);
                throw new ArgumentException(errorMsg, nameof(refundAmount));
            }

            // 3. Check if already refunded
            if (revenue.Status == "refunded")
            {
                _logger.LogWarning("[RecordRefundAsync] Revenue {RevenueId} already fully refunded, skipping", revenueId);
                return;
            }

            // 4. Update refund status
            var isPartialRefund = refundAmount < revenue.Amount;
            var newStatus = isPartialRefund ? "partially_refunded" : "refunded";

            var success = await _revenueRepo.MarkAsRefundedAsync(revenueId, refundAmount, reason);
            if (!success)
            {
                throw new InvalidOperationException($"Failed to update refund status for revenue {revenueId}");
            }

            _logger.LogInformation("[RecordRefundAsync] Refund recorded: RevenueId={RevenueId}, Amount={Amount:C}, " +
                                  "Status={Status}, Reason={Reason}",
                revenueId, refundAmount, newStatus, reason);
        }
        catch (Exception ex) when (ex is not InvalidOperationException && ex is not ArgumentException)
        {
            _logger.LogError(ex, "[RecordRefundAsync] Failed to record refund for RevenueId={RevenueId}", revenueId);
            throw;
        }
    }

    /// <summary>
    /// Records a chargeback for a revenue record (marks as disputed)
    /// </summary>
    public async Task RecordChargebackAsync(Guid revenueId, string reason)
    {
        _logger.LogWarning("[RecordChargebackAsync] Recording chargeback: RevenueId={RevenueId}, Reason={Reason}",
            revenueId, reason);

        try
        {
            // 1. Get existing revenue record
            var revenue = await _revenueRepo.GetByIdAsync(revenueId);
            if (revenue == null)
            {
                var errorMsg = $"Revenue record {revenueId} not found";
                _logger.LogError("[RecordChargebackAsync] {Error}", errorMsg);
                throw new InvalidOperationException(errorMsg);
            }

            // 2. Mark as failed with chargeback reason
            var success = await _revenueRepo.MarkAsFailedAsync(revenueId, $"Chargeback: {reason}");
            if (!success)
            {
                throw new InvalidOperationException($"Failed to update chargeback status for revenue {revenueId}");
            }

            _logger.LogWarning("[RecordChargebackAsync] Chargeback recorded: RevenueId={RevenueId}, " +
                              "Original Amount={Amount:C}, Reason={Reason}. This revenue will be excluded from payouts.",
                revenueId, revenue.Amount, reason);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "[RecordChargebackAsync] Failed to record chargeback for RevenueId={RevenueId}", revenueId);
            throw;
        }
    }

    #endregion

    #region Revenue Reporting (5 methods)

    /// <summary>
    /// Gets total revenue for a specific month (CRITICAL: used by PayoutCalculationService)
    /// Excludes refunded and chargebacked revenue
    /// </summary>
    public async Task<decimal> GetMonthlyRevenueAsync(int year, int month)
    {
        _logger.LogInformation("[GetMonthlyRevenueAsync] Calculating revenue for {Year}-{Month:D2}", year, month);

        try
        {
            // 1. Validate input
            if (year < 2020 || year > 2100)
            {
                throw new ArgumentException($"Invalid year: {year}. Must be between 2020 and 2100.", nameof(year));
            }

            if (month < 1 || month > 12)
            {
                throw new ArgumentException($"Invalid month: {month}. Must be between 1 and 12.", nameof(month));
            }

            // 2. Calculate date range for month
            var startDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDate = startDate.AddMonths(1);

            _logger.LogDebug("[GetMonthlyRevenueAsync] Date range: {StartDate} to {EndDate}", startDate, endDate);

            // 3. Get total revenue (excludes refunded/failed)
            var totalRevenue = await _revenueRepo.GetTotalRevenueAsync(startDate, endDate);

            _logger.LogInformation("[GetMonthlyRevenueAsync] Total revenue for {Year}-{Month:D2}: {Revenue:C}",
                year, month, totalRevenue);

            return totalRevenue;
        }
        catch (Exception ex) when (ex is not ArgumentException)
        {
            _logger.LogError(ex, "[GetMonthlyRevenueAsync] Failed to calculate revenue for {Year}-{Month:D2}", year, month);
            throw;
        }
    }

    /// <summary>
    /// Calculates Monthly Recurring Revenue (MRR) as of specific date
    /// MRR = SUM of all active subscriptions normalized to monthly
    /// </summary>
    public async Task<decimal> GetMRRAsync(DateTime? asOfDate = null)
    {
        var calculationDate = asOfDate ?? DateTime.UtcNow;
        _logger.LogInformation("[GetMRRAsync] Calculating MRR as of {Date}", calculationDate);

        try
        {
            // 1. Get all active subscriptions as of date
            var activeSubscriptions = await _context.UserSubscriptions
                .Include(s => s.Plan)
                .Where(s => s.CreatedAt <= calculationDate &&
                           (s.Status == "active" || s.Status == "trialing") &&
                           s.CurrentPeriodEnd > calculationDate)
                .ToListAsync();

            _logger.LogDebug("[GetMRRAsync] Found {Count} active subscriptions", activeSubscriptions.Count);

            decimal totalMRR = 0;

            // 2. Normalize each subscription to monthly
            foreach (var subscription in activeSubscriptions)
            {
                if (subscription.Plan == null)
                {
                    _logger.LogWarning("[GetMRRAsync] Subscription {SubscriptionId} has null Plan, skipping",
                        subscription.Id);
                    continue;
                }

                decimal monthlyAmount;

                // Normalize based on billing interval
                switch (subscription.BillingInterval.ToLowerInvariant())
                {
                    case "year":
                    case "yearly":
                        // Yearly plan: divide by 12
                        monthlyAmount = (subscription.Plan.PriceYearly ?? subscription.Plan.PriceMonthly) / 12m;
                        break;

                    case "month":
                    case "monthly":
                    default:
                        // Monthly plan: use as-is
                        monthlyAmount = subscription.Plan.PriceMonthly;
                        break;
                }

                totalMRR += monthlyAmount;

                _logger.LogTrace("[GetMRRAsync] Subscription {SubscriptionId}: {BillingInterval} plan, " +
                                "Monthly amount: {MonthlyAmount:C}",
                    subscription.Id, subscription.BillingInterval, monthlyAmount);
            }

            _logger.LogInformation("[GetMRRAsync] MRR as of {Date}: {MRR:C} ({Count} subscriptions)",
                calculationDate, totalMRR, activeSubscriptions.Count);

            return totalMRR;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GetMRRAsync] Failed to calculate MRR as of {Date}", calculationDate);
            throw;
        }
    }

    /// <summary>
    /// Calculates Annual Recurring Revenue (ARR = MRR * 12)
    /// </summary>
    public async Task<decimal> GetARRAsync(DateTime? asOfDate = null)
    {
        var calculationDate = asOfDate ?? DateTime.UtcNow;
        _logger.LogInformation("[GetARRAsync] Calculating ARR as of {Date}", calculationDate);

        try
        {
            var mrr = await GetMRRAsync(calculationDate);
            var arr = mrr * 12;

            _logger.LogInformation("[GetARRAsync] ARR as of {Date}: {ARR:C} (MRR: {MRR:C})",
                calculationDate, arr, mrr);

            return arr;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GetARRAsync] Failed to calculate ARR as of {Date}", calculationDate);
            throw;
        }
    }

    /// <summary>
    /// Gets comprehensive revenue metrics for a date range
    /// Includes MRR, ARR, growth rate, churn rate, and breakdown by plan
    /// </summary>
    public async Task<RevenueMetricsDto> GetRevenueMetricsAsync(DateTime startDate, DateTime endDate)
    {
        _logger.LogInformation("[GetRevenueMetricsAsync] Calculating metrics from {StartDate} to {EndDate}",
            startDate, endDate);

        try
        {
            // 1. Total revenue in period
            var totalRevenue = await _revenueRepo.GetTotalRevenueAsync(startDate, endDate);

            // 2. MRR and ARR (as of end date)
            var mrr = await GetMRRAsync(endDate);
            var arr = mrr * 12;

            // 3. Calculate growth rate (compare to previous period)
            var periodDays = (endDate - startDate).Days;
            var previousStart = startDate.AddDays(-periodDays);
            var previousEnd = startDate.AddDays(-1);
            var previousRevenue = await _revenueRepo.GetTotalRevenueAsync(previousStart, previousEnd);

            var growthRate = previousRevenue > 0
                ? ((totalRevenue - previousRevenue) / previousRevenue) * 100
                : 0;

            // 4. Calculate churn rate
            var activeCount = await _context.UserSubscriptions
                .Where(s => s.CreatedAt < endDate &&
                           (s.Status == "active" || s.Status == "trialing"))
                .CountAsync();

            var cancelledCount = await _context.UserSubscriptions
                .Where(s => s.CancelledAt >= startDate && s.CancelledAt < endDate)
                .CountAsync();

            var churnRate = activeCount > 0
                ? ((decimal)cancelledCount / activeCount) * 100
                : 0;

            // 5. Revenue by plan
            var revenueByPlan = new Dictionary<string, decimal>();
            var plans = await _planRepo.GetAllActiveAsync();

            foreach (var plan in plans)
            {
                var planRevenue = await _revenueRepo.GetRevenueByPlanAsync(plan.Id, startDate, endDate);
                revenueByPlan[plan.Name] = planRevenue;
            }

            // 6. Transaction counts
            var totalTransactions = await _revenueRepo.GetSuccessfulPaymentCountAsync(startDate, endDate);

            var failedTransactions = await _context.SubscriptionRevenues
                .Where(r => r.Status == "failed" &&
                           r.FailedAt >= startDate &&
                           r.FailedAt < endDate)
                .CountAsync();

            var refundedTransactions = await _context.SubscriptionRevenues
                .Where(r => (r.Status == "refunded" || r.Status == "partially_refunded") &&
                           r.RefundedAt >= startDate &&
                           r.RefundedAt < endDate)
                .CountAsync();

            // 7. Average transaction value
            var avgTransactionValue = totalTransactions > 0
                ? totalRevenue / totalTransactions
                : 0;

            var metrics = new RevenueMetricsDto
            {
                TotalRevenue = totalRevenue,
                MRR = mrr,
                ARR = arr,
                GrowthRate = growthRate,
                ChurnRate = churnRate,
                RevenueByPlan = revenueByPlan,
                TotalTransactions = totalTransactions,
                FailedTransactions = failedTransactions,
                RefundedTransactions = refundedTransactions,
                AverageTransactionValue = avgTransactionValue,
                StartDate = startDate,
                EndDate = endDate
            };

            _logger.LogInformation("[GetRevenueMetricsAsync] Metrics: Revenue={Revenue:C}, MRR={MRR:C}, " +
                                  "ARR={ARR:C}, Growth={Growth:F2}%, Churn={Churn:F2}%",
                metrics.TotalRevenue, metrics.MRR, metrics.ARR, metrics.GrowthRate, metrics.ChurnRate);

            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GetRevenueMetricsAsync] Failed to calculate metrics for {StartDate} to {EndDate}",
                startDate, endDate);
            throw;
        }
    }

    /// <summary>
    /// Gets monthly revenue breakdown for a full year (12 months)
    /// Useful for revenue charts and trend analysis
    /// </summary>
    public async Task<List<MonthlyRevenueDto>> GetMonthlyRevenueBreakdownAsync(int year)
    {
        _logger.LogInformation("[GetMonthlyRevenueBreakdownAsync] Calculating breakdown for year {Year}", year);

        try
        {
            // Validate year
            if (year < 2020 || year > 2100)
            {
                throw new ArgumentException($"Invalid year: {year}. Must be between 2020 and 2100.", nameof(year));
            }

            var breakdown = new List<MonthlyRevenueDto>();

            // Iterate through all 12 months
            for (int month = 1; month <= 12; month++)
            {
                var startDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
                var endDate = startDate.AddMonths(1);

                // Get revenue for month
                var totalRevenue = await _revenueRepo.GetTotalRevenueAsync(startDate, endDate);

                // Get transaction count
                var transactionCount = await _revenueRepo.GetSuccessfulPaymentCountAsync(startDate, endDate);

                // Calculate average
                var avgValue = transactionCount > 0 ? totalRevenue / transactionCount : 0;

                // Get subscription counts
                var newSubscriptions = await _context.UserSubscriptions
                    .Where(s => s.CreatedAt >= startDate && s.CreatedAt < endDate)
                    .CountAsync();

                var cancelledSubscriptions = await _context.UserSubscriptions
                    .Where(s => s.CancelledAt >= startDate && s.CancelledAt < endDate)
                    .CountAsync();

                // Get failed payments
                var failedPayments = await _context.SubscriptionRevenues
                    .Where(r => r.Status == "failed" &&
                               r.FailedAt >= startDate &&
                               r.FailedAt < endDate)
                    .CountAsync();

                // Get MRR at end of month
                var mrr = await GetMRRAsync(endDate.AddDays(-1));

                breakdown.Add(new MonthlyRevenueDto
                {
                    Year = year,
                    Month = month,
                    TotalRevenue = totalRevenue,
                    TransactionCount = transactionCount,
                    AverageTransactionValue = avgValue,
                    NewSubscriptions = newSubscriptions,
                    CancelledSubscriptions = cancelledSubscriptions,
                    FailedPayments = failedPayments,
                    MRR = mrr
                });

                _logger.LogDebug("[GetMonthlyRevenueBreakdownAsync] {Year}-{Month:D2}: Revenue={Revenue:C}, " +
                                "Transactions={Count}, MRR={MRR:C}",
                    year, month, totalRevenue, transactionCount, mrr);
            }

            var yearTotal = breakdown.Sum(b => b.TotalRevenue);
            _logger.LogInformation("[GetMonthlyRevenueBreakdownAsync] Year {Year} total: {Total:C}",
                year, yearTotal);

            return breakdown;
        }
        catch (Exception ex) when (ex is not ArgumentException)
        {
            _logger.LogError(ex, "[GetMonthlyRevenueBreakdownAsync] Failed to calculate breakdown for year {Year}", year);
            throw;
        }
    }

    #endregion
}
