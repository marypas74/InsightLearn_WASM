using InsightLearn.Core.Interfaces;
using InsightLearn.Core.DTOs.Subscription;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Stripe.Checkout;
using Stripe;
using Stripe.BillingPortal;
using Microsoft.Extensions.Configuration;

namespace InsightLearn.Application.Endpoints;

/// <summary>
/// Subscription Management API Endpoints (9 endpoints)
/// Tasks: T9 - Subscription API Endpoints
/// Version: v2.0.0
/// </summary>
public static class SubscriptionEndpoints
{
    public static void MapSubscriptionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/subscriptions")
            .WithTags("Subscriptions");

        // ==========================================================================
        // ENDPOINT 1: GET /api/subscriptions/plans
        // Get all active subscription plans (PUBLIC - no auth required)
        // ==========================================================================
        group.MapGet("/plans", async (
            [FromServices] ISubscriptionPlanService planService,
            [FromServices] ILogger<Program> logger) =>
        {
            try
            {
                logger.LogInformation("[SUBSCRIPTIONS] Fetching all active subscription plans");

                var plans = await planService.GetAllPlansAsync();

                logger.LogInformation("[SUBSCRIPTIONS] Retrieved {Count} active plans", plans.Count);
                return Results.Ok(new { success = true, data = plans });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[SUBSCRIPTIONS] Error retrieving subscription plans");
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Error retrieving subscription plans");
            }
        })
        .AllowAnonymous()
        .WithName("GetSubscriptionPlans")
        .WithSummary("Get all active subscription plans")
        .WithDescription("Public endpoint - returns Basic, Pro, Premium plans with pricing and features")
        .Produces<List<SubscriptionPlanDto>>(200)
        .Produces(500);

        // ==========================================================================
        // ENDPOINT 2: POST /api/subscriptions/subscribe
        // Create new subscription (requires authentication)
        // ==========================================================================
        group.MapPost("/subscribe", async (
            [FromBody] CreateSubscriptionRequest request,
            ClaimsPrincipal user,
            [FromServices] ISubscriptionService subscriptionService,
            [FromServices] ILogger<Program> logger) =>
        {
            try
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    logger.LogWarning("[SUBSCRIPTIONS] Unauthorized subscribe attempt");
                    return Results.Unauthorized();
                }

                logger.LogInformation("[SUBSCRIPTIONS] User {UserId} subscribing to plan {PlanId}", userId, request.PlanId);

                var subscription = await subscriptionService.CreateSubscriptionAsync(
                    userId,
                    request.PlanId,
                    request.BillingInterval,
                    request.StripeSubscriptionId);

                if (subscription == null)
                {
                    logger.LogWarning("[SUBSCRIPTIONS] Failed to create subscription for user {UserId}", userId);
                    return Results.BadRequest(new { success = false, error = "Failed to create subscription" });
                }

                logger.LogInformation("[SUBSCRIPTIONS] Subscription {SubscriptionId} created successfully", subscription.Id);
                return Results.Ok(new { success = true, data = subscription });
            }
            catch (BusinessException ex)
            {
                logger.LogWarning(ex, "[SUBSCRIPTIONS] Business validation failed");
                return Results.BadRequest(new { success = false, error = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[SUBSCRIPTIONS] Error creating subscription");
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Error creating subscription");
            }
        })
        .RequireAuthorization()
        .WithName("Subscribe")
        .WithSummary("Create new subscription")
        .WithDescription("Subscribe user to a plan (monthly or yearly billing)")
        .Produces<Core.Entities.UserSubscription>(200)
        .Produces(400)
        .Produces(401)
        .Produces(500);

