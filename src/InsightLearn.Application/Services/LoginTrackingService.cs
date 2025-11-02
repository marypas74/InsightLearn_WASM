using InsightLearn.Application.Interfaces;
using InsightLearn.Core.Entities;
using InsightLearn.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace InsightLearn.Application.Services;

public class LoginTrackingService : ILoginTrackingService
{
    private readonly InsightLearnDbContext _context;
    private readonly ILogger<LoginTrackingService> _logger;

    public LoginTrackingService(InsightLearnDbContext context, ILogger<LoginTrackingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Guid> LogLoginAttemptAsync(
        string email,
        Guid? userId,
        bool isSuccess,
        string loginMethod = "EmailPassword",
        string? failureReason = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? deviceFingerprint = null,
        string? geolocationData = null,
        decimal? riskScore = null,
        string? authProvider = null,
        string? providerUserId = null,
        string? correlationId = null)
    {
        try
        {
            var loginAttempt = new LoginAttempt
            {
                Id = Guid.NewGuid(),
                Email = email,
                UserId = userId,
                LoginMethod = loginMethod,
                IsSuccess = isSuccess,
                FailureReason = failureReason,
                IpAddress = ipAddress ?? "unknown",
                UserAgent = userAgent,
                DeviceFingerprint = deviceFingerprint,
                GeolocationData = geolocationData,
                RiskScore = riskScore,
                AuthProvider = authProvider,
                ProviderUserId = providerUserId,
                CorrelationId = correlationId,
                AttemptedAt = DateTime.UtcNow
            };

            _context.LoginAttempts.Add(loginAttempt);
            await _context.SaveChangesAsync();

            // Update login method statistics if user is known
            if (userId.HasValue && isSuccess)
            {
                await TrackLoginMethodAsync(userId.Value, loginMethod, true, providerUserId, authProvider, email);
            }
            else if (userId.HasValue && !isSuccess)
            {
                await TrackLoginMethodAsync(userId.Value, loginMethod, false, providerUserId, authProvider, email);
            }

            _logger.LogInformation("Login attempt logged for email {Email}, Success: {IsSuccess}, Method: {LoginMethod}, CorrelationId: {CorrelationId}",
                email, isSuccess, loginMethod, correlationId);

            return loginAttempt.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log login attempt for email {Email}", email);
            throw;
        }
    }

    public async Task<Guid> StartSessionAsync(
        Guid userId,
        string sessionId,
        string ipAddress,
        string? userAgent = null,
        string? deviceType = null,
        string? platform = null,
        string? browser = null,
        Guid? loginAttemptId = null,
        string? jwtTokenId = null,
        string? geolocationData = null,
        string? timeZone = null)
    {
        try
        {
            // End any existing active sessions for this user (optional - depends on business rules)
            var existingSessions = await _context.UserSessions
                .Where(s => s.UserId == userId && s.IsActive)
                .ToListAsync();

            foreach (var session in existingSessions)
            {
                session.IsActive = false;
                session.EndedAt = DateTime.UtcNow;
                session.EndReason = "NewSession";
            }

            var userSession = new UserSession
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                UserId = userId,
                StartedAt = DateTime.UtcNow,
                LastActivityAt = DateTime.UtcNow,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                DeviceType = deviceType,
                Platform = platform,
                Browser = browser,
                LoginAttemptId = loginAttemptId,
                JwtTokenId = jwtTokenId,
                GeolocationData = geolocationData,
                TimeZone = timeZone,
                IsActive = true
            };

            _context.UserSessions.Add(userSession);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Session started for user {UserId}, SessionId: {SessionId}", userId, sessionId);

            return userSession.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start session for user {UserId}", userId);
            throw;
        }
    }

    public async Task UpdateSessionActivityAsync(
        string sessionId,
        string? lastPageVisited = null,
        int? additionalActivityCount = null,
        long? additionalDataTransferred = null)
    {
        try
        {
            var session = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.IsActive);

            if (session != null)
            {
                session.LastActivityAt = DateTime.UtcNow;

                if (!string.IsNullOrEmpty(lastPageVisited))
                    session.LastPageVisited = lastPageVisited;

                if (additionalActivityCount.HasValue)
                    session.ActivityCount += additionalActivityCount.Value;

                if (additionalDataTransferred.HasValue)
                    session.DataTransferred += additionalDataTransferred.Value;

                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update session activity for session {SessionId}", sessionId);
        }
    }

