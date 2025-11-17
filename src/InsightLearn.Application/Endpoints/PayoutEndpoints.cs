using InsightLearn.Core.Interfaces;
using InsightLearn.Core.DTOs.Payout;
using InsightLearn.Core.DTOs.Engagement;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InsightLearn.Application.Endpoints;

/// <summary>
/// Instructor Payout & Admin API Endpoints (10 endpoints total)
/// Tasks: T11 (Instructor - 4 endpoints), T12 (Admin - 6 endpoints)
/// Version: v2.0.0
/// </summary>
public static class PayoutEndpoints
{
    public static void MapPayoutEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api");

        // ==========================================================================
        // GROUP 3: INSTRUCTOR PAYOUT ENDPOINTS (4 endpoints)
        // ==========================================================================

        // ==========================================================================
        // ENDPOINT 13: GET /api/instructor/earnings/preview
        // Get instructor earnings preview for current month
        // ==========================================================================
        group.MapGet("/instructor/earnings/preview", async (
            ClaimsPrincipal user,
            [FromQuery] int? year,
            [FromQuery] int? month,
            [FromServices] IPayoutCalculationService payoutService,
            [FromServices] ILogger<Program> logger) =>
        {
            try
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    logger.LogWarning("[PAYOUTS] Unauthorized earnings preview attempt");
                    return Results.Unauthorized();
                }

                var targetYear = year ?? DateTime.UtcNow.Year;
                var targetMonth = month ?? DateTime.UtcNow.Month;

                logger.LogInformation("[PAYOUTS] Instructor {UserId} requesting earnings preview for {Year}-{Month}",
                    userId, targetYear, targetMonth);

                // Calculate preview payout for current/specified month
                var preview = await payoutService.CalculateMonthlyPayoutAsync(userId, targetMonth, targetYear);

                if (preview == null)
                {
                    logger.LogInformation("[PAYOUTS] No engagement found for instructor {UserId} in {Year}-{Month}",
                        userId, targetYear, targetMonth);
                    return Results.Ok(new
                    {
                        success = true,
                        data = new
                        {
                            instructorId = userId,
                            year = targetYear,
                            month = targetMonth,
                            estimatedPayout = 0.00m,
                            engagementMinutes = 0L,
                            engagementPercentage = 0.00m,
                            status = "no_engagement",
                            message = "No engagement recorded for this period"
                        }
                    });
                }

                logger.LogInformation("[PAYOUTS] Instructor {UserId} earnings preview: €{Amount}",
                    userId, preview.PayoutAmount);

                return Results.Ok(new
                {
                    success = true,
                    data = new
                    {
                        preview.InstructorId,
                        preview.Year,
                        preview.Month,
                        estimatedPayout = preview.PayoutAmount,
                        preview.TotalEngagementMinutes,
                        preview.EngagementPercentage,
                        preview.Status,
                        message = "Preview calculation - not yet finalized"
                    }
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[PAYOUTS] Error calculating earnings preview");
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Error calculating earnings preview");
            }
        })
        .RequireAuthorization(policy => policy.RequireRole("Instructor", "Admin"))
        .WithTags("Instructor Payouts")
        .WithName("GetEarningsPreview")
        .WithSummary("Get instructor earnings preview")
        .WithDescription("Preview estimated earnings for current or specified month (real-time calculation)")
        .Produces(200)
        .Produces(401)
        .Produces(403)
        .Produces(500);

