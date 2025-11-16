#!/bin/bash
################################################################################
# InsightLearn HA Watchdog - Auto-Healing System
#
# Purpose: Continuous monitoring and automatic recovery
# - Monitors cluster health every 2 minutes
# - Automatically restores from backup if cluster is unhealthy
# - Verifies restoration success
# - Repeats until cluster is fully operational
#
# Author: InsightLearn DevOps Team
# Version: 2.0.0
################################################################################

set -euo pipefail

# Configuration
LOG_FILE="/var/log/insightlearn-watchdog.log"
BACKUP_DIR="/var/backups/k3s-cluster"
BACKUP_FILE="k3s-cluster-snapshot.tar.gz"
RESTORE_SCRIPT="/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/k8s/restore-cluster-state.sh"
MIN_DEPLOYMENTS=5
MIN_RUNNING_PODS=8
NAMESPACE="insightlearn"

# Colors for logging
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

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

# Function to check cluster health
check_cluster_health() {
    local health_status=0

    info "==========================================="
    info "HA Watchdog - Health Check Started"
    info "==========================================="

    # 1. Check if K3s is running
    if ! systemctl is-active --quiet k3s; then
        error "K3s service is not running!"
        health_status=1
    else
        log "✓ K3s service is running"
    fi

    # 2. Check kubectl connectivity
    if ! kubectl cluster-info &>/dev/null; then
        error "kubectl cannot connect to cluster"
        health_status=1
    else
        log "✓ kubectl connectivity OK"
    fi

    # 3. Check number of deployments
    DEPLOYMENT_COUNT=$(kubectl get deployments --all-namespaces --no-headers 2>/dev/null | wc -l || echo "0")
    if [[ $DEPLOYMENT_COUNT -lt $MIN_DEPLOYMENTS ]]; then
        warn "Insufficient deployments: $DEPLOYMENT_COUNT (expected: >=$MIN_DEPLOYMENTS)"
        health_status=1
    else
        log "✓ Deployment count OK: $DEPLOYMENT_COUNT"
    fi

    # 4. Check running pods
    RUNNING_PODS=$(kubectl get pods -n "$NAMESPACE" --field-selector=status.phase=Running --no-headers 2>/dev/null | wc -l || echo "0")
    if [[ $RUNNING_PODS -lt $MIN_RUNNING_PODS ]]; then
        warn "Insufficient running pods: $RUNNING_PODS (expected: >=$MIN_RUNNING_PODS)"
        health_status=1
    else
        log "✓ Running pods OK: $RUNNING_PODS"
    fi

    # 5. Check critical services
    CRITICAL_SERVICES=("insightlearn-api" "mongodb" "redis" "sqlserver")
    for service in "${CRITICAL_SERVICES[@]}"; do
        if kubectl get pod -n "$NAMESPACE" -l app="$service" --field-selector=status.phase=Running --no-headers 2>/dev/null | grep -q .; then
            log "✓ Service '$service' is running"
        else
            warn "Service '$service' is NOT running"
            health_status=1
        fi
    done

    return $health_status
}

# Function to perform auto-restore
perform_auto_restore() {
    info "==========================================="
    info "HA Watchdog - Auto-Restore Starting"
    info "==========================================="

    # Check if backup exists
    if [[ ! -f "$BACKUP_DIR/$BACKUP_FILE" ]]; then
        error "Backup file not found: $BACKUP_DIR/$BACKUP_FILE"
        return 1
    fi

    log "Backup found: $BACKUP_DIR/$BACKUP_FILE"

    # Execute restore script
    if [[ ! -x "$RESTORE_SCRIPT" ]]; then
        error "Restore script not executable: $RESTORE_SCRIPT"
        return 1
    fi

    log "Executing restore script..."
    if "$RESTORE_SCRIPT" 2>&1 | tee -a "$LOG_FILE"; then
        log "✓ Restore script completed successfully"

        # Wait for pods to stabilize
        log "Waiting 60 seconds for pods to stabilize..."
        sleep 60

        return 0
    else
        error "Restore script failed"
        return 1
    fi
}

# Function to verify restore success
verify_restore_success() {
    info "Verifying restore success..."

    local max_attempts=5
    local attempt=1

    while [[ $attempt -le $max_attempts ]]; do
        info "Verification attempt $attempt/$max_attempts..."

        if check_cluster_health; then
            log "✓✓✓ Cluster fully operational after restore!"
            return 0
        fi

        warn "Cluster not fully operational yet, waiting 30 seconds..."
        sleep 30
        ((attempt++))
    done

    error "Cluster still unhealthy after $max_attempts verification attempts"
    return 1
}

# Main watchdog loop
main() {
    log "================================================"
    log "InsightLearn HA Watchdog Starting - $(date)"
    log "================================================"

    # Check if running as root
    if [[ $EUID -ne 0 ]]; then
        error "This script must be run as root"
        exit 1
    fi

    # Perform health check
    if check_cluster_health; then
        log "==========================================="
        log "✓✓✓ Cluster is HEALTHY - No action needed"
        log "==========================================="
        exit 0
    fi

    # Cluster is unhealthy, attempt auto-restore
    warn "==========================================="
    warn "❌ Cluster is UNHEALTHY - Initiating Auto-Restore"
    warn "==========================================="

    if perform_auto_restore; then
        # Verify restore was successful
        if verify_restore_success; then
            log "================================================"
            log "✓✓✓ AUTO-RESTORE SUCCESSFUL - Cluster Operational"
            log "================================================"
            exit 0
        else
            error "================================================"
            error "❌ AUTO-RESTORE VERIFICATION FAILED"
            error "Manual intervention required!"
            error "================================================"
            exit 1
        fi
    else
        error "================================================"
        error "❌ AUTO-RESTORE FAILED"
        error "Manual intervention required!"
        error "================================================"
        exit 1
    fi
}

# Run main function
main "$@"
