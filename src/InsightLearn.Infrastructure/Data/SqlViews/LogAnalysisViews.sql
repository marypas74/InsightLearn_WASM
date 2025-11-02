-- ========================================
-- SQL Views for Log Analysis and Reporting
-- ========================================

-- 1. Daily Error Summary View
CREATE VIEW vw_DailyErrorSummary AS
SELECT 
    CAST(LoggedAt AS DATE) as ErrorDate,
    COUNT(*) as TotalErrors,
    COUNT(CASE WHEN Severity = 'Critical' THEN 1 END) as CriticalErrors,
    COUNT(CASE WHEN Severity = 'Error' THEN 1 END) as Errors,
    COUNT(CASE WHEN Severity = 'Warning' THEN 1 END) as Warnings,
    COUNT(CASE WHEN IsResolved = 1 THEN 1 END) as ResolvedErrors,
    COUNT(CASE WHEN IsResolved = 0 THEN 1 END) as UnresolvedErrors,
    COUNT(DISTINCT UserId) as AffectedUsers,
    COUNT(DISTINCT IpAddress) as UniqueIpAddresses
FROM ErrorLogs
GROUP BY CAST(LoggedAt AS DATE);

-- 2. Top Error Types View
CREATE VIEW vw_TopErrorTypes AS
SELECT 
    ExceptionType,
    COUNT(*) as ErrorCount,
    COUNT(CASE WHEN Severity = 'Critical' THEN 1 END) as CriticalCount,
    MAX(LoggedAt) as LastOccurrence,
    MIN(LoggedAt) as FirstOccurrence,
    COUNT(DISTINCT UserId) as AffectedUsers,
    CAST(AVG(CASE WHEN IsResolved = 1 THEN 1.0 ELSE 0.0 END) * 100 AS DECIMAL(5,2)) as ResolutionRate
FROM ErrorLogs
GROUP BY ExceptionType;

-- 3. User Error Analysis View
CREATE VIEW vw_UserErrorAnalysis AS
SELECT 
    el.UserId,
    u.Email as UserEmail,
    u.UserName,
    COUNT(*) as TotalErrors,
    COUNT(CASE WHEN el.Severity = 'Critical' THEN 1 END) as CriticalErrors,
    MAX(el.LoggedAt) as LastErrorDate,
    COUNT(DISTINCT el.ExceptionType) as UniqueErrorTypes,
    COUNT(DISTINCT el.Component) as AffectedComponents
FROM ErrorLogs el
LEFT JOIN AspNetUsers u ON el.UserId = u.Id
WHERE el.UserId IS NOT NULL
GROUP BY el.UserId, u.Email, u.UserName;

-- 4. Component Error Summary View
CREATE VIEW vw_ComponentErrorSummary AS
SELECT 
    Component,
    COUNT(*) as TotalErrors,
    COUNT(CASE WHEN Severity = 'Critical' THEN 1 END) as CriticalErrors,
    COUNT(DISTINCT ExceptionType) as UniqueErrorTypes,
    MAX(LoggedAt) as LastErrorDate,
    AVG(DATEDIFF(minute, LoggedAt, ISNULL(ResolvedAt, GETDATE()))) as AvgResolutionTimeMinutes,
    COUNT(CASE WHEN IsResolved = 1 THEN 1 END) as ResolvedCount,
    COUNT(CASE WHEN IsResolved = 0 THEN 1 END) as UnresolvedCount
FROM ErrorLogs
WHERE Component IS NOT NULL
GROUP BY Component;

-- 5. Login Attempt Analysis View
CREATE VIEW vw_LoginAttemptAnalysis AS
SELECT 
    CAST(AttemptedAt AS DATE) as LoginDate,
    COUNT(*) as TotalAttempts,
    COUNT(CASE WHEN IsSuccess = 1 THEN 1 END) as SuccessfulLogins,
    COUNT(CASE WHEN IsSuccess = 0 THEN 1 END) as FailedAttempts,
    CAST(AVG(CASE WHEN IsSuccess = 1 THEN 1.0 ELSE 0.0 END) * 100 AS DECIMAL(5,2)) as SuccessRate,
    COUNT(DISTINCT Email) as UniqueEmails,
    COUNT(DISTINCT IpAddress) as UniqueIpAddresses,
    COUNT(DISTINCT CASE WHEN IsSuccess = 1 THEN UserId END) as UniqueSuccessfulUsers
FROM LoginAttempts
GROUP BY CAST(AttemptedAt AS DATE);

-- 6. Security Events Summary View
CREATE VIEW vw_SecurityEventsSummary AS
SELECT 
    EventType,
    Severity,
    COUNT(*) as EventCount,
    COUNT(CASE WHEN IsResolved = 1 THEN 1 END) as ResolvedCount,
    COUNT(CASE WHEN AutoBlocked = 1 THEN 1 END) as AutoBlockedCount,
    MAX(DetectedAt) as LastDetection,
    AVG(RiskScore) as AvgRiskScore,
    COUNT(DISTINCT UserId) as AffectedUsers,
    COUNT(DISTINCT IpAddress) as UniqueIpAddresses
