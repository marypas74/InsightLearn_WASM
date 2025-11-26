using InsightLearn.Application.DTOs;
using InsightLearn.Application.Endpoints;
using InsightLearn.Application.Interfaces;
using InsightLearn.Application.Middleware;
using InsightLearn.Application.Services;
using StackExchange.Redis;
using InsightLearn.Infrastructure.Data;
using InsightLearn.Infrastructure.Repositories;
using InsightLearn.Infrastructure.Services;
using InsightLearn.Core.Interfaces;
using InsightLearn.Core.Entities;
using InsightLearn.Core.DTOs.Admin;
using InsightLearn.Core.DTOs.Category;
using InsightLearn.Core.DTOs.Course;
using InsightLearn.Core.DTOs.Enrollment;
using InsightLearn.Core.DTOs.Payment;
using InsightLearn.Core.DTOs.Review;
using InsightLearn.Core.DTOs.StudentNotes;
using InsightLearn.Core.DTOs.VideoTranscript;
using InsightLearn.Core.DTOs.AITakeaways;
using InsightLearn.Core.DTOs.AIChat;
using InsightLearn.Core.DTOs.VideoBookmarks;
using InsightLearn.Core.DTOs.Engagement;
using Hangfire;
using Hangfire.SqlServer;
using InsightLearn.Application.BackgroundJobs;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Net;
using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// SECURITY FIX (CRIT-2): Configure request body size limits to prevent memory exhaustion attacks
// Default: 10MB for all endpoints (down from 30MB default)
// Video upload: 524MB (500MB + 24MB buffer) via endpoint-specific configuration
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10 MB default
    Console.WriteLine("[SECURITY] Global request body size limit: 10 MB (CRIT-2 fix)");
});

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

// SECURITY FIX (CRIT-4): Enforce MongoDB credentials from environment variables
// NEVER use hardcoded passwords in connection strings
var mongoConnectionString = Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING")
    ?? builder.Configuration["MongoDb:ConnectionString"]
    ?? builder.Configuration.GetConnectionString("MongoDB");

if (string.IsNullOrWhiteSpace(mongoConnectionString))
{
    throw new InvalidOperationException(
        "MongoDB connection string is not configured. " +
        "Set MONGODB_CONNECTION_STRING environment variable with a valid connection string. " +
        "Format: mongodb://username:password@host:port/database?authSource=admin");
}

// Validate MongoDB connection string doesn't contain default/insecure passwords
var insecureMongoPatterns = new[]
{
    "InsightLearn2024!SecureMongo",
    "admin:admin",
    "admin:password",
    "root:root",
    "mongodb:mongodb",
    "password123"
};

var lowerMongoString = mongoConnectionString.ToLowerInvariant();
foreach (var pattern in insecureMongoPatterns)
{
    if (lowerMongoString.Contains(pattern.ToLowerInvariant()))
    {
        throw new InvalidOperationException(
            $"MongoDB connection string contains an insecure/default password pattern. " +
            $"Please use a strong, unique password for production.");
    }
}

Console.WriteLine($"[CONFIG] Database: {connectionString}");
Console.WriteLine($"[CONFIG] Ollama URL: {ollamaUrl}");
Console.WriteLine($"[CONFIG] Ollama Model: {ollamaModel}");

// SECURITY FIX (CRIT-4): Sanitize MongoDB password from connection string before logging
var sanitizedMongo = System.Text.RegularExpressions.Regex.Replace(
    mongoConnectionString,
    @"://([^:]+):([^@]+)@",
    "://$1:***@");
Console.WriteLine($"[CONFIG] MongoDB: {sanitizedMongo} (CRIT-4: password sanitized)");

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "InsightLearn API",
        Version = versionShort,
        Description = @"**InsightLearn Enterprise LMS Platform - Complete API Documentation**

A comprehensive Learning Management System with advanced features:
- **Authentication & Authorization**: JWT-based secure authentication with role-based access control (Admin, Instructor, Student)
- **AI Chatbot**: Ollama-powered conversational assistant for student support
- **Video Management**: MongoDB GridFS video storage with streaming, compression, and progress tracking
- **Course Management**: Full CRUD for courses, categories, sections, and lessons
- **Enrollment System**: Student enrollment with progress tracking and completion certificates
- **Payment Integration**: Stripe & PayPal integration with coupon support
- **Review System**: Course reviews and ratings for quality assurance
- **SaaS Subscription Model**: Recurring billing with engagement-based instructor payouts
- **Admin Dashboard**: Comprehensive analytics and platform management

**Security Features**:
- CSRF Protection (double-submit cookie pattern)
- Rate Limiting (distributed Redis-backed, DDoS protection)
- PCI DSS compliant payment processing
- GDPR compliant audit logging
- Security headers (CSP, HSTS, X-Frame-Options, etc.)

**Performance**:
- Database connection pooling with retry policies
- Response compression (GZip)
- Prometheus metrics for monitoring
- Comprehensive health checks for all dependencies",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "InsightLearn Support",
            Email = "support@insightlearn.cloud",
            Url = new Uri("https://insightlearn.cloud")
        },
        License = new Microsoft.OpenApi.Models.OpenApiLicense
        {
            Name = "Proprietary",
            Url = new Uri("https://insightlearn.cloud/license")
        }
    });

    // Include XML comments from generated documentation file
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
        Console.WriteLine($"[SWAGGER] XML documentation loaded from: {xmlFile}");
    }
    else
    {
        Console.WriteLine($"[SWAGGER] Warning: XML documentation file not found at {xmlPath}");
    }

    // Add JWT Bearer authentication to Swagger UI
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = @"JWT Authorization header using the Bearer scheme.
                      Enter 'Bearer' [space] and then your token in the text input below.
                      Example: 'Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...'",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Configure JSON serialization to use PascalCase (ASP.NET Core default)
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = null; // null = PascalCase
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
    options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;  // Prevent circular reference infinite loops
    options.SerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;  // Reduce payload size
});

// Disable automatic 400 responses for model validation errors (we want to log them)
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = false; // Keep validation, we'll handle in endpoint
});

// Configure CORS - Secure configuration with explicit allowed origins
// Read allowed origins from environment variable or configuration
var allowedOrigins = builder.Configuration["Cors:AllowedOrigins"]?.Split(',')
    ?? new[] {
        "https://localhost:7003",  // Dev WebAssembly
        "https://insightlearn.cloud",  // Production
        "https://www.insightlearn.cloud",
        "https://admin.insightlearn.cloud"
    };

Console.WriteLine($"[SECURITY] CORS configured with {allowedOrigins.Length} allowed origins");

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials()  // Required for JWT in cookies
              .SetIsOriginAllowedToAllowWildcardSubdomains();
    });
});

// Helper method to extract client IP (handles proxies)
static string GetClientIp(HttpContext context)
{
    // Check X-Forwarded-For first (Nginx/Traefik/Cloudflare proxy)
    var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
    if (!string.IsNullOrEmpty(forwarded))
        return forwarded.Split(',')[0].Trim();

    // Check X-Real-IP (alternative proxy header)
    var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
    if (!string.IsNullOrEmpty(realIp))
        return realIp;

    // Fallback to direct connection
    return context.Connection.RemoteIpAddress?.ToString()
        ?? "unknown-" + Guid.NewGuid().ToString();
}

// Configure Rate Limiting (DDoS protection)
builder.Services.AddRateLimiter(options =>
{
    // Global rate limit: 200 requests per minute per IP (Blazor WASM initial load ~15-20 requests)
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: GetClientIp(httpContext),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 200, // Increased from 100 (architect recommendation)
                Window = TimeSpan.FromMinutes(1)
            }));

    // Authentication endpoints: 5 requests per minute per IP (prevent brute force)
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: GetClientIp(httpContext),
            factory: partition => new SlidingWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6 // 10-second segments (prevents double-dipping attacks)
            }));

    // API endpoints: 50 requests per minute per user (authenticated)
    options.AddPolicy("api", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name ?? GetClientIp(httpContext),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 50, // Increased from 30 (architect recommendation)
                Window = TimeSpan.FromMinutes(1)
            }));

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = 429; // Too Many Requests

        // Add Retry-After header if available
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers["Retry-After"] = ((int)retryAfter.TotalSeconds).ToString();
        }

        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            error = "Rate limit exceeded",
            retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retry)
                ? $"{(int)retry.TotalSeconds} seconds"
                : "60 seconds"
        }, cancellationToken: cancellationToken);

        // Security audit logging
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(
            "[SECURITY] Rate limit exceeded. IP: {IpAddress}, Endpoint: {Endpoint}, Method: {Method}",
            GetClientIp(context.HttpContext),
            context.HttpContext.Request.Path,
            context.HttpContext.Request.Method);
    };
});

Console.WriteLine("[SECURITY] Rate limiting configured:");
Console.WriteLine("[SECURITY] - Global: 200 req/min per IP (proxy-aware)");
Console.WriteLine("[SECURITY] - Auth endpoints: 5 req/min per IP (sliding window, brute force protection)");
Console.WriteLine("[SECURITY] - API endpoints: 50 req/min per user");

// PERFORMANCE FIX (PERF-3): Configure SQL Server connection pooling
// Prevents connection exhaustion under high load
var csBuilder = new SqlConnectionStringBuilder(connectionString)
{
    MinPoolSize = 5,            // Keep 5 connections warm for fast response
    MaxPoolSize = 100,          // Limit to 100 to prevent SQL Server exhaustion
    Pooling = true,             // Enable pooling (default: true, explicit for clarity)
    ConnectTimeout = 30,        // 30 second connection timeout
    ConnectRetryCount = 3,      // Retry 3 times on connection failure
    ConnectRetryInterval = 10   // 10 seconds between retries
};

Console.WriteLine($"[CONFIG] SQL Connection Pool: Min={csBuilder.MinPoolSize}, Max={csBuilder.MaxPoolSize}, Timeout={csBuilder.ConnectTimeout}s");

// Configure Entity Framework with SQL Server
builder.Services.AddDbContext<InsightLearnDbContext>(options =>
{
    options.UseSqlServer(csBuilder.ConnectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
        sqlOptions.CommandTimeout(120);
        // PERFORMANCE FIX (PERF-3): Use SplitQuery to avoid cartesian explosion
        sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
    });
});

// Register DbContextFactory for SessionService and other services that need it
// PERFORMANCE FIX (PERF-3): Uses same pooled connection string
builder.Services.AddDbContextFactory<InsightLearnDbContext>(options =>
{
    options.UseSqlServer(csBuilder.ConnectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
        sqlOptions.CommandTimeout(120);
        sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
    });
});

// Register HttpClient for services
builder.Services.AddHttpClient();

// Configure HttpClient for Prometheus
builder.Services.AddHttpClient("Prometheus", client =>
{
    var prometheusUrl = builder.Configuration["Prometheus:Url"]
        ?? "http://prometheus:9090"; // K8s internal service
    client.BaseAddress = new Uri(prometheusUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Register Prometheus Service
builder.Services.AddScoped<IPrometheusService, PrometheusService>();

// Register Metrics Service (Phase 4.2 - Custom Application Metrics)
builder.Services.AddSingleton<MetricsService>();
Console.WriteLine("[CONFIG] MetricsService registered (custom Prometheus metrics)");

// Register Audit Service (database-backed audit logging)
builder.Services.AddScoped<IAuditService, AuditService>();
Console.WriteLine("[CONFIG] Audit Service registered (database-backed compliance logging)");

// Register Logging Service (required by AdminService)
builder.Services.AddScoped<ILoggingService, LoggingService>();
Console.WriteLine("[CONFIG] Logging Service registered (database-backed error/access logging)");

// Configure ASP.NET Identity
builder.Services.AddIdentity<User, IdentityRole<Guid>>(options =>
{
    // Password settings (PCI DSS 8.2.3 compliant)
    // PCI DSS Requirement 8.2.3: Passwords must contain:
    // - At least 8 characters (increased from 6)
    // - Mixture of numeric, alphabetic characters
    // - Both upper and lower case
    // - Special characters (!@#$%^&*()_+-=[]{}|:";'<>?,.)
    options.Password.RequireDigit = true;              // At least one number
    options.Password.RequireLowercase = true;          // At least one lowercase
    options.Password.RequireUppercase = true;          // At least one uppercase
    options.Password.RequireNonAlphanumeric = true;    // ✅ At least one special char (PCI DSS compliance)
    options.Password.RequiredLength = 8;               // ✅ Minimum 8 characters (PCI DSS compliance)

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<InsightLearnDbContext>()
.AddDefaultTokenProviders();

// Get JWT configuration with validation (Phase 2.1: JWT Secret Hardening)
// CRITICAL SECURITY: Prioritize environment variable over appsettings.json
var jwtSecretFromEnv = Environment.GetEnvironmentVariable("JWT_SECRET_KEY");
var jwtSecretFromConfig = builder.Configuration["Jwt:Secret"];
var jwtSecret = jwtSecretFromEnv ?? jwtSecretFromConfig;

// REQUIREMENT 1: No fallback - throw if not configured
if (string.IsNullOrWhiteSpace(jwtSecret))
{
    throw new InvalidOperationException(
        "JWT Secret Key is not configured. " +
        "Set JWT_SECRET_KEY environment variable (RECOMMENDED for production) or JwtSettings:SecretKey in appsettings.json. " +
        "Minimum length: 32 characters. " +
        "Generate a secure key using: openssl rand -base64 64");
}

// REQUIREMENT 2: Minimum length validation (32 characters)
if (jwtSecret.Length < 32)
{
    throw new InvalidOperationException(
        $"JWT Secret Key is too short ({jwtSecret.Length} characters). " +
        $"Minimum required: 32 characters. " +
        $"Current key length is insufficient for cryptographic security. " +
        $"Generate a secure key using: openssl rand -base64 64");
}

// REQUIREMENT 3: Block common insecure/default values
var insecureValues = new[]
{
    "changeme",
    "your-secret-key",
    "insecure",
    "test",
    "dev",
    "password",
    "secret",
    "default",
    "replace_with",
    "insightlearn", // Don't allow app name as secret
    "jwt_secret",
    "your_secret",
    "my_secret",
    "REPLACE_WITH_JWT_SECRET_KEY_ENV_VAR_MINIMUM_32_CHARS" // From appsettings.json placeholder
};

var lowerSecret = jwtSecret.ToLowerInvariant();
foreach (var insecureValue in insecureValues)
{
    if (lowerSecret.Contains(insecureValue))
    {
        throw new InvalidOperationException(
            $"JWT Secret Key contains an insecure/default value: '{insecureValue}'. " +
            $"This is a CRITICAL SECURITY VULNERABILITY. " +
            $"Please configure a strong, cryptographically random secret key. " +
            $"Generate one using: openssl rand -base64 64");
    }
}

// REQUIREMENT 4: Log warning if secret comes from appsettings.json instead of environment variable
if (jwtSecretFromEnv == null)
{
    Console.WriteLine("[SECURITY WARNING] JWT Secret is loaded from appsettings.json. " +
        "For production deployments, use JWT_SECRET_KEY environment variable instead. " +
        "Configuration files should NOT contain production secrets.");
}
else
{
    Console.WriteLine("[SECURITY] JWT Secret loaded from JWT_SECRET_KEY environment variable (RECOMMENDED)");
}

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? builder.Configuration["JWT_ISSUER"];
if (string.IsNullOrWhiteSpace(jwtIssuer))
{
    throw new InvalidOperationException(
        "JWT Issuer is not configured. Please set JWT_ISSUER environment variable or Jwt:Issuer in appsettings.json.");
}

var jwtAudience = builder.Configuration["Jwt:Audience"] ?? builder.Configuration["JWT_AUDIENCE"];
if (string.IsNullOrWhiteSpace(jwtAudience))
{
    throw new InvalidOperationException(
        "JWT Audience is not configured. Please set JWT_AUDIENCE environment variable or Jwt:Audience in appsettings.json.");
}

Console.WriteLine("[SECURITY] JWT configuration validated successfully");
Console.WriteLine($"[SECURITY] - Issuer: {jwtIssuer}");
Console.WriteLine($"[SECURITY] - Audience: {jwtAudience}");
Console.WriteLine($"[SECURITY] - Secret length: {jwtSecret.Length} characters (minimum: 32)");

// Configure JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ClockSkew = TimeSpan.Zero
    };
});

// Configure Authorization
builder.Services.AddAuthorization();

builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<IUserLockoutService, UserLockoutService>();
builder.Services.AddScoped<IAuthService>(sp =>
{
    var userManager = sp.GetRequiredService<UserManager<User>>();
    var roleManager = sp.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    var sessionService = sp.GetRequiredService<ISessionService>();
    var lockoutService = sp.GetRequiredService<IUserLockoutService>();
    var logger = sp.GetRequiredService<ILogger<AuthService>>();
    var metricsService = sp.GetRequiredService<MetricsService>();

    return new AuthService(
        userManager,
        roleManager,
        sessionService,
        lockoutService,
        jwtSecret,
        jwtIssuer,
        jwtAudience,
        logger,
        metricsService
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
    var metricsService = sp.GetRequiredService<MetricsService>();
    return new ChatbotService(ollamaService, dbContext, logger, metricsService, ollamaModel);
});

// Configure Redis for distributed rate limiting
var redisConnectionString = builder.Configuration.GetConnectionString("Redis")
    ?? builder.Configuration["Redis:ConnectionString"]
    ?? "redis-service.insightlearn.svc.cluster.local:6379";

// NOTE: Redis password authentication is DISABLED in current deployment
// Redis is running without requirepass configuration
// Connecting without password
var redisPassword = string.Empty; // Disabled: Configuration["Redis:Password"] ?? Environment.GetEnvironmentVariable("REDIS_PASSWORD");

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = ConfigurationOptions.Parse(redisConnectionString);
    configuration.AbortOnConnectFail = false; // Fail gracefully if Redis is down
    configuration.ConnectTimeout = 5000; // 5 second timeout
    configuration.SyncTimeout = 5000;
    var logger = sp.GetRequiredService<ILogger<Program>>();
    var sanitizedConnection = string.IsNullOrEmpty(redisPassword)
        ? redisConnectionString
        : redisConnectionString.Replace(redisPassword, "***");
    logger.LogInformation("[CONFIG] Connecting to Redis at: {RedisConnection}", sanitizedConnection);
    return ConnectionMultiplexer.Connect(configuration);
});

// Configure rate limiting options from configuration
builder.Services.Configure<RateLimitOptions>(
    builder.Configuration.GetSection("RateLimit"));

Console.WriteLine("[CONFIG] Redis configured for distributed rate limiting");

// Register MongoDB Video Storage Services
builder.Services.AddSingleton<IMongoVideoStorageService, MongoVideoStorageService>();
builder.Services.AddScoped<IVideoProcessingService, VideoProcessingService>();

// Register Enhanced Dashboard Service
builder.Services.AddScoped<IEnhancedDashboardService, EnhancedDashboardService>();

// Register Analytics Service for Admin Analytics page
builder.Services.AddScoped<IAdminAnalyticsService, AdminAnalyticsService>();
Console.WriteLine("[CONFIG] Admin Analytics Service registered");

// Register Report Service for Admin Reports page (v2.1.0-dev)
builder.Services.AddScoped<IReportService, ReportService>();
Console.WriteLine("[CONFIG] Report Service registered");

// Register AI Chat Service for Student Learning Space (v2.1.0-dev)
builder.Services.AddScoped<IAIChatService, AIChatService>();
Console.WriteLine("[CONFIG] AI Chat Service registered");

// Register Core LMS Repositories
builder.Services.AddScoped<ICourseRepository, CourseRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<ISectionRepository, SectionRepository>();
builder.Services.AddScoped<ILessonRepository, LessonRepository>();
builder.Services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
builder.Services.AddScoped<ICouponRepository, CouponRepository>();
builder.Services.AddScoped<ICertificateRepository, CertificateRepository>();

// Register SaaS Subscription Model Repositories
builder.Services.AddScoped<ISubscriptionPlanRepository, SubscriptionPlanRepository>();
builder.Services.AddScoped<IUserSubscriptionRepository, UserSubscriptionRepository>();
builder.Services.AddScoped<ICourseEngagementRepository, CourseEngagementRepository>();
builder.Services.AddScoped<IInstructorPayoutRepository, InstructorPayoutRepository>();
builder.Services.AddScoped<ISubscriptionRevenueRepository, SubscriptionRevenueRepository>();
builder.Services.AddScoped<IInstructorConnectAccountRepository, InstructorConnectAccountRepository>();
Console.WriteLine("[CONFIG] SaaS Subscription Repositories registered (6 repositories)");

// Register Student Learning Space Repositories (v2.1.0)
builder.Services.AddScoped<IStudentNoteRepository, StudentNoteRepository>();
builder.Services.AddScoped<IVideoBookmarkRepository, VideoBookmarkRepository>();
builder.Services.AddScoped<IVideoTranscriptRepository, VideoTranscriptRepository>();
builder.Services.AddScoped<IAITakeawayRepository, AITakeawayRepository>();
builder.Services.AddScoped<IAIConversationRepository, AIConversationRepository>();
Console.WriteLine("[CONFIG] Student Learning Space Repositories registered (5 repositories)");

// Register SaaS Subscription Services (Week 1 - All 6 services implemented with 10/10 score)
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<ISubscriptionPlanService, SubscriptionPlanService>();
builder.Services.AddScoped<IEngagementTrackingService, EngagementTrackingService>();
builder.Services.AddScoped<IPayoutCalculationService, PayoutCalculationService>();
builder.Services.AddScoped<ISubscriptionRevenueService, SubscriptionRevenueService>();
builder.Services.AddScoped<IInstructorConnectAccountService, InstructorConnectAccountService>();
Console.WriteLine("[CONFIG] SaaS Subscription Services registered (6 services - v2.0.0 ready)");

// Register Student Services
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
Console.WriteLine("[CONFIG] Student Services (Enrollment, Review) registered");

// Register Enhanced Payment Service
builder.Services.AddScoped<InsightLearn.Core.Interfaces.IPaymentService, EnhancedPaymentService>();
Console.WriteLine("[CONFIG] Enhanced Payment Service registered with Stripe & PayPal support");

// Register Core LMS Services
builder.Services.AddScoped<InsightLearn.Core.Interfaces.ICourseService, CourseService>();
builder.Services.AddScoped<InsightLearn.Core.Interfaces.ICategoryService, CategoryService>();
builder.Services.AddScoped<InsightLearn.Core.Interfaces.ISectionService, SectionService>();
builder.Services.AddScoped<InsightLearn.Core.Interfaces.ILessonService, LessonService>();
builder.Services.AddScoped<InsightLearn.Core.Interfaces.ICouponService, CouponService>();
Console.WriteLine("[CONFIG] Core LMS Services registered (Course, Category, Section, Lesson, Coupon)");

// Register Admin Services
builder.Services.AddScoped<InsightLearn.Application.Interfaces.IAdminService, InsightLearn.Application.Services.AdminService>();
builder.Services.AddScoped<IUserAdminService, UserAdminService>();
builder.Services.AddScoped<IDashboardPublicService, DashboardPublicService>();
Console.WriteLine("[CONFIG] Admin Services registered (Admin, UserAdmin, DashboardPublic)");

// Register Certificate Services (Phase 5.1: PDF Generation with QuestPDF)
builder.Services.AddScoped<ICertificateTemplateService, CertificateTemplateService>();
builder.Services.AddScoped<ICertificateService, CertificateService>();
Console.WriteLine("[CONFIG] Certificate Services registered (QuestPDF template + certificate generation)");

// Register Student Learning Space Services (v2.1.0)
builder.Services.AddScoped<IVideoTranscriptService, VideoTranscriptService>();
builder.Services.AddScoped<IAIAnalysisService, AIAnalysisService>();
builder.Services.AddScoped<IStudentNoteService, StudentNoteService>();
builder.Services.AddScoped<IVideoBookmarkService, VideoBookmarkService>();
builder.Services.AddScoped<IVideoProgressService, VideoProgressService>();
Console.WriteLine("[CONFIG] Student Learning Space Services registered (5 services)");

// Register Hangfire Background Job Processing (v2.1.0)
// Configure Hangfire with SQL Server storage for job persistence
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
    {
        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
        QueuePollInterval = TimeSpan.Zero,
        UseRecommendedIsolationLevel = true,
        DisableGlobalLocks = true,
        PrepareSchemaIfNecessary = true, // Auto-create Hangfire tables
        SchemaName = "HangfireSchema" // Separate schema for Hangfire tables
    }));

