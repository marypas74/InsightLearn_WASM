#!/bin/bash

###############################################################################
# Setup nginx on Rocky Linux host for InsightLearn
# This script configures nginx to reverse proxy to minikube NodePort services
###############################################################################

set -e

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo -e "${GREEN}InsightLearn Nginx Host Setup${NC}"
echo ""

# Check if running as root
if [[ $EUID -ne 0 ]]; then
   echo -e "${RED}This script must be run as root (use sudo)${NC}"
   exit 1
fi

# 1. Install nginx
echo -e "${YELLOW}[1/6] Installing nginx...${NC}"
if rpm -qa | grep -q "^nginx-"; then
    echo "nginx already installed"
else
    dnf install -y nginx
    echo -e "${GREEN}✓ nginx installed${NC}"
fi

# 2. Stop nginx if running
echo -e "${YELLOW}[2/6] Stopping nginx...${NC}"
systemctl stop nginx 2>/dev/null || true

# 3. Backup existing config
echo -e "${YELLOW}[3/6] Backing up existing config...${NC}"
if [ -f /etc/nginx/nginx.conf ]; then
    cp /etc/nginx/nginx.conf /etc/nginx/nginx.conf.backup.$(date +%Y%m%d_%H%M%S)
    echo -e "${GREEN}✓ Backed up to /etc/nginx/nginx.conf.backup.*${NC}"
fi

# 4. Copy new config
echo -e "${YELLOW}[4/6] Installing new config...${NC}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cp "$SCRIPT_DIR/nginx-host.conf" /etc/nginx/nginx.conf
echo -e "${GREEN}✓ Config installed${NC}"

# 5. Test config
echo -e "${YELLOW}[5/6] Testing nginx config...${NC}"
if nginx -t; then
    echo -e "${GREEN}✓ Config valid${NC}"
else
    echo -e "${RED}✗ Config invalid. Restoring backup...${NC}"
    cp /etc/nginx/nginx.conf.backup.* /etc/nginx/nginx.conf
    exit 1
fi

# 6. Configure firewall
echo -e "${YELLOW}[6/6] Configuring firewall...${NC}"
if systemctl is-active --quiet firewalld; then
    firewall-cmd --permanent --add-service=http
    firewall-cmd --permanent --add-service=https
    firewall-cmd --reload
    echo -e "${GREEN}✓ Firewall configured${NC}"
else
    echo "firewalld not running, skipping"
fi

# 7. Enable and start nginx
echo ""
echo -e "${YELLOW}Starting nginx...${NC}"
systemctl enable nginx
systemctl start nginx

if systemctl is-active --quiet nginx; then
    echo -e "${GREEN}✓ nginx is running${NC}"
else
    echo -e "${RED}✗ nginx failed to start${NC}"
    journalctl -u nginx --no-pager -n 20
    exit 1
fi

# 8. Test endpoints
echo ""
echo -e "${YELLOW}Testing endpoints...${NC}"
sleep 2

echo -n "HTTP health check: "
if curl -s http://localhost/health | grep -q "Healthy"; then
    echo -e "${GREEN}✓ OK${NC}"
else
    echo -e "${RED}✗ FAIL${NC}"
fi

echo -n "HTTP root: "
if curl -s http://localhost/ | grep -q "InsightLearn"; then
    echo -e "${GREEN}✓ OK${NC}"
else
    echo -e "${RED}✗ FAIL${NC}"
fi

echo ""
echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Setup complete!${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo "Test from outside:"
echo "  curl http://wasm.insightlearn.cloud/health"
echo "  curl http://wasm.insightlearn.cloud/"
echo ""
echo "View logs:"
echo "  sudo tail -f /var/log/nginx/insightlearn-access.log"
echo "  sudo tail -f /var/log/nginx/insightlearn-error.log"
echo ""
echo "Restart nginx:"
echo "  sudo systemctl restart nginx"
echo ""
