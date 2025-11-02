using InsightLearn.Core.Entities;
using InsightLearn.Application.DTOs;

namespace InsightLearn.Application.Interfaces;

public interface IAnalyticsService
{
    // Login Analytics
    Task<LoginAnalyticsDto> GetLoginAnalyticsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? groupBy = "Day"); // Day, Hour, Week, Month

    Task<List<LoginTrendDto>> GetLoginTrendsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? loginMethod = null);

    Task<UserBehaviorAnalyticsDto> GetUserBehaviorAnalyticsAsync(
        Guid? userId = null,
        DateTime? startDate = null,
        DateTime? endDate = null);

    // Error Analytics
    Task<ErrorAnalyticsDto> GetErrorAnalyticsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? severity = null);

    Task<List<ErrorTrendDto>> GetErrorTrendsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? groupBy = "Day");

    Task<List<ApiEndpointErrorDto>> GetApiEndpointErrorAnalysisAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        int topN = 20);

    // Performance Analytics
    Task<PerformanceAnalyticsDto> GetPerformanceAnalyticsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? component = null);

    Task<List<ApiPerformanceDto>> GetApiPerformanceAnalysisAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        int topN = 20);

    Task<DatabasePerformanceDto> GetDatabasePerformanceAnalysisAsync(
        DateTime? startDate = null,
        DateTime? endDate = null);

    // Security Analytics
    Task<SecurityAnalyticsDto> GetSecurityAnalyticsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null);

    Task<List<ThreatAnalysisDto>> GetThreatAnalysisAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        decimal minRiskScore = 0.5m);

    Task<List<RiskScoreDistributionDto>> GetRiskScoreDistributionAsync(
        DateTime? startDate = null,
        DateTime? endDate = null);

    // User Analytics
    Task<UserActivityAnalyticsDto> GetUserActivityAnalyticsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null);

    Task<List<SessionAnalyticsDto>> GetSessionAnalyticsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? groupBy = "Day");

    Task<List<DeviceAnalyticsDto>> GetDeviceAnalyticsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null);

    // Business Analytics
    Task<BusinessMetricsDto> GetBusinessMetricsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null);

    Task<List<FeatureUsageDto>> GetFeatureUsageAnalyticsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null);

    // Real-time Analytics
    Task<RealTimeMetricsDto> GetRealTimeMetricsAsync();
    Task<List<LiveUserSessionDto>> GetLiveUserSessionsAsync();
    Task<AnalyticsSystemHealthDto> GetSystemHealthMetricsAsync();

    // Custom Analytics
    Task<List<CustomAnalyticsResultDto>> ExecuteCustomAnalyticsQueryAsync(
        string queryName,
        Dictionary<string, object>? parameters = null);

    // Export and Reporting
    Task<byte[]> ExportAnalyticsReportAsync(
        string reportType,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string format = "Excel"); // Excel, PDF, CSV

    Task<string> GenerateAnalyticsSummaryAsync(
        DateTime? startDate = null,
        DateTime? endDate = null);

    // Predictive Analytics
    Task<LoginPatternPredictionDto> PredictLoginPatternsAsync(Guid userId);
    Task<ErrorPredictionDto> PredictErrorTrendsAsync(string? component = null);
    Task<CapacityPredictionDto> PredictCapacityNeedsAsync();
}