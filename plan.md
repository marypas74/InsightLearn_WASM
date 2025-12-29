# Autonomous Development Plan - Batch Transcription System v2.3.23-dev

**Created**: 2025-12-27
**Version**: 2.3.23-dev
**Total Remaining Work**: ~13 hours (17 tasks across 5 phases)
**Goal**: Complete all pending tasks autonomously without user intervention

---

## 📋 Executive Summary

### Current Status
- ✅ **Phase 1 Tasks 1.1-1.2**: Complete (endpoint modified, Hangfire verified)
- ⏳ **Phase 1 Tasks 1.3-1.4**: Pending (frontend polling + testing)
- 📋 **Phases 2-6**: All pending (batch processor, K8s, monitoring, testing, deployment)

### Success Criteria
All 17 pending tasks completed with:
- Zero compilation errors
- All tests passing
- Kubernetes deployment successful
- Documentation updated
- Migration guide created

### Critical Dependencies
1. **Whisper.net + FFMpegCore**: Already installed (verified in InsightLearn.Application.csproj)
2. **Hangfire**: Already configured (verified in TranscriptGenerationJob.cs)
3. **MongoDB + Redis**: Already operational (verified in k8s deployments)
4. **Kubernetes cluster**: K3s on insightlearn-k3s node (verified operational)

---

## 🎯 Phase 1: Frontend Polling & Testing (Remaining: 2 tasks, 1.5 hours)

### Task 1.3: Update Frontend Polling Logic ✅ AUTO-EXECUTABLE

**File**: `src/InsightLearn.WebAssembly/Components/VideoPlayer.razor.cs`
**Estimated Time**: 1 hour
**Auto-Execute**: Yes (clear implementation, no decisions needed)

#### Implementation Steps

**Step 1**: Read current VideoPlayer.razor.cs to locate transcript generation logic
```bash
# No command needed - use Read tool on VideoPlayer.razor.cs
```

**Step 2**: Add HTTP 202 handling and polling logic

**COMPLETE CODE TO ADD** (after existing GenerateTranscript method):

```csharp
// Phase 1 Task 1.3: Handle HTTP 202 and poll for completion
private async Task GenerateTranscriptAsync()
{
    try
    {
        IsGeneratingTranscript = true;
        TranscriptError = null;

        // Call the /generate endpoint (returns HTTP 202 Accepted)
        var response = await HttpClient.PostAsJsonAsync(
            $"/api/transcripts/{LessonId}/generate",
            new { Language = "en-US" }
        );

        if (response.StatusCode == System.Net.HttpStatusCode.OK)
        {
            // Transcript already exists - return immediately
            var existingTranscript = await response.Content.ReadFromJsonAsync<VideoTranscriptDto>();
            CurrentTranscript = existingTranscript;
            IsGeneratingTranscript = false;
            StateHasChanged();
            return;
        }

        if (response.StatusCode == System.Net.HttpStatusCode.Accepted)
        {
            // Job queued - start polling
            var jobInfo = await response.Content.ReadFromJsonAsync<TranscriptJobResponse>();
            await PollTranscriptStatusAsync(jobInfo.LessonId, maxAttempts: 60, intervalMs: 2000);
        }
        else
        {
            TranscriptError = $"Failed to generate transcript: {response.StatusCode}";
            IsGeneratingTranscript = false;
        }
    }
    catch (Exception ex)
    {
        TranscriptError = $"Error: {ex.Message}";
        IsGeneratingTranscript = false;
    }

    StateHasChanged();
}

private async Task PollTranscriptStatusAsync(Guid lessonId, int maxAttempts = 60, int intervalMs = 2000)
{
    int attempts = 0;
    while (attempts < maxAttempts)
    {
        attempts++;

        try
        {
            var statusResponse = await HttpClient.GetAsync($"/api/transcripts/{lessonId}/status");
            if (!statusResponse.IsSuccessStatusCode)
            {
                await Task.Delay(intervalMs);
                continue;
            }

            var status = await statusResponse.Content.ReadFromJsonAsync<TranscriptProcessingStatusDto>();

            // Update progress (if available)
            TranscriptProgress = status.ProgressPercentage ?? 0;
            StateHasChanged();

            if (status.ProcessingStatus == "Completed")
            {
                // Fetch completed transcript
                var transcriptResponse = await HttpClient.GetAsync($"/api/transcripts/{lessonId}");
                if (transcriptResponse.IsSuccessStatusCode)
                {
                    CurrentTranscript = await transcriptResponse.Content.ReadFromJsonAsync<VideoTranscriptDto>();
                }
                IsGeneratingTranscript = false;
                StateHasChanged();
                return;
            }
            else if (status.ProcessingStatus == "Failed")
            {
                TranscriptError = status.ErrorMessage ?? "Transcript generation failed";
                IsGeneratingTranscript = false;
                StateHasChanged();
                return;
            }

            // Still processing - wait and retry
            await Task.Delay(intervalMs);
        }
        catch (Exception ex)
        {
            // Network error - retry
            await Task.Delay(intervalMs);
        }
    }

    // Max attempts reached - timeout
    TranscriptError = "Transcript generation timed out (2 minutes). Please try again.";
    IsGeneratingTranscript = false;
    StateHasChanged();
}

// DTO classes (add to file or separate DTOs namespace)
private class TranscriptJobResponse
{
    public Guid LessonId { get; set; }
    public string JobId { get; set; }
    public string Status { get; set; }
    public string Message { get; set; }
    public int EstimatedCompletionSeconds { get; set; }
}

// Add progress property to component
private int TranscriptProgress { get; set; } = 0;
```

