#!/bin/bash

# Quick update script for development
# Scales down replicas, builds images, loads to minikube, then scales back up
# This ensures pods always use fresh images without cache issues

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
NAMESPACE="insightlearn"

echo "========================================="
echo "Quick Update - InsightLearn"
echo "========================================="
echo ""

# Parse arguments
BUILD_ALL=false
BUILD_API=false
BUILD_WEB=false
NO_CACHE=false

while [[ $# -gt 0 ]]; do
    case $1 in
        --all)
            BUILD_ALL=true
            shift
            ;;
        --api)
            BUILD_API=true
            shift
            ;;
        --web)
            BUILD_WEB=true
            shift
            ;;
        --no-cache)
            NO_CACHE=true
            shift
            ;;
        *)
            echo "Unknown option: $1"
            echo "Usage: $0 [--all|--api|--web] [--no-cache]"
            exit 1
            ;;
    esac
done

# Default to all if nothing specified
if [ "$BUILD_ALL" = false ] && [ "$BUILD_API" = false ] && [ "$BUILD_WEB" = false ]; then
    BUILD_ALL=true
fi

# Function to save current replica count
save_replica_count() {
    local deployment=$1
    local replicas=$(kubectl get deployment "$deployment" -n "$NAMESPACE" -o jsonpath='{.spec.replicas}' 2>/dev/null || echo "1")
    echo "$replicas"
}

# Function to scale deployment
scale_deployment() {
    local deployment=$1
    local replicas=$2
    echo ">>> Scaling $deployment to $replicas replicas..."
    kubectl scale deployment "$deployment" -n "$NAMESPACE" --replicas="$replicas"
}

# Save current replica counts
echo ">>> Saving current replica counts..."
if [ "$BUILD_ALL" = true ] || [ "$BUILD_API" = true ]; then
    API_REPLICAS=$(save_replica_count "insightlearn-api")
    # Ensure at least 1 replica
    if [ "$API_REPLICAS" -eq 0 ]; then
        API_REPLICAS=1
        echo "  API: 0 replicas found, will restore to 1"
    else
        echo "  API: $API_REPLICAS replicas"
    fi
fi

if [ "$BUILD_ALL" = true ] || [ "$BUILD_WEB" = true ]; then
    WEB_REPLICAS=$(save_replica_count "insightlearn-web")
    # Ensure at least 1 replica
    if [ "$WEB_REPLICAS" -eq 0 ]; then
        WEB_REPLICAS=1
        echo "  Web: 0 replicas found, will restore to 1"
    else
        echo "  Web: $WEB_REPLICAS replicas"
    fi
fi
echo ""

# Scale down to 0
echo ">>> Scaling down deployments to 0..."
if [ "$BUILD_ALL" = true ] || [ "$BUILD_API" = true ]; then
    scale_deployment "insightlearn-api" 0
fi

if [ "$BUILD_ALL" = true ] || [ "$BUILD_WEB" = true ]; then
    scale_deployment "insightlearn-web" 0
fi

# Wait for pods to terminate
echo ">>> Waiting for pods to terminate..."
sleep 5
echo ""

cd "$PROJECT_ROOT"

# Determine build flags
BUILD_FLAGS=""
if [ "$NO_CACHE" = true ]; then
    BUILD_FLAGS="--no-cache"
    echo "⚠️  Building with --no-cache (fresh build, ignoring Docker cache)"
    echo ""
fi

# Build images
if [ "$BUILD_ALL" = true ] || [ "$BUILD_API" = true ]; then
    echo ">>> Building API image..."
    docker build $BUILD_FLAGS -f Dockerfile -t insightlearn/api:latest -t insightlearn/api:v1.2.1-dev .
    echo "✓ API image built"
    echo ""
fi

if [ "$BUILD_ALL" = true ] || [ "$BUILD_WEB" = true ]; then
    echo ">>> Building Web image..."
    docker build $BUILD_FLAGS -f Dockerfile.web -t insightlearn/web:latest -t insightlearn/web:v1.2.1-dev .
    echo "✓ Web image built"
    echo ""
fi

# Remove old images from minikube to ensure fresh pull
echo ">>> Removing old images from minikube..."
if [ "$BUILD_ALL" = true ] || [ "$BUILD_API" = true ]; then
    minikube ssh "docker rmi -f insightlearn/api:latest insightlearn/api:v1.2.1-dev 2>/dev/null || true"
fi

if [ "$BUILD_ALL" = true ] || [ "$BUILD_WEB" = true ]; then
    minikube ssh "docker rmi -f insightlearn/web:latest insightlearn/web:v1.2.1-dev 2>/dev/null || true"
fi
echo "✓ Old images removed"
echo ""

# Load fresh images to minikube
echo ">>> Loading fresh images to minikube..."
if [ "$BUILD_ALL" = true ] || [ "$BUILD_API" = true ]; then
    minikube image load insightlearn/api:latest
    minikube image load insightlearn/api:v1.2.1-dev
    echo "✓ API image loaded"
fi

if [ "$BUILD_ALL" = true ] || [ "$BUILD_WEB" = true ]; then
    minikube image load insightlearn/web:latest
    minikube image load insightlearn/web:v1.2.1-dev
    echo "✓ Web image loaded"
fi
echo ""

# Scale deployments back up to original replica count
echo ">>> Scaling deployments back up..."
if [ "$BUILD_ALL" = true ] || [ "$BUILD_API" = true ]; then
    scale_deployment "insightlearn-api" "$API_REPLICAS"
fi

if [ "$BUILD_ALL" = true ] || [ "$BUILD_WEB" = true ]; then
    scale_deployment "insightlearn-web" "$WEB_REPLICAS"
fi
echo ""

# Wait for deployments to be ready
echo ">>> Waiting for deployments to be ready..."
if [ "$BUILD_ALL" = true ] || [ "$BUILD_API" = true ]; then
    kubectl rollout status deployment/insightlearn-api -n "$NAMESPACE" --timeout=180s
fi

if [ "$BUILD_ALL" = true ] || [ "$BUILD_WEB" = true ]; then
    kubectl rollout status deployment/insightlearn-web -n "$NAMESPACE" --timeout=180s
fi

echo ""
echo "========================================="
echo "✓ Quick update completed!"
echo "========================================="
echo ""

# Show current pods
kubectl get pods -n "$NAMESPACE" -l 'app in (insightlearn-web,insightlearn-api)'

echo ""
echo "Access your app:"
echo "  HTTPS: https://192.168.1.103"
echo "  HTTP:  http://192.168.49.2:31080"
