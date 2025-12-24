#!/bin/bash
# InsightLearn WASM - Load Testing Script
# Usage: ./load-test.sh [light|medium|heavy]

set -e

SITE_URL="${SITE_URL:-https://www.insightlearn.cloud}"
MODE="${1:-light}"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
REPORT_FILE="load-test-report-${TIMESTAMP}.txt"

echo "========================================="
echo "InsightLearn Load Testing"
echo "========================================="
echo "Site: $SITE_URL"
echo "Mode: $MODE"
echo "Timestamp: $(date)"
echo "========================================="

# Function to run concurrent requests
run_load_test() {
    local concurrent=$1
    local total=$2
    local endpoint=$3

    echo ""
    echo "Testing: $endpoint"
    echo "Concurrent users: $concurrent"
    echo "Total requests: $total"
    echo "-----------------------------------------"

    start_time=$(date +%s)

    # Use GNU parallel if available
    if command -v parallel &> /dev/null; then
        seq 1 $total | parallel -j $concurrent "curl -s -o /dev/null -w '%{http_code} %{time_total}\\n' ${SITE_URL}${endpoint}" > /tmp/load_test_results.txt
    else
        # Fallback to background jobs
        for i in $(seq 1 $total); do
            curl -s -o /dev/null -w '%{http_code} %{time_total}\n' "${SITE_URL}${endpoint}" >> /tmp/load_test_results.txt &

            # Limit concurrent connections
            if [ $(jobs -r | wc -l) -ge $concurrent ]; then
                wait -n
            fi
        done
        wait
    fi

    end_time=$(date +%s)
    duration=$((end_time - start_time))

    # Analyze results
    total_requests=$(wc -l < /tmp/load_test_results.txt)
    success_count=$(grep -c "^200" /tmp/load_test_results.txt || echo "0")
    failed_count=$((total_requests - success_count))

    # Calculate average response time
    avg_time=$(awk '{sum+=$2; count++} END {if(count>0) print sum/count; else print 0}' /tmp/load_test_results.txt)

    # Find min and max response times
    min_time=$(awk '{print $2}' /tmp/load_test_results.txt | sort -n | head -1)
    max_time=$(awk '{print $2}' /tmp/load_test_results.txt | sort -n | tail -1)

    # Calculate requests per second
    rps=$(echo "scale=2; $total_requests / $duration" | bc)

    echo ""
    echo "RESULTS:"
    echo "  Total requests: $total_requests"
    echo "  Successful (200): $success_count"
    echo "  Failed: $failed_count"
    echo "  Duration: ${duration}s"
    echo "  Requests/sec: $rps"
    echo "  Avg response time: ${avg_time}s"
    echo "  Min response time: ${min_time}s"
    echo "  Max response time: ${max_time}s"
    echo ""

    # Write to report file
    {
        echo "========================================="
        echo "Endpoint: $endpoint"
        echo "Concurrent: $concurrent | Total: $total"
        echo "========================================="
        echo "Total requests: $total_requests"
        echo "Successful: $success_count"
        echo "Failed: $failed_count"
        echo "Duration: ${duration}s"
        echo "Requests/sec: $rps"
        echo "Avg response time: ${avg_time}s"
        echo "Min/Max: ${min_time}s / ${max_time}s"
        echo ""
    } >> "$REPORT_FILE"

    # Cleanup
    rm -f /tmp/load_test_results.txt

    # Alert if failure rate > 5%
    failure_rate=$(echo "scale=2; ($failed_count / $total_requests) * 100" | bc)
    if (( $(echo "$failure_rate > 5" | bc -l) )); then
        echo "⚠️ WARNING: High failure rate: ${failure_rate}%"
        return 1
    fi

    return 0
}

# Load test configurations based on mode
case "$MODE" in
    light)
        echo "Running LIGHT load test..."
        run_load_test 10 50 "/"
        run_load_test 5 20 "/courses"
        ;;

    medium)
        echo "Running MEDIUM load test..."
        run_load_test 25 100 "/"
        run_load_test 15 50 "/courses"
        run_load_test 10 30 "/dashboard"
        ;;

    heavy)
        echo "Running HEAVY load test..."
        run_load_test 50 200 "/"
        run_load_test 30 100 "/courses"
        run_load_test 20 75 "/dashboard"
        run_load_test 15 50 "/login"
        ;;

    stress)
        echo "Running STRESS test (use with caution!)..."
        echo "This will simulate 1000+ requests"
        read -p "Are you sure? (yes/no): " confirm
        if [ "$confirm" = "yes" ]; then
            run_load_test 100 500 "/"
            run_load_test 75 300 "/courses"
            run_load_test 50 200 "/dashboard"
        else
            echo "Stress test cancelled"
            exit 0
        fi
        ;;

    *)
        echo "Unknown mode: $MODE"
        echo "Usage: $0 [light|medium|heavy|stress]"
        exit 1
        ;;
esac

echo "========================================="
echo "Load test completed!"
echo "Report saved to: $REPORT_FILE"
echo "========================================="

# Display summary
cat "$REPORT_FILE"