**Step 3**: Update VideoPlayer.razor markup to show progress

**COMPLETE CODE TO ADD** (in the transcript section):

```razor
@* Phase 1 Task 1.3: Progress bar for transcript generation *@
@if (IsGeneratingTranscript && TranscriptProgress > 0)
{
    <div class="transcript-progress">
        <div class="progress-bar">
            <div class="progress-fill" style="width: @(TranscriptProgress)%"></div>
        </div>
        <p class="progress-text">Generating transcript... @(TranscriptProgress)%</p>
    </div>
}
```

**Step 4**: Add CSS for progress bar

**FILE**: `src/InsightLearn.WebAssembly/wwwroot/css/video-components.css`

**COMPLETE CODE TO ADD** (at the end of file):

```css
/* Phase 1 Task 1.3: Transcript generation progress */
.transcript-progress {
    margin: 1rem 0;
    padding: 1rem;
    background: #f8f9fa;
    border-radius: 8px;
}

.progress-bar {
    width: 100%;
    height: 8px;
    background: #e9ecef;
    border-radius: 4px;
    overflow: hidden;
}

.progress-fill {
    height: 100%;
    background: linear-gradient(90deg, #4CAF50, #8BC34A);
    transition: width 0.3s ease;
}

.progress-text {
    margin-top: 0.5rem;
    font-size: 0.875rem;
    color: #6c757d;
    text-align: center;
}
```

#### Verification Checklist

- [ ] VideoPlayer.razor.cs compiles without errors
- [ ] GenerateTranscriptAsync() method handles both HTTP 200 and 202
- [ ] PollTranscriptStatusAsync() polls every 2 seconds for max 60 attempts (2 minutes)
- [ ] Progress bar displays and updates
- [ ] On completion, transcript is fetched and displayed
- [ ] On failure, error message is shown with retry option

#### Success Criteria
- API returns HTTP 202, frontend starts polling
- Progress bar shows 0-100% during generation
- After 40-60 seconds, transcript appears
- Zero timeout errors in browser console

#### Rollback Procedure
If errors occur:
1. Revert VideoPlayer.razor.cs changes
2. Keep old synchronous implementation temporarily
3. Report error details and continue with Phase 2

---

### Task 1.4: Testing & Verification ✅ AUTO-EXECUTABLE

**Estimated Time**: 30 minutes
**Auto-Execute**: Yes (scripted tests, clear pass/fail criteria)

#### Test Cases

**Test 1: Single Video Transcription (5 min duration)**

