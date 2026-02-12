using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using System.Text.Json;

namespace InsightLearn.Application.Endpoints;

/// <summary>
/// Video Render Endpoints - Proxy to Remotion renderer microservice
/// v2.4.5-dev: Burned-in subtitle video export
/// </summary>
public static class VideoRenderEndpoints
{
    private static readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromMinutes(30) // Long timeout for rendering
    };

    public static void MapVideoRenderEndpoints(this WebApplication app)
    {
        var remotionUrl = app.Configuration["Remotion:BaseUrl"]
            ?? Environment.GetEnvironmentVariable("REMOTION_URL")
            ?? "http://remotion-renderer-service:3000";

        Console.WriteLine($"[API] Remotion renderer URL: {remotionUrl}");

        // POST /api/videos/render - Start video render with burned-in captions
        app.MapPost("/api/videos/render", async (
            [FromBody] RenderVideoRequest request,
            [FromServices] ILogger<Program> logger) =>
        {
            try
            {
                logger.LogInformation("[RENDER] Starting render for lesson {LessonId}, lang {Lang}",
                    request.LessonId, request.TargetLanguage);

                // Forward to Remotion service
                var response = await _httpClient.PostAsJsonAsync(
                    $"{remotionUrl}/api/render",
                    new
                    {
                        lessonId = request.LessonId.ToString(),
                        targetLanguage = request.TargetLanguage,
                        videoUrl = request.VideoUrl,
                        compositionId = request.Resolution == "720p"
                            ? "VideoWithCaptions720p"
                            : "VideoWithCaptions",
                        fps = request.Fps ?? 30
                    });

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    logger.LogError("[RENDER] Remotion service error: {Error}", errorContent);
                    return Results.Problem($"Render service error: {errorContent}");
                }

                var result = await response.Content.ReadFromJsonAsync<RenderJobResponse>();
                return Results.Accepted($"/api/videos/render/{result?.JobId}/status", result);
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "[RENDER] Cannot reach Remotion service");
                return Results.Problem("Render service unavailable. Please try again later.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[RENDER] Unexpected error");
                return Results.Problem($"Error: {ex.Message}");
            }
        })
        .WithName("StartVideoRender")
        .WithTags("VideoRender")
        .Produces<RenderJobResponse>(202)
        .Produces(400)
        .Produces(500)
        .WithDescription("Start rendering a video with burned-in subtitles");

        // GET /api/videos/render/{jobId}/status - Get render job status
        app.MapGet("/api/videos/render/{jobId}/status", async (
            string jobId,
            [FromServices] ILogger<Program> logger) =>
        {
            try
            {
                var response = await _httpClient.GetAsync($"{remotionUrl}/api/render/{jobId}/status");

                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                        return Results.NotFound($"Job {jobId} not found");

                    return Results.Problem("Error checking job status");
                }

                var result = await response.Content.ReadFromJsonAsync<RenderJobStatusResponse>();
                return Results.Ok(result);
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "[RENDER] Cannot reach Remotion service for status");
                return Results.Problem("Render service unavailable");
            }
        })
        .WithName("GetRenderJobStatus")
        .WithTags("VideoRender")
        .Produces<RenderJobStatusResponse>(200)
        .Produces(404)
        .Produces(500);

        // GET /api/videos/render/{fileId}/download - Download rendered video
        app.MapGet("/api/videos/render/{fileId}/download", async (
            string fileId,
            HttpContext httpContext,
            [FromServices] ILogger<Program> logger) =>
        {
            try
            {
                // Stream the video from Remotion service
                var response = await _httpClient.GetAsync(
                    $"{remotionUrl}/api/render/{fileId}/download",
                    HttpCompletionOption.ResponseHeadersRead);

                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                        return Results.NotFound("Video file not found");

                    return Results.Problem("Error downloading video");
                }

                httpContext.Response.ContentType = "video/mp4";
                httpContext.Response.Headers["Content-Disposition"] =
                    $"attachment; filename=\"rendered_video_{fileId}.mp4\"";

                if (response.Content.Headers.ContentLength.HasValue)
                    httpContext.Response.ContentLength = response.Content.Headers.ContentLength;

                await response.Content.CopyToAsync(httpContext.Response.Body);
                return Results.Empty;
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "[RENDER] Cannot download from Remotion service");
                return Results.Problem("Render service unavailable");
            }
        })
        .WithName("DownloadRenderedVideo")
        .WithTags("VideoRender")
        .Produces(200)
        .Produces(404)
        .Produces(500);

        // GET /api/videos/render/jobs - List all render jobs
        app.MapGet("/api/videos/render/jobs", async (
            [FromServices] ILogger<Program> logger) =>
        {
            try
            {
                var response = await _httpClient.GetAsync($"{remotionUrl}/api/render/jobs");

                if (!response.IsSuccessStatusCode)
                    return Results.Problem("Error fetching render jobs");

                var result = await response.Content.ReadFromJsonAsync<RenderJobsListResponse>();
                return Results.Ok(result);
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "[RENDER] Cannot reach Remotion service");
                return Results.Problem("Render service unavailable");
            }
        })
        .WithName("ListRenderJobs")
        .WithTags("VideoRender")
        .Produces<RenderJobsListResponse>(200)
        .Produces(500);

        // DELETE /api/videos/render/{jobId} - Cancel/cleanup render job
        app.MapDelete("/api/videos/render/{jobId}", async (
            string jobId,
            [FromServices] ILogger<Program> logger) =>
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{remotionUrl}/api/render/{jobId}");

                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                        return Results.NotFound($"Job {jobId} not found");

                    return Results.Problem("Error canceling job");
                }

                return Results.Ok(new { message = $"Job {jobId} canceled" });
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "[RENDER] Cannot reach Remotion service");
                return Results.Problem("Render service unavailable");
            }
        })
        .WithName("CancelRenderJob")
        .WithTags("VideoRender")
        .Produces(200)
        .Produces(404)
        .Produces(500);

        // GET /api/videos/render/health - Check Remotion service health
        app.MapGet("/api/videos/render/health", async (
            [FromServices] ILogger<Program> logger) =>
        {
            try
            {
                var response = await _httpClient.GetAsync($"{remotionUrl}/health/ready");
                var isHealthy = response.IsSuccessStatusCode;

                return Results.Ok(new
                {
                    service = "remotion-renderer",
                    status = isHealthy ? "healthy" : "unhealthy",
                    url = remotionUrl,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[RENDER] Health check failed");
                return Results.Ok(new
                {
                    service = "remotion-renderer",
                    status = "unreachable",
                    url = remotionUrl,
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        })
        .WithName("CheckRenderServiceHealth")
        .WithTags("VideoRender")
        .Produces(200);

        Console.WriteLine("[API] Video Render endpoints registered (6 endpoints)");
    }
}

// Request/Response DTOs for Video Render

/// <summary>
/// Request to start video rendering with burned-in captions
/// </summary>
public record RenderVideoRequest(
    Guid LessonId,
    string TargetLanguage,
    string VideoUrl,
    string? Resolution = "1080p",
    int? Fps = 30
);

/// <summary>
/// Response when render job is created
/// </summary>
public record RenderJobResponse(
    bool Success,
    string? JobId,
    string? Status,
    string? Message
);

/// <summary>
/// Response for render job status
/// </summary>
public record RenderJobStatusResponse(
    bool Success,
    RenderJobInfo? Job
);

/// <summary>
/// Detailed render job info
/// </summary>
public record RenderJobInfo(
    string Id,
    string LessonId,
    string TargetLanguage,
    string Status,
    int Progress,
    string? GridfsFileId,
    string? Error,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

/// <summary>
/// Response for listing all render jobs
/// </summary>
public record RenderJobsListResponse(
    bool Success,
    List<RenderJobInfo>? Jobs
);
