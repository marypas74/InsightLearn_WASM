#!/bin/bash

###############################################################################
# Post-Reboot Restart Script per InsightLearn
# Riavvia tutti i servizi necessari dopo un reboot del sistema
###############################################################################

set -e

GREEN='\033[0;32m'
RED='\033[0;31m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m'

print_success() { echo -e "${GREEN}✅ $1${NC}"; }
print_error() { echo -e "${RED}❌ $1${NC}"; }
print_info() { echo -e "${BLUE}ℹ️  $1${NC}"; }
print_warning() { echo -e "${YELLOW}⚠️  $1${NC}"; }

cd "$(dirname "$0")"

echo "========================================="
echo "  InsightLearn Post-Reboot Startup"
echo "========================================="
echo ""

###############################################################################
# 1. Verifica Minikube
###############################################################################

print_info "Step 1/4: Checking minikube status..."
if minikube status | grep -q "Running"; then
    print_success "Minikube is already running"
else
    print_warning "Minikube not running, starting it..."
    minikube start --driver=podman --container-runtime=cri-o \
      --memory=14336 --cpus=6 \
      
    print_success "Minikube started"
fi

###############################################################################
# 2. Aspetta che i pod siano pronti
###############################################################################

print_info "Step 2/4: Waiting for pods to be ready..."
minikube kubectl -- wait --for=condition=ready pod -n insightlearn -l app=insightlearn-api --timeout=120s
minikube kubectl -- wait --for=condition=ready pod -n insightlearn -l app=ollama --timeout=120s
print_success "All pods are ready"

###############################################################################
# 3. Riavvia Port-Forward
###############################################################################

print_info "Step 3/4: Starting port-forwards..."

# Kill any existing port-forwards
pkill -f "port-forward.*api-service.*8081" 2>/dev/null || true
pkill -f "port-forward.*insightlearn-wasm.*8080" 2>/dev/null || true

# Start API port-forward (porta 8081)
minikube kubectl -- port-forward -n insightlearn --address 127.0.0.1 service/api-service 8081:80 \
  > /tmp/port-forward-api.log 2>&1 &
API_PF_PID=$!
print_success "API port-forward started on 8081 (PID: $API_PF_PID)"

# Start WASM port-forward (porta 8080)
minikube kubectl -- port-forward -n insightlearn --address 127.0.0.1 service/insightlearn-wasm-blazor-webassembly 8080:80 \
  > /tmp/port-forward-wasm.log 2>&1 &
WASM_PF_PID=$!
print_success "WASM port-forward started on 8080 (PID: $WASM_PF_PID)"

# Wait for port-forwards to be ready
sleep 5

###############################################################################
# 4. Riavvia Cloudflare Tunnel
###############################################################################

print_info "Step 4/4: Starting Cloudflare tunnel..."

# Kill any existing tunnels
pkill -f "cloudflared tunnel" 2>/dev/null || true
sleep 2

# Start Cloudflare tunnel (assumo che il binario sia in /tmp)
if [ -f "/tmp/cloudflared" ]; then
    nohup /tmp/cloudflared tunnel run insightlearn-wasm > /tmp/cloudflared.log 2>&1 &
    CF_PID=$!
    print_success "Cloudflare tunnel started (PID: $CF_PID)"
elif command -v cloudflared &> /dev/null; then
    nohup cloudflared tunnel run insightlearn-wasm > /tmp/cloudflared.log 2>&1 &
    CF_PID=$!
    print_success "Cloudflare tunnel started (PID: $CF_PID)"
else
    print_warning "cloudflared not found, skipping..."
fi

###############################################################################
# 5. Verifica che tutto funzioni
###############################################################################

echo ""
print_info "Verifying services..."
sleep 3

# Test API health
if curl -s http://localhost:8081/health > /dev/null; then
    print_success "API is responding on http://localhost:8081"
else
    print_error "API not responding on port 8081"
fi

# Test chatbot health
if curl -s http://localhost:8081/api/chat/health > /dev/null; then
    print_success "Chatbot API is responding"
else
    print_warning "Chatbot API not responding (might need warm-up)"
fi

# Test WASM
if curl -s http://localhost:8080 > /dev/null; then
    print_success "WASM is responding on http://localhost:8080"
else
    print_error "WASM not responding on port 8080"
fi

###############################################################################
# Summary
###############################################################################

echo ""
echo "========================================="
echo "  Startup Complete!"
echo "========================================="
echo ""
echo "Services Status:"
echo "  🌐 WASM:              http://localhost:8080"
echo "  🔌 API:               http://localhost:8081"
echo "  🤖 Chatbot:           http://localhost:8081/api/chat/health"
echo "  ☁️  Cloudflare:        https://wasm.insightlearn.cloud"
echo ""
echo "Port-forward PIDs:"
echo "  API (8081):  $API_PF_PID"
echo "  WASM (8080): $WASM_PF_PID"
[ ! -z "$CF_PID" ] && echo "  Cloudflare:  $CF_PID"
echo ""
print_info "Logs disponibili in:"
echo "  - /tmp/port-forward-api.log"
echo "  - /tmp/port-forward-wasm.log"
echo "  - /tmp/cloudflared.log"
echo ""
