#!/bin/bash
# Script to configure HTTPS access for InsightLearn from intranet

set -e

echo "=== InsightLearn HTTPS Access Setup ==="
echo ""

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Get host IP
HOST_IP=$(ip addr show ens18 | grep "inet " | awk '{print $2}' | cut -d/ -f1)
MINIKUBE_IP=$(minikube ip 2>/dev/null || echo "192.168.49.2")

echo "Host IP: $HOST_IP"
echo "Minikube IP: $MINIKUBE_IP"
echo ""

# Check if nginx is installed
if ! command -v nginx &> /dev/null; then
    echo "${YELLOW}Installing nginx...${NC}"
    sudo apt-get update && sudo apt-get install -y nginx
fi

# Check if certificates exist
if [ ! -f /etc/nginx/ssl/insightlearn/tls.crt ]; then
    echo "${YELLOW}Generating SSL certificates...${NC}"
    
    mkdir -p /tmp/insightlearn-certs
    cd /tmp/insightlearn-certs
    
    openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
        -keyout tls.key -out tls.crt \
        -subj "/C=IT/ST=Italy/L=Rome/O=InsightLearn/CN=$HOST_IP" \
        -addext "subjectAltName=IP:$HOST_IP,DNS:insightlearn.local"
    
    sudo mkdir -p /etc/nginx/ssl/insightlearn
    sudo cp tls.* /etc/nginx/ssl/insightlearn/
    sudo chmod 600 /etc/nginx/ssl/insightlearn/tls.key
    
    echo "${GREEN}✓ Certificates generated${NC}"
fi

# Check if nginx config exists
if [ ! -f /etc/nginx/sites-available/insightlearn ]; then
    echo "${YELLOW}Creating nginx configuration...${NC}"
    
    cat > /tmp/insightlearn-nginx.conf << 'NGINX_EOF'
upstream insightlearn_web {
    server MINIKUBE_IP_PLACEHOLDER:31080;
}

upstream insightlearn_api {
    server MINIKUBE_IP_PLACEHOLDER:31081;
}

server {
    listen 80;
    listen [::]:80;
    server_name HOST_IP_PLACEHOLDER insightlearn.local;
    return 301 https://$host$request_uri;
}

server {
    listen 443 ssl;
    listen [::]:443 ssl;
    http2 on;
    server_name HOST_IP_PLACEHOLDER insightlearn.local;

    ssl_certificate /etc/nginx/ssl/insightlearn/tls.crt;
    ssl_certificate_key /etc/nginx/ssl/insightlearn/tls.key;
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers HIGH:!aNULL:!MD5;

    client_max_body_size 100M;
    proxy_connect_timeout 300s;
    proxy_send_timeout 300s;
    proxy_read_timeout 300s;

    location /api {
        proxy_pass http://insightlearn_api;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_buffering off;
    }

    location / {
        proxy_pass http://insightlearn_web;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection $connection_upgrade;
        proxy_buffering off;
    }
}

map $http_upgrade $connection_upgrade {
    default upgrade;
    '' close;
}
NGINX_EOF

    # Replace placeholders
    sed -i "s/MINIKUBE_IP_PLACEHOLDER/$MINIKUBE_IP/g" /tmp/insightlearn-nginx.conf
    sed -i "s/HOST_IP_PLACEHOLDER/$HOST_IP/g" /tmp/insightlearn-nginx.conf
    
    sudo cp /tmp/insightlearn-nginx.conf /etc/nginx/sites-available/insightlearn
    sudo ln -sf /etc/nginx/sites-available/insightlearn /etc/nginx/sites-enabled/insightlearn
    
    echo "${GREEN}✓ Nginx configuration created${NC}"
fi

# Test nginx configuration
echo ""
echo "Testing nginx configuration..."
sudo nginx -t

# Restart nginx
echo ""
echo "Restarting nginx..."
sudo systemctl restart nginx
sudo systemctl enable nginx

echo ""
echo "${GREEN}=== Setup Complete! ===${NC}"
echo ""
echo "Access InsightLearn from the intranet:"
echo "  ${GREEN}https://$HOST_IP${NC}"
echo ""
echo "Note: You'll see a security warning because the certificate is self-signed."
echo "      This is normal. Click 'Advanced' and 'Proceed to site'."
echo ""
echo "To check status:"
echo "  - Nginx: sudo systemctl status nginx"
echo "  - Kubernetes: kubectl get pods -n insightlearn"
echo "  - Test: curl -k https://$HOST_IP"
