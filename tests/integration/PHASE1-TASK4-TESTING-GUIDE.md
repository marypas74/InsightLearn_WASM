# Phase 1 Task 1.4: Testing & Verification Guide

**Date**: 2025-12-28
**Phase**: 1 - Fix Transcript Generation Timeout
**Task**: 1.4 - Testing & Verification
**Status**: Ready to execute
**Estimated Time**: 30 minutes

## Overview

This guide provides comprehensive testing procedures for verifying the HTTP 202 Accepted pattern implementation for transcript generation. The implementation fixes the 30-second timeout issue by using Hangfire background jobs.

## Architecture

```
Frontend (Blazor WASM)
    ↓
POST /api/transcripts/{lessonId}/generate
    ↓
API checks cache (< 50ms)
    ↓
If exists: HTTP 200 + transcript data
If not exists: Queue Hangfire job → HTTP 202
    ↓
Frontend polls /api/transcripts/{lessonId}/status every 2s
    ↓
Hangfire job executes (40-60s in background)
    ↓
Frontend receives status: "completed" | "failed" | "processing"
    ↓
On completion: Fetch transcript via GET /api/transcripts/{lessonId}
```

## Prerequisites

1. **API Running**: InsightLearn API must be running on port 31081
   ```bash
   kubectl get pods -n insightlearn -l app=insightlearn-api
   # Expected: 1/1 Running
   ```

2. **Hangfire Active**: Hangfire service must be operational
   ```bash
   curl http://localhost:31081/hangfire
   # Expected: HTTP 200 or 302 (redirect to login)
   ```

3. **Test Lesson**: A lesson with a video file must exist in the database
   ```bash
   # Find a test lesson ID
   kubectl exec sqlserver-0 -n insightlearn -- \
     /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'PASSWORD' -C \
     -d InsightLearnDb \
     -Q "SELECT TOP 1 Id, Title FROM Lessons WHERE VideoFileId IS NOT NULL"
   ```

4. **Optional**: Authentication token if API requires authorization
   ```bash
   # Get token via login endpoint
   curl -X POST http://localhost:31081/api/auth/login \
     -H "Content-Type: application/json" \
     -d '{"email":"admin@insightlearn.cloud","password":"PASSWORD"}'
   ```

## Test Suite

### Automated Testing

The test suite is available as a shell script that runs all 6 tests automatically.

**Location**: `tests/integration/test-transcript-async-pattern.sh`

**Execution**:
```bash
# Navigate to project root
cd /home/mpasqui/insightlearn_WASM/InsightLearn_WASM

# Set test lesson ID (replace with actual ID)
export TEST_LESSON_ID="YOUR-LESSON-GUID-HERE"

# Run test suite
./tests/integration/test-transcript-async-pattern.sh
```

**Expected Output**:
```
========================================
Transcript Async Pattern Test Suite
Phase 1 Task 1.4: Testing & Verification
========================================

✓ PASS: API Health Check
  API is responding (HTTP 200)

✓ PASS: HTTP 202 Response Time
  Response in 87ms (HTTP 202) - Target: < 100ms
  JobId: abc123...

✓ PASS: Response Structure Validation
  All required fields present (LessonId, JobId, Status, Message)

✓ PASS: Status Polling Endpoint
  Status endpoint responding (HTTP 200, Status: processing)

✓ PASS: Polling Loop Simulation
  Completed in 5 attempts (10 seconds)

✓ PASS: Hangfire Dashboard
  Dashboard accessible (HTTP 200)

========================================
Test Summary
========================================
Total Tests: 6
Passed: 6
Failed: 0

✓ ALL TESTS PASSED
```

### Test 1: API Health Check

**Purpose**: Verify API is running and responding
**Expected**: HTTP 200
**Manual Test**:
```bash
curl http://localhost:31081/health
# Expected: {"status":"Healthy"}
```

### Test 2: HTTP 202 Response Time

**Purpose**: Verify endpoint responds in < 100ms (no synchronous blocking)
**Expected**: HTTP 202 (or 200 if cached), response time < 100ms
**Manual Test**:
```bash
time curl -X POST http://localhost:31081/api/transcripts/YOUR-LESSON-ID/generate \
  -H "Content-Type: application/json" \
  -d '{
    "lessonTitle": "Test Video",
    "language": "en-US",
    "videoUrl": "/api/video/stream/YOUR-LESSON-ID",
    "durationSeconds": 300
  }'

# Expected response (HTTP 202):
# {
#   "LessonId": "...",
#   "JobId": "...",
#   "Status": "Processing",
#   "Message": "Transcript generation started. Poll /api/transcripts/{lessonId}/status for updates.",
#   "EstimatedCompletionSeconds": 120
# }
```

**Success Criteria**:
- Response time < 100ms
- HTTP status 202 (or 200 if transcript already exists)
- Response includes JobId

### Test 3: Response Structure Validation

