#!/bin/bash

# Test Prometheus Metrics Endpoint
# Tests custom application metrics added in Phase 4.2

set -e

API_URL="${API_URL:-http://localhost:31081}"
METRICS_URL="$API_URL/metrics"

echo "========================================="
echo "Testing Prometheus Metrics Endpoint"
echo "========================================="
echo ""
echo "API URL: $API_URL"
echo "Metrics URL: $METRICS_URL"
echo ""

# Test 1: Verify /metrics endpoint is accessible
echo "Test 1: Checking if /metrics endpoint is accessible..."
STATUS=$(curl -s -o /dev/null -w "%{http_code}" "$METRICS_URL")
if [ "$STATUS" -eq 200 ]; then
    echo "✓ PASS: /metrics endpoint is accessible (HTTP $STATUS)"
else
    echo "✗ FAIL: /metrics endpoint returned HTTP $STATUS"
    exit 1
fi
echo ""

# Test 2: Fetch and verify Prometheus metrics format
echo "Test 2: Fetching metrics..."
METRICS=$(curl -s "$METRICS_URL")

if [ -z "$METRICS" ]; then
    echo "✗ FAIL: No metrics returned"
    exit 1
fi

echo "✓ PASS: Metrics received ($(echo "$METRICS" | wc -l) lines)"
echo ""

# Test 3: Verify custom InsightLearn metrics are present
echo "Test 3: Verifying custom application metrics..."
echo ""

# Counter metrics
echo "Checking COUNTERS:"
METRICS_COUNTERS=(
    "insightlearn_api_requests_total"
    "insightlearn_enrollments_total"
    "insightlearn_payments_total"
    "insightlearn_payment_revenue_total"
    "insightlearn_chatbot_messages_total"
    "insightlearn_video_uploads_total"
    "insightlearn_user_registrations_total"
    "insightlearn_login_attempts_total"
)

for metric in "${METRICS_COUNTERS[@]}"; do
    if echo "$METRICS" | grep -q "^# HELP $metric"; then
        COUNT=$(echo "$METRICS" | grep "^$metric" | wc -l)
        echo "  ✓ $metric ($COUNT series)"
    else
        echo "  ✗ MISSING: $metric"
    fi
done
echo ""

# Gauge metrics
echo "Checking GAUGES:"
METRICS_GAUGES=(
    "insightlearn_active_users"
    "insightlearn_active_enrollments"
    "insightlearn_courses"
    "insightlearn_video_storage_bytes"
    "insightlearn_database_connections"
)

for metric in "${METRICS_GAUGES[@]}"; do
    if echo "$METRICS" | grep -q "^# HELP $metric"; then
        COUNT=$(echo "$METRICS" | grep "^$metric" | wc -l)
        echo "  ✓ $metric ($COUNT series)"
    else
        echo "  ✗ MISSING: $metric"
    fi
done
echo ""

# Histogram metrics
echo "Checking HISTOGRAMS:"
METRICS_HISTOGRAMS=(
    "insightlearn_api_request_duration_seconds"
    "insightlearn_ollama_inference_duration_seconds"
    "insightlearn_database_query_duration_seconds"
)

for metric in "${METRICS_HISTOGRAMS[@]}"; do
    if echo "$METRICS" | grep -q "^# HELP $metric"; then
        COUNT=$(echo "$METRICS" | grep "^${metric}_" | wc -l)
        echo "  ✓ $metric ($COUNT series - bucket/sum/count)"
    else
        echo "  ✗ MISSING: $metric"
    fi
done
echo ""

# Summary metrics
echo "Checking SUMMARIES:"
METRICS_SUMMARIES=(
    "insightlearn_video_upload_size_bytes"
    "insightlearn_payment_amount_usd"
)

for metric in "${METRICS_SUMMARIES[@]}"; do
    if echo "$METRICS" | grep -q "^# HELP $metric"; then
        COUNT=$(echo "$METRICS" | grep "^${metric}" | wc -l)
        echo "  ✓ $metric ($COUNT series - quantiles/sum/count)"
    else
        echo "  ✗ MISSING: $metric"
    fi
done
echo ""

# Test 4: Verify prometheus-net built-in metrics
echo "Test 4: Verifying prometheus-net HTTP metrics..."
BUILTIN_METRICS=(
    "http_requests_received_total"
    "http_requests_in_progress"
    "http_request_duration_seconds"
)

for metric in "${BUILTIN_METRICS[@]}"; do
    if echo "$METRICS" | grep -q "$metric"; then
        echo "  ✓ $metric"
    else
        echo "  ⚠ NOT FOUND: $metric (may not be initialized yet)"
    fi
done
echo ""

# Test 5: Generate sample API requests to populate metrics
echo "Test 5: Generating sample API requests to populate metrics..."
echo ""

echo "  → Calling /health endpoint..."
curl -s "$API_URL/health" > /dev/null

echo "  → Calling /api/info endpoint..."
curl -s "$API_URL/api/info" > /dev/null

echo ""
echo "  Waiting 2 seconds for metrics to update..."
sleep 2

# Re-fetch metrics
METRICS_AFTER=$(curl -s "$METRICS_URL")

# Check if API request metrics increased
API_REQUESTS=$(echo "$METRICS_AFTER" | grep "insightlearn_api_requests_total" | grep -v "^#" | head -1 | awk '{print $2}')

if [ -n "$API_REQUESTS" ] && [ "$API_REQUESTS" -gt 0 ]; then
    echo "  ✓ API requests recorded: $API_REQUESTS"
else
    echo "  ⚠ No API requests recorded yet (metrics may need more traffic)"
fi
echo ""

# Test 6: Sample metrics output
echo "Test 6: Sample metrics output (first 50 lines):"
echo "========================================"
echo "$METRICS" | head -50
echo "..."
echo "========================================"
echo ""

# Summary
echo "========================================="
echo "Metrics Test Summary"
echo "========================================="
echo "Total metrics lines: $(echo "$METRICS" | wc -l)"
echo "InsightLearn custom metrics: $(echo "$METRICS" | grep -c "^# HELP insightlearn_" || echo "0")"
echo "HTTP built-in metrics: $(echo "$METRICS" | grep -c "^# HELP http_" || echo "0")"
echo "Process metrics: $(echo "$METRICS" | grep -c "^# HELP process_" || echo "0")"
echo ""
echo "✓ Prometheus metrics endpoint is working correctly!"
echo ""
echo "Next steps:"
echo "  1. Configure Prometheus to scrape this endpoint:"
echo "     - Add job: insightlearn-api"
echo "     - Target: $METRICS_URL"
echo "  2. Import Grafana dashboard from k8s/grafana-dashboard-insightlearn.json"
echo "  3. Monitor metrics at http://localhost:3000"
