#!/bin/bash
################################################################################
# K3s Cluster State Backup Script
#
# Purpose: Create complete backup of K3s cluster state including:
#   - All Kubernetes resources (deployments, services, configmaps, secrets, etc.)
#   - ETCD database snapshot
#   - Persistent volume data (optional, can be heavy)
#
# Schedule: Runs every hour via cron, overwrites previous snapshot
# Storage: Single backup file to save space (~50-100MB compressed)
#
# Author: InsightLearn DevOps Team
# Version: 1.0.0
################################################################################

set -euo pipefail

# Configuration
BACKUP_DIR="/var/backups/k3s-cluster"
BACKUP_FILE="k3s-cluster-snapshot.tar.gz"
BACKUP_PATH="${BACKUP_DIR}/${BACKUP_FILE}"
TEMP_DIR="/tmp/k3s-backup-$(date +%Y%m%d-%H%M%S)"
NAMESPACE="insightlearn"
LOG_FILE="/var/log/k3s-backup.log"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
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

# Check if running as root
if [[ $EUID -ne 0 ]]; then
   error "This script must be run as root (use sudo)"
   exit 1
fi

log "=========================================="
log "K3s Cluster Backup Started"
log "=========================================="

# Create backup directories
mkdir -p "$BACKUP_DIR"
mkdir -p "$TEMP_DIR"

# 1. Backup ETCD (K3s embedded etcd)
log "Step 1/7: Creating ETCD snapshot..."
if /usr/local/bin/k3s etcd-snapshot save \
    --name "snapshot-$(date +%Y%m%d-%H%M%S)" \
    --etcd-snapshot-dir "$TEMP_DIR/etcd" 2>&1 | tee -a "$LOG_FILE"; then
    log "ETCD snapshot created successfully"
else
    error "ETCD snapshot failed, continuing with resource backup..."
fi

# 2. Backup all Kubernetes resources by type
log "Step 2/7: Backing up Kubernetes resources..."

RESOURCE_DIR="$TEMP_DIR/resources"
mkdir -p "$RESOURCE_DIR"

# Function to backup resource type
backup_resource() {
    local resource_type=$1
    local namespace_flag=$2

    log "  Backing up: $resource_type"

    if kubectl get "$resource_type" $namespace_flag -o yaml > "$RESOURCE_DIR/${resource_type}.yaml" 2>/dev/null; then
        local count=$(grep -c "^kind: " "$RESOURCE_DIR/${resource_type}.yaml" 2>/dev/null || echo "0")
        log "    ✓ Saved $count $resource_type"
    else
        warn "    ! No $resource_type found or error occurred"
        rm -f "$RESOURCE_DIR/${resource_type}.yaml"
    fi
}

# Backup cluster-wide resources
log "  Backing up cluster-wide resources..."
backup_resource "namespaces" ""
backup_resource "persistentvolumes" ""
backup_resource "storageclasses" ""
backup_resource "clusterroles" ""
backup_resource "clusterrolebindings" ""

# Backup namespace-specific resources (all namespaces)
log "  Backing up all-namespaces resources..."
backup_resource "deployments" "--all-namespaces"
backup_resource "statefulsets" "--all-namespaces"
backup_resource "daemonsets" "--all-namespaces"
backup_resource "services" "--all-namespaces"
backup_resource "configmaps" "--all-namespaces"
backup_resource "secrets" "--all-namespaces"
backup_resource "persistentvolumeclaims" "--all-namespaces"
backup_resource "ingresses" "--all-namespaces"
backup_resource "networkpolicies" "--all-namespaces"
backup_resource "serviceaccounts" "--all-namespaces"
backup_resource "roles" "--all-namespaces"
backup_resource "rolebindings" "--all-namespaces"

# 3. Backup custom resources (if any)
log "Step 3/7: Backing up custom resources..."
if kubectl get crd -o name 2>/dev/null | grep -q "."; then
    kubectl get crd -o yaml > "$RESOURCE_DIR/customresourcedefinitions.yaml" 2>/dev/null || true
    log "  ✓ Custom resource definitions backed up"
