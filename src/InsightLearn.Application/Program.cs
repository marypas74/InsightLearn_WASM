using InsightLearn.Application.DTOs;
using InsightLearn.Application.Interfaces;
using InsightLearn.Application.Services;
using InsightLearn.Infrastructure.Data;
using InsightLearn.Infrastructure.Repositories;
using InsightLearn.Infrastructure.Services;
using InsightLearn.Core.Interfaces;
using InsightLearn.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Net;
using System.Reflection;
using System.Security.Claims;

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

// Disable automatic 400 responses for model validation errors (we want to log them)
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = false; // Keep validation, we'll handle in endpoint
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

// Register DbContextFactory for SessionService and other services that need it
builder.Services.AddDbContextFactory<InsightLearnDbContext>(options =>
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

// Configure ASP.NET Identity
builder.Services.AddIdentity<User, IdentityRole<Guid>>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<InsightLearnDbContext>()
.AddDefaultTokenProviders();

// Register Auth Services
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? builder.Configuration["JWT_SECRET_KEY"] ?? "your-very-long-and-secure-secret-key-minimum-32-characters-long!!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? builder.Configuration["JWT_ISSUER"] ?? "InsightLearn.Api";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? builder.Configuration["JWT_AUDIENCE"] ?? "InsightLearn.Users";

builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<IUserLockoutService, UserLockoutService>();
builder.Services.AddScoped<IAuthService>(sp =>
{
    var userManager = sp.GetRequiredService<UserManager<User>>();
    var roleManager = sp.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    var sessionService = sp.GetRequiredService<ISessionService>();
    var lockoutService = sp.GetRequiredService<IUserLockoutService>();
    var logger = sp.GetRequiredService<ILogger<AuthService>>();

    return new AuthService(
        userManager,
        roleManager,
        sessionService,
        lockoutService,
        jwtSecret,
        jwtIssuer,
        jwtAudience,
        logger
    );
});

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

        // Seed default admin user
        Console.WriteLine("[SEED] Checking for admin user...");
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        // Create Admin role if it doesn't exist
        const string adminRoleName = "Admin";
        try
        {
            if (!await roleManager.RoleExistsAsync(adminRoleName))
            {
                Console.WriteLine("[SEED] Creating Admin role...");
                var adminRole = new IdentityRole<Guid>(adminRoleName);
                var roleResult = await roleManager.CreateAsync(adminRole);
                if (roleResult.Succeeded)
                {
                    Console.WriteLine("[SEED] ✓ Admin role created successfully");
                }
                else
                {
                    Console.WriteLine($"[SEED] ⚠ Failed to create Admin role: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
                }
            }
            else
            {
                Console.WriteLine("[SEED] ✓ Admin role already exists");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SEED] ⚠ Exception checking/creating Admin role (this is OK if role already exists): {ex.Message}");
            // Continue - role might already exist from previous startup
        }

        // Create default admin user if it doesn't exist
        const string adminEmail = "admin@insightlearn.cloud";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            Console.WriteLine("[SEED] Creating default admin user...");

            // Get admin password from environment variable or use secure default
            var adminPassword = builder.Configuration["ADMIN_PASSWORD"]
                             ?? builder.Configuration["Admin:Password"]
                             ?? "Admin@InsightLearn2025!"; // Secure default

            adminUser = new User
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "Admin",
                LastName = "InsightLearn",
                EmailConfirmed = true,
                IsVerified = true,
                RegistrationCompleted = true,
                RegistrationCompletedDate = DateTime.UtcNow,
                DateJoined = DateTime.UtcNow,
                HasAgreedToTerms = true,
                TermsAgreedDate = DateTime.UtcNow,
                HasAgreedToPrivacyPolicy = true,
                PrivacyPolicyAgreedDate = DateTime.UtcNow,
                UserType = "Admin",
                WalletBalance = 0
            };

            var createResult = await userManager.CreateAsync(adminUser, adminPassword);

            if (createResult.Succeeded)
            {
                Console.WriteLine("[SEED] ✓ Admin user created successfully");
                Console.WriteLine($"[SEED]   Email: {adminEmail}");
                Console.WriteLine($"[SEED]   Password: {(builder.Configuration["ADMIN_PASSWORD"] != null ? "From ADMIN_PASSWORD env var" : "Default password (change in production!)")}");

                // Assign Admin role to user
                var addToRoleResult = await userManager.AddToRoleAsync(adminUser, adminRoleName);
                if (addToRoleResult.Succeeded)
                {
                    Console.WriteLine("[SEED] ✓ Admin role assigned to user");
                }
                else
                {
                    Console.WriteLine($"[SEED] ⚠ Failed to assign Admin role: {string.Join(", ", addToRoleResult.Errors.Select(e => e.Description))}");
                }
            }
            else
            {
                Console.WriteLine($"[SEED] ⚠ Failed to create admin user: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
            }
        }
        else
        {
            Console.WriteLine("[SEED] ✓ Admin user already exists");

            // Ensure admin has Admin role
            if (!await userManager.IsInRoleAsync(adminUser, adminRoleName))
            {
                Console.WriteLine("[SEED] Assigning Admin role to existing user...");
                var addToRoleResult = await userManager.AddToRoleAsync(adminUser, adminRoleName);
                if (addToRoleResult.Succeeded)
                {
                    Console.WriteLine("[SEED] ✓ Admin role assigned");
                }
            }
        }
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

// AUTHENTICATION API ENDPOINTS
app.MapPost("/api/auth/login", async (
    [FromBody] LoginDto loginDto,
    HttpContext httpContext,
    [FromServices] IAuthService authService,
    [FromServices] ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("[AUTH] 🔐 Login endpoint called");
        logger.LogInformation("[AUTH] 🔑 Attempting login for user: {Email}", loginDto.Email);
        logger.LogInformation("[AUTH] ContentType: {ContentType}", httpContext.Request.ContentType);

        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = httpContext.Request.Headers["User-Agent"].ToString();

        logger.LogInformation("[AUTH] Client IP: {IP}, UserAgent: {UserAgent}", ipAddress, userAgent);

        var result = await authService.LoginAsync(loginDto);

        if (!result.IsSuccess)
        {
            logger.LogWarning("[AUTH] ⚠️ Login failed for user: {Email}. Errors: {Errors}",
                loginDto.Email, string.Join(", ", result.Errors));
            return Results.BadRequest(result);
        }

        logger.LogInformation("[AUTH] ✅ Login successful for user: {Email}", loginDto.Email);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[AUTH] 💥 Unexpected error during login");
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.Accepts<LoginDto>("application/json")
.Produces<AuthResultDto>(200)
.Produces(400)
.Produces(500);

app.MapPost("/api/auth/register", async (
    [FromBody] RegisterDto registerDto,
    [FromServices] IAuthService authService,
    [FromServices] ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("[AUTH] Registration attempt for user: {Email}", registerDto.Email);

        var result = await authService.RegisterAsync(registerDto);

        if (!result.IsSuccess)
        {
            logger.LogWarning("[AUTH] Registration failed for user: {Email}. Errors: {Errors}",
                registerDto.Email, string.Join(", ", result.Errors));
            return Results.BadRequest(result);
        }

        logger.LogInformation("[AUTH] Registration successful for user: {Email}", registerDto.Email);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[AUTH] Error during registration for user: {Email}", registerDto.Email);
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
});

app.MapPost("/api/auth/refresh", async (
    HttpContext httpContext,
    [FromServices] IAuthService authService,
    [FromServices] ILogger<Program> logger) =>
{
    try
    {
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            logger.LogWarning("[AUTH] Refresh token attempt with no user ID");
            return Results.Unauthorized();
        }

        logger.LogInformation("[AUTH] Refresh token for user: {UserId}", userId);

        var result = await authService.RefreshTokenAsync(userId);

        if (!result.IsSuccess)
        {
            logger.LogWarning("[AUTH] Refresh token failed for user: {UserId}", userId);
            return Results.Unauthorized();
        }

        logger.LogInformation("[AUTH] Refresh token successful for user: {UserId}", userId);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[AUTH] Error during token refresh");
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
});

app.MapGet("/api/auth/me", async (
    HttpContext httpContext,
    [FromServices] IAuthService authService,
    [FromServices] ILogger<Program> logger) =>
{
    try
    {
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            logger.LogWarning("[AUTH] Me endpoint called with no user ID");
            return Results.Unauthorized();
        }

        logger.LogInformation("[AUTH] Getting current user info for: {UserId}", userId);

        var user = await authService.GetCurrentUserAsync(userId);

        if (user == null)
        {
            logger.LogWarning("[AUTH] User not found: {UserId}", userId);
            return Results.NotFound(new { message = "User not found" });
        }

        logger.LogInformation("[AUTH] Current user info retrieved for: {UserId}", userId);
        return Results.Ok(user);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[AUTH] Error getting current user");
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
}).RequireAuthorization();

app.MapPost("/api/auth/oauth-callback", async (
    [FromBody] GoogleLoginDto googleLoginDto,
    [FromServices] IAuthService authService,
    [FromServices] ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("[AUTH] Google OAuth callback received");

        var result = await authService.GoogleLoginAsync(googleLoginDto);

        if (!result.IsSuccess)
        {
            logger.LogWarning("[AUTH] Google login failed");
            return Results.BadRequest(result);
        }

        logger.LogInformation("[AUTH] Google login successful");
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[AUTH] Error during Google OAuth");
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
});

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

// ADMIN API ENDPOINTS (require Admin role)

// Dashboard Stats
app.MapGet("/api/admin/dashboard/stats", async (
    [FromServices] InsightLearnDbContext dbContext,
    [FromServices] UserManager<User> userManager,
    [FromServices] ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("[ADMIN] Fetching dashboard stats");

        var totalUsers = await dbContext.Users.CountAsync();
        var totalCourses = await dbContext.Courses.CountAsync();
        var totalEnrollments = await dbContext.Enrollments.CountAsync();
        var totalRevenue = await dbContext.Payments
            .Where(p => p.Status == PaymentStatus.Completed)
            .SumAsync(p => (decimal?)p.Amount) ?? 0;

        // Count users in Student role (using UserManager)
        var activeStudents = 0;
        var activeInstructors = await dbContext.Users.CountAsync(u => u.IsInstructor);

        try
        {
            var allUsers = await userManager.Users.ToListAsync();
            foreach (var user in allUsers)
            {
                var roles = await userManager.GetRolesAsync(user);
                if (roles.Contains("Student"))
                {
                    activeStudents++;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning("[ADMIN] Could not count student roles: {Error}", ex.Message);
        }

        var publishedCourses = await dbContext.Courses.CountAsync(c => c.Status == CourseStatus.Published);
        var draftCourses = await dbContext.Courses.CountAsync(c => c.Status == CourseStatus.Draft);

        var stats = new AdminDashboardDto
        {
            TotalUsers = totalUsers,
            TotalCourses = totalCourses,
            TotalEnrollments = totalEnrollments,
            TotalRevenue = totalRevenue,
            TotalActiveUsers = totalUsers, // For now, assume all are active
            TotalInstructors = activeInstructors,
            TotalStudents = activeStudents,
            TotalPayments = await dbContext.Payments.CountAsync(p => p.Status == PaymentStatus.Completed)
        };

        logger.LogInformation("[ADMIN] Dashboard stats retrieved successfully");
        return Results.Ok(stats);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[ADMIN] Error fetching dashboard stats");
        return Results.Json(new { error = ex.Message }, statusCode: (int)HttpStatusCode.InternalServerError);
    }
})
.RequireAuthorization(policy => policy.RequireRole("Admin"))
.WithName("GetDashboardStats")
.WithTags("Admin");

// Recent Activity
app.MapGet("/api/admin/dashboard/recent-activity", async (
    [FromServices] InsightLearnDbContext dbContext,
    [FromServices] ILogger<Program> logger,
    [FromQuery] int limit = 10) =>
{
    try
    {
        logger.LogInformation("[ADMIN] Fetching recent activity (limit: {Limit})", limit);

        var recentEnrollments = await dbContext.Enrollments
            .Include(e => e.User)
            .Include(e => e.Course)
            .OrderByDescending(e => e.EnrolledAt)
            .Take(limit)
            .Select(e => new RecentActivityDto
            {
                Type = "Enrollment",
                Description = $"{e.User.FirstName} {e.User.LastName} enrolled in {e.Course.Title}",
                UserName = e.User.Email,
                Timestamp = e.EnrolledAt,
                Icon = "graduation-cap",
                Severity = "Info"
            })
            .ToListAsync();

        return Results.Ok(recentEnrollments);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[ADMIN] Error fetching recent activity");
        return Results.Json(new { error = ex.Message }, statusCode: (int)HttpStatusCode.InternalServerError);
    }
})
.RequireAuthorization(policy => policy.RequireRole("Admin"))
.WithName("GetRecentActivity")
.WithTags("Admin");

// USER MANAGEMENT ENDPOINTS

// Get all users with pagination and search
app.MapGet("/api/admin/users", async (
    [FromServices] UserManager<User> userManager,
    [FromServices] InsightLearnDbContext dbContext,
    [FromServices] ILogger<Program> logger,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10,
    [FromQuery] string? search = null) =>
{
    try
    {
        logger.LogInformation("[ADMIN] Fetching users (page: {Page}, pageSize: {PageSize}, search: {Search})",
            page, pageSize, search ?? "none");

        var query = userManager.Users.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(u =>
                u.Email.Contains(search) ||
                u.FirstName.Contains(search) ||
                u.LastName.Contains(search));
        }

        var totalCount = await query.CountAsync();
        var users = await query
            .OrderByDescending(u => u.DateJoined)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var userDtos = new List<AdminUserDto>();
        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            var enrollmentCount = await dbContext.Enrollments.CountAsync(e => e.UserId == user.Id);
            var coursesCount = await dbContext.Courses.CountAsync(c => c.InstructorId == user.Id);

            userDtos.Add(new AdminUserDto
            {
                Id = user.Id,
                UserName = user.UserName ?? user.Email,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = user.FullName,
                IsVerified = user.IsVerified,
                IsInstructor = user.IsInstructor,
                DateJoined = user.DateJoined,
                LastLoginDate = user.LastLoginDate,
                WalletBalance = user.WalletBalance,
                LockoutEnabled = user.LockoutEnabled,
                LockoutEnd = user.LockoutEnd,
                Roles = roles.ToList(),
                TotalCourses = coursesCount,
                TotalEnrollments = enrollmentCount
            });
        }

        var result = new PagedResultDto<AdminUserDto>
        {
            Items = userDtos,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };

        logger.LogInformation("[ADMIN] Retrieved {Count} users", userDtos.Count);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[ADMIN] Error fetching users");
        return Results.Json(new { error = ex.Message }, statusCode: (int)HttpStatusCode.InternalServerError);
    }
})
.RequireAuthorization(policy => policy.RequireRole("Admin"))
.WithName("GetAdminUsers")
.WithTags("Admin");

// Get user by ID
app.MapGet("/api/admin/users/{id:guid}", async (
    Guid id,
    [FromServices] UserManager<User> userManager,
    [FromServices] InsightLearnDbContext dbContext,
    [FromServices] ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("[ADMIN] Fetching user {UserId}", id);

        var user = await userManager.FindByIdAsync(id.ToString());
        if (user == null)
        {
            return Results.NotFound(new { error = "User not found" });
        }

        var roles = await userManager.GetRolesAsync(user);
        var enrollmentCount = await dbContext.Enrollments.CountAsync(e => e.UserId == user.Id);
        var coursesCount = await dbContext.Courses.CountAsync(c => c.InstructorId == user.Id);

        var userDto = new AdminUserDto
        {
            Id = user.Id,
            UserName = user.UserName ?? user.Email,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = user.FullName,
            IsVerified = user.IsVerified,
            IsInstructor = user.IsInstructor,
            DateJoined = user.DateJoined,
            LastLoginDate = user.LastLoginDate,
            WalletBalance = user.WalletBalance,
            LockoutEnabled = user.LockoutEnabled,
            LockoutEnd = user.LockoutEnd,
            Roles = roles.ToList(),
            TotalCourses = coursesCount,
            TotalEnrollments = enrollmentCount
        };

        return Results.Ok(userDto);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[ADMIN] Error fetching user {UserId}", id);
        return Results.Json(new { error = ex.Message }, statusCode: (int)HttpStatusCode.InternalServerError);
    }
})
.RequireAuthorization(policy => policy.RequireRole("Admin"))
.WithName("GetAdminUserById")
.WithTags("Admin");

// Update user
app.MapPut("/api/admin/users/{id:guid}", async (
    Guid id,
    [FromBody] UpdateUserDto updateDto,
    [FromServices] UserManager<User> userManager,
    [FromServices] RoleManager<IdentityRole<Guid>> roleManager,
    [FromServices] ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("[ADMIN] Updating user {UserId}", id);

        var user = await userManager.FindByIdAsync(id.ToString());
        if (user == null)
        {
            return Results.NotFound(new { error = "User not found" });
        }

        // Update basic properties
        user.FirstName = updateDto.FirstName;
        user.LastName = updateDto.LastName;
        user.IsInstructor = updateDto.IsInstructor;
        user.IsVerified = updateDto.IsVerified;
        user.WalletBalance = updateDto.WalletBalance;

        // Update email if changed
        if (user.Email != updateDto.Email)
        {
            var setEmailResult = await userManager.SetEmailAsync(user, updateDto.Email);
            if (!setEmailResult.Succeeded)
            {
                return Results.BadRequest(new { errors = setEmailResult.Errors.Select(e => e.Description) });
            }
            await userManager.SetUserNameAsync(user, updateDto.Email);
        }

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return Results.BadRequest(new { errors = updateResult.Errors.Select(e => e.Description) });
        }

        logger.LogInformation("[ADMIN] User {UserId} updated successfully", id);
        return Results.Ok(new { success = true, message = "User updated successfully" });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[ADMIN] Error updating user {UserId}", id);
        return Results.Json(new { error = ex.Message }, statusCode: (int)HttpStatusCode.InternalServerError);
    }
})
.RequireAuthorization(policy => policy.RequireRole("Admin"))
.WithName("UpdateAdminUser")
.WithTags("Admin");