**Purpose**: Verify HTTP 202 response contains all required fields
**Expected**: LessonId, JobId, Status, Message fields present
**Manual Test**:
```bash
RESPONSE=$(curl -s -X POST http://localhost:31081/api/transcripts/YOUR-LESSON-ID/generate \
  -H "Content-Type: application/json" \
  -d '{"lessonTitle":"Test","language":"en-US"}')

echo "$RESPONSE" | jq .

# Verify fields exist:
echo "$RESPONSE" | jq 'has("LessonId")'   # Should be true
echo "$RESPONSE" | jq 'has("JobId")'      # Should be true
echo "$RESPONSE" | jq 'has("Status")'     # Should be true
echo "$RESPONSE" | jq 'has("Message")'    # Should be true
```

### Test 4: Status Polling Endpoint

**Purpose**: Verify /status endpoint returns processing status
**Expected**: HTTP 200 with status value ("processing", "completed", "failed")
**Manual Test**:
```bash
curl http://localhost:31081/api/transcripts/YOUR-LESSON-ID/status

# Expected response:
# {
#   "Status": "processing",  # or "completed", "failed", "error"
#   "Progress": 0.5,         # Optional
#   "ErrorMessage": null,    # Optional
#   "CompletedAt": null      # Optional
# }
```

### Test 5: Polling Loop Simulation

**Purpose**: Verify complete async workflow from job queue to completion
**Expected**: Status transitions from "processing" to "completed" within 2 minutes
**Manual Test**:
```bash
# 1. Queue job
curl -X POST http://localhost:31081/api/transcripts/YOUR-LESSON-ID/generate \
  -H "Content-Type: application/json" \
  -d '{"lessonTitle":"Test","language":"en-US"}'

# 2. Poll status every 2 seconds (max 60 attempts)
for i in {1..60}; do
  echo "Attempt $i/60"
  STATUS=$(curl -s http://localhost:31081/api/transcripts/YOUR-LESSON-ID/status | jq -r '.Status')
  echo "  Status: $STATUS"

  if [ "$STATUS" = "completed" ] || [ "$STATUS" = "success" ]; then
    echo "✓ Transcript generation completed"
    break
  elif [ "$STATUS" = "failed" ] || [ "$STATUS" = "error" ]; then
    echo "✗ Transcript generation failed"
    break
  fi

  sleep 2
done

# 3. Fetch completed transcript
curl http://localhost:31081/api/transcripts/YOUR-LESSON-ID
```

### Test 6: Hangfire Dashboard Verification

**Purpose**: Verify Hangfire job appears in dashboard and executes
**Expected**: Job visible in Hangfire UI with "Succeeded" status
**Manual Test**:
```bash
# 1. Access Hangfire dashboard
open http://localhost:31081/hangfire

# 2. Navigate to "Jobs" → "Enqueued" or "Processing"
# 3. Verify job with ID from Test 2 is visible
# 4. Wait for job to complete (status changes to "Succeeded")
```

**Dashboard Verification Checklist**:
- [ ] Job appears in "Enqueued" queue immediately after queueing
- [ ] Job moves to "Processing" state when executed
- [ ] Job completes within 2 minutes (for 5-min video)
- [ ] Job shows "Succeeded" status after completion
- [ ] No "Failed" or "Retry" states

## Failure Scenarios Testing

### Scenario 1: Invalid Lesson ID

**Purpose**: Verify graceful error handling for non-existent lessons
**Test**:
```bash
curl -X POST http://localhost:31081/api/transcripts/00000000-0000-0000-0000-000000000000/generate \
  -H "Content-Type: application/json" \
  -d '{"lessonTitle":"Test","language":"en-US"}'

# Expected: HTTP 404 or 500 with error message
```

### Scenario 2: Missing Required Fields

**Purpose**: Verify validation of request body
**Test**:
```bash
curl -X POST http://localhost:31081/api/transcripts/YOUR-LESSON-ID/generate \
  -H "Content-Type: application/json" \
  -d '{}'

# Expected: HTTP 400 Bad Request with validation errors
```

### Scenario 3: Hangfire Service Down

**Purpose**: Verify error handling when background job system is unavailable
**Test**:
```bash
# 1. Stop Hangfire (e.g., by stopping API)
kubectl scale deployment insightlearn-api -n insightlearn --replicas=0

# 2. Attempt to queue job
curl -X POST http://localhost:31081/api/transcripts/YOUR-LESSON-ID/generate \
  -H "Content-Type: application/json" \
  -d '{"lessonTitle":"Test","language":"en-US"}'

# Expected: HTTP 500 with error message

# 3. Restore Hangfire
kubectl scale deployment insightlearn-api -n insightlearn --replicas=1
```

### Scenario 4: Timeout (60 Attempts Exceeded)

