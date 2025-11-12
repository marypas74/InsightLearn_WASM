using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Security.Claims;

namespace InsightLearn.Application.Middleware;

/// <summary>
/// Distributed rate limiting middleware using Redis for cross-pod coordination
/// Enforces global rate limits across all API instances in Kubernetes
/// </summary>
public class DistributedRateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<DistributedRateLimitMiddleware> _logger;
    private readonly RateLimitOptions _options;

    public DistributedRateLimitMiddleware(
        RequestDelegate next,
        IConnectionMultiplexer redis,
        IOptions<RateLimitOptions> options,
        ILogger<DistributedRateLimitMiddleware> logger)
    {
        _next = next;
        _redis = redis;
        _logger = logger;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip rate limiting for exempt paths
        if (IsExemptPath(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var clientId = GetClientIdentifier(context);
        var currentMinute = DateTime.UtcNow.ToString("yyyyMMddHHmm");
        var rateLimitKey = $"ratelimit:{clientId}:{currentMinute}";

        var db = _redis.GetDatabase();

        try
        {
            // Increment request count with 60-second TTL
            var currentCount = await db.StringIncrementAsync(rateLimitKey);

            if (currentCount == 1)
            {
                // First request in this window - set expiry
                await db.KeyExpireAsync(rateLimitKey, TimeSpan.FromSeconds(60));
            }

            // Check if limit exceeded
            if (currentCount > _options.RequestsPerMinute)
            {
                _logger.LogWarning("[RateLimit] Limit exceeded for {ClientId}: {Count}/{Limit}",
                    clientId, currentCount, _options.RequestsPerMinute);

                context.Response.StatusCode = 429;
                context.Response.Headers["Retry-After"] = "60";
                context.Response.Headers["X-RateLimit-Limit"] = _options.RequestsPerMinute.ToString();
                context.Response.Headers["X-RateLimit-Remaining"] = "0";
                context.Response.Headers["X-RateLimit-Reset"] =
                    DateTimeOffset.UtcNow.AddSeconds(60).ToUnixTimeSeconds().ToString();

                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Rate limit exceeded",
                    message = $"Too many requests. Limit: {_options.RequestsPerMinute} requests per minute.",
                    retryAfter = 60
                });

                return;
            }

            // Add rate limit headers to response
            context.Response.OnStarting(() =>
            {
                context.Response.Headers["X-RateLimit-Limit"] = _options.RequestsPerMinute.ToString();
                context.Response.Headers["X-RateLimit-Remaining"] =
                    Math.Max(0, _options.RequestsPerMinute - (int)currentCount).ToString();
                context.Response.Headers["X-RateLimit-Reset"] =
                    DateTimeOffset.UtcNow.AddSeconds(60).ToUnixTimeSeconds().ToString();
                return Task.CompletedTask;
            });

            await _next(context);
        }
        catch (RedisConnectionException ex)
        {
            _logger.LogError(ex, "[RateLimit] Redis connection failed - allowing request");
            // Fail open: allow request if Redis is down
            await _next(context);
        }
    }

    private string GetClientIdentifier(HttpContext context)
    {
        // Priority: 1) JWT userId, 2) IP address
        var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            return $"user:{userId}";
        }

        // Get IP address (handle proxies)
        var ipAddress = context.Request.Headers["X-Forwarded-For"].FirstOrDefault()
            ?? context.Request.Headers["X-Real-IP"].FirstOrDefault()
            ?? context.Connection.RemoteIpAddress?.ToString()
            ?? "unknown";

        // Handle IPv6 loopback
        if (ipAddress == "::1")
            ipAddress = "127.0.0.1";

        return $"ip:{ipAddress}";
    }

    private bool IsExemptPath(PathString path)
    {
        var exemptPaths = _options.ExemptPaths ?? Array.Empty<string>();
        return exemptPaths.Any(exempt => path.Value?.StartsWith(exempt, StringComparison.OrdinalIgnoreCase) == true);
    }
}

/// <summary>
/// Configuration options for distributed rate limiting
/// </summary>
public class RateLimitOptions
{
    public int RequestsPerMinute { get; set; } = 100;
    public string[] ExemptPaths { get; set; } = Array.Empty<string>();
}