using InsightLearn.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace InsightLearn.Application.Endpoints;

public static class StripeWebhookEndpoints
{
    public static void MapStripeWebhookEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/webhooks/stripe")
            .WithTags("Stripe Webhooks")
            .AllowAnonymous(); // Webhooks are called by Stripe servers, not authenticated users

        // POST /api/webhooks/stripe/subscription-created - Handle new subscription
        group.MapPost("/subscription-created", async (
            [FromBody] SubscriptionCreatedWebhook webhook,
            [FromServices] ISubscriptionService subscriptionService,
            [FromServices] ILogger<Program> logger) =>
        {
            try
            {
                logger.LogInformation($"Received subscription.created webhook for {webhook.StripeSubscriptionId}");

                await subscriptionService.HandleSubscriptionCreatedAsync(
                    webhook.StripeSubscriptionId,
                    webhook.UserId,
                    webhook.PlanId);

                return Results.Ok(new { received = true });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error processing subscription.created webhook: {webhook.StripeSubscriptionId}");
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Error processing subscription.created webhook");
            }
        })
        .WithName("StripeSubscriptionCreated")
        .Produces(200)
        .Produces(500);

        // POST /api/webhooks/stripe/subscription-updated - Handle subscription renewal/update
        group.MapPost("/subscription-updated", async (
            [FromBody] SubscriptionUpdatedWebhook webhook,
            [FromServices] ISubscriptionService subscriptionService,
            [FromServices] ILogger<Program> logger) =>
        {
            try
            {
                logger.LogInformation($"Received subscription.updated webhook for {webhook.StripeSubscriptionId}");

                await subscriptionService.HandleSubscriptionUpdatedAsync(
                    webhook.StripeSubscriptionId,
                    webhook.CurrentPeriodEnd,
                    webhook.Status);

                return Results.Ok(new { received = true });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error processing subscription.updated webhook: {webhook.StripeSubscriptionId}");
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Error processing subscription.updated webhook");
            }
        })
        .WithName("StripeSubscriptionUpdated")
        .Produces(200)
        .Produces(500);

        // POST /api/webhooks/stripe/subscription-deleted - Handle subscription cancellation
        group.MapPost("/subscription-deleted", async (
            [FromBody] SubscriptionDeletedWebhook webhook,
            [FromServices] ISubscriptionService subscriptionService,
            [FromServices] ILogger<Program> logger) =>
        {
            try
            {
                logger.LogInformation($"Received subscription.deleted webhook for {webhook.StripeSubscriptionId}");

                await subscriptionService.HandleSubscriptionCancelledAsync(webhook.StripeSubscriptionId);

                return Results.Ok(new { received = true });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error processing subscription.deleted webhook: {webhook.StripeSubscriptionId}");
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Error processing subscription.deleted webhook");
            }
        })
        .WithName("StripeSubscriptionDeleted")
        .Produces(200)
        .Produces(500);

        // POST /api/webhooks/stripe/invoice-paid - Create revenue record
        group.MapPost("/invoice-paid", async (
            [FromBody] InvoicePaidWebhook webhook,
            [FromServices] ISubscriptionService subscriptionService,
            [FromServices] ILogger<Program> logger) =>
        {
            try
            {
                logger.LogInformation($"Received invoice.paid webhook for {webhook.StripeInvoiceId}");

                await subscriptionService.HandleInvoicePaidAsync(
                    webhook.StripeInvoiceId,
                    webhook.StripeSubscriptionId,
                    webhook.Amount);

                return Results.Ok(new { received = true });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error processing invoice.paid webhook: {webhook.StripeInvoiceId}");
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Error processing invoice.paid webhook");
            }
        })
        .WithName("StripeInvoicePaid")
        .Produces(200)
        .Produces(500);

        // POST /api/webhooks/stripe/invoice-payment-failed - Mark subscription past_due
        group.MapPost("/invoice-payment-failed", async (
            [FromBody] InvoicePaymentFailedWebhook webhook,
            [FromServices] ISubscriptionService subscriptionService,
            [FromServices] ILogger<Program> logger) =>
        {
            try
            {
                logger.LogInformation($"Received invoice.payment_failed webhook for {webhook.StripeInvoiceId}");

                await subscriptionService.HandleInvoicePaymentFailedAsync(
                    webhook.StripeInvoiceId,
                    webhook.StripeSubscriptionId,
                    webhook.FailureReason ?? "Unknown failure");

                return Results.Ok(new { received = true });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error processing invoice.payment_failed webhook: {webhook.StripeInvoiceId}");
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Error processing invoice.payment_failed webhook");
            }
        })
        .WithName("StripeInvoicePaymentFailed")
        .Produces(200)
        .Produces(500);
    }
}

// DTOs for Stripe Webhooks
public record SubscriptionCreatedWebhook(
    string StripeSubscriptionId,
    Guid UserId,
    Guid PlanId);

public record SubscriptionUpdatedWebhook(
    string StripeSubscriptionId,
    DateTime CurrentPeriodEnd,
    string Status);

public record SubscriptionDeletedWebhook(
    string StripeSubscriptionId);

public record InvoicePaidWebhook(
    string StripeInvoiceId,
    string StripeSubscriptionId,
    decimal Amount);

public record InvoicePaymentFailedWebhook(
    string StripeInvoiceId,
    string StripeSubscriptionId,
    string? FailureReason);
