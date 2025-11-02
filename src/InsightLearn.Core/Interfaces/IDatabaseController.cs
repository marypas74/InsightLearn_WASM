namespace InsightLearn.Core.Interfaces;

public interface IDatabaseController
{
    Task<T> ExecuteWithAuditAsync<T>(string operation, string userId, Func<Task<T>> dbOperation);
    Task ExecuteWithAuditAsync(string operation, string userId, Func<Task> dbOperation);
    Task<bool> CheckHealthAsync();
}

public class DatabaseMetrics
{
    public int ActiveConnections { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
    public int TotalOperations { get; set; }
    public int FailedOperations { get; set; }
    public DateTime CollectedAt { get; set; } = DateTime.UtcNow;
}