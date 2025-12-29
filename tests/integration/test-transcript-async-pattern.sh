#!/bin/bash
# Test script for Phase 1 Task 1.4: Testing & Verification
# Tests HTTP 202 Accepted pattern for transcript generation
# Date: 2025-12-28

set -e  # Exit on error

# Configuration
API_BASE_URL="http://localhost:31081"
TEST_LESSON_ID="c7da6be6-630e-44f5-9b67-b3634df77bd7"  # Lesson 1 - Video (Load Test Course 3)
AUTH_TOKEN="${AUTH_TOKEN:-}"  # Use environment variable if set, otherwise empty
RESULTS_FILE="/tmp/transcript-test-results-$(date +%Y%m%d-%H%M%S).txt"

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Test counters
TESTS_TOTAL=0
TESTS_PASSED=0
TESTS_FAILED=0

echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}Transcript Async Pattern Test Suite${NC}"
echo -e "${BLUE}Phase 1 Task 1.4: Testing & Verification${NC}"
echo -e "${BLUE}========================================${NC}"
echo ""

# Function to log test results
log_test() {
    local test_name="$1"
    local result="$2"
    local details="$3"

    TESTS_TOTAL=$((TESTS_TOTAL + 1))

    if [ "$result" = "PASS" ]; then
        TESTS_PASSED=$((TESTS_PASSED + 1))
        echo -e "${GREEN}✓ PASS${NC}: $test_name"
        echo "  $details"
    else
        TESTS_FAILED=$((TESTS_FAILED + 1))
        echo -e "${RED}✗ FAIL${NC}: $test_name"
        echo -e "${RED}  $details${NC}"
    fi
    echo ""

    # Append to results file
    echo "[$result] $test_name" >> "$RESULTS_FILE"
    echo "  $details" >> "$RESULTS_FILE"
    echo "" >> "$RESULTS_FILE"
}

# Test 1: API Health Check
echo -e "${YELLOW}Test 1: API Health Check${NC}"
HEALTH_RESPONSE=$(curl -s -w "\n%{http_code}" "${API_BASE_URL}/health" || echo "000")
HEALTH_CODE=$(echo "$HEALTH_RESPONSE" | tail -n 1)

if [ "$HEALTH_CODE" = "200" ]; then
    log_test "API Health Check" "PASS" "API is responding (HTTP $HEALTH_CODE)"
else
    log_test "API Health Check" "FAIL" "API is not responding (HTTP $HEALTH_CODE)"
    echo -e "${RED}Cannot continue testing - API is not available${NC}"
    exit 1
fi

# Test 2: HTTP 202 Response Time
echo -e "${YELLOW}Test 2: HTTP 202 Response Time (should be < 100ms)${NC}"

# Prepare test request
REQUEST_BODY=$(cat <<EOF
{
  "lessonTitle": "Test Video for Transcription",
  "language": "en-US",
  "videoUrl": "/api/video/stream/${TEST_LESSON_ID}",
  "durationSeconds": 300
}
EOF
)

# Measure response time
START_TIME=$(date +%s%3N)  # Milliseconds

# Prepare auth header if token is set
if [ -n "$AUTH_TOKEN" ]; then
    RESPONSE=$(curl -s -w "\n%{http_code}\n%{time_total}" \
        -X POST "${API_BASE_URL}/api/transcripts/${TEST_LESSON_ID}/generate" \
        -H "Content-Type: application/json" \
        -H "Authorization: Bearer $AUTH_TOKEN" \
        -d "$REQUEST_BODY" || echo "000\n0")
else
    RESPONSE=$(curl -s -w "\n%{http_code}\n%{time_total}" \
        -X POST "${API_BASE_URL}/api/transcripts/${TEST_LESSON_ID}/generate" \
        -H "Content-Type: application/json" \
        -d "$REQUEST_BODY" || echo "000\n0")
fi

HTTP_CODE=$(echo "$RESPONSE" | tail -n 2 | head -n 1)
TIME_TOTAL=$(echo "$RESPONSE" | tail -n 1)
RESPONSE_BODY=$(echo "$RESPONSE" | head -n -2)

# Convert time to milliseconds (curl returns seconds with decimals)
TIME_MS=$(echo "$TIME_TOTAL * 1000" | bc | cut -d. -f1)

if [ "$HTTP_CODE" = "202" ] || [ "$HTTP_CODE" = "200" ]; then
    if [ "$TIME_MS" -lt 100 ]; then
        log_test "HTTP 202 Response Time" "PASS" "Response in ${TIME_MS}ms (HTTP $HTTP_CODE) - Target: < 100ms"
    else
        log_test "HTTP 202 Response Time" "FAIL" "Response in ${TIME_MS}ms (HTTP $HTTP_CODE) - Exceeds 100ms target"
    fi

    # Extract JobId if HTTP 202
    if [ "$HTTP_CODE" = "202" ]; then
        JOB_ID=$(echo "$RESPONSE_BODY" | grep -oP '"JobId":\s*"\K[^"]+' || echo "")
        echo -e "  ${BLUE}JobId: $JOB_ID${NC}"
    fi
