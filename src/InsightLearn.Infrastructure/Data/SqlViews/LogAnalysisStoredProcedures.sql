-- ========================================
-- Stored Procedures for Log Analysis and Reporting
-- ========================================

-- 1. Get Error Summary for Date Range
CREATE PROCEDURE sp_GetErrorSummaryByDateRange
    @StartDate DATETIME = NULL,
    @EndDate DATETIME = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    IF @StartDate IS NULL SET @StartDate = DATEADD(day, -30, GETDATE())
    IF @EndDate IS NULL SET @EndDate = GETDATE()
    
    SELECT 
        COUNT(*) as TotalErrors,
        COUNT(CASE WHEN Severity = 'Critical' THEN 1 END) as CriticalErrors,
        COUNT(CASE WHEN Severity = 'Error' THEN 1 END) as Errors,
        COUNT(CASE WHEN Severity = 'Warning' THEN 1 END) as Warnings,
        COUNT(CASE WHEN IsResolved = 1 THEN 1 END) as ResolvedErrors,
        COUNT(CASE WHEN IsResolved = 0 THEN 1 END) as UnresolvedErrors,
        COUNT(DISTINCT UserId) as AffectedUsers,
        COUNT(DISTINCT Component) as AffectedComponents,
        COUNT(DISTINCT IpAddress) as UniqueIpAddresses
    FROM ErrorLogs
    WHERE LoggedAt BETWEEN @StartDate AND @EndDate;
END

-- 2. Get Login Security Analysis
CREATE PROCEDURE sp_GetLoginSecurityAnalysis
    @DaysBack INT = 7,
    @MinFailedAttempts INT = 5
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @StartDate DATETIME = DATEADD(day, -@DaysBack, GETDATE())
    
    -- Failed login analysis
    SELECT 
        'Failed Login Analysis' as AnalysisType,
        IpAddress,
        COUNT(*) as FailedAttempts,
        COUNT(DISTINCT Email) as UniqueEmails,
        MIN(AttemptedAt) as FirstAttempt,
        MAX(AttemptedAt) as LastAttempt,
        STRING_AGG(DISTINCT FailureReason, ', ') as FailureReasons
    FROM LoginAttempts
    WHERE IsSuccess = 0 
        AND AttemptedAt >= @StartDate
    GROUP BY IpAddress
    HAVING COUNT(*) >= @MinFailedAttempts
    ORDER BY COUNT(*) DESC;
    
    -- Suspicious IP analysis
    SELECT 
        'Suspicious IP Analysis' as AnalysisType,
        IpAddress,
        COUNT(*) as TotalAttempts,
        COUNT(CASE WHEN IsSuccess = 0 THEN 1 END) as FailedAttempts,
        COUNT(CASE WHEN IsSuccess = 1 THEN 1 END) as SuccessfulAttempts,
        CAST(AVG(CASE WHEN IsSuccess = 1 THEN 1.0 ELSE 0.0 END) * 100 AS DECIMAL(5,2)) as SuccessRate,
        COUNT(DISTINCT Email) as UniqueEmails
    FROM LoginAttempts
    WHERE AttemptedAt >= @StartDate
    GROUP BY IpAddress
    HAVING COUNT(CASE WHEN IsSuccess = 0 THEN 1 END) >= @MinFailedAttempts
    ORDER BY COUNT(CASE WHEN IsSuccess = 0 THEN 1 END) DESC;
END

-- 3. Get System Health Dashboard Data
CREATE PROCEDURE sp_GetSystemHealthDashboard
    @HoursBack INT = 24
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @StartDate DATETIME = DATEADD(hour, -@HoursBack, GETDATE())
    
    -- Error metrics
    SELECT 
        'Error Metrics' as MetricType,
        COUNT(*) as TotalErrors,
        COUNT(CASE WHEN Severity = 'Critical' THEN 1 END) as CriticalErrors,
        COUNT(CASE WHEN IsResolved = 0 THEN 1 END) as UnresolvedErrors,
        COUNT(DISTINCT Component) as AffectedComponents
    FROM ErrorLogs
    WHERE LoggedAt >= @StartDate;
    
    -- API performance metrics
    SELECT 
        'API Performance' as MetricType,
        COUNT(*) as TotalRequests,
        AVG(DurationMs) as AvgResponseTime,
        COUNT(CASE WHEN ResponseStatusCode >= 400 THEN 1 END) as ErrorRequests,
        COUNT(CASE WHEN ResponseStatusCode >= 500 THEN 1 END) as ServerErrors,
        MAX(DurationMs) as MaxResponseTime
    FROM ApiRequestLogs
    WHERE RequestedAt >= @StartDate;
    
    -- Login metrics
    SELECT 
        'Login Metrics' as MetricType,
        COUNT(*) as TotalAttempts,
        COUNT(CASE WHEN IsSuccess = 1 THEN 1 END) as SuccessfulLogins,
        COUNT(CASE WHEN IsSuccess = 0 THEN 1 END) as FailedAttempts,
        CAST(AVG(CASE WHEN IsSuccess = 1 THEN 1.0 ELSE 0.0 END) * 100 AS DECIMAL(5,2)) as SuccessRate
    FROM LoginAttempts
    WHERE AttemptedAt >= @StartDate;
    
    -- Security events
    SELECT 
        'Security Events' as MetricType,
        COUNT(*) as TotalEvents,
        COUNT(CASE WHEN Severity = 'Critical' THEN 1 END) as CriticalEvents,
        COUNT(CASE WHEN IsResolved = 0 THEN 1 END) as UnresolvedEvents,
        COUNT(CASE WHEN AutoBlocked = 1 THEN 1 END) as AutoBlockedEvents
    FROM SecurityEvents
    WHERE DetectedAt >= @StartDate;
