#!/bin/bash
# Script to deploy InsightLearn to Kubernetes (minikube)
# Supports versioned deployments via IMAGE_VERSION environment variable

set -e

echo "Deploying InsightLearn to Kubernetes..."

# Check if kubectl is available
if ! command -v kubectl &> /dev/null; then
    echo "Error: kubectl is not installed"
    exit 1
fi

# Check if minikube is running
if ! kubectl cluster-info &> /dev/null; then
    echo "Error: Kubernetes cluster is not accessible. Is minikube running?"
    echo "Start minikube with: minikube start"
    exit 1
fi

# Get the script directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

# Get version information
if [ -f "$PROJECT_ROOT/Directory.Build.props" ]; then
    VERSION=$(grep '<VersionPrefix>' "$PROJECT_ROOT/Directory.Build.props" | sed 's/.*<VersionPrefix>\(.*\)<\/VersionPrefix>.*/\1/')
    DEFAULT_IMAGE_VERSION="v${VERSION}"
else
    DEFAULT_IMAGE_VERSION="latest"
fi

# Use IMAGE_VERSION env var if set, otherwise use version from props
IMAGE_VERSION="${IMAGE_VERSION:-$DEFAULT_IMAGE_VERSION}"

echo "Applying Kubernetes manifests from: $SCRIPT_DIR"
echo "Using image version: $IMAGE_VERSION"
echo ""

# Apply manifests in order
echo ""
echo "=== Creating namespace ==="
kubectl apply -f "$SCRIPT_DIR/00-namespace.yaml"

echo ""
echo "=== Creating secrets ==="
kubectl apply -f "$SCRIPT_DIR/01-secrets.yaml"

echo ""
echo "=== Creating configmap ==="
kubectl apply -f "$SCRIPT_DIR/02-configmap.yaml"

echo ""
echo "=== Deploying SQL Server ==="
kubectl apply -f "$SCRIPT_DIR/03-sqlserver-statefulset.yaml"

echo ""
echo "=== Deploying Redis ==="
kubectl apply -f "$SCRIPT_DIR/04-redis-deployment.yaml"

echo ""
echo "=== Deploying Elasticsearch ==="
kubectl apply -f "$SCRIPT_DIR/05-elasticsearch-deployment.yaml"

echo ""
echo "=== Deploying MongoDB ==="
kubectl apply -f "$SCRIPT_DIR/13-mongodb-statefulset.yaml"

echo ""
echo "=== Deploying Ollama (LLM) ==="
kubectl apply -f "$SCRIPT_DIR/12-ollama-deployment.yaml"

echo ""
echo "Waiting for databases to be ready (this may take a few minutes)..."
kubectl wait --for=condition=ready pod -l app=sqlserver -n insightlearn --timeout=300s || echo "Warning: SQL Server might not be ready yet"
kubectl wait --for=condition=ready pod -l app=redis -n insightlearn --timeout=120s || echo "Warning: Redis might not be ready yet"
kubectl wait --for=condition=ready pod -l app=mongodb -n insightlearn --timeout=120s || echo "Warning: MongoDB might not be ready yet"
kubectl wait --for=condition=ready pod -l app=ollama -n insightlearn --timeout=180s || echo "Warning: Ollama might not be ready yet"

echo ""
echo "=== Deploying API ==="
envsubst < "$SCRIPT_DIR/06-api-deployment.yaml" | kubectl apply -f -

echo ""
echo "=== Deploying Web ==="
envsubst < "$SCRIPT_DIR/07-web-deployment.yaml" | kubectl apply -f -

echo ""
echo "=== Deploying WASM Frontend ==="
kubectl apply -f "$SCRIPT_DIR/12-wasm-deployment.yaml"

echo ""
echo "=== Creating Ingress ==="
kubectl apply -f "$SCRIPT_DIR/08-ingress.yaml"

echo ""
echo "=== Creating NodePort Services ==="
kubectl apply -f "$SCRIPT_DIR/09-nodeport-services.yaml"

echo ""
echo "=== Deployment completed ==="
echo ""
echo "Checking deployment status..."
kubectl get all -n insightlearn

echo ""
echo "========================================="
echo "Deployed version: $IMAGE_VERSION"
echo "========================================="
echo ""
echo "To access the application:"
echo "  1. HTTPS (Production): https://192.168.1.103"
echo "  2. HTTP (Direct):      http://192.168.49.2:31080"
echo ""
echo "To check logs:"
echo "  kubectl logs -n insightlearn -l app=insightlearn-api -f"
echo "  kubectl logs -n insightlearn -l app=insightlearn-web -f"
echo ""
echo "To verify image versions:"
echo "  kubectl get pods -n insightlearn -o jsonpath='{range .items[*]}{.metadata.name}{\"\t\"}{.spec.containers[0].image}{\"\n\"}{end}'"
