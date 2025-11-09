#!/bin/bash
################################################################################
# SQL Server Persistent Port-Forward Script
#
# Purpose: Maintains persistent port-forward to SQL Server service in Kubernetes
#   - Auto-restart on disconnection
#   - Runs in background with PID tracking
#   - Logs all activity
#
# Usage:
#   ./sqlserver-port-forward-persistent.sh &
#   tail -f /tmp/sqlserver-port-forward.log
#
# Stop:
#   pkill -f "sqlserver-port-forward-persistent"
#
# Author: InsightLearn DevOps Team
# Version: 1.0.0
################################################################################

set -euo pipefail

# Configuration
NAMESPACE="insightlearn"
SERVICE="sqlserver-service"
LOCAL_PORT="1433"
REMOTE_PORT="1433"
LOG_FILE="/tmp/sqlserver-port-forward.log"
PID_FILE="/tmp/sqlserver-port-forward.pid"
RETRY_DELAY=5

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Logging function
log() {
    echo -e "${GREEN}[$(date +'%Y-%m-%d %H:%M:%S')]${NC} $*" | tee -a "$LOG_FILE"
}

error() {
    echo -e "${RED}[$(date +'%Y-%m-%d %H:%M:%S')] ERROR:${NC} $*" | tee -a "$LOG_FILE" >&2
}

warn() {
    echo -e "${YELLOW}[$(date +'%Y-%m-%d %H:%M:%S')] WARNING:${NC} $*" | tee -a "$LOG_FILE"
}

info() {
    echo -e "${BLUE}[$(date +'%Y-%m-%d %H:%M:%S')] INFO:${NC} $*" | tee -a "$LOG_FILE"
}

# Cleanup function
cleanup() {
    log "Stopping SQL Server port-forward..."
    if [[ -f "$PID_FILE" ]]; then
        local pid=$(cat "$PID_FILE")
        if ps -p "$pid" > /dev/null 2>&1; then
            kill "$pid" 2>/dev/null || true
        fi
        rm -f "$PID_FILE"
    fi
    pkill -f "kubectl port-forward.*${SERVICE}" 2>/dev/null || true
    log "Cleanup completed"
    exit 0
}

# Trap signals
trap cleanup SIGINT SIGTERM EXIT

# Check if already running
if [[ -f "$PID_FILE" ]] && ps -p "$(cat "$PID_FILE")" > /dev/null 2>&1; then
    error "Port-forward already running (PID: $(cat "$PID_FILE"))"
    error "Stop it first with: pkill -f sqlserver-port-forward-persistent"
    exit 1
fi

# Save PID
echo $$ > "$PID_FILE"

log "=========================================="
log "SQL Server Port-Forward Starting"
log "=========================================="
log "Service: ${SERVICE}"
log "Namespace: ${NAMESPACE}"
log "Local port: ${LOCAL_PORT}"
log "Remote port: ${REMOTE_PORT}"
log "Log file: ${LOG_FILE}"
log "PID: $$"
log ""

# Check kubectl availability
if ! command -v kubectl &> /dev/null; then
    error "kubectl not found in PATH"
    exit 1
fi

# Check service exists
if ! kubectl get svc -n "$NAMESPACE" "$SERVICE" &> /dev/null; then
    error "Service ${SERVICE} not found in namespace ${NAMESPACE}"
    exit 1
fi

# Main loop - restart on failure
CONSECUTIVE_FAILURES=0
MAX_FAILURES=10

while true; do
    log "Starting port-forward (attempt $((CONSECUTIVE_FAILURES + 1)))..."

    # Start port-forward
    if kubectl port-forward -n "$NAMESPACE" "service/${SERVICE}" "${LOCAL_PORT}:${REMOTE_PORT}" >> "$LOG_FILE" 2>&1; then
        # Normal termination (user stopped it)
        log "Port-forward terminated normally"
        break
    else
        # Error occurred
        EXIT_CODE=$?
        error "Port-forward failed with exit code ${EXIT_CODE}"

        # Increment failure counter
        ((CONSECUTIVE_FAILURES++))

        if [[ $CONSECUTIVE_FAILURES -ge $MAX_FAILURES ]]; then
            error "Too many consecutive failures (${CONSECUTIVE_FAILURES}), giving up"
            exit 1
        fi

        # Check if service still exists
        if ! kubectl get svc -n "$NAMESPACE" "$SERVICE" &> /dev/null; then
            error "Service ${SERVICE} no longer exists"
            exit 1
        fi

        # Wait before retry
        warn "Retrying in ${RETRY_DELAY} seconds..."
        sleep "$RETRY_DELAY"
    fi
done

log "Port-forward script exiting"
