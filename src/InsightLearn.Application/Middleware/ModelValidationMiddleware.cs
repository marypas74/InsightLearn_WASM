using System.Text.Json;

namespace InsightLearn.Application.Middleware;

/// <summary>
/// Middleware to centrally log all model validation failures (400 Bad Request)
/// Enables monitoring of validation patterns for security and debugging purposes
/// Phase 3.2: Completes validation logging infrastructure after Phase 3.1 (DTO validation attributes)
/// </summary>
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
        // Store original response stream (may be null for GET requests)
        var originalBodyStream = context.Response.Body;

        try
        {
            // Use a memory stream to capture response body
            using (var responseBody = new MemoryStream())
            {
                context.Response.Body = responseBody;

                // Continue processing
                await _next(context);

                // If response is 400 Bad Request, capture and log validation errors
                if (context.Response.StatusCode == 400)
                {
                    var validationErrors = await ExtractValidationErrors(context, responseBody);

                    LogValidationFailure(context, validationErrors);
                }

                // Copy captured response back to original stream
                await responseBody.CopyToAsync(originalBodyStream);
            }
        }
        finally
        {
            // Restore original stream
            context.Response.Body = originalBodyStream;
        }
    }

    /// <summary>
    /// Extracts validation error details from response body
    /// Handles both ModelStateDictionary and ProblemDetails formats
    /// </summary>
    private async Task<Dictionary<string, string[]>> ExtractValidationErrors(HttpContext context, MemoryStream responseBody)
    {
        var validationErrors = new Dictionary<string, string[]>();

        try
        {
            // Reset stream position to read from beginning
            responseBody.Position = 0;

            using (var reader = new StreamReader(responseBody, leaveOpen: true))
            {
                var content = await reader.ReadToEndAsync();
                responseBody.Position = 0; // Reset for actual response writing

                if (!string.IsNullOrWhiteSpace(content))
                {
                    // Try to parse as JSON
                    using (var doc = JsonDocument.Parse(content))
                    {
                        var root = doc.RootElement;

                        // Handle ProblemDetails format (RFC 7231) - ASP.NET Core standard
                        // Example: { "type": "https://...", "title": "One or more validation errors occurred.",
                        //           "status": 400, "errors": { "field": ["error1", "error2"] } }
                        if (root.TryGetProperty("errors", out var errorsElement) &&
                            errorsElement.ValueKind == JsonValueKind.Object)
                        {
                            foreach (var property in errorsElement.EnumerateObject())
                            {
                                var fieldName = property.Name;
                                var errorMessages = new List<string>();

                                if (property.Value.ValueKind == JsonValueKind.Array)
                                {
                                    foreach (var errorElement in property.Value.EnumerateArray())
                                    {
                                        if (errorElement.ValueKind == JsonValueKind.String)
                                        {
                                            errorMessages.Add(errorElement.GetString() ?? "");
                                        }
                                    }
                                }

                                if (errorMessages.Count > 0)
                                    validationErrors[fieldName] = errorMessages.ToArray();
                            }
                        }
                        // Handle custom error response format
                        else if (root.TryGetProperty("error", out var errorElement))
                        {
                            validationErrors["general"] = new[] { errorElement.GetString() ?? "Unknown error" };
                        }
                    }
                }
            }
        }
        catch (JsonException ex)
        {
            // If JSON parsing fails, log the raw content as general error
            _logger.LogWarning(ex, "[ModelValidation] Failed to parse validation error response as JSON");
            validationErrors["parsing_error"] = new[] { "Could not parse validation errors" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ModelValidation] Unexpected error extracting validation errors");
        }

        return validationErrors;
    }

    /// <summary>
    /// Centrally logs validation failures with detailed context information
    /// Enables analysis of validation patterns, potential attack vectors, and debugging
    /// </summary>
    private void LogValidationFailure(HttpContext context, Dictionary<string, string[]> validationErrors)
    {
        try
        {
            var clientIp = GetClientIp(context);
            var userId = context.User?.FindFirst("sub")?.Value ??
                        context.User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value ??
                        "anonymous";
            var userName = context.User?.Identity?.Name ?? "unauthenticated";
            var errorCount = validationErrors.Count;
            var fieldNames = string.Join(", ", validationErrors.Keys.Take(5)); // Log first 5 fields
            var hasMoreFields = validationErrors.Count > 5;

            // Log structured validation failure event
            _logger.LogWarning(
                "[VALIDATION_FAILURE] Path: {Path}, Method: {Method}, IP: {IpAddress}, User: {UserId} ({UserName}), " +
                "ErrorCount: {ErrorCount}, Fields: {FieldNames}{MoreFields}, Timestamp: {Timestamp}",
                context.Request.Path,
                context.Request.Method,
                clientIp,
                userId,
                userName,
                errorCount,
                fieldNames,
                hasMoreFields ? " (and more)" : "",
                DateTime.UtcNow);

            // Log detailed error messages for security monitoring and debugging
            foreach (var kvp in validationErrors)
            {
                var fieldName = kvp.Key;
                var errors = kvp.Value;

                _logger.LogInformation(
                    "[VALIDATION_ERROR_DETAIL] Path: {Path}, Field: {Field}, " +
                    "Messages: {ErrorMessages}, IP: {IpAddress}, User: {UserId}",
                    context.Request.Path,
                    fieldName,
                    string.Join(" | ", errors),
                    clientIp,
                    userId);
            }

            // Log potential security concerns
            LogSecurityConcerns(context, validationErrors, clientIp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ModelValidation] Error logging validation failure");
        }
    }

    /// <summary>
    /// Analyzes validation errors for potential security concerns
    /// Examples: Brute force attempts, injection attacks, DoS patterns
    /// </summary>
    private void LogSecurityConcerns(HttpContext context, Dictionary<string, string[]> validationErrors, string clientIp)
    {
        // Pattern 1: Repeated 400 errors from same IP on same endpoint (potential brute force)
        var endpoint = context.Request.Path;
        var method = context.Request.Method;

        // Pattern 2: Multiple validation errors on authentication endpoints (brute force indicators)
        if ((endpoint.StartsWithSegments("/api/auth/login") ||
             endpoint.StartsWithSegments("/api/auth/register")) &&
            validationErrors.Count > 1)
        {
            _logger.LogWarning(
                "[SECURITY_CONCERN] Multiple validation errors on auth endpoint. IP: {IpAddress}, " +
                "Endpoint: {Endpoint}, Fields: {FieldCount}, Possible brute force attempt",
                clientIp,
                endpoint,
                validationErrors.Count);
        }

        // Pattern 3: Very long validation error messages (potential injection attempt)
        var longErrorCount = validationErrors.Values
            .SelectMany(msgs => msgs)
            .Count(msg => msg.Length > 500);

        if (longErrorCount > 0)
        {
            _logger.LogWarning(
                "[SECURITY_CONCERN] Oversized validation error message detected. IP: {IpAddress}, " +
                "Endpoint: {Endpoint}, Count: {Count}, Possible injection attempt",
                clientIp,
                endpoint,
                longErrorCount);
        }

        // Pattern 4: SQL/XSS keywords in validation error fields (already blocked by RequestValidationMiddleware)
        var suspiciousFields = validationErrors.Keys
            .Where(k => ContainsSuspiciousKeywords(k))
            .ToList();

        if (suspiciousFields.Count > 0)
        {
            _logger.LogWarning(
                "[SECURITY_CONCERN] Suspicious field names in validation error. IP: {IpAddress}, " +
                "Endpoint: {Endpoint}, Fields: {SuspiciousFields}",
                clientIp,
                endpoint,
                string.Join(", ", suspiciousFields));
        }
    }

    /// <summary>
    /// Checks if field name contains suspicious keywords
    /// </summary>
    private bool ContainsSuspiciousKeywords(string fieldName)
    {
        var suspiciousPatterns = new[]
        {
            "select", "insert", "update", "delete", "drop", "union",
            "script", "javascript", "onclick", "onerror", "onload",
            "eval", "iframe", "exec", "shell", "cmd"
        };

        var lowerFieldName = fieldName.ToLowerInvariant();
        return suspiciousPatterns.Any(pattern => lowerFieldName.Contains(pattern));
    }

    /// <summary>
    /// Extracts client IP address (proxy-aware for Kubernetes/Nginx environments)
    /// Consistent with implementation in RequestValidationMiddleware
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
