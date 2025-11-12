#!/bin/bash
# Import API image to K3s
set -e

echo "Importing API image to K3s..."
sudo /usr/local/bin/k3s ctr images import /tmp/api-image.tar

echo "Image imported successfully!"
echo "Restarting API deployment..."
kubectl rollout restart deployment/insightlearn-api -n insightlearn

echo "Waiting for rollout to complete..."
kubectl rollout status deployment/insightlearn-api -n insightlearn --timeout=120s

echo "Checking pod status..."
kubectl get pods -n insightlearn -l app=insightlearn-api

echo "Done! API updated with JWT authentication support."
