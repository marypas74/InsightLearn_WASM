#!/bin/bash
# Deploy script for instructor dashboard fix (v2.1.0-dev)
# This script imports the new Docker images and restarts the deployments

set -e

echo "==================================="
echo "InsightLearn Instructor Dashboard Fix"
echo "Deploying v2.1.0-dev with /api/dashboard/instructor endpoint"
echo "==================================="

# Import API image
echo "[1/4] Importing API image to K3s..."
sudo /usr/local/bin/k3s ctr images import /tmp/api-image.tar
echo "API image imported successfully!"

# Import WASM image
echo "[2/4] Importing WASM image to K3s..."
sudo /usr/local/bin/k3s ctr images import /tmp/wasm-image.tar
echo "WASM image imported successfully!"

# Tag images
echo "[3/4] Tagging images..."
sudo /usr/local/bin/k3s ctr images tag localhost/insightlearn/api:latest localhost/insightlearn/api:instructor-fix
sudo /usr/local/bin/k3s ctr images tag localhost/insightlearn/wasm:latest localhost/insightlearn/wasm:instructor-fix

# Update deployments
echo "[4/4] Updating Kubernetes deployments..."
kubectl set image deployment/insightlearn-api -n insightlearn \
  insightlearn-api=localhost/insightlearn/api:latest
kubectl set image deployment/insightlearn-wasm-blazor-webassembly -n insightlearn \
  insightlearn-wasm=localhost/insightlearn/wasm:latest

# Wait for rollout
echo "Waiting for API deployment rollout..."
kubectl rollout status deployment/insightlearn-api -n insightlearn --timeout=120s

echo "Waiting for WASM deployment rollout..."
kubectl rollout status deployment/insightlearn-wasm-blazor-webassembly -n insightlearn --timeout=120s

# Verify
echo ""
echo "==================================="
echo "Deployment complete! Verifying..."
echo "==================================="
kubectl get pods -n insightlearn | grep -E "api|wasm"

echo ""
echo "Test the instructor dashboard:"
echo "  curl http://localhost:31081/api/dashboard/instructor -H 'Authorization: Bearer <TOKEN>'"
echo ""
echo "Login as john.smith@instructors.insightlearn.cloud (password: Pa\$\$W0rd)"
echo "to verify courses are now visible on the dashboard."