else
    log "  ! No custom resource definitions found"
fi

# 4. Backup K3s configuration
log "Step 4/7: Backing up K3s configuration..."
mkdir -p "$TEMP_DIR/k3s-config"

if [[ -f /etc/rancher/k3s/k3s.yaml ]]; then
    cp /etc/rancher/k3s/k3s.yaml "$TEMP_DIR/k3s-config/" 2>/dev/null || warn "Could not copy k3s.yaml"
fi

if [[ -d /var/lib/rancher/k3s/server/manifests ]]; then
    cp -r /var/lib/rancher/k3s/server/manifests "$TEMP_DIR/k3s-config/" 2>/dev/null || warn "Could not copy manifests"
fi

log "  ✓ K3s configuration backed up"

# 5. Create backup metadata
log "Step 5/7: Creating backup metadata..."
cat > "$TEMP_DIR/backup-metadata.txt" <<EOF
Backup Timestamp: $(date -u +"%Y-%m-%d %H:%M:%S UTC")
Hostname: $(hostname)
K3s Version: $(/usr/local/bin/k3s --version | head -n1)
Kernel: $(uname -r)
OS: $(cat /etc/os-release | grep PRETTY_NAME | cut -d'"' -f2)

Cluster Info:
$(kubectl cluster-info 2>/dev/null || echo "Cluster info not available")

Node Status:
$(kubectl get nodes -o wide 2>/dev/null || echo "Node status not available")

Pod Status (all namespaces):
$(kubectl get pods --all-namespaces 2>/dev/null || echo "Pod status not available")

Storage (ZFS):
$(zfs list -r k3spool 2>/dev/null || echo "ZFS info not available")

Disk Usage:
$(df -h /k3s-zfs 2>/dev/null || echo "Disk usage not available")
EOF

log "  ✓ Metadata created"

# 6. Compress backup (keep last 3 backups with rotation)
log "Step 6/7: Compressing backup..."

# Determine which backup file to use (rotation between backup-1, backup-2, and backup-3)
BACKUP_1="$BACKUP_DIR/k3s-cluster-backup-1.tar.gz"
BACKUP_2="$BACKUP_DIR/k3s-cluster-backup-2.tar.gz"
BACKUP_3="$BACKUP_DIR/k3s-cluster-backup-3.tar.gz"

# Find oldest backup to overwrite (or create new if < 3 exist)
if [[ ! -f "$BACKUP_1" ]]; then
    # No backup-1, create it
    TARGET_BACKUP="$BACKUP_1"
    log "  Creating first backup: backup-1.tar.gz"
elif [[ ! -f "$BACKUP_2" ]]; then
    # backup-1 exists but not backup-2, create backup-2
    TARGET_BACKUP="$BACKUP_2"
    log "  Creating second backup: backup-2.tar.gz"
elif [[ ! -f "$BACKUP_3" ]]; then
    # backup-1 and backup-2 exist but not backup-3, create backup-3
    TARGET_BACKUP="$BACKUP_3"
    log "  Creating third backup: backup-3.tar.gz"
else
    # All 3 exist, overwrite the oldest one
    OLDEST="$BACKUP_1"
    OLDEST_NAME="backup-1"

    if [[ "$BACKUP_2" -ot "$OLDEST" ]]; then
        OLDEST="$BACKUP_2"
        OLDEST_NAME="backup-2"
    fi

    if [[ "$BACKUP_3" -ot "$OLDEST" ]]; then
        OLDEST="$BACKUP_3"
        OLDEST_NAME="backup-3"
    fi

    TARGET_BACKUP="$OLDEST"
    log "  Rotating: overwriting oldest backup ($OLDEST_NAME.tar.gz)"
fi

