#!/bin/bash
# Install InsightLearn autostart service
# Run with: sudo ./install-autostart.sh

set -e

if [ "$EUID" -ne 0 ]; then 
   echo "Please run with sudo: sudo ./install-autostart.sh"
   exit 1
fi

echo "Installing InsightLearn autostart service..."

# Copy service file
cp /tmp/insightlearn-startup.service /etc/systemd/system/

# Reload systemd
systemctl daemon-reload

# Disable old service if exists
systemctl disable minikube-start.service 2>/dev/null || true

# Enable new service
systemctl enable insightlearn-startup.service

echo "✅ Service installed and enabled!"
echo ""
echo "To test the service:"
echo "  sudo systemctl start insightlearn-startup.service"
echo "  sudo systemctl status insightlearn-startup.service"
echo ""
echo "To view logs:"
echo "  sudo journalctl -u insightlearn-startup.service -f"
