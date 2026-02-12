using InsightLearn.Application.Services;

namespace InsightLearn.Application.BackgroundJobs;

/// <summary>
/// Hangfire background job to update SEO metrics periodically
/// Runs every 5 minutes to refresh Prometheus gauges with current database values
/// v2.5.4-dev: Real-time SEO dashboard metrics
/// </summary>
public class SeoMetricsUpdateJob
{
    private readonly SeoMetricsService _seoMetricsService;
    private readonly ILogger<SeoMetricsUpdateJob> _logger;

    public SeoMetricsUpdateJob(
        SeoMetricsService seoMetricsService,
        ILogger<SeoMetricsUpdateJob> logger)
    {
        _seoMetricsService = seoMetricsService;
        _logger = logger;
    }

    /// <summary>
    /// Execute the SEO metrics update job
    /// Called by Hangfire scheduler every 5 minutes
    /// </summary>
    public async Task ExecuteAsync()
    {
        _logger.LogInformation("[SEO-JOB] Starting SEO metrics update...");
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            await _seoMetricsService.UpdateDynamicMetricsAsync();

            stopwatch.Stop();
            _logger.LogInformation("[SEO-JOB] SEO metrics updated successfully in {ElapsedMs}ms",
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "[SEO-JOB] Failed to update SEO metrics after {ElapsedMs}ms",
                stopwatch.ElapsedMilliseconds);
            throw; // Re-throw for Hangfire retry mechanism
        }
    }
}