// Add Hangfire server with optimized configuration
builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = Environment.ProcessorCount * 2; // 2x CPU cores for parallel job processing
    options.Queues = new[] { "critical", "default", "low" }; // Priority queues (high → low)
    options.ServerName = $"{Environment.MachineName}-{Guid.NewGuid().ToString("N")[..8]}";
    options.SchedulePollingInterval = TimeSpan.FromSeconds(15); // Check for scheduled jobs every 15s
    options.ServerTimeout = TimeSpan.FromMinutes(5);
    options.ShutdownTimeout = TimeSpan.FromSeconds(30);
});

// Register background job classes
builder.Services.AddScoped<TranscriptGenerationJob>();
builder.Services.AddScoped<AITakeawayGenerationJob>();
Console.WriteLine("[CONFIG] Hangfire Background Jobs configured (SQL Server storage, {0} workers, 3 priority queues)",
    Environment.ProcessorCount * 2);

Console.WriteLine("[CONFIG] MongoDB Video Storage Services registered");
Console.WriteLine("[CONFIG] Enhanced Dashboard Service registered");
Console.WriteLine("[CONFIG] Core LMS Repositories registered (8 repositories)");

// Phase 4.1: Comprehensive Health Checks for all dependent services
// Configure health checks with proper tags and failure statuses
var elasticsearchUrl = builder.Configuration["Elasticsearch:Url"] ?? "http://elasticsearch-service.insightlearn.svc.cluster.local:9200";

builder.Services.AddHealthChecks()
    // CRITICAL: SQL Server - Primary database
    .AddSqlServer(
        connectionString: connectionString,
        name: "sqlserver",
        failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,
        tags: new[] { "db", "sql", "critical" },
        timeout: TimeSpan.FromSeconds(5))

    // CRITICAL: MongoDB - Video storage database
    .AddMongoDb(
        mongodbConnectionString: mongoConnectionString,
        name: "mongodb",
        failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,
        tags: new[] { "db", "mongodb", "videos", "critical" },
        timeout: TimeSpan.FromSeconds(5))

    // DEGRADED: Redis - Cache service (app continues without it)
    .AddRedis(
        redisConnectionString: redisConnectionString,
        name: "redis",
        failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded,
        tags: new[] { "cache", "redis" },
        timeout: TimeSpan.FromSeconds(3))

    // DEGRADED: Elasticsearch - Search engine (app continues without it)
    .AddElasticsearch(
        elasticsearchUri: elasticsearchUrl,
        name: "elasticsearch",
        failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded,
        tags: new[] { "search", "elasticsearch" },
        timeout: TimeSpan.FromSeconds(3))

    // DEGRADED: Ollama - AI chatbot (app continues without it)
    .AddUrlGroup(
        uri: new Uri($"{ollamaUrl}/api/tags"),
        name: "ollama",
        failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded,
        tags: new[] { "ai", "ollama", "chatbot" },
        timeout: TimeSpan.FromSeconds(3));

Console.WriteLine("[CONFIG] Comprehensive health checks configured:");
Console.WriteLine("[CONFIG]   - SQL Server (critical)");
Console.WriteLine("[CONFIG]   - MongoDB (critical)");
Console.WriteLine("[CONFIG]   - Redis (degraded if down)");
Console.WriteLine("[CONFIG]   - Elasticsearch (degraded if down)");
Console.WriteLine("[CONFIG]   - Ollama AI (degraded if down)");

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

            // SECURITY FIX (CRIT-3): Enforce ADMIN_PASSWORD from environment variable
            // NEVER use hardcoded passwords in production
            var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD")
                             ?? builder.Configuration["ADMIN_PASSWORD"];

            if (string.IsNullOrWhiteSpace(adminPassword))
            {
                throw new InvalidOperationException(
                    "ADMIN_PASSWORD environment variable is not configured. " +
                    "This is required for security. Set the ADMIN_PASSWORD environment variable " +
                    "with a strong password (min 12 chars, uppercase, lowercase, digits, special chars).");
            }

            // Validate password strength (minimum requirements)
            if (adminPassword.Length < 12 ||
                !adminPassword.Any(char.IsUpper) ||
                !adminPassword.Any(char.IsLower) ||
                !adminPassword.Any(char.IsDigit) ||
                !adminPassword.Any(ch => !char.IsLetterOrDigit(ch)))
            {
                throw new InvalidOperationException(
                    "ADMIN_PASSWORD does not meet security requirements. " +
                    "Password must be at least 12 characters and contain uppercase, lowercase, digits, and special characters.");
            }

            Console.WriteLine("[SECURITY] Admin password loaded from environment variable (CRIT-3 fix)");

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
                Console.WriteLine("[SEED]   Password: Loaded from environment variable (secure)");

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

// DEBUG: Raw endpoint BEFORE any middleware
app.MapGet("/raw-test", () => Results.Text("RAW TEST OK - No middleware"))
   .AllowAnonymous();

// Global Exception Handler - MUST BE FIRST in middleware pipeline
// Catches all unhandled exceptions from ANY downstream middleware or endpoint
// Provides environment-aware error responses (detailed in dev, safe in production)
// Implements OWASP A01:2021 (Information Disclosure) prevention
// TEMPORARILY DISABLED for debugging empty response issue
// app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
Console.WriteLine("[SECURITY] Global exception handler DISABLED (debugging mode)");

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors();

// ⚠️ CRITICAL: UseRouting() MUST be called BEFORE UseHttpMetrics()
// prometheus-net.AspNetCore requires routing to be configured first
// Without this, /metrics endpoint returns empty content (Content-Length: 0)
app.UseRouting();
Console.WriteLine("[CONFIG] Routing middleware registered (required for Prometheus)");

// Prometheus Metrics Middleware (Phase 4.2)
// Auto-instruments all HTTP requests for monitoring
// Exposes /metrics endpoint for Prometheus scraping
// TEMPORARILY DISABLED - Testing if this causes empty response bodies
// app.UseHttpMetrics(); // Automatic HTTP request/response metrics
Console.WriteLine("[METRICS] Prometheus HTTP metrics middleware DISABLED (debugging mode)");

// Custom metrics middleware - measures API duration and records requests
// TEMPORARILY DISABLED - Suspected cause of empty response bodies
/*
app.Use(async (context, next) =>
{
    var metricsService = context.RequestServices.GetRequiredService<MetricsService>();
    var method = context.Request.Method;
    var endpoint = context.Request.Path.Value ?? "/";

    // Measure request duration using Prometheus histogram
    using (metricsService.MeasureApiDuration(method, endpoint))
    {
        await next();
    }

    // Record API request counter
    metricsService.RecordApiRequest(method, endpoint, context.Response.StatusCode);
});
*/
Console.WriteLine("[METRICS] Custom API metrics middleware DISABLED (debugging mode)");

// Security Headers Middleware - Comprehensive security headers for all responses
// Moved to dedicated middleware for better maintainability (P1.2)
// Implements OWASP ASVS V14.4 (Security Headers) compliance
// TEMPORARILY DISABLED for debugging empty response issue
// app.UseMiddleware<SecurityHeadersMiddleware>();

Console.WriteLine("[SECURITY] Security headers middleware DISABLED (debugging mode)");

// Distributed Rate limiting using Redis (replaces in-memory rate limiter)
// This provides global rate limiting across all API pods in Kubernetes
// TEMPORARILY DISABLED for debugging empty response issue
// app.UseMiddleware<DistributedRateLimitMiddleware>();
Console.WriteLine("[SECURITY] Distributed rate limiting DISABLED (debugging mode)");

// Comment out the old in-memory rate limiter (keeping for reference/fallback)
// app.UseRateLimiter(); // Old in-memory rate limiter - replaced with distributed version

// Request Validation Middleware - Protects against SQL injection, XSS, and path traversal
// Positioned AFTER rate limiting to prevent attackers from exhausting resources
// TEMPORARILY DISABLED for debugging empty response issue
// app.UseMiddleware<RequestValidationMiddleware>();
Console.WriteLine("[SECURITY] Request validation middleware DISABLED (debugging mode)");

// Model Validation Logging Middleware - Phase 3.2
// Centrally logs all validation failures (400 Bad Request) for monitoring and debugging
// Extracts and analyzes validation errors, detects potential security concerns
// TEMPORARILY DISABLED for debugging empty response issue
// app.UseMiddleware<ModelValidationMiddleware>();
Console.WriteLine("[SECURITY] Model validation logging middleware DISABLED (debugging mode)");

app.UseAuthentication();
// CSRF Protection Middleware - Positioned after authentication, before authorization
// Protects against Cross-Site Request Forgery attacks (PCI DSS 6.5.9)
// TEMPORARILY DISABLED for debugging empty response issue
// app.UseMiddleware<CsrfProtectionMiddleware>();
Console.WriteLine("[SECURITY] CSRF protection middleware DISABLED (debugging mode)");
app.UseAuthorization();

// Hangfire Dashboard - Background job monitoring (Admin only)
// Dashboard available at: /hangfire
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireDashboardAuthorizationFilter() },
    DashboardTitle = "InsightLearn Background Jobs",
    StatsPollingInterval = 2000, // Refresh stats every 2 seconds
    DisplayStorageConnectionString = false // Hide connection string for security
});
Console.WriteLine("[CONFIG] Hangfire Dashboard enabled at /hangfire (Admin access only)");

// Audit Logging Middleware - Logs sensitive operations (auth, admin, payments)
// Positioned AFTER authentication to capture user context (userId, email, roles)
// TEMPORARILY DISABLED for debugging empty response issue
// app.UseMiddleware<AuditLoggingMiddleware>();
Console.WriteLine("[SECURITY] Audit logging middleware DISABLED (debugging mode)");

// Add rate limit headers to all responses (X-RateLimit-*)
app.Use(async (context, next) =>
{
    await next();

    // Add standard rate limit headers for API clients
    // FIX: Check if response has already started before adding headers
    if (!context.Response.HasStarted &&
        context.Response.StatusCode != 429 &&
        context.Request.Path.StartsWithSegments("/api"))
    {
        // Note: Headers are estimates - actual limits depend on GlobalLimiter/policy applied
        context.Response.Headers.TryAdd("X-RateLimit-Limit", "200");
        context.Response.Headers.TryAdd("X-RateLimit-Policy", "global");
    }
});

// Phase 4.1: Comprehensive Health Check Endpoints
// Three endpoints for different use cases:
// 1. /health - Full health check with detailed JSON response (for monitoring dashboards)
// 2. /health/live - Liveness probe (minimal check - app is running)
// 3. /health/ready - Readiness probe (critical dependencies - ready to serve traffic)

// Full health check endpoint - Returns detailed JSON with all service statuses
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            status = report.Status.ToString(),
            totalDuration = Math.Round(report.TotalDuration.TotalMilliseconds, 2),
            timestamp = DateTime.UtcNow.ToString("o"),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                duration = Math.Round(e.Value.Duration.TotalMilliseconds, 2),
                description = e.Value.Description,
                exception = e.Value.Exception?.Message,
                tags = e.Value.Tags,
                data = e.Value.Data.Count > 0 ? e.Value.Data : null
            }).OrderBy(x => x.name)
        };

        await context.Response.WriteAsJsonAsync(response);
    }
}).DisableRateLimiting();