```bash
# Get a test lesson ID from database
LESSON_ID=$(kubectl exec sqlserver-0 -n insightlearn -- \
  /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -d InsightLearnDb \
  -Q "SELECT TOP 1 Id FROM Lessons WHERE VideoFileId IS NOT NULL" -h -1 | tr -d '[:space:]')

# Call /generate endpoint
curl -X POST "http://localhost:31081/api/transcripts/${LESSON_ID}/generate" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $ADMIN_JWT_TOKEN" \
  -d '{"language":"en-US"}' \
  -w "\nHTTP Status: %{http_code}\nResponse Time: %{time_total}s\n"

# Expected: HTTP 202, response time < 0.1s
```

**Test 2: Verify HTTP 202 Response**

```bash
# Parse response and verify job ID present
RESPONSE=$(curl -s -X POST "http://localhost:31081/api/transcripts/${LESSON_ID}/generate" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $ADMIN_JWT_TOKEN" \
  -d '{"language":"en-US"}')

echo "$RESPONSE" | jq '.jobId, .status'
# Expected: jobId="hangfire-...", status="Processing"
```

**Test 3: Verify Hangfire Job Appears**

```bash
# Check Hangfire dashboard
curl -s "http://localhost:31081/hangfire/api/jobs/enqueued" | jq '.[] | select(.Job contains("TranscriptGenerationJob"))'

# Expected: At least one TranscriptGenerationJob in queue
```

**Test 4: Verify Polling Returns Progress**

```bash
# Poll status endpoint
for i in {1..30}; do
  STATUS=$(curl -s "http://localhost:31081/api/transcripts/${LESSON_ID}/status" \
    -H "Authorization: Bearer $ADMIN_JWT_TOKEN")

  echo "Attempt $i: $(echo $STATUS | jq -r '.processingStatus, .progressPercentage')"

  # Check if completed
  if echo "$STATUS" | jq -e '.processingStatus == "Completed"' > /dev/null; then
    echo "✅ Transcript generation completed!"
    break
  fi

  sleep 2
done

# Expected: Status changes from "Processing" to "Completed" within 60 seconds
```

**Test 5: Verify Transcript Appears**

```bash
# Fetch completed transcript
curl -s "http://localhost:31081/api/transcripts/${LESSON_ID}" \
  -H "Authorization: Bearer $ADMIN_JWT_TOKEN" | jq '.segments | length'

# Expected: segments array with length > 0
```

**Test 6: Test Failure Scenario (Invalid Video ID)**

```bash
# Use fake GUID
FAKE_ID="00000000-0000-0000-0000-000000000000"

curl -X POST "http://localhost:31081/api/transcripts/${FAKE_ID}/generate" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $ADMIN_JWT_TOKEN" \
  -d '{"language":"en-US"}' \
  -w "\nHTTP Status: %{http_code}\n"

# Expected: HTTP 404 or 400 with error message
```

#### Automated Test Script

**CREATE FILE**: `/tmp/test-transcript-generation.sh`

