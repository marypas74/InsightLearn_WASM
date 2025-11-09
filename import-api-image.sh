#!/bin/bash
# Import API Docker image into K3s containerd
#
# This script must be run with sudo privileges to access K3s containerd socket
#
# Usage: sudo ./import-api-image.sh

set -e

echo "🔄 Importing InsightLearn API image into K3s..."

if [ ! -f "/tmp/insightlearn-api.tar" ]; then
    echo "❌ Error: /tmp/insightlearn-api.tar not found"
    echo "   Please build the image first with: podman build -t insightlearn/api:latest -f Dockerfile ."
    echo "   Then save it with: podman save insightlearn/api:latest -o /tmp/insightlearn-api.tar"
    exit 1
fi

echo "📦 Image file found: /tmp/insightlearn-api.tar"
echo "⬆️  Importing into K3s containerd..."

/usr/local/bin/k3s ctr images import /tmp/insightlearn-api.tar

echo "✅ Image imported successfully!"
echo ""
echo "📋 Verify with: k3s ctr images ls | grep insightlearn"
echo "🔄 Restart API pods with: kubectl delete pod -n insightlearn -l app=insightlearn-api"
echo ""
echo "🎯 Next steps:"
echo "   1. Delete existing API pods to force recreation with new image"
echo "   2. Watch pods starting: kubectl get pods -n insightlearn -w"
echo "   3. Check logs: kubectl logs -n insightlearn -l app=insightlearn-api --tail=50"
