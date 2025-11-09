#!/bin/bash
# Fix Cloudflare 502 Bad Gateway - Permanent Solution
# This script configures Cloudflare Tunnel to use NodePort services directly

set -e

echo "========================================"
echo "InsightLearn - Cloudflare 502 Fix"
echo "========================================"
echo ""

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration
CLOUDFLARE_CONFIG="/home/mpasqui/.cloudflared/config.yml"
API_PORT="31081"
WEB_PORT="31090"
LOCALHOST="localhost"

echo -e "${YELLOW}Step 1: Verifying services...${NC}"
kubectl get svc -n insightlearn | grep -E "api-service-nodeport|insightlearn-wasm-blazor-webassembly-nodeport"

echo ""
echo -e "${YELLOW}Step 2: Testing NodePort accessibility...${NC}"
curl -s http://${LOCALHOST}:${API_PORT}/health && echo -e "${GREEN}API NodePort OK${NC}" || echo -e "${RED}API NodePort FAILED${NC}"
curl -s http://${LOCALHOST}:${WEB_PORT}/ | head -5 && echo -e "${GREEN}Web NodePort OK${NC}" || echo -e "${RED}Web NodePort FAILED${NC}"

echo ""
echo -e "${YELLOW}Step 3: Backing up current Cloudflare config...${NC}"
cp ${CLOUDFLARE_CONFIG} ${CLOUDFLARE_CONFIG}.backup-$(date +%Y%m%d-%H%M%S)
echo -e "${GREEN}Backup created${NC}"

echo ""
echo -e "${YELLOW}Step 4: Updating Cloudflare Tunnel configuration...${NC}"
cat > ${CLOUDFLARE_CONFIG} <<EOF
tunnel: 4d4a2ce0-9133-4761-9886-90be465abc79
credentials-file: /home/mpasqui/.cloudflared/4d4a2ce0-9133-4761-9886-90be465abc79.json

ingress:
  # API routes - NodePort 31081
  - hostname: wasm.insightlearn.cloud
    path: ^/api(/.*)?$
    service: http://localhost:${API_PORT}
  - hostname: wasm.insightlearn.cloud
    path: ^/health$
    service: http://localhost:${API_PORT}
  # Web frontend - NodePort 31090
  - hostname: wasm.insightlearn.cloud
    service: http://localhost:${WEB_PORT}
  # 404 for all other requests
  - service: http_status:404
EOF

echo -e "${GREEN}Configuration updated${NC}"
cat ${CLOUDFLARE_CONFIG}

echo ""
echo -e "${YELLOW}Step 5: Stopping existing cloudflared processes...${NC}"
pkill -f cloudflared || echo "No existing cloudflared processes"

echo ""
echo -e "${YELLOW}Step 6: Testing Cloudflare Tunnel configuration...${NC}"
cloudflared tunnel validate /home/mpasqui/.cloudflared/config.yml && echo -e "${GREEN}Configuration valid${NC}" || echo -e "${RED}Configuration invalid${NC}"

echo ""
echo -e "${YELLOW}Step 7: Starting Cloudflare Tunnel...${NC}"
nohup cloudflared tunnel --config ${CLOUDFLARE_CONFIG} run > /tmp/cloudflared.log 2>&1 &
CLOUDFLARED_PID=$!
echo -e "${GREEN}Cloudflared started with PID: ${CLOUDFLARED_PID}${NC}"

echo ""
echo -e "${YELLOW}Step 8: Waiting for tunnel to establish (10 seconds)...${NC}"
sleep 10

echo ""
echo -e "${YELLOW}Step 9: Checking tunnel status...${NC}"
if pgrep -f cloudflared > /dev/null; then
    echo -e "${GREEN}Cloudflared is running${NC}"
    tail -20 /tmp/cloudflared.log
else
    echo -e "${RED}Cloudflared failed to start${NC}"
    cat /tmp/cloudflared.log
    exit 1
fi

echo ""
echo -e "${YELLOW}Step 10: Testing external access (this may take 30-60 seconds)...${NC}"
sleep 30
curl -s -I https://wasm.insightlearn.cloud/ | head -5 && echo -e "${GREEN}External access OK${NC}" || echo -e "${RED}External access failed - check DNS propagation${NC}"

echo ""
echo "========================================"
echo -e "${GREEN}Cloudflare 502 Fix Complete!${NC}"
echo "========================================"
echo ""
echo "Next steps:"
echo "1. Test site: https://wasm.insightlearn.cloud/"
echo "2. Check logs: tail -f /tmp/cloudflared.log"
echo "3. Check tunnel status: pgrep -a cloudflared"
echo ""
echo "To make cloudflared persistent across reboots:"
echo "  sudo cloudflared service install"
echo "  sudo systemctl enable cloudflared"
echo "  sudo systemctl start cloudflared"
echo ""
