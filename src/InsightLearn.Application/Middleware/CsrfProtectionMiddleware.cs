using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace InsightLearn.Application.Middleware;

/// <summary>
/// Middleware for CSRF (Cross-Site Request Forgery) protection
/// Generates and validates CSRF tokens for state-changing requests
/// PCI DSS Requirement 6.5.9 Compliance
/// </summary>
public class CsrfProtectionMiddleware
{
    private const string CSRF_TOKEN_HEADER = "X-CSRF-Token";
    private const string CSRF_COOKIE_NAME = "XSRF-TOKEN";
    private readonly RequestDelegate _next;
    private readonly ILogger<CsrfProtectionMiddleware> _logger;
    private readonly HashSet<string> _exemptPaths;

    public CsrfProtectionMiddleware(
        RequestDelegate next,
        ILogger<CsrfProtectionMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;

        // Read exempt paths from configuration
        var exemptPaths = configuration.GetSection("Security:CsrfExemptPaths").Get<string[]>()
            ?? new[] { "/api/auth/login", "/api/auth/register", "/health", "/api/info" };

        _exemptPaths = new HashSet<string>(exemptPaths, StringComparer.OrdinalIgnoreCase);

        _logger.LogInformation("[CSRF] Protection enabled with {Count} exempt paths", _exemptPaths.Count);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Generate and set CSRF token for all responses
        if (!context.Request.Cookies.ContainsKey(CSRF_COOKIE_NAME))
        {
            var token = GenerateCsrfToken();
            context.Response.Cookies.Append(CSRF_COOKIE_NAME, token, new CookieOptions
            {
                HttpOnly = false,  // JavaScript needs to read this
                Secure = true,      // HTTPS only
                SameSite = SameSiteMode.Strict,
                MaxAge = TimeSpan.FromHours(2)
            });

            _logger.LogDebug("[CSRF] Generated new token for path: {Path}", path);
        }

        // Validate CSRF token for state-changing requests
        if (IsStateChangingRequest(context.Request.Method) && !IsExemptPath(path))
        {
            if (!await ValidateCsrfTokenAsync(context))
            {
                _logger.LogWarning("[CSRF] Token validation failed for {Method} {Path} from {IP}",
                    context.Request.Method, path, context.Connection.RemoteIpAddress);

                context.Response.StatusCode = 403;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "CSRF token validation failed",
                    message = "Missing or invalid CSRF token. Include X-CSRF-Token header with the value from XSRF-TOKEN cookie."
                });
                return;
            }

            _logger.LogDebug("[CSRF] Token validated successfully for {Method} {Path}", context.Request.Method, path);
        }

        await _next(context);
    }

    private bool IsStateChangingRequest(string method)
    {
        return method == "POST" || method == "PUT" || method == "DELETE" || method == "PATCH";
    }

    private bool IsExemptPath(string path)
    {
        return _exemptPaths.Any(exempt => path.StartsWith(exempt, StringComparison.OrdinalIgnoreCase));
    }

    private string GenerateCsrfToken()
    {
        var randomBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        return Convert.ToBase64String(randomBytes);
    }

    private Task<bool> ValidateCsrfTokenAsync(HttpContext context)
    {
        // Get token from header
        if (!context.Request.Headers.TryGetValue(CSRF_TOKEN_HEADER, out var headerToken))
        {
            _logger.LogDebug("[CSRF] Missing CSRF token header");
            return Task.FromResult(false);
        }

        // Get token from cookie
        if (!context.Request.Cookies.TryGetValue(CSRF_COOKIE_NAME, out var cookieToken))
        {
            _logger.LogDebug("[CSRF] Missing CSRF token cookie");
            return Task.FromResult(false);
        }

        // Compare tokens (constant-time comparison to prevent timing attacks)
        if (string.IsNullOrEmpty(headerToken) || string.IsNullOrEmpty(cookieToken))
            return Task.FromResult(false);

        // Ensure both tokens are the same length before comparison
        var headerTokenBytes = Encoding.UTF8.GetBytes(headerToken!);
        var cookieTokenBytes = Encoding.UTF8.GetBytes(cookieToken);

        if (headerTokenBytes.Length != cookieTokenBytes.Length)
            return Task.FromResult(false);

        var isValid = CryptographicOperations.FixedTimeEquals(headerTokenBytes, cookieTokenBytes);
        return Task.FromResult(isValid);
    }
}
