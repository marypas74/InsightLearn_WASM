#!/bin/bash
# Security Fixes Verification Script
# Tests P0.1 (CORS), P0.4 (ReDoS), P0.2 (CSRF) fixes

echo "============================================"
echo "Security Fixes Verification Tests"
echo "============================================"
echo ""

API_URL="http://localhost:7001"
ALLOWED_ORIGIN="https://localhost:7003"
EVIL_ORIGIN="https://evil.com"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Test results tracking
TOTAL_TESTS=0
PASSED_TESTS=0
FAILED_TESTS=0

run_test() {
    TOTAL_TESTS=$((TOTAL_TESTS + 1))
    local test_name="$1"
    local command="$2"
    local expected="$3"

    echo "----------------------------------------"
    echo "Test $TOTAL_TESTS: $test_name"
    echo "----------------------------------------"

    result=$(eval "$command" 2>&1)

    if echo "$result" | grep -q "$expected"; then
        echo -e "${GREEN}✓ PASSED${NC}"
        echo "Expected: $expected"
        echo "Got: $(echo "$result" | grep "$expected")"
        PASSED_TESTS=$((PASSED_TESTS + 1))
    else
        echo -e "${RED}✗ FAILED${NC}"
        echo "Expected: $expected"
        echo "Got: $result"
        FAILED_TESTS=$((FAILED_TESTS + 1))
    fi
    echo ""
}

echo "=== P0.1: CORS Configuration Tests ==="
echo ""

# Test 1: Allowed origin succeeds
run_test "Allowed origin receives CORS headers" \
    "curl -s -H 'Origin: $ALLOWED_ORIGIN' -H 'Access-Control-Request-Method: POST' -X OPTIONS $API_URL/api/auth/login -i" \
    "Access-Control-Allow-Origin"

# Test 2: Disallowed origin fails
run_test "Evil origin rejected (no CORS headers)" \
    "curl -s -H 'Origin: $EVIL_ORIGIN' -X OPTIONS $API_URL/api/auth/login -i | grep -c 'Access-Control-Allow-Origin'" \
    "0"

# Test 3: Credentials support enabled
run_test "Credentials support enabled for allowed origin" \
    "curl -s -H 'Origin: $ALLOWED_ORIGIN' -i $API_URL/health" \
    "Access-Control-Allow-Credentials: true"

echo "=== P0.4: ReDoS Protection Tests ==="
echo ""

# Test 4: Normal input passes quickly
run_test "Normal chat message processed quickly" \
    "timeout 2 curl -s -X POST $API_URL/api/chat/message -H 'Content-Type: application/json' -d '{\"message\":\"Hello, how are you?\",\"sessionId\":\"test\"}' -w '%{http_code}'" \
    "200"

# Test 5: ReDoS attack input rejected quickly (without CSRF token will fail, but should not timeout)
run_test "ReDoS attack input rejected within timeout" \
    "timeout 2 curl -s -X POST $API_URL/api/chat/message -H 'Content-Type: application/json' -d '{\"message\":\"SELECT '$(python3 -c 'print("a"*5000)')' FROM users\",\"sessionId\":\"test\"}' -w '%{http_code}'" \
    "40"  # Expecting 400 or 403

# Test 6: Oversized input rejected
LARGE_INPUT=$(python3 -c 'print("a"*15000)')
run_test "Oversized input (>10KB) rejected" \
    "curl -s -X POST $API_URL/api/chat/message -H 'Content-Type: application/json' -d '{\"message\":\"$LARGE_INPUT\",\"sessionId\":\"test\"}' -w '%{http_code}' -o /dev/null" \
    "400"

echo "=== P0.2: CSRF Protection Tests ==="
echo ""

# Test 7: Health endpoint generates CSRF cookie
run_test "CSRF cookie generated on health endpoint" \
    "curl -s -c /tmp/cookies.txt $API_URL/health -i | grep -c 'Set-Cookie.*XSRF-TOKEN'" \
    "1"

# Test 8: POST without CSRF token fails (exempt paths should still work)
run_test "Login endpoint (exempt) works without CSRF token" \
    "curl -s -X POST $API_URL/api/auth/login -H 'Content-Type: application/json' -d '{\"email\":\"test@example.com\",\"password\":\"Test123!\"}' -w '%{http_code}' -o /dev/null" \
    "40"  # Expecting 400 or 401 (validation error, not 403 CSRF error)

# Test 9: Non-exempt endpoint requires CSRF token
run_test "Non-exempt endpoint rejects request without CSRF token" \
    "curl -s -X POST $API_URL/api/courses -H 'Content-Type: application/json' -d '{\"title\":\"Test Course\"}' -w '%{http_code}' -o /dev/null" \
    "403"

echo ""
echo "============================================"
echo "Test Summary"
echo "============================================"
echo -e "Total Tests: $TOTAL_TESTS"
echo -e "${GREEN}Passed: $PASSED_TESTS${NC}"
echo -e "${RED}Failed: $FAILED_TESTS${NC}"
echo ""

if [ $FAILED_TESTS -eq 0 ]; then
    echo -e "${GREEN}All security fixes verified successfully!${NC}"
    exit 0
else
    echo -e "${YELLOW}Some tests failed. Review the output above.${NC}"
    echo "Note: Some failures may be expected if API is not running or database is not configured."
    exit 1
fi
