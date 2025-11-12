using InsightLearn.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InsightLearn.Application.Endpoints;

public static class EngagementEndpoints
{
    public static void MapEngagementEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/engagement")
            .WithTags("Engagement")
            .RequireAuthorization();

        // POST /api/engagement/record - Record user engagement
        group.MapPost("/record", async (
            [FromBody] RecordEngagementRequest request,
            ClaimsPrincipal user,
            HttpContext context,
            [FromServices] IEngagementTrackingService engagementService) =>
        {
            try
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Results.Unauthorized();
                }

                var ipAddress = context.Connection.RemoteIpAddress?.ToString();
                var userAgent = context.Request.Headers["User-Agent"].ToString();

                var engagement = await engagementService.RecordEngagementAsync(
                    userId,
                    request.CourseId,
                    request.EngagementType,
                    request.DurationMinutes,
                    ipAddress,
                    userAgent,
                    request.DeviceFingerprint,
                    request.Metadata);

                if (engagement == null)
                    return Results.BadRequest(new { error = "Failed to record engagement" });

                return Results.Ok(new
                {
                    engagement.Id,
                    engagement.ValidationScore,
                    engagement.CountsForPayout,
                    message = engagement.CountsForPayout
                        ? "Engagement recorded and validated"
                        : "Engagement recorded but did not meet validation threshold"
                });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Error recording engagement");
            }
        })
        .WithName("RecordEngagement")
        .Produces(200)
        .Produces(400)
        .Produces(401)
        .Produces(500);

        // GET /api/engagement/my-analytics - Get current user's engagement analytics
        group.MapGet("/my-analytics", async (
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            ClaimsPrincipal user,
            [FromServices] IEngagementTrackingService engagementService) =>
        {
            try
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Results.Unauthorized();
                }

                var start = startDate ?? DateTime.UtcNow.AddMonths(-1);
                var end = endDate ?? DateTime.UtcNow;

                var analytics = await engagementService.GetUserEngagementAnalyticsAsync(userId, start, end);
                return Results.Ok(analytics);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Error retrieving engagement analytics");
            }
        })
        .WithName("GetMyEngagementAnalytics")
        .Produces<UserEngagementAnalytics>(200)
        .Produces(401)
        .Produces(500);

        // GET /api/engagement/course/{courseId}/analytics - Get course engagement analytics (Instructor/Admin only)
        group.MapGet("/course/{courseId:guid}/analytics", async (
            Guid courseId,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromServices] IEngagementTrackingService engagementService) =>
        {
            try
            {
                var start = startDate ?? DateTime.UtcNow.AddMonths(-1);
                var end = endDate ?? DateTime.UtcNow;

                var analytics = await engagementService.GetCourseEngagementAnalyticsAsync(courseId, start, end);
                return Results.Ok(analytics);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Error retrieving course engagement analytics");
            }
        })
        .RequireAuthorization(policy => policy.RequireRole("Instructor", "Admin"))
        .WithName("GetCourseEngagementAnalytics")
        .Produces<CourseEngagementAnalytics>(200)
        .Produces(401)
        .Produces(403)
        .Produces(500);

        // GET /api/engagement/instructor/{instructorId}/breakdown - Get instructor engagement breakdown (Admin only)
        group.MapGet("/instructor/{instructorId:guid}/breakdown", async (
            Guid instructorId,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromServices] IEngagementTrackingService engagementService) =>
        {
            try
            {
                var start = startDate ?? DateTime.UtcNow.AddMonths(-1);
                var end = endDate ?? DateTime.UtcNow;

                var breakdown = await engagementService.GetInstructorEngagementBreakdownAsync(instructorId, start, end);
                return Results.Ok(breakdown);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Error retrieving instructor engagement breakdown");
            }
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithName("GetInstructorEngagementBreakdown")
        .Produces<InstructorEngagementBreakdown>(200)
        .Produces(401)
        .Produces(403)
        .Produces(500);

        // POST /api/engagement/{id}/mark-fraudulent - Mark engagement as fraudulent (Admin only)
        group.MapPost("/{id:guid}/mark-fraudulent", async (
            Guid id,
            [FromBody] MarkFraudulentRequest request,
            [FromServices] IEngagementTrackingService engagementService) =>
        {
            try
            {
                var success = await engagementService.MarkEngagementAsFraudulentAsync(id, request.Reason);

                if (!success)
                    return Results.NotFound(new { error = "Engagement not found" });

                return Results.Ok(new { success = true, message = "Engagement marked as fraudulent" });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Error marking engagement as fraudulent");
            }
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithName("MarkEngagementFraudulent")
        .Produces(200)
        .Produces(401)
        .Produces(403)
        .Produces(404)
        .Produces(500);

        // POST /api/engagement/validate-pending - Validate pending engagements (Background job - Admin only)
        group.MapPost("/validate-pending", async (
            [FromServices] IEngagementTrackingService engagementService,
            [FromQuery] int batchSize = 100) =>
        {
            try
            {
                var validatedCount = await engagementService.ValidatePendingEngagementsAsync(null, batchSize);
                return Results.Ok(new
                {
                    success = true,
                    validatedCount,
                    message = $"Validated {validatedCount} pending engagements"
                });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Error validating pending engagements");
            }
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithName("ValidatePendingEngagements")
        .Produces(200)
        .Produces(401)
        .Produces(403)
        .Produces(500);
    }
}

// DTOs
public record RecordEngagementRequest(
    Guid CourseId,
    string EngagementType,
    int DurationMinutes,
    string? DeviceFingerprint = null,
    Dictionary<string, object>? Metadata = null);

public record MarkFraudulentRequest(string Reason);
