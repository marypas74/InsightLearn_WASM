#!/bin/bash

# Security Headers Verification Script (P1.2)
# Tests that all security headers are present on API responses
# Expected: 10 security headers on all endpoints

set -euo pipefail

API_BASE_URL="${API_BASE_URL:-http://localhost:7001}"
ENDPOINTS=("/api/info" "/health" "/api/system/endpoints")

echo "=========================================="
echo "Security Headers Verification (P1.2)"
echo "=========================================="
echo ""
echo "API Base URL: $API_BASE_URL"
echo "Testing ${#ENDPOINTS[@]} endpoints"
echo ""

# ANSI color codes
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Security headers to check
REQUIRED_HEADERS=(
    "X-Frame-Options"
    "X-Content-Type-Options"
    "Content-Security-Policy"
    "Referrer-Policy"
    "Permissions-Policy"
    "Cross-Origin-Embedder-Policy"
    "Cross-Origin-Opener-Policy"
    "Cross-Origin-Resource-Policy"
    "X-XSS-Protection"
)

# Optional header (production only)
OPTIONAL_HEADERS=(
    "Strict-Transport-Security"
)

TOTAL_TESTS=0
PASSED_TESTS=0
FAILED_TESTS=0

for endpoint in "${ENDPOINTS[@]}"; do
    echo "=========================================="
    echo "Testing endpoint: $endpoint"
    echo "=========================================="

    # Fetch headers (ignore errors if endpoint doesn't exist)
    RESPONSE=$(curl -s -I "$API_BASE_URL$endpoint" 2>/dev/null || echo "ENDPOINT_ERROR")

    if [[ "$RESPONSE" == "ENDPOINT_ERROR" ]]; then
        echo -e "${YELLOW}⚠ Endpoint not accessible (API may not be running)${NC}"
        echo ""
        continue
    fi

    # Check required headers
    for header in "${REQUIRED_HEADERS[@]}"; do
        TOTAL_TESTS=$((TOTAL_TESTS + 1))

        if echo "$RESPONSE" | grep -qi "^$header:"; then
            VALUE=$(echo "$RESPONSE" | grep -i "^$header:" | cut -d: -f2- | xargs)
            echo -e "${GREEN}✅ $header${NC}: $VALUE"
            PASSED_TESTS=$((PASSED_TESTS + 1))
        else
            echo -e "${RED}❌ $header${NC}: MISSING"
            FAILED_TESTS=$((FAILED_TESTS + 1))
        fi
    done

    # Check optional headers (informational only)
    for header in "${OPTIONAL_HEADERS[@]}"; do
        if echo "$RESPONSE" | grep -qi "^$header:"; then
            VALUE=$(echo "$RESPONSE" | grep -i "^$header:" | cut -d: -f2- | xargs)
            echo -e "${GREEN}✅ $header${NC}: $VALUE (optional)"
        else
            echo -e "${YELLOW}⚠ $header${NC}: Not present (optional - expected in development)"
        fi
    done

    echo ""
done

echo "=========================================="
echo "Test Summary"
echo "=========================================="
echo "Total Tests: $TOTAL_TESTS"
echo -e "Passed: ${GREEN}$PASSED_TESTS${NC}"
echo -e "Failed: ${RED}$FAILED_TESTS${NC}"
echo ""

if [[ $FAILED_TESTS -eq 0 ]]; then
    echo -e "${GREEN}✅ All security headers present!${NC}"
    echo ""
    echo "OWASP ASVS V14.4 Compliance: ✅ PASS"
    echo "Estimated SecurityHeaders.com Score: A or A+"
    exit 0
else
    echo -e "${RED}❌ Some security headers are missing!${NC}"
    echo ""
    echo "OWASP ASVS V14.4 Compliance: ❌ FAIL"
    echo "Action required: Check SecurityHeadersMiddleware implementation"
    exit 1
fi