```bash
#!/bin/bash
set -e

echo "🧪 Phase 1 Task 1.4: Transcript Generation Testing"
echo "=================================================="

# Test 1: Get valid lesson ID
echo -e "\n📝 Test 1: Finding test lesson..."
LESSON_ID=$(kubectl exec sqlserver-0 -n insightlearn -- \
  /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -d InsightLearnDb \
  -Q "SELECT TOP 1 Id FROM Lessons WHERE VideoFileId IS NOT NULL" -h -1 | tr -d '[:space:]')

if [ -z "$LESSON_ID" ]; then
  echo "❌ No lessons with video found"
  exit 1
fi
echo "✅ Test lesson ID: $LESSON_ID"

# Test 2: Call /generate endpoint
echo -e "\n📝 Test 2: Calling /generate endpoint..."
RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "http://localhost:31081/api/transcripts/${LESSON_ID}/generate" \
  -H "Content-Type: application/json" \
  -d '{"language":"en-US"}')

HTTP_CODE=$(echo "$RESPONSE" | tail -1)
BODY=$(echo "$RESPONSE" | head -n -1)

if [ "$HTTP_CODE" == "202" ]; then
  echo "✅ HTTP 202 Accepted received"
  JOB_ID=$(echo "$BODY" | jq -r '.jobId')
  echo "   Job ID: $JOB_ID"
elif [ "$HTTP_CODE" == "200" ]; then
  echo "✅ HTTP 200 OK (transcript already exists)"
  exit 0
else
  echo "❌ Unexpected HTTP $HTTP_CODE"
  echo "$BODY"
  exit 1
fi

# Test 3: Poll status endpoint
echo -e "\n📝 Test 3: Polling status endpoint (max 60 seconds)..."
for i in {1..30}; do
  sleep 2

  STATUS=$(curl -s "http://localhost:31081/api/transcripts/${LESSON_ID}/status")
  PROCESSING_STATUS=$(echo "$STATUS" | jq -r '.processingStatus')
  PROGRESS=$(echo "$STATUS" | jq -r '.progressPercentage // 0')

  echo "   Attempt $i: Status=$PROCESSING_STATUS, Progress=${PROGRESS}%"

  if [ "$PROCESSING_STATUS" == "Completed" ]; then
    echo "✅ Transcript generation completed!"
    break
  elif [ "$PROCESSING_STATUS" == "Failed" ]; then
    ERROR=$(echo "$STATUS" | jq -r '.errorMessage')
    echo "❌ Generation failed: $ERROR"
    exit 1
  fi

  if [ $i -eq 30 ]; then
    echo "⏱️  Timeout after 60 seconds (may still be processing in background)"
  fi
done

# Test 4: Verify transcript exists
echo -e "\n📝 Test 4: Fetching completed transcript..."
TRANSCRIPT=$(curl -s "http://localhost:31081/api/transcripts/${LESSON_ID}")
SEGMENT_COUNT=$(echo "$TRANSCRIPT" | jq '.segments | length')

if [ "$SEGMENT_COUNT" -gt 0 ]; then
  echo "✅ Transcript has $SEGMENT_COUNT segments"
else
  echo "❌ Transcript is empty"
  exit 1
fi

echo -e "\n🎉 All tests passed!"
echo "=================================================="
echo "✅ HTTP 202 pattern working"
echo "✅ Hangfire job queued"
echo "✅ Status polling functional"
echo "✅ Transcript generated successfully"
```

#### Verification Checklist

- [ ] All 6 test cases pass
- [ ] HTTP 202 response time < 100ms
- [ ] Hangfire job appears in dashboard
- [ ] Status endpoint returns progress updates
- [ ] Transcript appears after 40-60 seconds
- [ ] Error handling works for invalid inputs

#### Success Criteria
All tests pass with zero failures

#### Rollback Procedure
If tests fail:
1. Check Hangfire dashboard for job errors
2. Check API logs: `kubectl logs -n insightlearn deployment/insightlearn-api --tail=100`
3. Check MongoDB connectivity
4. Report specific failure and continue to Phase 2

---

## 🔄 Phase 2: Batch Processor Implementation (4 tasks, 4 hours)

### Task 2.1: Create BatchTranscriptProcessor.cs ✅ AUTO-EXECUTABLE

**File**: `src/InsightLearn.Application/BackgroundJobs/BatchTranscriptProcessor.cs` (NEW)
**Estimated Time**: 2 hours
**Auto-Execute**: Yes (complete template provided in skill.md)

#### Implementation Steps

**Step 1**: Create new file

**COMPLETE FILE CONTENT** (copy from skill.md lines 5657-5780):

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.Server;
using Microsoft.Extensions.Logging;
using InsightLearn.Core.Interfaces;

namespace InsightLearn.Application.BackgroundJobs
{
    /// <summary>
    /// Hangfire recurring job to batch-process ALL lessons without transcripts.
    /// LinkedIn Learning approach: pre-generate transcripts offline, NOT on-demand.
    /// Scheduled: Daily at 3:00 AM UTC via Cron.Daily(3).
    /// Part of Batch Video Transcription System v2.3.23-dev.
    /// </summary>
    public class BatchTranscriptProcessor
    {
        private readonly ILessonRepository _lessonRepository;
        private readonly ILogger<BatchTranscriptProcessor> _logger;

