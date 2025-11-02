using InsightLearn.Core.Entities;

namespace InsightLearn.Application.Interfaces;

public interface ILoginTrackingService
{
    // Login attempt tracking
    Task<Guid> LogLoginAttemptAsync(
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
        string? correlationId = null);

    // Session management
    Task<Guid> StartSessionAsync(
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
        string? timeZone = null);

    Task UpdateSessionActivityAsync(
        string sessionId,
        string? lastPageVisited = null,
        int? additionalActivityCount = null,
        long? additionalDataTransferred = null);

    Task EndSessionAsync(
        string sessionId,
        string endReason = "Logout");

    Task<UserSession?> GetActiveSessionAsync(string sessionId);
    Task<List<UserSession>> GetActiveUserSessionsAsync(Guid userId);

    // Login method tracking
    Task TrackLoginMethodAsync(
        Guid userId,
        string methodType,
        bool wasSuccessful,
        string? providerUserId = null,
        string? providerName = null,
        string? providerAccountEmail = null,
        string? metadataJson = null);

    Task<List<LoginMethod>> GetUserLoginMethodsAsync(Guid userId);

    // Analytics and reporting
    Task<List<LoginAttempt>> GetRecentLoginAttemptsAsync(
        Guid? userId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        bool? successOnly = null,
        int limit = 100);

    Task<Dictionary<string, int>> GetLoginStatsByMethodAsync(
        DateTime? startDate = null,
        DateTime? endDate = null);

    Task<List<(string IpAddress, int AttemptCount, int FailureCount)>> GetSuspiciousIpAddressesAsync(
        int minFailureCount = 5,
        TimeSpan? timeWindow = null);

    Task<(int TotalAttempts, int SuccessfulLogins, int FailedAttempts, decimal SuccessRate)> GetLoginSummaryAsync(
        DateTime? startDate = null,
        DateTime? endDate = null);

    // Risk assessment
    Task<decimal> CalculateRiskScoreAsync(
        string email,
        string ipAddress,
        string? userAgent = null,
        string? geolocationData = null);

    // Session cleanup
    Task CleanupExpiredSessionsAsync(TimeSpan? sessionTimeout = null);
    Task ForceLogoutUserAsync(Guid userId, string reason = "AdminForced");
    Task RevokeTokenAsync(string jwtTokenId);
}