using InsightLearn.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;

namespace InsightLearn.Application.Services;

public class AdvancedLoggingService : IAdvancedLoggingService
{
    private readonly ILogger<AdvancedLoggingService> _logger;
    private readonly ConcurrentQueue<BusinessEventData> _eventQueue = new();
    private readonly Timer _processingTimer;

    public AdvancedLoggingService(ILogger<AdvancedLoggingService> logger)
    {
        _logger = logger;
        _processingTimer = new Timer(ProcessEvents, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    public async Task LogBusinessEventAsync(string eventType, string eventName, string userId, Dictionary<string, object>? data = null)
    {
        var businessEvent = new BusinessEventData
        {
            EventType = eventType,
            EventName = eventName,
            UserId = userId,
            Data = data ?? new Dictionary<string, object>()
        };

        _eventQueue.Enqueue(businessEvent);
        
        _logger.LogInformation("Business event logged: {EventType}.{EventName} by user {UserId}",
            eventType, eventName, userId);

        await Task.CompletedTask;
    }

    public async Task LogSecurityEventAsync(string eventType, string userId, int riskScore, Dictionary<string, object>? data = null)
    {
        var securityData = data ?? new Dictionary<string, object>();
        securityData["RiskScore"] = riskScore;
        securityData["Timestamp"] = DateTime.UtcNow;

        _logger.LogWarning("Security event: {EventType} by user {UserId} with risk score {RiskScore}",
            eventType, userId, riskScore);

        await LogBusinessEventAsync("Security", eventType, userId, securityData);
    }

    public async Task LogPerformanceAsync(string metricName, double value, string? userId = null)
    {
        var performanceData = new Dictionary<string, object>
        {
            ["MetricName"] = metricName,
            ["Value"] = value,
            ["Timestamp"] = DateTime.UtcNow
        };

        _logger.LogInformation("Performance metric: {MetricName} = {Value} for user {UserId}",
            metricName, value, userId ?? "System");

        await LogBusinessEventAsync("Performance", metricName, userId ?? "System", performanceData);
    }

    public async Task<LoggingHealth> GetHealthAsync()
    {
        await Task.CompletedTask;
        
        return new LoggingHealth
        {
            IsHealthy = true,
            PendingLogs = _eventQueue.Count,
            AverageLogTime = TimeSpan.FromMilliseconds(50),
            CheckedAt = DateTime.UtcNow
        };
    }

    private async void ProcessEvents(object? state)
    {
        var processedCount = 0;
        const int batchSize = 100;

        while (processedCount < batchSize && _eventQueue.TryDequeue(out var businessEvent))
        {
            try
            {
                // In real implementation, would save to database or external system
                var json = JsonSerializer.Serialize(businessEvent);
                _logger.LogDebug("Processing business event: {EventJson}", json);
                
                processedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process business event: {EventType}.{EventName}",
                    businessEvent.EventType, businessEvent.EventName);
            }
        }

        if (processedCount > 0)
        {
            _logger.LogInformation("Processed {Count} business events", processedCount);
        }

        await Task.CompletedTask;
    }

    public void Dispose()
    {
        _processingTimer?.Dispose();
    }
}