using System.Diagnostics;
using InsightLearn.Core.DTOs;

namespace InsightLearn.Application.Middleware;

/// <summary>
/// Global exception handler middleware that catches all unhandled exceptions in the API pipeline.
/// Provides environment-aware error responses with safe messages in production.
///
/// Features:
/// - Centralized exception handling for all API endpoints
/// - HTTP status code mapping based on exception type
/// - Production-safe error messages (no stack traces or sensitive info)
/// - Development-detailed error messages (includes exception details)
/// - Correlation via TraceId for distributed tracing
/// - Structured logging of all exceptions
///
/// Security Considerations:
/// - NEVER exposes internal server details (stack traces, file paths) in production
/// - Prevents information disclosure attacks (OWASP A01:2021)
/// - Safe error messages prevent enumeration attacks
///
/// Position in Middleware Pipeline:
/// - MUST be registered FIRST (before all other middleware)
/// - Wraps entire request pipeline in try-catch
/// - Catches exceptions from ANY downstream middleware or endpoint
/// </summary>
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
        // Extract user context for logging (if authenticated)
        var userId = context.User?.FindFirst("userId")?.Value ?? "anonymous";
        var userEmail = context.User?.FindFirst("email")?.Value ?? "unknown";
        var requestPath = context.Request.Path;
        var requestMethod = context.Request.Method;

        // Log exception with full details (regardless of environment)
        _logger.LogError(
            exception,
            "[GLOBAL_ERROR] Unhandled exception | User: {UserId} ({Email}) | {Method} {Path} | TraceId: {TraceId}",
            userId,
            userEmail,
            requestMethod,
            requestPath,
            Activity.Current?.Id ?? context.TraceIdentifier);

        // Create standardized error response
        var response = new ErrorResponse
        {
            Error = "An error occurred processing your request",
            TraceId = Activity.Current?.Id ?? context.TraceIdentifier,
            Timestamp = DateTime.UtcNow
        };

        // Map exception type to HTTP status code
        var statusCode = exception switch
        {
            // 400 Bad Request - Client errors
            // Note: ArgumentNullException must come BEFORE ArgumentException (it's a subclass)
            ArgumentNullException => StatusCodes.Status400BadRequest,
            ArgumentException => StatusCodes.Status400BadRequest,
            FormatException => StatusCodes.Status400BadRequest,

            // 401 Unauthorized - Authentication required
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,

            // 403 Forbidden - Authentication succeeded but insufficient permissions
            // (No specific exception type - handled in authorization middleware)

            // 404 Not Found - Resource doesn't exist
            KeyNotFoundException => StatusCodes.Status404NotFound,
            FileNotFoundException => StatusCodes.Status404NotFound,

            // 409 Conflict - Resource conflict (duplicate, concurrency)
            // Check specific InvalidOperationException patterns BEFORE generic catch
            InvalidOperationException when exception.Message.Contains("conflict", StringComparison.OrdinalIgnoreCase)
                => StatusCodes.Status409Conflict,

            // 400 Bad Request - Generic InvalidOperationException (after checking for conflict)
            InvalidOperationException => StatusCodes.Status400BadRequest,

            // 501 Not Implemented - Feature not yet implemented
            NotImplementedException => StatusCodes.Status501NotImplemented,

            // 503 Service Unavailable - External dependency failure
            TimeoutException => StatusCodes.Status503ServiceUnavailable,
            HttpRequestException => StatusCodes.Status503ServiceUnavailable,

            // 500 Internal Server Error - Everything else
            _ => StatusCodes.Status500InternalServerError
        };

        // Set response error category based on status code
        response.Error = statusCode switch
        {
            400 => "BadRequest",
            401 => "Unauthorized",
            403 => "Forbidden",
            404 => "NotFound",
            409 => "Conflict",
            501 => "NotImplemented",
            503 => "ServiceUnavailable",
            _ => "InternalServerError"
        };

        // Environment-aware error messages
        if (_env.IsDevelopment())
        {
            // Development: Include full exception details
            response.Message = exception.Message;
            response.ValidationErrors = new Dictionary<string, string[]>
            {
                ["ExceptionType"] = new[] { exception.GetType().FullName ?? exception.GetType().Name },
                ["StackTrace"] = new[] { exception.StackTrace ?? "No stack trace available" },
                ["InnerException"] = exception.InnerException != null
                    ? new[] { exception.InnerException.Message }
                    : new[] { "None" }
            };

            _logger.LogDebug(
                "[GLOBAL_ERROR] Development mode - returning detailed error: {ExceptionType}",
                exception.GetType().Name);
        }
        else
        {
            // Production: Safe, generic messages to prevent information disclosure
            response.Message = statusCode switch
            {
                400 => "The request was invalid or cannot be processed. Please check your input and try again.",
                401 => "Authentication is required to access this resource. Please log in and try again.",
                403 => "You do not have permission to access this resource.",
                404 => "The requested resource was not found. Please verify the URL and try again.",
                409 => "A conflict occurred while processing your request. The resource may have been modified.",
                501 => "This feature is not yet implemented. Please contact support for more information.",
                503 => "The service is temporarily unavailable. Please try again later.",
                _ => "An unexpected error occurred. Please try again later or contact support if the problem persists."
            };

            _logger.LogInformation(
                "[GLOBAL_ERROR] Production mode - returning safe error message for status {StatusCode}",
                statusCode);
        }

        // Set response status code and content type
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        // Write JSON response
        await context.Response.WriteAsJsonAsync(response);

        _logger.LogInformation(
            "[GLOBAL_ERROR] Error response sent | Status: {StatusCode} | TraceId: {TraceId}",
            statusCode,
            response.TraceId);
    }
}
