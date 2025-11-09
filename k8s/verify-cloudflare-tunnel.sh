#!/bin/bash
################################################################################
# Verify and Restart Cloudflare Tunnel if Needed
#
# Purpose: Check Cloudflare Tunnel status and restart if not running
#   - Called by restore script after cluster recovery
#   - Ensures external access is restored
#
# Author: InsightLearn DevOps Team
# Version: 1.0.0
################################################################################

set -euo pipefail

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

log() {
    echo -e "${GREEN}[$(date +'%Y-%m-%d %H:%M:%S')]${NC} $*"
}

error() {
    echo -e "${RED}[$(date +'%Y-%m-%d %H:%M:%S')] ERROR:${NC} $*" >&2
}

warn() {
    echo -e "${YELLOW}[$(date +'%Y-%m-%d %H:%M:%S')] WARNING:${NC} $*"
}

log "Checking Cloudflare Tunnel status..."

# Check if cloudflared is installed
if ! command -v cloudflared &> /dev/null; then
    error "cloudflared command not found"
    exit 1
fi

# Check if config exists
CLOUDFLARE_CONFIG="/home/mpasqui/.cloudflared/config.yml"
if [[ ! -f "$CLOUDFLARE_CONFIG" ]]; then
    error "Cloudflare config not found: $CLOUDFLARE_CONFIG"
    exit 1
fi

# Check if systemd service is enabled
if systemctl is-enabled cloudflared.service &>/dev/null; then
    log "Cloudflared systemd service is enabled"

    # Check if service is running
    if systemctl is-active cloudflared.service &>/dev/null; then
        log "✓ Cloudflared service is running"
    else
        warn "Cloudflared service is not running, starting it..."
        sudo systemctl start cloudflared.service
        sleep 5

        if systemctl is-active cloudflared.service &>/dev/null; then
            log "✓ Cloudflared service started successfully"
        else
            error "Failed to start cloudflared service"
            sudo journalctl -u cloudflared.service -n 20 --no-pager
            exit 1
        fi
    fi
else
    warn "Cloudflared systemd service not enabled"
    log "Checking for manual cloudflared process..."

    if pgrep -f "cloudflared tunnel" > /dev/null; then
        log "✓ Cloudflared is running (manual process)"
    else
        warn "Cloudflared is not running, starting manually..."

        # Kill any stale processes
        pkill -f cloudflared || true

        # Start cloudflared in background
        nohup cloudflared tunnel --config "$CLOUDFLARE_CONFIG" run > /tmp/cloudflared.log 2>&1 &
        CLOUDFLARED_PID=$!

        log "Cloudflared started with PID: $CLOUDFLARED_PID"
        sleep 10

        if pgrep -f "cloudflared tunnel" > /dev/null; then
            log "✓ Cloudflared started successfully"
        else
            error "Failed to start cloudflared"
            cat /tmp/cloudflared.log
            exit 1
        fi
    fi
fi

# Verify tunnel connectivity
log "Verifying tunnel connectivity..."

# Wait for K3s services to be ready
sleep 5

# Test local NodePort endpoints
API_PORT="31081"
WEB_PORT="31090"

if curl -s http://localhost:${API_PORT}/health > /dev/null 2>&1; then
    log "✓ API NodePort (${API_PORT}) is accessible"
else
    warn "API NodePort (${API_PORT}) not accessible yet (may take a few minutes)"
fi

if curl -s http://localhost:${WEB_PORT}/ > /dev/null 2>&1; then
    log "✓ Web NodePort (${WEB_PORT}) is accessible"
else
    warn "Web NodePort (${WEB_PORT}) not accessible yet (may take a few minutes)"
fi

log "Cloudflare Tunnel check completed"

exit 0
