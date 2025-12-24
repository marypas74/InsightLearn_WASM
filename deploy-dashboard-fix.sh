#!/bin/bash

echo "=========================================="
echo "InsightLearn Dashboard Fix Deployment"
echo "=========================================="
echo ""
echo "This script will deploy the fixed dashboard routing"
echo ""

# Check if the Docker image was built
if docker images | grep -q "localhost/insightlearn/web.*fix-dashboard"; then
    echo "✅ Docker image 'localhost/insightlearn/web:fix-dashboard' found"
else
    echo "❌ Docker image not found. Building..."
    docker build -f Dockerfile.web -t localhost/insightlearn/web:fix-dashboard --build-arg BUILD_CONFIG=Release .
fi

# Tag as latest
echo ""
echo "Tagging image as latest..."
docker tag localhost/insightlearn/web:fix-dashboard localhost/insightlearn/web:latest

# Import to K3s (requires sudo)
echo ""
echo "⚠️  You need to import the image to K3s. Please run this command with your sudo password:"
echo ""
echo "sudo sh -c 'docker save localhost/insightlearn/web:latest | /usr/local/bin/k3s ctr images import -'"
echo ""
echo "Press Enter after importing the image to continue..."
read -r

# Update the deployment
echo ""
echo "Updating Kubernetes deployment..."
kubectl set image deployment/insightlearn-web insightlearn-web=localhost/insightlearn/web:latest -n insightlearn

# Force a restart to pull new image
echo "Restarting deployment..."
kubectl rollout restart deployment/insightlearn-web -n insightlearn

# Wait for rollout
echo "Waiting for rollout to complete..."
kubectl rollout status deployment/insightlearn-web -n insightlearn --timeout=120s

# Check pod status
echo ""
echo "Checking pod status..."
kubectl get pods -n insightlearn | grep insightlearn-web

echo ""
echo "=========================================="
echo "Dashboard Fix Deployment Complete!"
echo "=========================================="
echo ""
echo "TESTING INSTRUCTIONS:"
echo ""
echo "1. Clear browser cache (Ctrl+Shift+R)"
echo "2. Navigate to: https://www.insightlearn.cloud/login"
echo "3. Login with admin credentials:"
echo "   Email: admin@insightlearn.cloud"
echo "   Password: Admin123!@#"
echo ""
echo "4. After successful login, you should be redirected to:"
echo "   → https://www.insightlearn.cloud/admin/dashboard"
echo ""
echo "If you still see issues:"
echo "- Check browser console for errors (F12)"
echo "- Try incognito/private browsing mode"
echo "- Check that JWT token is saved in localStorage"
echo ""
echo "To check API endpoints:"
echo "curl -H 'Authorization: Bearer YOUR_TOKEN' https://api.insightlearn.cloud/api/admin/dashboard/stats"