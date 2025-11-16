#!/bin/bash
################################################################################
# Install InsightLearn HA Watchdog System
#
# This script installs the complete HA auto-healing system:
# - Watchdog script in /usr/local/bin
# - Systemd service and timer
# - Enables automatic startup
#
# Usage: sudo ./install-ha-watchdog.sh
################################################################################

set -euo pipefail

# Check if running as root
if [[ $EUID -ne 0 ]]; then
   echo "ERROR: This script must be run as root"
   echo "Usage: sudo $0"
   exit 1
fi

echo "=========================================="
echo "InsightLearn HA Watchdog Installation"
echo "=========================================="
echo ""

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

# Step 1: Install watchdog script
echo "[1/5] Installing watchdog script..."
cp "$SCRIPT_DIR/insightlearn-ha-watchdog.sh" /usr/local/bin/insightlearn-ha-watchdog.sh
chmod +x /usr/local/bin/insightlearn-ha-watchdog.sh
echo "  ✓ Installed: /usr/local/bin/insightlearn-ha-watchdog.sh"
echo ""

# Step 2: Install systemd service
echo "[2/5] Installing systemd service..."
cp "$SCRIPT_DIR/insightlearn-ha-watchdog.service" /etc/systemd/system/
echo "  ✓ Installed: /etc/systemd/system/insightlearn-ha-watchdog.service"
echo ""

# Step 3: Install systemd timer
echo "[3/5] Installing systemd timer..."
cp "$SCRIPT_DIR/insightlearn-ha-watchdog.timer" /etc/systemd/system/
echo "  ✓ Installed: /etc/systemd/system/insightlearn-ha-watchdog.timer"
echo ""

# Step 4: Reload systemd and enable services
echo "[4/5] Enabling services..."
systemctl daemon-reload
systemctl enable insightlearn-ha-watchdog.timer
systemctl enable insightlearn-ha-watchdog.service
echo "  ✓ Services enabled"
echo ""

# Step 5: Start timer
echo "[5/5] Starting timer..."
systemctl start insightlearn-ha-watchdog.timer
echo "  ✓ Timer started"
echo ""

# Show status
echo "=========================================="
echo "Installation Complete!"
echo "=========================================="
echo ""
echo "Timer Status:"
systemctl status insightlearn-ha-watchdog.timer --no-pager || true
echo ""
echo "Next Check:"
systemctl list-timers insightlearn-ha-watchdog.timer --no-pager || true
echo ""
echo "=========================================="
echo "HA Watchdog Configuration:"
echo "=========================================="
echo "  • Check interval: Every 2 minutes"
echo "  • First check: 2 minutes after boot"
echo "  • Log file: /var/log/insightlearn-watchdog.log"
echo "  • Min deployments required: 5"
echo "  • Min running pods required: 8"
echo ""
echo "To check logs:"
echo "  tail -f /var/log/insightlearn-watchdog.log"
echo ""
echo "To manually trigger watchdog:"
echo "  sudo /usr/local/bin/insightlearn-ha-watchdog.sh"
echo ""
echo "To stop watchdog:"
echo "  sudo systemctl stop insightlearn-ha-watchdog.timer"
echo ""
