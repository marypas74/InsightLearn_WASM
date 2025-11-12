#!/bin/bash

# Test distributed rate limiting for InsightLearn API
# This script tests that rate limiting is enforced globally across pods

API_URL="http://localhost:31081"
TOTAL_REQUESTS=150
CONCURRENT_REQUESTS=10

echo "==========================================="
echo "Distributed Rate Limiting Test"
echo "==========================================="
echo "API URL: $API_URL"
echo "Total requests: $TOTAL_REQUESTS"
echo "Rate limit: 100 requests/minute (configured)"
echo ""

# Function to make a single request and capture the response code
make_request() {
    local response=$(curl -w "%{http_code}" -o /dev/null -s "$API_URL/api/info")
    echo "$response"
}

echo "Starting test at $(date)..."
echo ""

# Counter for response codes
success_count=0
rate_limited_count=0
error_count=0

# Make requests concurrently
for i in $(seq 1 $TOTAL_REQUESTS); do
    response=$(make_request)

    if [ "$response" == "200" ]; then
        ((success_count++))
        echo -n "."
    elif [ "$response" == "429" ]; then
        ((rate_limited_count++))
        echo -n "X"
    else
        ((error_count++))
        echo -n "E"
    fi

    # Small delay between requests to avoid overwhelming
    if [ $((i % $CONCURRENT_REQUESTS)) -eq 0 ]; then
        sleep 0.1
    fi
done

echo ""
echo ""
echo "Test completed at $(date)"
echo "==========================================="
echo "Results:"
echo "  ✅ Successful (200): $success_count"
echo "  🚫 Rate Limited (429): $rate_limited_count"
echo "  ❌ Errors: $error_count"
echo ""

# Verify rate limiting is working
if [ $rate_limited_count -gt 0 ]; then
    echo "✅ PASS: Rate limiting is working (received 429 responses)"
else
    echo "❌ FAIL: No rate limiting detected (no 429 responses)"
fi

# Check if approximately 100 requests succeeded (allowing for some variance)
if [ $success_count -ge 90 ] && [ $success_count -le 110 ]; then
    echo "✅ PASS: Rate limit threshold is approximately correct (~100 requests)"
else
    echo "⚠️  WARNING: Unexpected success count ($success_count), expected ~100"
fi

echo "==========================================="

# Test rate limit headers
echo ""
echo "Testing rate limit headers..."
echo ""
curl -v "$API_URL/api/info" 2>&1 | grep -i "x-ratelimit"
echo ""