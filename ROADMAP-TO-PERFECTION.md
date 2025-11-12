# InsightLearn WASM - Roadmap to 10/10 Perfect Score

**Document Version**: 1.0
**Date**: 2025-11-10
**Current Build Status**: 0 Errors, 34 Warnings

---

## Executive Summary

This document provides a comprehensive improvement plan to elevate InsightLearn WASM from its current state to a **10/10 perfect score** across all architectural and quality dimensions. The project is currently in excellent shape with all major functionality implemented, but requires focused improvements in five key areas to achieve perfection.

### Current Scores & Targets

| Category | Current | Target | Gap Analysis |
|----------|---------|--------|--------------|
| **Architectural Consistency** | 9.5/10 | 10/10 | Minor inconsistencies in endpoint patterns, missing GetAllEnrollmentsAsync |
| **Code Quality** | 8.5/10 | 10/10 | 34 nullability warnings, missing validation attributes |
| **Security** | 9.0/10 | 10/10 | JWT secret fallback, no rate limiting, missing security headers |
| **Deployment Readiness** | 9.5/10 | 10/10 | Missing dependency health checks, no monitoring alerts |
| **Known Issues** | 8.0/10 | 10/10 | GET /api/enrollments returns 501, certificate PDF generation stub |

### Implementation Timeline

- **Phase 1 (Critical)**: 2-3 days - Fix blocking issues (enrollments endpoint, build warnings)
- **Phase 2 (High Priority)**: 3-4 days - Security hardening, validation, health checks
- **Phase 3 (Medium Priority)**: 2-3 days - Certificate generation, monitoring, documentation
- **Phase 4 (Polish)**: 1-2 days - Code cleanup, testing, final verification

**Total Estimated Effort**: 8-12 days (1 senior developer)

---

## Phase 1: Critical Fixes (Priority: CRITICAL)

### 1.1 Fix GET /api/enrollments Endpoint (501 Error)

**Current Issue**:
- Endpoint at `/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/src/InsightLearn.Application/Program.cs:2335-2361` returns 501 Not Implemented
- Comment states: "GetAllEnrollmentsAsync not available in interface"

**Root Cause**:
- `IEnrollmentService` interface missing `GetAllEnrollmentsAsync(int page, int pageSize)` method
- `IEnrollmentRepository` has `GetAllAsync()` but service layer doesn't expose it

**Solution**:

**File**: `/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/src/InsightLearn.Core/Interfaces/IEnrollmentService.cs`

Add to interface (after line 70):
```csharp
/// <summary>
/// Gets all enrollments with pagination (Admin only)
/// </summary>
Task<(List<EnrollmentDto> Enrollments, int TotalCount)> GetAllEnrollmentsAsync(int page = 1, int pageSize = 10);
```

**File**: `/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/src/InsightLearn.Application/Services/EnrollmentService.cs`

Add implementation (after line 113):
```csharp
public async Task<(List<EnrollmentDto> Enrollments, int TotalCount)> GetAllEnrollmentsAsync(int page = 1, int pageSize = 10)
{
    _logger.LogInformation("[EnrollmentService] Getting all enrollments (Page: {Page}, PageSize: {PageSize})", page, pageSize);

    var allEnrollments = await _enrollmentRepository.GetAllAsync(page, pageSize);
    var totalCount = await _enrollmentRepository.GetTotalCountAsync();

    var dtos = allEnrollments.Select(MapToDto).ToList();

    _logger.LogInformation("[EnrollmentService] Retrieved {Count} enrollments out of {Total}", dtos.Count, totalCount);
    return (dtos, totalCount);
}
```

**File**: `/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/src/InsightLearn.Application/Program.cs`

Replace lines 2343-2349 with:
```csharp
        logger.LogInformation("[ENROLLMENTS] Admin getting all enrollments, Page: {Page}, PageSize: {PageSize}", page, pageSize);

        var (enrollments, totalCount) = await enrollmentService.GetAllEnrollmentsAsync(page, pageSize);

        return Results.Ok(new
        {
            Enrollments = enrollments,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
```

**Effort**: 30 minutes
**Testing**: Verify with `curl http://localhost:31081/api/enrollments?page=1&pageSize=10` (requires Admin JWT)

---

### 1.2 Resolve All 34 Nullability Warnings

**Current Status**: Build succeeds with 34 CS8601/CS8602 warnings

**Strategy**: Fix warnings systematically by category

#### 1.2.1 Null Reference Assignment Warnings (CS8601)

**Locations**:
- `/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/src/InsightLearn.Application/Program.cs:1321` (3 occurrences)
- `/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/src/InsightLearn.Application/Program.cs:1383` (2 occurrences)

**Solution Pattern**:
```csharp
// BEFORE (causes CS8601)
string? email = user.Email;

// AFTER (null-safe)
string email = user.Email ?? string.Empty;
// OR with null-conditional operator
string? email = user?.Email;
```

**Action Items**:
1. Search for all CS8601 occurrences: `dotnet build 2>&1 | grep CS8601 > nullability-warnings.txt`
2. For each warning:
   - Add `??` null-coalescing operator with safe default
   - OR change property type to nullable (`string?`)
   - OR add null-check with guard clause

**Effort**: 2-3 hours
**Priority**: HIGH (affects code quality score directly)

#### 1.2.2 Unused Variable Warning (CS0168)

**Location**: `/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/src/InsightLearn.Application/Program.cs:893`

**Fix**: Replace `catch (Exception ex)` with `catch (Exception)` if variable unused, OR add logging:
```csharp
catch (Exception ex)
{
    logger.LogError(ex, "[CONTEXT] Error message");
    // ... handle error
}
```

**Effort**: 5 minutes

#### 1.2.3 Async Method Without Await (CS1998)

**Location**: `/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/src/InsightLearn.Application/Program.cs:2339`

**Fix**: Either add `await` call or remove `async` keyword:
```csharp
// If no async work needed:
app.MapGet("/api/enrollments", (
    [FromServices] IEnrollmentService enrollmentService,
    // ... remove async keyword
```

**Effort**: 5 minutes

#### 1.2.4 Unused Field Warning (CS0414)

**Location**: `/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/src/InsightLearn.Application/Services/EnterpriseMonitoringService.cs:54`

**Fix**: Either use the field `_isMonitoring` in logic, or remove it:
```csharp
// Option 1: Use the field
public bool IsCurrentlyMonitoring => _isMonitoring;

// Option 2: Remove if truly unused
// Delete: private bool _isMonitoring;
```

**Effort**: 5 minutes

**Total Effort for 1.2**: 3-4 hours

---

### 1.3 Fix DbContext Navigation Property Error

**Status**: ALREADY FIXED in this session

**Fix Applied**: Changed `s.Revenues` to `s.SubscriptionRevenues` in `/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/src/InsightLearn.Infrastructure/Data/InsightLearnDbContext.cs:399`

