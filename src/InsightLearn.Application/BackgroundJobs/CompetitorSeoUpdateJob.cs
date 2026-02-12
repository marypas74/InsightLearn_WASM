using InsightLearn.Application.Services;

namespace InsightLearn.Application.BackgroundJobs;

/// <summary>
/// Hangfire background job to update competitor SEO metrics
/// Runs every hour to fetch real data from Google PageSpeed Insights API
/// v2.5.4-dev: Real competitor SEO comparison
/// </summary>
public class CompetitorSeoUpdateJob
{
    private readonly CompetitorSeoService _competitorSeoService;
    private readonly ILogger<CompetitorSeoUpdateJob> _logger;

    public CompetitorSeoUpdateJob(
        CompetitorSeoService competitorSeoService,
        ILogger<CompetitorSeoUpdateJob> logger)
    {
        _competitorSeoService = competitorSeoService;
        _logger = logger;
    }

    /// <summary>
    /// Execute the competitor SEO metrics update job
    /// Called by Hangfire scheduler every hour
    /// </summary>
    public async Task ExecuteAsync()
    {
        _logger.LogInformation("[COMPETITOR-JOB] Starting competitor SEO metrics update...");
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            await _competitorSeoService.UpdateCompetitorMetricsAsync();

            stopwatch.Stop();
            _logger.LogInformation("[COMPETITOR-JOB] Competitor SEO metrics updated successfully in {ElapsedMs}ms",
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "[COMPETITOR-JOB] Failed to update competitor SEO metrics after {ElapsedMs}ms",
                stopwatch.ElapsedMilliseconds);
            throw; // Re-throw for Hangfire retry mechanism
        }
    }
}