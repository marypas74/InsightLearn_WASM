using InsightLearn.Core.Entities;
using InsightLearn.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace InsightLearn.Application.Services;

public class PayoutCalculationService : IPayoutCalculationService
{
    private readonly IInstructorPayoutRepository _payoutRepository;
    private readonly ISubscriptionRevenueRepository _revenueRepository;
    private readonly ICourseEngagementRepository _engagementRepository;
    private readonly IEngagementTrackingService _engagementTrackingService;
    private readonly ILogger<PayoutCalculationService> _logger;

    // Payout configuration
    private const decimal PLATFORM_COMMISSION_RATE = 0.20m; // 20% to platform
    private const decimal INSTRUCTOR_SHARE_RATE = 0.80m;    // 80% to instructors

    public PayoutCalculationService(
        IInstructorPayoutRepository payoutRepository,
        ISubscriptionRevenueRepository revenueRepository,
        ICourseEngagementRepository engagementRepository,
        IEngagementTrackingService engagementTrackingService,
        ILogger<PayoutCalculationService> logger)
    {
        _payoutRepository = payoutRepository;
        _revenueRepository = revenueRepository;
        _engagementRepository = engagementRepository;
        _engagementTrackingService = engagementTrackingService;
        _logger = logger;
    }

    public async Task<InstructorPayout?> CalculateMonthlyPayoutAsync(Guid instructorId, int month, int year)
    {
        try
        {
            // Check if payout already exists
            var existing = await _payoutRepository.GetByInstructorAndPeriodAsync(instructorId, month, year);
            if (existing != null)
            {
                _logger.LogInformation($"Payout already exists for instructor {instructorId} for {month}/{year}");
                return existing;
            }

            var startDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDate = startDate.AddMonths(1);

            // Get total platform revenue for the period
            var totalRevenue = await _revenueRepository.GetTotalRevenueAsync(startDate, endDate);

            // Get total platform engagement minutes
            var totalEngagementMinutes = await _engagementRepository.GetTotalEngagementMinutesAsync(startDate, endDate);

            // Get instructor's engagement minutes
            var instructorEngagementMinutes = await _engagementRepository.GetInstructorEngagementMinutesAsync(instructorId, startDate, endDate);

            // Get unique students count
            var uniqueStudents = await _engagementRepository.GetUniqueStudentCountAsync(instructorId, startDate, endDate);

            // Calculate engagement percentage
            decimal engagementPercentage = 0m;
            if (totalEngagementMinutes > 0)
            {
                engagementPercentage = (decimal)instructorEngagementMinutes / (decimal)totalEngagementMinutes;
            }

            // Calculate payout amount
            // Formula: (total_revenue * 0.80) * (instructor_engagement / total_engagement)
            var instructorSharePool = totalRevenue * INSTRUCTOR_SHARE_RATE;
            var payoutAmount = instructorSharePool * engagementPercentage;

            // Create payout record
            var payout = new InstructorPayout
            {
                Id = Guid.NewGuid(),
                InstructorId = instructorId,
                Month = month,
                Year = year,
                TotalEngagementMinutes = instructorEngagementMinutes,
                PlatformTotalEngagementMinutes = totalEngagementMinutes,
                TotalPlatformRevenue = totalRevenue,
                EngagementPercentage = engagementPercentage,
                PlatformCommissionRate = PLATFORM_COMMISSION_RATE,
                PayoutAmount = Math.Round(payoutAmount, 2),
                Status = "pending",
                UniqueStudentCount = uniqueStudents,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var created = await _payoutRepository.CreateAsync(payout);

            _logger.LogInformation(
                $"Calculated payout for instructor {instructorId} for {month}/{year}: " +
                $"€{payoutAmount:F2} ({engagementPercentage:P2} of total engagement)");

            return created;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error calculating payout for instructor {instructorId} for {month}/{year}");
            return null;
        }
    }

    public async Task<List<InstructorPayout>> CalculateAllPayoutsForPeriodAsync(int month, int year)
    {
        try
        {
            _logger.LogInformation($"Starting payout calculation for all instructors for {month}/{year}");

            var startDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDate = startDate.AddMonths(1);

            // Get all instructors who had engagement in this period
            var totalEngagementMinutes = await _engagementRepository.GetTotalEngagementMinutesAsync(startDate, endDate);

            if (totalEngagementMinutes == 0)
            {
                _logger.LogWarning($"No engagement found for period {month}/{year}, skipping payout calculation");
                return new List<InstructorPayout>();
            }

            // Get all unique instructor IDs from engagements
            var allEngagements = await GetAllEngagementsForPeriodAsync(startDate, endDate);
            var instructorIds = allEngagements
                .Select(e => e.Course.InstructorId)
                .Distinct()
                .ToList();

            _logger.LogInformation($"Found {instructorIds.Count} instructors with engagement in {month}/{year}");

            var payouts = new List<InstructorPayout>();

            foreach (var instructorId in instructorIds)
            {
                var payout = await CalculateMonthlyPayoutAsync(instructorId, month, year);
                if (payout != null)
                {
                    payouts.Add(payout);
                }
            }

            _logger.LogInformation($"Calculated {payouts.Count} payouts for {month}/{year}");

            return payouts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error calculating all payouts for {month}/{year}");
            return new List<InstructorPayout>();
        }
    }

    public async Task<int> ProcessPendingPayoutsAsync()
    {
        try
        {
            var pendingPayouts = await _payoutRepository.GetPendingPayoutsAsync();

            _logger.LogInformation($"Processing {pendingPayouts.Count} pending payouts");

            int processedCount = 0;

            foreach (var payout in pendingPayouts)
            {
                // Change status from pending to processing
                payout.Status = "processing";
                payout.UpdatedAt = DateTime.UtcNow;

                await _payoutRepository.UpdateAsync(payout);
                processedCount++;

                _logger.LogInformation($"Marked payout {payout.Id} as processing (€{payout.PayoutAmount:F2} for instructor {payout.InstructorId})");
            }

            return processedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing pending payouts");
            return 0;
        }
    }

    public async Task<bool> ExecutePayoutAsync(Guid payoutId)
    {
        try
        {
            var payout = await _payoutRepository.GetByIdAsync(payoutId);
            if (payout == null)
            {
                _logger.LogWarning($"Payout {payoutId} not found");
                return false;
            }

            if (payout.Status == "paid")
            {
                _logger.LogWarning($"Payout {payoutId} already paid");
                return true;
            }

            // TODO: Integration with Stripe Connect for actual payout execution
            // This will be implemented in StripeConnectService (optional for Week 2)
            // For now, we just mark as paid with a placeholder transfer ID

            _logger.LogWarning(
                $"Stripe Connect integration not implemented. " +
                $"Marking payout {payoutId} as paid with placeholder transfer ID");

            var transferId = $"placeholder_transfer_{Guid.NewGuid().ToString().Substring(0, 8)}";
            await _payoutRepository.MarkAsPaidAsync(payoutId, transferId);

            _logger.LogInformation($"Executed payout {payoutId}: €{payout.PayoutAmount:F2} to instructor {payout.InstructorId}");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error executing payout {payoutId}");
            return false;
        }
    }

    public async Task<PayoutPeriodSummary> GetPayoutSummaryAsync(int month, int year)
    {
        try
        {
            var startDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDate = startDate.AddMonths(1);

            var totalRevenue = await _revenueRepository.GetTotalRevenueAsync(startDate, endDate);
            var totalEngagementMinutes = await _engagementRepository.GetTotalEngagementMinutesAsync(startDate, endDate);

            var payouts = await _payoutRepository.GetPayoutsByPeriodAsync(month, year);

            var summary = new PayoutPeriodSummary
            {
                Month = month,
                Year = year,
                TotalRevenue = totalRevenue,
                PlatformCommission = totalRevenue * PLATFORM_COMMISSION_RATE,
                TotalInstructorPayouts = payouts.Sum(p => p.PayoutAmount),
                TotalPlatformEngagementMinutes = totalEngagementMinutes,
                TotalInstructors = payouts.Count,
                PayoutsPending = payouts.Count(p => p.Status == "pending"),
                PayoutsProcessing = payouts.Count(p => p.Status == "processing"),
                PayoutsPaid = payouts.Count(p => p.Status == "paid"),
                PayoutsFailed = payouts.Count(p => p.Status == "failed")
            };

            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting payout summary for {month}/{year}");
            return new PayoutPeriodSummary { Month = month, Year = year };
        }
    }

    public async Task<List<InstructorPayout>> GetInstructorPayoutHistoryAsync(Guid instructorId, int page = 1, int pageSize = 12)
    {
        try
        {
            return await _payoutRepository.GetByInstructorIdAsync(instructorId, page, pageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting payout history for instructor {instructorId}");
            return new List<InstructorPayout>();
        }
    }

    public async Task<decimal> GetInstructorTotalEarnedAsync(Guid instructorId)
    {
        try
        {
            return await _payoutRepository.GetTotalPaidOutAsync(instructorId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting total earned for instructor {instructorId}");
            return 0m;
        }
    }

    public async Task<List<InstructorEarningsSummary>> GetTopEarningInstructorsAsync(int month, int year, int topN = 10)
    {
        try
        {
            var payouts = await _payoutRepository.GetPayoutsByPeriodAsync(month, year);

            var topPayouts = payouts
                .OrderByDescending(p => p.PayoutAmount)
                .Take(topN)
                .ToList();

            var summaries = new List<InstructorEarningsSummary>();

            foreach (var payout in topPayouts)
            {
                var instructor = payout.Instructor;
                if (instructor == null)
                    continue;

                // Get engagement breakdown for courses count
                var startDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
                var endDate = startDate.AddMonths(1);
                var breakdown = await _engagementRepository.GetCourseEngagementBreakdownAsync(payout.InstructorId, startDate, endDate);

                var summary = new InstructorEarningsSummary
                {
                    InstructorId = payout.InstructorId,
                    InstructorName = $"{instructor.FirstName} {instructor.LastName}".Trim(),
                    InstructorEmail = instructor.Email,
                    PayoutAmount = payout.PayoutAmount,
                    EngagementMinutes = payout.TotalEngagementMinutes,
                    EngagementPercentage = payout.EngagementPercentage,
                    UniqueStudents = payout.UniqueStudentCount,
                    CoursesWithEngagement = breakdown.Count
                };

                summaries.Add(summary);
            }

            return summaries;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting top earning instructors for {month}/{year}");
            return new List<InstructorEarningsSummary>();
        }
    }

    public async Task<InstructorPayout?> RecalculatePayoutAsync(Guid payoutId)
    {
        try
        {
            var existingPayout = await _payoutRepository.GetByIdAsync(payoutId);
            if (existingPayout == null)
            {
                _logger.LogWarning($"Payout {payoutId} not found for recalculation");
                return null;
            }

            // Delete existing payout
            _logger.LogInformation($"Recalculating payout {payoutId} for instructor {existingPayout.InstructorId} for {existingPayout.Month}/{existingPayout.Year}");

            // Recalculate
            var newPayout = await CalculateMonthlyPayoutAsync(
                existingPayout.InstructorId,
                existingPayout.Month,
                existingPayout.Year);

            if (newPayout != null)
            {
                _logger.LogInformation(
                    $"Recalculated payout: Old=€{existingPayout.PayoutAmount:F2}, New=€{newPayout.PayoutAmount:F2}");
            }

            return newPayout;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error recalculating payout {payoutId}");
            return null;
        }
    }

    public async Task<bool> MarkPayoutAsFailedAsync(Guid payoutId, string errorMessage)
    {
        try
        {
            return await _payoutRepository.MarkAsFailedAsync(payoutId, errorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error marking payout {payoutId} as failed");
            return false;
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Gets all engagements for a period with course information
    /// </summary>
    private async Task<List<CourseEngagement>> GetAllEngagementsForPeriodAsync(DateTime startDate, DateTime endDate)
    {
        // This is a placeholder - we need to add a method to CourseEngagementRepository
        // For now, we'll use the breakdown by instructor approach
        return new List<CourseEngagement>();
    }

    #endregion
}