// Delete user
app.MapDelete("/api/admin/users/{id:guid}", async (
    Guid id,
    [FromServices] UserManager<User> userManager,
    [FromServices] ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("[ADMIN] Deleting user {UserId}", id);

        var user = await userManager.FindByIdAsync(id.ToString());
        if (user == null)
        {
            return Results.NotFound(new { error = "User not found" });
        }

        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            return Results.BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        logger.LogInformation("[ADMIN] User {UserId} deleted successfully", id);
        return Results.Ok(new { success = true, message = "User deleted successfully" });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[ADMIN] Error deleting user {UserId}", id);
        return Results.Json(new { error = ex.Message }, statusCode: (int)HttpStatusCode.InternalServerError);
    }
})
.RequireAuthorization(policy => policy.RequireRole("Admin"))
.WithName("DeleteAdminUser")
.WithTags("Admin");

// COURSE MANAGEMENT ENDPOINTS

// Get all courses (admin view)
app.MapGet("/api/admin/courses", async (
    [FromServices] InsightLearnDbContext dbContext,
    [FromServices] ILogger<Program> logger,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10,
    [FromQuery] string? search = null) =>
{
    try
    {
        logger.LogInformation("[ADMIN] Fetching courses (page: {Page}, pageSize: {PageSize})", page, pageSize);

        var query = dbContext.Courses
            .Include(c => c.Instructor)
            .Include(c => c.Category)
            .Include(c => c.Enrollments)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(c =>
                c.Title.Contains(search) ||
                c.Description.Contains(search));
        }

        var totalCount = await query.CountAsync();
        var courses = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CourseDto
            {
                Id = c.Id,
                Title = c.Title,
                Description = c.Description,
                ShortDescription = c.ShortDescription,
                InstructorId = c.InstructorId,
                InstructorName = c.Instructor.FirstName + " " + c.Instructor.LastName,
                CategoryId = c.CategoryId,
                CategoryName = c.Category.Name,
                Price = c.Price,
                DiscountPercentage = c.DiscountPercentage,
                CurrentPrice = c.CurrentPrice,
                ThumbnailUrl = c.ThumbnailUrl,
                Level = c.Level,
                Status = c.Status,
                CreatedAt = c.CreatedAt,
                PublishedAt = c.PublishedAt,
                EnrollmentCount = c.Enrollments.Count,
                AverageRating = c.AverageRating,
                ReviewCount = c.ReviewCount,
                ViewCount = c.ViewCount
            })
            .ToListAsync();

        var result = new PagedResultDto<CourseDto>
        {
            Items = courses,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };

        logger.LogInformation("[ADMIN] Retrieved {Count} courses", courses.Count);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[ADMIN] Error fetching courses");
        return Results.Json(new { error = ex.Message }, statusCode: (int)HttpStatusCode.InternalServerError);
    }
})
.RequireAuthorization(policy => policy.RequireRole("Admin"))
.WithName("GetAdminCourses")
.WithTags("Admin");

