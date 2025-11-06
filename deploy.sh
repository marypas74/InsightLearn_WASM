#!/bin/bash
# InsightLearn WASM - Deployment Script
# Deploys all services to Kubernetes

set -e

echo "=================================================="
echo "  InsightLearn WASM - Kubernetes Deployment"
echo "=================================================="
echo ""

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

# Check if minikube is running
if ! minikube status &>/dev/null; then
    echo -e "${RED}ERROR: Minikube is not running${NC}"
    echo "Run: minikube start --driver=podman --memory=9216 --cpus=6"
    exit 1
fi

# Set minikube docker environment
echo -e "${YELLOW}[1/6] Setting up minikube environment...${NC}"
eval $(minikube podman-env)
echo -e "${GREEN}✓ Environment configured${NC}"

# Build Docker images
echo -e "${YELLOW}[2/6] Building Docker images...${NC}"
echo "  Building API image..."
podman build -t localhost/insightlearn/api:latest -f Dockerfile . 2>&1 | tail -5
minikube image load localhost/insightlearn/api:latest
echo -e "${GREEN}✓ API image built and loaded${NC}"

# Deploy Kubernetes manifests
echo -e "${YELLOW}[3/6] Deploying Kubernetes manifests...${NC}"
kubectl apply -f k8s/ -n insightlearn
echo -e "${GREEN}✓ Manifests applied${NC}"

# Wait for pods to be ready
echo -e "${YELLOW}[4/6] Waiting for pods to be ready...${NC}"
kubectl wait --for=condition=ready pod -l app=sqlserver -n insightlearn --timeout=300s
kubectl wait --for=condition=ready pod -l app=mongodb -n insightlearn --timeout=300s
kubectl wait --for=condition=ready pod -l app=redis -n insightlearn --timeout=300s
echo -e "${GREEN}✓ Database pods ready${NC}"

echo -e "${YELLOW}[5/6] Waiting for API pods...${NC}"
kubectl wait --for=condition=ready pod -l app=api -n insightlearn --timeout=300s
echo -e "${GREEN}✓ API pods ready${NC}"

echo -e "${YELLOW}[6/6] Waiting for WASM pod...${NC}"
kubectl wait --for=condition=ready pod -l component=wasm -n insightlearn --timeout=300s
echo -e "${GREEN}✓ WASM pod ready${NC}"

# Show deployment status
echo ""
echo -e "${GREEN}=================================================="
echo -e "  Deployment completed successfully!"
echo -e "==================================================${NC}"
echo ""
echo "Deployment status:"
kubectl get pods -n insightlearn
echo ""
kubectl get svc -n insightlearn
echo ""
echo "Next steps:"
echo "  1. Run: ./start-all.sh     # Start port-forwards"
echo "  2. Access: http://localhost:8080 (WASM)"
echo "  3. Access: http://localhost:8081 (API)"
