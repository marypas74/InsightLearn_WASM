namespace InsightLearn.Shared.DTOs;

public class LoginAnalyticsDto
{
    public int TotalAttempts { get; set; }
    public int SuccessfulLogins { get; set; }
    public int FailedAttempts { get; set; }  // Changed from FailedLogins to match AnalyticsService
    public decimal SuccessRate { get; set; }  // Changed from double to decimal
    public int UniqueUsers { get; set; }  // Added for AnalyticsService
    public Dictionary<string, int> LoginsByMethod { get; set; } = new();
    public Dictionary<string, int> LoginsByDevice { get; set; } = new();  // Added for AnalyticsService
    public List<HourlyLoginDto> HourlyDistribution { get; set; } = new();  // Added for AnalyticsService
    public Dictionary<DateTime, int> LoginsByHour { get; set; } = new();  // Keep for backward compatibility
}

public class ErrorAnalyticsDto
{
    public int TotalErrors { get; set; }
    public int ResolvedErrors { get; set; }
    public int UnresolvedErrors { get; set; }  // Changed from PendingErrors to match AnalyticsService
    public decimal ResolutionRate { get; set; }  // Changed from double to decimal
    public decimal AverageResolutionTimeHours { get; set; }  // Added for AnalyticsService
    public Dictionary<string, int> ErrorsBySeverity { get; set; } = new();  // Added for AnalyticsService
    public Dictionary<string, int> ErrorsByType { get; set; } = new();
    public List<string> TopErrorSources { get; set; } = new();  // Added for AnalyticsService
    public Dictionary<DateTime, int> ErrorsByHour { get; set; } = new();  // Keep for backward compatibility
}

public class PerformanceAnalyticsDto
{
    public decimal AverageResponseTimeMs { get; set; }  // Changed from double to decimal
    public decimal P95ResponseTimeMs { get; set; }  // Added for AnalyticsService
    public decimal P99ResponseTimeMs { get; set; }  // Added for AnalyticsService
    public decimal AverageMemoryUsageMB { get; set; }  // Added for AnalyticsService
    public decimal AverageCpuUsagePercent { get; set; }  // Added for AnalyticsService
    public int DatabaseConnectionCount { get; set; }  // Added for AnalyticsService
    public decimal AverageDatabaseQueryTimeMs { get; set; }  // Added for AnalyticsService
    public List<PerformanceTrendDto> ResponseTimeTrends { get; set; } = new();  // Renamed from ResponseTimeTrendDto
    // Keep old properties for backward compatibility
    public double MaxResponseTimeMs { get; set; }
    public double MinResponseTimeMs { get; set; }
    public int TotalRequests { get; set; }
    public Dictionary<string, double> EndpointResponseTimes { get; set; } = new();
    public Dictionary<DateTime, double> ResponseTimesByHour { get; set; } = new();
}

public class SecurityAnalyticsDto
{
    public int TotalSecurityEvents { get; set; }
    public int CriticalEvents { get; set; }
    public int BlockedIpAddresses { get; set; }  // Added for AnalyticsService
    public int SuspendedAccounts { get; set; }  // Added for AnalyticsService
    public decimal AverageRiskScore { get; set; }  // Added for AnalyticsService
    public Dictionary<string, int> EventsByType { get; set; } = new();
    public List<string> TopRiskIpAddresses { get; set; } = new();  // Added for AnalyticsService
    // Keep old properties for backward compatibility
    public int WarningEvents { get; set; }
    public int InfoEvents { get; set; }
    public Dictionary<DateTime, int> EventsByHour { get; set; } = new();
}

public class BusinessMetricsDto
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int NewRegistrations { get; set; }
    public decimal UserRetentionRate { get; set; }  // Changed from double to decimal
    public decimal UserEngagementScore { get; set; }  // Changed from double to decimal
    public Dictionary<string, int> FeatureAdoptionRates { get; set; } = new();  // Added for AnalyticsService
    // Keep old properties for backward compatibility
    public int TotalCourseEnrollments { get; set; }
    public int CompletedCourses { get; set; }
    public decimal TotalRevenue { get; set; }
    public Dictionary<string, int> PopularCourses { get; set; } = new();
}

public class LoginTrendDto
{
    public DateTime Date { get; set; }
    public int TotalAttempts { get; set; }
    public int SuccessfulLogins { get; set; }
    public int FailedAttempts { get; set; }
    public decimal SuccessRate { get; set; }  // Changed from computed double to decimal property
}

public class ErrorTrendDto
{
    public DateTime Date { get; set; }
    public int ErrorCount { get; set; }
    public Dictionary<string, int> ErrorsBySeverity { get; set; } = new();
}

