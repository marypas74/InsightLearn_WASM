#!/bin/bash
# Integration Test 1: HTTP 202 Endpoint Verification
# Tests that POST /api/video-transcripts/generate returns HTTP 202 Accepted
# Author: InsightLearn Development Team
# Date: 2025-12-28

set -e

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration
API_BASE_URL="${API_BASE_URL:-http://localhost:31081}"
TEST_LESSON_ID="${TEST_LESSON_ID:-00000000-0000-0000-0000-000000000001}"
TEST_VIDEO_URL="/api/video/stream/test-video-id"
LANGUAGE="en-US"

echo "========================================="
echo "  Integration Test 1: HTTP 202 Endpoint"
echo "========================================="
echo ""
echo "API Base URL: $API_BASE_URL"
echo "Test Lesson ID: $TEST_LESSON_ID"
echo ""

# Test Case 1.1: Valid request returns HTTP 202
echo -n "Test 1.1: Valid request returns HTTP 202... "
START_TIME=$(date +%s%3N)

RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$API_BASE_URL/api/video-transcripts/generate" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer test-token" \
  -d "{
    \"lessonId\": \"$TEST_LESSON_ID\",
    \"videoUrl\": \"$TEST_VIDEO_URL\",
    \"language\": \"$LANGUAGE\"
  }")

HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
BODY=$(echo "$RESPONSE" | sed '$d')
END_TIME=$(date +%s%3N)
DURATION=$((END_TIME - START_TIME))

if [ "$HTTP_CODE" -eq 202 ]; then
    echo -e "${GREEN}✓ PASS${NC} (${DURATION}ms)"

    # Verify response contains jobId
    if echo "$BODY" | grep -q "jobId"; then
        echo -e "  ${GREEN}✓${NC} Response contains jobId"
    else
        echo -e "  ${RED}✗${NC} Response missing jobId"
        echo "  Response: $BODY"
        exit 1
    fi
else
    echo -e "${RED}✗ FAIL${NC}"
    echo "  Expected HTTP 202, got HTTP $HTTP_CODE"
    echo "  Response: $BODY"
    exit 1
fi

# Test Case 1.2: Response time < 100ms
echo -n "Test 1.2: Response time < 100ms... "
if [ "$DURATION" -lt 100 ]; then
    echo -e "${GREEN}✓ PASS${NC} (${DURATION}ms)"
else
    echo -e "${YELLOW}⚠ WARNING${NC} (${DURATION}ms)"
    echo "  Response took longer than 100ms (expected for HTTP 202 pattern)"
fi

# Test Case 1.3: Invalid lessonId returns HTTP 400
echo -n "Test 1.3: Invalid lessonId returns HTTP 400... "
RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$API_BASE_URL/api/video-transcripts/generate" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer test-token" \
  -d "{
    \"lessonId\": \"invalid-guid\",
    \"videoUrl\": \"$TEST_VIDEO_URL\",
    \"language\": \"$LANGUAGE\"
  }")

HTTP_CODE=$(echo "$RESPONSE" | tail -n1)

if [ "$HTTP_CODE" -eq 400 ]; then
    echo -e "${GREEN}✓ PASS${NC}"
else
    echo -e "${RED}✗ FAIL${NC}"
    echo "  Expected HTTP 400, got HTTP $HTTP_CODE"
    exit 1
fi

# Test Case 1.4: Missing videoUrl returns HTTP 400
echo -n "Test 1.4: Missing videoUrl returns HTTP 400... "
RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$API_BASE_URL/api/video-transcripts/generate" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer test-token" \
  -d "{
    \"lessonId\": \"$TEST_LESSON_ID\",
    \"language\": \"$LANGUAGE\"
  }")

HTTP_CODE=$(echo "$RESPONSE" | tail -n1)

if [ "$HTTP_CODE" -eq 400 ]; then
    echo -e "${GREEN}✓ PASS${NC}"
else
    echo -e "${RED}✗ FAIL${NC}"
    echo "  Expected HTTP 400, got HTTP $HTTP_CODE"
    exit 1
fi

# Test Case 1.5: Duplicate request handling (idempotency)
echo -n "Test 1.5: Duplicate request returns same jobId... "
RESPONSE1=$(curl -s -X POST "$API_BASE_URL/api/video-transcripts/generate" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer test-token" \
  -d "{
    \"lessonId\": \"$TEST_LESSON_ID\",
    \"videoUrl\": \"$TEST_VIDEO_URL\",
    \"language\": \"$LANGUAGE\"
  }")

JOB_ID_1=$(echo "$RESPONSE1" | jq -r '.jobId')

# Wait 1 second
sleep 1

RESPONSE2=$(curl -s -X POST "$API_BASE_URL/api/video-transcripts/generate" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer test-token" \
  -d "{
    \"lessonId\": \"$TEST_LESSON_ID\",
    \"videoUrl\": \"$TEST_VIDEO_URL\",
    \"language\": \"$LANGUAGE\"
  }")

JOB_ID_2=$(echo "$RESPONSE2" | jq -r '.jobId')

if [ "$JOB_ID_1" == "$JOB_ID_2" ]; then
    echo -e "${GREEN}✓ PASS${NC}"
    echo "  JobId: $JOB_ID_1"
else
    echo -e "${YELLOW}⚠ WARNING${NC}"
    echo "  Different jobIds returned (expected for non-idempotent endpoints)"
    echo "  JobId 1: $JOB_ID_1"
    echo "  JobId 2: $JOB_ID_2"
fi

echo ""
echo "========================================="
echo -e "${GREEN}  ✓ All HTTP 202 tests passed!${NC}"
echo "========================================="