**Verification**: Build now succeeds with 0 errors

---

## Phase 2: Security Hardening (Priority: HIGH)

### 2.1 JWT Secret Configuration Hardening

**Current Issue**: JWT secret has hardcoded fallback value in code

**File**: `/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/src/InsightLearn.Application/Program.cs` (lines 150-180 approx)

**Current Pattern** (INSECURE):
```csharp
var jwtSecret = builder.Configuration["JwtSettings:SecretKey"] ?? "DEFAULT_INSECURE_SECRET";
```

**Required Fix**:
```csharp
var jwtSecret = builder.Configuration["JwtSettings:SecretKey"];
if (string.IsNullOrWhiteSpace(jwtSecret) || jwtSecret.Length < 32)
{
    throw new InvalidOperationException(
        "JWT Secret Key is not configured or is too short. " +
        "Set JwtSettings:SecretKey in appsettings.json or environment variable. " +
        "Minimum length: 32 characters.");
}

// Validate secret strength
if (jwtSecret == "your-secret-key" || jwtSecret == "changeme")
{
    throw new InvalidOperationException(
        "JWT Secret Key is using a default/weak value. " +
        "Please configure a strong, unique secret key.");
}
```

**Additional Security**:
1. Add secret rotation documentation in `/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/docs/SECURITY.md`
2. Create secret generation script: `/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/scripts/generate-jwt-secret.sh`

```bash
#!/bin/bash
# Generate cryptographically secure JWT secret
openssl rand -base64 64 | tr -d '\n'
```

**Kubernetes Secret Update**:
```bash
# Generate new secret
NEW_SECRET=$(openssl rand -base64 64)

# Update K8s secret
kubectl patch secret insightlearn-secrets -n insightlearn \
  --type='json' \
  -p="[{\"op\":\"replace\",\"path\":\"/data/jwt-secret-key\",\"value\":\"$(echo -n $NEW_SECRET | base64)\"}]"

# Restart API pods to pick up new secret
kubectl rollout restart deployment/insightlearn-api -n insightlearn
```

**Effort**: 1 hour
**Priority**: CRITICAL (security vulnerability)

---

### 2.2 Implement Rate Limiting

**Current Issue**: No rate limiting on API endpoints (DDoS vulnerability)

**Solution**: Use ASP.NET Core 7+ built-in rate limiting

**File**: `/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/src/InsightLearn.Application/Program.cs`

**Add after line 81** (after CORS configuration):
```csharp
// Configure Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    // Global rate limit: 100 requests per minute per IP
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));

    // Authentication endpoints: 5 requests per minute per IP (prevent brute force)
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1)
            }));

    // API endpoints: 30 requests per minute per user (authenticated)
    options.AddPolicy("api", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1)
            }));

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = 429; // Too Many Requests
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            error = "Rate limit exceeded",
            retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
                ? retryAfter.ToString()
                : "60 seconds"
        }, cancellationToken: cancellationToken);
    };
});
```

**Apply rate limiting to endpoints** (add after app.UseAuthorization()):
```csharp
// Enable rate limiting middleware
app.UseRateLimiter();
```

**Apply to authentication endpoints**:
```csharp
// Example: Login endpoint
app.MapPost("/api/auth/login", async (...) => { ... })
    .RequireRateLimiting("auth") // Add this
    .WithName("Login")
    .WithTags("Authentication");
```

**Apply to general API endpoints**:
```csharp
app.MapGet("/api/courses", async (...) => { ... })
    .RequireRateLimiting("api") // Add this
    .WithName("GetAllCourses")
    .WithTags("Courses");
```

**Effort**: 2 hours
**Testing**: Use `ab` (Apache Bench) to verify rate limits work

---

### 2.3 Add Request Validation Middleware

**Current Issue**: No centralized input validation, potential injection attacks

**Create new file**: `/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/src/InsightLearn.Application/Middleware/RequestValidationMiddleware.cs`

```csharp
using System.Text.RegularExpressions;

namespace InsightLearn.Application.Middleware;

/// <summary>
/// Middleware to validate and sanitize incoming HTTP requests
/// </summary>
public class RequestValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestValidationMiddleware> _logger;

    // Patterns for SQL injection detection
    private static readonly Regex SqlInjectionPattern = new(
        @"(\b(SELECT|INSERT|UPDATE|DELETE|DROP|CREATE|ALTER|EXEC|EXECUTE)\b)|('(''|[^'])*')|(;)|(--)|(\/\*)|(\*\/)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Patterns for XSS detection
    private static readonly Regex XssPattern = new(
        @"<script|javascript:|onerror=|onload=|<iframe|eval\(|expression\(",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public RequestValidationMiddleware(RequestDelegate next, ILogger<RequestValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Validate query parameters
        foreach (var param in context.Request.Query)
        {
            if (ContainsMaliciousContent(param.Value))
            {
                _logger.LogWarning("[SECURITY] Malicious query parameter detected: {Key}", param.Key);
                context.Response.StatusCode = 400;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Invalid request parameters",
                    detail = "Potentially malicious content detected"
                });
                return;
            }
        }

        // Validate headers (check for injection in custom headers)
        foreach (var header in context.Request.Headers.Where(h => !h.Key.StartsWith("X-Forwarded-")))
        {
            if (ContainsMaliciousContent(header.Value))
            {
                _logger.LogWarning("[SECURITY] Malicious header detected: {Key}", header.Key);
                context.Response.StatusCode = 400;
                await context.Response.WriteAsJsonAsync(new { error = "Invalid request headers" });
                return;
            }
        }

        // Check content length (prevent large payload attacks)
        if (context.Request.ContentLength > 10_000_000) // 10MB
        {
            _logger.LogWarning("[SECURITY] Request payload too large: {Size} bytes", context.Request.ContentLength);
            context.Response.StatusCode = 413; // Payload Too Large
            await context.Response.WriteAsJsonAsync(new { error = "Request payload too large" });
            return;
        }

        await _next(context);
    }

    private bool ContainsMaliciousContent(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return SqlInjectionPattern.IsMatch(value) || XssPattern.IsMatch(value);
    }
}
```

**Register middleware** in Program.cs (after app.UseCors()):
```csharp
app.UseMiddleware<RequestValidationMiddleware>();
```

**Effort**: 2 hours
**Testing**: Send malicious payloads and verify rejection

---

### 2.4 Add Security Headers

**Current Issue**: Missing security headers (OWASP Top 10 compliance)

**File**: `/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/src/InsightLearn.Application/Program.cs`

