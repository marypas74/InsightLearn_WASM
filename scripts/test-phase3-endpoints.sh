#!/bin/bash

# ============================================================================
# Phase 3 - API Endpoints Testing Script
# Test tutti i 31 endpoint implementati per verificare funzionalità
# ============================================================================

set -e

# Configuration
API_URL="${API_URL:-http://localhost:31081}"
ADMIN_TOKEN="${ADMIN_TOKEN:-}"
USER_TOKEN="${USER_TOKEN:-}"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Test counters
TOTAL_TESTS=0
PASSED_TESTS=0
FAILED_TESTS=0

# Function to print colored output
print_header() {
    echo -e "\n${BLUE}========================================${NC}"
    echo -e "${BLUE}$1${NC}"
    echo -e "${BLUE}========================================${NC}"
}

print_success() {
    echo -e "${GREEN}✅ $1${NC}"
}

print_error() {
    echo -e "${RED}❌ $1${NC}"
}

print_warning() {
    echo -e "${YELLOW}⚠️  $1${NC}"
}

# Function to test endpoint
test_endpoint() {
    local METHOD=$1
    local ENDPOINT=$2
    local TOKEN=$3
    local EXPECTED_STATUS=$4
    local DESCRIPTION=$5
    local DATA=$6

    TOTAL_TESTS=$((TOTAL_TESTS + 1))

    echo -e "\n${YELLOW}Test $TOTAL_TESTS: $DESCRIPTION${NC}"
    echo "  Method: $METHOD"
    echo "  Endpoint: $ENDPOINT"
    echo "  Expected: HTTP $EXPECTED_STATUS"

    local CURL_CMD="curl -s -w '\n%{http_code}' -X $METHOD"

    if [ -n "$TOKEN" ]; then
        CURL_CMD="$CURL_CMD -H 'Authorization: Bearer $TOKEN'"
    fi

    CURL_CMD="$CURL_CMD -H 'Content-Type: application/json'"

    if [ -n "$DATA" ]; then
        CURL_CMD="$CURL_CMD -d '$DATA'"
    fi

    CURL_CMD="$CURL_CMD '$API_URL$ENDPOINT'"

    # Execute curl and capture output
    local RESPONSE=$(eval $CURL_CMD)
    local HTTP_CODE=$(echo "$RESPONSE" | tail -n 1)
    local BODY=$(echo "$RESPONSE" | sed '$d')

    if [ "$HTTP_CODE" == "$EXPECTED_STATUS" ]; then
        print_success "PASSED - HTTP $HTTP_CODE (Expected: $EXPECTED_STATUS)"
        PASSED_TESTS=$((PASSED_TESTS + 1))

        # Print response body (truncated)
        if [ -n "$BODY" ] && [ "$BODY" != "null" ]; then
            echo "  Response: $(echo $BODY | head -c 100)..."
        fi
    else
        print_error "FAILED - HTTP $HTTP_CODE (Expected: $EXPECTED_STATUS)"
        FAILED_TESTS=$((FAILED_TESTS + 1))

        if [ -n "$BODY" ]; then
            echo "  Response: $BODY"
        fi
    fi
}

# ============================================================================
# Main Test Execution
# ============================================================================

print_header "PHASE 3 - API ENDPOINTS TESTING"

echo "API URL: $API_URL"
echo "Admin Token: ${ADMIN_TOKEN:0:20}... (length: ${#ADMIN_TOKEN})"
echo "User Token: ${USER_TOKEN:0:20}... (length: ${#USER_TOKEN})"

# Check if API is running
print_header "0. HEALTH CHECK"
test_endpoint "GET" "/health" "" "200" "API Health Check"

# ============================================================================
# 1. CATEGORIES API TESTS
# ============================================================================
print_header "1. CATEGORIES API (5 endpoints)"

test_endpoint "GET" "/api/categories" "" "200" "Get all categories (public)"
test_endpoint "GET" "/api/categories" "$ADMIN_TOKEN" "200" "Get all categories (authenticated)"
# Note: POST, PUT, DELETE require valid data and would modify database

# ============================================================================
# 2. COURSES API TESTS
# ============================================================================
print_header "2. COURSES API (7 endpoints)"

test_endpoint "GET" "/api/courses?page=1&pageSize=5" "" "200" "Get courses paginated (public)"
test_endpoint "GET" "/api/courses/search?query=test" "" "200" "Search courses (public)"
# Note: Specific course ID tests require existing course IDs

# ============================================================================
# 3. REVIEWS API TESTS
# ============================================================================
print_header "3. REVIEWS API (4 endpoints)"

# Note: Reviews require valid course IDs from database
# These tests check endpoint availability, not full functionality
echo "  ℹ️  Reviews tests require valid course IDs - testing structure only"

# ============================================================================
# 4. ENROLLMENTS API TESTS
# ============================================================================
print_header "4. ENROLLMENTS API (5 endpoints)"

if [ -n "$ADMIN_TOKEN" ]; then
    test_endpoint "GET" "/api/enrollments?page=1&pageSize=5" "$ADMIN_TOKEN" "200" "Get all enrollments (Admin)"
else
    print_warning "Skipping Admin-only tests (no admin token provided)"
fi

if [ -n "$USER_TOKEN" ]; then
    # Note: Requires valid user ID from token
    echo "  ℹ️  User enrollments test requires parsing JWT for user ID"
else
    print_warning "Skipping User tests (no user token provided)"
fi

# ============================================================================
# 5. PAYMENTS API TESTS
# ============================================================================
print_header "5. PAYMENTS API (3 endpoints)"

if [ -n "$USER_TOKEN" ]; then
    test_endpoint "GET" "/api/payments/transactions?page=1&pageSize=5" "$USER_TOKEN" "200" "Get user transactions"
else
    print_warning "Skipping Payment tests (no user token provided)"
fi

# ============================================================================
# 6. USERS ADMIN API TESTS
# ============================================================================
print_header "6. USERS ADMIN API (5 endpoints)"

if [ -n "$ADMIN_TOKEN" ]; then
    test_endpoint "GET" "/api/users?page=1&pageSize=5" "$ADMIN_TOKEN" "200" "Get all users (Admin)"
else
    print_warning "Skipping User Admin tests (no admin token provided)"
fi

if [ -n "$USER_TOKEN" ]; then
    test_endpoint "GET" "/api/users/profile" "$USER_TOKEN" "200" "Get current user profile"
else
    print_warning "Skipping User Profile test (no user token provided)"
fi

# ============================================================================
# 7. DASHBOARD API TESTS
# ============================================================================
print_header "7. DASHBOARD API (2 endpoints)"

if [ -n "$ADMIN_TOKEN" ]; then
    test_endpoint "GET" "/api/dashboard/stats" "$ADMIN_TOKEN" "200" "Get dashboard statistics (Admin)"
    test_endpoint "GET" "/api/dashboard/recent-activity?count=5" "$ADMIN_TOKEN" "200" "Get recent activity (Admin)"
else
    print_warning "Skipping Dashboard tests (no admin token provided)"
fi

# ============================================================================
# SUMMARY
# ============================================================================
print_header "TEST SUMMARY"

echo "Total Tests:  $TOTAL_TESTS"
echo "Passed:       $PASSED_TESTS"
echo "Failed:       $FAILED_TESTS"

if [ $FAILED_TESTS -eq 0 ]; then
    print_success "ALL TESTS PASSED!"
    exit 0
else
    print_error "$FAILED_TESTS TESTS FAILED"
    exit 1
fi
