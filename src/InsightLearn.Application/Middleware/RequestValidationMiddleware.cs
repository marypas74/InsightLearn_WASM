using System.Text;
using System.Text.RegularExpressions;

namespace InsightLearn.Application.Middleware;

/// <summary>
/// Middleware to validate and sanitize incoming HTTP requests
/// Protects against SQL injection, XSS, path traversal, and oversized payload attacks
/// </summary>
public class RequestValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestValidationMiddleware> _logger;

    // Maximum request body size for standard API requests (1MB)
    private const long MaxContentLengthStandard = 1_000_000;

    // Maximum request body size for bulk operations (5MB)
    private const long MaxContentLengthBulk = 5_000_000;

    // Context-aware SQL injection detection (only flags actual SQL syntax, not isolated keywords)
    // SECURITY FIX: Added 100ms timeout to prevent ReDoS attacks
    private static readonly Regex SqlInjectionPattern = new(
        @"(?i)(\bSELECT\b.*\bFROM\b)|" +           // SELECT ... FROM (context required)
        @"(\bINSERT\b.*\bINTO\b)|" +               // INSERT INTO
        @"(\bUPDATE\b.*\bSET\b)|" +                // UPDATE ... SET
        @"(\bDELETE\b.*\bFROM\b)|" +               // DELETE FROM
        @"(\bDROP\b\s+(TABLE|DATABASE|SCHEMA))|" + // DROP TABLE/DATABASE
        @"('.*--)|" +                              // SQL comment after string
        @"(;\s*(SELECT|INSERT|UPDATE|DELETE|DROP|EXEC))|" + // Command chaining
        @"(\bUNION\b.*\bSELECT\b)|" +              // UNION injection
        @"(\bEXEC\s*\()|" +                        // EXEC() function
        @"(xp_cmdshell)|" +                        // SQL Server command execution
        @"(sp_executesql)",                        // Dynamic SQL execution
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline,
        TimeSpan.FromMilliseconds(100));  // CRITICAL FIX: Prevent ReDoS attacks

    // Enhanced XSS detection (includes DOM-based XSS, event handlers, SVG-based XSS)
    // SECURITY FIX: Added 100ms timeout to prevent ReDoS attacks
    private static readonly Regex XssPattern = new(
        @"<script|javascript:|onerror\s*=|onload\s*=|onmouseover\s*=|onclick\s*=|onfocus\s*=|" +
        @"<iframe|eval\(|expression\(|vbscript:|<embed|<object|" +
        @"<svg.*onload|innerHTML|document\.write|window\.location\s*=|" +
        @"data:text/html",
        RegexOptions.IgnoreCase | RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(100));  // CRITICAL FIX: Prevent ReDoS attacks

    // Enhanced path traversal detection (includes null byte, double encoding, UNC paths)
    // SECURITY FIX: Added 100ms timeout to prevent ReDoS attacks
    private static readonly Regex PathTraversalPattern = new(
        @"(\.\./|\.\.\\|%2e%2e%2f|%2e%2e/|\.\.%2f|%2e%2e%5c|" +
        @"%252e%252e|\\\\|%00|\.\.%5c)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(100));  // CRITICAL FIX: Prevent ReDoS attacks

    // Whitelisted paths that skip validation (file uploads, CSP violations, health checks)
    private static readonly HashSet<string> WhitelistedPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/video/upload",
        "/api/csp-violations",
        "/health",
        "/metrics"
    };

    // Paths where database-related keywords in content are legitimate (courses, reviews, chat)
    private static readonly HashSet<string> ContentWhitelistedPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/courses",
        "/api/reviews",
        "/api/chat/message"
    };

    public RequestValidationMiddleware(RequestDelegate next, ILogger<RequestValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip validation for whitelisted paths
        if (WhitelistedPaths.Any(path => context.Request.Path.StartsWithSegments(path)))
        {
            await _next(context);
            return;
        }

        // Check if path allows database-related keywords in content
        var isContentWhitelisted = ContentWhitelistedPaths.Any(path =>
            context.Request.Path.StartsWithSegments(path));

        // 1. Validate query parameters
        foreach (var param in context.Request.Query)
        {
            foreach (var value in param.Value)
            {
                if (ContainsMaliciousContent(value, isContentWhitelisted))
                {
                    _logger.LogWarning(
                        "[SECURITY] Malicious query parameter detected. IP: {IpAddress}, Param: {ParamKey}, ValueLength: {Length}",
                        GetClientIp(context),
                        param.Key,
                        value?.Length ?? 0);

                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = "Invalid request parameters",
                        detail = "Potentially malicious content detected in query parameters"
                    });
                    return;
                }
            }
        }

        // 2. Validate custom headers (skip standard headers)
        foreach (var header in context.Request.Headers
            .Where(h => !IsStandardHeader(h.Key)))
        {
            foreach (var value in header.Value)
            {
                if (ContainsMaliciousContent(value, isContentWhitelisted: false))
                {
                    _logger.LogWarning(
                        "[SECURITY] Malicious header detected. IP: {IpAddress}, Header: {HeaderKey}",
                        GetClientIp(context),
                        header.Key);

                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = "Invalid request headers",
                        detail = "Potentially malicious content detected in headers"
                    });
                    return;
                }
            }
        }

        // 3. Validate path for traversal attacks
        if (PathTraversalPattern.IsMatch(context.Request.Path))
        {
            _logger.LogWarning(
                "[SECURITY] Path traversal attack detected. IP: {IpAddress}, Path: {Path}",
                GetClientIp(context),
                context.Request.Path);

            context.Response.StatusCode = 400;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Invalid request path",
                detail = "Path traversal patterns are not allowed"
            });
            return;
        }

        // 4. Check content length (different limits for bulk operations)
        var maxAllowed = context.Request.Path.StartsWithSegments("/api/admin/bulk")
            ? MaxContentLengthBulk
            : MaxContentLengthStandard;

        if (context.Request.ContentLength.HasValue &&
            context.Request.ContentLength.Value > maxAllowed)
        {
            _logger.LogWarning(
                "[SECURITY] Request payload too large. IP: {IpAddress}, Size: {Size} bytes, Max: {Max} bytes",
                GetClientIp(context),
                context.Request.ContentLength.Value,
                maxAllowed);

            context.Response.StatusCode = 413; // Payload Too Large
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Request payload too large",
                detail = $"Maximum allowed size is {maxAllowed / 1_000_000}MB"
            });
            return;
        }

        // 5. Validate request body for POST/PUT/PATCH requests (CRITICAL FIX)
        if (context.Request.ContentLength > 0 &&
            (context.Request.Method == "POST" || context.Request.Method == "PUT" || context.Request.Method == "PATCH") &&
            context.Request.ContentType?.Contains("application/json") == true)
        {
            // Enable buffering to allow multiple reads
            context.Request.EnableBuffering();

            using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true))
            {
                var body = await reader.ReadToEndAsync();

                // Reset stream position for next middleware
                context.Request.Body.Position = 0;

                if (ContainsMaliciousContent(body, isContentWhitelisted))
                {
                    _logger.LogWarning(
                        "[SECURITY] Malicious content in request body. IP: {IpAddress}, Path: {Path}, ContentType: {ContentType}, BodyLength: {Length}",
                        GetClientIp(context),
                        context.Request.Path,
                        context.Request.ContentType,
                        body?.Length ?? 0);

                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = "Invalid request body",
                        detail = "Potentially malicious content detected in request body"
                    });
                    return;
                }
            }
        }

        // All validations passed, continue to next middleware
        await _next(context);
    }

    /// <summary>
    /// Checks if content contains malicious patterns (SQL injection, XSS)
    /// </summary>
    /// <param name="value">Content to validate</param>
    /// <param name="isContentWhitelisted">If true, SQL keywords in natural language are allowed</param>
    private bool ContainsMaliciousContent(string? value, bool isContentWhitelisted)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        // Performance optimization: skip regex for very short strings
        if (value.Length < 10)
            return false;

        // SECURITY FIX: Reject oversized input to prevent ReDoS attacks
        if (value.Length > 10000)
        {
            _logger.LogWarning("[RequestValidation] Oversized input rejected: {Length} characters", value.Length);
            return true;
        }

        try
        {
            // Check for SQL injection patterns (skip if content is whitelisted for database keywords)
            if (!isContentWhitelisted && SqlInjectionPattern.IsMatch(value))
                return true;

            // Check for XSS patterns (always validate, never whitelisted)
            if (XssPattern.IsMatch(value))
                return true;

            return false;
        }
        catch (RegexMatchTimeoutException ex)
        {
            // SECURITY FIX: Treat regex timeout as potential ReDoS attack
            _logger.LogWarning(ex, "[RequestValidation] Regex timeout on input validation - potential ReDoS attack. Input length: {Length}", value.Length);
            return true;  // Treat timeout as malicious
        }
    }

    /// <summary>
    /// Checks if header is a standard HTTP header (should not be validated for malicious content)
    /// </summary>
    private bool IsStandardHeader(string headerName)
    {
        // Standard headers that should not be validated (includes authentication headers)
        var standardHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Accept", "Accept-Encoding", "Accept-Language", "Authorization",
            "Cache-Control", "Connection", "Content-Type", "Content-Length",
            "Cookie", "Host", "Origin", "Referer", "User-Agent",
            "X-Forwarded-For", "X-Forwarded-Proto", "X-Real-IP",
            "X-Requested-With", "X-RateLimit-Limit", "X-RateLimit-Policy",
            "X-Api-Key", "X-CSRF-Token", "X-Request-ID", "X-Correlation-Id"
        };

        return standardHeaders.Contains(headerName);
    }

    /// <summary>
    /// Extracts client IP address (proxy-aware for Kubernetes/Nginx environments)
    /// </summary>
    private string GetClientIp(HttpContext context)
    {
        // Check X-Forwarded-For first (proxy/load balancer)
        var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwarded))
            return forwarded.Split(',')[0].Trim();

        // Check X-Real-IP (alternative proxy header)
        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
            return realIp;

        // Fallback to remote IP
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