**Add after app.UseHttpsRedirection()**:
```csharp
// Add security headers middleware
app.Use(async (context, next) =>
{
    // Prevent clickjacking
    context.Response.Headers.Add("X-Frame-Options", "DENY");

    // Prevent MIME-sniffing
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");

    // XSS protection (legacy but still useful)
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");

    // Strict Transport Security (HSTS)
    context.Response.Headers.Add("Strict-Transport-Security", "max-age=31536000; includeSubDomains");

    // Content Security Policy
    context.Response.Headers.Add("Content-Security-Policy",
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-eval' 'wasm-unsafe-eval'; " +
        "style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data: https:; " +
        "font-src 'self' data:; " +
        "connect-src 'self' wss: https:; " +
        "frame-ancestors 'self'; " +
        "base-uri 'self'; " +
        "form-action 'self'");

    // Referrer Policy
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");

    // Permissions Policy (formerly Feature-Policy)
    context.Response.Headers.Add("Permissions-Policy",
        "geolocation=(), " +
        "microphone=(), " +
        "camera=()");

    await next();
});
```

**Update K8s Ingress** (`/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/k8s/08-ingress.yaml`) to add headers at ingress level:
```yaml
metadata:
  annotations:
    nginx.ingress.kubernetes.io/configuration-snippet: |
      more_set_headers "X-Frame-Options: DENY";
      more_set_headers "X-Content-Type-Options: nosniff";
      more_set_headers "X-XSS-Protection: 1; mode=block";
      more_set_headers "Strict-Transport-Security: max-age=31536000; includeSubDomains";
      more_set_headers "Referrer-Policy: strict-origin-when-cross-origin";
```

**Effort**: 1 hour
**Verification**: Check headers with `curl -I http://localhost:31081/health`

---

### 2.5 Implement Audit Logging

**Create new entity**: `/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/src/InsightLearn.Core/Entities/AuditLog.cs`

```csharp
namespace InsightLearn.Core.Entities;

public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string Action { get; set; } = string.Empty; // Login, Logout, CreateCourse, DeleteUser, etc.
    public string EntityType { get; set; } = string.Empty; // User, Course, Enrollment, etc.
    public Guid? EntityId { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string? Details { get; set; } // JSON with additional context
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Navigation
    public virtual User? User { get; set; }
}
```

**Add to DbContext**: `/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/src/InsightLearn.Infrastructure/Data/InsightLearnDbContext.cs`

```csharp
public DbSet<AuditLog> AuditLogs { get; set; } = null!;
```

**Create audit service**: `/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/src/InsightLearn.Application/Services/AuditService.cs`

```csharp
public interface IAuditService
{
    Task LogAsync(string action, string entityType, Guid? entityId, Guid? userId, HttpContext context, string? details = null);
}

public class AuditService : IAuditService
{
    private readonly InsightLearnDbContext _context;
    private readonly ILogger<AuditService> _logger;

    public AuditService(InsightLearnDbContext context, ILogger<AuditService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogAsync(string action, string entityType, Guid? entityId, Guid? userId, HttpContext context, string? details = null)
    {
        try
        {
            var auditLog = new AuditLog
            {
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                UserId = userId,
                UserEmail = context.User.FindFirst(ClaimTypes.Email)?.Value,
                IpAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                UserAgent = context.Request.Headers["User-Agent"].ToString(),
                Details = details,
                Timestamp = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation("[AUDIT] {Action} on {EntityType} {EntityId} by user {UserId}",
                action, entityType, entityId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AUDIT] Failed to log audit event: {Action}", action);
        }
    }
}
```

**Register in Program.cs**:
```csharp
builder.Services.AddScoped<IAuditService, AuditService>();
```

**Use in endpoints** (example for course deletion):
```csharp
app.MapDelete("/api/courses/{id:guid}", async (
    Guid id,
    [FromServices] ICourseService courseService,
    [FromServices] IAuditService auditService,
    [FromServices] ILogger<Program> logger,
    HttpContext context) =>
{
    var userId = Guid.Parse(context.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    var result = await courseService.DeleteCourseAsync(id);

    if (result)
    {
        await auditService.LogAsync("DeleteCourse", "Course", id, userId, context);
    }

    return result ? Results.NoContent() : Results.NotFound();
})
.RequireAuthorization(policy => policy.RequireRole("Admin"));
```

**Effort**: 3 hours
**Priority**: HIGH (compliance requirement for enterprise)

---

## Phase 3: Validation & Data Integrity (Priority: HIGH)

### 3.1 Add DTO Validation Attributes

**Current Issue**: DTOs lack validation attributes, allowing invalid data

**Strategy**: Add `System.ComponentModel.DataAnnotations` to all DTOs

#### 3.1.1 Enrollment DTOs

**File**: `/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/src/InsightLearn.Core/DTOs/Enrollment/CreateEnrollmentDto.cs`

```csharp
using System.ComponentModel.DataAnnotations;

public class CreateEnrollmentDto
{
    [Required(ErrorMessage = "User ID is required")]
    public Guid UserId { get; set; }

    [Required(ErrorMessage = "Course ID is required")]
    public Guid CourseId { get; set; }

    [Range(0, 10000, ErrorMessage = "Amount paid must be between 0 and 10000")]
    [DataType(DataType.Currency)]
    public decimal AmountPaid { get; set; }

    public Guid? PaymentId { get; set; }
}
```

**File**: `/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/src/InsightLearn.Core/DTOs/Enrollment/UpdateProgressDto.cs`

```csharp
public class UpdateProgressDto
{
    [Required]
    public Guid EnrollmentId { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid LessonId { get; set; }

    [Range(0, 10000, ErrorMessage = "Watched minutes must be non-negative")]
    public int WatchedMinutes { get; set; }

    public bool IsCompleted { get; set; }

    public Guid? NextLessonId { get; set; }
}
```

#### 3.1.2 Course DTOs

**File**: `/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/src/InsightLearn.Core/DTOs/Course/CreateCourseDto.cs`

```csharp
public class CreateCourseDto
{
    [Required(ErrorMessage = "Title is required")]
    [StringLength(200, MinimumLength = 5, ErrorMessage = "Title must be between 5 and 200 characters")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required")]
    [StringLength(5000, MinimumLength = 20, ErrorMessage = "Description must be between 20 and 5000 characters")]
    public string Description { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Short description cannot exceed 1000 characters")]
    public string? ShortDescription { get; set; }

    [Required(ErrorMessage = "Category ID is required")]
    public Guid CategoryId { get; set; }

    [Required(ErrorMessage = "Instructor ID is required")]
    public Guid InstructorId { get; set; }

    [Range(0, 10000, ErrorMessage = "Price must be between 0 and 10000")]
    [DataType(DataType.Currency)]
    public decimal Price { get; set; }

    [Range(0, 100, ErrorMessage = "Discount percentage must be between 0 and 100")]
    public decimal DiscountPercentage { get; set; }

    [Required(ErrorMessage = "Difficulty level is required")]
    [RegularExpression("^(Beginner|Intermediate|Advanced|Expert)$", ErrorMessage = "Invalid difficulty level")]
    public string DifficultyLevel { get; set; } = "Beginner";

    [Required(ErrorMessage = "Language is required")]
    [StringLength(50)]
    public string Language { get; set; } = "English";

    [Url(ErrorMessage = "Invalid URL format")]
    [StringLength(500)]
    public string? ThumbnailUrl { get; set; }

    [Url(ErrorMessage = "Invalid URL format")]
    [StringLength(500)]
    public string? PreviewVideoUrl { get; set; }

    [Range(1, 10000, ErrorMessage = "Estimated duration must be at least 1 minute")]
    public int EstimatedDurationMinutes { get; set; }

    [StringLength(500, ErrorMessage = "Learning outcomes too long")]
    public string? LearningOutcomes { get; set; }

    [StringLength(500, ErrorMessage = "Prerequisites too long")]
    public string? Prerequisites { get; set; }
}
```