FROM SecurityEvents
GROUP BY EventType, Severity;

-- 7. API Request Performance View
CREATE VIEW vw_ApiRequestPerformance AS
SELECT 
    Path,
    Method,
    COUNT(*) as RequestCount,
    AVG(DurationMs) as AvgDurationMs,
    MIN(DurationMs) as MinDurationMs,
    MAX(DurationMs) as MaxDurationMs,
    COUNT(CASE WHEN ResponseStatusCode >= 400 THEN 1 END) as ErrorCount,
    COUNT(CASE WHEN ResponseStatusCode >= 500 THEN 1 END) as ServerErrorCount,
    CAST(AVG(CASE WHEN ResponseStatusCode < 400 THEN 1.0 ELSE 0.0 END) * 100 AS DECIMAL(5,2)) as SuccessRate,
    AVG(DatabaseQueries) as AvgDbQueries,
    AVG(DatabaseDurationMs) as AvgDbDurationMs
FROM ApiRequestLogs
GROUP BY Path, Method;

-- 8. Hourly Error Trends View
CREATE VIEW vw_HourlyErrorTrends AS
SELECT 
    DATEPART(hour, LoggedAt) as HourOfDay,
    COUNT(*) as TotalErrors,
    COUNT(CASE WHEN Severity = 'Critical' THEN 1 END) as CriticalErrors,
    COUNT(DISTINCT Component) as AffectedComponents,
    COUNT(DISTINCT UserId) as AffectedUsers
FROM ErrorLogs
WHERE LoggedAt >= DATEADD(day, -7, GETDATE())
GROUP BY DATEPART(hour, LoggedAt);

-- 9. Failed Login Patterns View
CREATE VIEW vw_FailedLoginPatterns AS
SELECT 
    IpAddress,
    COUNT(*) as FailedAttempts,
    COUNT(DISTINCT Email) as UniqueEmails,
    MIN(AttemptedAt) as FirstAttempt,
    MAX(AttemptedAt) as LastAttempt,
    STRING_AGG(DISTINCT FailureReason, ', ') as FailureReasons,
    COUNT(DISTINCT UserAgent) as UniqueUserAgents
FROM LoginAttempts
WHERE IsSuccess = 0
GROUP BY IpAddress
HAVING COUNT(*) > 3;

-- 10. Session Analytics View
CREATE VIEW vw_SessionAnalytics AS
SELECT 
    CAST(StartedAt AS DATE) as SessionDate,
    COUNT(*) as TotalSessions,
    COUNT(CASE WHEN IsActive = 1 THEN 1 END) as ActiveSessions,
    AVG(DATEDIFF(minute, StartedAt, ISNULL(EndedAt, GETDATE()))) as AvgSessionDurationMinutes,
    COUNT(DISTINCT UserId) as UniqueUsers,
    COUNT(DISTINCT IpAddress) as UniqueIpAddresses,
    COUNT(DISTINCT DeviceType) as UniqueDeviceTypes,
    AVG(ActivityCount) as AvgActivityCount
FROM UserSessions
GROUP BY CAST(StartedAt AS DATE);

-- 11. Real-time Error Monitoring View
CREATE VIEW vw_RecentCriticalErrors AS
SELECT 
    Id,
    ExceptionType,
    ExceptionMessage,
    Component,
    UserId,
    Email,
    IpAddress,
    LoggedAt,
    RequestPath,
    IsResolved,
    NotificationSent
FROM ErrorLogs
WHERE Severity = 'Critical' 
    AND LoggedAt >= DATEADD(hour, -24, GETDATE())
    AND IsResolved = 0;

-- 12. Cross-System Correlation View
CREATE VIEW vw_SystemCorrelationAnalysis AS
SELECT 
    el.CorrelationId,
    COUNT(DISTINCT el.Id) as ErrorCount,
    COUNT(DISTINCT la.Id) as LoginAttemptCount,
    COUNT(DISTINCT se.Id) as SecurityEventCount,
    COUNT(DISTINCT ar.Id) as ApiRequestCount,
    MAX(el.LoggedAt) as LastErrorTime,
    MAX(la.AttemptedAt) as LastLoginTime,
    MAX(se.DetectedAt) as LastSecurityEventTime,
    MAX(ar.RequestedAt) as LastApiRequestTime
FROM ErrorLogs el
LEFT JOIN LoginAttempts la ON el.CorrelationId = la.CorrelationId
LEFT JOIN SecurityEvents se ON el.CorrelationId = se.CorrelationId
LEFT JOIN ApiRequestLogs ar ON el.CorrelationId = ar.CorrelationId
WHERE el.CorrelationId IS NOT NULL
GROUP BY el.CorrelationId;