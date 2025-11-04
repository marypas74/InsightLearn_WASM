#!/bin/bash
###############################################################################
# Expose InsightLearn API on port 80 for Cloudflare access
###############################################################################

echo "╔═══════════════════════════════════════════════════════════╗"
echo "║  Exposing InsightLearn API on port 80                    ║"
echo "╚═══════════════════════════════════════════════════════════╝"
echo ""

# Kill existing port-forwards
pkill -f "kubectl port-forward.*api-service" 2>/dev/null || true
sleep 1

echo "Starting port-forward on 0.0.0.0:80..."
echo "This will make the API accessible to Cloudflare"
echo ""

# Run with sudo and keep running
sudo -E kubectl port-forward -n insightlearn --address 0.0.0.0 service/api-service 80:80
