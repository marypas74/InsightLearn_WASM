#!/bin/bash
################################################################################
# K3s Cluster State Restore Script
#
# Purpose: Automatically restore K3s cluster state from backup
#   - Runs automatically at system boot (via systemd service)
#   - Restores all Kubernetes resources from latest snapshot
#   - Non-interactive, designed for unattended systems
#
# Behavior:
#   - Checks if cluster needs restoration (crash detection)
#   - Waits for K3s service to be ready
#   - Applies resources in correct order (namespaces → secrets → deployments)
#   - Verifies restoration success
#
# Author: InsightLearn DevOps Team
# Version: 1.0.0
################################################################################

set -euo pipefail

# Configuration
BACKUP_DIR="/var/backups/k3s-cluster"
BACKUP_FILE="k3s-cluster-snapshot.tar.gz"
BACKUP_PATH="${BACKUP_DIR}/${BACKUP_FILE}"
TEMP_DIR="/tmp/k3s-restore-$(date +%Y%m%d-%H%M%S)"
LOG_FILE="/var/log/k3s-restore.log"
STATE_FILE="/tmp/k3s-restore-state"

# Maximum wait time for K3s to be ready (seconds)
MAX_WAIT=300
WAIT_INTERVAL=5

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Logging functions
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

# Check if running as root
if [[ $EUID -ne 0 ]]; then
   error "This script must be run as root"
   exit 1
fi

log "=========================================="
log "K3s Cluster Restore Process Started"
log "=========================================="

# Check if backup exists
if [[ ! -f "$BACKUP_PATH" ]]; then
    warn "No backup found at $BACKUP_PATH"
    warn "Skipping restore - this might be first boot"
    exit 0
fi

# Check if restore was already done today
RESTORE_DATE=$(date +%Y%m%d)
if [[ -f "$STATE_FILE" ]]; then
    LAST_RESTORE=$(cat "$STATE_FILE" 2>/dev/null || echo "")
    if [[ "$LAST_RESTORE" == "$RESTORE_DATE" ]]; then
        info "Restore already performed today, skipping"
        exit 0
    fi
fi

# Step 1: Wait for K3s service to be ready
log "Step 1/6: Waiting for K3s service to be ready..."

elapsed=0
while ! systemctl is-active --quiet k3s; do
    if [[ $elapsed -ge $MAX_WAIT ]]; then
        error "K3s service did not start within ${MAX_WAIT}s"
        exit 1
    fi

    info "  Waiting for K3s service... (${elapsed}s/${MAX_WAIT}s)"
    sleep $WAIT_INTERVAL
    elapsed=$((elapsed + WAIT_INTERVAL))
done

log "  ✓ K3s service is active"

# Step 2: Wait for K3s API server to be ready
log "Step 2/6: Waiting for K3s API server..."

elapsed=0
while ! kubectl cluster-info &>/dev/null; do
    if [[ $elapsed -ge $MAX_WAIT ]]; then
        error "K3s API server did not respond within ${MAX_WAIT}s"
        exit 1
    fi

    info "  Waiting for API server... (${elapsed}s/${MAX_WAIT}s)"
    sleep $WAIT_INTERVAL
    elapsed=$((elapsed + WAIT_INTERVAL))
done

log "  ✓ K3s API server is ready"

# Step 3: Check if cluster is empty (needs restore)
log "Step 3/6: Checking cluster state..."

NAMESPACE_COUNT=$(kubectl get namespaces --no-headers 2>/dev/null | wc -l)
DEPLOYMENT_COUNT=$(kubectl get deployments --all-namespaces --no-headers 2>/dev/null | wc -l)

info "  Current state: $NAMESPACE_COUNT namespaces, $DEPLOYMENT_COUNT deployments"

# If cluster has resources (excluding default namespaces), skip restore
if [[ $DEPLOYMENT_COUNT -gt 5 ]]; then
    info "Cluster appears healthy, skipping restore"
    echo "$RESTORE_DATE" > "$STATE_FILE"
    exit 0
fi

warn "Cluster appears empty or incomplete - proceeding with restore"

# Step 4: Extract backup
log "Step 4/6: Extracting backup..."

mkdir -p "$TEMP_DIR"
if tar -xzf "$BACKUP_PATH" -C "$TEMP_DIR" 2>&1 | tee -a "$LOG_FILE"; then
    log "  ✓ Backup extracted"
else
    error "Failed to extract backup"
    rm -rf "$TEMP_DIR"
    exit 1
fi

# Find extracted directory (timestamped)
EXTRACT_DIR=$(find "$TEMP_DIR" -maxdepth 1 -type d -name "k3s-backup-*" | head -n1)
if [[ -z "$EXTRACT_DIR" ]]; then
    error "Could not find extracted backup directory"
    rm -rf "$TEMP_DIR"
    exit 1
