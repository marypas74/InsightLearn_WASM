#!/bin/bash

# Phase 4.1: Deploy Health Checks Implementation
# This script builds and deploys the updated API with comprehensive health checks

set -e

COLOR_GREEN='\033[0;32m'
COLOR_RED='\033[0;31m'
COLOR_YELLOW='\033[1;33m'
COLOR_BLUE='\033[0;34m'
COLOR_RESET='\033[0m'

echo -e "${COLOR_BLUE}========================================${COLOR_RESET}"
echo -e "${COLOR_BLUE}Phase 4.1: Health Checks Deployment${COLOR_RESET}"
echo -e "${COLOR_BLUE}========================================${COLOR_RESET}"
echo ""

# Step 1: Build Docker image
echo -e "${COLOR_YELLOW}Step 1: Building Docker image...${COLOR_RESET}"
docker build -t localhost/insightlearn/api:latest -f Dockerfile .

if [ $? -eq 0 ]; then
    echo -e "${COLOR_GREEN}✓ Docker image built successfully${COLOR_RESET}"
else
    echo -e "${COLOR_RED}✗ Docker build failed${COLOR_RESET}"
    exit 1
fi
echo ""

# Step 2: Import to K3s
echo -e "${COLOR_YELLOW}Step 2: Importing image to K3s...${COLOR_RESET}"
echo -e "${COLOR_BLUE}(This requires sudo password)${COLOR_RESET}"
docker save localhost/insightlearn/api:latest | sudo /usr/local/bin/k3s ctr images import -

if [ $? -eq 0 ]; then
    echo -e "${COLOR_GREEN}✓ Image imported to K3s successfully${COLOR_RESET}"
else
    echo -e "${COLOR_RED}✗ K3s import failed${COLOR_RESET}"
    exit 1
fi
echo ""

# Step 3: Apply updated deployment
echo -e "${COLOR_YELLOW}Step 3: Applying updated Kubernetes deployment...${COLOR_RESET}"
kubectl apply -f k8s/06-api-deployment.yaml

if [ $? -eq 0 ]; then
    echo -e "${COLOR_GREEN}✓ Deployment configuration applied${COLOR_RESET}"
else
    echo -e "${COLOR_RED}✗ Deployment apply failed${COLOR_RESET}"
    exit 1
fi
echo ""

# Step 4: Restart deployment
echo -e "${COLOR_YELLOW}Step 4: Restarting API deployment...${COLOR_RESET}"
kubectl rollout restart deployment/insightlearn-api -n insightlearn

if [ $? -eq 0 ]; then
    echo -e "${COLOR_GREEN}✓ Deployment restart initiated${COLOR_RESET}"
else
    echo -e "${COLOR_RED}✗ Deployment restart failed${COLOR_RESET}"
    exit 1
fi
echo ""

# Step 5: Wait for rollout
echo -e "${COLOR_YELLOW}Step 5: Waiting for rollout to complete...${COLOR_RESET}"
kubectl rollout status deployment/insightlearn-api -n insightlearn --timeout=120s

if [ $? -eq 0 ]; then
    echo -e "${COLOR_GREEN}✓ Rollout completed successfully${COLOR_RESET}"
else
    echo -e "${COLOR_RED}✗ Rollout failed or timed out${COLOR_RESET}"
    exit 1
fi
echo ""

# Step 6: Verify pod is ready
echo -e "${COLOR_YELLOW}Step 6: Verifying pod readiness...${COLOR_RESET}"
kubectl wait --for=condition=ready pod -l app=insightlearn-api -n insightlearn --timeout=60s

if [ $? -eq 0 ]; then
    echo -e "${COLOR_GREEN}✓ Pod is ready${COLOR_RESET}"
else
    echo -e "${COLOR_RED}✗ Pod not ready${COLOR_RESET}"
    exit 1
fi
echo ""

# Step 7: Get pod details
echo -e "${COLOR_YELLOW}Step 7: Current pod status:${COLOR_RESET}"
kubectl get pods -n insightlearn -l app=insightlearn-api
echo ""

# Step 8: Test health endpoints
echo -e "${COLOR_YELLOW}Step 8: Testing health check endpoints...${COLOR_RESET}"
sleep 5  # Give the API a moment to fully start

echo -e "${COLOR_BLUE}Testing /health/live...${COLOR_RESET}"
LIVE_STATUS=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:31081/health/live)
if [ "$LIVE_STATUS" -eq 200 ]; then
    echo -e "${COLOR_GREEN}✓ Liveness probe: 200 OK${COLOR_RESET}"
else
    echo -e "${COLOR_RED}✗ Liveness probe: $LIVE_STATUS${COLOR_RESET}"
fi

echo -e "${COLOR_BLUE}Testing /health/ready...${COLOR_RESET}"
READY_STATUS=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:31081/health/ready)
if [ "$READY_STATUS" -eq 200 ]; then
    echo -e "${COLOR_GREEN}✓ Readiness probe: 200 OK${COLOR_RESET}"
else
    echo -e "${COLOR_YELLOW}⚠ Readiness probe: $READY_STATUS (critical services may be unhealthy)${COLOR_RESET}"
fi

echo -e "${COLOR_BLUE}Testing /health (full check)...${COLOR_RESET}"
HEALTH_STATUS=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:31081/health)
if [ "$HEALTH_STATUS" -eq 200 ]; then
    echo -e "${COLOR_GREEN}✓ Full health check: 200 OK${COLOR_RESET}"
else
    echo -e "${COLOR_YELLOW}⚠ Full health check: $HEALTH_STATUS (some services may be unhealthy)${COLOR_RESET}"
fi
echo ""

# Summary
echo -e "${COLOR_BLUE}========================================${COLOR_RESET}"
echo -e "${COLOR_GREEN}Deployment complete!${COLOR_RESET}"
echo -e "${COLOR_BLUE}========================================${COLOR_RESET}"
echo ""
echo "Health check endpoints available:"
echo "  - http://localhost:31081/health       (full status, JSON)"
echo "  - http://localhost:31081/health/live  (liveness probe)"
echo "  - http://localhost:31081/health/ready (readiness probe)"
echo ""
echo "Run comprehensive tests:"
echo "  ./test-health-checks.sh"
echo ""
echo "View pod logs:"
echo "  kubectl logs -n insightlearn -l app=insightlearn-api --tail=50 -f"
echo ""
echo "Check health status:"
echo "  curl http://localhost:31081/health | jq"
echo ""
