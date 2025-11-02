using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using InsightLearn.Application.Interfaces;
using InsightLearn.Core.Entities;
using InsightLearn.Infrastructure.Data;

namespace InsightLearn.Application.Services;

/// <summary>
/// Service for managing user lockout functionality including automatic lockout after failed login attempts
/// </summary>
public class UserLockoutService : IUserLockoutService
{
    private readonly IDbContextFactory<InsightLearnDbContext> _contextFactory;
    private readonly UserManager<User> _userManager;
    private readonly ISessionService _sessionService;
    private readonly ILogger<UserLockoutService> _logger;
    
    // Configuration constants
    private const int MAX_FAILED_ATTEMPTS = 5;
    private const int LOCKOUT_DURATION_MINUTES = 30;
    private const int LOCKOUT_CHECK_WINDOW_MINUTES = 15; // Time window to check for failed attempts

    public UserLockoutService(
        IDbContextFactory<InsightLearnDbContext> contextFactory,
        UserManager<User> userManager,
        ISessionService sessionService,
        ILogger<UserLockoutService> logger)
    {
        _contextFactory = contextFactory;
        _userManager = userManager;
        _sessionService = sessionService;
        _logger = logger;
    }

    /// <summary>
    /// Processes a failed login attempt and applies automatic lockout if necessary
    /// </summary>
    public async Task<bool> ProcessFailedLoginAsync(string email, string ipAddress)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                _logger.LogWarning("Failed login attempt for non-existent user: {Email} from IP: {IP}", email, ipAddress);
                return false;
            }

            using var context = _contextFactory.CreateDbContext();
            
            // Check recent failed attempts within the time window
            var cutoffTime = DateTime.UtcNow.AddMinutes(-LOCKOUT_CHECK_WINDOW_MINUTES);
            var recentFailedAttempts = await context.LoginAttempts
                .CountAsync(la => la.Email == email && 
                           !la.IsSuccess && 
                           la.AttemptedAt >= cutoffTime);

            _logger.LogInformation("User {Email} has {Count} recent failed attempts", email, recentFailedAttempts);

            // Check if we should apply lockout
            if (recentFailedAttempts >= MAX_FAILED_ATTEMPTS - 1) // -1 because current attempt hasn't been logged yet
            {
                var lockoutEnd = DateTimeOffset.UtcNow.AddMinutes(LOCKOUT_DURATION_MINUTES);
                var result = await _userManager.SetLockoutEndDateAsync(user, lockoutEnd);
                
                if (result.Succeeded)
                {
                    // Enable lockout for this user
                    await _userManager.SetLockoutEnabledAsync(user, true);
                    
                    // End all active sessions for this user
                    await _sessionService.EndAllUserSessionsAsync(user.Id, "AccountLocked");
                    
                    _logger.LogWarning("User {Email} locked out until {LockoutEnd} after {Count} failed attempts", 
                        email, lockoutEnd, recentFailedAttempts + 1);

                    // Log security event
                    await LogSecurityEventAsync(user.Id, "AccountLocked", 
                        $"User locked after {recentFailedAttempts + 1} failed login attempts from IP: {ipAddress}");
                    
                    return true; // User has been locked
                }
                else
                {
                    _logger.LogError("Failed to set lockout for user {Email}: {Errors}", 
                        email, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }

            return false; // User not locked
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing failed login for user {Email}", email);
            return false;
        }
    }

    /// <summary>
    /// Checks if a user is currently locked out
    /// </summary>
    public async Task<bool> IsUserLockedOutAsync(string email)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return false;

            var isLockedOut = await _userManager.IsLockedOutAsync(user);
            
            if (isLockedOut)
            {
                _logger.LogInformation("User {Email} is currently locked out until {LockoutEnd}", 
                    email, user.LockoutEnd);
            }

            return isLockedOut;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking lockout status for user {Email}", email);
            return false;
        }
    }

    /// <summary>
    /// Gets the lockout information for a user
    /// </summary>
    public async Task<UserLockoutInfo> GetLockoutInfoAsync(string email)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return new UserLockoutInfo { UserExists = false };
            }

            var isLockedOut = await _userManager.IsLockedOutAsync(user);
            
            using var context = _contextFactory.CreateDbContext();
            var cutoffTime = DateTime.UtcNow.AddMinutes(-LOCKOUT_CHECK_WINDOW_MINUTES);
            var recentFailedAttempts = await context.LoginAttempts
                .CountAsync(la => la.Email == email && 
                           !la.IsSuccess && 
                           la.AttemptedAt >= cutoffTime);

            return new UserLockoutInfo
            {
                UserExists = true,
                IsLockedOut = isLockedOut,
                LockoutEnd = user.LockoutEnd,
                RecentFailedAttempts = recentFailedAttempts,
                RemainingAttempts = Math.Max(0, MAX_FAILED_ATTEMPTS - recentFailedAttempts)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting lockout info for user {Email}", email);
            return new UserLockoutInfo { UserExists = false, HasError = true };
        }
    }

    /// <summary>
    /// Manually locks a user account (admin function)
    /// </summary>
    public async Task<bool> LockUserAsync(Guid userId, DateTimeOffset lockoutEnd, string reason, Guid adminUserId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                _logger.LogWarning("Attempt to lock non-existent user {UserId}", userId);
                return false;
            }

            var result = await _userManager.SetLockoutEndDateAsync(user, lockoutEnd);
            if (result.Succeeded)
            {
                await _userManager.SetLockoutEnabledAsync(user, true);
                
                // End all active sessions
                await _sessionService.EndAllUserSessionsAsync(userId, "ManuallyLocked");
                
                _logger.LogWarning("User {UserId} manually locked by admin {AdminId} until {LockoutEnd}. Reason: {Reason}", 
                    userId, adminUserId, lockoutEnd, reason);

                await LogSecurityEventAsync(userId, "ManualLock", 
                    $"Manually locked by admin {adminUserId}. Reason: {reason}");
                
                return true;
            }

            _logger.LogError("Failed to manually lock user {UserId}: {Errors}", 
                userId, string.Join(", ", result.Errors.Select(e => e.Description)));
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error manually locking user {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// Manually unlocks a user account (admin function)
    /// </summary>
    public async Task<bool> UnlockUserAsync(Guid userId, string reason, Guid adminUserId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                _logger.LogWarning("Attempt to unlock non-existent user {UserId}", userId);
                return false;
            }

            var result = await _userManager.SetLockoutEndDateAsync(user, null);
            if (result.Succeeded)
            {
                // Reset failed access attempts
                await _userManager.ResetAccessFailedCountAsync(user);
                
                _logger.LogInformation("User {UserId} manually unlocked by admin {AdminId}. Reason: {Reason}", 
                    userId, adminUserId, reason);

                await LogSecurityEventAsync(userId, "ManualUnlock", 
                    $"Manually unlocked by admin {adminUserId}. Reason: {reason}");
                
                return true;
            }

            _logger.LogError("Failed to manually unlock user {UserId}: {Errors}", 
                userId, string.Join(", ", result.Errors.Select(e => e.Description)));
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error manually unlocking user {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// Resets failed login attempts count (typically called after successful login)
    /// </summary>
    public async Task ResetFailedAttemptsAsync(string email)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user != null)
            {
                await _userManager.ResetAccessFailedCountAsync(user);
                _logger.LogInformation("Reset failed attempts count for user {Email}", email);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting failed attempts for user {Email}", email);
        }
    }

    /// <summary>
    /// Gets lockout statistics for admin dashboard
    /// </summary>
    public async Task<LockoutStatistics> GetLockoutStatisticsAsync(DateTime? fromDate = null)
    {
        try
        {
            fromDate ??= DateTime.UtcNow.AddDays(-30); // Default to last 30 days

            using var context = _contextFactory.CreateDbContext();
            
            var currentlyLockedUsers = await context.Users
                .CountAsync(u => u.LockoutEnd != null && u.LockoutEnd > DateTimeOffset.UtcNow);

            var securityEvents = await context.SecurityEvents
                .Where(se => se.DetectedAt >= fromDate)
                .Where(se => se.EventType == "AccountLocked" || se.EventType == "ManualLock")
                .CountAsync();

            var failedAttempts = await context.LoginAttempts
                .Where(la => !la.IsSuccess && la.AttemptedAt >= fromDate)
                .CountAsync();

            return new LockoutStatistics
            {
                CurrentlyLockedUsers = currentlyLockedUsers,
                LockoutEventsLast30Days = securityEvents,
                FailedLoginAttemptsLast30Days = failedAttempts,
                FromDate = fromDate.Value
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting lockout statistics");
            return new LockoutStatistics();
        }
    }

    private async Task LogSecurityEventAsync(Guid userId, string eventType, string description)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            var securityEvent = new SecurityEvent
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                EventType = eventType,
                EventDetails = description,
                DetectedAt = DateTime.UtcNow,
                IpAddress = "System",
                UserAgent = "UserLockoutService"
            };

            context.SecurityEvents.Add(securityEvent);
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging security event {EventType} for user {UserId}", eventType, userId);
        }
    }
}

/// <summary>
/// Information about a user's lockout status
/// </summary>
public class UserLockoutInfo
{
    public bool UserExists { get; set; }
    public bool IsLockedOut { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public int RecentFailedAttempts { get; set; }
    public int RemainingAttempts { get; set; }
    public bool HasError { get; set; }
}

/// <summary>
/// Statistics about lockout events
/// </summary>
public class LockoutStatistics
{
    public int CurrentlyLockedUsers { get; set; }
    public int LockoutEventsLast30Days { get; set; }
    public int FailedLoginAttemptsLast30Days { get; set; }
    public DateTime FromDate { get; set; }
}