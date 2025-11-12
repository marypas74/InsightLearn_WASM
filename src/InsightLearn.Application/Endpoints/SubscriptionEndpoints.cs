using InsightLearn.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InsightLearn.Application.Endpoints;

public static class SubscriptionEndpoints
{
    public static void MapSubscriptionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/subscriptions")
            .WithTags("Subscriptions")
            .RequireAuthorization();

        // GET /api/subscriptions/plans - Get active subscription plans
        group.MapGet("/plans", async (
            [FromServices] ISubscriptionService subscriptionService) =>
        {
            try
            {
                var plans = await subscriptionService.GetActiveSubscriptionPlansAsync();
                return Results.Ok(plans);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Error retrieving subscription plans");
            }
        })
        .AllowAnonymous()
        .WithName("GetSubscriptionPlans")
        .Produces<List<Core.Entities.SubscriptionPlan>>(200)
        .Produces(500);

        // GET /api/subscriptions/plans/{id} - Get specific plan
        group.MapGet("/plans/{id:guid}", async (
            Guid id,
            [FromServices] ISubscriptionService subscriptionService) =>
        {
            try
            {
                var plan = await subscriptionService.GetSubscriptionPlanByIdAsync(id);
                if (plan == null)
                    return Results.NotFound(new { error = "Subscription plan not found" });

                return Results.Ok(plan);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Error retrieving subscription plan");
            }
        })
        .AllowAnonymous()
        .WithName("GetSubscriptionPlanById")
        .Produces<Core.Entities.SubscriptionPlan>(200)
        .Produces(404)
        .Produces(500);

        // GET /api/subscriptions/my-subscription - Get current user's active subscription
        group.MapGet("/my-subscription", async (
            ClaimsPrincipal user,
            [FromServices] ISubscriptionService subscriptionService) =>
        {
            try
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Results.Unauthorized();
                }

                var subscription = await subscriptionService.GetActiveSubscriptionAsync(userId);
                if (subscription == null)
                    return Results.NotFound(new { error = "No active subscription found" });

                return Results.Ok(subscription);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Error retrieving subscription");
            }
        })
        .WithName("GetMySubscription")
        .Produces<Core.Entities.UserSubscription>(200)
        .Produces(401)
        .Produces(404)
        .Produces(500);

        // POST /api/subscriptions/subscribe - Create new subscription
        group.MapPost("/subscribe", async (
            [FromBody] SubscribeRequest request,
            ClaimsPrincipal user,
            [FromServices] ISubscriptionService subscriptionService) =>
        {
            try
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Results.Unauthorized();
                }

                var subscription = await subscriptionService.CreateSubscriptionAsync(
                    userId,
                    request.PlanId,
                    request.BillingInterval,
                    request.StripeSubscriptionId);

                if (subscription == null)
                    return Results.BadRequest(new { error = "Failed to create subscription" });

                return Results.Ok(subscription);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Error creating subscription");
            }
        })
        .WithName("Subscribe")
        .Produces<Core.Entities.UserSubscription>(200)
        .Produces(400)
        .Produces(401)
        .Produces(500);

        // POST /api/subscriptions/{id}/cancel - Cancel subscription
        group.MapPost("/{id:guid}/cancel", async (
            Guid id,
            [FromBody] CancelSubscriptionRequest? request,
            ClaimsPrincipal user,
            [FromServices] ISubscriptionService subscriptionService) =>
        {
            try
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Results.Unauthorized();
                }

                var success = await subscriptionService.CancelSubscriptionAsync(
                    id,
                    request?.Reason,
                    request?.Feedback);

                if (!success)
                    return Results.BadRequest(new { error = "Failed to cancel subscription" });

                return Results.Ok(new { success = true, message = "Subscription cancelled successfully" });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Error cancelling subscription");
            }
        })
        .WithName("CancelSubscription")
        .Produces(200)
        .Produces(400)
        .Produces(401)
        .Produces(500);

        // POST /api/subscriptions/{id}/reactivate - Reactivate cancelled subscription
        group.MapPost("/{id:guid}/reactivate", async (
            Guid id,
            ClaimsPrincipal user,
            [FromServices] ISubscriptionService subscriptionService) =>
        {
            try
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Results.Unauthorized();
                }

                var success = await subscriptionService.ReactivateSubscriptionAsync(id);

                if (!success)
                    return Results.BadRequest(new { error = "Failed to reactivate subscription" });

                return Results.Ok(new { success = true, message = "Subscription reactivated successfully" });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Error reactivating subscription");
            }
        })
        .WithName("ReactivateSubscription")
        .Produces(200)
        .Produces(400)
        .Produces(401)
        .Produces(500);

        // GET /api/subscriptions/analytics/mrr - Monthly Recurring Revenue (Admin only)
        group.MapGet("/analytics/mrr", async (
            [FromServices] ISubscriptionService subscriptionService) =>
        {
            try
            {
                var mrr = await subscriptionService.GetMonthlyRecurringRevenueAsync();
                return Results.Ok(new { mrr, currency = "EUR" });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Error calculating MRR");
            }
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithName("GetMRR")
        .Produces(200)
        .Produces(401)
        .Produces(403)
        .Produces(500);

        // GET /api/subscriptions/analytics/active-count - Active subscription count (Admin only)
        group.MapGet("/analytics/active-count", async (
            [FromServices] ISubscriptionService subscriptionService) =>
        {
            try
            {
                var count = await subscriptionService.GetActiveSubscriptionCountAsync();
                return Results.Ok(new { activeSubscriptions = count });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Error counting active subscriptions");
            }
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithName("GetActiveSubscriptionCount")
        .Produces(200)
        .Produces(401)
        .Produces(403)
        .Produces(500);
    }
}

// DTOs
public record SubscribeRequest(Guid PlanId, string BillingInterval, string? StripeSubscriptionId = null);
public record CancelSubscriptionRequest(string? Reason, string? Feedback);