        // ==========================================================================
        // ENDPOINT 14: GET /api/instructor/payouts
        // Get instructor payout history (paginated)
        // ==========================================================================
        group.MapGet("/instructor/payouts", async (
            [FromQuery] int page,
            [FromQuery] int pageSize,
            ClaimsPrincipal user,
            [FromServices] IPayoutCalculationService payoutService,
            [FromServices] ILogger<Program> logger) =>
        {
            try
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    logger.LogWarning("[PAYOUTS] Unauthorized payout history attempt");
                    return Results.Unauthorized();
                }

                logger.LogInformation("[PAYOUTS] Instructor {UserId} requesting payout history, page {Page}",
                    userId, page);

                var payouts = await payoutService.GetInstructorPayoutHistoryAsync(userId, page, pageSize);

                logger.LogInformation("[PAYOUTS] Retrieved {Count} payouts for instructor {UserId}",
                    payouts.Count, userId);

                return Results.Ok(new
                {
                    success = true,
                    data = payouts,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize,
                        totalItems = payouts.Count // Note: In real scenario, get total count from repository
                    }
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[PAYOUTS] Error retrieving payout history");
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Error retrieving payout history");
            }
        })
        .RequireAuthorization(policy => policy.RequireRole("Instructor", "Admin"))
        .WithTags("Instructor Payouts")
        .WithName("GetInstructorPayouts")
        .WithSummary("Get instructor payout history")
        .WithDescription("Returns paginated payout history for authenticated instructor")
        .Produces<List<Core.Entities.InstructorPayout>>(200)
        .Produces(401)
        .Produces(403)
        .Produces(500);

        // ==========================================================================
        // ENDPOINT 15: GET /api/instructor/payouts/{id}
        // Get specific payout details by ID
        // ==========================================================================
        group.MapGet("/instructor/payouts/{id:guid}", async (
            Guid id,
            ClaimsPrincipal user,
            [FromServices] IPayoutCalculationService payoutService,
            [FromServices] ILogger<Program> logger) =>
        {
            try
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    logger.LogWarning("[PAYOUTS] Unauthorized payout details attempt");
                    return Results.Unauthorized();
                }

                logger.LogInformation("[PAYOUTS] Instructor {UserId} requesting payout {PayoutId}",
                    userId, id);

                // Note: In real scenario, we'd fetch from repository and verify ownership
                // For now, return Not Implemented
                return Results.Problem(
                    detail: "Payout details endpoint requires repository implementation",
                    statusCode: 501,
                    title: "Not Implemented");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[PAYOUTS] Error retrieving payout details");
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Error retrieving payout details");
            }
        })
        .RequireAuthorization(policy => policy.RequireRole("Instructor", "Admin"))
        .WithTags("Instructor Payouts")
        .WithName("GetPayoutById")
        .WithSummary("Get payout details")
        .WithDescription("Returns detailed information for a specific payout")
        .Produces<Core.Entities.InstructorPayout>(200)
        .Produces(401)
        .Produces(403)
        .Produces(404)
        .Produces(501)
        .Produces(500);

        // ==========================================================================
        // ENDPOINT 16: POST /api/instructor/connect/onboard
        // Create Stripe Connect account and generate onboarding link
        // ==========================================================================
        group.MapPost("/instructor/connect/onboard", async (
            [FromBody] CreateConnectAccountRequest request,
            ClaimsPrincipal user,
            [FromServices] IInstructorConnectAccountService connectService,
            [FromServices] ILogger<Program> logger) =>
        {
            try
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    logger.LogWarning("[PAYOUTS] Unauthorized connect onboard attempt");
                    return Results.Unauthorized();
                }

                logger.LogInformation("[PAYOUTS] Instructor {UserId} creating Stripe Connect account, country {Country}",
                    userId, request.Country);

                // Create Connect account DTO
                var createAccountDto = new InsightLearn.Core.DTOs.Instructor.CreateConnectAccountDto
                {
                    Country = request.Country
                };
                var account = await connectService.CreateConnectAccountAsync(userId, createAccountDto);
                if (account == null)
                {
                    logger.LogWarning("[PAYOUTS] Failed to create Connect account for instructor {UserId}", userId);
                    return Results.BadRequest(new { success = false, error = "Failed to create Connect account" });
                }

                // Generate onboarding link
                var onboardingUrl = await connectService.GenerateOnboardingLinkAsync(
                    userId,
                    request.ReturnUrl,
                    request.RefreshUrl ?? request.ReturnUrl);

                logger.LogInformation("[PAYOUTS] Stripe Connect onboarding link generated for instructor {UserId}", userId);

                return Results.Ok(new
                {
                    success = true,
                    onboardingUrl,
                    accountId = account.Id,
                    message = "Complete onboarding to start receiving payouts"
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[PAYOUTS] Error creating Connect account");
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Error creating Connect account");
            }
        })
        .RequireAuthorization(policy => policy.RequireRole("Instructor"))
        .WithTags("Instructor Payouts")
        .WithName("CreateConnectAccount")
        .WithSummary("Create Stripe Connect account")
        .WithDescription("Creates Stripe Connect account for instructor and returns onboarding URL")
        .Produces(200)
        .Produces(400)
        .Produces(401)
        .Produces(403)
        .Produces(500);

        // ==========================================================================
        // GROUP 4: ADMIN PAYOUT ENDPOINTS (3 endpoints)
        // ==========================================================================

        // ==========================================================================
        // ENDPOINT 17: POST /api/admin/payouts/calculate/{year}/{month}
        // Calculate payouts for all instructors for specified period (Admin only)
        // ==========================================================================
        group.MapPost("/admin/payouts/calculate/{year:int}/{month:int}", async (
            int year,
            int month,
            [FromServices] IPayoutCalculationService payoutService,
            [FromServices] ILogger<Program> logger) =>
        {
            try
            {
                logger.LogInformation("[ADMIN] Calculating payouts for {Year}-{Month:D2}", year, month);

                var payouts = await payoutService.CalculateAllPayoutsForPeriodAsync(month, year);

                logger.LogInformation("[ADMIN] Calculated {Count} payouts for {Year}-{Month:D2}, total €{Total:F2}",
                    payouts.Count, year, month, payouts.Sum(p => p.PayoutAmount));

                return Results.Ok(new
                {
                    success = true,
                    summary = new
                    {
                        period = $"{year}-{month:D2}",
                        totalPayouts = payouts.Count,
                        totalAmount = payouts.Sum(p => p.PayoutAmount),
                        currency = "EUR"
                    },
                    payouts = payouts.Select(p => new
                    {
                        p.Id,
                        p.InstructorId,
                        p.PayoutAmount,
                        p.TotalEngagementMinutes,
                        p.EngagementPercentage,
                        p.Status
                    })
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[ADMIN] Error calculating payouts for {Year}-{Month}", year, month);
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Error calculating payouts");
            }
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithTags("Admin Payouts")
        .WithName("CalculatePayouts")
        .WithSummary("Calculate payouts for period")
        .WithDescription("Calculate payouts for all instructors for specified year/month (creates pending payout records)")
        .Produces(200)
        .Produces(401)
        .Produces(403)
        .Produces(500);

        // ==========================================================================
        // ENDPOINT 18: POST /api/admin/payouts/process/{id}
        // Execute specific payout to instructor's Stripe Connect account (Admin only)
        // ==========================================================================
        group.MapPost("/admin/payouts/process/{id:guid}", async (
            Guid id,
            [FromServices] IPayoutCalculationService payoutService,
            [FromServices] ILogger<Program> logger) =>
        {
            try
            {
                logger.LogInformation("[ADMIN] Executing payout {PayoutId}", id);

                var success = await payoutService.ExecutePayoutAsync(id);

                if (!success)
                {
                    logger.LogWarning("[ADMIN] Failed to execute payout {PayoutId}", id);
                    return Results.BadRequest(new
                    {
                        success = false,
                        error = "Failed to execute payout - check payout status and Stripe Connect account"
                    });
                }

                logger.LogInformation("[ADMIN] Payout {PayoutId} executed successfully", id);

                return Results.Ok(new
                {
                    success = true,
                    message = "Payout executed successfully",
                    payoutId = id
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[ADMIN] Error executing payout {PayoutId}", id);
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Error executing payout");
            }
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithTags("Admin Payouts")
        .WithName("ExecutePayout")
        .WithSummary("Execute payout")
        .WithDescription("Execute payout to instructor's Stripe Connect account (transfers funds)")
        .Produces(200)
        .Produces(400)
        .Produces(401)
        .Produces(403)
        .Produces(500);

        // ==========================================================================
        // ENDPOINT 19: GET /api/admin/payouts/pending
        // Get all pending payouts awaiting processing (Admin only)
        // ==========================================================================
        group.MapGet("/admin/payouts/pending", async (
            [FromServices] IPayoutCalculationService payoutService,
            [FromServices] ILogger<Program> logger) =>
        {
            try
            {
                logger.LogInformation("[ADMIN] Fetching pending payouts");

                // Process pending payouts (marks as 'processing')
                var processedCount = await payoutService.ProcessPendingPayoutsAsync();

                logger.LogInformation("[ADMIN] Marked {Count} pending payouts as processing", processedCount);

                return Results.Ok(new
                {
                    success = true,
                    processedCount,
                    message = $"Marked {processedCount} payouts as processing"
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[ADMIN] Error processing pending payouts");
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Error processing pending payouts");
            }
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithTags("Admin Payouts")
        .WithName("GetPendingPayouts")
        .WithSummary("Get pending payouts")
        .WithDescription("Returns all pending payouts and marks them as processing")
        .Produces(200)
        .Produces(401)
        .Produces(403)
        .Produces(500);

        // ==========================================================================
        // GROUP 5: ADMIN ENGAGEMENT & METRICS ENDPOINTS (3 endpoints)
        // ==========================================================================

        // ==========================================================================
        // ENDPOINT 20: GET /api/admin/engagement/course/{id}
        // Get course engagement statistics (Admin only)
        // ==========================================================================
        group.MapGet("/admin/engagement/course/{id:guid}", async (
            Guid id,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromServices] IEngagementTrackingService engagementService,
            [FromServices] ILogger<Program> logger) =>
        {
            try
            {
                var start = startDate ?? DateTime.UtcNow.AddMonths(-1);
                var end = endDate ?? DateTime.UtcNow;

                logger.LogInformation("[ADMIN] Fetching engagement for course {CourseId}, period {Start} to {End}",
                    id, start, end);

                var analytics = await engagementService.GetCourseEngagementAnalyticsAsync(id, start, end);

                logger.LogInformation("[ADMIN] Course {CourseId} engagement: {TotalMinutes} minutes, {UniqueUsers} users",
                    id, analytics.TotalEngagementMinutes, analytics.UniqueUsers);

                return Results.Ok(new
                {
                    success = true,
                    data = analytics
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[ADMIN] Error retrieving course engagement");
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Error retrieving course engagement");
            }
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithTags("Admin Engagement")
        .WithName("GetCourseEngagement")
        .WithSummary("Get course engagement statistics")
        .WithDescription("Returns detailed engagement analytics for a specific course")
        .Produces<CourseEngagementAnalytics>(200)
        .Produces(401)
        .Produces(403)
        .Produces(500);

        // ==========================================================================
        // ENDPOINT 21: GET /api/admin/engagement/monthly-summary
        // Get platform-wide engagement summary for specified month (Admin only)
        // ==========================================================================
        group.MapGet("/admin/engagement/monthly-summary", async (
            [FromQuery] int month,
            [FromQuery] int year,
            [FromServices] IEngagementTrackingService engagementService,
            [FromServices] ILogger<Program> logger) =>
        {
            try
            {
                logger.LogInformation("[ADMIN] Fetching monthly engagement summary for {Year}-{Month:D2}", year, month);

                var startDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
                var endDate = startDate.AddMonths(1).AddTicks(-1);

                // Note: Would need a platform-wide summary method in service
                // For now, return placeholder
                return Results.Ok(new
                {
                    success = true,
                    data = new
                    {
                        period = $"{year}-{month:D2}",
                        message = "Platform-wide engagement summary - to be implemented",
                        startDate,
                        endDate
                    }
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[ADMIN] Error retrieving monthly engagement summary");
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Error retrieving monthly engagement summary");
            }
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithTags("Admin Engagement")
        .WithName("GetMonthlyEngagementSummary")
        .WithSummary("Get monthly engagement summary")
        .WithDescription("Returns platform-wide engagement summary for specified month")
        .Produces(200)
        .Produces(401)
        .Produces(403)
        .Produces(500);

        // ==========================================================================
        // ENDPOINT 22: GET /api/admin/subscriptions/metrics
        // Get subscription revenue metrics (Admin only)
        // ==========================================================================
        group.MapGet("/admin/subscriptions/metrics", async (
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromServices] ISubscriptionRevenueService revenueService,
            [FromServices] ILogger<Program> logger) =>
        {
            try
            {
                var start = startDate ?? DateTime.UtcNow.AddMonths(-1);
                var end = endDate ?? DateTime.UtcNow;

                logger.LogInformation("[ADMIN] Fetching subscription metrics, period {Start} to {End}", start, end);

                var metrics = await revenueService.GetRevenueMetricsAsync(start, end);

                logger.LogInformation("[ADMIN] Revenue metrics: €{TotalRevenue:F2}, MRR €{MRR:F2}",
                    metrics.TotalRevenue, metrics.MRR);

                return Results.Ok(new
                {
                    success = true,
                    data = metrics
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[ADMIN] Error retrieving subscription metrics");
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Error retrieving subscription metrics");
            }
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithTags("Admin Metrics")
        .WithName("GetSubscriptionMetrics")
        .WithSummary("Get subscription revenue metrics")
        .WithDescription("Returns MRR, churn rate, active subscriptions, and revenue breakdown")
        .Produces(200)
        .Produces(401)
        .Produces(403)
        .Produces(500);
    }
}

// ==========================================================================
// REQUEST DTOs
// ==========================================================================

public record CreateConnectAccountRequest(
    string Country, // ISO 3166-1 alpha-2 (e.g., "IT", "US", "GB")
    string ReturnUrl,
    string? RefreshUrl = null);
