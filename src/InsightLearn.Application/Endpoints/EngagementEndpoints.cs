using InsightLearn.Core.Interfaces;
using InsightLearn.Core.DTOs.Engagement;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InsightLearn.Application.Endpoints;

/// <summary>
/// Engagement Tracking API Endpoints (6 endpoints)
/// Tasks: T10 - Engagement API Endpoints
/// Version: v2.1.0-dev - Added lesson/course progress endpoints
/// </summary>
public static class EngagementEndpoints
{
    public static void MapEngagementEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/engagement")
            .WithTags("Engagement");

        // ==========================================================================
        // ENDPOINT 10: POST /api/engagement/track
        // Record engagement event (video watch, quiz, assignment, etc.)
        // ==========================================================================
        group.MapPost("/track", async (
            [FromBody] TrackEngagementDto dto,
            ClaimsPrincipal user,
            HttpContext context,
            [FromServices] IEngagementTrackingService engagementService,
            [FromServices] ILogger<Program> logger) =>
        {
            try
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    logger.LogWarning("[ENGAGEMENT] Unauthorized track attempt");
                    return Results.Unauthorized();
                }

                var ipAddress = context.Connection.RemoteIpAddress?.ToString();
                var userAgent = context.Request.Headers["User-Agent"].ToString();

                logger.LogInformation("[ENGAGEMENT] User {UserId} tracking engagement: lesson {LessonId}, type {Type}, duration {Duration}s",
                    userId, dto.LessonId, dto.EngagementType, dto.TimeSpentSeconds);

                // Convert LessonId to CourseId (assume they're the same for simplicity - in real scenario, lookup Course from Lesson)
                var courseId = dto.LessonId; // TODO: Implement proper Lesson → Course lookup
                var durationMinutes = dto.TimeSpentSeconds / 60;

                var engagement = await engagementService.RecordEngagementAsync(
                    userId,
                    courseId,
                    dto.EngagementType,
                    durationMinutes,
                    ipAddress,
                    userAgent,
                    dto.SessionId,
                    dto.Metadata);

                if (engagement == null)
                {
                    logger.LogWarning("[ENGAGEMENT] Failed to record engagement for user {UserId}", userId);
                    return Results.BadRequest(new { success = false, error = "Failed to record engagement" });
                }

                logger.LogInformation("[ENGAGEMENT] Engagement {EngagementId} recorded, validation score: {Score}",
                    engagement.Id, engagement.ValidationScore);

                return Results.Ok(new
                {
                    success = true,
                    engagement = new
                    {
                        engagement.Id,
                        engagement.ValidationScore,
                        engagement.CountsForPayout,
                        message = engagement.CountsForPayout
                            ? "Engagement recorded and validated"
                            : "Engagement recorded but did not meet validation threshold"
                    }
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[ENGAGEMENT] Error recording engagement");
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Error recording engagement");
            }
        })
        .RequireAuthorization()
        .WithName("TrackEngagement")
        .WithSummary("Record engagement event")
        .WithDescription("Track user engagement (video watch, quiz, assignment, etc.) with anti-fraud validation")
        .Produces(200)
        .Produces(400)
        .Produces(401)
        .Produces(500);