// Create course
app.MapPost("/api/admin/courses", async (
    [FromBody] CreateCourseDto createDto,
    [FromServices] InsightLearnDbContext dbContext,
    [FromServices] ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("[ADMIN] Creating new course: {Title}", createDto.Title);

        // Verify instructor exists
        var instructorExists = await dbContext.Users.AnyAsync(u => u.Id == createDto.InstructorId);
        if (!instructorExists)
        {
            return Results.BadRequest(new { error = "Instructor not found" });
        }

        // Verify category exists
        var categoryExists = await dbContext.Categories.AnyAsync(c => c.Id == createDto.CategoryId);
        if (!categoryExists)
        {
            return Results.BadRequest(new { error = "Category not found" });
        }

        // Generate slug from title
        var slug = createDto.Title.ToLower()
            .Replace(" ", "-")
            .Replace("[^a-z0-9-]", "");

        var course = new Course
        {
            Title = createDto.Title,
            Description = createDto.Description,
            ShortDescription = createDto.ShortDescription,
            InstructorId = createDto.InstructorId,
            CategoryId = createDto.CategoryId,
            Price = createDto.Price,
            DiscountPercentage = createDto.DiscountPercentage,
            ThumbnailUrl = createDto.ThumbnailUrl,
            PreviewVideoUrl = createDto.PreviewVideoUrl,
            Level = createDto.Level,
            EstimatedDurationMinutes = createDto.EstimatedDurationMinutes,
            Requirements = createDto.Requirements,
            WhatYouWillLearn = createDto.WhatYouWillLearn,
            Language = createDto.Language ?? "English",
            HasCertificate = createDto.HasCertificate,
            Slug = slug,
            Status = CourseStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Courses.Add(course);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("[ADMIN] Course created successfully: {CourseId}", course.Id);
        return Results.Created($"/api/admin/courses/{course.Id}", new { id = course.Id, success = true });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[ADMIN] Error creating course");
        return Results.Json(new { error = ex.Message }, statusCode: (int)HttpStatusCode.InternalServerError);
    }
})
.RequireAuthorization(policy => policy.RequireRole("Admin"))
.WithName("CreateAdminCourse")
.WithTags("Admin");

