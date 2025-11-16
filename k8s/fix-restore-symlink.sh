#!/bin/bash
################################################################################
# Fix Restore Symlink Script
#
# Purpose: Create symlink for restore service compatibility
# Issue: k3s-auto-restore.service expects k3s-cluster-snapshot.tar.gz
#        but backup-cluster-state.sh creates backup-1.tar.gz and backup-2.tar.gz
#
# Solution: Create symlink pointing to latest-backup.tar.gz
#
# Usage: sudo ./fix-restore-symlink.sh
################################################################################

set -euo pipefail

# Check if running as root
if [[ $EUID -ne 0 ]]; then
   echo "ERROR: This script must be run as root (use: sudo $0)"
   exit 1
fi

echo "[FIX] Creating symlink for restore compatibility..."

BACKUP_DIR="/var/backups/k3s-cluster"
SNAPSHOT_FILE="$BACKUP_DIR/k3s-cluster-snapshot.tar.gz"
LATEST_FILE="$BACKUP_DIR/latest-backup.tar.gz"

if [[ ! -f "$LATEST_FILE" ]]; then
    echo "ERROR: Latest backup file not found: $LATEST_FILE"
    exit 1
fi

# Remove old symlink if exists
if [[ -L "$SNAPSHOT_FILE" ]]; then
    echo "  Removing old symlink: $SNAPSHOT_FILE"
    rm -f "$SNAPSHOT_FILE"
fi

# Create new symlink
ln -sf "$LATEST_FILE" "$SNAPSHOT_FILE"

echo "  ✓ Symlink created: k3s-cluster-snapshot.tar.gz -> $(basename $(readlink $SNAPSHOT_FILE))"

# Verify
if [[ -f "$SNAPSHOT_FILE" ]]; then
    SIZE=$(du -h "$SNAPSHOT_FILE" | cut -f1)
    echo "  ✓ Verification: snapshot file exists ($SIZE)"

    # Test if systemd condition is met
    systemctl show k3s-auto-restore.service -p ConditionResult

    echo ""
    echo "[SUCCESS] Restore symlink fixed!"
    echo ""
    echo "Next steps:"
    echo "  1. Test restore manually: sudo /home/mpasqui/insightlearn_WASM/InsightLearn_WASM/k8s/restore-cluster-state.sh"
    echo "  2. Or restart k3s-auto-restore: sudo systemctl restart k3s-auto-restore.service"
else
    echo "ERROR: Symlink creation failed"
    exit 1
fi

exit 0