    public async Task EndSessionAsync(string sessionId, string endReason = "Logout")
    {
        try
        {
            var session = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.IsActive);

            if (session != null)
            {
                session.IsActive = false;
                session.EndedAt = DateTime.UtcNow;
                session.EndReason = endReason;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Session ended for SessionId: {SessionId}, Reason: {EndReason}", sessionId, endReason);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to end session {SessionId}", sessionId);
            throw;
        }
    }

    public async Task<UserSession?> GetActiveSessionAsync(string sessionId)
    {
        try
        {
            return await _context.UserSessions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.IsActive);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active session {SessionId}", sessionId);
            return null;
        }
    }

    public async Task<List<UserSession>> GetActiveUserSessionsAsync(Guid userId)
    {
        try
        {
            return await _context.UserSessions
                .Where(s => s.UserId == userId && s.IsActive)
                .OrderByDescending(s => s.StartedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active sessions for user {UserId}", userId);
            return new List<UserSession>();
        }
    }

    public async Task TrackLoginMethodAsync(
        Guid userId,
        string methodType,
        bool wasSuccessful,
        string? providerUserId = null,
        string? providerName = null,
        string? providerAccountEmail = null,
        string? metadataJson = null)
    {
        try
        {
            var loginMethod = await _context.LoginMethods
                .FirstOrDefaultAsync(lm => lm.UserId == userId && lm.MethodType == methodType);

            if (loginMethod == null)
            {
                loginMethod = new LoginMethod
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    MethodType = methodType,
                    ProviderUserId = providerUserId,
                    ProviderName = providerName,
                    ProviderAccountEmail = providerAccountEmail,
                    MetadataJson = metadataJson,
                    FirstUsedAt = DateTime.UtcNow,
                    LastUsedAt = DateTime.UtcNow,
                    SuccessfulLogins = wasSuccessful ? 1 : 0,
                    FailedAttempts = wasSuccessful ? 0 : 1
                };

                _context.LoginMethods.Add(loginMethod);
            }
            else
            {
                loginMethod.LastUsedAt = DateTime.UtcNow;

                if (wasSuccessful)
                    loginMethod.SuccessfulLogins++;
                else
                    loginMethod.FailedAttempts++;

                // Update metadata if provided
                if (!string.IsNullOrEmpty(metadataJson))
                    loginMethod.MetadataJson = metadataJson;
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track login method for user {UserId}, method {MethodType}", userId, methodType);
        }
    }

    public async Task<List<LoginMethod>> GetUserLoginMethodsAsync(Guid userId)
    {
        try
        {
            return await _context.LoginMethods
                .Where(lm => lm.UserId == userId)
                .OrderByDescending(lm => lm.LastUsedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get login methods for user {UserId}", userId);
            return new List<LoginMethod>();
        }
    }

    public async Task<List<LoginAttempt>> GetRecentLoginAttemptsAsync(
        Guid? userId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        bool? successOnly = null,
        int limit = 100)
    {
        try
        {
            var query = _context.LoginAttempts.AsQueryable();

            if (userId.HasValue)
                query = query.Where(la => la.UserId == userId.Value);

            if (startDate.HasValue)
                query = query.Where(la => la.AttemptedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(la => la.AttemptedAt <= endDate.Value);

            if (successOnly.HasValue)
                query = query.Where(la => la.IsSuccess == successOnly.Value);

            return await query
                .OrderByDescending(la => la.AttemptedAt)
                .Take(limit)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get recent login attempts");
            return new List<LoginAttempt>();
        }
    }

    public async Task<Dictionary<string, int>> GetLoginStatsByMethodAsync(
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        try
        {
            var query = _context.LoginAttempts.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(la => la.AttemptedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(la => la.AttemptedAt <= endDate.Value);

            return await query
                .Where(la => la.IsSuccess)
                .GroupBy(la => la.LoginMethod)
                .Select(g => new { Method = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Method, x => x.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get login stats by method");
            return new Dictionary<string, int>();
        }
    }

    public async Task<List<(string IpAddress, int AttemptCount, int FailureCount)>> GetSuspiciousIpAddressesAsync(
        int minFailureCount = 5,
        TimeSpan? timeWindow = null)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.Subtract(timeWindow ?? TimeSpan.FromHours(24));

            return await _context.LoginAttempts
                .Where(la => la.AttemptedAt >= cutoffDate)
                .GroupBy(la => la.IpAddress)
                .Select(g => new
                {
                    IpAddress = g.Key,
                    AttemptCount = g.Count(),
                    FailureCount = g.Count(x => !x.IsSuccess)
                })
                .Where(x => x.FailureCount >= minFailureCount)
                .OrderByDescending(x => x.FailureCount)
                .Select(x => ValueTuple.Create(x.IpAddress, x.AttemptCount, x.FailureCount))
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get suspicious IP addresses");
            return new List<(string, int, int)>();
        }
    }

    public async Task<(int TotalAttempts, int SuccessfulLogins, int FailedAttempts, decimal SuccessRate)> GetLoginSummaryAsync(
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        try
        {
            var query = _context.LoginAttempts.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(la => la.AttemptedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(la => la.AttemptedAt <= endDate.Value);

            var totalAttempts = await query.CountAsync();
            var successfulLogins = await query.CountAsync(la => la.IsSuccess);
            var failedAttempts = totalAttempts - successfulLogins;
            var successRate = totalAttempts > 0 ? (decimal)successfulLogins / totalAttempts * 100 : 0;

            return (totalAttempts, successfulLogins, failedAttempts, successRate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get login summary");
            return (0, 0, 0, 0);
        }
    }

    public async Task<decimal> CalculateRiskScoreAsync(
        string email,
        string ipAddress,
        string? userAgent = null,
        string? geolocationData = null)
    {
        try
        {
            decimal riskScore = 0.0m;

            // Check recent failed attempts from this IP
            var recentFailuresFromIp = await _context.LoginAttempts
                .CountAsync(la => la.IpAddress == ipAddress && 
                                 !la.IsSuccess && 
                                 la.AttemptedAt >= DateTime.UtcNow.AddHours(-1));

            riskScore += Math.Min(recentFailuresFromIp * 0.2m, 0.5m);

            // Check if this is a new IP for this user
            var isNewIp = await _context.LoginAttempts
                .Where(la => la.Email == email && la.IsSuccess)
                .AnyAsync(la => la.IpAddress == ipAddress);

            if (!isNewIp)
                riskScore += 0.3m;

            // Check for unusual time patterns (simplified - just check if outside business hours)
            var currentHour = DateTime.UtcNow.Hour;
            if (currentHour < 6 || currentHour > 22)
                riskScore += 0.1m;

            // Geolocation-based risk (simplified)
            if (!string.IsNullOrEmpty(geolocationData))
            {
                try
                {
                    var geoData = JsonSerializer.Deserialize<Dictionary<string, object>>(geolocationData);
                    if (geoData?.ContainsKey("country") == true)
                    {
                        var country = geoData["country"].ToString();
                        // Add risk for high-risk countries (this is a simplified example)
                        var highRiskCountries = new[] { "XX", "YY" }; // placeholder
                        if (highRiskCountries.Contains(country))
                            riskScore += 0.2m;
                    }
                }
                catch
                {
                    // Invalid geolocation data
                    riskScore += 0.1m;
                }
            }

            return Math.Min(riskScore, 1.0m);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate risk score for email {Email}", email);
            return 0.5m; // Default medium risk on error
        }
    }

    public async Task CleanupExpiredSessionsAsync(TimeSpan? sessionTimeout = null)
    {
        try
        {
            var timeout = sessionTimeout ?? TimeSpan.FromHours(24);
            var cutoffDate = DateTime.UtcNow.Subtract(timeout);

            var expiredSessions = await _context.UserSessions
                .Where(s => s.IsActive && s.LastActivityAt < cutoffDate)
                .ToListAsync();

            foreach (var session in expiredSessions)
            {
                session.IsActive = false;
                session.EndedAt = DateTime.UtcNow;
                session.EndReason = "Expired";
            }

            if (expiredSessions.Any())
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Cleaned up {Count} expired sessions", expiredSessions.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup expired sessions");
        }
    }

    public async Task ForceLogoutUserAsync(Guid userId, string reason = "AdminForced")
    {
        try
        {
            var activeSessions = await _context.UserSessions
                .Where(s => s.UserId == userId && s.IsActive)
                .ToListAsync();

            foreach (var session in activeSessions)
            {
                session.IsActive = false;
                session.EndedAt = DateTime.UtcNow;
                session.EndReason = reason;
            }

            if (activeSessions.Any())
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Force logged out user {UserId}, ended {Count} sessions", userId, activeSessions.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to force logout user {UserId}", userId);
            throw;
        }
    }

    public async Task RevokeTokenAsync(string jwtTokenId)
    {
        try
        {
            var sessions = await _context.UserSessions
                .Where(s => s.JwtTokenId == jwtTokenId && s.IsActive)
                .ToListAsync();

            foreach (var session in sessions)
            {
                session.IsActive = false;
                session.EndedAt = DateTime.UtcNow;
                session.EndReason = "TokenRevoked";
            }

            if (sessions.Any())
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Revoked token {TokenId}, ended {Count} sessions", jwtTokenId, sessions.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revoke token {TokenId}", jwtTokenId);
            throw;
        }
    }
}