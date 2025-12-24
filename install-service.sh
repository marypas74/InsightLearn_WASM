#!/bin/bash
###############################################################################
# Install InsightLearn API proxy as systemd service
# This makes the API accessible at https://www.insightlearn.cloud automatically
###############################################################################

set -e

if [[ $EUID -ne 0 ]]; then
   echo "❌ This script must be run as root (use sudo)"
   exit 1
fi

echo "╔══════════════════════════════════════════════════════════════╗"
echo "║  Installing InsightLearn API Proxy Service                   ║"
echo "╚══════════════════════════════════════════════════════════════╝"
echo ""

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# 1. Setup iptables redirect
echo "[1/4] Setting up iptables redirect 80→8080..."
sysctl -w net.ipv4.ip_forward=1 >/dev/null
iptables -t nat -C PREROUTING -p tcp --dport 80 -j REDIRECT --to-port 8080 2>/dev/null || \
    iptables -t nat -A PREROUTING -p tcp --dport 80 -j REDIRECT --to-port 8080
echo "✅ iptables configured"

# 2. Make iptables persistent
echo "[2/4] Making iptables rules persistent..."
if command -v iptables-save >/dev/null; then
    iptables-save > /etc/sysconfig/iptables 2>/dev/null || true
fi
echo "✅ iptables rules saved"

# 3. Install systemd service
echo "[3/4] Installing systemd service..."
cp "$SCRIPT_DIR/insightlearn-proxy.service" /etc/systemd/system/
systemctl daemon-reload
systemctl enable insightlearn-proxy.service
echo "✅ Service installed and enabled"

# 4. Start service
echo "[4/4] Starting service..."
systemctl start insightlearn-proxy.service
sleep 3

# Check status
if systemctl is-active --quiet insightlearn-proxy.service; then
    echo "✅ Service is running"
else
    echo "⚠️  Service may not be running, checking status..."
    systemctl status insightlearn-proxy.service --no-pager
fi

echo ""
echo "╔══════════════════════════════════════════════════════════════╗"
echo "║  Installation Complete!                                       ║"
echo "╚══════════════════════════════════════════════════════════════╝"
echo ""
echo "✅ API is now accessible at:"
echo "   • http://localhost"
echo "   • https://www.insightlearn.cloud"
echo ""
echo "📊 Service management:"
echo "   sudo systemctl status insightlearn-proxy"
echo "   sudo systemctl restart insightlearn-proxy"
echo "   sudo systemctl stop insightlearn-proxy"
echo "   sudo journalctl -u insightlearn-proxy -f"
echo ""
echo "🔥 The service will start automatically on boot!"
echo ""

# Test endpoint
echo "Testing endpoint..."
sleep 2
if curl -s http://localhost:8080/health | grep -q "Healthy"; then
    echo "✅ API responds: Healthy"
else
    echo "⚠️  API not responding yet, may need a few more seconds"
fi
