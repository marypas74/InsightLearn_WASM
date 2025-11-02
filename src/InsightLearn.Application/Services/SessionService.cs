using InsightLearn.Application.Interfaces;
using InsightLearn.Core.Entities;
using InsightLearn.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InsightLearn.Application.Services;

public class SessionService : ISessionService
{
    private readonly IDbContextFactory<InsightLearnDbContext> _contextFactory;
    private readonly ILogger<SessionService> _logger;

    public SessionService(IDbContextFactory<InsightLearnDbContext> contextFactory, ILogger<SessionService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task<UserSession> CreateSessionAsync(Guid userId, string jwtToken, string sessionId, string ipAddress, string userAgent)
    {
        try
        {
            _logger.LogInformation("Creating new session for user {UserId} with sessionId {SessionId}", userId, sessionId);

            // End any existing active sessions for this user
            await EndAllUserSessionsAsync(userId, "NewLoginSession");

            // 🔥 CRITICAL FIX: Use DbContextFactory to prevent threading issues
            using var context = _contextFactory.CreateDbContext();

            var session = new UserSession
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                UserId = userId,
                JwtTokenId = ExtractTokenId(jwtToken),
                JwtToken = jwtToken, // Store full JWT token
                StartedAt = DateTime.UtcNow,
                LastActivityAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow, // Set UpdatedAt timestamp
                IpAddress = ipAddress,
                UserAgent = userAgent,
                IsActive = true
            };

            context.UserSessions.Add(session);
            await context.SaveChangesAsync();

            _logger.LogInformation("Session created successfully for user {UserId}", userId);
            return session;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating session for user {UserId}", userId);
            throw;
        }
    }