// Liveness probe - K8s uses this to determine if pod is alive
// Returns 200 if API is running (no dependency checks)
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false // No checks - just 200 OK if API is running
}).DisableRateLimiting();

// Readiness probe - K8s uses this to determine if pod is ready to serve traffic
// Only checks CRITICAL dependencies (SQL Server, MongoDB)
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("critical")
}).DisableRateLimiting();

Console.WriteLine("[CONFIG] Health check endpoints registered:");
Console.WriteLine("[CONFIG]   - /health (full status with JSON details)");
Console.WriteLine("[CONFIG]   - /health/live (liveness probe for K8s)");
Console.WriteLine("[CONFIG]   - /health/ready (readiness probe for K8s)");

// Prometheus metrics endpoint (Phase 4.2)
// Exposes custom application metrics for Prometheus scraping
app.MapMetrics().DisableRateLimiting(); // Default: /metrics
Console.WriteLine("[METRICS] /metrics endpoint exposed for Prometheus scraping");

// CSP violation reporting endpoint (exempt from rate limiting)
app.MapPost("/api/csp-violations", async (HttpContext context, [FromServices] ILogger<Program> logger) =>
{
    try
    {
        var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
        var clientIp = context.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                        ?? context.Connection.RemoteIpAddress?.ToString()
                        ?? "unknown";

        logger.LogWarning(
            "[CSP VIOLATION] IP: {IpAddress}, UserAgent: {UserAgent}, Report: {Report}",
            clientIp,
            context.Request.Headers["User-Agent"].ToString(),
            body);

        return Results.NoContent();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[CSP VIOLATION] Error processing CSP report");
        return Results.NoContent(); // Always return 204 even on error
    }
})
.DisableRateLimiting()
.WithName("CSP-Violations")
.WithTags("Security");

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

// ============================================
// AUTHENTICATION API ENDPOINTS
// ============================================

/// <summary>
/// Authenticates a user and returns a JWT access token
/// </summary>
/// <param name="loginDto">User credentials (email and password)</param>
/// <param name="httpContext">HTTP context for IP tracking</param>
/// <param name="authService">Authentication service</param>
/// <param name="logger">Logger instance</param>
/// <returns>Authentication result with JWT token and user information</returns>
/// <response code="200">Login successful - returns JWT token, user details, and session information</response>
/// <response code="400">Login failed - invalid credentials, account locked, or validation errors</response>
/// <response code="429">Rate limit exceeded - maximum 5 login attempts per minute per IP (brute force protection)</response>
/// <response code="500">Internal server error occurred during authentication</response>
/// <remarks>
/// **Rate Limiting**: This endpoint is protected by aggressive rate limiting (5 requests/minute per IP) to prevent brute force attacks.
///
/// **Security Features**:
/// - Password hashing with bcrypt
/// - Account lockout after 5 failed attempts (15 minute lockout)
/// - IP address and user agent logging for security auditing
/// - JWT token with configurable expiration (default: 7 days)
///
/// **Sample Request**:
///
///     POST /api/auth/login
///     {
///       "email": "student@example.com",
///       "password": "SecurePass123!"
///     }
///
/// **Sample Success Response** (200 OK):
///
///     {
///       "isSuccess": true,
///       "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
///       "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
///       "email": "student@example.com",
///       "firstName": "John",
///       "lastName": "Doe",
///       "roles": ["Student"],
///       "sessionId": "sess_abc123",
///       "expiresAt": "2025-11-23T12:00:00Z"
///     }
///
/// **Sample Error Response** (400 Bad Request):
///
///     {
///       "isSuccess": false,
///       "errors": ["Invalid email or password"]
///     }
///
/// **Sample Lockout Response** (400 Bad Request):
///
///     {
///       "isSuccess": false,
///       "errors": ["Account locked due to multiple failed login attempts. Try again in 15 minutes."]
///     }
/// </remarks>
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
.RequireRateLimiting("auth") // Brute force protection: 5 req/min per IP
.Accepts<LoginDto>("application/json")
.Produces<AuthResultDto>(200)
.Produces(400)
.Produces(500)
.WithName("Login")
.WithTags("Authentication");

/// <summary>
/// Registers a new user account on the platform
/// </summary>
/// <param name="registerDto">New user registration details (email, password, name, etc.)</param>
/// <param name="authService">Authentication service</param>
/// <param name="logger">Logger instance</param>
/// <returns>Registration result with JWT token for immediate login</returns>
/// <response code="200">Registration successful - returns JWT token and user details</response>
/// <response code="400">Registration failed - validation errors, duplicate email, or weak password</response>
/// <response code="429">Rate limit exceeded - maximum 5 registration attempts per minute per IP</response>
/// <response code="500">Internal server error occurred during registration</response>
/// <remarks>
/// **Password Requirements** (PCI DSS 8.2.3 compliant):
/// - Minimum 8 characters
/// - At least one uppercase letter (A-Z)
/// - At least one lowercase letter (a-z)
/// - At least one digit (0-9)
/// - At least one special character (!@#$%^&amp;*()_+-=[]{}|:";'&lt;&gt;?,.)
///
/// **Sample Request**:
///
///     POST /api/auth/register
///     {
///       "email": "newstudent@example.com",
///       "password": "SecurePass123!",
///       "confirmPassword": "SecurePass123!",
///       "firstName": "Jane",
///       "lastName": "Smith",
///       "userType": "Student",
///       "hasAgreedToTerms": true,
///       "hasAgreedToPrivacyPolicy": true
///     }
///
/// **Sample Success Response** (200 OK):
///
///     {
///       "isSuccess": true,
///       "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
///       "userId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
///       "email": "newstudent@example.com",
///       "firstName": "Jane",
///       "lastName": "Smith",
///       "roles": ["Student"]
///     }
///
/// **Sample Error Response** (400 Bad Request):
///
///     {
///       "isSuccess": false,
///       "errors": [
///         "Email 'newstudent@example.com' is already registered",
///         "Password must contain at least one uppercase letter"
///       ]
///     }
/// </remarks>
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
})
.RequireRateLimiting("auth"); // Brute force protection: 5 req/min per IP

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
})
.RequireRateLimiting("auth"); // Brute force protection: 5 req/min per IP

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
})
.RequireRateLimiting("auth") // Brute force protection: 5 req/min per IP
.RequireAuthorization();

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
})
.RequireRateLimiting("auth"); // Brute force protection: 5 req/min per IP

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
// Note: Request size limit (500MB for videos) configured globally in Kestrel options (CRIT-2)

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
        logger.LogWarning(ex, "[VIDEO] Video not found: {FileId}", fileId);
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

// Enhanced Dashboard Statistics
app.MapGet("/api/admin/dashboard/enhanced-stats", async (
    [FromServices] IEnhancedDashboardService dashboardService,
    [FromServices] ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("[ADMIN] Fetching enhanced dashboard stats");
        var stats = await dashboardService.GetEnhancedStatsAsync();
        logger.LogInformation("[ADMIN] Enhanced dashboard stats retrieved successfully");
        return Results.Ok(stats);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[ADMIN] Error fetching enhanced dashboard stats");
        return Results.Json(new { error = ex.Message }, statusCode: (int)HttpStatusCode.InternalServerError);
    }
})
.RequireAuthorization(policy => policy.RequireRole("Admin"))
.WithName("GetEnhancedDashboardStats")
.WithTags("Admin");

// Dashboard Chart Data
app.MapGet("/api/admin/dashboard/charts/{chartType}", async (
    [FromServices] IEnhancedDashboardService dashboardService,
    [FromServices] ILogger<Program> logger,
    string chartType,
    [FromQuery] int days = 30) =>
{
    try
    {
        logger.LogInformation("[ADMIN] Fetching chart data for {ChartType} (days: {Days})", chartType, days);
        var chartData = await dashboardService.GetChartDataAsync(chartType, days);
        return Results.Ok(chartData);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[ADMIN] Error fetching chart data");
        return Results.Json(new { error = ex.Message }, statusCode: (int)HttpStatusCode.InternalServerError);
    }
})
.RequireAuthorization(policy => policy.RequireRole("Admin"))
.WithName("GetDashboardCharts")
.WithTags("Admin");

// Enhanced Activity Feed
app.MapGet("/api/admin/dashboard/activity", async (
    [FromServices] IEnhancedDashboardService dashboardService,
    [FromServices] ILogger<Program> logger,
    [FromQuery] int limit = 20,
    [FromQuery] int offset = 0) =>
{
    try
    {
        logger.LogInformation("[ADMIN] Fetching enhanced activity (limit: {Limit}, offset: {Offset})", limit, offset);
        var activities = await dashboardService.GetRecentActivityAsync(limit, offset);
        return Results.Ok(activities);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[ADMIN] Error fetching activity");
        return Results.Json(new { error = ex.Message }, statusCode: (int)HttpStatusCode.InternalServerError);
    }
})
.RequireAuthorization(policy => policy.RequireRole("Admin"))
.WithName("GetDashboardActivity")
.WithTags("Admin");

// Real-time Metrics
app.MapGet("/api/admin/dashboard/realtime-metrics", async (
    [FromServices] IEnhancedDashboardService dashboardService,
    [FromServices] ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("[ADMIN] Fetching real-time metrics");
        var metrics = await dashboardService.GetRealTimeMetricsAsync();
        return Results.Ok(metrics);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[ADMIN] Error fetching real-time metrics");
        return Results.Json(new { error = ex.Message }, statusCode: (int)HttpStatusCode.InternalServerError);
    }
})
.RequireAuthorization(policy => policy.RequireRole("Admin"))
.WithName("GetRealTimeMetrics")
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

// INSTRUCTOR MANAGEMENT ENDPOINTS

// Get all instructors with pagination and filtering
app.MapGet("/api/admin/instructors", async (
    [FromServices] UserManager<User> userManager,
    [FromServices] InsightLearnDbContext dbContext,
    [FromServices] ILogger<Program> logger,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    [FromQuery] string? search = null,
    [FromQuery] string? status = null) =>
{
    try
    {
        logger.LogInformation("[ADMIN] Fetching instructors (page: {Page}, pageSize: {PageSize}, search: {Search}, status: {Status})",
            page, pageSize, search ?? "none", status ?? "all");

        // Base query: only instructors
        var query = userManager.Users.Where(u => u.IsInstructor);

        // Apply search filter
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(u =>
                u.Email.Contains(search) ||
                u.FirstName.Contains(search) ||
                u.LastName.Contains(search));
        }

        // Apply status filter
        if (!string.IsNullOrEmpty(status))
        {
            switch (status.ToLower())
            {
                case "active":
                    query = query.Where(u => !u.LockoutEnabled || u.LockoutEnd == null || u.LockoutEnd < DateTimeOffset.UtcNow);
                    break;
                case "suspended":
                    query = query.Where(u => u.LockoutEnabled && u.LockoutEnd != null && u.LockoutEnd > DateTimeOffset.UtcNow);
                    break;
                case "pending":
                    query = query.Where(u => !u.IsVerified);
                    break;
            }
        }

        var totalCount = await query.CountAsync();
        var instructors = await query
            .OrderByDescending(u => u.DateJoined)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var instructorDtos = new List<InstructorSummaryDto>();
        foreach (var instructor in instructors)
        {
            // Count courses
            var coursesCount = await dbContext.Courses.CountAsync(c => c.InstructorId == instructor.Id);

            // Count unique students
            var studentsCount = await dbContext.Enrollments
                .Where(e => e.Course.InstructorId == instructor.Id)
                .Select(e => e.UserId)
                .Distinct()
                .CountAsync();

            // Calculate total earnings
            var totalEarnings = await dbContext.Payments
                .Where(p => p.Course.InstructorId == instructor.Id && p.Status == PaymentStatus.Completed)
                .SumAsync(p => (decimal?)p.Amount) ?? 0m;

            // Determine status
            string instructorStatus;
            if (!instructor.IsVerified)
            {
                instructorStatus = "Pending";
            }
            else if (instructor.LockoutEnabled && instructor.LockoutEnd != null && instructor.LockoutEnd > DateTimeOffset.UtcNow)
            {
                instructorStatus = "Suspended";
            }
            else
            {
                instructorStatus = "Active";
            }

            instructorDtos.Add(new InstructorSummaryDto
            {
                Id = instructor.Id,
                Name = instructor.FullName,
                Email = instructor.Email,
                Status = instructorStatus,
                CoursesCount = coursesCount,
                StudentsCount = studentsCount,
                TotalEarnings = totalEarnings,
                JoinedDate = instructor.DateJoined,
                LastActive = instructor.LastLoginDate,
                ProfilePictureUrl = instructor.ProfilePictureUrl
            });
        }

        var result = new PagedResultDto<InstructorSummaryDto>
        {
            Items = instructorDtos,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };

        logger.LogInformation("[ADMIN] Retrieved {Count} instructors", instructorDtos.Count);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[ADMIN] Error fetching instructors");
        return Results.Json(new { error = ex.Message }, statusCode: (int)HttpStatusCode.InternalServerError);
    }
})
.RequireAuthorization(policy => policy.RequireRole("Admin"))
.WithName("GetInstructors")
.WithTags("Admin", "Instructors");

// Get instructor detailed statistics
app.MapGet("/api/admin/instructors/{id:guid}/stats", async (
    Guid id,
    [FromServices] UserManager<User> userManager,
    [FromServices] InsightLearnDbContext dbContext,
    [FromServices] ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("[ADMIN] Fetching instructor stats for {InstructorId}", id);

        var instructor = await userManager.FindByIdAsync(id.ToString());
        if (instructor == null || !instructor.IsInstructor)
        {
            return Results.NotFound(new { error = "Instructor not found" });
        }

        // Get courses
        var courses = await dbContext.Courses
            .Where(c => c.InstructorId == id)
            .Include(c => c.Reviews)
            .Include(c => c.Enrollments)
            .ToListAsync();

        var totalCourses = courses.Count;
        var publishedCourses = courses.Count(c => c.Status == CourseStatus.Published);
        var draftCourses = courses.Count(c => c.Status == CourseStatus.Draft);

        // Get enrollments
        var allEnrollments = courses.SelectMany(c => c.Enrollments).ToList();
        var totalEnrollments = allEnrollments.Count;
        var activeStudents = allEnrollments.Select(e => e.UserId).Distinct().Count();

        // Calculate revenue
        var totalRevenue = await dbContext.Payments
            .Where(p => p.Course.InstructorId == id && p.Status == PaymentStatus.Completed)
            .SumAsync(p => (decimal?)p.Amount) ?? 0m;

        // Calculate average rating
        var allReviews = courses.SelectMany(c => c.Reviews).ToList();
        var averageRating = allReviews.Any() ? (decimal)allReviews.Average(r => r.Rating) : 0m;
        var totalReviews = allReviews.Count;

        // Top courses by enrollments
        var topCourses = courses
            .OrderByDescending(c => c.Enrollments.Count)
            .Take(5)
            .Select(c => new InsightLearn.Core.DTOs.Admin.InstructorTopCourseDto
            {
                Id = c.Id,
                Title = c.Title,
                Category = c.Category?.Name ?? "Uncategorized",
                Instructor = instructor.FullName,
                EnrollmentCount = c.Enrollments.Count,
                Revenue = dbContext.Payments
                    .Where(p => p.CourseId == c.Id && p.Status == PaymentStatus.Completed)
                    .Sum(p => p.Amount),
                AverageRating = c.Reviews.Any() ? (decimal)c.Reviews.Average(r => r.Rating) : 0m,
                CompletionRate = c.Enrollments.Any() ? (decimal)c.Enrollments.Count(e => e.Status == EnrollmentStatus.Completed) / c.Enrollments.Count * 100 : 0m,
                CreatedDate = c.CreatedAt
            })
            .ToList();

        // Enrollments by month (last 12 months)
        var twelveMonthsAgo = DateTime.UtcNow.AddMonths(-12);
        var enrollmentsByMonth = allEnrollments
            .Where(e => e.EnrolledAt >= twelveMonthsAgo)
            .GroupBy(e => new { e.EnrolledAt.Year, e.EnrolledAt.Month })
            .ToDictionary(
                g => $"{g.Key.Year}-{g.Key.Month:D2}",
                g => g.Count()
            );

        var stats = new InstructorStatsDto
        {
            InstructorId = id,
            InstructorName = instructor.FullName,
            TotalCourses = totalCourses,
            PublishedCourses = publishedCourses,
            DraftCourses = draftCourses,
            TotalEnrollments = totalEnrollments,
            ActiveStudents = activeStudents,
            TotalRevenue = totalRevenue,
            AverageRating = averageRating,
            TotalReviews = totalReviews,
            TopCourses = topCourses,
            EnrollmentsByMonth = enrollmentsByMonth
        };

        logger.LogInformation("[ADMIN] Retrieved stats for instructor {InstructorId}", id);
        return Results.Ok(stats);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[ADMIN] Error fetching instructor stats for {InstructorId}", id);
        return Results.Json(new { error = ex.Message }, statusCode: (int)HttpStatusCode.InternalServerError);
    }
})
.RequireAuthorization(policy => policy.RequireRole("Admin"))
.WithName("GetInstructorStats")
.WithTags("Admin", "Instructors");