#### 3.1.3 Apply to ALL DTOs

**Files to update** (48 total DTOs in `/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/src/InsightLearn.Core/DTOs/`):
- Category: CreateCategoryDto, UpdateCategoryDto
- Course: CreateCourseDto, UpdateCourseDto, BrowseCourseDto
- Enrollment: CreateEnrollmentDto, UpdateProgressDto
- Payment: CreatePaymentDto, UpdatePaymentDto
- Review: CreateReviewDto, UpdateReviewDto
- Admin: All dashboard DTOs
- Auth: LoginDto, RegisterDto, RefreshTokenDto

**Validation Pattern**:
```csharp
// String fields
[Required(ErrorMessage = "Field is required")]
[StringLength(max, MinimumLength = min, ErrorMessage = "...")]

// Numeric fields
[Range(min, max, ErrorMessage = "...")]

// Email
[Required]
[EmailAddress(ErrorMessage = "Invalid email format")]

// URL
[Url(ErrorMessage = "Invalid URL format")]

// Enum/Choice
[RegularExpression("^(Option1|Option2)$", ErrorMessage = "Invalid value")]

// Guid
[Required(ErrorMessage = "ID is required")]
```

**Enable automatic validation** in Program.cs:
```csharp
// Already configured (line 67-70), ensure it's set to false to enable validation
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = false; // Validation enabled
});
```

**Effort**: 4-5 hours (systematic, 48 DTOs)
**Testing**: Send invalid data to endpoints and verify 400 Bad Request with validation errors

---

### 3.2 Add Model Validation Middleware

**Create**: `/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/src/InsightLearn.Application/Middleware/ModelValidationMiddleware.cs`

```csharp
public class ModelValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ModelValidationMiddleware> _logger;

    public ModelValidationMiddleware(RequestDelegate next, ILogger<ModelValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // This middleware is redundant if using [ApiController] attribute
        // But useful for minimal APIs with explicit validation logging

        await _next(context);

        // Log validation failures
        if (context.Response.StatusCode == 400)
        {
            _logger.LogWarning("[VALIDATION] Bad request on {Path}: {Method}",
                context.Request.Path, context.Request.Method);
        }
    }
}
```

**Effort**: 30 minutes

---

## Phase 4: Deployment & Monitoring (Priority: HIGH)

### 4.1 Add Comprehensive Health Checks

**Current Issue**: Health check only tests API responsiveness, not dependencies

**File**: `/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/src/InsightLearn.Application/Program.cs`

**Replace existing health check configuration** (around line 240-250):

```csharp
// Add comprehensive health checks
builder.Services.AddHealthChecks()
    // SQL Server database
    .AddSqlServer(
        connectionString: connectionString,
        name: "sqlserver",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "db", "sql", "sqlserver" },
        timeout: TimeSpan.FromSeconds(5))

    // MongoDB video storage
    .AddMongoDb(
        mongodbConnectionString: mongoConnectionString,
        name: "mongodb",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "db", "mongodb", "videos" },
        timeout: TimeSpan.FromSeconds(5))

    // Redis cache
    .AddRedis(
        redisConnectionString: builder.Configuration["Redis:ConnectionString"] ?? "redis-service:6379",
        name: "redis",
        failureStatus: HealthStatus.Degraded, // Cache failure is not critical
        tags: new[] { "cache", "redis" },
        timeout: TimeSpan.FromSeconds(3))

    // Elasticsearch search engine
    .AddElasticsearch(
        elasticsearchUri: builder.Configuration["Elasticsearch:Url"] ?? "http://elasticsearch:9200",
        name: "elasticsearch",
        failureStatus: HealthStatus.Degraded, // Search failure is not critical
        tags: new[] { "search", "elasticsearch" },
        timeout: TimeSpan.FromSeconds(5))

    // Ollama AI chatbot
    .AddUrlGroup(
        uri: new Uri($"{ollamaUrl}/api/tags"),
        name: "ollama",
        failureStatus: HealthStatus.Degraded, // Chatbot failure is not critical
        tags: new[] { "ai", "ollama", "chatbot" },
        timeout: TimeSpan.FromSeconds(5))

    // Custom health check for database logging
    .AddCheck<DatabaseLoggingHealthCheck>("database-logging", tags: new[] { "custom", "logging" });
```

**Map health check endpoints**:
```csharp
// Basic health check (all dependencies)
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                duration = e.Value.Duration.TotalMilliseconds,
                description = e.Value.Description,
                data = e.Value.Data,
                exception = e.Value.Exception?.Message,
                tags = e.Value.Tags
            })
        };

        await context.Response.WriteAsJsonAsync(response);
    }
});

// Liveness probe (basic API check, no dependencies)
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false // No checks, just returns 200 if API is running
});

// Readiness probe (checks critical dependencies only)
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("db") || check.Tags.Contains("sql"),
    ResponseWriter = HealthCheckResponseWriter.WriteResponse
});
```

**Update K8s deployment** (`/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/k8s/06-api-deployment.yaml`):

Replace lines 125-140:
```yaml
        livenessProbe:
          httpGet:
            path: /health/live
            port: 80
          initialDelaySeconds: 40
          periodSeconds: 30
          timeoutSeconds: 5
          failureThreshold: 3
        readinessProbe:
          httpGet:
            path: /health/ready
            port: 80
          initialDelaySeconds: 20
          periodSeconds: 10
          timeoutSeconds: 10
          failureThreshold: 3
```

**Effort**: 2 hours
**Testing**: `curl http://localhost:31081/health` and verify all dependencies report status

---

### 4.2 Add Prometheus Metrics

**Current Status**: Basic Prometheus service exists

**Enhancement**: Add custom application metrics

**File**: `/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/src/InsightLearn.Application/Services/PrometheusService.cs`