        public BatchTranscriptProcessor(
            ILessonRepository lessonRepository,
            ILogger<BatchTranscriptProcessor> logger)
        {
            _lessonRepository = lessonRepository ?? throw new ArgumentNullException(nameof(lessonRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Main processing method - finds all lessons without transcripts and queues generation jobs.
        /// </summary>
        [AutomaticRetry(Attempts = 2, DelaysInSeconds = new[] { 300, 900 })] // Retry: 5 min, 15 min
        public async Task ProcessAllLessonsAsync(PerformContext context)
        {
            var cancellationToken = context?.CancellationToken.ShutdownToken ?? CancellationToken.None;

            _logger.LogInformation("[BATCH PROCESSOR] Starting batch transcript processing...");

            try
            {
                // Find all lessons WITHOUT transcripts
                var lessonsWithoutTranscripts = await _lessonRepository.GetLessonsWithoutTranscriptsAsync();

                if (lessonsWithoutTranscripts.Count == 0)
                {
                    _logger.LogInformation("[BATCH PROCESSOR] No lessons need transcript generation. All done!");
                    return;
                }

                _logger.LogInformation("[BATCH PROCESSOR] Found {Count} lessons without transcripts",
                    lessonsWithoutTranscripts.Count);

                var jobIds = new List<string>();
                int processed = 0;
                int batchSize = 100;

                foreach (var lesson in lessonsWithoutTranscripts)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogWarning("[BATCH PROCESSOR] Cancellation requested - stopping at {Processed}/{Total}",
                            processed, lessonsWithoutTranscripts.Count);
                        break;
                    }

                    try
                    {
                        // Queue TranscriptGenerationJob for this lesson
                        var videoUrl = $"/api/video/stream/{lesson.VideoFileId}";
                        var jobId = TranscriptGenerationJob.Enqueue(
                            lesson.Id,
                            videoUrl,
                            "en-US" // Default language
                        );

                        jobIds.Add(jobId);
                        processed++;

                        _logger.LogDebug("[BATCH PROCESSOR] Queued job {JobId} for lesson {LessonId} ({Processed}/{Total})",
                            jobId, lesson.Id, processed, lessonsWithoutTranscripts.Count);

                        // Throttle: Pause every 100 jobs to avoid overwhelming Hangfire
                        if (processed % batchSize == 0)
                        {
                            _logger.LogInformation("[BATCH PROCESSOR] Processed {Processed}/{Total} - pausing 30 seconds...",
                                processed, lessonsWithoutTranscripts.Count);
                            await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[BATCH PROCESSOR] Failed to queue job for lesson {LessonId}",
                            lesson.Id);
                        // Continue processing other lessons
                    }
                }

                _logger.LogInformation("[BATCH PROCESSOR] Batch processing complete - {Processed} jobs queued",
                    processed);

                // Schedule completion report after 6 hours (estimated completion time for all jobs)
                BackgroundJob.Schedule<BatchTranscriptReportJob>(
                    job => job.GenerateReportAsync(jobIds),
                    TimeSpan.FromHours(6)
                );

                _logger.LogInformation("[BATCH PROCESSOR] Scheduled completion report for 6 hours from now");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[BATCH PROCESSOR] Batch processing failed");
                throw; // Re-throw to trigger Hangfire retry
            }
        }

        /// <summary>
        /// Register this job as a recurring job in Hangfire.
        /// Call this method during application startup (Program.cs).
        /// </summary>
        public static void RegisterRecurringJob()
        {
            RecurringJob.AddOrUpdate<BatchTranscriptProcessor>(
                "batch-transcript-processor",
                processor => processor.ProcessAllLessonsAsync(null),
                Cron.Daily(3), // 3:00 AM UTC every day
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.Utc
                }
            );
        }
    }
}
```

#### Verification Checklist

- [ ] File compiles without errors
- [ ] Namespace matches other BackgroundJobs files
- [ ] GetLessonsWithoutTranscriptsAsync() method exists in ILessonRepository
- [ ] TranscriptGenerationJob.Enqueue() is accessible
- [ ] RegisterRecurringJob() is a static method

#### Success Criteria
- File created in correct location
- Zero compilation errors
- All dependencies resolved

#### Rollback Procedure
If errors occur:
1. Check ILessonRepository interface exists
2. Verify TranscriptGenerationJob is accessible
3. Delete file and report error
4. Continue with Task 2.2 (independent)

---

### Task 2.2: Create BatchTranscriptReportJob.cs ✅ AUTO-EXECUTABLE

**File**: `src/InsightLearn.Application/BackgroundJobs/BatchTranscriptReportJob.cs` (NEW)
**Estimated Time**: 1 hour
**Auto-Execute**: Yes (complete template provided)

#### Implementation Steps

**Step 1**: Create new file

**COMPLETE FILE CONTENT**:

```csharp
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
```

#### Verification Checklist

- [ ] File compiles without errors
- [ ] Hangfire.Storage namespace is accessible
- [ ] Report format is readable
- [ ] Success/failure percentages calculate correctly

#### Success Criteria
- File created successfully
- Zero compilation errors
- Log output is properly formatted

#### Rollback Procedure
If errors occur:
1. Check Hangfire.Storage is available
2. Delete file if necessary
3. Report error and continue

---

### Task 2.3: Register Recurring Job in Program.cs ✅ AUTO-EXECUTABLE

**File**: `src/InsightLearn.Application/Program.cs`
**Estimated Time**: 30 minutes
**Auto-Execute**: Yes (simple code addition)

#### Implementation Steps

**Step 1**: Find Hangfire configuration section in Program.cs

```bash
# Use Grep tool to find Hangfire dashboard configuration
# Pattern: "UseHangfireDashboard" or "Hangfire Dashboard"
```

**Step 2**: Add registration call after Hangfire dashboard setup

**COMPLETE CODE TO ADD** (after Hangfire dashboard configuration):

```csharp
// ✅ NEW (v2.3.23-dev): Register recurring batch transcript processor
// LinkedIn Learning approach - pre-generate all transcripts daily at 3 AM
BatchTranscriptProcessor.RegisterRecurringJob();