# Compress to target backup
if tar -czf "$TARGET_BACKUP.tmp" -C "$(dirname "$TEMP_DIR")" "$(basename "$TEMP_DIR")" 2>&1 | tee -a "$LOG_FILE"; then
    mv "$TARGET_BACKUP.tmp" "$TARGET_BACKUP"
    BACKUP_SIZE=$(du -h "$TARGET_BACKUP" | cut -f1)
    log "  ✓ Backup compressed: $BACKUP_SIZE"
    log "  ✓ Backup saved: $(basename "$TARGET_BACKUP")"

    # Update BACKUP_PATH for final summary
    BACKUP_PATH="$TARGET_BACKUP"
else
    error "Compression failed"
    rm -f "$TARGET_BACKUP.tmp"
    exit 1
fi

# 7. Cleanup temporary files
log "Step 7/7: Cleaning up..."
rm -rf "$TEMP_DIR"
log "  ✓ Temporary files removed"

# Keep only last 3 ETCD snapshots in K3s data directory
log "Cleaning up old ETCD snapshots (keeping last 3)..."
if [[ -d /var/lib/rancher/k3s/server/db/snapshots ]]; then
    cd /var/lib/rancher/k3s/server/db/snapshots
    ls -t snapshot-*.zip 2>/dev/null | tail -n +4 | xargs -r rm -f
    log "  ✓ Old ETCD snapshots cleaned"
fi

# Final summary
log "=========================================="
log "Backup completed successfully!"
log "Backup location: $BACKUP_PATH"
log "Backup size: $BACKUP_SIZE"
log "=========================================="

# Create symlink to latest backup
ln -sf "$(basename "$BACKUP_PATH")" "$BACKUP_DIR/latest-backup.tar.gz"
log "  ✓ Symlink updated: latest-backup.tar.gz -> $(basename "$BACKUP_PATH")"

# Create symlink for restore service compatibility (k3s-auto-restore.service expects this file)
ln -sf "$(basename "$BACKUP_PATH")" "$BACKUP_DIR/k3s-cluster-snapshot.tar.gz"
log "  ✓ Snapshot symlink updated: k3s-cluster-snapshot.tar.gz -> $(basename "$BACKUP_PATH")"

# Show backup rotation status
log "Backup rotation status:"
if [[ -f "$BACKUP_1" ]]; then
    BACKUP_1_SIZE=$(du -h "$BACKUP_1" | cut -f1)
    BACKUP_1_DATE=$(stat -c %y "$BACKUP_1" | cut -d'.' -f1)
    log "  • backup-1.tar.gz: $BACKUP_1_SIZE ($BACKUP_1_DATE)"
fi
if [[ -f "$BACKUP_2" ]]; then
    BACKUP_2_SIZE=$(du -h "$BACKUP_2" | cut -f1)
    BACKUP_2_DATE=$(stat -c %y "$BACKUP_2" | cut -d'.' -f1)
    log "  • backup-2.tar.gz: $BACKUP_2_SIZE ($BACKUP_2_DATE)"
fi
if [[ -f "$BACKUP_3" ]]; then
    BACKUP_3_SIZE=$(du -h "$BACKUP_3" | cut -f1)
    BACKUP_3_DATE=$(stat -c %y "$BACKUP_3" | cut -d'.' -f1)
    log "  • backup-3.tar.gz: $BACKUP_3_SIZE ($BACKUP_3_DATE)"
fi

# Export metrics for Prometheus/Grafana monitoring
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
if [[ -f "$SCRIPT_DIR/export-dr-metrics.sh" ]]; then
    log "Exporting disaster recovery metrics..."
    bash "$SCRIPT_DIR/export-dr-metrics.sh" 2>&1 | tee -a "$LOG_FILE" || warn "Failed to export metrics"
fi

# Collect GeoIP metrics from Cloudflare CF-IPCountry header
GEOIP_SCRIPT="/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/scripts/collect-geoip-metrics.sh"
if [[ -f "$GEOIP_SCRIPT" ]]; then
    log "Collecting GeoIP metrics from Cloudflare..."
    bash "$GEOIP_SCRIPT" 2>&1 | tee -a "$LOG_FILE" || warn "Failed to collect GeoIP metrics"
fi

exit 0
