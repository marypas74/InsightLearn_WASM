using Microsoft.AspNetCore.Http;

namespace InsightLearn.Application.Middleware;

/// <summary>
/// Middleware to add comprehensive security headers to all HTTP responses.
/// Protects against XSS, clickjacking, MIME sniffing, and cross-origin attacks.
///
/// Implements OWASP ASVS V14.4 (Security Headers) compliance.
///
/// Security Headers Applied:
/// - X-Frame-Options: Prevents clickjacking attacks
/// - X-Content-Type-Options: Prevents MIME type sniffing
/// - Strict-Transport-Security (HSTS): Forces HTTPS (production only)
/// - Content-Security-Policy (CSP): Advanced XSS protection with Blazor WASM compatibility
/// - Permissions-Policy: Restricts access to sensitive browser features
/// - Referrer-Policy: Controls referrer information leakage
/// - Cross-Origin-Embedder-Policy (COEP): Prevents cross-origin resource loading
/// - Cross-Origin-Opener-Policy (COOP): Isolates browsing context
/// - Cross-Origin-Resource-Policy (CORP): Controls cross-origin resource access
/// - X-XSS-Protection: Legacy XSS filter for old browsers
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityHeadersMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public SecurityHeadersMiddleware(
        RequestDelegate next,
        ILogger<SecurityHeadersMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Add security headers to response
        AddSecurityHeaders(context);

        await _next(context);
    }

    private void AddSecurityHeaders(HttpContext context)
    {
        var headers = context.Response.Headers;

        // X-Frame-Options: Prevent clickjacking attacks
        // Disallows embedding this page in iframe/frame/object
        // OWASP ASVS V14.4.3: Set X-Frame-Options to DENY or SAMEORIGIN
        if (!headers.ContainsKey("X-Frame-Options"))
        {
            headers["X-Frame-Options"] = "DENY";
        }

        // X-Content-Type-Options: Prevent MIME type sniffing
        // Forces browser to respect declared Content-Type
        // OWASP ASVS V14.4.4: Set X-Content-Type-Options to nosniff
        if (!headers.ContainsKey("X-Content-Type-Options"))
        {
            headers["X-Content-Type-Options"] = "nosniff";
        }

        // Strict-Transport-Security (HSTS): Force HTTPS for 1 year (production only)
        // Prevents SSL stripping attacks and cookie hijacking
        // OWASP ASVS V14.4.5: Use HSTS with max-age of at least 31536000 seconds
        if (!_environment.IsDevelopment() && !headers.ContainsKey("Strict-Transport-Security"))
        {
            headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains; preload";
        }

        // Content-Security-Policy (CSP): Advanced XSS protection
        // Configured for Blazor WASM compatibility (requires unsafe-eval, wasm-unsafe-eval)
        // Includes CSP violation reporting endpoint
        // OWASP ASVS V14.4.7: Define a Content-Security-Policy
        if (!headers.ContainsKey("Content-Security-Policy"))
        {
            headers["Content-Security-Policy"] =
                "default-src 'self'; " +
                "script-src 'self' 'unsafe-eval' 'wasm-unsafe-eval'; " +  // Blazor WASM needs unsafe-eval
                "style-src 'self' 'unsafe-inline'; " +                     // Blazor needs inline styles
                "img-src 'self' data: https:; " +
                "font-src 'self' data:; " +
                "connect-src 'self' wss: https:; " +                       // WebSocket + HTTPS APIs
                "frame-ancestors 'self'; " +                               // Same as X-Frame-Options
                "base-uri 'self'; " +                                      // Prevent base tag injection
                "form-action 'self'; " +                                   // Prevent form hijacking
                "object-src 'none'; " +                                    // Block plugins (Flash, Java, etc.)
                "media-src 'self' data:; " +                               // Audio/video from same origin
                "worker-src 'self' blob:; " +                              // Web workers for Blazor
                "manifest-src 'self'; " +                                  // PWA manifest
                "report-uri /api/csp-violations";                          // CSP violation reporting
        }

        // Referrer-Policy: Control referrer information leakage
        // Prevents sensitive URL parameters from being sent in Referer header
        // Balance between privacy and analytics
        if (!headers.ContainsKey("Referrer-Policy"))
        {
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        }

        // Permissions-Policy (formerly Feature-Policy): Restrict access to sensitive browser features
        // Prevents unauthorized use of camera, microphone, geolocation, payment APIs
        // LMS-specific: Enable clipboard (for code editors), fullscreen (video player), autoplay (video courses)
        if (!headers.ContainsKey("Permissions-Policy"))
        {
            headers["Permissions-Policy"] =
                "geolocation=(), " +                                       // Block location access
                "microphone=(), " +                                        // Block microphone (no video conferencing)
                "camera=(), " +                                            // Block camera (no video conferencing)
                "payment=(), " +                                           // Block Payment Request API (use Stripe instead)
                "usb=(), " +                                               // Block USB device access
                "magnetometer=(), " +                                      // Block magnetometer sensor
                "clipboard-read=(self), " +                                // Allow copy/paste in forms
                "clipboard-write=(self), " +                               // Allow copy/paste in code editors
                "fullscreen=(self), " +                                    // Allow fullscreen for video player
                "autoplay=(self)";                                         // Allow autoplay for video courses
        }

        // Cross-Origin-Embedder-Policy (COEP): Prevent cross-origin resource loading
        // Required for SharedArrayBuffer and high-resolution timers (performance.now)
        // Part of "cross-origin isolation" (COEP + COOP)
        if (!headers.ContainsKey("Cross-Origin-Embedder-Policy"))
        {
            headers["Cross-Origin-Embedder-Policy"] = "require-corp";
        }

        // Cross-Origin-Opener-Policy (COOP): Isolate browsing context
        // Prevents cross-origin documents from opening this page in a popup
        // Protects against Spectre-like side-channel attacks
        // Part of "cross-origin isolation" (COEP + COOP)
        if (!headers.ContainsKey("Cross-Origin-Opener-Policy"))
        {
            headers["Cross-Origin-Opener-Policy"] = "same-origin";
        }

        // Cross-Origin-Resource-Policy (CORP): Control cross-origin resource access
        // Prevents other origins from reading this resource (defense-in-depth with CORS)
        // Complements Cross-Origin-Embedder-Policy
        if (!headers.ContainsKey("Cross-Origin-Resource-Policy"))
        {
            headers["Cross-Origin-Resource-Policy"] = "same-origin";
        }

        // X-XSS-Protection: Legacy XSS filter for old browsers (IE11, Safari 9)
        // NOTE: Deprecated in modern browsers (Chrome 78+, Firefox, Edge)
        // Modern browsers rely on Content-Security-Policy instead
        // Kept for legacy browser support only
        if (!headers.ContainsKey("X-XSS-Protection"))
        {
            headers["X-XSS-Protection"] = "1; mode=block";
        }

        _logger.LogDebug("[SecurityHeaders] Security headers added to response for {Path}",
            context.Request.Path);
    }
}
