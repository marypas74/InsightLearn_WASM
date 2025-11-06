#!/bin/bash
# InsightLearn WASM - Initial Setup Script
# This script sets up the entire development environment from scratch

set -e  # Exit on error

echo "=================================================="
echo "  InsightLearn WASM - Initial Setup"
echo "  Version: 1.5.0-dev"
echo "=================================================="
echo ""

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Check prerequisites
echo -e "${YELLOW}[1/8] Checking prerequisites...${NC}"
command -v dotnet >/dev/null 2>&1 || { echo -e "${RED}ERROR: dotnet is not installed${NC}"; exit 1; }
command -v kubectl >/dev/null 2>&1 || { echo -e "${RED}ERROR: kubectl is not installed${NC}"; exit 1; }
command -v minikube >/dev/null 2>&1 || { echo -e "${RED}ERROR: minikube is not installed${NC}"; exit 1; }
echo -e "${GREEN}✓ All prerequisites found${NC}"

# Check .env file
echo -e "${YELLOW}[2/8] Checking environment configuration...${NC}"
if [ ! -f .env ]; then
    echo -e "${YELLOW}Creating .env from .env.example...${NC}"
    if [ -f .env.example ]; then
        cp .env.example .env
        echo -e "${RED}⚠ IMPORTANT: Edit .env and replace placeholder passwords!${NC}"
        echo -e "${RED}   Run: nano .env${NC}"
        exit 1
    else
        echo -e "${RED}ERROR: .env.example not found${NC}"
        exit 1
    fi
else
    echo -e "${GREEN}✓ .env file exists${NC}"
fi

# Source environment variables
source .env

# Restore NuGet packages
echo -e "${YELLOW}[3/8] Restoring NuGet packages...${NC}"
dotnet restore InsightLearn.WASM.sln
echo -e "${GREEN}✓ Packages restored${NC}"

# Build solution
echo -e "${YELLOW}[4/8] Building solution...${NC}"
dotnet build InsightLearn.WASM.sln -c Release
echo -e "${GREEN}✓ Solution built successfully${NC}"

# Check minikube status
echo -e "${YELLOW}[5/8] Checking Kubernetes cluster...${NC}"
if minikube status &>/dev/null; then
    echo -e "${GREEN}✓ Minikube is running${NC}"
else
    echo -e "${YELLOW}Starting minikube with Podman driver...${NC}"
    minikube config set rootless true
    minikube start --driver=podman --container-runtime=cri-o \
                   --memory=9216 --cpus=6 \
                   --base-image=gcr.io/k8s-minikube/kicbase-rocky:v0.0.48
    echo -e "${GREEN}✓ Minikube started${NC}"
fi

# Enable required addons
echo -e "${YELLOW}[6/8] Enabling Kubernetes addons...${NC}"
minikube addons enable ingress
echo -e "${GREEN}✓ Ingress enabled${NC}"

# Create namespace
echo -e "${YELLOW}[7/8] Creating Kubernetes namespace...${NC}"
kubectl create namespace insightlearn --dry-run=client -o yaml | kubectl apply -f -
echo -e "${GREEN}✓ Namespace created/verified${NC}"

# Create Kubernetes secrets
echo -e "${YELLOW}[8/8] Creating Kubernetes secrets...${NC}"
kubectl create secret generic sqlserver-secret \
  --from-literal=SA_PASSWORD="${MSSQL_SA_PASSWORD}" \
  -n insightlearn --dry-run=client -o yaml | kubectl apply -f -

kubectl create secret generic mongodb-secret \
  --from-literal=MONGO_INITDB_ROOT_PASSWORD="${MONGO_PASSWORD}" \
  -n insightlearn --dry-run=client -o yaml | kubectl apply -f -

kubectl create secret generic redis-secret \
  --from-literal=REDIS_PASSWORD="${REDIS_PASSWORD}" \
  -n insightlearn --dry-run=client -o yaml | kubectl apply -f -

echo -e "${GREEN}✓ Secrets created${NC}"

echo ""
echo -e "${GREEN}=================================================="
echo -e "  Setup completed successfully!"
echo -e "==================================================${NC}"
echo ""
echo "Next steps:"
echo "  1. Run: ./deploy.sh        # Deploy to Kubernetes"
echo "  2. Run: ./start-all.sh     # Start port-forwards and tunnel"
echo ""
echo "Documentation: ./DEPLOYMENT-COMPLETE-GUIDE.md"
