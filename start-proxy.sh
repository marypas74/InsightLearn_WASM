#!/bin/bash

###############################################################################
# Start kubectl port-forward to expose InsightLearn API on port 80
# This makes the API accessible to Cloudflare
###############################################################################

echo "╔═══════════════════════════════════════════════════════════╗"
echo "║  InsightLearn - Expose API on port 80                    ║"
echo "╚═══════════════════════════════════════════════════════════╝"
echo ""

# Check if running as root
if [[ $EUID -ne 0 ]]; then
   echo "❌ This script must be run as root (use sudo)"
   echo "   Reason: Port 80 requires root privileges"
   exit 1
fi

echo "Starting kubectl port-forward..."
echo "API will be accessible at:"
echo "  • http://localhost"
echo "  • http://wasm.insightlearn.cloud (via Cloudflare)"
echo ""
echo "Press Ctrl+C to stop"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""

# Run as the original user (not root) to access kubectl config
ORIGINAL_USER=${SUDO_USER:-$USER}
KUBECONFIG="/home/$ORIGINAL_USER/.kube/config"

# Port forward
su - $ORIGINAL_USER -c "kubectl port-forward -n insightlearn --address 0.0.0.0 service/api-service 80:80"