        // ==========================================================================
        // ENDPOINT 3: POST /api/subscriptions/cancel
        // Cancel subscription (requires authentication)
        // ==========================================================================
        group.MapPost("/cancel", async (
            [FromBody] CancelSubscriptionRequest request,
            ClaimsPrincipal user,
            [FromServices] ISubscriptionService subscriptionService,
            [FromServices] ILogger<Program> logger) =>
        {
            try
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    logger.LogWarning("[SUBSCRIPTIONS] Unauthorized cancel attempt");
                    return Results.Unauthorized();
                }

                logger.LogInformation("[SUBSCRIPTIONS] Cancelling subscription {SubscriptionId}", request.SubscriptionId);

                var success = await subscriptionService.CancelSubscriptionAsync(
                    request.SubscriptionId,
                    request.Reason,
                    request.Feedback);

                if (!success)
                {
                    logger.LogWarning("[SUBSCRIPTIONS] Failed to cancel subscription {SubscriptionId}", request.SubscriptionId);
                    return Results.BadRequest(new { success = false, error = "Failed to cancel subscription" });
                }

                logger.LogInformation("[SUBSCRIPTIONS] Subscription {SubscriptionId} cancelled successfully", request.SubscriptionId);
                return Results.Ok(new { success = true, message = "Subscription cancelled successfully" });
            }
            catch (NotFoundException ex)
            {
                logger.LogWarning(ex, "[SUBSCRIPTIONS] Subscription not found");
                return Results.NotFound(new { success = false, error = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[SUBSCRIPTIONS] Error cancelling subscription");
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Error cancelling subscription");
            }
        })
        .RequireAuthorization()
        .WithName("CancelSubscription")
        .WithSummary("Cancel subscription")
        .WithDescription("Cancel subscription (remains active until period end unless immediate cancellation requested)")
        .Produces(200)
        .Produces(400)
        .Produces(401)
        .Produces(404)
        .Produces(500);

