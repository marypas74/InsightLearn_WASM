using System.Diagnostics;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using InsightLearn.Core.Entities;
using InsightLearn.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InsightLearn.Application.Middleware;

/// <summary>
/// Middleware for auditing sensitive operations and security events
/// Logs authentication, authorization, validation failures, and admin operations
/// </summary>
public class AuditLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditLoggingMiddleware> _logger;
    private readonly IDbContextFactory<InsightLearnDbContext> _contextFactory;

    // Maximum length for request/response body preview in logs
    private const int BODY_PREVIEW_MAX_LENGTH = 200;

    // Sensitive paths that require audit logging
    private static readonly HashSet<string> AuditPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/auth/login",
        "/api/auth/register",
        "/api/auth/refresh",
        "/api/admin",
        "/api/users",
        "/api/enrollments",
        "/api/payments"
    };

    // Sensitive fields to redact from request/response bodies
    private static readonly HashSet<string> SensitiveFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "oldPassword",
        "newPassword",
        "confirmPassword",
        "secret",
        "token",
        "refreshToken",
        "accessToken",
        "apiKey",
        "cardNumber",
        "cvv",
        "securityCode",
        "ssn",
        "taxId"
    };

    // Regex patterns for sensitive data in strings
    private static readonly Regex EmailPattern = new(@"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}", RegexOptions.Compiled);
    private static readonly Regex CreditCardPattern = new(@"\b\d{4}[\s-]?\d{4}[\s-]?\d{4}[\s-]?\d{4}\b", RegexOptions.Compiled);
    private static readonly Regex JwtPattern = new(@"eyJ[a-zA-Z0-9_-]*\.eyJ[a-zA-Z0-9_-]*\.[a-zA-Z0-9_-]*", RegexOptions.Compiled);

    public AuditLoggingMiddleware(RequestDelegate next, ILogger<AuditLoggingMiddleware> logger, IDbContextFactory<InsightLearnDbContext> contextFactory)
    {
        _next = next;
        _logger = logger;
        _contextFactory = contextFactory;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Check if path requires audit logging
        var requiresAudit = AuditPaths.Any(path => context.Request.Path.StartsWithSegments(path));

        if (!requiresAudit)
        {
            // Skip audit logging for non-sensitive paths (performance optimization)
            await _next(context);
            return;
        }

        // Start timing
        var stopwatch = Stopwatch.StartNew();

        // Extract user context
        var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
        var userEmail = context.User?.FindFirst(ClaimTypes.Email)?.Value ?? "unknown";
        var userRoles = context.User?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList() ?? new List<string>();

        // Extract request metadata
        var requestId = context.TraceIdentifier;
        var clientIp = GetClientIp(context);
        var method = context.Request.Method;
        var path = context.Request.Path.Value ?? "";
        var queryString = context.Request.QueryString.Value ?? "";

        // Capture request body for audit trail (only for POST/PUT/PATCH)
        string? requestBody = null;
        if (method == "POST" || method == "PUT" || method == "PATCH")
        {
            requestBody = await CaptureRequestBodyAsync(context);
        }

        // Capture original response body stream
        var originalResponseBodyStream = context.Response.Body;

        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        try
        {
            // Execute next middleware
            await _next(context);

            // Capture response status
            var statusCode = context.Response.StatusCode;

            // Capture response body for audit trail (only for error responses)
            string? responseBody = null;
            if (statusCode >= 400)
            {
                responseBody = await CaptureResponseBodyAsync(context);
            }

            // Stop timing
            stopwatch.Stop();

            // Log audit event based on status code and endpoint
            await LogAuditEvent(
                context,
                requestId,
                userId,
                userEmail,
                userRoles,
                clientIp,
                method,
                path,
                queryString,
                requestBody,
                statusCode,
                responseBody,
                stopwatch.ElapsedMilliseconds);

            // Copy response body back to original stream
            responseBodyStream.Seek(0, SeekOrigin.Begin);
            await responseBodyStream.CopyToAsync(originalResponseBodyStream);
        }
        catch (Exception ex)
        {
            // Stop timing
            stopwatch.Stop();

            // Log exception in audit trail
            _logger.LogError(ex,
                "[AUDIT] EXCEPTION - RequestId: {RequestId}, UserId: {UserId}, Email: {Email}, IP: {IpAddress}, " +
                "Method: {Method}, Path: {Path}, Duration: {Duration}ms",
                requestId, userId, RedactEmail(userEmail), clientIp, method, path, stopwatch.ElapsedMilliseconds);

            // Re-throw to be handled by global exception handler
            throw;
        }
        finally
        {
            // Restore original response body stream
            context.Response.Body = originalResponseBodyStream;
        }
    }

    /// <summary>
    /// Logs structured audit event based on endpoint and status code
    /// </summary>
    private async Task LogAuditEvent(
        HttpContext context,
        string requestId,
        string userId,
        string userEmail,
        List<string> userRoles,
        string clientIp,
        string method,
        string path,
        string queryString,
        string? requestBody,
        int statusCode,
        string? responseBody,
        long durationMs)
    {
        // Determine event type based on path and status code
        var eventType = DetermineEventType(path, method, statusCode);

        // Redact sensitive data from bodies
        var redactedRequestBody = RedactSensitiveData(requestBody);
        var redactedResponseBody = RedactSensitiveData(responseBody);

        // Build structured log entry (JSON format for Elasticsearch)
        var auditEvent = new
        {
            Timestamp = DateTime.UtcNow,
            EventType = eventType,
            RequestId = requestId,
            UserId = userId,
            UserEmail = RedactEmail(userEmail),
            UserRoles = userRoles,
            ClientIp = clientIp,
            Method = method,
            Path = path,
            QueryString = RedactQueryString(queryString),
            RequestBodyLength = requestBody?.Length ?? 0,
            RequestBodyPreview = redactedRequestBody != null
                ? redactedRequestBody.Substring(0, Math.Min(BODY_PREVIEW_MAX_LENGTH, redactedRequestBody.Length))
                : null,
            StatusCode = statusCode,
            ResponseBodyLength = responseBody?.Length ?? 0,
            ResponseBodyPreview = redactedResponseBody != null
                ? redactedResponseBody.Substring(0, Math.Min(BODY_PREVIEW_MAX_LENGTH, redactedResponseBody.Length))
                : null,
            DurationMs = durationMs,
            UserAgent = SanitizeHeaderValue(context.Request.Headers["User-Agent"]),
            Referer = SanitizeHeaderValue(context.Request.Headers["Referer"])
        };

        // Log with appropriate level based on status code
        if (statusCode >= 500)
        {
            _logger.LogError("[AUDIT] {EventType} - {@AuditEvent}", eventType, auditEvent);
        }
        else if (statusCode >= 400)
        {
            _logger.LogWarning("[AUDIT] {EventType} - {@AuditEvent}", eventType, auditEvent);
        }
        else if (eventType.Contains("SUCCESS") || eventType.Contains("ADMIN"))
        {
            _logger.LogInformation("[AUDIT] {EventType} - {@AuditEvent}", eventType, auditEvent);
        }
        else
        {
            // Debug level for non-critical audit events
            _logger.LogDebug("[AUDIT] {EventType} - {@AuditEvent}", eventType, auditEvent);
        }

        // Persist audit log to database using isolated DbContext (thread-safe)
        try
        {
            // Create isolated DbContext (no shared state)
            await using var dbContext = await _contextFactory.CreateDbContextAsync();

            // Determine entity type and ID from path
            var (entityType, entityId) = ExtractEntityFromPath(path);

            // Create audit log entity
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                Action = eventType,
                EntityType = entityType,
                EntityId = entityId,
                UserId = Guid.TryParse(userId, out var userGuid) ? userGuid : null,
                UserEmail = userEmail,
                UserRoles = userRoles != null ? JsonSerializer.Serialize(userRoles) : null,
                IpAddress = clientIp,
                HttpMethod = method,
                Path = path,
                StatusCode = statusCode,
                DurationMs = durationMs,
                Details = JsonSerializer.Serialize(auditEvent),
                UserAgent = auditEvent.UserAgent?.ToString(),
                Referer = auditEvent.Referer?.ToString(),
                RequestId = requestId,
                Timestamp = DateTime.UtcNow
            };

            await dbContext.AuditLogs.AddAsync(auditLog);
            await dbContext.SaveChangesAsync();

            _logger.LogDebug("[AuditLog] Saved: {Action} by {User} at {Timestamp}",
                auditLog.Action, auditLog.UserEmail, auditLog.Timestamp);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "[AuditLog] Database error saving audit log - retrying once");

            // Retry once with new context
            try
            {
                await using var retryDbContext = await _contextFactory.CreateDbContextAsync();

                var (entityType, entityId) = ExtractEntityFromPath(path);

                var auditLog = new AuditLog
                {
                    Id = Guid.NewGuid(),
                    Action = eventType,
                    EntityType = entityType,
                    EntityId = entityId,
                    UserId = Guid.TryParse(userId, out var userGuid) ? userGuid : null,
                    UserEmail = userEmail,
                    UserRoles = userRoles != null ? JsonSerializer.Serialize(userRoles) : null,
                    IpAddress = clientIp,
                    HttpMethod = method,
                    Path = path,
                    StatusCode = statusCode,
                    DurationMs = durationMs,
                    Details = JsonSerializer.Serialize(auditEvent),
                    UserAgent = auditEvent.UserAgent?.ToString(),
                    Referer = auditEvent.Referer?.ToString(),
                    RequestId = requestId,
                    Timestamp = DateTime.UtcNow
                };

                await retryDbContext.AuditLogs.AddAsync(auditLog);
                await retryDbContext.SaveChangesAsync();

                _logger.LogInformation("[AuditLog] Retry successful for {Action}", auditLog.Action);
            }
            catch (Exception retryEx)
            {
                _logger.LogError(retryEx, "[AuditLog] Audit log save failed after retry");
                // Swallow exception to not break request
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AuditLog] Failed to save audit log");
            // Swallow exception to not break request
        }
    }

    /// <summary>
    /// Determines audit event type based on path and status code
    /// </summary>
    private string DetermineEventType(string path, string method, int statusCode)
    {
        // Authentication events
        if (path.Contains("/auth/login", StringComparison.OrdinalIgnoreCase))
        {
            return statusCode == 200 ? "AUTH_LOGIN_SUCCESS" : "AUTH_LOGIN_FAILURE";
        }
        if (path.Contains("/auth/register", StringComparison.OrdinalIgnoreCase))
        {
            return statusCode == 200 || statusCode == 201 ? "AUTH_REGISTER_SUCCESS" : "AUTH_REGISTER_FAILURE";
        }
        if (path.Contains("/auth/refresh", StringComparison.OrdinalIgnoreCase))
        {
            return statusCode == 200 ? "AUTH_REFRESH_SUCCESS" : "AUTH_REFRESH_FAILURE";
        }

        // Authorization failures
        if (statusCode == 401)
        {
            return "AUTH_UNAUTHORIZED";
        }
        if (statusCode == 403)
        {
            return "AUTH_FORBIDDEN";
        }

        // Validation failures
        if (statusCode == 400 && path.Contains("/api"))
        {
            return "VALIDATION_FAILURE";
        }

        // Rate limit exceeded
        if (statusCode == 429)
        {
            return "RATE_LIMIT_EXCEEDED";
        }

        // Admin operations
        if (path.Contains("/admin", StringComparison.OrdinalIgnoreCase))
        {
            return method switch
            {
                "POST" => "ADMIN_CREATE",
                "PUT" => "ADMIN_UPDATE",
                "DELETE" => "ADMIN_DELETE",
                "GET" => "ADMIN_VIEW",
                _ => "ADMIN_OPERATION"
            };
        }

        // User management
        if (path.Contains("/users", StringComparison.OrdinalIgnoreCase))
        {
            return method switch
            {
                "POST" => "USER_CREATE",
                "PUT" => "USER_UPDATE",
                "DELETE" => "USER_DELETE",
                "GET" => "USER_VIEW",
                _ => "USER_OPERATION"
            };
        }

        // Enrollment operations
        if (path.Contains("/enrollments", StringComparison.OrdinalIgnoreCase))
        {
            return method switch
            {
                "POST" => "ENROLLMENT_CREATE",
                "DELETE" => "ENROLLMENT_CANCEL",
                _ => "ENROLLMENT_OPERATION"
            };
        }

        // Payment operations
        if (path.Contains("/payments", StringComparison.OrdinalIgnoreCase))
        {
            return "PAYMENT_OPERATION";
        }

        return "AUDIT_EVENT";
    }

    /// <summary>
    /// Captures request body from buffered stream
    /// </summary>
    private async Task<string?> CaptureRequestBodyAsync(HttpContext context)
    {
        try
        {
            if (context.Request.ContentLength == null || context.Request.ContentLength == 0)
                return null;

            // Enable buffering (should already be enabled by RequestValidationMiddleware)
            context.Request.EnableBuffering();

            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0; // Reset for next middleware

            return body;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Captures response body from memory stream
    /// </summary>
    private async Task<string?> CaptureResponseBodyAsync(HttpContext context)
    {
        try
        {
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(context.Response.Body, Encoding.UTF8, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            context.Response.Body.Seek(0, SeekOrigin.Begin); // Reset for copy

            return body;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Redacts sensitive data from request/response bodies
    /// </summary>
    private string? RedactSensitiveData(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return content;

        try
        {
            // Try to parse as JSON and redact fields
            var jsonDoc = JsonDocument.Parse(content);
            var redactedJson = RedactJsonFields(jsonDoc.RootElement);
            return JsonSerializer.Serialize(redactedJson);
        }
        catch
        {
            // Not JSON, apply regex-based redaction
            content = EmailPattern.Replace(content, "***@***.***");
            content = CreditCardPattern.Replace(content, "****-****-****-****");
            content = JwtPattern.Replace(content, "***JWT_REDACTED***");
            return content;
        }
    }

    /// <summary>
    /// Recursively redacts sensitive fields from JSON
    /// </summary>
    private object RedactJsonFields(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var obj = new Dictionary<string, object>();
                foreach (var property in element.EnumerateObject())
                {
                    // Check if field name is sensitive
                    if (SensitiveFields.Contains(property.Name))
                    {
                        obj[property.Name] = "***REDACTED***";
                    }
                    else
                    {
                        obj[property.Name] = RedactJsonFields(property.Value);
                    }
                }
                return obj;

            case JsonValueKind.Array:
                return element.EnumerateArray().Select(RedactJsonFields).ToList();

            case JsonValueKind.String:
                var str = element.GetString() ?? "";
                // Redact email patterns in string values
                str = EmailPattern.Replace(str, "***@***.***");
                str = JwtPattern.Replace(str, "***JWT***");
                return str;

            case JsonValueKind.Number:
                return element.GetDouble();

            case JsonValueKind.True:
            case JsonValueKind.False:
                return element.GetBoolean();

            case JsonValueKind.Null:
                return null!;

            default:
                return element.ToString();
        }
    }

    /// <summary>
    /// Redacts sensitive data from query strings
    /// </summary>
    private string RedactQueryString(string queryString)
    {
        if (string.IsNullOrWhiteSpace(queryString))
            return queryString;

        // Redact common sensitive query parameters
        queryString = Regex.Replace(queryString, @"(token|apikey|password|secret)=[^&]*", "$1=***REDACTED***", RegexOptions.IgnoreCase);

        return queryString;
    }

    /// <summary>
    /// Redacts email addresses (keeps domain for analytics, masks local part)
    /// </summary>
    private string RedactEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || email == "unknown")
            return email;

        var parts = email.Split('@');
        if (parts.Length != 2)
            return "***@***";

        var localPart = parts[0];
        var domain = parts[1];

        // Keep first and last character of local part, redact middle
        if (localPart.Length <= 2)
        {
            return $"**@{domain}";
        }

        return $"{localPart[0]}***{localPart[^1]}@{domain}";
    }

    /// <summary>
    /// Extracts entity type and ID from request path
    /// </summary>
    /// <param name="path">Request path (e.g., /api/users/123, /api/courses/abc-def)</param>
    /// <returns>Tuple of (EntityType, EntityId), both nullable</returns>
    private (string? EntityType, Guid? EntityId) ExtractEntityFromPath(string path)
    {
        // Parse path segments
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

        // Need at least "/api/entity/id" pattern (3 segments minimum)
        if (segments.Length < 3)
            return (null, null);

        // Extract entity type from path (e.g., "users", "courses", "enrollments")
        string? entityType = null;
        if (segments.Length >= 2 && segments[0].Equals("api", StringComparison.OrdinalIgnoreCase))
        {
            entityType = char.ToUpper(segments[1][0]) + segments[1].Substring(1).TrimEnd('s'); // "users" → "User"
        }

        // Extract entity ID if present (GUID format)
        Guid? entityId = null;
        if (segments.Length >= 3 && Guid.TryParse(segments[2], out var guid))
        {
            entityId = guid;
        }

        return (entityType, entityId);
    }

    /// <summary>
    /// Sanitizes header values to prevent DoS and log injection attacks
    /// </summary>
    /// <param name="headerValue">Header value to sanitize</param>
    /// <param name="maxLength">Maximum allowed length (default: 500 characters)</param>
    private string SanitizeHeaderValue(string? headerValue, int maxLength = 500)
    {
        if (string.IsNullOrWhiteSpace(headerValue))
            return "unknown";

        // Remove control characters (prevent terminal escape sequences and log injection)
        headerValue = Regex.Replace(headerValue, @"[\x00-\x1F\x7F]", "");

        // Truncate if too long (prevent DoS via large headers)
        if (headerValue.Length > maxLength)
            headerValue = headerValue.Substring(0, maxLength) + "...[TRUNCATED]";

        return headerValue;
    }

    /// <summary>
    /// Extracts client IP address (proxy-aware)
    /// </summary>
    private string GetClientIp(HttpContext context)
    {
        var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwarded))
            return forwarded.Split(',')[0].Trim();

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
            return realIp;

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
