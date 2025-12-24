#!/bin/bash
################################################################################
# Install K3s Disaster Recovery System
#
# Purpose: Automated installation of backup/restore system
#   - Installs systemd service for auto-restore at boot
#   - Configures hourly cron job for backups
#   - Creates necessary directories and permissions
#   - Runs initial backup
#
# Usage: sudo ./install-disaster-recovery.sh
#
# Author: InsightLearn DevOps Team
# Version: 1.0.0
################################################################################

set -euo pipefail

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

log() {
    echo -e "${GREEN}[INFO]${NC} $*"
}

error() {
    echo -e "${RED}[ERROR]${NC} $*" >&2
}

warn() {
    echo -e "${YELLOW}[WARN]${NC} $*"
}

info() {
    echo -e "${BLUE}[INFO]${NC} $*"
}

# Check if running as root
if [[ $EUID -ne 0 ]]; then
   error "This script must be run as root (use sudo)"
   exit 1
fi

echo ""
log "=========================================="
log "K3s Disaster Recovery System Installation"
log "=========================================="
echo ""

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
BACKUP_DIR="/var/backups/k3s-cluster"
LOG_DIR="/var/log"

# Step 1: Create directories
log "Step 1/8: Creating directories..."
mkdir -p "$BACKUP_DIR"
mkdir -p "$LOG_DIR"
log "  ✓ Directories created"

# Step 2: Copy backup script
log "Step 2/8: Installing backup script..."
if [[ ! -f "$SCRIPT_DIR/backup-cluster-state.sh" ]]; then
    error "backup-cluster-state.sh not found in $SCRIPT_DIR"
    exit 1
fi

chmod +x "$SCRIPT_DIR/backup-cluster-state.sh"
log "  ✓ Backup script ready: $SCRIPT_DIR/backup-cluster-state.sh"

# Step 3: Copy restore script
log "Step 3/8: Installing restore script..."
if [[ ! -f "$SCRIPT_DIR/restore-cluster-state.sh" ]]; then
    error "restore-cluster-state.sh not found in $SCRIPT_DIR"
    exit 1
fi

chmod +x "$SCRIPT_DIR/restore-cluster-state.sh"
log "  ✓ Restore script ready: $SCRIPT_DIR/restore-cluster-state.sh"

# Step 4: Install systemd service
log "Step 4/8: Installing systemd service..."
if [[ ! -f "$SCRIPT_DIR/k3s-auto-restore.service" ]]; then
    error "k3s-auto-restore.service not found in $SCRIPT_DIR"
    exit 1
fi

cp "$SCRIPT_DIR/k3s-auto-restore.service" /etc/systemd/system/
systemctl daemon-reload

if systemctl enable k3s-auto-restore.service; then
    log "  ✓ Systemd service enabled (will run at boot)"
else
    error "Failed to enable systemd service"
    exit 1
fi

log "  ✓ Service status:"
systemctl status k3s-auto-restore.service --no-pager || true

# Step 5: Install Cloudflare Tunnel systemd service (optional)
log "Step 5/8: Installing Cloudflare Tunnel systemd service..."

if command -v cloudflared &> /dev/null; then
    if [[ -f "$SCRIPT_DIR/cloudflared-tunnel.service" ]]; then
        cp "$SCRIPT_DIR/cloudflared-tunnel.service" /etc/systemd/system/
        systemctl daemon-reload

        if systemctl enable cloudflared-tunnel.service 2>/dev/null; then
            log "  ✓ Cloudflared service enabled"

            # Start service if config exists
            if [[ -f /home/mpasqui/.cloudflared/config.yml ]]; then
                if systemctl start cloudflared-tunnel.service 2>/dev/null; then
                    log "  ✓ Cloudflared service started"
                else
                    warn "  ! Could not start cloudflared service (check config)"
                fi
            else
                warn "  ! Cloudflare config not found, service will start when configured"
            fi
        else
            warn "  ! Could not enable cloudflared service"
        fi
    else
        warn "  ! cloudflared-tunnel.service file not found"
    fi
else
    info "  ℹ cloudflared not installed, skipping tunnel setup"
    info "    Install with: sudo cloudflared service install"
fi

# Make Cloudflare verification script executable
if [[ -f "$SCRIPT_DIR/verify-cloudflare-tunnel.sh" ]]; then
    chmod +x "$SCRIPT_DIR/verify-cloudflare-tunnel.sh"
    log "  ✓ Cloudflare verification script ready"
fi

# Step 6: Configure cron job for hourly backup
log "Step 6/8: Configuring hourly backup cron job..."

CRON_FILE="/etc/cron.d/k3s-cluster-backup"

cat > "$CRON_FILE" <<EOF
# K3s Cluster Hourly Backup
# Runs every hour, overwrites previous snapshot to save space
# Logs to /var/log/k3s-backup.log

SHELL=/bin/bash
PATH=/usr/local/sbin:/usr/local/bin:/sbin:/bin:/usr/sbin:/usr/bin

# Run backup at minute 5 of every hour
5 * * * * root $SCRIPT_DIR/backup-cluster-state.sh >/dev/null 2>&1

# Alternative: Run every 2 hours (uncomment to use)
# 5 */2 * * * root $SCRIPT_DIR/backup-cluster-state.sh >/dev/null 2>&1
EOF

chmod 644 "$CRON_FILE"
log "  ✓ Cron job configured: hourly at :05"
log "  ✓ Cron file: $CRON_FILE"

# Verify cron is running
if systemctl is-active --quiet cron || systemctl is-active --quiet crond; then
    log "  ✓ Cron service is active"
