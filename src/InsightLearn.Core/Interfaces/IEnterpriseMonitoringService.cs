using InsightLearn.Core.Entities;

namespace InsightLearn.Application.Interfaces;

public interface IEnterpriseMonitoringService
{
    Task<SystemHealth> GetSystemHealthAsync();
    Task<IEnumerable<SystemAlert>> GetActiveAlertsAsync();
    Task<IEnumerable<PerformanceMetric>> GetPerformanceMetricsAsync(int count = 100);
    Task<Dictionary<string, RealTimeMetric>> GetRealTimeMetricsAsync();
    Task CreateAlertAsync(SystemAlert alert);
    Task ResolveAlertAsync(int alertId, string resolutionNotes);
    Task ResolveAlertAsync(Guid alertId, string resolutionNotes);
    Task<bool> IsHealthyAsync();
    Task<EnterpriseHealthStatus> GetEnterpriseHealthStatusAsync();
    Task<object> GetPerformanceHistoryAsync(int hours);
}