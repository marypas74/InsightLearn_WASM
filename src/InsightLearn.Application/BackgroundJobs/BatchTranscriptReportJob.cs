using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.Storage;
using Microsoft.Extensions.Logging;

namespace InsightLearn.Application.BackgroundJobs
{
    /// <summary>
    /// Generates a report after batch transcript processing completes.
    /// Checks Hangfire job states and calculates success/failure statistics.
    /// Part of Batch Video Transcription System v2.3.23-dev.
    /// </summary>
    public class BatchTranscriptReportJob
    {
        private readonly ILogger<BatchTranscriptReportJob> _logger;

        public BatchTranscriptReportJob(ILogger<BatchTranscriptReportJob> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Generate completion report for a list of Hangfire job IDs.
        /// </summary>
        public async Task GenerateReportAsync(List<string> jobIds)
        {
            _logger.LogInformation("[BATCH REPORT] Generating report for {Count} jobs", jobIds.Count);

            if (jobIds == null || jobIds.Count == 0)
            {
                _logger.LogWarning("[BATCH REPORT] No job IDs provided - skipping report");
                return;
            }

            try
            {
                // Get Hangfire storage connection
                using var connection = JobStorage.Current.GetConnection();

                int succeeded = 0;
                int failed = 0;
                int processing = 0;
                int pending = 0;

                foreach (var jobId in jobIds)
                {
                    try
                    {
                        var jobData = connection.GetJobData(jobId);
                        if (jobData == null)
                        {
                            pending++;
                            continue;
                        }

                        var state = jobData.State;
                        switch (state)
                        {
                            case "Succeeded":
                                succeeded++;
                                break;
                            case "Failed":
                                failed++;
                                break;
                            case "Processing":
                            case "Enqueued":
                                processing++;
                                break;
                            default:
                                pending++;
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "[BATCH REPORT] Failed to get job data for {JobId}", jobId);
                        pending++;
                    }
                }

                // Calculate percentages
                var total = jobIds.Count;
                var successRate = total > 0 ? (double)succeeded / total * 100 : 0;
                var failureRate = total > 0 ? (double)failed / total * 100 : 0;

                // Generate report text
                var report = new StringBuilder();
                report.AppendLine("═══════════════════════════════════════════════════");
                report.AppendLine("  BATCH TRANSCRIPT GENERATION REPORT");
                report.AppendLine("═══════════════════════════════════════════════════");
                report.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
                report.AppendLine($"Total Jobs: {total}");
                report.AppendLine();
                report.AppendLine("Status Breakdown:");
                report.AppendLine($"  ✅ Succeeded:  {succeeded,4} ({successRate:F1}%)");
                report.AppendLine($"  ❌ Failed:     {failed,4} ({failureRate:F1}%)");
                report.AppendLine($"  ⏳ Processing: {processing,4}");
                report.AppendLine($"  📋 Pending:    {pending,4}");
                report.AppendLine("═══════════════════════════════════════════════════");

                // Log report
                _logger.LogInformation("[BATCH REPORT]\n{Report}", report.ToString());

                // TODO: Optional - Send email to admin with report
                // await _emailService.SendAsync("admin@insightlearn.cloud", "Batch Transcript Report", report.ToString());

                await Task.CompletedTask; // Placeholder for async operations
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[BATCH REPORT] Failed to generate report");
                throw;
            }
        }
    }
}
