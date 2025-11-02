#!/bin/bash
# Script to remove InsightLearn from Kubernetes

set -e

echo "Removing InsightLearn from Kubernetes..."

# Get the script directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Delete in reverse order
echo "=== Deleting Ingress ==="
kubectl delete -f "$SCRIPT_DIR/08-ingress.yaml" --ignore-not-found=true

echo ""
echo "=== Deleting Web deployment ==="
kubectl delete -f "$SCRIPT_DIR/07-web-deployment.yaml" --ignore-not-found=true

echo ""
echo "=== Deleting API deployment ==="
kubectl delete -f "$SCRIPT_DIR/06-api-deployment.yaml" --ignore-not-found=true

echo ""
echo "=== Deleting Elasticsearch ==="
kubectl delete -f "$SCRIPT_DIR/05-elasticsearch-deployment.yaml" --ignore-not-found=true

echo ""
echo "=== Deleting Redis ==="
kubectl delete -f "$SCRIPT_DIR/04-redis-deployment.yaml" --ignore-not-found=true

echo ""
echo "=== Deleting SQL Server ==="
kubectl delete -f "$SCRIPT_DIR/03-sqlserver-statefulset.yaml" --ignore-not-found=true

echo ""
echo "=== Deleting ConfigMap ==="
kubectl delete -f "$SCRIPT_DIR/02-configmap.yaml" --ignore-not-found=true

echo ""
echo "=== Deleting Secrets ==="
kubectl delete -f "$SCRIPT_DIR/01-secrets.yaml" --ignore-not-found=true

echo ""
echo "Do you want to delete the namespace and all persistent data? (y/N)"
read -r response
if [[ "$response" =~ ^[Yy]$ ]]; then
    echo "=== Deleting namespace (this will delete all persistent volumes) ==="
    kubectl delete -f "$SCRIPT_DIR/00-namespace.yaml" --ignore-not-found=true
    echo "Namespace deleted."
else
    echo "Namespace preserved. PVCs still exist."
fi

echo ""
echo "=== Cleanup completed ==="