else
    warn "  ! Cron service not active, starting it..."
    systemctl start crond 2>/dev/null || systemctl start cron 2>/dev/null || warn "Could not start cron service"
fi

# Step 7: Run initial backup
log "Step 7/8: Running initial backup..."
info "  This may take a few minutes depending on cluster size..."
echo ""

if "$SCRIPT_DIR/backup-cluster-state.sh"; then
    log "  ✓ Initial backup completed successfully"
else
    warn "  ! Initial backup failed (may be normal if K3s not fully ready)"
    warn "  ! First automatic backup will run at next hour :05"
fi

# Step 8: Create monitoring script
log "Step 8/8: Creating monitoring script..."

MONITOR_SCRIPT="$SCRIPT_DIR/check-disaster-recovery-status.sh"

cat > "$MONITOR_SCRIPT" <<'EOF'
#!/bin/bash
# Quick status check for disaster recovery system

echo "=== K3s Disaster Recovery Status ==="
echo ""

echo "Backup Status:"
if [[ -f /var/backups/k3s-cluster/k3s-cluster-snapshot.tar.gz ]]; then
    BACKUP_SIZE=$(du -h /var/backups/k3s-cluster/k3s-cluster-snapshot.tar.gz | cut -f1)
    BACKUP_DATE=$(stat -c %y /var/backups/k3s-cluster/k3s-cluster-snapshot.tar.gz | cut -d'.' -f1)
    echo "  ✓ Latest backup: $BACKUP_SIZE ($BACKUP_DATE)"
else
    echo "  ✗ No backup found"
fi

echo ""
echo "Systemd Service Status:"
systemctl is-enabled k3s-auto-restore.service >/dev/null 2>&1 && echo "  ✓ Auto-restore service: enabled" || echo "  ✗ Auto-restore service: disabled"
systemctl is-active k3s-auto-restore.service >/dev/null 2>&1 && echo "  ✓ Auto-restore service: active" || echo "  ℹ Auto-restore service: inactive (normal, runs at boot)"

echo ""
echo "Cloudflare Tunnel Status:"
if systemctl is-enabled cloudflared-tunnel.service >/dev/null 2>&1; then
    systemctl is-active cloudflared-tunnel.service >/dev/null 2>&1 && echo "  ✓ Cloudflared service: running" || echo "  ✗ Cloudflared service: not running"
elif pgrep -f "cloudflared tunnel" >/dev/null 2>&1; then
    echo "  ✓ Cloudflared: running (manual process)"
else
    echo "  ✗ Cloudflared: not running"
fi

# Check external access
if curl -s -m 5 https://www.insightlearn.cloud/health >/dev/null 2>&1; then
    echo "  ✓ External access: OK (https://www.insightlearn.cloud)"
else
    echo "  ℹ External access: not reachable (tunnel may be down or DNS propagating)"
fi

echo ""
echo "Cron Job Status:"
if [[ -f /etc/cron.d/k3s-cluster-backup ]]; then
    echo "  ✓ Hourly backup cron job configured"
    grep "^5 \* \* \* \*" /etc/cron.d/k3s-cluster-backup
else
    echo "  ✗ Cron job not found"
fi

echo ""
echo "Recent Logs:"
echo "  Backup log (last 10 lines):"
tail -n 10 /var/log/k3s-backup.log 2>/dev/null || echo "    (no logs yet)"

echo ""
echo "  Restore log (last 10 lines):"
tail -n 10 /var/log/k3s-restore.log 2>/dev/null || echo "    (no logs yet)"

echo ""
echo "K3s Cluster Status:"
kubectl get nodes 2>/dev/null || echo "  ✗ K3s not accessible"
kubectl get pods -n insightlearn 2>/dev/null | head -n 5 || echo "  ✗ No pods found"

echo ""
echo "==================================="
EOF

chmod +x "$MONITOR_SCRIPT"
log "  ✓ Monitoring script created: $MONITOR_SCRIPT"

# Final summary
echo ""
log "=========================================="
log "Installation Complete! ✓"
log "=========================================="
echo ""
info "Summary:"
info "  • Backup script: $SCRIPT_DIR/backup-cluster-state.sh"
info "  • Restore script: $SCRIPT_DIR/restore-cluster-state.sh"
info "  • Cloudflare check: $SCRIPT_DIR/verify-cloudflare-tunnel.sh"
info "  • K3s systemd service: /etc/systemd/system/k3s-auto-restore.service (enabled)"
info "  • Cloudflare systemd service: /etc/systemd/system/cloudflared-tunnel.service (if installed)"
info "  • Cron job: /etc/cron.d/k3s-cluster-backup (hourly at :05)"
info "  • Backup location: /var/backups/k3s-cluster/"
info "  • Logs: /var/log/k3s-backup.log and /var/log/k3s-restore.log"
echo ""
info "Automatic behavior:"
info "  • Backup: Every hour at :05 (overwrites previous snapshot)"
info "  • Restore: Automatically at system boot if cluster is empty"
info "  • Cloudflare Tunnel: Automatically verified/started after restore"
echo ""
info "Manual operations:"
info "  • Run backup now: sudo $SCRIPT_DIR/backup-cluster-state.sh"
info "  • Test restore: sudo $SCRIPT_DIR/restore-cluster-state.sh"
info "  • Check Cloudflare: sudo $SCRIPT_DIR/verify-cloudflare-tunnel.sh"
info "  • Check status: sudo $MONITOR_SCRIPT"
info "  • View logs: sudo tail -f /var/log/k3s-backup.log"
info "  • View Cloudflare logs: sudo journalctl -u cloudflared-tunnel.service -f"
echo ""
log "=========================================="
echo ""

exit 0