fi

RESOURCE_DIR="$EXTRACT_DIR/resources"

# Step 5: Restore resources in correct order
log "Step 5/6: Restoring Kubernetes resources..."

# Function to apply resource with error handling
apply_resource() {
    local resource_file=$1
    local resource_name=$(basename "$resource_file" .yaml)

    if [[ ! -f "$resource_file" ]]; then
        return 0
    fi

    log "  Applying: $resource_name"

    # Filter out problematic resources that shouldn't be restored
    local filtered_file="${resource_file}.filtered"

    # Remove status fields and some metadata that cause conflicts
    kubectl apply --dry-run=client -f "$resource_file" -o yaml 2>/dev/null | \
        grep -v "resourceVersion:" | \
        grep -v "uid:" | \
        grep -v "creationTimestamp:" | \
        grep -v "selfLink:" > "$filtered_file" 2>/dev/null || cp "$resource_file" "$filtered_file"

    if kubectl apply -f "$filtered_file" 2>&1 | tee -a "$LOG_FILE"; then
        log "    ✓ Applied $resource_name successfully"
        return 0
    else
        warn "    ! Failed to apply $resource_name (may be normal for some resources)"
        return 1
    fi
}

# Restore order (important!)
RESTORE_ORDER=(
    "namespaces"
    "storageclasses"
    "persistentvolumes"
    "persistentvolumeclaims"
    "customresourcedefinitions"
    "serviceaccounts"
    "clusterroles"
    "clusterrolebindings"
    "roles"
    "rolebindings"
    "configmaps"
    "secrets"
    "services"
    "daemonsets"
    "deployments"
    "statefulsets"
    "ingresses"
    "networkpolicies"
)

SUCCESS_COUNT=0
FAIL_COUNT=0

for resource in "${RESTORE_ORDER[@]}"; do
    if apply_resource "$RESOURCE_DIR/${resource}.yaml"; then
        ((SUCCESS_COUNT++))
    else
        ((FAIL_COUNT++))
    fi
done

log "  Restore summary: $SUCCESS_COUNT succeeded, $FAIL_COUNT failed/skipped"

# Step 6: Verify cluster health
log "Step 6/6: Verifying cluster health..."

sleep 10  # Give cluster time to stabilize

# Check nodes
NODE_STATUS=$(kubectl get nodes --no-headers 2>/dev/null | grep -c "Ready" || echo "0")
log "  Nodes ready: $NODE_STATUS"

# Check pods
RUNNING_PODS=$(kubectl get pods --all-namespaces --no-headers 2>/dev/null | grep -c "Running" || echo "0")
TOTAL_PODS=$(kubectl get pods --all-namespaces --no-headers 2>/dev/null | wc -l || echo "0")
log "  Pods running: $RUNNING_PODS/$TOTAL_PODS"

# Check InsightLearn namespace specifically
if kubectl get namespace insightlearn &>/dev/null; then
    INSIGHTLEARN_PODS=$(kubectl get pods -n insightlearn --no-headers 2>/dev/null | wc -l || echo "0")
    log "  InsightLearn pods: $INSIGHTLEARN_PODS"
fi

# Cleanup
log "Cleaning up temporary files..."
rm -rf "$TEMP_DIR"

# Step 7: Verify and restore Cloudflare Tunnel
log "Step 7/7: Verifying Cloudflare Tunnel..."

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
CLOUDFLARE_SCRIPT="$SCRIPT_DIR/verify-cloudflare-tunnel.sh"

if [[ -f "$CLOUDFLARE_SCRIPT" ]]; then
    if bash "$CLOUDFLARE_SCRIPT" 2>&1 | tee -a "$LOG_FILE"; then
        log "  ✓ Cloudflare Tunnel verified/restored"
    else
        warn "  ! Cloudflare Tunnel check failed (may need manual intervention)"
        warn "  ! Run manually: $CLOUDFLARE_SCRIPT"
    fi
else
    warn "  ! Cloudflare verification script not found: $CLOUDFLARE_SCRIPT"
fi

# Mark restore as complete
echo "$RESTORE_DATE" > "$STATE_FILE"

log "=========================================="
log "Cluster Restore Completed!"
log "Note: Some pods may take several minutes to become fully ready"
log "Monitor with: kubectl get pods --all-namespaces -w"
log "External access: https://wasm.insightlearn.cloud/"
log "=========================================="

# Export metrics for Prometheus/Grafana monitoring
SCRIPT_DIR_METRICS="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
if [[ -f "$SCRIPT_DIR_METRICS/export-dr-metrics.sh" ]]; then
    log "Exporting disaster recovery metrics..."
    bash "$SCRIPT_DIR_METRICS/export-dr-metrics.sh" 2>&1 | tee -a "$LOG_FILE" || warn "Failed to export metrics"
fi

exit 0