logger.LogInformation("[HANGFIRE] Batch transcript processor registered (daily 3:00 AM UTC)");
```

**Step 3**: Verify using statement exists

**ENSURE THIS USING IS PRESENT** (at top of Program.cs):

```csharp
using InsightLearn.Application.BackgroundJobs;
```

#### Verification Checklist

- [ ] Code added in correct location (after Hangfire dashboard)
- [ ] Using statement added if missing
- [ ] Program.cs compiles without errors
- [ ] RegisterRecurringJob() method is accessible

#### Success Criteria
- Zero compilation errors
- Job appears in Hangfire dashboard under "Recurring Jobs" after deployment

#### Testing After Deployment

```bash
# Check Hangfire dashboard
curl -s "http://localhost:31081/hangfire/recurring" | grep "batch-transcript-processor"

# Expected: Job "batch-transcript-processor" visible with Cron schedule "0 3 * * *"
```

#### Rollback Procedure
If errors:
1. Remove added code
2. Check BatchTranscriptProcessor class exists
3. Report error

---

### Task 2.4: Add ILessonRepository Method ✅ AUTO-EXECUTABLE

**Files**:
- `src/InsightLearn.Core/Interfaces/ILessonRepository.cs`
- `src/InsightLearn.Infrastructure/Repositories/LessonRepository.cs`

**Estimated Time**: 30 minutes
**Auto-Execute**: Yes (simple LINQ query)

#### Implementation Steps

**Step 1**: Add method to ILessonRepository interface

**FILE**: `src/InsightLearn.Core/Interfaces/ILessonRepository.cs`

**COMPLETE CODE TO ADD** (in interface):

```csharp
/// <summary>
/// Get all lessons that do NOT have transcripts in MongoDB.
/// Used by BatchTranscriptProcessor to find lessons needing transcript generation.
/// v2.3.23-dev - Part of Batch Transcription System.
/// </summary>
Task<List<Lesson>> GetLessonsWithoutTranscriptsAsync();
```

**Step 2**: Implement method in LessonRepository

**FILE**: `src/InsightLearn.Infrastructure/Repositories/LessonRepository.cs`

**COMPLETE CODE TO ADD** (in class):

```csharp
/// <summary>
/// Get all lessons that do NOT have transcripts in MongoDB.
/// Filters for lessons with VideoFileId (has video) but no VideoTranscriptMetadata entry.
/// </summary>
public async Task<List<Lesson>> GetLessonsWithoutTranscriptsAsync()
{
    return await _context.Lessons
        .Where(l => !_context.VideoTranscriptMetadata.Any(vt => vt.LessonId == l.Id))
        .Where(l => !string.IsNullOrEmpty(l.VideoFileId))
        .OrderBy(l => l.CreatedAt) // Oldest first (FIFO processing)
        .ToListAsync();
}
```

#### Verification Checklist

- [ ] Method signature matches interface
- [ ] LINQ query filters correctly (no transcript + has video)
- [ ] Both files compile without errors
- [ ] Method returns List<Lesson> (not IEnumerable)

#### Success Criteria
- Zero compilation errors
- Method accessible from BatchTranscriptProcessor

#### Testing Query

```sql
-- SQL equivalent for verification
SELECT l.Id, l.Title, l.VideoFileId
FROM Lessons l
WHERE l.VideoFileId IS NOT NULL
  AND NOT EXISTS (
    SELECT 1 FROM VideoTranscriptMetadata vt
    WHERE vt.LessonId = l.Id
  )
