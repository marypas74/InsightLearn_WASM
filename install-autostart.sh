#!/bin/bash
# Install InsightLearn autostart and watchdog services
# Run with: sudo ./install-autostart.sh

set -e

if [ "$EUID" -ne 0 ]; then
   echo "Please run with sudo: sudo ./install-autostart.sh"
   exit 1
fi

echo "=============================================="
echo "  Installing InsightLearn Services"
echo "=============================================="
echo ""

# Install startup service
echo "1/4 Installing startup service..."
cp /tmp/insightlearn-startup.service /etc/systemd/system/

# Install watchdog service
echo "2/4 Installing watchdog service..."
cp /tmp/insightlearn-watchdog.service /etc/systemd/system/

# Create log file for watchdog
echo "3/4 Creating log file..."
touch /var/log/insightlearn-watchdog.log
chown mpasqui:mpasqui /var/log/insightlearn-watchdog.log

# Reload systemd
echo "4/4 Configuring services..."
systemctl daemon-reload

# Disable old service if exists
systemctl disable minikube-start.service 2>/dev/null || true

# Enable new services
systemctl enable insightlearn-startup.service
systemctl enable insightlearn-watchdog.service

echo ""
echo "✅ Services installed and enabled!"
echo ""
echo "================================================"
echo "  Installed Services"
echo "================================================"
echo ""
echo "1. insightlearn-startup.service"
echo "   - Runs at boot (with 20s delay)"
echo "   - Starts minikube, port-forwards, tunnel"
echo ""
echo "2. insightlearn-watchdog.service"
echo "   - Monitors continuously (every 30s)"
echo "   - Auto-restarts failed services"
echo "   - Checks: minikube, pods, port-forwards, tunnel"
echo ""
echo "================================================"
echo "  Commands"
echo "================================================"
echo ""
echo "Start services now:"
echo "  sudo systemctl start insightlearn-startup.service"
echo "  sudo systemctl start insightlearn-watchdog.service"
echo ""
echo "Check status:"
echo "  sudo systemctl status insightlearn-startup.service"
echo "  sudo systemctl status insightlearn-watchdog.service"
echo ""
echo "View logs:"
echo "  sudo journalctl -u insightlearn-startup.service -f"
echo "  sudo journalctl -u insightlearn-watchdog.service -f"
echo "  tail -f /var/log/insightlearn-watchdog.log"
echo ""