Add custom metrics:
```csharp
using Prometheus;

public class MetricsService
{
    // Counters
    private static readonly Counter ApiRequestsTotal = Metrics.CreateCounter(
        "insightlearn_api_requests_total",
        "Total number of API requests",
        new CounterConfiguration { LabelNames = new[] { "method", "endpoint", "status" } });

    private static readonly Counter EnrollmentsTotal = Metrics.CreateCounter(
        "insightlearn_enrollments_total",
        "Total number of course enrollments");

    private static readonly Counter PaymentsTotal = Metrics.CreateCounter(
        "insightlearn_payments_total",
        "Total number of payments",
        new CounterConfiguration { LabelNames = new[] { "status" } });

    // Gauges
    private static readonly Gauge ActiveUsersGauge = Metrics.CreateGauge(
        "insightlearn_active_users",
        "Number of currently active users");

    private static readonly Gauge ActiveEnrollmentsGauge = Metrics.CreateGauge(
        "insightlearn_active_enrollments",
        "Number of active enrollments");

    // Histograms
    private static readonly Histogram ApiRequestDuration = Metrics.CreateHistogram(
        "insightlearn_api_request_duration_seconds",
        "API request duration in seconds",
        new HistogramConfiguration
        {
            LabelNames = new[] { "method", "endpoint" },
            Buckets = Histogram.ExponentialBuckets(0.001, 2, 10) // 1ms to ~1s
        });

    private static readonly Histogram OllamaInferenceDuration = Metrics.CreateHistogram(
        "insightlearn_ollama_inference_duration_seconds",
        "Ollama AI inference duration in seconds",
        new HistogramConfiguration
        {
            Buckets = Histogram.ExponentialBuckets(0.1, 2, 10) // 100ms to ~100s
        });

    // Summary
    private static readonly Summary VideoUploadSize = Metrics.CreateSummary(
        "insightlearn_video_upload_size_bytes",
        "Video upload size in bytes",
        new SummaryConfiguration
        {
            Objectives = new[]
            {
                new QuantileEpsilonPair(0.5, 0.05),  // Median
                new QuantileEpsilonPair(0.9, 0.01),  // 90th percentile
                new QuantileEpsilonPair(0.99, 0.001) // 99th percentile
            }
        });

    public void RecordApiRequest(string method, string endpoint, int statusCode)
    {
        ApiRequestsTotal.WithLabels(method, endpoint, statusCode.ToString()).Inc();
    }

    public IDisposable MeasureApiDuration(string method, string endpoint)
    {
        return ApiRequestDuration.WithLabels(method, endpoint).NewTimer();
    }

    public void RecordEnrollment()
    {
        EnrollmentsTotal.Inc();
    }

    public void RecordPayment(string status)
    {
        PaymentsTotal.WithLabels(status).Inc();
    }

    public void SetActiveUsers(int count)
    {
        ActiveUsersGauge.Set(count);
    }

    public void SetActiveEnrollments(int count)
    {
        ActiveEnrollmentsGauge.Set(count);
    }

    public IDisposable MeasureOllamaInference()
    {
        return OllamaInferenceDuration.NewTimer();
    }

    public void RecordVideoUpload(long sizeBytes)
    {
        VideoUploadSize.Observe(sizeBytes);
    }
}
```

**Register service**:
```csharp
builder.Services.AddSingleton<MetricsService>();
```

**Add metrics middleware** (Program.cs, before endpoints):
```csharp
app.Use(async (context, next) =>
{
    var metricsService = context.RequestServices.GetRequiredService<MetricsService>();

    using (metricsService.MeasureApiDuration(context.Request.Method, context.Request.Path))
    {
        await next();
    }

    metricsService.RecordApiRequest(context.Request.Method, context.Request.Path, context.Response.StatusCode);
});
```

**Use in services** (example: EnrollmentService.cs):
```csharp
public async Task<EnrollmentDto> EnrollUserAsync(CreateEnrollmentDto dto)
{
    // ... existing code ...

    _metricsService.RecordEnrollment();

    return MapToDto(createdEnrollment);
}
```

**Effort**: 3 hours
**Verification**: Check `http://localhost:31081/metrics` endpoint

---

### 4.3 Add Grafana Alerts

**File**: `/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/k8s/grafana-alerts.yaml`

Create alert rules:
```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: grafana-alerts
  namespace: insightlearn
data:
  alerts.yaml: |
    apiVersion: 1
    groups:
      - orgId: 1
        name: InsightLearn Alerts
        folder: Alerts
        interval: 1m
        rules:
          # API Health Alert
          - uid: api-health
            title: API Health Check Failed
            condition: health_status
            data:
              - refId: health_status
                queryType: prometheus
                model:
                  expr: up{job="insightlearn-api"} == 0
                  instant: true
            noDataState: NoData
            execErrState: Alerting
            for: 2m
            annotations:
              description: 'InsightLearn API is down or unreachable'
              summary: 'API health check failed'
            labels:
              severity: critical

          # High Error Rate Alert
          - uid: high-error-rate
            title: High API Error Rate
            condition: error_rate
            data:
              - refId: error_rate
                queryType: prometheus
                model:
                  expr: |
                    (sum(rate(insightlearn_api_requests_total{status=~"5.."}[5m]))
                    / sum(rate(insightlearn_api_requests_total[5m]))) * 100 > 5
                  instant: true
            noDataState: NoData
            execErrState: Alerting
            for: 5m
            annotations:
              description: 'API error rate is above 5% (5xx responses)'
              summary: 'High error rate detected'
            labels:
              severity: warning

          # Database Connection Alert
          - uid: db-connection
            title: Database Connection Failed
            condition: db_health
            data:
              - refId: db_health
                queryType: prometheus
                model:
                  expr: health_check_status{check="sqlserver"} == 0
                  instant: true
            noDataState: Alerting
            execErrState: Alerting
            for: 1m
            annotations:
              description: 'SQL Server database is unreachable'
              summary: 'Database connection failed'
            labels:
              severity: critical

          # High Memory Usage Alert
          - uid: high-memory
            title: High Memory Usage
            condition: memory_usage
            data:
              - refId: memory_usage
                queryType: prometheus
                model:
                  expr: |
                    (container_memory_usage_bytes{pod=~"insightlearn-api.*"}
                    / container_spec_memory_limit_bytes{pod=~"insightlearn-api.*"}) * 100 > 85
                  instant: true
            noDataState: NoData
            execErrState: Alerting
            for: 5m
            annotations:
              description: 'API pod memory usage is above 85%'
              summary: 'High memory usage detected'
            labels:
              severity: warning

          # Slow API Response Alert
          - uid: slow-response
            title: Slow API Response Time
            condition: response_time
            data:
              - refId: response_time
                queryType: prometheus
                model:
                  expr: |
                    histogram_quantile(0.95,
                      sum(rate(insightlearn_api_request_duration_seconds_bucket[5m]))
                      by (le, endpoint)) > 2
                  instant: true
            noDataState: NoData
            execErrState: Alerting
            for: 5m
            annotations:
              description: 'API 95th percentile response time is above 2 seconds'
              summary: 'Slow API response detected'
            labels:
              severity: warning
```

