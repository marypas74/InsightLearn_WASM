#!/bin/bash
###############################################################################
# InsightLearn Watchdog Service
# Monitora continuamente tutti i servizi e li riavvia se necessario
###############################################################################

set -e

GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m'

LOG_FILE="/var/log/insightlearn-watchdog.log"
CHECK_INTERVAL=30  # secondi tra i controlli

log() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] $1" | tee -a "$LOG_FILE"
}

log_success() { log "✅ $1"; }
log_error() { log "❌ $1"; }
log_warning() { log "⚠️  $1"; }
log_info() { log "ℹ️  $1"; }

cd "$(dirname "$0")"

log_info "=== InsightLearn Watchdog Started ==="

while true; do
    ###########################################################################
    # 1. Check Minikube
    ###########################################################################
    if ! minikube status | grep -q "Running"; then
        log_error "Minikube not running, restarting..."
        minikube start --driver=podman --container-runtime=cri-o --memory=14336 --cpus=6 \
                        \
                       >> "$LOG_FILE" 2>&1
        log_success "Minikube restarted"
        sleep 60  # Wait for pods to stabilize
    fi

    ###########################################################################
    # 2. Check Critical Pods
    ###########################################################################
    CRITICAL_PODS=("sqlserver" "mongodb" "redis" "api" "ollama" "wasm")
    
    for pod in "${CRITICAL_PODS[@]}"; do
        if ! kubectl get pods -n insightlearn | grep -q "$pod.*Running"; then
            log_warning "Pod $pod not running, checking..."
            
            # Get pod name
            POD_NAME=$(kubectl get pods -n insightlearn | grep "$pod" | awk '{print $1}' | head -1)
            
            if [ ! -z "$POD_NAME" ]; then
                POD_STATUS=$(kubectl get pod -n insightlearn "$POD_NAME" -o jsonpath='{.status.phase}')
                
                if [ "$POD_STATUS" != "Running" ]; then
                    log_error "Pod $POD_NAME is $POD_STATUS, restarting..."
                    kubectl delete pod -n insightlearn "$POD_NAME" >> "$LOG_FILE" 2>&1
                    log_success "Pod $POD_NAME deleted, Kubernetes will recreate it"
                    sleep 30
                fi
            fi
        fi
    done

    ###########################################################################
    # 3. Check Port-Forwards
    ###########################################################################
    
    # Check API port-forward (8081)
    if ! ps aux | grep -v grep | grep -q "port-forward.*api-service.*8081"; then
        log_error "API port-forward not running, restarting..."
        pkill -f "port-forward.*api-service.*8081" 2>/dev/null || true
        kubectl port-forward -n insightlearn --address 127.0.0.1 \
                service/api-service 8081:80 \
                >> /tmp/port-forward-api.log 2>&1 &
        log_success "API port-forward restarted on 8081"
    fi

    # Check WASM port-forward (8080)
    if ! ps aux | grep -v grep | grep -q "port-forward.*insightlearn-wasm.*8080"; then
        log_error "WASM port-forward not running, restarting..."
        pkill -f "port-forward.*insightlearn-wasm.*8080" 2>/dev/null || true
        kubectl port-forward -n insightlearn --address 127.0.0.1 \
                service/insightlearn-wasm-blazor-webassembly 8080:80 \
                >> /tmp/port-forward-wasm.log 2>&1 &
        log_success "WASM port-forward restarted on 8080"
    fi

    ###########################################################################
    # 4. Check Cloudflare Tunnel
    ###########################################################################
    if command -v cloudflared &> /dev/null; then
        if ! ps aux | grep -v grep | grep -q "cloudflared tunnel"; then
            log_error "Cloudflare tunnel not running, restarting..."
            pkill -f "cloudflared tunnel" 2>/dev/null || true
            sleep 2
            nohup cloudflared tunnel run insightlearn-wasm \
                    >> /tmp/cloudflared.log 2>&1 &
            log_success "Cloudflare tunnel restarted"
        fi
    fi

    ###########################################################################
    # 5. Check Service Health
    ###########################################################################
    
    # Check API health
    if ! curl -s --max-time 5 http://localhost:8081/health > /dev/null 2>&1; then
        log_warning "API health check failed"
    fi

    # Check WASM
    if ! curl -s --max-time 5 http://localhost:8080 > /dev/null 2>&1; then
        log_warning "WASM health check failed"
    fi

    ###########################################################################
    # Wait before next check
    ###########################################################################
    sleep $CHECK_INTERVAL

done
