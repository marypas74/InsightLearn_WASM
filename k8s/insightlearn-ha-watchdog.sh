#!/bin/bash
################################################################################
# InsightLearn HA Watchdog - Auto-Healing System
#
# Purpose: Continuous monitoring and automatic recovery
# - FIRST: Imports ZFS pool if not mounted (CRITICAL for K3s storage)
# - Monitors cluster health every 2 minutes
# - Automatically restores from backup if cluster is unhealthy
# - Verifies restoration success
# - Fixes CoreDNS issues after restart
#
# Author: InsightLearn DevOps Team
# Version: 2.1.0 (Updated 2025-12-01)
# Changes: Added ZFS auto-import, CoreDNS recovery, improved networking checks
################################################################################

set -euo pipefail

# Set PATH and KUBECONFIG for kubectl
export PATH="/usr/local/bin:/usr/local/sbin:/usr/bin:/usr/sbin:/bin:/sbin"
export KUBECONFIG=/etc/rancher/k3s/k3s.yaml

# Configuration
LOG_FILE="/var/log/insightlearn-watchdog.log"
BACKUP_DIR="/var/backups/k3s-cluster"
BACKUP_FILE="k3s-cluster-snapshot.tar.gz"
RESTORE_SCRIPT="/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/k8s/restore-cluster-state.sh"
MIN_DEPLOYMENTS=5
MIN_RUNNING_PODS=8
NAMESPACE="insightlearn"

# ZFS Configuration
ZFS_POOL="k3spool"
ZFS_POOL_IMAGE="/home/zfs-k3s-pool.img"
ZFS_MOUNT="/k3s-zfs"
ZPOOL_BIN="/usr/local/sbin/zpool"
ZFS_BIN="/usr/local/sbin/zfs"

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

# NEW: Function to ensure ZFS pool is imported (CRITICAL - must run FIRST)
ensure_zfs_pool() {
    info "==========================================="
    info "Step 0: Checking ZFS Pool Status"
    info "==========================================="

    # Check if ZFS binaries exist
    if [[ ! -x "$ZPOOL_BIN" ]]; then
        error "ZFS binaries not found at $ZPOOL_BIN"
        return 1
    fi

    # Check if pool is already imported
    if "$ZPOOL_BIN" status "$ZFS_POOL" &>/dev/null; then
        log "✓ ZFS pool '$ZFS_POOL' is already imported and online"

        # Verify mount
        if mountpoint -q "$ZFS_MOUNT"; then
            log "✓ ZFS mount '$ZFS_MOUNT' is active"
            return 0
        else
            warn "ZFS pool imported but mount point not active, forcing mount..."
            "$ZFS_BIN" mount -a 2>/dev/null || true
            sleep 2
            if mountpoint -q "$ZFS_MOUNT"; then
                log "✓ ZFS mount restored successfully"
                return 0
            fi
        fi
    fi

    # Pool not imported - try to import it
    warn "ZFS pool '$ZFS_POOL' not imported, attempting import..."

    # Check if pool image exists
    if [[ ! -f "$ZFS_POOL_IMAGE" ]]; then
        error "ZFS pool image not found: $ZFS_POOL_IMAGE"
        error "This is a CRITICAL failure - K3s storage is unavailable!"
        return 1
    fi

    # Try to import the pool
    log "Importing ZFS pool from $ZFS_POOL_IMAGE..."
    if "$ZPOOL_BIN" import -d /home "$ZFS_POOL" 2>&1 | tee -a "$LOG_FILE"; then
        log "✓ ZFS pool '$ZFS_POOL' imported successfully"

        # Wait for mount
        sleep 3

        # Verify mount
        if mountpoint -q "$ZFS_MOUNT"; then
            log "✓ ZFS mount '$ZFS_MOUNT' is active"
        else
            warn "Mount point not immediately available, mounting all ZFS datasets..."
            "$ZFS_BIN" mount -a 2>/dev/null || true
            sleep 2
        fi

        # Show pool status
        "$ZPOOL_BIN" status "$ZFS_POOL" | head -10 | tee -a "$LOG_FILE"
        return 0
    else
        error "Failed to import ZFS pool '$ZFS_POOL'"
        return 1
    fi
}

