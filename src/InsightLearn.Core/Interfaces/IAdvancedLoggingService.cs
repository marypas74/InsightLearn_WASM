namespace InsightLearn.Core.Interfaces;

public interface IAdvancedLoggingService
{
    Task LogBusinessEventAsync(string eventType, string eventName, string userId, Dictionary<string, object>? data = null);
    Task LogSecurityEventAsync(string eventType, string userId, int riskScore, Dictionary<string, object>? data = null);
    Task LogPerformanceAsync(string metricName, double value, string? userId = null);
    Task<LoggingHealth> GetHealthAsync();
}

public class LoggingHealth
{
    public bool IsHealthy { get; set; }
    public int PendingLogs { get; set; }
    public TimeSpan AverageLogTime { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
}

public class BusinessEventData
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EventType { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public Dictionary<string, object> Data { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool IsSuccessful { get; set; } = true;
}