public class DeviceAnalyticsDto
{
    public string DeviceType { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;  // Added for AnalyticsService
    public string Browser { get; set; } = string.Empty;  // Added for AnalyticsService
    public int UserCount { get; set; }
    public int SessionCount { get; set; }  // Added for AnalyticsService
    public decimal AverageSessionDurationMinutes { get; set; }  // Added for AnalyticsService
    public double Percentage { get; set; }  // Keep for backward compatibility
}

public class FeatureUsageDto
{
    public string FeatureName { get; set; } = string.Empty;
    public int UsageCount { get; set; }
    public int UniqueUsers { get; set; }  // Added for AnalyticsService
    public decimal AdoptionRate { get; set; }  // Changed from double to decimal
    public decimal AverageUsageTimeSeconds { get; set; }  // Added for AnalyticsService
}

public class ResponseTimeTrendDto
{
    public DateTime Timestamp { get; set; }
    public double Value { get; set; }
}

// Note: PerformanceTrendDto is defined separately below at line 220

// Additional DTOs for new endpoints
public class AnalyticsOverviewDto
{
    public LoginAnalyticsDto LoginAnalytics { get; set; } = new();
    public ErrorAnalyticsDto ErrorAnalytics { get; set; } = new();
    public PerformanceAnalyticsDto PerformanceAnalytics { get; set; } = new();
    public SecurityAnalyticsDto SecurityAnalytics { get; set; } = new();
    public BusinessMetricsDto BusinessMetrics { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
    public string Period { get; set; } = string.Empty;
}

public class UserEngagementDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime LastLoginDate { get; set; }
    public int TotalSessions { get; set; }
    public TimeSpan TotalTimeSpent { get; set; }
    public int CoursesEnrolled { get; set; }
    public int CoursesCompleted { get; set; }
    public double EngagementScore { get; set; }
    public DateTime RegistrationDate { get; set; }
}

public class CoursePopularityDto
{
    public Guid CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string InstructorName { get; set; } = string.Empty;
    public int EnrollmentCount { get; set; }
    public int ViewCount { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public double CompletionRate { get; set; }
    public decimal Revenue { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class RevenueAnalyticsDto
{
    public decimal TotalRevenue { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public decimal YearlyRevenue { get; set; }
    public double RevenueGrowthRate { get; set; }
    public Dictionary<string, decimal> RevenueByCategory { get; set; } = new();
    public Dictionary<DateTime, decimal> RevenueByMonth { get; set; } = new();
    public List<TopSellingCourseDto> TopSellingCourses { get; set; } = new();
    public decimal AverageOrderValue { get; set; }
    public int TotalTransactions { get; set; }
}

public class TopSellingCourseDto
{
    public Guid CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int SalesCount { get; set; }
    public decimal TotalRevenue { get; set; }
    public string Category { get; set; } = string.Empty;
}

public class UserRetentionDto
{
    public string Period { get; set; } = string.Empty;
    public int NewUsers { get; set; }
    public int ReturnedUsers { get; set; }
    public double RetentionRate { get; set; }
    public Dictionary<string, int> UsersByActivity { get; set; } = new();
    public List<RetentionCohortDto> CohortAnalysis { get; set; } = new();
}

public class RetentionCohortDto
{
    public DateTime CohortMonth { get; set; }
    public int InitialUsers { get; set; }
    public Dictionary<int, double> RetentionByWeek { get; set; } = new();
}

public class ConversionAnalyticsDto
{
    public double OverallConversionRate { get; set; }
    public int TotalVisitors { get; set; }
    public int TotalSignups { get; set; }
    public int TotalPurchases { get; set; }
    public Dictionary<string, double> ConversionRateBySource { get; set; } = new();
    public List<ConversionFunnelDto> ConversionFunnel { get; set; } = new();
}

public class ConversionFunnelDto
{
    public string Stage { get; set; } = string.Empty;
    public int Users { get; set; }
    public double ConversionRate { get; set; }
    public double DropoffRate { get; set; }
}

// Additional DTOs for IAnalyticsService
public class HourlyLoginDto
{
    public int Hour { get; set; }
    public int LoginCount { get; set; }
}

public class UserBehaviorAnalyticsDto
{
    public int TotalSessions { get; set; }
    public decimal AverageSessionDurationMinutes { get; set; }
    public int TotalPageViews { get; set; }
    public DateTime? LastLoginDate { get; set; }
    public List<string> PreferredDevices { get; set; } = new();
    public List<string> CommonLocations { get; set; } = new();
    public Dictionary<string, int> FeatureUsage { get; set; } = new();
}

public class ApiEndpointErrorDto
{
    public string Path { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public int TotalRequests { get; set; }
    public int ErrorCount { get; set; }
    public decimal ErrorRate { get; set; }
    public decimal AverageResponseTime { get; set; }
}

public class PerformanceTrendDto
{
    public DateTime Timestamp { get; set; }
    public decimal Value { get; set; }
    public string MetricType { get; set; } = string.Empty;
}

public class ApiPerformanceDto
{
    public string Path { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public int RequestCount { get; set; }
    public decimal AverageResponseTimeMs { get; set; }
    public decimal P95ResponseTimeMs { get; set; }
    public decimal P99ResponseTimeMs { get; set; }
    public int ErrorCount { get; set; }
    public decimal ErrorRate { get; set; }
}

public class DatabasePerformanceDto
{
    public decimal AverageQueryTimeMs { get; set; }
    public int TotalQueries { get; set; }
    public int SlowQueries { get; set; }
    public List<string> TopSlowQueries { get; set; } = new();
    public Dictionary<string, int> QueriesByTable { get; set; } = new();
    public int ConnectionPoolUsage { get; set; }
}

public class ThreatAnalysisDto
{
    public string EventType { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public decimal RiskScore { get; set; }
    public int EventCount { get; set; }
    public DateTime FirstDetected { get; set; }
    public DateTime LastDetected { get; set; }
    public bool IsResolved { get; set; }
}

public class RiskScoreDistributionDto
{
    public string RiskRange { get; set; } = string.Empty;
    public int EventCount { get; set; }
    public decimal Percentage { get; set; }
}

public class UserActivityAnalyticsDto
{
    public int TotalActiveUsers { get; set; }
    public int NewUsers { get; set; }
    public int ReturningUsers { get; set; }
    public decimal AverageSessionsPerUser { get; set; }
    public decimal AverageSessionDurationMinutes { get; set; }
    public Dictionary<string, int> UsersByDevice { get; set; } = new();
    public Dictionary<string, int> UsersByLocation { get; set; } = new();
}

public class SessionAnalyticsDto
{
    public DateTime Date { get; set; }
    public int SessionCount { get; set; }
    public int UniqueUsers { get; set; }
    public decimal AverageSessionDurationMinutes { get; set; }
    public int BounceCount { get; set; }
}

public class RealTimeMetricsDto
{
    public int CurrentActiveUsers { get; set; }
    public int RequestsPerMinute { get; set; }
    public decimal CurrentResponseTimeMs { get; set; }
    public decimal ErrorRatePercent { get; set; }
    public decimal CpuUsagePercent { get; set; }
    public decimal MemoryUsagePercent { get; set; }
    public int DatabaseConnections { get; set; }
    public List<RecentErrorDto> RecentErrors { get; set; } = new();
}

public class RecentErrorDto
{
    public DateTime Timestamp { get; set; }
    public string ErrorType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
}

public class LiveUserSessionDto
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public DateTime LoginTime { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public string LastActivity { get; set; } = string.Empty;
    public int ActivityCount { get; set; }
}

// Analytics-specific system health metrics (different from AdminDtos.SystemHealthDto)
// Used by IAnalyticsService.GetSystemHealthMetricsAsync()
public class AnalyticsSystemHealthDto
{
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public decimal CpuUsagePercent { get; set; }
    public long MemoryUsageMB { get; set; }
    public long DiskUsageMB { get; set; }
    public int ActiveConnections { get; set; }
    public DateTime CheckedAt { get; set; }
}

public class CustomAnalyticsResultDto
{
    public string QueryName { get; set; } = string.Empty;
    public Dictionary<string, object> Results { get; set; } = new();
    public DateTime ExecutedAt { get; set; }
    public long ExecutionTimeMs { get; set; }
}

public class LoginPatternPredictionDto
{
    public Guid UserId { get; set; }
    public List<DateTime> PredictedLoginTimes { get; set; } = new();
    public Dictionary<string, decimal> PreferredDeviceProbabilities { get; set; } = new();
    public decimal AnomalyDetectionThreshold { get; set; }
}

public class ErrorPredictionDto
{
    public string Component { get; set; } = string.Empty;
    public List<PredictedErrorDto> PredictedErrors { get; set; } = new();
    public decimal ConfidenceScore { get; set; }
}

public class PredictedErrorDto
{
    public DateTime PredictedTime { get; set; }
    public string ErrorType { get; set; } = string.Empty;
    public decimal Probability { get; set; }
}

public class CapacityPredictionDto
{
    public DateTime PredictionDate { get; set; }
    public int PredictedUserCount { get; set; }
    public decimal PredictedCpuUsage { get; set; }
    public decimal PredictedMemoryUsage { get; set; }
    public decimal PredictedDiskUsage { get; set; }
    public List<string> RecommendedActions { get; set; } = new();
}