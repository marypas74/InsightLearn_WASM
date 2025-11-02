using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using InsightLearn.Application.Interfaces;

namespace InsightLearn.Application.Services;

public class LogCleanupBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LogCleanupBackgroundService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(24); // Run daily
    private readonly int _retentionDays = 30; // Keep logs for 30 days

    public LogCleanupBackgroundService(IServiceProvider serviceProvider, ILogger<LogCleanupBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Log cleanup background service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformCleanupAsync();
                await Task.Delay(_cleanupInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Log cleanup background service was cancelled");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during log cleanup");
                // Wait 5 minutes before retrying after an error
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("Log cleanup background service stopped");
    }

    private async Task PerformCleanupAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var loggingService = scope.ServiceProvider.GetRequiredService<ILoggingService>();

            _logger.LogInformation("Starting log cleanup process");
            await loggingService.CleanupOldLogsAsync(_retentionDays);
            _logger.LogInformation("Log cleanup process completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform log cleanup");
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Log cleanup background service is stopping");
        await base.StopAsync(stoppingToken);
    }
}