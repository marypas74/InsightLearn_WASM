#!/bin/bash
# Test script for GlobalExceptionHandlerMiddleware (Phase 6.2)
# Verifies environment-aware error responses (Development vs Production)

API_URL="${API_URL:-http://localhost:31081}"
echo "=================================================="
echo "Testing Global Exception Handler Middleware"
echo "=================================================="
echo "API URL: $API_URL"
echo ""

# Function to test an endpoint and show the error response
test_error() {
    local test_name="$1"
    local method="$2"
    local endpoint="$3"
    local data="$4"

    echo "Test: $test_name"
    echo "----------------------------------------"

    if [ -n "$data" ]; then
        response=$(curl -s -X "$method" "$API_URL$endpoint" \
            -H "Content-Type: application/json" \
            -d "$data" 2>&1)
    else
        response=$(curl -s -X "$method" "$API_URL$endpoint" 2>&1)
    fi

    echo "Response:"
    echo "$response" | jq '.' 2>/dev/null || echo "$response"
    echo ""
}

# Test 1: 404 Not Found - Nonexistent endpoint
test_error \
    "404 Not Found - Nonexistent Endpoint" \
    "GET" \
    "/api/nonexistent" \
    ""

# Test 2: 400 Bad Request - Invalid enrollment (null userId)
# This should trigger ArgumentNullException or ArgumentException
test_error \
    "400 Bad Request - Invalid Enrollment (Null UserId)" \
    "POST" \
    "/api/enrollments" \
    '{"userId":"00000000-0000-0000-0000-000000000000","courseId":"00000000-0000-0000-0000-000000000000"}'

# Test 3: 401 Unauthorized - Access protected endpoint without auth
test_error \
    "401 Unauthorized - No Authentication Token" \
    "GET" \
    "/api/users/profile" \
    ""

# Test 4: 404 Not Found - Get nonexistent user
test_error \
    "404 Not Found - Nonexistent User" \
    "GET" \
    "/api/users/99999999-9999-9999-9999-999999999999" \
    ""

# Test 5: 400 Bad Request - Invalid payment data
test_error \
    "400 Bad Request - Invalid Payment (Missing Required Fields)" \
    "POST" \
    "/api/payments/create-checkout" \
    '{"amount":-100,"currency":"INVALID"}'

echo "=================================================="
echo "Test Notes:"
echo "=================================================="
echo ""
echo "DEVELOPMENT MODE:"
echo "  - Error responses include 'ValidationErrors' with StackTrace"
echo "  - 'Message' field contains actual exception message"
echo "  - 'ExceptionType' shows the exact exception class"
echo ""
echo "PRODUCTION MODE:"
echo "  - Error responses have safe, generic messages"
echo "  - NO stack traces or internal details exposed"
echo "  - 'ValidationErrors' only includes actual validation errors"
echo ""
echo "All responses should include:"
echo "  - 'Error': Error category (BadRequest, NotFound, etc.)"
echo "  - 'Message': Human-readable error message"
echo "  - 'TraceId': Correlation ID for logs"
echo "  - 'Timestamp': UTC timestamp of error"
echo ""
echo "To switch environments:"
echo "  Development: Set ASPNETCORE_ENVIRONMENT=Development"
echo "  Production:  Set ASPNETCORE_ENVIRONMENT=Production"
echo ""
echo "=================================================="
