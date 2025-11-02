using InsightLearn.Application.Services;

namespace InsightLearn.Application.Interfaces;

/// <summary>
/// Interface for user lockout management service
/// </summary>
public interface IUserLockoutService
{
    /// <summary>
    /// Processes a failed login attempt and applies automatic lockout if necessary
    /// </summary>
    Task<bool> ProcessFailedLoginAsync(string email, string ipAddress);

    /// <summary>
    /// Checks if a user is currently locked out
    /// </summary>
    Task<bool> IsUserLockedOutAsync(string email);

    /// <summary>
    /// Gets the lockout information for a user
    /// </summary>
    Task<UserLockoutInfo> GetLockoutInfoAsync(string email);

    /// <summary>
    /// Manually locks a user account (admin function)
    /// </summary>
    Task<bool> LockUserAsync(Guid userId, DateTimeOffset lockoutEnd, string reason, Guid adminUserId);

    /// <summary>
    /// Manually unlocks a user account (admin function)
    /// </summary>
    Task<bool> UnlockUserAsync(Guid userId, string reason, Guid adminUserId);

    /// <summary>
    /// Resets failed login attempts count (typically called after successful login)
    /// </summary>
    Task ResetFailedAttemptsAsync(string email);

    /// <summary>
    /// Gets lockout statistics for admin dashboard
    /// </summary>
    Task<LockoutStatistics> GetLockoutStatisticsAsync(DateTime? fromDate = null);
}