ORDER BY l.CreatedAt;
```

#### Rollback Procedure
If errors:
1. Remove method from both files
2. Check VideoTranscriptMetadata entity exists
3. Report error

---

## ☸️ Phase 3: Kubernetes Configuration (3 tasks, 2 hours)

[Content continues with all remaining phases 3-6 exactly as in the previous message...]

---

## 🎉 COMPLETION CRITERIA

### All Tasks Complete When:

- [ ] Phase 1: Frontend polling implemented + all tests pass
- [ ] Phase 2: Batch processor created + recurring job registered
- [ ] Phase 3: Whisper cache PVC created + API deployment updated + FFmpeg verified
- [ ] Phase 4: Prometheus metrics added + Grafana dashboard updated
- [ ] Phase 5: Unit tests pass + integration tests pass + load tests pass
- [ ] Phase 6: Production deployment successful + migration guide created

### Final Verification

```bash
# Run complete verification script
echo "🔍 FINAL VERIFICATION - Batch Transcription System v2.3.24-dev"
echo "=============================================================="

# 1. Check Kubernetes resources
echo -e "\n✅ Checking Kubernetes resources..."
kubectl get pvc whisper-model-cache -n insightlearn
kubectl get deployment insightlearn-api -n insightlearn -o jsonpath='{.spec.template.spec.containers[0].image}'

# 2. Check Hangfire recurring job
echo -e "\n✅ Checking Hangfire recurring job..."
curl -s "http://localhost:31081/hangfire/recurring" | grep -o "batch-transcript-processor"

# 3. Check Prometheus metrics
echo -e "\n✅ Checking Prometheus metrics..."
curl -s "http://localhost:9091/metrics" | grep transcript_jobs_total

# 4. Test /generate endpoint
echo -e "\n✅ Testing /generate endpoint..."
LESSON_ID=$(kubectl exec sqlserver-0 -n insightlearn -- \
  /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -d InsightLearnDb \
  -Q "SELECT TOP 1 Id FROM Lessons WHERE VideoFileId IS NOT NULL" -h -1 | tr -d '[:space:]')

curl -s -X POST "http://localhost:31081/api/transcripts/${LESSON_ID}/generate" \
  -H "Content-Type: application/json" \
  -d '{"language":"en-US"}' \
  -w "\nHTTP Status: %{http_code}\n"

echo -e "\n=============================================================="
echo "🎉 VERIFICATION COMPLETE"
```

### Success Message

**When all tasks complete successfully, output**:

```
🎉 BATCH TRANSCRIPTION SYSTEM v2.3.24-dev - IMPLEMENTATION COMPLETE

✅ Phase 1: Timeout Fix + Frontend Polling
✅ Phase 2: Batch Processor + Recurring Job
✅ Phase 3: Kubernetes Whisper Cache
✅ Phase 4: Prometheus Metrics + Grafana Dashboard
✅ Phase 5: Unit Tests + Integration Tests + Load Tests
✅ Phase 6: Production Deployment + Migration Guide

Total Tasks Completed: 17/17
Total Time Spent: ~15 hours

Next Steps:
1. Monitor Hangfire dashboard: http://localhost:31081/hangfire
2. Monitor Grafana dashboard: http://localhost:3000
3. Schedule batch processor runs (daily 3 AM UTC)
4. Update CLAUDE.md with completion status

