using InsightLearn.Application.DTOs;
using InsightLearn.Application.Interfaces;
using InsightLearn.Application.Services;
using InsightLearn.Infrastructure.Data;
using InsightLearn.Infrastructure.Repositories;
using InsightLearn.Infrastructure.Services;
using InsightLearn.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Net;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Get version from assembly (defined in Directory.Build.props)
var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.6.0.0";
var versionShort = version.Substring(0, version.LastIndexOf('.')) + "-dev"; // e.g., "1.6.0-dev"

// Load configuration from mounted config file
builder.Configuration.AddJsonFile("/app/config/appsettings.json", optional: true, reloadOnChange: true);

// Get configuration values
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("DefaultConnection not configured");

var ollamaUrl = builder.Configuration["Ollama:BaseUrl"]
    ?? builder.Configuration["Ollama:Url"]
    ?? "http://ollama-service.insightlearn.svc.cluster.local:11434";
var ollamaModel = builder.Configuration["Ollama:Model"] ?? "tinyllama";

// MongoDB connection string for video storage
var mongoConnectionString = builder.Configuration["MongoDb:ConnectionString"]
    ?? builder.Configuration.GetConnectionString("MongoDB")
    ?? "mongodb://admin:InsightLearn2024!SecureMongo@mongodb-service.insightlearn.svc.cluster.local:27017/insightlearn_videos?authSource=admin";

Console.WriteLine($"[CONFIG] Database: {connectionString}");
Console.WriteLine($"[CONFIG] Ollama URL: {ollamaUrl}");
Console.WriteLine($"[CONFIG] Ollama Model: {ollamaModel}");
Console.WriteLine($"[CONFIG] MongoDB: {mongoConnectionString.Replace("InsightLearn2024!SecureMongo", "***")}");

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure JSON serialization to use PascalCase (ASP.NET Core default)
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = null; // null = PascalCase
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configure Entity Framework with SQL Server
builder.Services.AddDbContext<InsightLearnDbContext>(options =>
{
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
        sqlOptions.CommandTimeout(120);
    });
});

// Register HttpClient for services
builder.Services.AddHttpClient();

// Register MemoryCache for endpoint caching
builder.Services.AddMemoryCache();

// Register Endpoint Management Services
builder.Services.AddScoped<ISystemEndpointRepository, SystemEndpointRepository>();
builder.Services.AddScoped<IEndpointService, EndpointService>();

// Register Ollama Service
builder.Services.AddScoped<IOllamaService>(sp =>
{
    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
    var logger = sp.GetRequiredService<ILogger<OllamaService>>();
    return new OllamaService(httpClient, ollamaUrl, ollamaModel, logger);
});

// Register Chatbot Service
builder.Services.AddScoped<IChatbotService>(sp =>
{
    var ollamaService = sp.GetRequiredService<IOllamaService>();
    var dbContext = sp.GetRequiredService<InsightLearnDbContext>();
    var logger = sp.GetRequiredService<ILogger<ChatbotService>>();
    return new ChatbotService(ollamaService, dbContext, logger, ollamaModel);
});

// Register MongoDB Video Storage Services
builder.Services.AddSingleton<IMongoVideoStorageService, MongoVideoStorageService>();
builder.Services.AddScoped<IVideoProcessingService, VideoProcessingService>();

Console.WriteLine("[CONFIG] MongoDB Video Storage Services registered");

// Health checks (simple - no additional packages needed)
builder.Services.AddHealthChecks();

var app = builder.Build();