END

-- 4. Get User Activity Analysis
CREATE PROCEDURE sp_GetUserActivityAnalysis
    @UserId UNIQUEIDENTIFIER = NULL,
    @DaysBack INT = 30
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @StartDate DATETIME = DATEADD(day, -@DaysBack, GETDATE())
    
    -- User login analysis
    SELECT 
        'Login Analysis' as AnalysisType,
        la.UserId,
        u.Email,
        u.UserName,
        COUNT(*) as TotalAttempts,
        COUNT(CASE WHEN la.IsSuccess = 1 THEN 1 END) as SuccessfulLogins,
        COUNT(CASE WHEN la.IsSuccess = 0 THEN 1 END) as FailedAttempts,
        MAX(la.AttemptedAt) as LastLoginAttempt,
        COUNT(DISTINCT la.IpAddress) as UniqueIpAddresses,
        COUNT(DISTINCT la.LoginMethod) as LoginMethods
    FROM LoginAttempts la
    LEFT JOIN AspNetUsers u ON la.UserId = u.Id
    WHERE (@UserId IS NULL OR la.UserId = @UserId)
        AND la.AttemptedAt >= @StartDate
    GROUP BY la.UserId, u.Email, u.UserName;
    
    -- User error analysis
    SELECT 
        'Error Analysis' as AnalysisType,
        el.UserId,
        u.Email,
        u.UserName,
        COUNT(*) as TotalErrors,
        COUNT(CASE WHEN el.Severity = 'Critical' THEN 1 END) as CriticalErrors,
        MAX(el.LoggedAt) as LastError,
        COUNT(DISTINCT el.Component) as AffectedComponents,
        COUNT(DISTINCT el.ExceptionType) as UniqueErrorTypes
    FROM ErrorLogs el
    LEFT JOIN AspNetUsers u ON el.UserId = u.Id
    WHERE (@UserId IS NULL OR el.UserId = @UserId)
        AND el.LoggedAt >= @StartDate
    GROUP BY el.UserId, u.Email, u.UserName;
    
    -- User session analysis
    SELECT 
        'Session Analysis' as AnalysisType,
        us.UserId,
        u.Email,
        u.UserName,
        COUNT(*) as TotalSessions,
        COUNT(CASE WHEN us.IsActive = 1 THEN 1 END) as ActiveSessions,
        AVG(DATEDIFF(minute, us.StartedAt, ISNULL(us.EndedAt, GETDATE()))) as AvgSessionMinutes,
        MAX(us.StartedAt) as LastSessionStart,
        COUNT(DISTINCT us.IpAddress) as UniqueIpAddresses
    FROM UserSessions us
    LEFT JOIN AspNetUsers u ON us.UserId = u.Id
    WHERE (@UserId IS NULL OR us.UserId = @UserId)
        AND us.StartedAt >= @StartDate
    GROUP BY us.UserId, u.Email, u.UserName;
END

