using InsightLearn.Core.Entities;

namespace InsightLearn.Application.Interfaces;

public interface ISecurityMonitoringService
{
    // Security event logging
    Task<Guid> LogSecurityEventAsync(
        string eventType,
        string severity,
        string eventDetails,
        string ipAddress,
        Guid? userId = null,
        string? email = null,
        string? userAgent = null,
        decimal riskScore = 0.00m,
        string? geolocationData = null,
        string? relatedSessionId = null,
        Guid? relatedLoginAttemptId = null,
        bool autoBlock = false,
        DateTime? blockedUntil = null,
        string? correlationId = null);

    // Threat detection
    Task DetectBruteForceAttacksAsync(string? ipAddress = null, string? email = null);
    Task DetectSuspiciousLoginPatternsAsync(Guid userId);
    Task DetectAccountTakeoverAttemptsAsync(Guid userId);
    Task<bool> IsIpAddressBlockedAsync(string ipAddress);
    Task<bool> IsUserAccountCompromisedAsync(Guid userId);

    // Risk assessment
    Task<decimal> AssessLoginRiskAsync(
        string email,
        string ipAddress,
        string? userAgent = null,
        string? geolocationData = null,
        string? deviceFingerprint = null);

    Task<decimal> AssessTransactionRiskAsync(
        Guid userId,
        decimal amount,
        string transactionType,
        string? ipAddress = null);

    // Automated responses
    Task AutoBlockIpAddressAsync(string ipAddress, TimeSpan blockDuration, string reason);
    Task AutoSuspendUserAccountAsync(Guid userId, string reason);
    Task SendSecurityAlertAsync(Guid userId, string alertType, string details);

    // Security event management
    Task<SecurityEvent?> GetSecurityEventAsync(Guid eventId);
    Task<List<SecurityEvent>> GetSecurityEventsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? eventType = null,
        string? severity = null,
        bool? resolvedOnly = null,
        int limit = 100);

    Task ResolveSecurityEventAsync(
        Guid eventId,
        Guid resolvedByUserId,
        string resolutionNotes);

    // Analytics and reporting
    Task<Dictionary<string, int>> GetSecurityEventsByTypeAsync(
        DateTime? startDate = null,
        DateTime? endDate = null);

    Task<List<(string IpAddress, int EventCount, decimal MaxRiskScore)>> GetHighRiskIpAddressesAsync(
        decimal minRiskScore = 0.7m,
        int limit = 50);

    Task<List<(Guid UserId, string Email, int EventCount, decimal MaxRiskScore)>> GetHighRiskUsersAsync(
        decimal minRiskScore = 0.7m,
        int limit = 50);

    // Configuration and rules
    Task<bool> UpdateThreatDetectionRuleAsync(string ruleName, string ruleConfig);
    Task<Dictionary<string, string>> GetThreatDetectionRulesAsync();

    // Real-time monitoring
    Task StartRealTimeMonitoringAsync();
    Task StopRealTimeMonitoringAsync();
    Task<List<SecurityEvent>> GetRealtimeSecurityAlertsAsync();

    // Cleanup and maintenance
    Task CleanupOldSecurityEventsAsync(int retentionDays = 90);
    Task ArchiveResolvedEventsAsync(int archiveAfterDays = 30);
}