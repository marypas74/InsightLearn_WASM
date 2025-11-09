#!/bin/bash
################################################################################
# Install Disaster Recovery Grafana Monitoring
#
# Purpose: Install and configure Grafana monitoring for disaster recovery system
#   - Deploy DR metrics server
#   - Configure Prometheus scraping
#   - Import Grafana dashboard
#
# Usage: sudo ./install-dr-grafana-monitoring.sh
#
# Author: InsightLearn DevOps Team
# Version: 1.0.0
################################################################################

set -euo pipefail

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

log() {
    echo -e "${GREEN}[INFO]${NC} $*"
}

error() {
    echo -e "${RED}[ERROR]${NC} $*" >&2
}

warn() {
    echo -e "${YELLOW}[WARN]${NC} $*"
}

info() {
    echo -e "${BLUE}[INFO]${NC} $*"
}

# Check if running as root
if [[ $EUID -ne 0 ]]; then
   error "This script must be run as root (use sudo)"
   exit 1
fi

echo ""
log "=========================================="
log "DR Grafana Monitoring Installation"
log "=========================================="
echo ""

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

# Step 1: Install/verify Python3
log "Step 1/6: Verifying Python3 installation..."
if ! command -v python3 &> /dev/null; then
    warn "Python3 not found, installing..."
    yum install -y python3 || error "Failed to install Python3"
fi
PYTHON_VERSION=$(python3 --version)
log "  ✓ $PYTHON_VERSION"

# Step 2: Install systemd service for DR metrics server
log "Step 2/6: Installing DR metrics server systemd service..."
cp "$SCRIPT_DIR/dr-metrics-server.service" /etc/systemd/system/
systemctl daemon-reload
systemctl enable dr-metrics-server.service
log "  ✓ Service enabled"

# Start the service
log "  Starting DR metrics server..."
systemctl start dr-metrics-server.service
sleep 3

if systemctl is-active --quiet dr-metrics-server.service; then
    log "  ✓ Service is running"
else
    warn "  ! Service failed to start, check logs with: journalctl -u dr-metrics-server.service -n 50"
fi

# Step 3: Verify metrics endpoint
log "Step 3/6: Verifying metrics endpoint..."
sleep 2

if curl -s http://localhost:9101/health > /dev/null 2>&1; then
    log "  ✓ Health endpoint OK (http://localhost:9101/health)"
else
    warn "  ! Health endpoint not responding"
fi

if curl -s http://localhost:9101/metrics | head -5 > /dev/null 2>&1; then
    log "  ✓ Metrics endpoint OK (http://localhost:9101/metrics)"
    info "  Sample metrics:"
    curl -s http://localhost:9101/metrics | grep "insightlearn_dr" | head -3 | sed 's/^/    /'
else
    warn "  ! Metrics endpoint not responding"
fi

# Step 4: Deploy Kubernetes resources (optional, if using K8s)
log "Step 4/6: Deploying Kubernetes resources (if K8s available)..."

if command -v kubectl &> /dev/null; then
    if kubectl cluster-info &> /dev/null; then
        log "  Applying DR metrics deployment..."
        kubectl apply -f "$SCRIPT_DIR/20-dr-metrics-prometheus-config.yaml" 2>&1 | sed 's/^/    /'

        log "  Waiting for deployment..."
        kubectl rollout status deployment/dr-metrics-server -n insightlearn --timeout=60s 2>&1 | sed 's/^/    /' || warn "Deployment timeout (may still be starting)"

        log "  ✓ Kubernetes resources deployed"
    else
        info "  ℹ K8s cluster not accessible, skipping K8s deployment"
    fi
else
    info "  ℹ kubectl not found, skipping K8s deployment"
fi

# Step 5: Import Grafana dashboard
log "Step 5/6: Importing Grafana dashboard..."

GRAFANA_URL="${GRAFANA_URL:-http://localhost:3000}"
GRAFANA_USER="${GRAFANA_USER:-admin}"
GRAFANA_PASSWORD="${GRAFANA_PASSWORD:-admin}"
DASHBOARD_FILE="$SCRIPT_DIR/../grafana/grafana-dashboard-disaster-recovery.json"

if [[ -f "$DASHBOARD_FILE" ]]; then
    log "  Dashboard file found: $DASHBOARD_FILE"

    # Try to import dashboard via API
    if curl -s "$GRAFANA_URL/api/health" > /dev/null 2>&1; then
        log "  Grafana is accessible at $GRAFANA_URL"

        # Create dashboard JSON payload
        DASHBOARD_JSON=$(cat "$DASHBOARD_FILE")
        PAYLOAD=$(jq -n --argjson dashboard "$DASHBOARD_JSON" '{dashboard: $dashboard, overwrite: true}')

        RESPONSE=$(curl -s -X POST \
            -H "Content-Type: application/json" \
            -u "$GRAFANA_USER:$GRAFANA_PASSWORD" \
            -d "$PAYLOAD" \
            "$GRAFANA_URL/api/dashboards/db" 2>&1)

        if echo "$RESPONSE" | grep -q '"status":"success"'; then
            log "  ✓ Dashboard imported successfully"
            DASHBOARD_URL=$(echo "$RESPONSE" | jq -r '.url // empty')
            [[ -n "$DASHBOARD_URL" ]] && info "  Dashboard URL: $GRAFANA_URL$DASHBOARD_URL"
        else
            warn "  ! Dashboard import failed"
            warn "  ! Manual import: Grafana UI → Dashboards → Import → Upload JSON"
            warn "  ! File: $DASHBOARD_FILE"
        fi
    else
        warn "  ! Grafana not accessible at $GRAFANA_URL"
        info "  ! Manual import required:"
        info "    1. Login to Grafana: $GRAFANA_URL"
        info "    2. Go to Dashboards → Import"
        info "    3. Upload file: $DASHBOARD_FILE"
    fi
else
    error "Dashboard file not found: $DASHBOARD_FILE"
fi

# Step 6: Update install-disaster-recovery.sh to include metrics export
log "Step 6/6: Updating disaster recovery installation script..."

# Check if export-dr-metrics.sh is called in backup/restore scripts
if grep -q "export-dr-metrics.sh" "$SCRIPT_DIR/backup-cluster-state.sh"; then
    log "  ✓ Backup script already configured for metrics export"
else
    warn "  ! Backup script needs manual update to call export-dr-metrics.sh"
fi

# Final summary
echo ""
log "=========================================="
log "Installation Complete! ✓"
log "=========================================="
echo ""

info "Summary:"
info "  • DR Metrics Server: http://localhost:9101/metrics"
info "  • Systemd Service: dr-metrics-server.service (enabled, running)"
info "  • Grafana Dashboard: $DASHBOARD_FILE"
echo ""

info "Next steps:"
info "  1. Check metrics: curl http://localhost:9101/metrics"
info "  2. View dashboard: $GRAFANA_URL (admin/admin)"
info "  3. Configure Prometheus to scrape http://localhost:9101/metrics"
echo ""

info "Prometheus scrape config (add to prometheus.yml):"
cat <<'EOF'
  - job_name: 'insightlearn-disaster-recovery'
    static_configs:
      - targets: ['localhost:9101']
        labels:
          service: 'disaster-recovery'
    scrape_interval: 60s
    scrape_timeout: 30s
EOF
echo ""

info "Service management:"
info "  • Status: sudo systemctl status dr-metrics-server.service"
info "  • Logs: sudo journalctl -u dr-metrics-server.service -f"
info "  • Restart: sudo systemctl restart dr-metrics-server.service"
echo ""

log "=========================================="
echo ""

exit 0
