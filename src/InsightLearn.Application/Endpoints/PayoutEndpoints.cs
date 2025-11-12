using InsightLearn.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InsightLearn.Application.Endpoints;

public static class PayoutEndpoints
{
    public static void MapPayoutEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/payouts")
            .WithTags("Payouts")
            .RequireAuthorization();

        // GET /api/payouts/my-history - Get instructor's payout history
        group.MapGet("/my-history", async (
            ClaimsPrincipal user,
            [FromServices] IPayoutCalculationService payoutService,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 12) =>
        {
            try
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Results.Unauthorized();
                }

                var history = await payoutService.GetInstructorPayoutHistoryAsync(userId, page, pageSize);
                return Results.Ok(history);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Error retrieving payout history");
            }
        })
        .RequireAuthorization(policy => policy.RequireRole("Instructor", "Admin"))
        .WithName("GetMyPayoutHistory")
        .Produces<List<Core.Entities.InstructorPayout>>(200)
        .Produces(401)
        .Produces(403)
        .Produces(500);

        // GET /api/payouts/my-total-earned - Get instructor's total lifetime earnings
        group.MapGet("/my-total-earned", async (
            ClaimsPrincipal user,
            [FromServices] IPayoutCalculationService payoutService) =>
        {
            try
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Results.Unauthorized();
                }

                var totalEarned = await payoutService.GetInstructorTotalEarnedAsync(userId);
                return Results.Ok(new { totalEarned, currency = "EUR" });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Error retrieving total earnings");
            }
        })
        .RequireAuthorization(policy => policy.RequireRole("Instructor", "Admin"))
        .WithName("GetMyTotalEarned")
        .Produces(200)
        .Produces(401)
        .Produces(403)
        .Produces(500);

        // POST /api/payouts/calculate - Calculate payouts for a specific period (Admin only)
        group.MapPost("/calculate", async (
            [FromBody] CalculatePayoutsRequest request,
            [FromServices] IPayoutCalculationService payoutService,
            [FromServices] ILogger<Program> logger) =>
        {
            try
            {
                logger.LogInformation($"Calculating payouts for {request.Month}/{request.Year}");

                var payouts = await payoutService.CalculateAllPayoutsForPeriodAsync(request.Month, request.Year);

                logger.LogInformation($"Calculated {payouts.Count} payouts for {request.Month}/{request.Year}");

                return Results.Ok(new
                {
                    success = true,
                    payoutsCalculated = payouts.Count,
                    totalAmount = payouts.Sum(p => p.PayoutAmount),
                    payouts = payouts.Select(p => new
                    {
                        p.Id,
                        p.InstructorId,
                        p.PayoutAmount,
                        p.EngagementPercentage,
                        p.Status
                    })
                });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Error calculating payouts");
            }
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithName("CalculatePayouts")
        .Produces(200)
        .Produces(401)
        .Produces(403)
        .Produces(500);

        // POST /api/payouts/process-pending - Process pending payouts (Admin only)
        group.MapPost("/process-pending", async (
            [FromServices] IPayoutCalculationService payoutService,
            [FromServices] ILogger<Program> logger) =>
        {
            try
            {
                logger.LogInformation("Processing pending payouts");

                var processedCount = await payoutService.ProcessPendingPayoutsAsync();

                logger.LogInformation($"Processed {processedCount} pending payouts");

                return Results.Ok(new
                {
                    success = true,
                    processedCount,
                    message = $"Marked {processedCount} payouts as processing"
                });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Error processing pending payouts");
            }
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithName("ProcessPendingPayouts")
        .Produces(200)
        .Produces(401)
        .Produces(403)
        .Produces(500);

        // POST /api/payouts/{id}/execute - Execute a specific payout (Admin only)
        group.MapPost("/{id:guid}/execute", async (
            Guid id,
            [FromServices] IPayoutCalculationService payoutService,
            [FromServices] ILogger<Program> logger) =>
        {
            try
            {
                logger.LogInformation($"Executing payout {id}");

                var success = await payoutService.ExecutePayoutAsync(id);

                if (!success)
                    return Results.BadRequest(new { error = "Failed to execute payout" });

                logger.LogInformation($"Payout {id} executed successfully");

                return Results.Ok(new
                {
                    success = true,
                    message = "Payout executed successfully"
                });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Error executing payout");
            }
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithName("ExecutePayout")
        .Produces(200)
        .Produces(400)
        .Produces(401)
        .Produces(403)
        .Produces(500);

        // GET /api/payouts/period-summary - Get payout summary for a period (Admin only)
        group.MapGet("/period-summary", async (
            [FromQuery] int month,
            [FromQuery] int year,
            [FromServices] IPayoutCalculationService payoutService) =>
        {
            try
            {
                var summary = await payoutService.GetPayoutSummaryAsync(month, year);
                return Results.Ok(summary);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Error retrieving payout summary");
            }
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithName("GetPayoutPeriodSummary")
        .Produces<PayoutPeriodSummary>(200)
        .Produces(401)
        .Produces(403)
        .Produces(500);

        // GET /api/payouts/top-earners - Get top earning instructors for a period (Admin only)
        group.MapGet("/top-earners", async (
            [FromQuery] int month,
            [FromQuery] int year,
            [FromServices] IPayoutCalculationService payoutService,
            [FromQuery] int topN = 10) =>
        {
            try
            {
                var topEarners = await payoutService.GetTopEarningInstructorsAsync(month, year, topN);
                return Results.Ok(topEarners);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Error retrieving top earners");
            }
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithName("GetTopEarningInstructors")
        .Produces<List<InstructorEarningsSummary>>(200)
        .Produces(401)
        .Produces(403)
        .Produces(500);

        // POST /api/payouts/{id}/recalculate - Recalculate a payout (Admin only)
        group.MapPost("/{id:guid}/recalculate", async (
            Guid id,
            [FromServices] IPayoutCalculationService payoutService,
            [FromServices] ILogger<Program> logger) =>
        {
            try
            {
                logger.LogInformation($"Recalculating payout {id}");

                var payout = await payoutService.RecalculatePayoutAsync(id);

                if (payout == null)
                    return Results.NotFound(new { error = "Payout not found" });

                logger.LogInformation($"Payout {id} recalculated: €{payout.PayoutAmount:F2}");

                return Results.Ok(new
                {
                    success = true,
                    payout = new
                    {
                        payout.Id,
                        payout.PayoutAmount,
                        payout.EngagementPercentage,
                        payout.Status
                    }
                });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Error recalculating payout");
            }
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithName("RecalculatePayout")
        .Produces(200)
        .Produces(401)
        .Produces(403)
        .Produces(404)
        .Produces(500);
    }
}

// DTOs
public record CalculatePayoutsRequest(int Month, int Year);