// Update course
app.MapPut("/api/admin/courses/{id:guid}", async (
    Guid id,
    [FromBody] UpdateCourseDto updateDto,
    [FromServices] InsightLearnDbContext dbContext,
    [FromServices] ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("[ADMIN] Updating course {CourseId}", id);

        var course = await dbContext.Courses.FindAsync(id);
        if (course == null)
        {
            return Results.NotFound(new { error = "Course not found" });
        }

        // Update properties
        course.Title = updateDto.Title;
        course.Description = updateDto.Description;
        course.ShortDescription = updateDto.ShortDescription;
        course.CategoryId = updateDto.CategoryId;
        course.Price = updateDto.Price;
        course.DiscountPercentage = updateDto.DiscountPercentage;
        course.ThumbnailUrl = updateDto.ThumbnailUrl;
        course.PreviewVideoUrl = updateDto.PreviewVideoUrl;
        course.Level = updateDto.Level;
        course.Status = updateDto.Status;
        course.EstimatedDurationMinutes = updateDto.EstimatedDurationMinutes;
        course.Requirements = updateDto.Requirements;
        course.WhatYouWillLearn = updateDto.WhatYouWillLearn;
        course.Language = updateDto.Language ?? "English";
        course.HasCertificate = updateDto.HasCertificate;
        course.UpdatedAt = DateTime.UtcNow;

        // Update instructor if provided
        if (updateDto.InstructorId.HasValue)
        {
            course.InstructorId = updateDto.InstructorId.Value;
        }

        // Set published date if status changed to Published
        if (updateDto.Status == CourseStatus.Published && !course.PublishedAt.HasValue)
        {
            course.PublishedAt = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync();

        logger.LogInformation("[ADMIN] Course {CourseId} updated successfully", id);
        return Results.Ok(new { success = true, message = "Course updated successfully" });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[ADMIN] Error updating course {CourseId}", id);
        return Results.Json(new { error = ex.Message }, statusCode: (int)HttpStatusCode.InternalServerError);
    }
})
.RequireAuthorization(policy => policy.RequireRole("Admin"))
.WithName("UpdateAdminCourse")
.WithTags("Admin");