        // ==========================================================================
        // ENDPOINT 4: POST /api/subscriptions/upgrade
        // Upgrade subscription plan (requires authentication)
        // ==========================================================================
        group.MapPost("/upgrade", async (
            [FromBody] UpgradeSubscriptionRequest request,
            ClaimsPrincipal user,
            [FromServices] ISubscriptionService subscriptionService,
            [FromServices] ILogger<Program> logger) =>
        {
            try
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    logger.LogWarning("[SUBSCRIPTIONS] Unauthorized upgrade attempt");
                    return Results.Unauthorized();
                }

                logger.LogInformation("[SUBSCRIPTIONS] Upgrading subscription {SubscriptionId} to plan {NewPlanId}",
                    request.SubscriptionId, request.NewPlanId);

                // Note: Upgrade logic would be implemented in SubscriptionService
                // For now, return 501 Not Implemented as this requires Stripe integration
                return Results.Problem(
                    detail: "Subscription upgrade requires Stripe integration (to be implemented)",
                    statusCode: 501,
                    title: "Not Implemented");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[SUBSCRIPTIONS] Error upgrading subscription");
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Error upgrading subscription");
            }
        })
        .RequireAuthorization()
        .WithName("UpgradeSubscription")
        .WithSummary("Upgrade subscription plan")
        .WithDescription("Upgrade to higher-tier plan (prorated billing)")
        .Produces(200)
        .Produces(400)
        .Produces(401)
        .Produces(501)
        .Produces(500);

        // ==========================================================================
        // ENDPOINT 5: POST /api/subscriptions/downgrade
        // Downgrade subscription plan (requires authentication)
        // ==========================================================================
        group.MapPost("/downgrade", async (
            [FromBody] DowngradeSubscriptionRequest request,
            ClaimsPrincipal user,
            [FromServices] ISubscriptionService subscriptionService,
            [FromServices] ILogger<Program> logger) =>
        {
            try
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    logger.LogWarning("[SUBSCRIPTIONS] Unauthorized downgrade attempt");
                    return Results.Unauthorized();
                }

                logger.LogInformation("[SUBSCRIPTIONS] Downgrading subscription {SubscriptionId} to plan {NewPlanId}",
                    request.SubscriptionId, request.NewPlanId);

                // Note: Downgrade logic would be implemented in SubscriptionService
                // For now, return 501 Not Implemented as this requires Stripe integration
                return Results.Problem(
                    detail: "Subscription downgrade requires Stripe integration (to be implemented)",
                    statusCode: 501,
                    title: "Not Implemented");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[SUBSCRIPTIONS] Error downgrading subscription");
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Error downgrading subscription");
            }
        })
        .RequireAuthorization()
        .WithName("DowngradeSubscription")
        .WithSummary("Downgrade subscription plan")
        .WithDescription("Downgrade to lower-tier plan (applied at period end)")
        .Produces(200)
        .Produces(400)
        .Produces(401)
        .Produces(501)
        .Produces(500);

        // ==========================================================================
        // ENDPOINT 6: GET /api/subscriptions/my-subscription
        // Get current user's active subscription
        // ==========================================================================
        group.MapGet("/my-subscription", async (
            ClaimsPrincipal user,
            [FromServices] ISubscriptionService subscriptionService,
            [FromServices] ILogger<Program> logger) =>
        {
            try
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    logger.LogWarning("[SUBSCRIPTIONS] Unauthorized my-subscription attempt");
                    return Results.Unauthorized();
                }

                logger.LogInformation("[SUBSCRIPTIONS] Fetching subscription for user {UserId}", userId);

                var subscription = await subscriptionService.GetActiveSubscriptionAsync(userId);
                if (subscription == null)
                {
                    logger.LogInformation("[SUBSCRIPTIONS] No active subscription found for user {UserId}", userId);
                    return Results.NotFound(new { success = false, error = "No active subscription found" });
                }

                logger.LogInformation("[SUBSCRIPTIONS] Retrieved subscription {SubscriptionId} for user {UserId}",
                    subscription.Id, userId);
                return Results.Ok(new { success = true, data = subscription });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[SUBSCRIPTIONS] Error retrieving user subscription");
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Error retrieving subscription");
            }
        })
        .RequireAuthorization()
        .WithName("GetMySubscription")
        .WithSummary("Get current user's subscription")
        .WithDescription("Returns active subscription for authenticated user")
        .Produces<Core.Entities.UserSubscription>(200)
        .Produces(401)
        .Produces(404)
        .Produces(500);

        // ==========================================================================
        // ENDPOINT 7: POST /api/subscriptions/create-checkout-session
        // Create Stripe checkout session for subscription payment
        // ==========================================================================
        group.MapPost("/create-checkout-session", async (
            [FromBody] CreateCheckoutSessionRequest request,
            ClaimsPrincipal user,
            [FromServices] ISubscriptionService subscriptionService,
            [FromServices] ISubscriptionPlanService planService,
            [FromServices] IConfiguration configuration,
            [FromServices] ILogger<Program> logger) =>
        {
            try
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    logger.LogWarning("[SUBSCRIPTIONS] Unauthorized checkout session attempt");
                    return Results.Unauthorized();
                }

                logger.LogInformation("[SUBSCRIPTIONS] Creating Stripe checkout session for user {UserId}, plan {PlanId}",
                    userId, request.PlanId);

                // Get plan
                var plan = await planService.GetPlanByIdAsync(request.PlanId);
                if (plan == null)
                {
                    logger.LogWarning("[SUBSCRIPTIONS] Plan {PlanId} not found", request.PlanId);
                    return Results.NotFound(new { success = false, error = "Subscription plan not found" });
                }

                // Determine Stripe Price ID based on billing interval
                var stripePriceId = request.BillingInterval == "yearly"
                    ? plan.StripePriceYearlyId
                    : plan.StripePriceMonthlyId;

                if (string.IsNullOrEmpty(stripePriceId))
                {
                    logger.LogWarning("[SUBSCRIPTIONS] Stripe price ID not configured for plan {PlanId}", plan.Id);
                    return Results.BadRequest(new { success = false, error = "Stripe price ID not configured for this plan" });
                }

                // Initialize Stripe
                var stripeSecretKey = Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY")
                    ?? configuration["Stripe:SecretKey"];
                if (string.IsNullOrEmpty(stripeSecretKey))
                {
                    logger.LogError("[SUBSCRIPTIONS] Stripe secret key not configured");
                    return Results.Problem(
                        detail: "Payment system not configured",
                        statusCode: 500,
                        title: "Configuration Error");
                }

                StripeConfiguration.ApiKey = stripeSecretKey;

                // Create Stripe Checkout Session
                var options = new Stripe.Checkout.SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string> { "card" },
                    LineItems = new List<Stripe.Checkout.SessionLineItemOptions>
                    {
                        new Stripe.Checkout.SessionLineItemOptions
                        {
                            Price = stripePriceId,
                            Quantity = 1
                        }
                    },
                    Mode = "subscription",
                    SuccessUrl = request.ReturnUrl + "?session_id={CHECKOUT_SESSION_ID}",
                    CancelUrl = request.ReturnUrl + "?cancelled=true",
                    ClientReferenceId = userId.ToString(),
                    Metadata = new Dictionary<string, string>
                    {
                        { "userId", userId.ToString() },
                        { "planId", request.PlanId.ToString() },
                        { "billingInterval", request.BillingInterval }
                    }
                };

                var service = new Stripe.Checkout.SessionService();
                var session = await service.CreateAsync(options);

                logger.LogInformation("[SUBSCRIPTIONS] Stripe checkout session created: {SessionId}", session.Id);

                return Results.Ok(new
                {
                    success = true,
                    checkoutUrl = session.Url,
                    sessionId = session.Id
                });
            }
            catch (StripeException ex)
            {
                logger.LogError(ex, "[SUBSCRIPTIONS] Stripe error creating checkout session");
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 400,
                    title: "Stripe Error");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[SUBSCRIPTIONS] Error creating checkout session");
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Error creating checkout session");
            }
        })
        .RequireAuthorization()
        .WithName("CreateCheckoutSession")
        .WithSummary("Create Stripe checkout session")
        .WithDescription("Creates Stripe checkout session for subscription payment")
        .Produces(200)
        .Produces(400)
        .Produces(401)
        .Produces(404)
        .Produces(500);

        // ==========================================================================
        // ENDPOINT 8: POST /api/subscriptions/create-portal-session
        // Create Stripe customer portal session for subscription management
        // ==========================================================================
        group.MapPost("/create-portal-session", async (
            [FromBody] CreatePortalSessionRequest request,
            ClaimsPrincipal user,
            [FromServices] ISubscriptionService subscriptionService,
            [FromServices] IConfiguration configuration,
            [FromServices] ILogger<Program> logger) =>
        {
            try
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    logger.LogWarning("[SUBSCRIPTIONS] Unauthorized portal session attempt");
                    return Results.Unauthorized();
                }

                logger.LogInformation("[SUBSCRIPTIONS] Creating Stripe portal session for user {UserId}", userId);

                // Get user's active subscription
                var subscription = await subscriptionService.GetActiveSubscriptionAsync(userId);
                if (subscription == null || string.IsNullOrEmpty(subscription.StripeCustomerId))
                {
                    logger.LogWarning("[SUBSCRIPTIONS] No active subscription with Stripe customer ID for user {UserId}", userId);
                    return Results.NotFound(new { success = false, error = "No active subscription found" });
                }

                // Initialize Stripe
                var stripeSecretKey = Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY")
                    ?? configuration["Stripe:SecretKey"];
                if (string.IsNullOrEmpty(stripeSecretKey))
                {
                    logger.LogError("[SUBSCRIPTIONS] Stripe secret key not configured");
                    return Results.Problem(
                        detail: "Payment system not configured",
                        statusCode: 500,
                        title: "Configuration Error");
                }

                StripeConfiguration.ApiKey = stripeSecretKey;

                // Create Stripe Customer Portal Session
                var options = new Stripe.BillingPortal.SessionCreateOptions
                {
                    Customer = subscription.StripeCustomerId,
                    ReturnUrl = request.ReturnUrl
                };

                var service = new Stripe.BillingPortal.SessionService();
                var session = await service.CreateAsync(options);

                logger.LogInformation("[SUBSCRIPTIONS] Stripe portal session created: {SessionId}", session.Id);

                return Results.Ok(new
                {
                    success = true,
                    portalUrl = session.Url
                });
            }
            catch (StripeException ex)
            {
                logger.LogError(ex, "[SUBSCRIPTIONS] Stripe error creating portal session");
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 400,
                    title: "Stripe Error");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[SUBSCRIPTIONS] Error creating portal session");
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Error creating portal session");
            }
        })
        .RequireAuthorization()
        .WithName("CreatePortalSession")
        .WithSummary("Create Stripe customer portal session")
        .WithDescription("Creates Stripe customer portal session for subscription management")
        .Produces(200)
        .Produces(400)
        .Produces(401)
        .Produces(404)
        .Produces(500);

        // ==========================================================================
        // ENDPOINT 9: POST /api/subscriptions/resume
        // Resume cancelled subscription
        // ==========================================================================
        group.MapPost("/resume", async (
            [FromBody] ResumeSubscriptionRequest request,
            ClaimsPrincipal user,
            [FromServices] ISubscriptionService subscriptionService,
            [FromServices] ILogger<Program> logger) =>
        {
            try
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    logger.LogWarning("[SUBSCRIPTIONS] Unauthorized resume attempt");
                    return Results.Unauthorized();
                }

                logger.LogInformation("[SUBSCRIPTIONS] Resuming subscription {SubscriptionId}", request.SubscriptionId);

                var success = await subscriptionService.ReactivateSubscriptionAsync(request.SubscriptionId);

                if (!success)
                {
                    logger.LogWarning("[SUBSCRIPTIONS] Failed to resume subscription {SubscriptionId}", request.SubscriptionId);
                    return Results.BadRequest(new { success = false, error = "Failed to resume subscription" });
                }

                logger.LogInformation("[SUBSCRIPTIONS] Subscription {SubscriptionId} resumed successfully", request.SubscriptionId);
                return Results.Ok(new { success = true, message = "Subscription resumed successfully" });
            }
            catch (NotFoundException ex)
            {
                logger.LogWarning(ex, "[SUBSCRIPTIONS] Subscription not found");
                return Results.NotFound(new { success = false, error = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[SUBSCRIPTIONS] Error resuming subscription");
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Error resuming subscription");
            }
        })
        .RequireAuthorization()
        .WithName("ResumeSubscription")
        .WithSummary("Resume cancelled subscription")
        .WithDescription("Resume a subscription that was cancelled but still active until period end")
        .Produces(200)
        .Produces(400)
        .Produces(401)
        .Produces(404)
        .Produces(500);
    }
}

// ==========================================================================
// REQUEST/RESPONSE DTOs
// ==========================================================================

public record CreateSubscriptionRequest(
    Guid PlanId,
    string BillingInterval, // "monthly" or "yearly"
    string? StripeSubscriptionId = null);

public record CancelSubscriptionRequest(
    Guid SubscriptionId,
    string? Reason,
    string? Feedback);

public record UpgradeSubscriptionRequest(
    Guid SubscriptionId,
    Guid NewPlanId);

public record DowngradeSubscriptionRequest(
    Guid SubscriptionId,
    Guid NewPlanId);

public record CreateCheckoutSessionRequest(
    Guid PlanId,
    string BillingInterval, // "monthly" or "yearly"
    string ReturnUrl);

public record CreatePortalSessionRequest(
    string ReturnUrl);

public record ResumeSubscriptionRequest(
    Guid SubscriptionId);

// Custom exception types
public class BusinessException : Exception
{
    public BusinessException(string message) : base(message) { }
}

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}