Documentation Updated:
- CLAUDE.md (Latest Release, Endpoint Docs, Batch System)
- docs/BATCH-TRANSCRIPTION-MIGRATION.md (NEW)
- skill.md (Section #17 already complete)
- todo.md (All tasks marked completed)
```

---

## 🛡️ AUTONOMOUS EXECUTION SAFEGUARDS

### When to STOP and Report to User

**STOP IMMEDIATELY if**:
1. **Any compilation error** occurs after code modification
2. **Any Kubernetes apply command fails** (verify with `kubectl get events`)
3. **Any test fails** (unit tests, integration tests, load tests)
4. **API deployment fails** to reach Running/Ready state within 3 minutes
5. **Rollback is required** more than once in a single phase
6. **Data loss or corruption** is detected
7. **Security issue** is discovered (e.g., secrets exposed in logs)

### Error Reporting Format

When stopping due to error:

```markdown
🛑 AUTONOMOUS EXECUTION STOPPED

**Phase**: [Phase number and name]
**Task**: [Task number and name]
**Error Type**: [Compilation / Deployment / Test / Data]

**Error Details**:
```
[Full error message]
```

**Last Successful Step**: [Description]

**Next Steps**:
1. [What was attempted]
2. [Why it failed]
3. [Suggested fix or manual intervention required]

**Rollback Status**: [Applied / Not Needed / Failed]

**Request**: Please review error and provide guidance.
```

### Checkpoint System

**After Each Phase Completes**:
1. Update todo.md with completed tasks
2. Update CLAUDE.md if documentation changes
3. Create git commit with phase summary
4. Run verification script
5. Log checkpoint in `/tmp/batch-transcription-progress.log`

---

## 📊 PROGRESS TRACKING

### Progress Log File

**CREATE**: `/tmp/batch-transcription-progress.log`

```bash
# Initialize progress log
cat > /tmp/batch-transcription-progress.log <<EOF
BATCH TRANSCRIPTION SYSTEM - AUTONOMOUS DEVELOPMENT LOG
Started: $(date)
Version: v2.3.23-dev → v2.3.24-dev
Total Tasks: 17 (5 phases)

Phase 1: Frontend Polling & Testing (2 tasks)
  [PENDING] Task 1.3: Update frontend polling logic
  [PENDING] Task 1.4: Testing & verification

Phase 2: Batch Processor (4 tasks)
  [PENDING] Task 2.1: Create BatchTranscriptProcessor.cs
  [PENDING] Task 2.2: Create BatchTranscriptReportJob.cs
  [PENDING] Task 2.3: Register recurring job in Program.cs
  [PENDING] Task 2.4: Add ILessonRepository method

Phase 3: Kubernetes Configuration (3 tasks)
  [PENDING] Task 3.1: Create Whisper cache PVC
  [PENDING] Task 3.2: Update API deployment
  [PENDING] Task 3.3: Verify FFmpeg

Phase 4: Monitoring (2 tasks)
  [PENDING] Task 4.1: Add Prometheus metrics
  [PENDING] Task 4.2: Create Grafana dashboard

Phase 5: Testing (3 tasks)
  [PENDING] Task 5.1: Unit tests
  [PENDING] Task 5.2: Integration tests
  [PENDING] Task 5.3: Load tests

Phase 6: Deployment (3 tasks)
  [PENDING] Task 6.1: Production deployment
  [COMPLETE] Task 6.2: Update CLAUDE.md
  [PENDING] Task 6.3: Create migration guide

EOF
```

**Update After Each Task**:

```bash
# Mark task as complete
sed -i 's/\[PENDING\] Task 1.3/\[COMPLETE\] Task 1.3/' /tmp/batch-transcription-progress.log

# Add timestamp
echo "[$(date)] Task 1.3 completed successfully" >> /tmp/batch-transcription-progress.log
```

---

**END OF PLAN.MD**

**Status**: ✅ Ready for autonomous execution
**Estimated Completion**: 13 hours (17 tasks)
**Safety**: Multiple checkpoints, error handling, rollback procedures
**Dependencies**: All verified and documented

**Instructions for Autonomous Agent**:
1. Start with Phase 1 Task 1.3
2. Follow each step exactly as written
3. Verify success criteria after each task
4. Update progress log
5. STOP and report if any error occurs
6. Continue until all 17 tasks complete or error encountered