// Apply database migrations on startup (Kubernetes best practice)
try
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<InsightLearnDbContext>();

    Console.WriteLine("[DATABASE] Checking database connection...");

    // Quick retry: 3 attempts with 2 second delay (6 seconds total max)
    bool connected = false;
    for (int i = 0; i < 3; i++)
    {
        try
        {
            connected = await dbContext.Database.CanConnectAsync();
            if (connected)
            {
                Console.WriteLine($"[DATABASE] ✓ Database connected successfully!");
                break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DATABASE] Connection attempt {i + 1}/3 failed: {ex.Message}");
        }

        if (!connected && i < 2)
        {
            Console.WriteLine($"[DATABASE] Retrying in 2 seconds...");
            await Task.Delay(2000);
        }
    }

    if (connected)
    {
        Console.WriteLine("[DATABASE] Applying pending migrations...");
        await dbContext.Database.MigrateAsync();
        Console.WriteLine("[DATABASE] ✓ Migrations completed!");
    }
    else
    {
        Console.WriteLine("[DATABASE] ⚠ Warning: Could not connect to database at startup");
        Console.WriteLine("[DATABASE] The API will start anyway - database will reconnect on first request");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"[DATABASE] ⚠ Warning: {ex.Message}");
    Console.WriteLine("[DATABASE] API starting without database connection - will retry on first request");
}

// Configure the HTTP request pipeline
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

// Health check endpoint
app.MapHealthChecks("/health");

app.MapControllers();

// Simple test endpoint
app.MapGet("/", () => new
{
    application = "InsightLearn API",
    version = versionShort,
    status = "running",
    timestamp = DateTime.UtcNow,
    environment = app.Environment.EnvironmentName,
    chatbot = "enabled",
    features = new[] { "mongodb-video-storage", "gridfs-compression", "courses-browse", "course-detail" }
});

// API info endpoint
app.MapGet("/api/info", () => Results.Ok(new
{
    name = "InsightLearn API",
    version = versionShort,
    assemblyVersion = version,
    status = "operational",
    timestamp = DateTime.UtcNow,
    features = new[] {
        "chatbot",
        "auth",
        "courses",
        "payments",
        "mongodb-video-storage",
        "gridfs-compression",
        "video-streaming",
        "browse-courses-page",
        "course-detail-page"
    }
}));

// CHATBOT API ENDPOINTS
app.MapPost("/api/chat/message", async (
    [FromBody] ChatMessageRequest request,
    [FromServices] IChatbotService chatbotService,
    [FromServices] ILogger<Program> logger,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        logger.LogInformation("[CHATBOT] ========== NEW CHAT REQUEST ==========");
        logger.LogInformation("[CHATBOT] Received message from session {SessionId}", request.SessionId);
        logger.LogInformation("[CHATBOT] Request Message: {Message}", request.Message);
        logger.LogInformation("[CHATBOT] Request Email: {Email}", request.Email);
        logger.LogInformation("[CHATBOT] Request CourseId: {CourseId}", request.CourseId);

        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = httpContext.Request.Headers["User-Agent"].ToString();

        logger.LogInformation("[CHATBOT] Client IP: {IP}", ipAddress);
        logger.LogInformation("[CHATBOT] User Agent: {UserAgent}", userAgent);

        var response = await chatbotService.ProcessMessageAsync(request, ipAddress, userAgent, cancellationToken);

        logger.LogInformation("[CHATBOT] Response generated in {ResponseTime}ms", response.ResponseTimeMs);

        // DETAILED LOGGING: Trace DTO before serialization
        logger.LogInformation("[CHATBOT] DTO Type: {Type}", response.GetType().FullName);
        logger.LogInformation("[CHATBOT] DTO Response property: {Response}", response.Response);
        logger.LogInformation("[CHATBOT] DTO SessionId property: {SessionId}", response.SessionId);
        logger.LogInformation("[CHATBOT] DTO AiModel property: {AiModel}", response.AiModel);

        // Serialize manually to see JSON format
        var jsonString = System.Text.Json.JsonSerializer.Serialize(response, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = null // PascalCase
        });
        logger.LogInformation("[CHATBOT] Serialized JSON (PascalCase): {Json}", jsonString);

        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[CHATBOT] Error processing message");
        return Results.Json(new ChatMessageResponse
        {
            Response = "Mi dispiace, si è verificato un errore. Riprova più tardi.",
            SessionId = request.SessionId ?? Guid.NewGuid().ToString(),
            Timestamp = DateTime.UtcNow,
            HasError = true,
            ErrorMessage = ex.Message
        }, statusCode: (int)HttpStatusCode.InternalServerError);
    }
})
.WithName("SendChatMessage")
.WithTags("Chatbot");