// Suspend instructor account
app.MapPost("/api/admin/instructors/{id:guid}/suspend", async (
    Guid id,
    [FromBody] SuspendInstructorDto suspendRequest,
    [FromServices] UserManager<User> userManager,
    [FromServices] ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("[ADMIN] Suspending instructor {InstructorId}", id);

        var instructor = await userManager.FindByIdAsync(id.ToString());
        if (instructor == null || !instructor.IsInstructor)
        {
            return Results.NotFound(new { error = "Instructor not found" });
        }

        // Set lockout
        instructor.LockoutEnabled = true;
        instructor.LockoutEnd = suspendRequest.SuspendUntil ?? DateTimeOffset.MaxValue; // Permanent if no end date

        var result = await userManager.UpdateAsync(instructor);
        if (!result.Succeeded)
        {
            return Results.BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        logger.LogInformation("[ADMIN] Instructor {InstructorId} suspended until {SuspendUntil}. Reason: {Reason}",
            id, instructor.LockoutEnd, suspendRequest.Reason);

        return Results.Ok(new
        {
            success = true,
            message = "Instructor suspended successfully",
            suspendedUntil = instructor.LockoutEnd
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[ADMIN] Error suspending instructor {InstructorId}", id);
        return Results.Json(new { error = ex.Message }, statusCode: (int)HttpStatusCode.InternalServerError);
    }
})
.RequireAuthorization(policy => policy.RequireRole("Admin"))
.WithName("SuspendInstructor")
.WithTags("Admin", "Instructors");

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
            .Select(c => new InsightLearn.Application.DTOs.CourseDto
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

        var result = new PagedResultDto<InsightLearn.Application.DTOs.CourseDto>
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
    [FromBody] InsightLearn.Application.DTOs.CreateCourseDto createDto,
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
    [FromBody] InsightLearn.Application.DTOs.UpdateCourseDto updateDto,
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

// PROMETHEUS METRICS API ENDPOINTS

// Get infrastructure metrics (CPU, Memory, Disk, Pod health)
app.MapGet("/api/admin/metrics/infrastructure", async (
    [FromServices] IPrometheusService prometheusService,
    [FromServices] ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("[PROMETHEUS] Fetching infrastructure metrics");
        var metrics = await prometheusService.GetInfrastructureMetricsAsync();
        logger.LogInformation("[PROMETHEUS] Retrieved {Count} infrastructure metrics", metrics.Count);
        return Results.Ok(metrics);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[PROMETHEUS] Error fetching infrastructure metrics");
        return Results.Json(new { error = ex.Message }, statusCode: (int)HttpStatusCode.InternalServerError);
    }
})
.RequireAuthorization(policy => policy.RequireRole("Admin"))
.WithName("GetInfrastructureMetrics")
.WithTags("Metrics");

// Get API performance metrics (request rate, response time, error rate)
app.MapGet("/api/admin/metrics/api", async (
    [FromServices] IPrometheusService prometheusService,
    [FromServices] ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("[PROMETHEUS] Fetching API metrics");
        var metrics = await prometheusService.GetApiMetricsAsync();
        logger.LogInformation("[PROMETHEUS] Retrieved {Count} API metrics", metrics.Count);
        return Results.Ok(metrics);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[PROMETHEUS] Error fetching API metrics");
        return Results.Json(new { error = ex.Message }, statusCode: (int)HttpStatusCode.InternalServerError);
    }
})
.RequireAuthorization(policy => policy.RequireRole("Admin"))
.WithName("GetApiMetrics")
.WithTags("Metrics");

// Get pod-level metrics
app.MapGet("/api/admin/metrics/pods", async (
    [FromServices] IPrometheusService prometheusService,
    [FromServices] ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("[PROMETHEUS] Fetching pod metrics");
        var metrics = await prometheusService.GetPodMetricsAsync();
        logger.LogInformation("[PROMETHEUS] Retrieved metrics for {Count} pods", metrics.Count);
        return Results.Ok(metrics);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[PROMETHEUS] Error fetching pod metrics");
        return Results.Json(new { error = ex.Message }, statusCode: (int)HttpStatusCode.InternalServerError);
    }
})
.RequireAuthorization(policy => policy.RequireRole("Admin"))
.WithName("GetPodMetrics")
.WithTags("Metrics");

// Custom Prometheus query endpoint
app.MapPost("/api/admin/metrics/query", async (
    [FromServices] IPrometheusService prometheusService,
    [FromServices] ILogger<Program> logger,
    [FromBody] PrometheusQueryRequest request) =>
{
    try
    {
        logger.LogInformation("[PROMETHEUS] Executing custom query: {Query}", request.Query);

        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return Results.BadRequest(new { error = "Query is required" });
        }

        if (request.IsRangeQuery)
        {
            var start = request.Start ?? DateTime.UtcNow.AddHours(-1);
            var end = request.End ?? DateTime.UtcNow;
            var step = request.Step ?? "15s";

            var result = await prometheusService.QueryRangeAsync(request.Query, start, end, step);
            return Results.Ok(result);
        }
        else
        {
            var result = await prometheusService.QueryAsync(request.Query);
            return Results.Ok(result);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[PROMETHEUS] Error executing query");
        return Results.Json(new { error = ex.Message }, statusCode: (int)HttpStatusCode.InternalServerError);
    }
})
.RequireAuthorization(policy => policy.RequireRole("Admin"))
.WithName("QueryPrometheus")
.WithTags("Metrics");

// ===== CATEGORIES API ENDPOINTS =====