// Delete course
app.MapDelete("/api/admin/courses/{id:guid}", async (
    Guid id,
    [FromServices] InsightLearnDbContext dbContext,
    [FromServices] ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("[ADMIN] Deleting course {CourseId}", id);

        var course = await dbContext.Courses
            .Include(c => c.Enrollments)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (course == null)
        {
            return Results.NotFound(new { error = "Course not found" });
        }

        // Check if course has enrollments
        if (course.Enrollments.Any())
        {
            return Results.BadRequest(new { error = "Cannot delete course with active enrollments. Archive it instead." });
        }

        dbContext.Courses.Remove(course);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("[ADMIN] Course {CourseId} deleted successfully", id);
        return Results.Ok(new { success = true, message = "Course deleted successfully" });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[ADMIN] Error deleting course {CourseId}", id);
        return Results.Json(new { error = ex.Message }, statusCode: (int)HttpStatusCode.InternalServerError);
    }
})
.RequireAuthorization(policy => policy.RequireRole("Admin"))
.WithName("DeleteAdminCourse")
.WithTags("Admin");

Console.WriteLine("🚀 InsightLearn API Starting...");
Console.WriteLine($"Environment: {app.Environment.EnvironmentName}");
Console.WriteLine($"Listening on: {string.Join(", ", builder.WebHost.GetSetting("urls")?.Split(';') ?? new[] { "http://localhost:5000" })}");
Console.WriteLine($"Chatbot enabled with model: {ollamaModel}");
Console.WriteLine($"Ollama URL: {ollamaUrl}");

app.Run();
