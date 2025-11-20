#!/bin/bash
# Deploy WebAssembly Frontend v2.1.0-dev to K3s
# Requires sudo access for K3s ctr images import

set -e

VERSION="2.1.0-dev"
IMAGE_NAME="localhost/insightlearn/wasm"
TAR_FILE="/tmp/wasm-blazor-2.1.0-dev.tar"

echo "=========================================="
echo "InsightLearn WASM Deployment v${VERSION}"
echo "=========================================="
echo ""

# Step 1: Check if tar file exists
if [ ! -f "$TAR_FILE" ]; then
    echo "Error: Image tar file not found at $TAR_FILE"
    echo "Creating tar file from Docker image..."
    docker save ${IMAGE_NAME}:${VERSION} -o "$TAR_FILE"
    echo "✅ Tar file created"
fi

echo "Step 1: Import image into K3s containerd (requires sudo)"
echo "----------------------------------------"
sudo /usr/local/bin/k3s ctr images import "$TAR_FILE"

if [ $? -eq 0 ]; then
    echo "✅ Image imported successfully"
else
    echo "❌ Image import failed"
    exit 1
fi

echo ""
echo "Step 2: Verify image in K3s"
echo "----------------------------------------"
sudo /usr/local/bin/k3s ctr images ls | grep "${IMAGE_NAME}"

echo ""
echo "Step 3: Restart deployment to use new image"
echo "----------------------------------------"
kubectl rollout restart deployment/insightlearn-wasm-blazor-webassembly -n insightlearn

echo ""
echo "Step 4: Wait for rollout to complete"
echo "----------------------------------------"
kubectl rollout status deployment/insightlearn-wasm-blazor-webassembly -n insightlearn --timeout=120s

echo ""
echo "Step 5: Verify new pod is running"
echo "----------------------------------------"
kubectl get pods -n insightlearn | grep wasm

echo ""
echo "=========================================="
echo "✅ Deployment completed successfully!"
echo "=========================================="
echo ""
echo "WebAssembly Frontend v${VERSION} is now running"
echo ""
echo "Access the application:"
echo "  NodePort: http://localhost:31090"
echo ""
echo "Check logs:"
echo "  kubectl logs -n insightlearn -l app=insightlearn-wasm-blazor-webassembly -f"
echo ""
echo "Verify version:"
echo "  kubectl get deployment insightlearn-wasm-blazor-webassembly -n insightlearn -o jsonpath='{.spec.template.spec.containers[0].image}'"
echo ""