# NEW: Function to fix CoreDNS after restart
fix_coredns() {
    info "Checking CoreDNS status..."

    local coredns_ready
    coredns_ready=$(kubectl get pods -n kube-system -l k8s-app=kube-dns --no-headers 2>/dev/null | awk '{print $2}' | head -1)

    if [[ "$coredns_ready" != "1/1" ]]; then
        warn "CoreDNS not ready ($coredns_ready), attempting recovery..."

        # Delete the CoreDNS pod to force recreation
        kubectl delete pods -n kube-system -l k8s-app=kube-dns --wait=false 2>/dev/null || true

        # Wait for new pod
        sleep 20

        # Check again
        coredns_ready=$(kubectl get pods -n kube-system -l k8s-app=kube-dns --no-headers 2>/dev/null | awk '{print $2}' | head -1)
        if [[ "$coredns_ready" == "1/1" ]]; then
            log "✓ CoreDNS recovered successfully"
        else
            warn "CoreDNS still not ready: $coredns_ready"
        fi
    else
        log "✓ CoreDNS is running and ready"
    fi
}

# Function to check cluster health
check_cluster_health() {
    local health_status=0

    info "==========================================="
    info "HA Watchdog - Health Check Started"
    info "==========================================="

    # 0. FIRST: Ensure ZFS pool is imported (CRITICAL)
    if ! ensure_zfs_pool; then
        error "ZFS pool check failed - this is CRITICAL!"
        health_status=1
    fi

    # 1. Check if K3s is running
    if ! systemctl is-active --quiet k3s; then
        error "K3s service is not running!"
        warn "Attempting to start K3s..."
        systemctl start k3s 2>/dev/null || true
        sleep 15

        if systemctl is-active --quiet k3s; then
            log "✓ K3s service started successfully"
        else
            health_status=1
        fi
    else
        log "✓ K3s service is running"
    fi

    # 2. Check kubectl connectivity
    if ! kubectl cluster-info &>/dev/null; then
        error "kubectl cannot connect to cluster"
        health_status=1
    else
        log "✓ kubectl connectivity OK"

        # 2.1 Check and fix CoreDNS if needed
        fix_coredns
    fi

    # 3. Check number of deployments
    local deployment_count
    deployment_count=$(kubectl get deployments --all-namespaces --no-headers 2>/dev/null | wc -l)
    if [[ ${deployment_count:-0} -lt $MIN_DEPLOYMENTS ]]; then
        warn "Insufficient deployments: ${deployment_count:-0} (expected: >=$MIN_DEPLOYMENTS)"
        health_status=1
    else
        log "✓ Deployment count OK: $deployment_count"
    fi

    # 4. Check running pods
    local running_pods
    running_pods=$(kubectl get pods -n "$NAMESPACE" --field-selector=status.phase=Running --no-headers 2>/dev/null | wc -l)
    if [[ ${running_pods:-0} -lt $MIN_RUNNING_PODS ]]; then
        warn "Insufficient running pods: ${running_pods:-0} (expected: >=$MIN_RUNNING_PODS)"
        health_status=1
    else
        log "✓ Running pods OK: $running_pods"
    fi

    # 5. Check ALL critical services
    CRITICAL_SERVICES=(
        "elasticsearch"
        "ollama"
        "prometheus"
        "insightlearn-api"
        "sqlserver"
        "insightlearn-wasm-blazor-webassembly"
        "redis"
        "mongodb"
        "grafana"
    )

    local missing_services=0
    for service in "${CRITICAL_SERVICES[@]}"; do
        if kubectl get pod --all-namespaces --no-headers 2>/dev/null | grep -q "$service.*Running"; then
            log "✓ Service '$service' is running"
        else
            warn "Service '$service' is NOT running"
            health_status=1
            ((missing_services++)) || true
        fi
    done

    if [[ $missing_services -gt 0 ]]; then
        warn "$missing_services critical services are missing"
    fi

    return $health_status
}

# Function to perform auto-restore
perform_auto_restore() {
    info "==========================================="
    info "HA Watchdog - Auto-Restore Starting"
    info "==========================================="

    # FIRST: Ensure ZFS is available before restore
    if ! ensure_zfs_pool; then
        error "Cannot proceed with restore - ZFS pool unavailable!"
        return 1
    fi

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

        # Fix CoreDNS after restore
        fix_coredns

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
    log "InsightLearn HA Watchdog v2.1.0 Starting - $(date)"
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
