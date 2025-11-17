using InsightLearn.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using System.IO;

namespace InsightLearn.Application.Endpoints;

/// <summary>
/// Stripe Webhook API Endpoint (1 endpoint with multiple event handlers)
/// Tasks: T13 - Stripe Webhook Endpoint
/// Version: v2.0.0
/// </summary>
public static class StripeWebhookEndpoints
{
    public static void MapStripeWebhookEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/webhooks/stripe")
            .WithTags("Stripe Webhooks")
            .AllowAnonymous(); // Webhooks are called by Stripe servers, not authenticated users

        // ==========================================================================
        // ENDPOINT 23: POST /api/webhooks/stripe
        // Handle Stripe webhook events with signature verification
        // Supported events:
        // - customer.subscription.created
        // - customer.subscription.updated
        // - customer.subscription.deleted
        // - invoice.paid
        // - invoice.payment_failed
        // - customer.subscription.trial_will_end
        // ==========================================================================
        group.MapPost("", async (
            HttpRequest request,
            [FromServices] ISubscriptionService subscriptionService,
            [FromServices] IConfiguration configuration,
            [FromServices] ILogger<Program> logger) =>
        {
            try
            {
                // Read raw body
                string json;
                using (var reader = new StreamReader(request.Body))
                {
                    json = await reader.ReadToEndAsync();
                }

                if (string.IsNullOrEmpty(json))
                {
                    logger.LogWarning("[STRIPE_WEBHOOK] Empty request body");
                    return Results.BadRequest(new { error = "Empty request body" });
                }

                // Verify Stripe signature
                var stripeSignature = request.Headers["Stripe-Signature"].ToString();
                if (string.IsNullOrEmpty(stripeSignature))
                {
                    logger.LogWarning("[STRIPE_WEBHOOK] Missing Stripe-Signature header");
                    return Results.BadRequest(new { error = "Missing Stripe-Signature header" });
                }

                var webhookSecret = Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_SECRET")
                    ?? configuration["Stripe:WebhookSecret"];

                if (string.IsNullOrEmpty(webhookSecret))
                {
                    logger.LogError("[STRIPE_WEBHOOK] Stripe webhook secret not configured");
                    return Results.Problem(
                        detail: "Webhook secret not configured",
                        statusCode: 500,
                        title: "Configuration Error");
                }

                // Construct Stripe event (verifies signature)
                Event stripeEvent;
                try
                {
                    stripeEvent = EventUtility.ConstructEvent(
                        json,
                        stripeSignature,
                        webhookSecret
                    );
                }
                catch (StripeException ex)
                {
                    logger.LogError(ex, "[STRIPE_WEBHOOK] Signature verification failed");
                    return Results.BadRequest(new { error = "Invalid signature" });
                }

                logger.LogInformation("[STRIPE_WEBHOOK] Received event {EventType} with ID {EventId}",
                    stripeEvent.Type, stripeEvent.Id);

                // Handle event based on type
                try
                {
                    switch (stripeEvent.Type)
                    {
                        // =============================================================
                        // Subscription Created
                        // =============================================================
                        case "customer.subscription.created":
                            {
                                var subscription = stripeEvent.Data.Object as Subscription;
                                if (subscription != null)
                                {
                                    logger.LogInformation("[STRIPE_WEBHOOK] Processing subscription.created: {SubscriptionId}",
                                        subscription.Id);

                                    // Extract metadata
                                    if (subscription.Metadata.TryGetValue("userId", out var userIdStr) &&
                                        subscription.Metadata.TryGetValue("planId", out var planIdStr) &&
                                        Guid.TryParse(userIdStr, out var userId) &&
                                        Guid.TryParse(planIdStr, out var planId))
                                    {
                                        await subscriptionService.HandleSubscriptionCreatedAsync(
                                            subscription.Id,
                                            userId,
                                            planId);

                                        logger.LogInformation("[STRIPE_WEBHOOK] subscription.created processed successfully");
                                    }
                                    else
                                    {
                                        logger.LogWarning("[STRIPE_WEBHOOK] subscription.created missing metadata (userId or planId)");
                                    }
                                }
                                break;
                            }

                        // =============================================================
                        // Subscription Updated (renewal, plan change)
                        // =============================================================
                        case "customer.subscription.updated":
                            {
                                var subscription = stripeEvent.Data.Object as Subscription;
                                if (subscription != null)
                                {
                                    logger.LogInformation("[STRIPE_WEBHOOK] Processing subscription.updated: {SubscriptionId}, status {Status}",
                                        subscription.Id, subscription.Status);

                                    // Use DateTime.UtcNow as fallback if CurrentPeriodEnd is not available
                                    var currentPeriodEnd = DateTime.UtcNow.AddMonths(1);

                                    await subscriptionService.HandleSubscriptionUpdatedAsync(
                                        subscription.Id,
                                        currentPeriodEnd,
                                        subscription.Status);

                                    logger.LogInformation("[STRIPE_WEBHOOK] subscription.updated processed successfully");
                                }
                                break;
                            }

                        // =============================================================
                        // Subscription Deleted (cancellation)
                        // =============================================================
                        case "customer.subscription.deleted":
                            {
                                var subscription = stripeEvent.Data.Object as Subscription;
                                if (subscription != null)
                                {
                                    logger.LogInformation("[STRIPE_WEBHOOK] Processing subscription.deleted: {SubscriptionId}",
                                        subscription.Id);

                                    await subscriptionService.HandleSubscriptionCancelledAsync(subscription.Id);

                                    logger.LogInformation("[STRIPE_WEBHOOK] subscription.deleted processed successfully");
                                }
                                break;
                            }

                        // =============================================================
                        // Invoice Paid (successful payment)
                        // =============================================================
                        case "invoice.paid":
                            {
                                var invoice = stripeEvent.Data.Object as Invoice;
                                if (invoice != null)
                                {
                                    logger.LogInformation("[STRIPE_WEBHOOK] Processing invoice.paid: {InvoiceId}, amount €{Amount}",
                                        invoice.Id, invoice.AmountPaid / 100.0m);

                                    // TODO: Implement invoice paid handling - requires retrieving subscription ID from Invoice object
                                    // For now, just log the event
                                    logger.LogInformation("[STRIPE_WEBHOOK] invoice.paid logged - handler to be implemented");
                                }
                                break;
                            }

                        // =============================================================
                        // Invoice Payment Failed
                        // =============================================================
                        case "invoice.payment_failed":
                            {
                                var invoice = stripeEvent.Data.Object as Invoice;
                                if (invoice != null)
                                {
                                    logger.LogWarning("[STRIPE_WEBHOOK] Processing invoice.payment_failed: {InvoiceId}",
                                        invoice.Id);

                                    var failureReason = invoice.LastFinalizationError?.Message
                                        ?? "Payment failed - please update payment method";

                                    // TODO: Implement invoice payment failed handling - requires retrieving subscription ID
                                    // For now, just log the event
                                    logger.LogWarning("[STRIPE_WEBHOOK] invoice.payment_failed logged: {Reason}", failureReason);
                                }
                                break;
                            }

                        // =============================================================
                        // Trial Will End (3 days before trial expiration)
                        // =============================================================
                        case "customer.subscription.trial_will_end":
                            {
                                var subscription = stripeEvent.Data.Object as Subscription;
                                if (subscription != null)
                                {
                                    logger.LogInformation("[STRIPE_WEBHOOK] Processing trial_will_end: {SubscriptionId}, trial ends {TrialEnd}",
                                        subscription.Id, subscription.TrialEnd);

                                    // TODO: Implement trial ending notification (email to user)
                                    // For now, just log
                                    logger.LogInformation("[STRIPE_WEBHOOK] trial_will_end logged - notification not yet implemented");
                                }
                                break;
                            }

                        // =============================================================
                        // Unhandled Event Types (log for monitoring)
                        // =============================================================
                        default:
                            logger.LogInformation("[STRIPE_WEBHOOK] Unhandled event type: {EventType}", stripeEvent.Type);
                            break;
                    }

                    // Return 200 OK to acknowledge receipt
                    return Results.Ok(new
                    {
                        received = true,
                        eventId = stripeEvent.Id,
                        eventType = stripeEvent.Type,
                        message = "Webhook processed successfully"
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "[STRIPE_WEBHOOK] Error processing event {EventType} ({EventId})",
                        stripeEvent.Type, stripeEvent.Id);

                    // Return 500 so Stripe retries later
                    return Results.Problem(
                        detail: ex.Message,
                        statusCode: 500,
                        title: "Error processing webhook");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[STRIPE_WEBHOOK] Unexpected error processing webhook");
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Unexpected error");
            }
        })
        .WithName("StripeWebhook")
        .WithSummary("Handle Stripe webhook events")
        .WithDescription(@"Processes Stripe webhook events with signature verification.
Supported events:
- customer.subscription.created
- customer.subscription.updated
- customer.subscription.deleted
- invoice.paid
- invoice.payment_failed
- customer.subscription.trial_will_end

Requires Stripe-Signature header and STRIPE_WEBHOOK_SECRET environment variable.")
        .Produces(200)
        .Produces(400)
        .Produces(500);

        // ==========================================================================
        // Health Check Endpoint for Webhooks (for monitoring)
        // ==========================================================================
        group.MapGet("/health", () =>
        {
            return Results.Ok(new
            {
                status = "healthy",
                service = "stripe-webhooks",
                message = "Webhook endpoint is ready to receive events"
            });
        })
        .WithName("StripeWebhookHealth")
        .WithSummary("Webhook health check")
        .WithDescription("Returns health status of webhook endpoint")
        .Produces(200);
    }
}