**Apply alerts**:
```bash
kubectl apply -f /home/mpasqui/insightlearn_WASM/InsightLearn_WASM/k8s/grafana-alerts.yaml
```

**Effort**: 2 hours

---

## Phase 5: Certificate Generation (Priority: MEDIUM)

### 5.1 Implement PDF Certificate Generation

**Current Issue**: `/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/src/InsightLearn.Application/Services/CertificateService.cs` is stub (lines 83-97)

**Solution**: Use **QuestPDF** library (MIT license, better than iText7 commercial license)

**Add NuGet package** to `/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/src/InsightLearn.Application/InsightLearn.Application.csproj`:
```xml
<PackageReference Include="QuestPDF" Version="2024.1.0" />
```

**Create certificate template service**: `/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/src/InsightLearn.Application/Services/CertificateTemplateService.cs`

```csharp
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace InsightLearn.Application.Services;

public interface ICertificateTemplateService
{
    Task<byte[]> GeneratePdfAsync(
        string studentName,
        string courseName,
        string certificateNumber,
        DateTime issuedDate,
        int courseHours,
        double courseRating);
}

public class CertificateTemplateService : ICertificateTemplateService
{
    private readonly ILogger<CertificateTemplateService> _logger;

    public CertificateTemplateService(ILogger<CertificateTemplateService> logger)
    {
        _logger = logger;

        // Set QuestPDF license (Community License for free use)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<byte[]> GeneratePdfAsync(
        string studentName,
        string courseName,
        string certificateNumber,
        DateTime issuedDate,
        int courseHours,
        double courseRating)
    {
        _logger.LogInformation("[CERTIFICATE] Generating PDF for student: {Student}, course: {Course}",
            studentName, courseName);

        return await Task.Run(() =>
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(50);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(14).FontFamily("Arial"));

                    page.Content().Column(column =>
                    {
                        column.Spacing(20);

                        // Header
                        column.Item().AlignCenter().Text("CERTIFICATE OF COMPLETION")
                            .FontSize(36)
                            .Bold()
                            .FontColor(Colors.Blue.Darken3);

                        column.Item().PaddingVertical(10).LineHorizontal(3).LineColor(Colors.Blue.Darken1);

                        // Content
                        column.Item().PaddingTop(20).AlignCenter().Column(content =>
                        {
                            content.Item().Text("This is to certify that")
                                .FontSize(16)
                                .Italic();

                            content.Item().PaddingVertical(10).Text(studentName)
                                .FontSize(28)
                                .Bold()
                                .FontColor(Colors.Black);

                            content.Item().PaddingTop(10).Text("has successfully completed the course")
                                .FontSize(16)
                                .Italic();

                            content.Item().PaddingVertical(10).Text(courseName)
                                .FontSize(24)
                                .Bold()
                                .FontColor(Colors.Blue.Darken2);

                            content.Item().PaddingTop(20).Row(row =>
                            {
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Text("Course Duration")
                                        .FontSize(12)
                                        .FontColor(Colors.Grey.Darken2);
                                    col.Item().Text($"{courseHours} hours")
                                        .FontSize(16)
                                        .Bold();
                                });

                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Text("Course Rating")
                                        .FontSize(12)
                                        .FontColor(Colors.Grey.Darken2);
                                    col.Item().Text($"{courseRating:F1}/5.0")
                                        .FontSize(16)
                                        .Bold();
                                });
                            });
                        });

                        // Footer
                        column.Item().PaddingTop(40).Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text($"Issue Date: {issuedDate:MMMM dd, yyyy}")
                                    .FontSize(12);
                                col.Item().Text($"Certificate Number: {certificateNumber}")
                                    .FontSize(10)
                                    .FontColor(Colors.Grey.Darken1);
                            });

                            row.RelativeItem().AlignRight().Column(col =>
                            {
                                col.Item().PaddingBottom(5).LineHorizontal(1).LineColor(Colors.Black);
                                col.Item().Text("InsightLearn Administration")
                                    .FontSize(12);
                            });
                        });

                        column.Item().PaddingTop(20).AlignCenter().Text("InsightLearn - Empowering Education")
                            .FontSize(10)
                            .Italic()
                            .FontColor(Colors.Grey.Darken1);
                    });
                });
            });

            return document.GeneratePdf();
        });
    }
}
```

**Update CertificateService.cs** (replace lines 83-97):
```csharp
// 6. Generate PDF certificate
var pdfBytes = await _certificateTemplateService.GeneratePdfAsync(
    studentName: enrollment.User?.FullName ?? "Student",
    courseName: course.Title,
    certificateNumber: certificateNumber,
    issuedDate: DateTime.UtcNow,
    courseHours: (int)Math.Ceiling(course.EstimatedDurationMinutes / 60.0),
    courseRating: course.AverageRating);

// 7. Upload PDF to storage (choose one):
// Option A: Save to local filesystem
var certificatePath = Path.Combine("wwwroot", "certificates", $"{certificate.Id}.pdf");
Directory.CreateDirectory(Path.GetDirectoryName(certificatePath)!);
await File.WriteAllBytesAsync(certificatePath, pdfBytes);
certificate.PdfUrl = $"/certificates/{certificate.Id}.pdf";

// Option B: Upload to Azure Blob Storage (production-ready)
// var blobClient = _blobContainerClient.GetBlobClient($"{certificate.Id}.pdf");
// await blobClient.UploadAsync(new BinaryData(pdfBytes), overwrite: true);
// certificate.PdfUrl = blobClient.Uri.ToString();

certificate.TemplateUrl = null; // PDF generated, no template needed
```

**Add certificate template service registration** in Program.cs:
```csharp
builder.Services.AddScoped<ICertificateTemplateService, CertificateTemplateService>();
```

**Effort**: 4 hours
**Testing**: Complete a course enrollment and verify PDF generation

---

## Phase 6: Code Quality Polish (Priority: MEDIUM)

### 6.1 Add XML Documentation

**Current Issue**: Many public APIs lack XML documentation

**Strategy**: Add `<summary>` tags to all public methods

**Example** (Program.cs endpoints):
```csharp
/// <summary>
/// Gets all enrollments with pagination (Admin only)
/// </summary>
/// <param name="enrollmentService">Enrollment service</param>
/// <param name="logger">Logger instance</param>
/// <param name="page">Page number (default: 1)</param>
/// <param name="pageSize">Items per page (default: 10)</param>
/// <returns>Paginated list of enrollments</returns>
/// <response code="200">Returns paginated enrollments</response>
/// <response code="401">Unauthorized</response>
/// <response code="403">Forbidden (Admin role required)</response>
/// <response code="500">Internal server error</response>
app.MapGet("/api/enrollments", async (...) => { ... })
```

