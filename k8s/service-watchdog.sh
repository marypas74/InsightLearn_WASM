#!/bin/bash
# InsightLearn Service Watchdog
# Monitors all services and restarts pods if they fail health checks

set -e

NAMESPACE="insightlearn"
CHECK_INTERVAL=60  # Check every 60 seconds
LOG_FILE="/tmp/insightlearn-watchdog.log"

log() {
    echo "[$(date +'%Y-%m-%d %H:%M:%S')] $*" | tee -a "$LOG_FILE"
}

check_pod_health() {
    local label=$1
    local service_name=$2

    # Check if pods are running
    local ready_pods=$(kubectl get pods -n "$NAMESPACE" -l "$label" -o jsonpath='{.items[*].status.conditions[?(@.type=="Ready")].status}' 2>/dev/null || echo "")

    if [[ -z "$ready_pods" ]]; then
        log "⚠️  $service_name: No pods found"
        return 1
    fi

    if echo "$ready_pods" | grep -q "False"; then
        log "❌ $service_name: Pod not ready"
        return 1
    fi

    log "✅ $service_name: Healthy"
    return 0
}

check_http_endpoint() {
    local url=$1
    local service_name=$2

    if curl -s -f -m 5 "$url" > /dev/null 2>&1; then
        log "✅ $service_name: HTTP endpoint OK"
        return 0
    else
        log "❌ $service_name: HTTP endpoint failed"
        return 1
    fi
}

restart_pod() {
    local label=$1
    local service_name=$2

    log "🔄 Restarting $service_name pods..."
    kubectl delete pod -n "$NAMESPACE" -l "$label" --grace-period=30
    sleep 10

    # Wait for new pods to be ready
    kubectl wait --for=condition=ready pod -l "$label" -n "$NAMESPACE" --timeout=120s
    log "✅ $service_name pods restarted successfully"
}

check_and_heal() {
    log "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    log "🔍 Starting health check cycle"

    # Check API
    if ! check_pod_health "app=insightlearn-api" "API"; then
        restart_pod "app=insightlearn-api" "API"
    elif ! check_http_endpoint "http://localhost:31081/health" "API"; then
        restart_pod "app=insightlearn-api" "API"
    fi

    # Check Grafana
    if ! check_pod_health "app=grafana" "Grafana"; then
        restart_pod "app=grafana" "Grafana"
    elif ! check_http_endpoint "http://localhost:31300/api/health" "Grafana"; then
        restart_pod "app=grafana" "Grafana"
    fi

    # Check Prometheus
    if ! check_pod_health "app=prometheus" "Prometheus"; then
        restart_pod "app=prometheus" "Prometheus"
    fi

    # Check MongoDB
    if ! check_pod_health "app=mongodb" "MongoDB"; then
        restart_pod "app=mongodb" "MongoDB"
    fi

    # Check SQL Server
    if ! check_pod_health "app=sqlserver" "SQL Server"; then
        restart_pod "app=sqlserver" "SQL Server"
    fi

    # Check Redis
    if ! check_pod_health "app=redis" "Redis"; then
        restart_pod "app=redis" "Redis"
    fi

    # Check Ollama
    if ! check_pod_health "app=ollama" "Ollama"; then
        restart_pod "app=ollama" "Ollama"
    fi

    # Check WebAssembly frontend
    if ! check_pod_health "app=insightlearn-wasm-blazor-webassembly" "WebAssembly"; then
        restart_pod "app=insightlearn-wasm-blazor-webassembly" "WebAssembly"
    fi

    log "🏁 Health check cycle completed"
}

# Main loop
log "🚀 InsightLearn Service Watchdog started"
log "   Namespace: $NAMESPACE"
log "   Check interval: ${CHECK_INTERVAL}s"
log "   Log file: $LOG_FILE"

while true; do
    check_and_heal
    log "😴 Sleeping for ${CHECK_INTERVAL}s..."
    sleep "$CHECK_INTERVAL"
done
