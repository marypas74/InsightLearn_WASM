#!/bin/bash

# Force update pods in minikube by deleting them
# This script ensures the latest local images are used

set -e

NAMESPACE="insightlearn"

echo "========================================="
echo "Force Update InsightLearn Pods"
echo "========================================="
echo ""

# Function to force update a deployment
force_update_deployment() {
    local deployment=$1
    echo ">>> Forcing update for deployment: $deployment"

    # Add a timestamp annotation to force pod recreation
    kubectl patch deployment "$deployment" -n "$NAMESPACE" \
        -p "{\"spec\":{\"template\":{\"metadata\":{\"annotations\":{\"force-update\":\"$(date +%s)\"}}}}}"

    echo "✓ Deployment $deployment updated with new annotation"
    echo ""
}

# Update web deployment
force_update_deployment "insightlearn-web"

# Update API deployment
force_update_deployment "insightlearn-api"

echo "========================================="
echo "Waiting for rollout to complete..."
echo "========================================="
echo ""

# Wait for both deployments
kubectl rollout status deployment/insightlearn-web -n "$NAMESPACE" --timeout=180s
kubectl rollout status deployment/insightlearn-api -n "$NAMESPACE" --timeout=180s

echo ""
echo "========================================="
echo "✓ All deployments updated successfully!"
echo "========================================="
echo ""

# Show current pods
echo "Current pods:"
kubectl get pods -n "$NAMESPACE" -l 'app in (insightlearn-web,insightlearn-api)' -o wide

echo ""
echo "To verify images:"
echo "  kubectl get pods -n $NAMESPACE -o jsonpath='{range .items[*]}{.metadata.name}{\": \"}{.spec.containers[0].image}{\"\\n\"}{end}'"