**Purpose**: Verify frontend handles timeout gracefully
**Expected**: Frontend returns HTTP 408 after 2 minutes (60 attempts × 2s)
**Test**:
```bash
# Simulate by polling a lesson that never completes
# (This would be tested in frontend integration tests)
```

## Success Criteria

All tests must pass with the following criteria:

| Test | Criterion | Target |
|------|-----------|--------|
| API Health | HTTP 200 | ✓ |
| Response Time | < 100ms | ✓ |
| HTTP Status | 202 or 200 | ✓ |
| Response Fields | LessonId, JobId, Status, Message | ✓ |
| Polling Endpoint | HTTP 200 with valid status | ✓ |
| Job Completion | Within 2 minutes for 5-min video | ✓ |
| Hangfire Dashboard | Job visible and succeeds | ✓ |

## Performance Benchmarks

Expected performance metrics:

| Metric | Target | Actual (Example) |
|--------|--------|------------------|
| `/generate` endpoint response time | < 100ms | 87ms |
| `/status` endpoint response time | < 50ms | 32ms |
| Hangfire job start delay | < 5 seconds | 2.1s |
| Transcript generation (5-min video) | < 2 minutes | 95s |
| Cache hit response time | < 50ms | 28ms |

## Troubleshooting

### Issue: HTTP 500 on /generate

**Possible Causes**:
- Hangfire service not running
- Database connection issues
- Invalid lesson ID

**Resolution**:
```bash
# Check API logs
kubectl logs -n insightlearn -l app=insightlearn-api --tail=50

# Check Hangfire dashboard
curl http://localhost:31081/hangfire

# Verify lesson exists
kubectl exec sqlserver-0 -n insightlearn -- \
  /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'PASSWORD' -C \
  -d InsightLearnDb \
  -Q "SELECT Id, Title FROM Lessons WHERE Id = 'YOUR-LESSON-ID'"
```

### Issue: Polling Never Completes

**Possible Causes**:
- Hangfire worker not processing jobs
- Whisper model not downloaded
- Video file not accessible

**Resolution**:
```bash
# Check Hangfire workers
curl http://localhost:31081/hangfire/servers

# Check Whisper cache volume
kubectl exec -n insightlearn -l app=insightlearn-api -- ls -la /root/.cache/whisper

# Verify video file in MongoDB
kubectl exec mongodb-0 -n insightlearn -- mongosh \
  -u insightlearn -p PASSWORD --authenticationDatabase admin insightlearn_videos \
  --eval "db.videos.files.find({_id: ObjectId('YOUR-VIDEO-ID')}).count()"
```

### Issue: Timeout After 2 Minutes

**Possible Causes**:
- Video too long (> 10 minutes)
- Whisper model slow (CPU-only inference)
- Background job stuck

**Resolution**:
```bash
# Check job status in Hangfire dashboard
curl http://localhost:31081/hangfire/jobs/enqueued

# Check job logs
kubectl logs -n insightlearn -l app=insightlearn-api --tail=100 | grep TRANSCRIPT

# Increase timeout in frontend (VideoTranscriptClientService.cs line 72)
# const int maxAttempts = 120;  // 4 minutes instead of 2
```

## Completion Checklist

Phase 1 Task 1.4 is complete when:

- [ ] All 6 automated tests pass
- [ ] HTTP 202 response time consistently < 100ms
- [ ] Polling completes successfully within 2 minutes
- [ ] Hangfire job appears in dashboard and succeeds
- [ ] Failure scenarios handled gracefully
- [ ] No timeout errors in frontend
- [ ] Performance benchmarks met
- [ ] Documentation updated in todo.md

## Next Steps

After completing Phase 1 Task 1.4:

1. **Update todo.md**: Mark Task 1.4 as "✅ COMPLETED"
2. **Update Phase 1 header**: Change status to "100% Complete (4/4 tasks done)"
3. **Deploy to production**:
   ```bash
   # Build new image with v2.3.24-dev tag
   podman build -f Dockerfile.wasm -t localhost/insightlearn/wasm:2.3.24-dev .

   # Deploy to K3s
   kubectl set image deployment/insightlearn-wasm-blazor-webassembly -n insightlearn \
     wasm-blazor=localhost/insightlearn/wasm:2.3.24-dev
   ```
4. **Proceed to Phase 7**: Complete subtitle endpoint implementation (1.5 hours)

## References

- [todo.md - Phase 1](../../todo.md#phase-1-fix-transcript-generation-timeout-priority-critical)
- [KANBAN-STATUS-REPORT-2025-12-28.md](../../KANBAN-STATUS-REPORT-2025-12-28.md)
- [VideoTranscriptClientService.cs](../../src/InsightLearn.WebAssembly/Services/LearningSpace/VideoTranscriptClientService.cs)
- [Program.cs - Transcript endpoints](../../src/InsightLearn.Application/Program.cs)
- [TranscriptGenerationJob.cs](../../src/InsightLearn.Application/BackgroundJobs/TranscriptGenerationJob.cs)