else
    log_test "HTTP 202 Response Time" "FAIL" "Unexpected HTTP code: $HTTP_CODE"
fi

# Test 3: Response Structure Validation
echo -e "${YELLOW}Test 3: HTTP 202 Response Structure Validation${NC}"

if [ "$HTTP_CODE" = "202" ]; then
    # Check required fields in response
    HAS_LESSON_ID=$(echo "$RESPONSE_BODY" | grep -q '"LessonId"' && echo "true" || echo "false")
    HAS_JOB_ID=$(echo "$RESPONSE_BODY" | grep -q '"JobId"' && echo "true" || echo "false")
    HAS_STATUS=$(echo "$RESPONSE_BODY" | grep -q '"Status"' && echo "true" || echo "false")
    HAS_MESSAGE=$(echo "$RESPONSE_BODY" | grep -q '"Message"' && echo "true" || echo "false")

    if [ "$HAS_LESSON_ID" = "true" ] && [ "$HAS_JOB_ID" = "true" ] && [ "$HAS_STATUS" = "true" ] && [ "$HAS_MESSAGE" = "true" ]; then
        log_test "Response Structure Validation" "PASS" "All required fields present (LessonId, JobId, Status, Message)"
    else
        MISSING_FIELDS=""
        [ "$HAS_LESSON_ID" = "false" ] && MISSING_FIELDS="$MISSING_FIELDS LessonId"
        [ "$HAS_JOB_ID" = "false" ] && MISSING_FIELDS="$MISSING_FIELDS JobId"
        [ "$HAS_STATUS" = "false" ] && MISSING_FIELDS="$MISSING_FIELDS Status"
        [ "$HAS_MESSAGE" = "false" ] && MISSING_FIELDS="$MISSING_FIELDS Message"
        log_test "Response Structure Validation" "FAIL" "Missing fields:$MISSING_FIELDS"
    fi
elif [ "$HTTP_CODE" = "200" ]; then
    log_test "Response Structure Validation" "PASS" "HTTP 200 - Existing transcript returned (cache hit)"
else
    log_test "Response Structure Validation" "FAIL" "Cannot validate - unexpected HTTP code: $HTTP_CODE"
fi

# Test 4: Status Polling Endpoint
echo -e "${YELLOW}Test 4: Status Polling Endpoint${NC}"

if [ -n "$AUTH_TOKEN" ]; then
    STATUS_RESPONSE=$(curl -s -w "\n%{http_code}" -H "Authorization: Bearer $AUTH_TOKEN" "${API_BASE_URL}/api/transcripts/${TEST_LESSON_ID}/status" || echo "000")
else
    STATUS_RESPONSE=$(curl -s -w "\n%{http_code}" "${API_BASE_URL}/api/transcripts/${TEST_LESSON_ID}/status" || echo "000")
fi
STATUS_CODE=$(echo "$STATUS_RESPONSE" | tail -n 1)
STATUS_BODY=$(echo "$STATUS_RESPONSE" | head -n -1)

if [ "$STATUS_CODE" = "200" ]; then
    STATUS_VALUE=$(echo "$STATUS_BODY" | grep -oP '"Status":\s*"\K[^"]+' || echo "")
    log_test "Status Polling Endpoint" "PASS" "Status endpoint responding (HTTP $STATUS_CODE, Status: $STATUS_VALUE)"
elif [ "$STATUS_CODE" = "404" ]; then
    log_test "Status Polling Endpoint" "PASS" "HTTP 404 - No processing found (expected if transcript already exists)"
else
    log_test "Status Polling Endpoint" "FAIL" "Unexpected HTTP code: $STATUS_CODE"
fi

# Test 5: Polling Loop Simulation (max 10 attempts, 2-second intervals)
echo -e "${YELLOW}Test 5: Polling Loop Simulation (10 attempts, 2s intervals)${NC}"

MAX_ATTEMPTS=10
INTERVAL=2
POLLING_SUCCESS="false"
ATTEMPT=0

echo -e "  ${BLUE}Starting polling loop...${NC}"