-- 5. Get Component Performance Analysis
CREATE PROCEDURE sp_GetComponentPerformanceAnalysis
    @Component NVARCHAR(100) = NULL,
    @DaysBack INT = 7
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @StartDate DATETIME = DATEADD(day, -@DaysBack, GETDATE())
    
    -- Component error analysis
    SELECT 
        Component,
        COUNT(*) as TotalErrors,
        COUNT(CASE WHEN Severity = 'Critical' THEN 1 END) as CriticalErrors,
        COUNT(CASE WHEN Severity = 'Error' THEN 1 END) as Errors,
        COUNT(CASE WHEN Severity = 'Warning' THEN 1 END) as Warnings,
        COUNT(DISTINCT ExceptionType) as UniqueErrorTypes,
        MAX(LoggedAt) as LastError,
        AVG(DATEDIFF(minute, LoggedAt, ISNULL(ResolvedAt, GETDATE()))) as AvgResolutionMinutes,
        COUNT(CASE WHEN IsResolved = 1 THEN 1 END) as ResolvedCount,
        COUNT(CASE WHEN IsResolved = 0 THEN 1 END) as UnresolvedCount
    FROM ErrorLogs
    WHERE (@Component IS NULL OR Component = @Component)
        AND LoggedAt >= @StartDate
    GROUP BY Component
    ORDER BY COUNT(*) DESC;
    
    -- Most frequent error types by component
    SELECT 
        Component,
        ExceptionType,
        COUNT(*) as ErrorCount,
        MAX(LoggedAt) as LastOccurrence,
        COUNT(CASE WHEN IsResolved = 1 THEN 1 END) as ResolvedCount
    FROM ErrorLogs
    WHERE (@Component IS NULL OR Component = @Component)
        AND LoggedAt >= @StartDate
    GROUP BY Component, ExceptionType
    ORDER BY Component, COUNT(*) DESC;
END

-- 6. Get Real-time Alerts
CREATE PROCEDURE sp_GetRealTimeAlerts
    @MinutesBack INT = 60
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @StartDate DATETIME = DATEADD(minute, -@MinutesBack, GETDATE())
    
    -- Critical errors requiring immediate attention
    SELECT 
        'Critical Error Alert' as AlertType,
        Id,
        ExceptionType,
        ExceptionMessage,
        Component,
        UserId,
        Email,
        IpAddress,
        LoggedAt,
        RequestPath,
        'High' as Priority
    FROM ErrorLogs
    WHERE Severity = 'Critical' 
        AND LoggedAt >= @StartDate
        AND IsResolved = 0
        AND NotificationSent = 0
    ORDER BY LoggedAt DESC;
    
    -- High frequency failed logins
    SELECT 
        'Failed Login Alert' as AlertType,
        IpAddress,
        COUNT(*) as FailedAttempts,
        MAX(AttemptedAt) as LastAttempt,
        STRING_AGG(DISTINCT Email, ', ') as TargetEmails,
        'Medium' as Priority
    FROM LoginAttempts
    WHERE IsSuccess = 0 
        AND AttemptedAt >= @StartDate
    GROUP BY IpAddress
    HAVING COUNT(*) >= 10
    ORDER BY COUNT(*) DESC;
    
    -- High error rate components
    SELECT 
        'Component Error Alert' as AlertType,
        Component,
        COUNT(*) as ErrorCount,
        MAX(LoggedAt) as LastError,
        COUNT(DISTINCT ExceptionType) as ErrorTypes,
        'Medium' as Priority
    FROM ErrorLogs
    WHERE LoggedAt >= @StartDate
        AND Component IS NOT NULL
    GROUP BY Component
    HAVING COUNT(*) >= 20
    ORDER BY COUNT(*) DESC;
END

-- 7. Clean up old logs
CREATE PROCEDURE sp_CleanupOldLogs
    @RetentionDays INT = 90,
    @BatchSize INT = 1000
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @CutoffDate DATETIME = DATEADD(day, -@RetentionDays, GETDATE())
    DECLARE @DeletedCount INT
    
    -- Clean up resolved error logs
    WHILE (1=1)
    BEGIN
        DELETE TOP (@BatchSize) FROM ErrorLogs
        WHERE LoggedAt < @CutoffDate 
            AND IsResolved = 1
            
        SET @DeletedCount = @@ROWCOUNT
        IF @DeletedCount = 0 BREAK
        
        WAITFOR DELAY '00:00:01' -- Small delay between batches
    END
    
    -- Clean up old API request logs
    WHILE (1=1)
    BEGIN
        DELETE TOP (@BatchSize) FROM ApiRequestLogs
        WHERE RequestedAt < @CutoffDate
            
        SET @DeletedCount = @@ROWCOUNT
        IF @DeletedCount = 0 BREAK
        
        WAITFOR DELAY '00:00:01'
    END
    
    -- Clean up old successful login attempts
    WHILE (1=1)
    BEGIN
        DELETE TOP (@BatchSize) FROM LoginAttempts
        WHERE AttemptedAt < @CutoffDate 
            AND IsSuccess = 1
            
        SET @DeletedCount = @@ROWCOUNT
        IF @DeletedCount = 0 BREAK
        
        WAITFOR DELAY '00:00:01'
    END
    
    -- Clean up old inactive sessions
    WHILE (1=1)
    BEGIN
        DELETE TOP (@BatchSize) FROM UserSessions
        WHERE StartedAt < @CutoffDate 
            AND IsActive = 0
            
        SET @DeletedCount = @@ROWCOUNT
        IF @DeletedCount = 0 BREAK
        
        WAITFOR DELAY '00:00:01'
    END
    
    SELECT 'Cleanup completed successfully' as Result
END