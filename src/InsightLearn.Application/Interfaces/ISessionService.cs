using System;
using System.Threading.Tasks;
using InsightLearn.Core.Entities;

namespace InsightLearn.Application.Interfaces
{
    public interface ISessionService
    {
        /// <summary>
        /// 🔥 Create new user session with JWT token
        /// </summary>
        Task<UserSession> CreateSessionAsync(Guid userId, string jwtToken, string sessionId, string ipAddress, string userAgent);

        /// <summary>
        /// 🔥 Get session by session ID
        /// </summary>
        Task<UserSession?> GetSessionByIdAsync(string sessionId);

        /// <summary>
        /// 🔥 Get active session by user ID
        /// </summary>
        Task<UserSession?> GetActiveSessionByUserIdAsync(Guid userId);

        /// <summary>
        /// 🔥 Validate if session is active and not expired
        /// </summary>
        Task<bool> ValidateSessionAsync(string sessionId);

        /// <summary>
        /// 🔥 Update session activity timestamp
        /// </summary>
        Task UpdateSessionActivityAsync(string sessionId);

        /// <summary>
        /// 🔥 End specific session
        /// </summary>
        Task EndSessionAsync(string sessionId, string reason);

        /// <summary>
        /// 🔥 End all active sessions for a user
        /// </summary>
        Task EndAllUserSessionsAsync(Guid userId, string reason);

        /// <summary>
        /// 🔥 Check if session is active
        /// </summary>
        Task<bool> IsSessionActiveAsync(string sessionId);

        /// <summary>
        /// 🔥 Get active session for user (for JWT token retrieval)
        /// </summary>
        Task<UserSession?> GetActiveSessionForUserAsync(Guid userId);

        /// <summary>
        /// 🔥 Update JWT token in user's active session
        /// </summary>
        Task UpdateSessionJwtTokenAsync(Guid userId, string jwtToken);

        /// <summary>
        /// 🔥 Get JWT token from session
        /// </summary>
        Task<string?> GetTokenFromSessionAsync(string sessionId);

        /// <summary>
        /// Invalidates all active sessions for a specific user (legacy)
        /// </summary>
        /// <param name="userId">User identifier to invalidate sessions for</param>
        /// <returns>Task representing the invalidation operation</returns>
        Task InvalidateUserSessionsAsync(string userId);

        /// <summary>
        /// Tracks user session creation and provides diagnostic information (legacy)
        /// </summary>
        /// <param name="userId">User identifier</param>
        /// <param name="sessionId">Unique session identifier</param>
        /// <returns>Task representing session tracking</returns>
        Task TrackSessionAsync(string userId, string sessionId);

        /// <summary>
        /// Checks if a specific session is still valid (legacy)
        /// </summary>
        /// <param name="userId">User identifier</param>
        /// <param name="sessionId">Session identifier to validate</param>
        /// <returns>Boolean indicating session validity</returns>
        Task<bool> IsSessionValidAsync(string userId, string sessionId);

        /// <summary>
        /// Retrieves active sessions for a user (legacy)
        /// </summary>
        /// <param name="userId">User identifier</param>
        /// <returns>List of active session identifiers</returns>
        Task<IEnumerable<string>> GetActiveSessionsAsync(string userId);
    }
}