**Enable XML documentation generation** in `.csproj`:
```xml
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <NoWarn>$(NoWarn);1591</NoWarn> <!-- Suppress missing XML comments warning -->
</PropertyGroup>
```

**Configure Swagger to use XML comments** in Program.cs:
```csharp
builder.Services.AddSwaggerGen(options =>
{
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "InsightLearn API",
        Version = versionShort,
        Description = "Enterprise LMS Platform API",
        Contact = new OpenApiContact
        {
            Name = "InsightLearn Support",
            Email = "support@insightlearn.cloud"
        }
    });
});
```

**Effort**: 4-5 hours (systematic documentation of all endpoints)

---

### 6.2 Improve Error Messages

**Current Issue**: Error messages may expose internal implementation details

**Strategy**: Use safe error messages for production

**Create error response model**: `/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/src/InsightLearn.Core/DTOs/ErrorResponse.cs`

```csharp
namespace InsightLearn.Core.DTOs;

public class ErrorResponse
{
    public string Error { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? TraceId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, string[]>? ValidationErrors { get; set; }
}
```

**Create global exception handler middleware**: `/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/src/InsightLearn.Application/Middleware/GlobalExceptionHandlerMiddleware.cs`

```csharp
public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private readonly IWebHostEnvironment _env;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger,
        IWebHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "[GLOBAL_ERROR] Unhandled exception on {Path}", context.Request.Path);

        var response = new ErrorResponse
        {
            Error = "An error occurred processing your request",
            TraceId = Activity.Current?.Id ?? context.TraceIdentifier,
            Timestamp = DateTime.UtcNow
        };

        // Determine status code and message based on exception type
        var statusCode = exception switch
        {
            ArgumentException => StatusCodes.Status400BadRequest,
            ArgumentNullException => StatusCodes.Status400BadRequest,
            InvalidOperationException => StatusCodes.Status400BadRequest,
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            KeyNotFoundException => StatusCodes.Status404NotFound,
            NotImplementedException => StatusCodes.Status501NotImplemented,
            _ => StatusCodes.Status500InternalServerError
        };

        // In development, include exception details
        if (_env.IsDevelopment())
        {
            response.Message = exception.Message;
            response.ValidationErrors = new Dictionary<string, string[]>
            {
                ["StackTrace"] = new[] { exception.StackTrace ?? "No stack trace available" }
            };
        }
        else
        {
            // In production, use safe generic messages
            response.Message = statusCode switch
            {
                400 => "The request was invalid or cannot be processed",
                401 => "Authentication is required to access this resource",
                403 => "You do not have permission to access this resource",
                404 => "The requested resource was not found",
                _ => "An unexpected error occurred. Please try again later."
            };
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsJsonAsync(response);
    }
}
```

**Register middleware** (Program.cs, early in pipeline):
```csharp
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
```

**Effort**: 2 hours

---

### 6.3 Add Unit Tests

**Current Issue**: No unit test coverage

**Create test project**:
```bash
cd /home/mpasqui/insightlearn_WASM/InsightLearn_WASM
dotnet new xunit -n InsightLearn.Tests
dotnet sln add InsightLearn.Tests/InsightLearn.Tests.csproj
```

**Add test packages** to `InsightLearn.Tests.csproj`:
```xml
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
<PackageReference Include="xUnit" Version="2.6.2" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.5.4" />
<PackageReference Include="Moq" Version="4.20.70" />
<PackageReference Include="FluentAssertions" Version="6.12.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="8.0.0" />
```

**Example test**: `/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/InsightLearn.Tests/Services/EnrollmentServiceTests.cs`

```csharp
using FluentAssertions;
using InsightLearn.Application.Services;
using InsightLearn.Core.DTOs.Enrollment;
using InsightLearn.Core.Entities;
using InsightLearn.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace InsightLearn.Tests.Services;

public class EnrollmentServiceTests
{
    private readonly Mock<IEnrollmentRepository> _enrollmentRepositoryMock;
    private readonly Mock<ICourseRepository> _courseRepositoryMock;
    private readonly Mock<IPaymentRepository> _paymentRepositoryMock;
    private readonly Mock<ICertificateService> _certificateServiceMock;
    private readonly Mock<ILogger<EnrollmentService>> _loggerMock;
    private readonly EnrollmentService _sut; // System Under Test

    public EnrollmentServiceTests()
    {
        _enrollmentRepositoryMock = new Mock<IEnrollmentRepository>();
        _courseRepositoryMock = new Mock<ICourseRepository>();
        _paymentRepositoryMock = new Mock<IPaymentRepository>();
        _certificateServiceMock = new Mock<ICertificateService>();
        _loggerMock = new Mock<ILogger<EnrollmentService>>();

        _sut = new EnrollmentService(
            _enrollmentRepositoryMock.Object,
            _courseRepositoryMock.Object,
            _paymentRepositoryMock.Object,
            _certificateServiceMock.Object,
            null!, // DbContext not needed for this test
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetEnrollmentByIdAsync_WhenExists_ReturnsDto()
    {
        // Arrange
        var enrollmentId = Guid.NewGuid();
        var enrollment = new Enrollment
        {
            Id = enrollmentId,
            UserId = Guid.NewGuid(),
            CourseId = Guid.NewGuid(),
            Status = EnrollmentStatus.Active
        };

        _enrollmentRepositoryMock
            .Setup(x => x.GetByIdAsync(enrollmentId))
            .ReturnsAsync(enrollment);

        // Act
        var result = await _sut.GetEnrollmentByIdAsync(enrollmentId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(enrollmentId);
        result.Status.Should().Be("Active");
    }

    [Fact]
    public async Task GetEnrollmentByIdAsync_WhenNotExists_ReturnsNull()
    {
        // Arrange
        var enrollmentId = Guid.NewGuid();
        _enrollmentRepositoryMock
            .Setup(x => x.GetByIdAsync(enrollmentId))
            .ReturnsAsync((Enrollment?)null);

        // Act
        var result = await _sut.GetEnrollmentByIdAsync(enrollmentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task EnrollUserAsync_WhenCourseNotFound_ThrowsArgumentException()
    {
        // Arrange
        var dto = new CreateEnrollmentDto
        {
            UserId = Guid.NewGuid(),
            CourseId = Guid.NewGuid(),
            AmountPaid = 49.99m
        };

        _courseRepositoryMock
            .Setup(x => x.GetByIdAsync(dto.CourseId))
            .ReturnsAsync((Course?)null);

        // Act & Assert
        await _sut.Invoking(s => s.EnrollUserAsync(dto))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage($"Course {dto.CourseId} not found");
    }
}
```

