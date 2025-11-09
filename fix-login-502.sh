#!/bin/bash
# Fix 502 Bad Gateway - Import new API image with authentication endpoints
set -e

echo "╔════════════════════════════════════════════════════════════╗"
echo "║  Fix 502 Bad Gateway - Deploy Authentication Endpoints    ║"
echo "╚════════════════════════════════════════════════════════════╝"
echo ""

# Check if image tar exists
if [ ! -f "/tmp/insightlearn-api.tar" ]; then
    echo "❌ Error: /tmp/insightlearn-api.tar not found"
    echo ""
    echo "Building new image first..."
    podman build -t insightlearn/api:latest -f Dockerfile .
    echo ""
    echo "Saving image..."
    podman save insightlearn/api:latest -o /tmp/insightlearn-api.tar
    echo "✅ Image saved to /tmp/insightlearn-api.tar"
    echo ""
fi

echo "📦 Step 1/4: Importing new API image to K3s..."
echo "   (This requires sudo privileges)"
echo ""

# Import image
sudo /usr/local/bin/k3s ctr images import /tmp/insightlearn-api.tar

echo ""
echo "✅ Image imported successfully!"
echo ""

# Verify import
echo "📋 Verifying image in K3s..."
sudo /usr/local/bin/k3s crictl images | grep insightlearn/api || echo "⚠️  Warning: Could not verify image"
echo ""

echo "🔄 Step 2/4: Deleting old API pods..."
kubectl delete pod -n insightlearn -l app=insightlearn-api --grace-period=5

echo ""
echo "⏳ Step 3/4: Waiting for new pods to start..."
sleep 10

kubectl wait --for=condition=ready pod -l app=insightlearn-api -n insightlearn --timeout=120s

echo ""
echo "✅ New API pods are ready!"
echo ""

echo "📊 Step 4/4: Checking pod status and logs..."
echo ""
kubectl get pods -n insightlearn -l app=insightlearn-api -o wide

echo ""
echo "📋 Checking logs for admin user creation..."
echo ""
kubectl logs -n insightlearn -l app=insightlearn-api --tail=200 | grep -E '\[SEED\]|\[DATABASE\]|\[AUTH\]' || echo "Fetching all logs..."
echo ""

echo "╔════════════════════════════════════════════════════════════╗"
echo "║  Deployment Complete!                                      ║"
echo "╚════════════════════════════════════════════════════════════╝"
echo ""
echo "🔐 Admin Credentials:"
echo "   Email: admin@insightlearn.cloud"
echo "   Password: Admin@InsightLearn2025!"
echo ""
echo "🌐 Test login at: https://wasm.insightlearn.cloud/login"
echo ""
echo "🧪 Test endpoint manually:"
echo "   curl -X POST http://localhost:8081/api/auth/login \\"
echo "     -H 'Content-Type: application/json' \\"
echo "     -d '{\"email\":\"admin@insightlearn.cloud\",\"password\":\"Admin@InsightLearn2025!\"}'"
echo ""
