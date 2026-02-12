using InsightLearn.Application.Services;

namespace InsightLearn.Application.BackgroundJobs;

/// <summary>
/// Hangfire background job to update search visibility metrics
/// Runs every 4 hours to check Google indexation and PageSpeed for InsightLearn
/// v2.5.6-dev: Real visibility monitoring
/// </summary>
public class SearchVisibilityUpdateJob
{
    private readonly SearchVisibilityService _searchVisibilityService;
    private readonly ILogger<SearchVisibilityUpdateJob> _logger;

    public SearchVisibilityUpdateJob(
        SearchVisibilityService searchVisibilityService,
        ILogger<SearchVisibilityUpdateJob> logger)
    {
        _searchVisibilityService = searchVisibilityService;
        _logger = logger;
    }

    /// <summary>
    /// Execute the search visibility metrics update job
    /// Called by Hangfire scheduler every 4 hours
    /// </summary>
    public async Task ExecuteAsync()
    {
        _logger.LogInformation("[VISIBILITY-JOB] Starting search visibility metrics update...");
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            await _searchVisibilityService.UpdateVisibilityMetricsAsync();

            stopwatch.Stop();
            _logger.LogInformation("[VISIBILITY-JOB] Search visibility metrics updated successfully in {ElapsedMs}ms",
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "[VISIBILITY-JOB] Failed to update search visibility metrics after {ElapsedMs}ms",
                stopwatch.ElapsedMilliseconds);
            throw; // Re-throw for Hangfire retry mechanism
        }
    }
}