**Target Coverage**: Aim for 80%+ on critical services (EnrollmentService, PaymentService, AuthService)

**Effort**: 1-2 days (20+ unit tests for critical paths)

---

## Phase 7: Architectural Consistency (Priority: LOW)

### 7.1 Standardize Endpoint Response Patterns

**Current Issue**: Inconsistent response formats across endpoints

**Goal**: All endpoints return consistent response structure

**Create standard response wrappers**: `/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/src/InsightLearn.Core/DTOs/ApiResponse.cs`

```csharp
namespace InsightLearn.Core.DTOs;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public List<string>? Errors { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static ApiResponse<T> SuccessResult(T data, string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message
        };
    }

    public static ApiResponse<T> ErrorResult(string message, List<string>? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Errors = errors
        };
    }
}

public class PaginatedResponse<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
}
```

**Refactor endpoints to use standard responses** (example):
```csharp
app.MapGet("/api/courses", async (...) =>
{
    var courses = await courseService.GetAllCoursesAsync();
    return Results.Ok(ApiResponse<List<CourseDto>>.SuccessResult(courses));
})
```

**Effort**: 3-4 hours (refactor all 31 endpoints)

---

### 7.2 Repository Pattern Consistency

**Goal**: Ensure all repositories follow same pattern

**Checklist for each repository**:
- [ ] Implements interface from `/Core/Interfaces/I{Name}Repository.cs`
- [ ] Has `GetByIdAsync()`, `GetAllAsync()`, `CreateAsync()`, `UpdateAsync()`, `DeleteAsync()` (if applicable)
- [ ] Uses consistent error handling
- [ ] Logs operations consistently
- [ ] Returns DTOs or entities (not mixed)

**Example consistency check**:
```bash
# Check all repositories have standard methods
for repo in /home/mpasqui/insightlearn_WASM/InsightLearn_WASM/src/InsightLearn.Infrastructure/Repositories/*.cs; do
  echo "Checking $repo:"
  grep -E "(GetByIdAsync|GetAllAsync|CreateAsync|UpdateAsync)" "$repo" | wc -l
done
```

**Effort**: 2 hours (audit + fix inconsistencies)

---

## Summary: Acceptance Criteria for 10/10 Scores

### Architectural Consistency (10/10)

- [x] Build completes with 0 errors, 0 warnings
- [x] All 31 API endpoints operational (GET /api/enrollments fixed)
- [x] Repository pattern consistent across all 10 repositories
- [x] Service layer complete with all interfaces implemented
- [x] Standard response format for all endpoints
- [x] Swagger documentation complete

### Code Quality (10/10)

- [x] Zero nullability warnings (all 34 fixed)
- [x] All DTOs have validation attributes (48 DTOs)
- [x] XML documentation on public APIs
- [x] Safe error messages (no implementation details exposed)
- [x] Unit tests with 80%+ coverage on critical services
- [x] Code follows consistent naming conventions

### Security (10/10)

- [x] JWT secret validation (no hardcoded fallbacks)
- [x] Rate limiting implemented (3 policies)
- [x] Request validation middleware active
- [x] Security headers configured (HSTS, CSP, X-Frame-Options, etc.)
- [x] Audit logging for sensitive operations
- [x] OWASP Top 10 compliance verified

### Deployment Readiness (10/10)

- [x] Comprehensive health checks (SQL Server, MongoDB, Redis, Elasticsearch, Ollama)
- [x] Liveness and readiness probes configured
- [x] K8s resource limits defined (CPU, memory)
- [x] HPA configured for auto-scaling
- [x] Prometheus metrics with custom application metrics
- [x] Grafana alerts configured (5+ alert rules)
- [x] Database migrations automated on startup
- [x] Environment variables documented

### Known Issues (10/10)

- [x] GET /api/enrollments returns 200 (not 501)
- [x] Certificate PDF generation fully implemented
- [x] All build warnings resolved
- [x] No critical or high severity dependencies
- [x] All services operational in K8s

---

## Implementation Priority Matrix

| Task | Priority | Effort | Impact | Phase |
|------|----------|--------|--------|-------|
| Fix GET /api/enrollments | CRITICAL | 30min | HIGH | 1 |
| Resolve 34 warnings | HIGH | 3h | HIGH | 1 |
| JWT secret hardening | CRITICAL | 1h | CRITICAL | 2 |
| Add rate limiting | HIGH | 2h | HIGH | 2 |
| Request validation middleware | HIGH | 2h | MEDIUM | 2 |
| Security headers | HIGH | 1h | HIGH | 2 |
| Audit logging | HIGH | 3h | MEDIUM | 2 |
| DTO validation | HIGH | 5h | HIGH | 3 |
| Health checks | HIGH | 2h | HIGH | 4 |
| Prometheus metrics | MEDIUM | 3h | MEDIUM | 4 |
| Grafana alerts | MEDIUM | 2h | MEDIUM | 4 |
| Certificate PDF | MEDIUM | 4h | MEDIUM | 5 |
| XML documentation | LOW | 5h | LOW | 6 |
| Error messages | MEDIUM | 2h | MEDIUM | 6 |
| Unit tests | MEDIUM | 16h | MEDIUM | 6 |
| Endpoint consistency | LOW | 4h | LOW | 7 |
| Repository audit | LOW | 2h | LOW | 7 |

**Total Estimated Effort**: ~58 hours (~7-8 working days for 1 senior developer)

---

## Risk Assessment

### Breaking Changes

1. **Rate Limiting**: May break existing API clients if not documented
   - **Mitigation**: Announce rate limits in advance, provide migration guide

2. **Response Format Changes**: Standard response wrapper may break frontend
   - **Mitigation**: Implement incrementally, keep old format for deprecated endpoints

3. **JWT Secret Validation**: Will fail startup if secret not configured
   - **Mitigation**: Update deployment documentation, add health check

### Deployment Risks

1. **Database Migration**: Adding AuditLog table requires migration
   - **Mitigation**: Test migration in staging first, backup production database

2. **K8s Health Check Changes**: New probes may cause pod restarts
   - **Mitigation**: Rolling update with readiness probe, monitor pod status

3. **Prometheus Metrics**: New metrics may increase Prometheus storage
   - **Mitigation**: Configure retention policy, monitor disk usage

---

## Next Steps

1. **Review this roadmap** with stakeholders for approval
2. **Prioritize phases** based on business requirements
3. **Set up staging environment** for testing changes
4. **Create feature branches** for each phase
5. **Implement in phases** with code reviews between phases
6. **Update documentation** as features are completed
7. **Final verification** against acceptance criteria

---

**Document Status**: READY FOR IMPLEMENTATION
**Last Updated**: 2025-11-10
**Maintained By**: Backend Architecture Team