app.MapGet("/api/chat/history", async (
    [FromQuery] string sessionId,
    [FromQuery] int limit,
    [FromServices] IChatbotService chatbotService,
    [FromServices] ILogger<Program> logger) =>
{
    try
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return Results.BadRequest(new { error = "SessionId is required" });
        }

        logger.LogInformation("[CHATBOT] Fetching history for session {SessionId}", sessionId);

        var history = await chatbotService.GetChatHistoryAsync(sessionId, limit > 0 ? limit : 50);

        return Results.Ok(history);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[CHATBOT] Error fetching chat history");
        return Results.Json(new { error = ex.Message }, statusCode: (int)HttpStatusCode.InternalServerError);
    }
})
.WithName("GetChatHistory")
.WithTags("Chatbot");

app.MapDelete("/api/chat/history/{sessionId}", async (
    [FromRoute] string sessionId,
    [FromServices] IChatbotService chatbotService,
    [FromServices] ILogger<Program> logger) =>
{
    try
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return Results.BadRequest(new { error = "SessionId is required" });
        }

        logger.LogInformation("[CHATBOT] Clearing history for session {SessionId}", sessionId);

        var success = await chatbotService.ClearChatHistoryAsync(sessionId);

        if (success)
        {
            logger.LogInformation("[CHATBOT] History cleared for session {SessionId}", sessionId);
            return Results.Ok(new { message = "Chat history cleared successfully", sessionId });
        }
        else
        {
            logger.LogWarning("[CHATBOT] Failed to clear history for session {SessionId}", sessionId);
            return Results.Json(new { error = "Failed to clear chat history" }, statusCode: (int)HttpStatusCode.InternalServerError);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[CHATBOT] Error clearing chat history");
        return Results.Json(new { error = ex.Message }, statusCode: (int)HttpStatusCode.InternalServerError);
    }
})
.WithName("ClearChatHistory")
.WithTags("Chatbot");