    public async Task<UserSession?> GetSessionByIdAsync(string sessionId)
    {
        try
        {
            // 🔥 CRITICAL FIX: Use DbContextFactory to prevent threading issues
            using var context = _contextFactory.CreateDbContext();
            return await context.UserSessions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.IsActive);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting session {SessionId}", sessionId);
            return null;
        }
    }

    public async Task<UserSession?> GetActiveSessionByUserIdAsync(Guid userId)
    {
        try
        {
            // 🔥 CRITICAL FIX: Use DbContextFactory to prevent threading issues
            using var context = _contextFactory.CreateDbContext();
            return await context.UserSessions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == userId && s.IsActive);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active session for user {UserId}", userId);
            return null;
        }
    }

    public async Task<bool> ValidateSessionAsync(string sessionId)
    {
        try
        {
            var session = await GetSessionByIdAsync(sessionId);
            if (session == null)
            {
                _logger.LogWarning("Session {SessionId} not found", sessionId);
                return false;
            }

            // Check if session is expired (7 days)
            if (session.LastActivityAt.AddDays(7) < DateTime.UtcNow)
            {
                _logger.LogInformation("Session {SessionId} expired, ending it", sessionId);
                await EndSessionAsync(sessionId, "Expired");
                return false;
            }

            // Update last activity
            await UpdateSessionActivityAsync(sessionId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating session {SessionId}", sessionId);
            return false;
        }
    }

    public async Task UpdateSessionActivityAsync(string sessionId)
    {
        try
        {
            // 🔥 CRITICAL FIX: Use DbContextFactory to prevent threading issues
            using var context = _contextFactory.CreateDbContext();
            var session = await context.UserSessions
                .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.IsActive);

            if (session != null)
            {
                session.LastActivityAt = DateTime.UtcNow;
                session.UpdatedAt = DateTime.UtcNow;
                session.ActivityCount++;
                await context.SaveChangesAsync();
                
                _logger.LogDebug("Updated activity for session {SessionId}", sessionId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating session activity {SessionId}", sessionId);
        }
    }

    public async Task EndSessionAsync(string sessionId, string reason)
    {
        try
        {
            // 🔥 CRITICAL FIX: Use DbContextFactory to prevent threading issues
            using var context = _contextFactory.CreateDbContext();
            var session = await context.UserSessions
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);

            if (session != null)
            {
                session.IsActive = false;
                session.EndedAt = DateTime.UtcNow;
                session.EndReason = reason;
                await context.SaveChangesAsync();

                _logger.LogInformation("Session {SessionId} ended with reason: {Reason}", sessionId, reason);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ending session {SessionId}", sessionId);
        }
    }

    public async Task EndAllUserSessionsAsync(Guid userId, string reason)
    {
        try
        {
            // 🔥 CRITICAL FIX: Use DbContextFactory to prevent threading issues during login
            using var context = _contextFactory.CreateDbContext();
            var activeSessions = await context.UserSessions
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
                await context.SaveChangesAsync();
                _logger.LogInformation("Ended {Count} active sessions for user {UserId}", activeSessions.Count, userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ending all sessions for user {UserId}", userId);
        }
    }

    public async Task<bool> IsSessionActiveAsync(string sessionId)
    {
        try
        {
            // 🔥 CRITICAL FIX: Use DbContextFactory to prevent threading issues
            using var context = _contextFactory.CreateDbContext();
            return await context.UserSessions
                .AnyAsync(s => s.SessionId == sessionId && s.IsActive);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if session is active {SessionId}", sessionId);
            return false;
        }
    }

    public async Task<string?> GetTokenFromSessionAsync(string sessionId)
    {
        try
        {
            // 🔥 CRITICAL FIX: Return actual JWT token from database column
            using var context = _contextFactory.CreateDbContext();
            var session = await context.UserSessions
                .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.IsActive);

            if (session == null)
            {
                _logger.LogWarning("No active session found for sessionId {SessionId}", sessionId);
                return null;
            }

            // Return the actual JWT token stored in the database
            if (string.IsNullOrEmpty(session.JwtToken))
            {
                _logger.LogWarning("Session {SessionId} exists but has no JWT token stored", sessionId);
                return null;
            }

            return session.JwtToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting token from session {SessionId}", sessionId);
            return null;
        }
    }

    private string ExtractTokenId(string jwtToken)
    {
        // Extract token ID from JWT (simplified)
        return jwtToken.GetHashCode().ToString();
    }

    // 🔥 IMPLEMENTATION: Legacy methods for backward compatibility
    public async Task InvalidateUserSessionsAsync(string userId)
    {
        try
        {
            if (Guid.TryParse(userId, out var userGuid))
            {
                await EndAllUserSessionsAsync(userGuid, "Invalidated");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating sessions for user {UserId}", userId);
        }
    }

    public async Task TrackSessionAsync(string userId, string sessionId)
    {
        try
        {
            // Update session activity if it exists
            await UpdateSessionActivityAsync(sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking session {SessionId} for user {UserId}", sessionId, userId);
        }
    }

    public async Task<UserSession?> GetActiveSessionForUserAsync(Guid userId)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            
            var session = await context.UserSessions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => 
                    s.UserId == userId && 
                    s.IsActive && 
                    s.EndedAt == null);
            
            _logger.LogInformation("Retrieved active session for user {UserId}: {Found}", userId, session != null);
            return session;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active session for user {UserId}", userId);
            return null;
        }
    }

    public async Task UpdateSessionJwtTokenAsync(Guid userId, string jwtToken)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            
            var session = await context.UserSessions
                .FirstOrDefaultAsync(s => 
                    s.UserId == userId && 
                    s.IsActive && 
                    s.EndedAt == null);
            
            if (session != null)
            {
                session.JwtToken = jwtToken;
                session.UpdatedAt = DateTime.UtcNow;
                
                await context.SaveChangesAsync();
                _logger.LogInformation("Updated JWT token for user {UserId} in session {SessionId}", userId, session.SessionId);
            }
            else
            {
                _logger.LogWarning("No active session found to update JWT token for user {UserId}", userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating JWT token for user {UserId}", userId);
        }
    }

    public async Task<bool> IsSessionValidAsync(string userId, string sessionId)
    {
        try
        {
            return await ValidateSessionAsync(sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating session {SessionId} for user {UserId}", sessionId, userId);
            return false;
        }
    }

    public async Task<IEnumerable<string>> GetActiveSessionsAsync(string userId)
    {
        try
        {
            if (Guid.TryParse(userId, out var userGuid))
            {
                using var context = _contextFactory.CreateDbContext();
                var sessions = await context.UserSessions
                    .Where(s => s.UserId == userGuid && s.IsActive)
                    .Select(s => s.SessionId)
                    .ToListAsync();
                return sessions;
            }
            return Enumerable.Empty<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active sessions for user {UserId}", userId);
            return Enumerable.Empty<string>();
        }
    }
}