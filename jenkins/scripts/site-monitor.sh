#!/bin/bash
# InsightLearn WASM - Continuous Site Monitoring Script
# Usage: ./site-monitor.sh [interval_seconds]

set -e

SITE_URL="${SITE_URL:-https://wasm.insightlearn.cloud}"
INTERVAL="${1:-60}"  # Default: check every 60 seconds
LOG_FILE="monitoring-$(date +%Y%m%d).log"
ALERT_FILE="alerts-$(date +%Y%m%d).log"

# Thresholds
RESPONSE_TIME_THRESHOLD=1.0  # seconds
FAILURE_THRESHOLD=3          # consecutive failures before alert
FAILURE_COUNT=0

echo "========================================="
echo "InsightLearn Site Monitoring"
echo "========================================="
echo "Site: $SITE_URL"
echo "Check interval: ${INTERVAL}s"
echo "Log file: $LOG_FILE"
echo "Alert file: $ALERT_FILE"
echo "========================================="
echo ""

# Function to log with timestamp
log_message() {
    local level=$1
    shift
    local message="$@"
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] [$level] $message" | tee -a "$LOG_FILE"
}

# Function to send alert
send_alert() {
    local severity=$1
    shift
    local message="$@"

    log_message "ALERT" "$message"
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] [$severity] $message" >> "$ALERT_FILE"

    # Optional: Send email or Slack notification
    # curl -X POST ... (configure webhook)
}

# Function to check endpoint
check_endpoint() {
    local endpoint=$1
    local expected_status=${2:-200}

    response=$(curl -s -o /dev/null -w "%{http_code}|%{time_total}" "$SITE_URL$endpoint")
    status_code=$(echo "$response" | cut -d'|' -f1)
    response_time=$(echo "$response" | cut -d'|' -f2)

    # Check status code
    if [ "$status_code" != "$expected_status" ]; then
        log_message "ERROR" "Endpoint $endpoint returned $status_code (expected $expected_status)"
        return 1
    fi

    # Check response time
    if (( $(echo "$response_time > $RESPONSE_TIME_THRESHOLD" | bc -l) )); then
        log_message "WARN" "Endpoint $endpoint slow: ${response_time}s (threshold: ${RESPONSE_TIME_THRESHOLD}s)"
    fi

    log_message "INFO" "Endpoint $endpoint OK: $status_code in ${response_time}s"
    return 0
}

# Function to check all critical endpoints
check_all_endpoints() {
    local all_ok=true

    # Frontend pages (expect 200)
    for page in "" "login" "register" "courses" "dashboard"; do
        if ! check_endpoint "/$page" 200; then
            all_ok=false
        fi
    done

    # Static assets
    for asset in "css/app.css" "js/httpClient.js" "favicon.png"; do
        if ! check_endpoint "/$asset" 200; then
            all_ok=false
        fi
    done

    # API endpoints (currently expect 502, change to 200 when fixed)
    # Uncomment when API is working:
    # check_endpoint "/health" 200 || all_ok=false
    # check_endpoint "/api/info" 200 || all_ok=false

    if [ "$all_ok" = true ]; then
        return 0
    else
        return 1
    fi
}

# Function to check Kubernetes pods (if kubectl available)
check_k8s_pods() {
    if ! command -v kubectl &> /dev/null; then
        return 0  # Skip if kubectl not available
    fi

    log_message "INFO" "Checking Kubernetes pods..."

    # Check if namespace exists
    if ! kubectl get namespace insightlearn &> /dev/null; then
        log_message "WARN" "Namespace 'insightlearn' not found"
        return 0
    fi

    # Get pod status
    pod_status=$(kubectl get pods -n insightlearn --no-headers 2>/dev/null || echo "")

    if [ -z "$pod_status" ]; then
        log_message "WARN" "No pods found in namespace"
        return 0
    fi

    # Check for pod issues
    not_ready=$(echo "$pod_status" | grep -v "Running" | grep -v "Completed" || true)

    if [ -n "$not_ready" ]; then
        log_message "ERROR" "Pods not ready:"
        echo "$not_ready" | while read line; do
            log_message "ERROR" "  $line"
        done
        send_alert "HIGH" "Kubernetes pods not ready"
        return 1
    fi

    log_message "INFO" "All Kubernetes pods healthy"
    return 0
}

# Function to check SSL certificate
check_ssl_certificate() {
    log_message "INFO" "Checking SSL certificate..."

    # Get certificate expiry date
    expiry=$(echo | openssl s_client -servername wasm.insightlearn.cloud -connect wasm.insightlearn.cloud:443 2>/dev/null | openssl x509 -noout -dates | grep notAfter | cut -d= -f2)

    if [ -z "$expiry" ]; then
        log_message "WARN" "Could not retrieve SSL certificate expiry"
        return 0
    fi

    expiry_epoch=$(date -d "$expiry" +%s 2>/dev/null || echo "0")
    now_epoch=$(date +%s)
    days_until_expiry=$(( (expiry_epoch - now_epoch) / 86400 ))

    if [ $days_until_expiry -lt 30 ]; then
        send_alert "HIGH" "SSL certificate expires in $days_until_expiry days!"
    elif [ $days_until_expiry -lt 60 ]; then
        log_message "WARN" "SSL certificate expires in $days_until_expiry days"
    else
        log_message "INFO" "SSL certificate valid for $days_until_expiry days"
    fi
}

# Function to calculate uptime percentage
calculate_uptime() {
    if [ ! -f "$LOG_FILE" ]; then
        echo "No data available"
        return
    fi

    total_checks=$(grep -c "\[INFO\] Endpoint / OK" "$LOG_FILE" 2>/dev/null || echo "0")
    failed_checks=$(grep -c "\[ERROR\]" "$LOG_FILE" 2>/dev/null || echo "0")

    if [ "$total_checks" -eq 0 ]; then
        echo "No checks recorded"
        return
    fi

    success_checks=$((total_checks - failed_checks))
    uptime=$(echo "scale=2; ($success_checks / $total_checks) * 100" | bc)

    log_message "INFO" "Uptime: ${uptime}% ($success_checks/$total_checks successful)"
}

# Trap Ctrl+C to show summary before exit
trap 'echo ""; log_message "INFO" "Monitoring stopped by user"; calculate_uptime; exit 0' INT TERM

# Main monitoring loop
log_message "INFO" "Monitoring started"
log_message "INFO" "Press Ctrl+C to stop"
echo ""

iteration=0
while true; do
    iteration=$((iteration + 1))
    log_message "INFO" "=== Check #$iteration ==="

    if check_all_endpoints; then
        FAILURE_COUNT=0
        log_message "INFO" "All endpoints healthy ✅"
    else
        FAILURE_COUNT=$((FAILURE_COUNT + 1))
        log_message "ERROR" "Some endpoints failed (consecutive failures: $FAILURE_COUNT)"

        if [ $FAILURE_COUNT -ge $FAILURE_THRESHOLD ]; then
            send_alert "CRITICAL" "Site health check failed $FAILURE_COUNT times consecutively!"
        fi
    fi

    # Check Kubernetes pods every 5 iterations (5 minutes at 60s interval)
    if [ $((iteration % 5)) -eq 0 ]; then
        check_k8s_pods
    fi

    # Check SSL certificate once per day
    if [ $((iteration % 1440)) -eq 0 ]; then
        check_ssl_certificate
    fi

    # Calculate uptime every 10 iterations
    if [ $((iteration % 10)) -eq 0 ]; then
        calculate_uptime
    fi

    echo ""
    sleep "$INTERVAL"
done
