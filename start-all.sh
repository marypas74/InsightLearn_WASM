#!/bin/bash
# InsightLearn WASM - Start All Services
# Starts port-forwards and Cloudflare tunnel

set -e

echo "=================================================="
echo "  InsightLearn WASM - Starting All Services"
echo "=================================================="
echo ""

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

# Check if minikube is running
if ! minikube status &>/dev/null; then
    echo -e "${RED}ERROR: Minikube is not running${NC}"
    echo "Run: ./deploy.sh first"
    exit 1
fi

# Kill existing port-forwards
echo -e "${YELLOW}Stopping existing port-forwards...${NC}"
pkill -f "kubectl port-forward" 2>/dev/null || true
sleep 2

# Start port-forward for WASM (8080 -> 80)
echo -e "${YELLOW}Starting WASM port-forward...${NC}"
kubectl port-forward -n insightlearn svc/wasm-blazor-webassembly-service 8080:80 >/dev/null 2>&1 &
WASM_PID=$!
echo -e "${GREEN}✓ WASM port-forward started (PID: $WASM_PID)${NC}"

# Start port-forward for API (8081 -> 80)
echo -e "${YELLOW}Starting API port-forward...${NC}"
kubectl port-forward -n insightlearn svc/api-service 8081:80 >/dev/null 2>&1 &
API_PID=$!
echo -e "${GREEN}✓ API port-forward started (PID: $API_PID)${NC}"

# Wait for port-forwards to be ready
sleep 3

# Test connections
echo ""
echo -e "${YELLOW}Testing connections...${NC}"
if curl -s http://localhost:8080 >/dev/null 2>&1; then
    echo -e "${GREEN}✓ WASM accessible at http://localhost:8080${NC}"
else
    echo -e "${RED}✗ WASM not accessible${NC}"
fi

if curl -s http://localhost:8081/health >/dev/null 2>&1; then
    echo -e "${GREEN}✓ API accessible at http://localhost:8081${NC}"
else
    echo -e "${RED}✗ API not accessible${NC}"
fi

# Check if cloudflared is installed
if command -v cloudflared &>/dev/null; then
    echo ""
    echo -e "${YELLOW}Cloudflare Tunnel configuration:${NC}"
    echo "To start tunnel, run in another terminal:"
    echo -e "${BLUE}  cloudflared tunnel run insightlearn${NC}"
    echo ""
    echo "Or use this command to run in background:"
    echo -e "${BLUE}  nohup cloudflared tunnel run insightlearn > cloudflared.log 2>&1 &${NC}"
else
    echo ""
    echo -e "${YELLOW}Cloudflare Tunnel not found${NC}"
    echo "To install: https://developers.cloudflare.com/cloudflare-one/connections/connect-apps/install-and-setup/installation"
fi

echo ""
echo -e "${GREEN}=================================================="
echo -e "  All services started!"
echo -e "==================================================${NC}"
echo ""
echo "Access URLs:"
echo -e "  ${BLUE}• WASM:${NC} http://localhost:8080"
echo -e "  ${BLUE}• API:${NC}  http://localhost:8081"
echo -e "  ${BLUE}• API Health:${NC} http://localhost:8081/health"
echo -e "  ${BLUE}• Swagger:${NC} http://localhost:8081/swagger"
echo ""
echo -e "Port-forward PIDs: WASM=$WASM_PID, API=$API_PID"
echo ""
echo "To stop all services:"
echo -e "  ${BLUE}pkill -f 'kubectl port-forward'${NC}"
echo ""
echo "Keeping port-forwards alive... (press Ctrl+C to stop)"
echo ""

# Keep script running
wait