        // ==========================================================================
        // ENDPOINT 11: POST /api/engagement/video-progress
        // Update video progress (simplified version of track for video-specific data)
        // ==========================================================================
        group.MapPost("/video-progress", async (
            [FromBody] TrackVideoProgressDto dto,
            ClaimsPrincipal user,
            HttpContext context,
            [FromServices] IEngagementTrackingService engagementService,
            [FromServices] ILogger<Program> logger) =>
        {
            try
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    logger.LogWarning("[ENGAGEMENT] Unauthorized video progress track attempt");
                    return Results.Unauthorized();
                }

                var ipAddress = context.Connection.RemoteIpAddress?.ToString();
                var userAgent = context.Request.Headers["User-Agent"].ToString();

                var progressPercentage = (dto.CurrentTimestampSeconds * 100) / dto.TotalDurationSeconds;
                logger.LogInformation("[ENGAGEMENT] User {UserId} video progress: lesson {LessonId}, progress {Progress}%, current {Current}s",
                    userId, dto.LessonId, progressPercentage, dto.CurrentTimestampSeconds);

                // Convert to engagement tracking
                var courseId = dto.LessonId; // TODO: Implement proper Lesson → Course lookup
                var durationMinutes = dto.CurrentTimestampSeconds / 60;

                var metadata = new Dictionary<string, object>
                {
                    { "progressPercentage", progressPercentage },
                    { "currentTimestamp", dto.CurrentTimestampSeconds },
                    { "totalDuration", dto.TotalDurationSeconds },
                    { "playbackSpeed", dto.PlaybackSpeed ?? 1.0m },
                    { "tabActive", dto.TabActive ?? true }
                };

                var engagement = await engagementService.RecordEngagementAsync(
                    userId,
                    courseId,
                    "video_watch",
                    durationMinutes,
                    ipAddress,
                    userAgent,
                    null, // deviceFingerprint
                    metadata);

                logger.LogInformation("[ENGAGEMENT] Video progress tracked for user {UserId}", userId);

                return Results.NoContent(); // 204 No Content - success with no response body
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[ENGAGEMENT] Error tracking video progress");
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Error tracking video progress");
            }
        })
        .RequireAuthorization()
        .WithName("TrackVideoProgress")
        .WithSummary("Update video progress")
        .WithDescription("Track video watch progress for engagement analytics")
        .Produces(204)
        .Produces(401)
        .Produces(500);

        // ==========================================================================
        // ENDPOINT 12: GET /api/engagement/my-stats
        // Get current user's engagement statistics
        // ==========================================================================
        group.MapGet("/my-stats", async (
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            ClaimsPrincipal user,
            [FromServices] IEngagementTrackingService engagementService,
            [FromServices] ILogger<Program> logger) =>
        {
            try
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    logger.LogWarning("[ENGAGEMENT] Unauthorized my-stats attempt");
                    return Results.Unauthorized();
                }

                var start = startDate ?? DateTime.UtcNow.AddMonths(-1);
                var end = endDate ?? DateTime.UtcNow;

                logger.LogInformation("[ENGAGEMENT] Fetching stats for user {UserId}, period {Start} to {End}",
                    userId, start, end);

                var analytics = await engagementService.GetUserEngagementAnalyticsAsync(userId, start, end);

                logger.LogInformation("[ENGAGEMENT] Retrieved stats for user {UserId}: {TotalMinutes} minutes, {Count} engagements",
                    userId, analytics.TotalEngagementMinutes, analytics.TotalEngagements);

                return Results.Ok(new
                {
                    success = true,
                    data = analytics
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[ENGAGEMENT] Error retrieving user engagement stats");
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Error retrieving engagement statistics");
            }
        })
        .RequireAuthorization()
        .WithName("GetMyEngagementStats")
        .WithSummary("Get user engagement statistics")
        .WithDescription("Returns engagement analytics for authenticated user (total time, validation score, etc.)")
        .Produces<UserEngagementAnalytics>(200)
        .Produces(401)
        .Produces(500);

        // ==========================================================================
        // ENDPOINT 13: GET /api/engagement/lesson/{lessonId}/position
        // Get last video position for a lesson (v2.1.0-dev)
        // ==========================================================================
        group.MapGet("/lesson/{lessonId:guid}/position", async (
            Guid lessonId,
            ClaimsPrincipal user,
            [FromServices] ILogger<Program> logger) =>
        {
            try
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    logger.LogWarning("[ENGAGEMENT] Unauthorized position request");
                    return Results.Unauthorized();
                }

                logger.LogInformation("[ENGAGEMENT] Getting position for user {UserId}, lesson {LessonId}", userId, lessonId);

                // TODO: Implement actual last position retrieval from database
                // For now, return placeholder to prevent 404 errors
                return Results.Ok(new
                {
                    success = true,
                    data = new
                    {
                        lessonId,
                        lastPositionSeconds = 0,
                        totalDurationSeconds = 0,
                        progressPercentage = 0,
                        lastUpdated = DateTime.UtcNow
                    }
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[ENGAGEMENT] Error getting lesson position");
                return Results.Problem(detail: ex.Message, statusCode: 500, title: "Error getting lesson position");
            }
        })
        .RequireAuthorization()
        .WithName("GetLessonPosition")
        .WithSummary("Get last video position for a lesson")
        .WithDescription("Returns the user's last watched position in a video lesson")
        .Produces(200)
        .Produces(401)
        .Produces(500);

        // ==========================================================================
        // ENDPOINT 14: GET /api/engagement/lesson/{lessonId}
        // Get progress for a specific lesson (v2.1.0-dev)
        // ==========================================================================
        group.MapGet("/lesson/{lessonId:guid}", async (
            Guid lessonId,
            ClaimsPrincipal user,
            [FromServices] ILogger<Program> logger) =>
        {
            try
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    logger.LogWarning("[ENGAGEMENT] Unauthorized lesson progress request");
                    return Results.Unauthorized();
                }

                logger.LogInformation("[ENGAGEMENT] Getting lesson progress for user {UserId}, lesson {LessonId}", userId, lessonId);

                // TODO: Implement actual lesson progress retrieval from database
                return Results.Ok(new
                {
                    success = true,
                    data = new
                    {
                        lessonId,
                        userId,
                        progressPercentage = 0,
                        isCompleted = false,
                        totalWatchTimeSeconds = 0,
                        lastWatchedAt = (DateTime?)null
                    }
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[ENGAGEMENT] Error getting lesson progress");
                return Results.Problem(detail: ex.Message, statusCode: 500, title: "Error getting lesson progress");
            }
        })
        .RequireAuthorization()
        .WithName("GetLessonProgress")
        .WithSummary("Get progress for a specific lesson")
        .WithDescription("Returns the user's progress for a specific lesson")
        .Produces(200)
        .Produces(401)
        .Produces(500);

        // ==========================================================================
        // ENDPOINT 15: GET /api/engagement/course/{courseId}
        // Get progress for all lessons in a course (v2.1.0-dev)
        // ==========================================================================
        group.MapGet("/course/{courseId:guid}", async (
            Guid courseId,
            ClaimsPrincipal user,
            [FromServices] ILogger<Program> logger) =>
        {
            try
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    logger.LogWarning("[ENGAGEMENT] Unauthorized course progress request");
                    return Results.Unauthorized();
                }

                logger.LogInformation("[ENGAGEMENT] Getting course progress for user {UserId}, course {CourseId}", userId, courseId);

                // TODO: Implement actual course progress retrieval from database
                return Results.Ok(new
                {
                    success = true,
                    data = new List<object>() // Empty list for now - will be populated when lessons exist
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[ENGAGEMENT] Error getting course progress");
                return Results.Problem(detail: ex.Message, statusCode: 500, title: "Error getting course progress");
            }
        })
        .RequireAuthorization()
        .WithName("GetCourseProgress")
        .WithSummary("Get progress for all lessons in a course")
        .WithDescription("Returns the user's progress for all lessons in a course")
        .Produces(200)
        .Produces(401)
        .Produces(500);
    }
}