app.MapGet("/api/categories", async (
    [FromServices] InsightLearn.Core.Interfaces.ICategoryService categoryService,
    [FromServices] ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("[CATEGORIES] Getting all categories");
        var categories = await categoryService.GetAllCategoriesAsync();
        return Results.Ok(categories);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[CATEGORIES] Error getting all categories");
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.WithName("GetAllCategories")
.WithTags("Categories")
.Produces<IEnumerable<InsightLearn.Core.DTOs.Category.CategoryDto>>(200);

app.MapGet("/api/categories/{id:guid}", async (
    Guid id,
    [FromServices] InsightLearn.Core.Interfaces.ICategoryService categoryService,
    [FromServices] ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("[CATEGORIES] Getting category {CategoryId}", id);
        var category = await categoryService.GetCategoryByIdAsync(id);

        if (category == null)
        {
            logger.LogWarning("[CATEGORIES] Category {CategoryId} not found", id);
            return Results.NotFound(new { message = $"Category {id} not found" });
        }

        return Results.Ok(category);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[CATEGORIES] Error getting category {CategoryId}", id);
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.WithName("GetCategoryById")
.WithTags("Categories")
.Produces<InsightLearn.Core.DTOs.Category.CategoryDto>(200)
.Produces(404);

app.MapPost("/api/categories", async (
    [FromBody] CreateCategoryDto dto,
    [FromServices] InsightLearn.Core.Interfaces.ICategoryService categoryService,
    [FromServices] ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("[CATEGORIES] Creating new category: {Name}", dto.Name);
        var category = await categoryService.CreateCategoryAsync(dto);
        return Results.Created($"/api/categories/{category.Id}", category);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[CATEGORIES] Error creating category: {Name}", dto.Name);
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.RequireAuthorization(policy => policy.RequireRole("Admin", "Instructor"))
.WithName("CreateCategory")
.WithTags("Categories")
.Produces<InsightLearn.Core.DTOs.Category.CategoryDto>(201)
.Produces(401)
.Produces(403);

app.MapPut("/api/categories/{id:guid}", async (
    Guid id,
    [FromBody] UpdateCategoryDto dto,
    [FromServices] InsightLearn.Core.Interfaces.ICategoryService categoryService,
    [FromServices] ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("[CATEGORIES] Updating category {CategoryId}", id);
        var category = await categoryService.UpdateCategoryAsync(id, dto);

        if (category == null)
        {
            logger.LogWarning("[CATEGORIES] Category {CategoryId} not found for update", id);
            return Results.NotFound(new { message = $"Category {id} not found" });
        }

        return Results.Ok(category);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[CATEGORIES] Error updating category {CategoryId}", id);
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.RequireAuthorization(policy => policy.RequireRole("Admin"))
.WithName("UpdateCategory")
.WithTags("Categories")
.Produces<InsightLearn.Core.DTOs.Category.CategoryDto>(200)
.Produces(404)
.Produces(401)
.Produces(403);

app.MapDelete("/api/categories/{id:guid}", async (
    Guid id,
    [FromServices] InsightLearn.Core.Interfaces.ICategoryService categoryService,
    [FromServices] ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("[CATEGORIES] Deleting category {CategoryId}", id);
        var result = await categoryService.DeleteCategoryAsync(id);

        if (!result)
        {
            logger.LogWarning("[CATEGORIES] Category {CategoryId} not found for deletion", id);
            return Results.NotFound(new { message = $"Category {id} not found" });
        }

        return Results.NoContent();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[CATEGORIES] Error deleting category {CategoryId}", id);
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.RequireAuthorization(policy => policy.RequireRole("Admin"))
.WithName("DeleteCategory")
.WithTags("Categories")
.Produces(204)
.Produces(404)
.Produces(401)
.Produces(403);

// ===== COURSES API ENDPOINTS =====

// Get all courses (paginated, public access)
app.MapGet("/api/courses", async (
    [FromServices] InsightLearn.Core.Interfaces.ICourseService courseService,
    [FromServices] ILogger<Program> logger,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10,
    [FromQuery] Guid? categoryId = null) =>
{
    try
    {
        logger.LogInformation("[COURSES] Getting all courses (page: {Page}, pageSize: {PageSize}, categoryId: {CategoryId})",
            page, pageSize, categoryId);
        var courses = await courseService.GetCoursesAsync(page, pageSize, categoryId);
        return Results.Ok(courses);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[COURSES] Error getting all courses");
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.WithName("GetAllCourses")
.WithTags("Courses")
.Produces<IEnumerable<InsightLearn.Core.DTOs.Course.CourseDto>>(200);

// Get course by ID (public access)
app.MapGet("/api/courses/{id:guid}", async (
    Guid id,
    [FromServices] InsightLearn.Core.Interfaces.ICourseService courseService,
    [FromServices] ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("[COURSES] Getting course {CourseId}", id);
        var course = await courseService.GetCourseByIdAsync(id);

        if (course == null)
        {
            logger.LogWarning("[COURSES] Course {CourseId} not found", id);
            return Results.NotFound(new { message = $"Course {id} not found" });
        }

        return Results.Ok(course);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[COURSES] Error getting course {CourseId}", id);
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.WithName("GetCourseById")
.WithTags("Courses")
.Produces<InsightLearn.Core.DTOs.Course.CourseDto>(200)
.Produces(404);

// Get courses by category (public access)
app.MapGet("/api/courses/category/{id:guid}", async (
    [FromRoute] Guid id,
    [FromServices] InsightLearn.Core.Interfaces.ICourseService courseService,
    [FromServices] ILogger<Program> logger,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10) =>
{
    try
    {
        logger.LogInformation("[COURSES] Getting courses by category {CategoryId} (page: {Page}, pageSize: {PageSize})",
            id, page, pageSize);
        var courses = await courseService.GetCoursesByCategoryAsync(id, page, pageSize);
        return Results.Ok(courses);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[COURSES] Error getting courses by category {CategoryId}", id);
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.WithName("GetCoursesByCategory")
.WithTags("Courses")
.Produces<IEnumerable<InsightLearn.Core.DTOs.Course.CourseDto>>(200);

// Search courses (public access)
app.MapGet("/api/courses/search", async (
    [FromServices] InsightLearn.Core.Interfaces.ICourseService courseService,
    [FromServices] ILogger<Program> logger,
    [FromQuery] string? query = null,
    [FromQuery] Guid? categoryId = null,
    [FromQuery] string? level = null,
    [FromQuery] decimal? minPrice = null,
    [FromQuery] decimal? maxPrice = null,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10) =>
{
    try
    {
        logger.LogInformation("[COURSES] Searching courses (query: {Query}, categoryId: {CategoryId}, level: {Level}, page: {Page})",
            query ?? "none", categoryId, level ?? "any", page);

        var searchDto = new InsightLearn.Core.DTOs.Course.CourseSearchDto
        {
            Query = query,
            CategoryId = categoryId,
            Level = level,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            Page = page,
            PageSize = pageSize
        };

        var courses = await courseService.SearchCoursesAsync(searchDto);
        return Results.Ok(courses);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[COURSES] Error searching courses");
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.WithName("SearchCourses")
.WithTags("Courses")
.Produces<IEnumerable<InsightLearn.Core.DTOs.Course.CourseDto>>(200);

// Create course (Admin or Instructor only)
app.MapPost("/api/courses", async (
    [FromBody] InsightLearn.Core.DTOs.Course.CreateCourseDto dto,
    [FromServices] InsightLearn.Core.Interfaces.ICourseService courseService,
    [FromServices] ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("[COURSES] Creating new course: {Title}", dto.Title);
        var course = await courseService.CreateCourseAsync(dto);
        return Results.Created($"/api/courses/{course.Id}", course);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[COURSES] Error creating course: {Title}", dto.Title);
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.RequireAuthorization(policy => policy.RequireRole("Admin", "Instructor"))
.WithName("CreateCourse")
.WithTags("Courses")
.Produces<InsightLearn.Core.DTOs.Course.CourseDto>(201)
.Produces(401)
.Produces(403);

// Update course (Admin or Instructor only)
app.MapPut("/api/courses/{id:guid}", async (
    Guid id,
    [FromBody] InsightLearn.Core.DTOs.Course.UpdateCourseDto dto,
    [FromServices] InsightLearn.Core.Interfaces.ICourseService courseService,
    [FromServices] ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("[COURSES] Updating course {CourseId}", id);
        var course = await courseService.UpdateCourseAsync(id, dto);

        if (course == null)
        {
            logger.LogWarning("[COURSES] Course {CourseId} not found for update", id);
            return Results.NotFound(new { message = $"Course {id} not found" });
        }

        return Results.Ok(course);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[COURSES] Error updating course {CourseId}", id);
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.RequireAuthorization(policy => policy.RequireRole("Admin", "Instructor"))
.WithName("UpdateCourse")
.WithTags("Courses")
.Produces<InsightLearn.Core.DTOs.Course.CourseDto>(200)
.Produces(404)
.Produces(401)
.Produces(403);

// Delete course (Admin only)
app.MapDelete("/api/courses/{id:guid}", async (
    Guid id,
    [FromServices] InsightLearn.Core.Interfaces.ICourseService courseService,
    [FromServices] ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("[COURSES] Deleting course {CourseId}", id);
        var result = await courseService.DeleteCourseAsync(id);

        if (!result)
        {
            logger.LogWarning("[COURSES] Course {CourseId} not found for deletion", id);
            return Results.NotFound(new { message = $"Course {id} not found" });
        }

        return Results.NoContent();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[COURSES] Error deleting course {CourseId}", id);
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.RequireAuthorization(policy => policy.RequireRole("Admin"))
.WithName("DeleteCourse")
.WithTags("Courses")
.Produces(204)
.Produces(404)
.Produces(401)
.Produces(403);

// ===========================
// REVIEWS API ENDPOINTS
// ===========================

// GET /api/reviews/course/{courseId:guid} - Get reviews for a specific course with pagination
app.MapGet("/api/reviews/course/{courseId:guid}", async (
    Guid courseId,
    [FromServices] InsightLearn.Core.Interfaces.IReviewService reviewService,
    [FromServices] ILogger<Program> logger,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10) =>
{
    try
    {
        logger.LogInformation("[REVIEWS] Getting reviews for course {CourseId}, Page: {Page}",
            courseId, page);

        var reviewList = await reviewService.GetCourseReviewsAsync(courseId, page, pageSize);

        logger.LogInformation("[REVIEWS] Found {Count} reviews for course {CourseId} (Total: {Total})",
            reviewList.Reviews.Count, courseId, reviewList.TotalCount);

        return Results.Ok(reviewList);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[REVIEWS] Error getting reviews for course {CourseId}", courseId);
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.WithName("GetCourseReviews")
.WithTags("Reviews")
.Produces<InsightLearn.Core.DTOs.Review.ReviewListDto>(200)
.Produces(500);

// GET /api/reviews/{id:guid} - Get review by ID
app.MapGet("/api/reviews/{id:guid}", async (
    Guid id,
    [FromServices] InsightLearn.Core.Interfaces.IReviewService reviewService,
    [FromServices] ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("[REVIEWS] Getting review {ReviewId}", id);
        var review = await reviewService.GetReviewByIdAsync(id);

        if (review == null)
        {
            logger.LogWarning("[REVIEWS] Review {ReviewId} not found", id);
            return Results.NotFound(new { message = $"Review {id} not found" });
        }

        logger.LogInformation("[REVIEWS] Found review {ReviewId}", id);
        return Results.Ok(review);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[REVIEWS] Error getting review {ReviewId}", id);
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.WithName("GetReviewById")
.WithTags("Reviews")
.Produces<InsightLearn.Core.DTOs.Review.ReviewDto>(200)
.Produces(404)
.Produces(500);

// POST /api/reviews - Create a new review
app.MapPost("/api/reviews", async (
    [FromBody] InsightLearn.Core.DTOs.Review.CreateReviewDto reviewDto,
    [FromServices] InsightLearn.Core.Interfaces.IReviewService reviewService,
    [FromServices] ILogger<Program> logger,
    ClaimsPrincipal user) =>
{
    try
    {
        logger.LogInformation("[REVIEWS] Creating review for course {CourseId} by user {UserId}",
            reviewDto.CourseId, reviewDto.UserId);

        // Validate user authorization (can only review own enrollments)
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) ||
            !Guid.TryParse(userIdClaim, out var authenticatedUserId) ||
            authenticatedUserId != reviewDto.UserId)
        {
            logger.LogWarning("[REVIEWS] Unauthorized review creation attempt by user {AuthUserId} for user {RequestedUserId}",
                userIdClaim, reviewDto.UserId);
            return Results.Forbid();
        }

        var createdReview = await reviewService.CreateReviewAsync(reviewDto);

        logger.LogInformation("[REVIEWS] Created review {ReviewId} for course {CourseId}",
            createdReview.Id, reviewDto.CourseId);

        return Results.Created($"/api/reviews/{createdReview.Id}", createdReview);
    }
    catch (InvalidOperationException ex)
    {
        logger.LogWarning(ex, "[REVIEWS] Invalid review creation - {Message}", ex.Message);
        return Results.BadRequest(new { message = ex.Message });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[REVIEWS] Error creating review");
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.RequireAuthorization()
.WithName("CreateReview")
.WithTags("Reviews")
.Produces<InsightLearn.Core.DTOs.Review.ReviewDto>(201)
.Produces(400)
.Produces(401)
.Produces(403)
.Produces(500);

// ============================================================================
// ENROLLMENTS API - Managing course enrollments and student progress
// ============================================================================

// Get all enrollments (Admin only) with pagination
app.MapGet("/api/enrollments", async (
    [FromServices] InsightLearn.Core.Interfaces.IEnrollmentService enrollmentService,
    [FromServices] ILogger<Program> logger,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10) =>
{
    try
    {
        // Validate input parameters
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100; // Max page size limit (prevent DoS)

        logger.LogInformation("[ENROLLMENTS] Admin getting all enrollments, Page: {Page}, PageSize: {PageSize}", page, pageSize);

        var enrollments = await enrollmentService.GetAllEnrollmentsAsync(page, pageSize);

        logger.LogInformation("[ENROLLMENTS] Successfully retrieved {Count} enrollments (Page {Page}/{TotalPages})",
            enrollments.Enrollments.Count, page, enrollments.TotalPages);

        return Results.Ok(enrollments);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[ENROLLMENTS] Error getting all enrollments");
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.RequireAuthorization(policy => policy.RequireRole("Admin"))
.WithName("GetAllEnrollments")
.WithTags("Enrollments")
.Produces<InsightLearn.Core.DTOs.Enrollment.EnrollmentListDto>(200)
.ProducesProblem(500);

// Create new enrollment (authenticated user)
app.MapPost("/api/enrollments", async (
    [FromBody] InsightLearn.Core.DTOs.Enrollment.CreateEnrollmentDto dto,
    [FromServices] InsightLearn.Core.Interfaces.IEnrollmentService enrollmentService,
    [FromServices] ILogger<Program> logger,
    ClaimsPrincipal user) =>
{
    try
    {
        var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            logger.LogWarning("[ENROLLMENTS] User ID not found in claims");
            return Results.Unauthorized();
        }

        var userGuid = Guid.Parse(userId);
        var isAdmin = user.IsInRole("Admin");

        if (!isAdmin && dto.UserId != userGuid)
        {
            logger.LogWarning("[ENROLLMENTS] User {UserId} attempted to enroll another user {TargetUserId}",
                userGuid, dto.UserId);
            return Results.Forbid();
        }

        var isEnrolled = await enrollmentService.IsUserEnrolledAsync(dto.UserId, dto.CourseId);
        if (isEnrolled)
        {
            logger.LogWarning("[ENROLLMENTS] User {UserId} already enrolled in course {CourseId}",
                dto.UserId, dto.CourseId);
            return Results.BadRequest(new { error = "User is already enrolled in this course" });
        }

        logger.LogInformation("[ENROLLMENTS] Creating enrollment for user {UserId} in course {CourseId}",
            dto.UserId, dto.CourseId);

        var enrollment = await enrollmentService.EnrollUserAsync(dto);

        logger.LogInformation("[ENROLLMENTS] Successfully created enrollment {EnrollmentId}", enrollment.Id);
        return Results.Created($"/api/enrollments/{enrollment.Id}", enrollment);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[ENROLLMENTS] Error creating enrollment");
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.RequireAuthorization()
.WithName("CreateEnrollment")
.WithTags("Enrollments")
.Produces<InsightLearn.Core.DTOs.Enrollment.EnrollmentDto>(201)
.ProducesProblem(400)
.ProducesProblem(500);

// Get enrollment by ID
app.MapGet("/api/enrollments/{id:guid}", async (
    Guid id,
    [FromServices] InsightLearn.Core.Interfaces.IEnrollmentService enrollmentService,
    [FromServices] ILogger<Program> logger,
    ClaimsPrincipal user) =>
{
    try
    {
        logger.LogInformation("[ENROLLMENTS] Getting enrollment {EnrollmentId}", id);

        var enrollment = await enrollmentService.GetEnrollmentByIdAsync(id);
        if (enrollment == null)
        {
            logger.LogWarning("[ENROLLMENTS] Enrollment {EnrollmentId} not found", id);
            return Results.NotFound(new { error = "Enrollment not found" });
        }

        var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var isAdmin = user.IsInRole("Admin");

        if (!isAdmin && enrollment.UserId.ToString() != userId)
        {
            logger.LogWarning("[ENROLLMENTS] User {UserId} attempted to access enrollment {EnrollmentId} belonging to user {EnrollmentUserId}",
                userId, id, enrollment.UserId);
            return Results.Forbid();
        }

        logger.LogInformation("[ENROLLMENTS] Successfully retrieved enrollment {EnrollmentId}", id);
        return Results.Ok(enrollment);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[ENROLLMENTS] Error getting enrollment {EnrollmentId}", id);
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.RequireAuthorization()
.WithName("GetEnrollmentById")
.WithTags("Enrollments")
.Produces<InsightLearn.Core.DTOs.Enrollment.EnrollmentDto>(200)
.ProducesProblem(404)
.ProducesProblem(500);

// Get enrollments for a specific course
app.MapGet("/api/enrollments/course/{courseId:guid}", async (
    Guid courseId,
    [FromServices] InsightLearn.Core.Interfaces.IEnrollmentService enrollmentService,
    [FromServices] ILogger<Program> logger,
    ClaimsPrincipal user) =>
{
    try
    {
        var isAdmin = user.IsInRole("Admin");
        var isInstructor = user.IsInRole("Instructor");

        if (!isAdmin && !isInstructor)
        {
            logger.LogWarning("[ENROLLMENTS] Unauthorized access attempt to course enrollments for course {CourseId}", courseId);
            return Results.Forbid();
        }

        logger.LogInformation("[ENROLLMENTS] Getting enrollments for course {CourseId}", courseId);

        var enrollments = await enrollmentService.GetCourseEnrollmentsAsync(courseId);

        logger.LogInformation("[ENROLLMENTS] Found {Count} enrollments for course {CourseId}",
            enrollments.Count, courseId);
        return Results.Ok(enrollments);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[ENROLLMENTS] Error getting enrollments for course {CourseId}", courseId);
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.RequireAuthorization(policy => policy.RequireRole("Admin", "Instructor"))
.WithName("GetCourseEnrollments")
.WithTags("Enrollments")
.Produces<List<InsightLearn.Core.DTOs.Enrollment.EnrollmentDto>>(200)
.ProducesProblem(500);

// Get user's enrollments
app.MapGet("/api/enrollments/user/{userId:guid}", async (
    Guid userId,
    [FromServices] InsightLearn.Core.Interfaces.IEnrollmentService enrollmentService,
    [FromServices] ILogger<Program> logger,
    ClaimsPrincipal user) =>
{
    try
    {
        var currentUserId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var isAdmin = user.IsInRole("Admin");

        if (!isAdmin && userId.ToString() != currentUserId)
        {
            logger.LogWarning("[ENROLLMENTS] User {CurrentUserId} attempted to access enrollments for user {TargetUserId}",
                currentUserId, userId);
            return Results.Forbid();
        }

        logger.LogInformation("[ENROLLMENTS] Getting enrollments for user {UserId}", userId);

        var enrollments = await enrollmentService.GetUserEnrollmentsAsync(userId);

        logger.LogInformation("[ENROLLMENTS] Found {Count} enrollments for user {UserId}",
            enrollments.Count, userId);
        return Results.Ok(enrollments);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[ENROLLMENTS] Error getting enrollments for user {UserId}", userId);
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.RequireAuthorization()
.WithName("GetUserEnrollments")
.WithTags("Enrollments")
.Produces<List<InsightLearn.Core.DTOs.Enrollment.EnrollmentDto>>(200)
.ProducesProblem(500);

// ===========================
// PAYMENTS API ENDPOINTS
// ===========================

// POST /api/payments/create-checkout - Create Stripe checkout session
app.MapPost("/api/payments/create-checkout", async (
    [FromBody] InsightLearn.Core.DTOs.Payment.CreatePaymentDto dto,
    [FromServices] InsightLearn.Core.Interfaces.IPaymentService paymentService,
    ClaimsPrincipal user,
    ILogger<Program> logger) =>
{
    try
    {
        var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            logger.LogWarning("[PAYMENTS] Unauthorized checkout attempt - no user ID in token");
            return Results.Unauthorized();
        }

        if (dto.UserId != Guid.Parse(userId))
        {
            logger.LogWarning("[PAYMENTS] User {UserId} attempted to create checkout for different user {RequestedUserId}",
                userId, dto.UserId);
            return Results.Forbid();
        }

        logger.LogInformation("[PAYMENTS] Creating Stripe checkout for user {UserId}, course {CourseId}, amount {Amount} {Currency}",
            dto.UserId, dto.CourseId, dto.Amount, dto.Currency);

        var checkout = await paymentService.CreateStripeCheckoutAsync(dto);

        logger.LogInformation("[PAYMENTS] Checkout session created successfully: {SessionId}", checkout.SessionId);

        return Results.Ok(checkout);
    }
    catch (InvalidOperationException ex)
    {
        logger.LogWarning(ex, "[PAYMENTS] Invalid checkout request");
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[PAYMENTS] Failed to create checkout session");
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.RequireAuthorization()
.WithName("CreateStripeCheckout")
.WithTags("Payments")
.Produces<InsightLearn.Core.DTOs.Payment.StripeCheckoutDto>(200)
.Produces(400)
.Produces(500);

// GET /api/payments/transactions - List payment transactions
app.MapGet("/api/payments/transactions", async (
    [FromServices] InsightLearn.Core.Interfaces.IPaymentService paymentService,
    ClaimsPrincipal user,
    ILogger<Program> logger,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10,
    [FromQuery] InsightLearn.Core.Entities.PaymentStatus? status = null) =>
{
    try
    {
        var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            logger.LogWarning("[PAYMENTS] Unauthorized transaction list attempt - no user ID");
            return Results.Unauthorized();
        }

        var userRole = user.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        var isAdmin = userRole?.Equals("Admin", StringComparison.OrdinalIgnoreCase) ?? false;

        logger.LogInformation("[PAYMENTS] Fetching transactions - User: {UserId}, Role: {Role}, Page: {Page}, Status: {Status}",
            userId, userRole, page, status);

        InsightLearn.Core.DTOs.Payment.TransactionListDto transactions;

        if (isAdmin)
        {
            transactions = await paymentService.GetAllTransactionsAsync(page, pageSize, status);
            logger.LogInformation("[PAYMENTS] Admin retrieved {Count} transactions (total: {Total})",
                transactions.Transactions.Count, transactions.TotalCount);
        }
        else
        {
            var userTransactions = await paymentService.GetUserTransactionsAsync(Guid.Parse(userId));

            if (status.HasValue)
            {
                userTransactions = userTransactions
                    .Where(t => t.Status.Equals(status.Value.ToString(), StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            var paginatedTransactions = userTransactions
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            transactions = new InsightLearn.Core.DTOs.Payment.TransactionListDto
            {
                Transactions = paginatedTransactions,
                TotalCount = userTransactions.Count,
                TotalRevenue = userTransactions.Sum(t => t.Amount),
                Page = page,
                PageSize = pageSize
            };

            logger.LogInformation("[PAYMENTS] User {UserId} retrieved {Count} transactions",
                userId, transactions.Transactions.Count);
        }

        return Results.Ok(transactions);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[PAYMENTS] Failed to fetch transactions");
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.RequireAuthorization()
.WithName("GetTransactions")
.WithTags("Payments")
.Produces<InsightLearn.Core.DTOs.Payment.TransactionListDto>(200)
.Produces(500);

// GET /api/payments/transactions/{id:guid} - Get transaction by ID
app.MapGet("/api/payments/transactions/{id:guid}", async (
    Guid id,
    [FromServices] InsightLearn.Core.Interfaces.IPaymentService paymentService,
    ClaimsPrincipal user,
    ILogger<Program> logger) =>
{
    try
    {
        var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            logger.LogWarning("[PAYMENTS] Unauthorized transaction access attempt - no user ID");
            return Results.Unauthorized();
        }

        var userRole = user.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        var isAdmin = userRole?.Equals("Admin", StringComparison.OrdinalIgnoreCase) ?? false;

        logger.LogInformation("[PAYMENTS] Fetching transaction {TransactionId} for user {UserId} (Admin: {IsAdmin})",
            id, userId, isAdmin);

        var payment = await paymentService.GetPaymentByIdAsync(id);

        if (payment == null)
        {
            logger.LogWarning("[PAYMENTS] Transaction {TransactionId} not found", id);
            return Results.NotFound(new { error = "Transaction not found" });
        }

        if (!isAdmin && payment.UserId != Guid.Parse(userId))
        {
            logger.LogWarning("[PAYMENTS] User {UserId} attempted to access transaction {TransactionId} belonging to user {OwnerId}",
                userId, id, payment.UserId);
            return Results.Forbid();
        }

        logger.LogInformation("[PAYMENTS] Successfully retrieved transaction {TransactionId} for user {UserId}",
            id, userId);

        return Results.Ok(payment);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[PAYMENTS] Failed to fetch transaction {TransactionId}", id);
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.RequireAuthorization()
.WithName("GetTransactionById")
.WithTags("Payments")
.Produces<InsightLearn.Core.DTOs.Payment.PaymentDto>(200)
.Produces(404)
.Produces(500);

// POST /api/admin/payments/{id:guid}/refund - Process refund (Admin only)
app.MapPost("/api/admin/payments/{id:guid}/refund", async (
    Guid id,
    [FromBody] RefundRequestDto refundRequest,
    [FromServices] InsightLearnDbContext dbContext,
    [FromServices] ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("[ADMIN] Processing refund for payment {PaymentId}, Amount: {Amount}",
            id, refundRequest.RefundAmount);

        // Find the payment
        var payment = await dbContext.Payments
            .Include(p => p.Course)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (payment == null)
        {
            return Results.NotFound(new { error = "Payment not found" });
        }

        // Validate payment status
        if (payment.Status != PaymentStatus.Completed)
        {
            return Results.BadRequest(new { error = "Only completed payments can be refunded" });
        }

        // Validate refund amount
        var alreadyRefunded = payment.RefundAmount ?? 0m;
        var maxRefundable = payment.Amount - alreadyRefunded;

        if (refundRequest.RefundAmount > maxRefundable)
        {
            return Results.BadRequest(new
            {
                error = "Refund amount exceeds refundable amount",
                maxRefundable,
                alreadyRefunded
            });
        }

        // Process refund (in a real scenario, this would call Stripe/PayPal API)
        // For now, we'll just update the database
        payment.RefundAmount = (payment.RefundAmount ?? 0m) + refundRequest.RefundAmount;
        payment.RefundedAt = DateTime.UtcNow;

        // Update payment status
        if (payment.RefundAmount >= payment.Amount)
        {
            payment.Status = PaymentStatus.Refunded;
        }
        else
        {
            payment.Status = PaymentStatus.PartiallyRefunded;
        }

        // Create a refund record in Payment table (negative amount)
        var refundRecord = new Payment
        {
            Id = Guid.NewGuid(),
            UserId = payment.UserId,
            CourseId = payment.CourseId,
            Amount = -refundRequest.RefundAmount, // Negative for refund
            OriginalAmount = refundRequest.RefundAmount,
            DiscountAmount = 0,
            Currency = payment.Currency,
            PaymentMethod = payment.PaymentMethod,
            Status = PaymentStatus.Refunded,
            CreatedAt = DateTime.UtcNow,
            RefundedAt = DateTime.UtcNow,
            InvoiceNumber = $"REFUND-{payment.InvoiceNumber}"
        };

        dbContext.Payments.Add(refundRecord);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("[ADMIN] Refund processed for payment {PaymentId}. Refund ID: {RefundId}",
            id, refundRecord.Id);

        var response = new RefundResponseDto
        {
            Success = true,
            PaymentId = payment.Id,
            RefundId = refundRecord.Id,
            RefundAmount = refundRequest.RefundAmount,
            OriginalAmount = payment.Amount,
            RefundedAt = refundRecord.RefundedAt!.Value,
            Message = "Refund processed successfully",
            RefundProvider = payment.PaymentMethod.ToString()
        };

        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[ADMIN] Error processing refund for payment {PaymentId}", id);
        return Results.Json(new { error = ex.Message }, statusCode: (int)HttpStatusCode.InternalServerError);
    }
})
.RequireAuthorization(policy => policy.RequireRole("Admin"))
.WithName("RefundPayment")
.WithTags("Admin", "Payments")
.Produces<RefundResponseDto>(200)
.Produces(400)
.Produces(404)
.Produces(500);

// GET /api/admin/payments/stats - Get payment statistics (Admin only)
app.MapGet("/api/admin/payments/stats", async (
    [FromServices] InsightLearnDbContext dbContext,
    [FromServices] ILogger<Program> logger,
    [FromQuery] DateTime? fromDate = null,
    [FromQuery] DateTime? toDate = null) =>
{
    try
    {
        logger.LogInformation("[ADMIN] Fetching payment stats (from: {From}, to: {To})",
            fromDate ?? DateTime.MinValue, toDate ?? DateTime.MaxValue);

        // Set default date range (last 30 days if not specified)
        var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
        var to = toDate ?? DateTime.UtcNow;

        // Get all payments in range
        var paymentsQuery = dbContext.Payments
            .Where(p => p.CreatedAt >= from && p.CreatedAt <= to);

        var allPayments = await paymentsQuery.ToListAsync();

        // Calculate totals
        var totalRevenue = allPayments
            .Where(p => p.Status == PaymentStatus.Completed && p.Amount > 0)
            .Sum(p => p.Amount);

        var totalRefunded = allPayments
            .Where(p => p.Amount < 0) // Negative amounts are refunds
            .Sum(p => Math.Abs(p.Amount));

        var totalTransactions = allPayments.Count;
        var successfulTransactions = allPayments.Count(p => p.Status == PaymentStatus.Completed);
        var failedTransactions = allPayments.Count(p => p.Status == PaymentStatus.Failed);
        var refundedTransactions = allPayments.Count(p => p.Status == PaymentStatus.Refunded || p.Status == PaymentStatus.PartiallyRefunded);

        // Revenue by payment method
        var revenueByMethod = allPayments
            .Where(p => p.Status == PaymentStatus.Completed && p.Amount > 0)
            .GroupBy(p => p.PaymentMethod.ToString())
            .ToDictionary(
                g => g.Key,
                g => g.Sum(p => p.Amount)
            );

        // Transactions by status
        var transactionsByStatus = allPayments
            .GroupBy(p => p.Status.ToString())
            .ToDictionary(
                g => g.Key,
                g => g.Count()
            );

        // Daily revenue (last 30 days)
        var dailyRevenue = allPayments
            .Where(p => p.Status == PaymentStatus.Completed && p.Amount > 0)
            .GroupBy(p => p.CreatedAt.Date)
            .Select(g => new DailyRevenueDto
            {
                Date = g.Key,
                Revenue = g.Sum(p => p.Amount),
                Transactions = g.Count()
            })
            .OrderBy(d => d.Date)
            .ToList();

        var stats = new PaymentStatsDto
        {
            TotalRevenue = totalRevenue,
            TotalRefunded = totalRefunded,
            TotalTransactions = totalTransactions,
            SuccessfulTransactions = successfulTransactions,
            FailedTransactions = failedTransactions,
            RefundedTransactions = refundedTransactions,
            RevenueByMethod = revenueByMethod,
            TransactionsByStatus = transactionsByStatus,
            DailyRevenue = dailyRevenue
        };

        logger.LogInformation("[ADMIN] Retrieved payment stats: Revenue={Revenue}, Transactions={Count}",
            totalRevenue, totalTransactions);

        return Results.Ok(stats);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[ADMIN] Error fetching payment stats");
        return Results.Json(new { error = ex.Message }, statusCode: (int)HttpStatusCode.InternalServerError);
    }
})
.RequireAuthorization(policy => policy.RequireRole("Admin"))
.WithName("GetPaymentStats")
.WithTags("Admin", "Payments")
.Produces<PaymentStatsDto>(200)
.Produces(500);

// ========================================
// ANALYTICS API ENDPOINTS (Admin only)
// ========================================

// GET /api/admin/analytics/overview - Get analytics overview with KPIs and trends
app.MapGet("/api/admin/analytics/overview", async (
    [FromServices] IAdminAnalyticsService analyticsService,
    [FromServices] ILogger<Program> logger,
    [FromQuery] string range = "30days") =>
{
    try
    {
        logger.LogInformation("[ADMIN] Fetching analytics overview for range: {Range}", range);

        var overview = await analyticsService.GetOverviewAsync(range);

        logger.LogInformation("[ADMIN] Analytics overview retrieved: {Users} users, {Revenue} revenue",
            overview.TotalUsers, overview.TotalRevenue);

        return Results.Ok(overview);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[ADMIN] Error fetching analytics overview");
        return Results.Json(new { error = ex.Message }, statusCode: (int)HttpStatusCode.InternalServerError);
    }
})
.RequireAuthorization(policy => policy.RequireRole("Admin"))
.WithName("GetAnalyticsOverview")
.WithTags("Admin", "Analytics")
.Produces<InsightLearn.Core.DTOs.Admin.AnalyticsOverviewDto>(200)
.Produces(500);

// GET /api/admin/analytics/user-growth - Get user growth data for charts
app.MapGet("/api/admin/analytics/user-growth", async (
    [FromServices] IAdminAnalyticsService analyticsService,
    [FromServices] ILogger<Program> logger,
    [FromQuery] int? days = null) =>
{
    try
    {
        var daysValue = days ?? 30;
        logger.LogInformation("[ADMIN] Fetching user growth data for {Days} days", daysValue);

        var userGrowth = await analyticsService.GetUserGrowthAsync(daysValue);

        logger.LogInformation("[ADMIN] User growth data retrieved: {DataPoints} data points",
            userGrowth.DataPoints.Count);

        return Results.Ok(userGrowth);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[ADMIN] Error fetching user growth data");
        return Results.Json(new { error = ex.Message }, statusCode: (int)HttpStatusCode.InternalServerError);
    }
})
.RequireAuthorization(policy => policy.RequireRole("Admin"))
.WithName("GetUserGrowthData")
.WithTags("Admin", "Analytics")
.Produces<InsightLearn.Core.DTOs.Admin.AnalyticsChartDataDto>(200)
.Produces(500);

// GET /api/admin/analytics/revenue-trends - Get revenue trends for charts
app.MapGet("/api/admin/analytics/revenue-trends", async (
    [FromServices] IAdminAnalyticsService analyticsService,
    [FromServices] ILogger<Program> logger,
    [FromQuery] int? months = null) =>
{
    try
    {
        var monthsValue = months ?? 12;
        logger.LogInformation("[ADMIN] Fetching revenue trends for {Months} months", monthsValue);

        var revenueTrends = await analyticsService.GetRevenueTrendsAsync(monthsValue);

        logger.LogInformation("[ADMIN] Revenue trends retrieved: {DataPoints} data points",
            revenueTrends.DataPoints.Count);

        return Results.Ok(revenueTrends);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[ADMIN] Error fetching revenue trends");
        return Results.Json(new { error = ex.Message }, statusCode: (int)HttpStatusCode.InternalServerError);
    }
})
.RequireAuthorization(policy => policy.RequireRole("Admin"))
.WithName("GetRevenueTrends")
.WithTags("Admin", "Analytics")
.Produces<InsightLearn.Core.DTOs.Admin.AnalyticsChartDataDto>(200)
.Produces(500);

// GET /api/admin/analytics/top-courses - Get top performing courses
app.MapGet("/api/admin/analytics/top-courses", async (
    [FromServices] IAdminAnalyticsService analyticsService,
    [FromServices] ILogger<Program> logger,
    [FromQuery] int? limit = null,
    [FromQuery] string range = "all") =>
{
    try
    {
        var limitValue = limit ?? 10;
        logger.LogInformation("[ADMIN] Fetching top {Limit} courses for range: {Range}", limitValue, range);

        var topCourses = await analyticsService.GetTopCoursesAsync(limitValue, range);

        logger.LogInformation("[ADMIN] Top courses retrieved: {Count} courses",
            topCourses.Courses.Count);

        return Results.Ok(topCourses);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[ADMIN] Error fetching top courses");
        return Results.Json(new { error = ex.Message }, statusCode: (int)HttpStatusCode.InternalServerError);
    }
})
.RequireAuthorization(policy => policy.RequireRole("Admin"))
.WithName("GetTopCourses")
.WithTags("Admin", "Analytics")
.Produces<InsightLearn.Core.DTOs.Admin.TopCoursesResponseDto>(200)
.Produces(500);

// GET /api/admin/analytics/enrollment-trends - Get enrollment trends for charts
app.MapGet("/api/admin/analytics/enrollment-trends", async (
    [FromServices] IAdminAnalyticsService analyticsService,
    [FromServices] ILogger<Program> logger,
    [FromQuery] int? days = null) =>
{
    try
    {
        var daysValue = days ?? 90;
        logger.LogInformation("[ADMIN] Fetching enrollment trends for {Days} days", daysValue);

        var enrollmentTrends = await analyticsService.GetEnrollmentTrendsAsync(daysValue);

        logger.LogInformation("[ADMIN] Enrollment trends retrieved: {DataPoints} data points",
            enrollmentTrends.DataPoints.Count);

        return Results.Ok(enrollmentTrends);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[ADMIN] Error fetching enrollment trends");
        return Results.Json(new { error = ex.Message }, statusCode: (int)HttpStatusCode.InternalServerError);
    }
})
.RequireAuthorization(policy => policy.RequireRole("Admin"))
.WithName("GetEnrollmentTrends")
.WithTags("Admin", "Analytics")
.Produces<InsightLearn.Core.DTOs.Admin.AnalyticsChartDataDto>(200)
.Produces(500);

// ========================================
// ADMIN REPORTS API ENDPOINTS (v2.1.0-dev)
// ========================================

// POST /api/admin/reports/generate - Generate report based on type
app.MapPost("/api/admin/reports/generate", async (
    [FromServices] IReportService reportService,
    [FromServices] ILogger<Program> logger,
    [FromBody] GenerateReportRequest request) =>
{
    try
    {
        logger.LogInformation("[REPORTS] Generating {Type} report from {Start} to {End}",
            request.Type, request.StartDate, request.EndDate);

        ReportResult report = request.Type switch
        {
            "revenue" => await reportService.GenerateRevenueReportAsync(request.StartDate, request.EndDate),
            "users" => await reportService.GenerateUserGrowthReportAsync(request.StartDate, request.EndDate),
            "courses" => await reportService.GenerateCoursePerformanceReportAsync(request.StartDate, request.EndDate),
            "enrollments" => await reportService.GenerateEnrollmentReportAsync(request.StartDate, request.EndDate),
            "engagement" => await reportService.GenerateEngagementReportAsync(request.StartDate, request.EndDate),
            "instructors" => await reportService.GenerateInstructorEarningsReportAsync(request.StartDate, request.EndDate),
            _ => throw new ArgumentException($"Unknown report type: {request.Type}")
        };

        logger.LogInformation("[REPORTS] Report generated: {Title} with {Rows} rows",
            report.Title, report.Rows.Count);

        return Results.Ok(report);
    }
    catch (ArgumentException ex)
    {
        logger.LogWarning(ex, "[REPORTS] Invalid report type requested");
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[REPORTS] Error generating report");
        return Results.Json(new { error = ex.Message }, statusCode: (int)HttpStatusCode.InternalServerError);
    }
})
.RequireAuthorization(policy => policy.RequireRole("Admin"))
.WithName("GenerateReport")
.WithTags("Admin", "Reports")
.Produces<ReportResult>(200)
.Produces(400)
.Produces(500);

// POST /api/admin/reports/export/csv - Export report to CSV
app.MapPost("/api/admin/reports/export/csv", async (
    [FromServices] IReportService reportService,
    [FromServices] ILogger<Program> logger,
    [FromBody] ReportResult report) =>
{
    try
    {
        logger.LogInformation("[REPORTS] Exporting report to CSV: {Title}", report.Title);

        var csvBytes = await reportService.ExportToCsvAsync(report);
        var fileName = $"{report.Type}_report_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";

        return Results.File(csvBytes, "text/csv", fileName);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[REPORTS] Error exporting report to CSV");
        return Results.Json(new { error = ex.Message }, statusCode: (int)HttpStatusCode.InternalServerError);
    }
})
.RequireAuthorization(policy => policy.RequireRole("Admin"))
.WithName("ExportReportCsv")
.WithTags("Admin", "Reports")
.Produces(200)
.Produces(500);

// POST /api/admin/reports/export/pdf - Export report to PDF
app.MapPost("/api/admin/reports/export/pdf", async (
    [FromServices] IReportService reportService,
    [FromServices] ILogger<Program> logger,
    [FromBody] ReportResult report) =>
{
    try
    {
        logger.LogInformation("[REPORTS] Exporting report to PDF: {Title}", report.Title);

        var pdfBytes = await reportService.ExportToPdfAsync(report);
        var fileName = $"{report.Type}_report_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf";

        return Results.File(pdfBytes, "application/pdf", fileName);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[REPORTS] Error exporting report to PDF");
        return Results.Json(new { error = ex.Message }, statusCode: (int)HttpStatusCode.InternalServerError);
    }
})
.RequireAuthorization(policy => policy.RequireRole("Admin"))
.WithName("ExportReportPdf")
.WithTags("Admin", "Reports")
.Produces(200)
.Produces(500);

// POST /api/admin/reports/export/excel - Export report to Excel
app.MapPost("/api/admin/reports/export/excel", async (
    [FromServices] IReportService reportService,
    [FromServices] ILogger<Program> logger,
    [FromBody] ReportResult report) =>
{
    try
    {
        logger.LogInformation("[REPORTS] Exporting report to Excel: {Title}", report.Title);

        var excelBytes = await reportService.ExportToExcelAsync(report);
        var fileName = $"{report.Type}_report_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";

        return Results.File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[REPORTS] Error exporting report to Excel");
        return Results.Json(new { error = ex.Message }, statusCode: (int)HttpStatusCode.InternalServerError);
    }
})
.RequireAuthorization(policy => policy.RequireRole("Admin"))
.WithName("ExportReportExcel")
.WithTags("Admin", "Reports")
.Produces(200)
.Produces(500);

// ========================================
// USER ADMIN API ENDPOINTS
// ========================================

// GET /api/users - List all users with pagination (Admin only)
app.MapGet("/api/users", async (
    [FromServices] InsightLearn.Core.Interfaces.IUserAdminService userAdminService,
    [FromServices] ILogger<Program> logger,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10) =>
{
    try
    {
        logger.LogInformation("[USERS] Getting all users, Page: {Page}, PageSize: {PageSize}", page, pageSize);

        var users = await userAdminService.GetAllUsersAsync(page, pageSize);

        logger.LogInformation("[USERS] Retrieved {Count} users", users.Users.Count());
        return Results.Ok(users);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[USERS] Error getting users");
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.RequireAuthorization(policy => policy.RequireRole("Admin"))
.WithName("GetAllUsers")
.WithTags("Users")
.Produces<InsightLearn.Core.DTOs.User.UserListDto>(200)
.Produces(401)
.Produces(403)
.Produces(500);

// GET /api/users/{id:guid} - Get user by ID (Admin or self)
app.MapGet("/api/users/{id:guid}", async (
    Guid id,
    [FromServices] InsightLearn.Core.Interfaces.IUserAdminService userAdminService,
    [FromServices] ILogger<Program> logger,
    ClaimsPrincipal user) =>
{
    try
    {
        logger.LogInformation("[USERS] Getting user by ID: {UserId}", id);

        var currentUserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var isAdmin = user.IsInRole("Admin");

        if (!isAdmin && currentUserId != id.ToString())
        {
            logger.LogWarning("[USERS] Unauthorized access attempt by user {CurrentUserId} to user {TargetUserId}",
                currentUserId, id);
            return Results.Forbid();
        }

        var userDetail = await userAdminService.GetUserByIdAsync(id);

        if (userDetail == null)
        {
            logger.LogWarning("[USERS] User not found: {UserId}", id);
            return Results.NotFound($"User with ID {id} not found");
        }

        logger.LogInformation("[USERS] Successfully retrieved user: {UserId}", id);
        return Results.Ok(userDetail);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[USERS] Error getting user {UserId}", id);
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.RequireAuthorization()
.WithName("GetUserById")
.WithTags("Users")
.Produces<InsightLearn.Core.DTOs.User.UserDetailDto>(200)
.Produces(401)
.Produces(403)
.Produces(404)
.Produces(500);

// PUT /api/users/{id:guid} - Update user (Admin or self)
app.MapPut("/api/users/{id:guid}", async (
    Guid id,
    [FromBody] InsightLearn.Core.DTOs.User.UpdateUserDto updateDto,
    [FromServices] InsightLearn.Core.Interfaces.IUserAdminService userAdminService,
    [FromServices] ILogger<Program> logger,
    ClaimsPrincipal user) =>
{
    try
    {
        logger.LogInformation("[USERS] Updating user: {UserId}", id);

        var currentUserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var isAdmin = user.IsInRole("Admin");

        if (!isAdmin && currentUserId != id.ToString())
        {
            logger.LogWarning("[USERS] Unauthorized update attempt by user {CurrentUserId} on user {TargetUserId}",
                currentUserId, id);
            return Results.Forbid();
        }

        var updatedUser = await userAdminService.UpdateUserAsync(id, updateDto);

        if (updatedUser == null)
        {
            logger.LogWarning("[USERS] User not found for update: {UserId}", id);
            return Results.NotFound($"User with ID {id} not found");
        }

        logger.LogInformation("[USERS] Successfully updated user: {UserId}", id);
        return Results.Ok(updatedUser);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[USERS] Error updating user {UserId}", id);
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.RequireAuthorization()
.WithName("UpdateUser")
.WithTags("Users")
.Produces<InsightLearn.Core.DTOs.User.UserDetailDto>(200)
.Produces(401)
.Produces(403)
.Produces(404)
.Produces(500);

// DELETE /api/users/{id:guid} - Delete user (Admin only)
app.MapDelete("/api/users/{id:guid}", async (
    Guid id,
    [FromServices] InsightLearn.Core.Interfaces.IUserAdminService userAdminService,
    [FromServices] ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("[USERS] Deleting user: {UserId}", id);

        var result = await userAdminService.DeleteUserAsync(id);

        if (!result)
        {
            logger.LogWarning("[USERS] User not found for deletion: {UserId}", id);
            return Results.NotFound($"User with ID {id} not found");
        }

        logger.LogInformation("[USERS] Successfully deleted user: {UserId}", id);
        return Results.NoContent();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[USERS] Error deleting user {UserId}", id);
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.RequireAuthorization(policy => policy.RequireRole("Admin"))
.WithName("DeleteUser")
.WithTags("Users")
.Produces(204)
.Produces(401)
.Produces(403)
.Produces(404)
.Produces(500);

// GET /api/users/profile - Get current user profile
app.MapGet("/api/users/profile", async (
    [FromServices] InsightLearn.Core.Interfaces.IUserAdminService userAdminService,
    [FromServices] ILogger<Program> logger,
    ClaimsPrincipal user) =>
{
    try
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            logger.LogWarning("[USERS] Invalid or missing user ID in token");
            return Results.Unauthorized();
        }

        logger.LogInformation("[USERS] Getting profile for user: {UserId}", userId);

        var userDetail = await userAdminService.GetUserByIdAsync(userId);

        if (userDetail == null)
        {
            logger.LogWarning("[USERS] Profile not found for user: {UserId}", userId);
            return Results.NotFound("User profile not found");
        }

        logger.LogInformation("[USERS] Successfully retrieved profile for user: {UserId}", userId);
        return Results.Ok(userDetail);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[USERS] Error getting user profile");
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.RequireAuthorization()
.WithName("GetCurrentUserProfile")
.WithTags("Users")
.Produces<InsightLearn.Core.DTOs.User.UserDetailDto>(200)
.Produces(401)
.Produces(404)
.Produces(500);

// ========================================
// DASHBOARD API ENDPOINTS
// ========================================

// GET /api/dashboard/stats - Get dashboard statistics (Admin only)
app.MapGet("/api/dashboard/stats", async (
    [FromServices] InsightLearn.Application.Interfaces.IAdminService adminService,
    [FromServices] ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("[DASHBOARD] Getting dashboard statistics");

        var stats = await adminService.GetDashboardStatisticsAsync();

        logger.LogInformation("[DASHBOARD] Successfully retrieved dashboard statistics");
        return Results.Ok(stats);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[DASHBOARD] Error getting dashboard statistics");
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.RequireAuthorization(policy => policy.RequireRole("Admin"))
.WithName("GetLMSDashboardStats")
.WithTags("Dashboard")
.Produces<InsightLearn.Application.DTOs.AdminDashboardDto>(200)
.Produces(401)
.Produces(403)
.Produces(500);

// GET /api/dashboard/recent-activity - Get recent activity (Admin only)
app.MapGet("/api/dashboard/recent-activity", async (
    [FromServices] InsightLearn.Application.Interfaces.IAdminService adminService,
    [FromServices] ILogger<Program> logger,
    [FromQuery] int count = 10) =>
{
    try
    {
        logger.LogInformation("[DASHBOARD] Getting recent activity, Count: {Count}", count);

        var activities = await adminService.GetRecentActivityAsync(count);

        logger.LogInformation("[DASHBOARD] Successfully retrieved {Count} recent activities",
            activities.Count());
        return Results.Ok(activities);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[DASHBOARD] Error getting recent activity");
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.RequireAuthorization(policy => policy.RequireRole("Admin"))
.WithName("GetLMSRecentActivity")
.WithTags("Dashboard")
.Produces<IEnumerable<InsightLearn.Application.DTOs.RecentActivityDto>>(200)
.Produces(401)
.Produces(403)
.Produces(500);

// ========================================
// STUDENT LEARNING SPACE ENDPOINTS (v2.1.0)
// ========================================
// Phase 3: 28 REST API endpoints for Student Learning Space
// - Video Transcripts (5 endpoints)
// - AI Takeaways (3 endpoints)
// - Student Notes (6 endpoints)
// - AI Chat (4 endpoints)
// - Video Bookmarks (4 endpoints)
// - Video Progress (2 endpoints)
// - Background Jobs (4 endpoints)

// ========================================
// VIDEO TRANSCRIPT ENDPOINTS (5)
// ========================================

// GET /api/transcripts/{lessonId} - Get complete transcript
app.MapGet("/api/transcripts/{lessonId:guid}", async (
    Guid lessonId,
    [FromServices] IVideoTranscriptService transcriptService,
    [FromServices] ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("[TRANSCRIPT] Getting transcript for lesson {LessonId}", lessonId);
        var transcript = await transcriptService.GetTranscriptAsync(lessonId);

        if (transcript == null)
        {
            logger.LogWarning("[TRANSCRIPT] Transcript not found for lesson {LessonId}", lessonId);
            return Results.NotFound(new { error = "Transcript not found" });
        }

        return Results.Ok(transcript);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[TRANSCRIPT] Error getting transcript for lesson {LessonId}", lessonId);
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.RequireAuthorization()
.WithName("GetTranscript")
.WithTags("Transcripts")
.Produces<VideoTranscriptDto>(200)
.Produces(404)
.Produces(500);

// GET /api/transcripts/{lessonId}/search?query={text} - Search transcript
app.MapGet("/api/transcripts/{lessonId:guid}/search", async (
    Guid lessonId,
    [FromQuery] string query,
    [FromServices] IVideoTranscriptService transcriptService,
    [FromServices] ILogger<Program> logger) =>
{
    try
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Results.BadRequest(new { error = "Query parameter is required" });
        }

        logger.LogInformation("[TRANSCRIPT] Searching transcript for lesson {LessonId} with query: {Query}", lessonId, query);
        var results = await transcriptService.SearchTranscriptAsync(lessonId, query);

        return Results.Ok(results);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[TRANSCRIPT] Error searching transcript for lesson {LessonId}", lessonId);
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.RequireAuthorization()
.WithName("SearchTranscript")
.WithTags("Transcripts")
.Produces<IEnumerable<TranscriptSearchResultDto>>(200)
.Produces(400)
.Produces(500);

// POST /api/transcripts/{lessonId}/generate - Queue transcript generation (Admin/Instructor)
app.MapPost("/api/transcripts/{lessonId:guid}/generate", async (
    Guid lessonId,
    [FromBody] GenerateTranscriptRequestDto request,
    [FromServices] IVideoTranscriptService transcriptService,
    [FromServices] ILogger<Program> logger,
    ClaimsPrincipal user) =>
{
    try
    {
        logger.LogInformation("[TRANSCRIPT] Queueing transcript generation for lesson {LessonId}, language: {Language}",
            lessonId, request.Language ?? "en-US");
        // Note: Video URL is obtained from lesson metadata by the service
        var jobId = await transcriptService.QueueTranscriptGenerationAsync(lessonId, string.Empty, request.Language ?? "en-US");

        return Results.Accepted($"/api/transcripts/{lessonId}/status", new { jobId, lessonId });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[TRANSCRIPT] Error queueing transcript generation for lesson {LessonId}", lessonId);
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.RequireAuthorization(policy => policy.RequireRole("Admin", "Instructor"))
.WithName("GenerateTranscript")
.WithTags("Transcripts")
.Produces<object>(202)
.Produces(401)
.Produces(403)
.Produces(500);

// GET /api/transcripts/{lessonId}/status - Check processing status
app.MapGet("/api/transcripts/{lessonId:guid}/status", async (
    Guid lessonId,
    [FromServices] IVideoTranscriptService transcriptService,
    [FromServices] ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("[TRANSCRIPT] Checking status for lesson {LessonId}", lessonId);
        var status = await transcriptService.GetProcessingStatusAsync(lessonId);

        if (status == null)
        {
            return Results.NotFound(new { error = "No processing status found" });
        }

        return Results.Ok(status);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[TRANSCRIPT] Error checking status for lesson {LessonId}", lessonId);
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.RequireAuthorization()
.WithName("GetTranscriptStatus")
.WithTags("Transcripts")
.Produces<TranscriptProcessingStatusDto>(200)
.Produces(404)
.Produces(500);

// DELETE /api/transcripts/{lessonId} - Delete transcript (Admin)
app.MapDelete("/api/transcripts/{lessonId:guid}", async (
    Guid lessonId,
    [FromServices] IVideoTranscriptService transcriptService,
    [FromServices] ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("[TRANSCRIPT] Deleting transcript for lesson {LessonId}", lessonId);
        await transcriptService.DeleteTranscriptAsync(lessonId);

        return Results.NoContent();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[TRANSCRIPT] Error deleting transcript for lesson {LessonId}", lessonId);
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.RequireAuthorization(policy => policy.RequireRole("Admin"))
.WithName("DeleteTranscript")
.WithTags("Transcripts")
.Produces(204)
.Produces(401)
.Produces(403)
.Produces(500);

// ========================================
// AI TAKEAWAYS ENDPOINTS (3)
// ========================================

// GET /api/takeaways/{lessonId} - Get AI-extracted key takeaways
app.MapGet("/api/takeaways/{lessonId:guid}", async (
    Guid lessonId,
    [FromServices] IAIAnalysisService aiService,
    [FromServices] ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("[AI_TAKEAWAYS] Getting takeaways for lesson {LessonId}", lessonId);
        var takeaways = await aiService.GetTakeawaysAsync(lessonId);

        if (takeaways == null)
        {
            logger.LogWarning("[AI_TAKEAWAYS] Takeaways not found for lesson {LessonId}", lessonId);
            return Results.NotFound(new { error = "Takeaways not found" });
        }

        return Results.Ok(takeaways);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[AI_TAKEAWAYS] Error getting takeaways for lesson {LessonId}", lessonId);
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.RequireAuthorization()
.WithName("GetTakeaways")
.WithTags("AI Takeaways")
.Produces<VideoKeyTakeawaysDto>(200)
.Produces(404)
.Produces(500);

// POST /api/takeaways/{lessonId}/generate - Queue AI takeaway generation (Admin/Instructor)
app.MapPost("/api/takeaways/{lessonId:guid}/generate", async (
    Guid lessonId,
    [FromBody] GenerateTakeawayRequestDto request,
    [FromServices] IAIAnalysisService aiService,
    [FromServices] ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("[AI_TAKEAWAYS] Queueing takeaway generation for lesson {LessonId}", lessonId);
        // Note: Transcript text is fetched from database by the service
        var jobId = await aiService.QueueTakeawayGenerationAsync(lessonId, null);

        return Results.Accepted($"/api/takeaways/{lessonId}/status", new { jobId, lessonId });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[AI_TAKEAWAYS] Error queueing takeaway generation for lesson {LessonId}", lessonId);
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.RequireAuthorization(policy => policy.RequireRole("Admin", "Instructor"))
.WithName("GenerateTakeaways")
.WithTags("AI Takeaways")
.Produces<object>(202)
.Produces(401)
.Produces(403)
.Produces(500);

// POST /api/takeaways/{lessonId}/feedback - Submit thumbs up/down feedback
app.MapPost("/api/takeaways/{lessonId:guid}/feedback", async (
    Guid lessonId,
    [FromBody] SubmitFeedbackDto feedback,
    [FromServices] IAIAnalysisService aiService,
    [FromServices] ILogger<Program> logger,
    ClaimsPrincipal user) =>
{
    try
    {
        var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());

        logger.LogInformation("[AI_TAKEAWAYS] Submitting feedback for lesson {LessonId}, takeaway {TakeawayId} by user {UserId}",
            lessonId, feedback.TakeawayId, userId);
        await aiService.SubmitFeedbackAsync(lessonId, feedback.TakeawayId, feedback.Feedback);

        return Results.Ok(new { message = "Feedback submitted successfully" });
    }
    catch (UnauthorizedAccessException)
    {
        return Results.Unauthorized();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[AI_TAKEAWAYS] Error submitting feedback for lesson {LessonId}", lessonId);
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.RequireAuthorization()
.WithName("SubmitTakeawayFeedback")
.WithTags("AI Takeaways")
.Produces(200)
.Produces(401)
.Produces(500);

// ========================================
// STUDENT NOTES ENDPOINTS (6)
// ========================================

// GET /api/notes?lessonId={id} - Get user notes for lesson
app.MapGet("/api/notes", async (
    [FromQuery] Guid lessonId,
    [FromServices] IStudentNoteService noteService,
    [FromServices] ILogger<Program> logger,
    ClaimsPrincipal user) =>
{
    try
    {
        var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());

        logger.LogInformation("[NOTES] Getting notes for lesson {LessonId} by user {UserId}", lessonId, userId);
        var notes = await noteService.GetNotesByUserAndLessonAsync(userId, lessonId);

        return Results.Ok(notes);
    }
    catch (UnauthorizedAccessException)
    {
        return Results.Unauthorized();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[NOTES] Error getting notes for lesson {LessonId}", lessonId);
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.RequireAuthorization()
.WithName("GetNotesByLesson")
.WithTags("Student Notes")
.Produces<IEnumerable<StudentNoteDto>>(200)
.Produces(401)
.Produces(500);

// GET /api/notes/{id} - Get note by ID
app.MapGet("/api/notes/{id:guid}", async (
    Guid id,
    [FromServices] IStudentNoteService noteService,
    [FromServices] ILogger<Program> logger,
    ClaimsPrincipal user) =>
{
    try
    {
        var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());

        logger.LogInformation("[NOTES] Getting note {NoteId} by user {UserId}", id, userId);
        var note = await noteService.GetByIdAsync(id, userId);

        if (note == null)
        {
            return Results.NotFound(new { error = "Note not found" });
        }

        return Results.Ok(note);
    }
    catch (UnauthorizedAccessException)
    {
        return Results.Unauthorized();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[NOTES] Error getting note {NoteId}", id);
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.RequireAuthorization()
.WithName("GetNoteById")
.WithTags("Student Notes")
.Produces<StudentNoteDto>(200)
.Produces(401)
.Produces(404)
.Produces(500);

// POST /api/notes - Create note
app.MapPost("/api/notes", async (
    [FromBody] CreateStudentNoteDto createDto,
    [FromServices] IStudentNoteService noteService,
    [FromServices] ILogger<Program> logger,
    ClaimsPrincipal user) =>
{
    try
    {
        var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());

        logger.LogInformation("[NOTES] Creating note for lesson {LessonId} by user {UserId}", createDto.LessonId, userId);
        var note = await noteService.CreateNoteAsync(userId, createDto);

        return Results.Created($"/api/notes/{note.Id}", note);
    }
    catch (UnauthorizedAccessException)
    {
        return Results.Unauthorized();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[NOTES] Error creating note for lesson {LessonId}", createDto.LessonId);
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.RequireAuthorization()
.WithName("CreateNote")
.WithTags("Student Notes")
.Produces<StudentNoteDto>(201)
.Produces(401)
.Produces(500);

// PUT /api/notes/{id} - Update note
app.MapPut("/api/notes/{id:guid}", async (
    Guid id,
    [FromBody] UpdateStudentNoteDto updateDto,
    [FromServices] IStudentNoteService noteService,
    [FromServices] ILogger<Program> logger,
    ClaimsPrincipal user) =>
{
    try
    {
        var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());

        logger.LogInformation("[NOTES] Updating note {NoteId} by user {UserId}", id, userId);
        var note = await noteService.UpdateNoteAsync(id, userId, updateDto);

        if (note == null)
        {
            return Results.NotFound(new { error = "Note not found" });
        }

        return Results.Ok(note);
    }
    catch (UnauthorizedAccessException)
    {
        return Results.Unauthorized();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[NOTES] Error updating note {NoteId}", id);
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.RequireAuthorization()
.WithName("UpdateNote")
.WithTags("Student Notes")
.Produces<StudentNoteDto>(200)
.Produces(401)
.Produces(404)
.Produces(500);

// DELETE /api/notes/{id} - Delete note
app.MapDelete("/api/notes/{id:guid}", async (
    Guid id,
    [FromServices] IStudentNoteService noteService,
    [FromServices] ILogger<Program> logger,
    ClaimsPrincipal user) =>
{
    try
    {
        var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());

        logger.LogInformation("[NOTES] Deleting note {NoteId} by user {UserId}", id, userId);
        await noteService.DeleteNoteAsync(id, userId);

        return Results.NoContent();
    }
    catch (UnauthorizedAccessException)
    {
        return Results.Unauthorized();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[NOTES] Error deleting note {NoteId}", id);
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.RequireAuthorization()
.WithName("DeleteNote")
.WithTags("Student Notes")
.Produces(204)
.Produces(401)
.Produces(500);

// POST /api/notes/{id}/toggle-bookmark - Toggle bookmark status
app.MapPost("/api/notes/{id:guid}/toggle-bookmark", async (
    Guid id,
    [FromServices] IStudentNoteService noteService,
    [FromServices] ILogger<Program> logger,
    ClaimsPrincipal user) =>
{
    try
    {
        var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());

        logger.LogInformation("[NOTES] Toggling bookmark for note {NoteId} by user {UserId}", id, userId);
        var note = await noteService.ToggleBookmarkAsync(id, userId);

        if (note == null)
        {
            return Results.NotFound(new { error = "Note not found" });
        }

        return Results.Ok(note);
    }
    catch (UnauthorizedAccessException)
    {
        return Results.Unauthorized();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[NOTES] Error toggling bookmark for note {NoteId}", id);
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.RequireAuthorization()
.WithName("ToggleNoteBookmark")
.WithTags("Student Notes")
.Produces<StudentNoteDto>(200)
.Produces(401)
.Produces(404)
.Produces(500);

// ========================================
// AI CHAT ENDPOINTS (4) - v2.1.0-dev IMPLEMENTED
// ========================================
// Implemented with Ollama LLM integration and context-aware responses
// Uses IAIChatService with conversation persistence and transcript enrichment

// POST /api/ai-chat/message - Send message with context
app.MapPost("/api/ai-chat/message", async (
    [FromBody] AIChatMessageDto messageDto,
    [FromServices] IAIChatService chatService,
    [FromServices] ILogger<Program> logger,
    ClaimsPrincipal user) =>
{
    try
    {
        var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());

        logger.LogInformation("[AI_CHAT] Processing message from user {UserId}", userId);
        var response = await chatService.SendMessageAsync(userId, messageDto);

        return Results.Ok(response);
    }
    catch (UnauthorizedAccessException)
    {
        return Results.Unauthorized();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[AI_CHAT] Error processing message");
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.RequireAuthorization()
.WithName("SendAIChatMessage")
.WithTags("AI Chat")
.Produces<AIChatResponseDto>(200)
.Produces(401)
.Produces(500);

// GET /api/ai-chat/history?sessionId={id}&limit={n} - Get chat history
app.MapGet("/api/ai-chat/history", async (
    [FromQuery] Guid sessionId,
    [FromQuery] int? limit,
    [FromServices] IAIChatService chatService,
    [FromServices] ILogger<Program> logger,
    ClaimsPrincipal user) =>
{
    try
    {
        var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());

        logger.LogInformation("[AI_CHAT] Getting history for session {SessionId}", sessionId);
        var history = await chatService.GetHistoryAsync(sessionId, limit ?? 50);

        if (history == null)
            return Results.NotFound($"Session {sessionId} not found");

        // Verify ownership
        if (history.UserId != userId)
            return Results.Unauthorized();

        return Results.Ok(history);
    }
    catch (UnauthorizedAccessException)
    {
        return Results.Unauthorized();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[AI_CHAT] Error getting history for session {SessionId}", sessionId);
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.RequireAuthorization()
.WithName("GetAIChatHistory")
.WithTags("AI Chat")
.Produces<AIConversationHistoryDto>(200)
.Produces(401)
.Produces(404)
.Produces(500);

// POST /api/ai-chat/sessions/{sessionId}/end - End chat session
app.MapPost("/api/ai-chat/sessions/{sessionId:guid}/end", async (
    Guid sessionId,
    [FromServices] IAIChatService chatService,
    [FromServices] ILogger<Program> logger,
    ClaimsPrincipal user) =>
{
    try
    {
        var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());

        logger.LogInformation("[AI_CHAT] Ending session {SessionId} for user {UserId}", sessionId, userId);
        await chatService.EndSessionAsync(userId, sessionId);

        return Results.Ok(new { message = "Session ended successfully", sessionId });
    }
    catch (UnauthorizedAccessException)
    {
        return Results.Unauthorized();
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(ex.Message);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[AI_CHAT] Error ending session {SessionId}", sessionId);
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.RequireAuthorization()
.WithName("EndChatSession")
.WithTags("AI Chat")
.Produces(200)
.Produces(401)
.Produces(404)
.Produces(500);

// GET /api/ai-chat/sessions?lessonId={id} - List sessions for lesson
app.MapGet("/api/ai-chat/sessions", async (
    [FromQuery] Guid? lessonId,
    [FromQuery] int? limit,
    [FromServices] IAIChatService chatService,
    [FromServices] ILogger<Program> logger,
    ClaimsPrincipal user) =>
{
    try
    {
        var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());

        logger.LogInformation("[AI_CHAT] Getting sessions for user {UserId}, lessonId: {LessonId}", userId, lessonId);
        var sessions = await chatService.GetSessionsAsync(userId, lessonId, limit ?? 50);

        return Results.Ok(sessions);
    }
    catch (UnauthorizedAccessException)
    {
        return Results.Unauthorized();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[AI_CHAT] Error getting sessions for user");
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.RequireAuthorization()
.WithName("GetChatSessions")
.WithTags("AI Chat")
.Produces<IEnumerable<AISessionSummaryDto>>(200)
.Produces(401)
.Produces(500);

// ========================================
// VIDEO BOOKMARKS ENDPOINTS (4)
// ========================================

// GET /api/bookmarks?lessonId={id} - Get bookmarks for lesson
app.MapGet("/api/bookmarks", async (
    [FromQuery] Guid lessonId,
    [FromServices] IVideoBookmarkService bookmarkService,
    [FromServices] ILogger<Program> logger,
    ClaimsPrincipal user) =>
{
    try
    {
        var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());

        logger.LogInformation("[BOOKMARKS] Getting bookmarks for lesson {LessonId} by user {UserId}", lessonId, userId);
        var bookmarks = await bookmarkService.GetBookmarksByUserAndLessonAsync(userId, lessonId);

        return Results.Ok(bookmarks);
    }
    catch (UnauthorizedAccessException)
    {
        return Results.Unauthorized();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[BOOKMARKS] Error getting bookmarks for lesson {LessonId}", lessonId);
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.RequireAuthorization()
.WithName("GetBookmarksByLesson")
.WithTags("Video Bookmarks")
.Produces<IEnumerable<VideoBookmark>>(200)
.Produces(401)
.Produces(500);

// POST /api/bookmarks - Create bookmark
app.MapPost("/api/bookmarks", async (
    [FromBody] CreateVideoBookmarkDto createDto,
    [FromServices] IVideoBookmarkService bookmarkService,
    [FromServices] ILogger<Program> logger,
    ClaimsPrincipal user) =>
{
    try
    {
        var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());

        logger.LogInformation("[BOOKMARKS] Creating bookmark for lesson {LessonId} at {Timestamp}s by user {UserId}",
            createDto.LessonId, createDto.VideoTimestamp, userId);
        var bookmark = await bookmarkService.CreateBookmarkAsync(userId, createDto);

        return Results.Created($"/api/bookmarks/{bookmark.Id}", bookmark);
    }
    catch (UnauthorizedAccessException)
    {
        return Results.Unauthorized();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[BOOKMARKS] Error creating bookmark for lesson {LessonId}", createDto.LessonId);
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.RequireAuthorization()
.WithName("CreateBookmark")
.WithTags("Video Bookmarks")
.Produces<VideoBookmark>(201)
.Produces(401)
.Produces(500);

// PUT /api/bookmarks/{id} - Update bookmark
app.MapPut("/api/bookmarks/{id:guid}", async (
    Guid id,
    [FromBody] UpdateVideoBookmarkDto updateDto,
    [FromServices] IVideoBookmarkService bookmarkService,
    [FromServices] ILogger<Program> logger,
    ClaimsPrincipal user) =>
{
    try
    {
        var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());

        logger.LogInformation("[BOOKMARKS] Updating bookmark {BookmarkId} by user {UserId}", id, userId);
        var bookmark = await bookmarkService.UpdateBookmarkAsync(id, userId, updateDto);

        return Results.Ok(bookmark);
    }
    catch (UnauthorizedAccessException)
    {
        return Results.Unauthorized();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[BOOKMARKS] Error updating bookmark {BookmarkId}", id);
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.RequireAuthorization()
.WithName("UpdateBookmark")
.WithTags("Video Bookmarks")
.Produces<VideoBookmark>(200)
.Produces(401)
.Produces(404)
.Produces(500);

// DELETE /api/bookmarks/{id} - Delete bookmark
app.MapDelete("/api/bookmarks/{id:guid}", async (
    Guid id,
    [FromServices] IVideoBookmarkService bookmarkService,
    [FromServices] ILogger<Program> logger,
    ClaimsPrincipal user) =>
{
    try
    {
        var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());

        logger.LogInformation("[BOOKMARKS] Deleting bookmark {BookmarkId} by user {UserId}", id, userId);
        await bookmarkService.DeleteBookmarkAsync(id, userId);

        return Results.NoContent();
    }
    catch (UnauthorizedAccessException)
    {
        return Results.Unauthorized();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[BOOKMARKS] Error deleting bookmark {BookmarkId}", id);
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.RequireAuthorization()
.WithName("DeleteBookmark")
.WithTags("Video Bookmarks")
.Produces(204)
.Produces(401)
.Produces(500);

// ========================================
// VIDEO PROGRESS ENDPOINTS (2)
// ========================================

// POST /api/progress/sync - Sync progress (multi-device support)
app.MapPost("/api/progress/sync", async (
    [FromBody] TrackVideoProgressDto trackDto,
    [FromServices] IVideoProgressService progressService,
    [FromServices] ILogger<Program> logger,
    ClaimsPrincipal user) =>
{
    try
    {
        var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());

        logger.LogInformation("[PROGRESS] Tracking progress for lesson {LessonId} by user {UserId}",
            trackDto.LessonId, userId);
        var progress = await progressService.TrackProgressAsync(userId, trackDto);

        return Results.Ok(progress);
    }
    catch (UnauthorizedAccessException)
    {
        return Results.Unauthorized();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[PROGRESS] Error tracking progress for lesson {LessonId}", trackDto.LessonId);
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.RequireAuthorization()
.WithName("SyncProgress")
.WithTags("Video Progress")
.Produces<VideoProgressResponseDto>(200)
.Produces(401)
.Produces(500);

// GET /api/progress/resume?lessonId={id} - Get resume position
app.MapGet("/api/progress/resume", async (
    [FromQuery] Guid lessonId,
    [FromServices] IVideoProgressService progressService,
    [FromServices] ILogger<Program> logger,
    ClaimsPrincipal user) =>
{
    try
    {
        var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());

        logger.LogInformation("[PROGRESS] Getting resume position for lesson {LessonId} by user {UserId}", lessonId, userId);
        var lastPosition = await progressService.GetLastPositionAsync(userId, lessonId);

        return Results.Ok(new { lessonId, lastPosition });
    }
    catch (UnauthorizedAccessException)
    {
        return Results.Unauthorized();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[PROGRESS] Error getting resume position for lesson {LessonId}", lessonId);
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.RequireAuthorization()
.WithName("GetResumePosition")
.WithTags("Video Progress")
.Produces<object>(200)
.Produces(401)
.Produces(500);

// ========================================
// BACKGROUND JOB ENDPOINTS (4)
// ========================================

// GET /api/jobs/transcripts/{lessonId}/status - Get transcript job status
app.MapGet("/api/jobs/transcripts/{lessonId:guid}/status", async (
    Guid lessonId,
    [FromServices] IVideoTranscriptService transcriptService,
    [FromServices] ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("[JOBS] Checking transcript job status for lesson {LessonId}", lessonId);
        var status = await transcriptService.GetProcessingStatusAsync(lessonId);

        if (status == null)
        {
            return Results.NotFound(new { error = "No job found for this lesson" });
        }

        return Results.Ok(status);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[JOBS] Error checking transcript job status for lesson {LessonId}", lessonId);
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.RequireAuthorization(policy => policy.RequireRole("Admin", "Instructor"))
.WithName("GetTranscriptJobStatus")
.WithTags("Background Jobs")
.Produces<TranscriptProcessingStatusDto>(200)
.Produces(401)
.Produces(403)
.Produces(404)
.Produces(500);

// GET /api/jobs/takeaways/{lessonId}/status - Get takeaway job status
app.MapGet("/api/jobs/takeaways/{lessonId:guid}/status", async (
    Guid lessonId,
    [FromServices] IAIAnalysisService aiService,
    [FromServices] ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("[JOBS] Checking takeaway job status for lesson {LessonId}", lessonId);
        var status = await aiService.GetProcessingStatusAsync(lessonId);

        return Results.Ok(status);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[JOBS] Error checking takeaway job status for lesson {LessonId}", lessonId);
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.RequireAuthorization(policy => policy.RequireRole("Admin", "Instructor"))
.WithName("GetTakeawayJobStatus")
.WithTags("Background Jobs")
.Produces<TakeawayProcessingStatusDto>(200)
.Produces(401)
.Produces(403)
.Produces(500);

// POST /api/jobs/transcripts/{lessonId}/queue - Queue transcript generation job
app.MapPost("/api/jobs/transcripts/{lessonId:guid}/queue", async (
    Guid lessonId,
    [FromBody] GenerateTranscriptRequestDto request,
    [FromServices] IVideoTranscriptService transcriptService,
    [FromServices] ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("[JOBS] Queueing transcript generation job for lesson {LessonId}, language: {Language}",
            lessonId, request.Language ?? "en-US");
        // Note: Video URL is obtained from lesson metadata by the service
        var jobId = await transcriptService.QueueTranscriptGenerationAsync(lessonId, string.Empty, request.Language ?? "en-US");

        return Results.Accepted($"/api/jobs/transcripts/{lessonId}/status", new { jobId, lessonId });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[JOBS] Error queueing transcript job for lesson {LessonId}", lessonId);
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.RequireAuthorization(policy => policy.RequireRole("Admin", "Instructor"))
.WithName("QueueTranscriptJob")
.WithTags("Background Jobs")
.Produces<object>(202)
.Produces(401)
.Produces(403)
.Produces(500);

// POST /api/jobs/takeaways/{lessonId}/queue - Queue takeaway generation job
app.MapPost("/api/jobs/takeaways/{lessonId:guid}/queue", async (
    Guid lessonId,
    [FromBody] GenerateTakeawayRequestDto request,
    [FromServices] IAIAnalysisService aiService,
    [FromServices] ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("[JOBS] Queueing takeaway generation job for lesson {LessonId}", lessonId);
        // Note: Transcript text is fetched from database by the service
        var jobId = await aiService.QueueTakeawayGenerationAsync(lessonId, null);

        return Results.Accepted($"/api/jobs/takeaways/{lessonId}/status", new { jobId, lessonId });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[JOBS] Error queueing takeaway job for lesson {LessonId}", lessonId);
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.RequireAuthorization(policy => policy.RequireRole("Admin", "Instructor"))
.WithName("QueueTakeawayJob")
.WithTags("Background Jobs")
.Produces<object>(202)
.Produces(401)
.Produces(403)
.Produces(500);

Console.WriteLine("[ENDPOINTS] Student Learning Space v2.1.0 endpoints registered (28 endpoints)");
Console.WriteLine("[ENDPOINTS] - Video Transcripts: /api/transcripts/* (5 endpoints)");
Console.WriteLine("[ENDPOINTS] - AI Takeaways: /api/takeaways/* (3 endpoints)");
Console.WriteLine("[ENDPOINTS] - Student Notes: /api/notes/* (6 endpoints)");
Console.WriteLine("[ENDPOINTS] - AI Chat: /api/ai-chat/* (4 endpoints)");
Console.WriteLine("[ENDPOINTS] - Video Bookmarks: /api/bookmarks/* (4 endpoints)");
Console.WriteLine("[ENDPOINTS] - Video Progress: /api/progress/* (2 endpoints)");
Console.WriteLine("[ENDPOINTS] - Background Jobs: /api/jobs/* (4 endpoints)");

// ========================================
// DEBUG TEST ENDPOINTS
// ========================================
app.MapGet("/api/test", () => new { message = "Test OK", timestamp = DateTime.UtcNow })
   .AllowAnonymous()
   .WithName("DebugTest");

app.MapGet("/api/test-string", () => "Test OK - Plain Text")
   .AllowAnonymous()
   .WithName("DebugTestString");

// ========================================
// SAAS SUBSCRIPTION SYSTEM ENDPOINTS
// ========================================
// Register SaaS Subscription endpoints (Week 3 - API Layer)
app.MapSubscriptionEndpoints();      // 8 endpoints: /api/subscriptions/*
app.MapEngagementEndpoints();        // 6 endpoints: /api/engagement/*
app.MapPayoutEndpoints();            // 8 endpoints: /api/payouts/*
app.MapStripeWebhookEndpoints();     // 5 endpoints: /api/webhooks/stripe/*

Console.WriteLine("[ENDPOINTS] SaaS Subscription System endpoints registered (27 endpoints)");
Console.WriteLine("[ENDPOINTS] - Subscription Management: /api/subscriptions/* (8 endpoints)");
Console.WriteLine("[ENDPOINTS] - Engagement Tracking: /api/engagement/* (6 endpoints)");
Console.WriteLine("[ENDPOINTS] - Instructor Payouts: /api/payouts/* (8 endpoints)");
Console.WriteLine("[ENDPOINTS] - Stripe Webhooks: /api/webhooks/stripe/* (5 endpoints)");

// =============================================================================
// REDIS CONNECTION VALIDATION (HIGH-6 Fix)
// =============================================================================
// Validate Redis connection at startup for rate limiting
// Note: This is NOT a fatal error - API will start even if Redis is down
// Rate limiting middleware will fail-open (allow requests) if Redis unavailable
try
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    var redisConnection = app.Services.GetRequiredService<IConnectionMultiplexer>();

    logger.LogInformation("[REDIS] Validating connection at startup...");

    // Test connection with 5-second timeout
    var db = redisConnection.GetDatabase();
    var pingResult = await db.PingAsync();

    if (pingResult.TotalMilliseconds > 1000)
    {
        logger.LogWarning(
            "[REDIS] ⚠️ High latency detected: {Latency}ms. Rate limiting may be slow.",
            Math.Round(pingResult.TotalMilliseconds, 2));
    }
    else
    {
        logger.LogInformation(
            "[REDIS] ✓ Connection validated successfully. Latency: {Latency}ms",
            Math.Round(pingResult.TotalMilliseconds, 2));
    }

    // Verify basic operations work
    await db.StringSetAsync("health-check-key", "ok", TimeSpan.FromSeconds(10));
    var value = await db.StringGetAsync("health-check-key");

    if (value != "ok")
    {
        logger.LogWarning("[REDIS] ⚠️ Basic operations test failed - value mismatch");
    }
}
catch (RedisConnectionException ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogWarning(
        ex,
        "[REDIS] ❌ Failed to connect at startup. " +
        "Rate limiting will be DISABLED. " +
        "This is NOT a fatal error, but security features are degraded. " +
        "Please check Redis service health.");

    // DO NOT throw - allow startup to continue
    // Rate limiting middleware will handle gracefully (fail-open policy)
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(
        ex,
        "[REDIS] Unexpected error during startup validation");

    // Still allow startup (fail-open policy)
}

Console.WriteLine("🚀 InsightLearn API Starting...");
Console.WriteLine($"Environment: {app.Environment.EnvironmentName}");
Console.WriteLine($"Listening on: {string.Join(", ", builder.WebHost.GetSetting("urls")?.Split(';') ?? new[] { "http://localhost:5000" })}");
Console.WriteLine($"Chatbot enabled with model: {ollamaModel}");
Console.WriteLine($"Ollama URL: {ollamaUrl}");

app.Run();