while [ $ATTEMPT -lt $MAX_ATTEMPTS ]; do
    ATTEMPT=$((ATTEMPT + 1))
    echo -e "  ${BLUE}Attempt $ATTEMPT/$MAX_ATTEMPTS${NC}"

    if [ -n "$AUTH_TOKEN" ]; then
        STATUS_RESPONSE=$(curl -s -w "\n%{http_code}" -H "Authorization: Bearer $AUTH_TOKEN" "${API_BASE_URL}/api/transcripts/${TEST_LESSON_ID}/status" || echo "000")
    else
        STATUS_RESPONSE=$(curl -s -w "\n%{http_code}" "${API_BASE_URL}/api/transcripts/${TEST_LESSON_ID}/status" || echo "000")
    fi
    STATUS_CODE=$(echo "$STATUS_RESPONSE" | tail -n 1)
    STATUS_BODY=$(echo "$STATUS_RESPONSE" | head -n -1)

    if [ "$STATUS_CODE" = "200" ]; then
        STATUS_VALUE=$(echo "$STATUS_BODY" | grep -oP '"Status":\s*"\K[^"]+' || echo "")
        echo -e "  ${BLUE}Status: $STATUS_VALUE${NC}"

        # Check for completion
        if [ "$STATUS_VALUE" = "completed" ] || [ "$STATUS_VALUE" = "success" ]; then
            echo -e "  ${GREEN}✓ Transcript generation completed${NC}"
            POLLING_SUCCESS="true"
            break
        elif [ "$STATUS_VALUE" = "failed" ] || [ "$STATUS_VALUE" = "error" ]; then
            echo -e "  ${RED}✗ Transcript generation failed${NC}"
            break
        fi
    elif [ "$STATUS_CODE" = "404" ]; then
        # Transcript might already exist - try to fetch it
        if [ -n "$AUTH_TOKEN" ]; then
            TRANSCRIPT_RESPONSE=$(curl -s -w "\n%{http_code}" -H "Authorization: Bearer $AUTH_TOKEN" "${API_BASE_URL}/api/transcripts/${TEST_LESSON_ID}" || echo "000")
        else
            TRANSCRIPT_RESPONSE=$(curl -s -w "\n%{http_code}" "${API_BASE_URL}/api/transcripts/${TEST_LESSON_ID}" || echo "000")
        fi
        TRANSCRIPT_CODE=$(echo "$TRANSCRIPT_RESPONSE" | tail -n 1)

        if [ "$TRANSCRIPT_CODE" = "200" ]; then
            echo -e "  ${GREEN}✓ Transcript already exists (cache hit)${NC}"
            POLLING_SUCCESS="true"
            break
        fi
    fi

    # Wait before next attempt (except on last attempt)
    if [ $ATTEMPT -lt $MAX_ATTEMPTS ]; then
        sleep $INTERVAL
    fi
done

if [ "$POLLING_SUCCESS" = "true" ]; then
    log_test "Polling Loop Simulation" "PASS" "Completed in $ATTEMPT attempts ($(($ATTEMPT * $INTERVAL)) seconds)"
else
    log_test "Polling Loop Simulation" "FAIL" "Did not complete after $MAX_ATTEMPTS attempts ($(($MAX_ATTEMPTS * $INTERVAL)) seconds)"
fi

# Test 6: Hangfire Dashboard Accessibility (Optional)
echo -e "${YELLOW}Test 6: Hangfire Dashboard Accessibility${NC}"

HANGFIRE_RESPONSE=$(curl -s -w "\n%{http_code}" "${API_BASE_URL}/hangfire" || echo "000")
HANGFIRE_CODE=$(echo "$HANGFIRE_RESPONSE" | tail -n 1)

if [ "$HANGFIRE_CODE" = "200" ] || [ "$HANGFIRE_CODE" = "302" ]; then
    log_test "Hangfire Dashboard" "PASS" "Dashboard accessible (HTTP $HANGFIRE_CODE)"
    echo -e "  ${BLUE}View jobs at: ${API_BASE_URL}/hangfire${NC}"
elif [ "$HANGFIRE_CODE" = "401" ] || [ "$HANGFIRE_CODE" = "403" ]; then
    log_test "Hangfire Dashboard" "PASS" "Dashboard protected by authentication (HTTP $HANGFIRE_CODE)"
else
    log_test "Hangfire Dashboard" "FAIL" "Dashboard not accessible (HTTP $HANGFIRE_CODE)"
fi

# Test Summary
echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}Test Summary${NC}"
echo -e "${BLUE}========================================${NC}"
echo -e "Total Tests: $TESTS_TOTAL"
echo -e "${GREEN}Passed: $TESTS_PASSED${NC}"
echo -e "${RED}Failed: $TESTS_FAILED${NC}"
echo ""
echo -e "Results saved to: ${BLUE}$RESULTS_FILE${NC}"

# Overall result
if [ $TESTS_FAILED -eq 0 ]; then
    echo -e "${GREEN}✓ ALL TESTS PASSED${NC}"
    exit 0
else
    echo -e "${RED}✗ SOME TESTS FAILED${NC}"
    exit 1
fi