app.MapGet("/api/chat/health", async (
    [FromServices] IOllamaService ollamaService,
    [FromServices] ILogger<Program> logger) =>
{
    try
    {
        var isAvailable = await ollamaService.IsAvailableAsync();
        var models = await ollamaService.GetAvailableModelsAsync();

        return Results.Ok(new
        {
            ollama = isAvailable ? "available" : "unavailable",
            models = models,
            configured_model = ollamaModel,
            url = ollamaUrl
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[CHATBOT] Error checking Ollama health");
        return Results.Json(new
        {
            ollama = "unavailable",
            error = ex.Message
        }, statusCode: (int)HttpStatusCode.ServiceUnavailable);
    }
})
.WithName("ChatbotHealth")
.WithTags("Chatbot");

// VIDEO API ENDPOINTS (MongoDB GridFS)

// Upload video (Teacher/Instructor only)
app.MapPost("/api/video/upload", async (
    HttpRequest request,
    [FromServices] IVideoProcessingService videoService,
    [FromServices] ILogger<Program> logger) =>
{
    try
    {
        if (!request.HasFormContentType)
        {
            return Results.BadRequest(new { error = "Multipart form data required" });
        }

        var form = await request.ReadFormAsync();
        var videoFile = form.Files.GetFile("video");
        var lessonIdStr = form["lessonId"].ToString();
        var userIdStr = form["userId"].ToString();

        if (videoFile == null)
        {
            return Results.BadRequest(new { error = "Video file is required" });
        }

        if (!Guid.TryParse(lessonIdStr, out var lessonId))
        {
            return Results.BadRequest(new { error = "Valid lessonId is required" });
        }

        if (!Guid.TryParse(userIdStr, out var userId))
        {
            return Results.BadRequest(new { error = "Valid userId is required" });
        }

        // Validate file size (max 500MB)
        if (videoFile.Length > 500 * 1024 * 1024)
        {
            return Results.BadRequest(new { error = "Video file too large (max 500MB)" });
        }

        // Validate file type
        var allowedTypes = new[] { "video/mp4", "video/webm", "video/ogg", "video/quicktime" };
        if (!allowedTypes.Contains(videoFile.ContentType))
        {
            return Results.BadRequest(new { error = "Invalid video format. Allowed: MP4, WebM, OGG, MOV" });
        }

        logger.LogInformation("[VIDEO] Starting upload for lesson {LessonId} by user {UserId}", lessonId, userId);

        var result = await videoService.ProcessAndSaveVideoAsync(videoFile, lessonId, userId);

        logger.LogInformation(
            "[VIDEO] Upload successful: {FileSize}MB compressed to {CompressedSize}MB ({Ratio:F2}% reduction)",
            result.FileSize / 1024.0 / 1024.0,
            result.CompressedSize / 1024.0 / 1024.0,
            result.CompressionRatio
        );

        return Results.Ok(new
        {
            success = true,
            message = "Video uploaded and processed successfully",
            videoUrl = result.VideoUrl,
            thumbnailUrl = result.ThumbnailUrl,
            fileSize = result.FileSize,
            compressedSize = result.CompressedSize,
            compressionRatio = result.CompressionRatio,
            format = result.Format,
            quality = result.Quality
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[VIDEO] Upload failed");
        return Results.Json(new { error = ex.Message }, statusCode: (int)HttpStatusCode.InternalServerError);
    }
})
.WithName("UploadVideo")
.WithTags("Video")
.DisableAntiforgery(); // Required for file upload

// Stream video (Students + Instructors)
app.MapGet("/api/video/stream/{fileId}", async (
    [FromRoute] string fileId,
    [FromQuery] string? quality,
    [FromServices] IMongoVideoStorageService mongoStorage,
    [FromServices] ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("[VIDEO] Streaming video: {FileId} (quality: {Quality})", fileId, quality ?? "original");

        var videoStream = await mongoStorage.DownloadVideoAsync(fileId);
        var metadata = await mongoStorage.GetVideoMetadataAsync(fileId);

        return Results.Stream(videoStream, metadata.ContentType, enableRangeProcessing: true);
    }
    catch (FileNotFoundException ex)
    {
        logger.LogWarning("[VIDEO] Video not found: {FileId}", fileId);
        return Results.NotFound(new { error = "Video not found" });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[VIDEO] Streaming failed for {FileId}", fileId);
        return Results.Json(new { error = "Failed to stream video" }, statusCode: (int)HttpStatusCode.InternalServerError);
    }
})
.WithName("StreamVideo")
.WithTags("Video");

// Get video metadata
app.MapGet("/api/video/metadata/{fileId}", async (
    [FromRoute] string fileId,
    [FromServices] IMongoVideoStorageService mongoStorage,
    [FromServices] ILogger<Program> logger) =>
{
    try
    {
        var metadata = await mongoStorage.GetVideoMetadataAsync(fileId);

        return Results.Ok(new
        {
            fileId = metadata.FileId,
            fileName = metadata.FileName,
            fileSize = metadata.FileSize,
            compressedSize = metadata.CompressedSize,
            contentType = metadata.ContentType,
            uploadDate = metadata.UploadDate,
            lessonId = metadata.LessonId,
            format = metadata.Format,
            compressionRatio = metadata.FileSize > 0
                ? (double)(metadata.FileSize - metadata.CompressedSize) / metadata.FileSize * 100
                : 0
        });
    }
    catch (FileNotFoundException)
    {
        return Results.NotFound(new { error = "Video not found" });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[VIDEO] Failed to get metadata for {FileId}", fileId);
        return Results.Json(new { error = "Failed to get video metadata" }, statusCode: (int)HttpStatusCode.InternalServerError);
    }
})
.WithName("GetVideoMetadata")
.WithTags("Video");

// Delete video (Instructor/Admin only)
app.MapDelete("/api/video/{videoId}", async (
    [FromRoute] Guid videoId,
    [FromQuery] Guid userId,
    [FromServices] IVideoProcessingService videoService,
    [FromServices] ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("[VIDEO] Deleting video {VideoId} by user {UserId}", videoId, userId);

        var success = await videoService.DeleteVideoAsync(videoId, userId);

        if (success)
        {
            return Results.Ok(new { message = "Video deleted successfully" });
        }
        else
        {
            return Results.NotFound(new { error = "Video not found or already deleted" });
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[VIDEO] Failed to delete video {VideoId}", videoId);
        return Results.Json(new { error = "Failed to delete video" }, statusCode: (int)HttpStatusCode.InternalServerError);
    }
})
.WithName("DeleteVideo")
.WithTags("Video");

// Get upload progress (for large files)
app.MapGet("/api/video/upload/progress/{uploadId}", async (
    [FromRoute] Guid uploadId,
    [FromServices] IVideoProcessingService videoService) =>
{
    var progress = await videoService.GetUploadProgressAsync(uploadId);
    return Results.Ok(progress);
})
.WithName("GetUploadProgress")
.WithTags("Video");

// SYSTEM ENDPOINTS API
app.MapGet("/api/system/endpoints", async (
    [FromServices] ISystemEndpointRepository repository,
    [FromServices] ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("[ENDPOINTS] Fetching all active endpoints");
        var endpoints = await repository.GetAllActiveAsync();

        // Group by category for frontend
        var grouped = endpoints
            .GroupBy(e => e.Category)
            .ToDictionary(
                g => g.Key,
                g => g.ToDictionary(e => e.EndpointKey, e => e.EndpointPath)
            );

        return Results.Ok(grouped);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[ENDPOINTS] Error fetching endpoints");
        return Results.Json(new { error = ex.Message }, statusCode: (int)HttpStatusCode.InternalServerError);
    }
})
.WithName("GetAllEndpoints")
.WithTags("System");

app.MapGet("/api/system/endpoints/{category}", async (
    [FromRoute] string category,
    [FromServices] IEndpointService endpointService,
    [FromServices] ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("[ENDPOINTS] Fetching endpoints for category: {Category}", category);
        var endpoints = await endpointService.GetCategoryEndpointsAsync(category);
        return Results.Ok(endpoints);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[ENDPOINTS] Error fetching category endpoints");
        return Results.Json(new { error = ex.Message }, statusCode: (int)HttpStatusCode.InternalServerError);
    }
})
.WithName("GetCategoryEndpoints")
.WithTags("System");

app.MapGet("/api/system/endpoints/{category}/{key}", async (
    [FromRoute] string category,
    [FromRoute] string key,
    [FromServices] IEndpointService endpointService,
    [FromServices] ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("[ENDPOINTS] Fetching endpoint: {Category}.{Key}", category, key);
        var endpoint = await endpointService.GetEndpointAsync(category, key);

        if (endpoint == null)
        {
            return Results.NotFound(new { error = $"Endpoint {category}.{key} not found" });
        }

        return Results.Ok(new { category, key, path = endpoint });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[ENDPOINTS] Error fetching endpoint");
        return Results.Json(new { error = ex.Message }, statusCode: (int)HttpStatusCode.InternalServerError);
    }
})
.WithName("GetEndpoint")
.WithTags("System");

app.MapPost("/api/system/endpoints/refresh-cache", async (
    [FromServices] IEndpointService endpointService,
    [FromServices] ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("[ENDPOINTS] Refreshing endpoint cache");
        await endpointService.RefreshCacheAsync();
        return Results.Ok(new { message = "Cache refreshed successfully" });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[ENDPOINTS] Error refreshing cache");
        return Results.Json(new { error = ex.Message }, statusCode: (int)HttpStatusCode.InternalServerError);
    }
})
.WithName("RefreshEndpointCache")
.WithTags("System");

Console.WriteLine("🚀 InsightLearn API Starting...");
Console.WriteLine($"Environment: {app.Environment.EnvironmentName}");
Console.WriteLine($"Listening on: {string.Join(", ", builder.WebHost.GetSetting("urls")?.Split(';') ?? new[] { "http://localhost:5000" })}");
Console.WriteLine($"Chatbot enabled with model: {ollamaModel}");
Console.WriteLine($"Ollama URL: {ollamaUrl}");

app.Run();
