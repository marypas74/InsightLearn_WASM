#!/bin/bash

# Phase 4.1: Comprehensive Health Checks Testing Script
# Tests all three health check endpoints:
# 1. /health - Full health check with detailed JSON
# 2. /health/live - Liveness probe (minimal check)
# 3. /health/ready - Readiness probe (critical dependencies)

set -e

# Configuration
API_URL="${API_URL:-http://localhost:31081}"
COLOR_GREEN='\033[0;32m'
COLOR_RED='\033[0;31m'
COLOR_YELLOW='\033[1;33m'
COLOR_BLUE='\033[0;34m'
COLOR_RESET='\033[0m'

echo -e "${COLOR_BLUE}========================================${COLOR_RESET}"
echo -e "${COLOR_BLUE}Phase 4.1: Health Checks Testing${COLOR_RESET}"
echo -e "${COLOR_BLUE}API URL: ${API_URL}${COLOR_RESET}"
echo -e "${COLOR_BLUE}========================================${COLOR_RESET}"
echo ""

# Test 1: Full Health Check (/health)
echo -e "${COLOR_YELLOW}Test 1: Full Health Check (/health)${COLOR_RESET}"
echo -e "${COLOR_BLUE}Expected: JSON response with detailed status for all services${COLOR_RESET}"
echo ""

RESPONSE=$(curl -s -w "\nHTTP_STATUS:%{http_code}" "${API_URL}/health")
HTTP_STATUS=$(echo "$RESPONSE" | grep "HTTP_STATUS" | cut -d':' -f2)
BODY=$(echo "$RESPONSE" | sed '/HTTP_STATUS/d')

echo "HTTP Status: $HTTP_STATUS"
echo "Response Body:"
echo "$BODY" | jq '.' 2>/dev/null || echo "$BODY"
echo ""

if [ "$HTTP_STATUS" -eq 200 ] || [ "$HTTP_STATUS" -eq 503 ]; then
    echo -e "${COLOR_GREEN}✓ PASS: /health endpoint returned $HTTP_STATUS (200=Healthy, 503=Degraded/Unhealthy)${COLOR_RESET}"

    # Parse and display individual service statuses
    echo -e "${COLOR_YELLOW}Service Statuses:${COLOR_RESET}"
    echo "$BODY" | jq -r '.checks[] | "  - \(.name): \(.status) (\(.duration)ms)"' 2>/dev/null || echo "  (Could not parse JSON)"
else
    echo -e "${COLOR_RED}✗ FAIL: /health endpoint returned unexpected status $HTTP_STATUS${COLOR_RESET}"
fi
echo ""
echo -e "${COLOR_BLUE}----------------------------------------${COLOR_RESET}"
echo ""

# Test 2: Liveness Probe (/health/live)
echo -e "${COLOR_YELLOW}Test 2: Liveness Probe (/health/live)${COLOR_RESET}"
echo -e "${COLOR_BLUE}Expected: 200 OK (API is running, no dependency checks)${COLOR_RESET}"
echo ""

RESPONSE=$(curl -s -w "\nHTTP_STATUS:%{http_code}" "${API_URL}/health/live")
HTTP_STATUS=$(echo "$RESPONSE" | grep "HTTP_STATUS" | cut -d':' -f2)
BODY=$(echo "$RESPONSE" | sed '/HTTP_STATUS/d')

echo "HTTP Status: $HTTP_STATUS"
echo "Response Body: $BODY"
echo ""

if [ "$HTTP_STATUS" -eq 200 ]; then
    echo -e "${COLOR_GREEN}✓ PASS: Liveness probe returned 200 OK${COLOR_RESET}"
    echo -e "${COLOR_GREEN}  This endpoint is suitable for K8s livenessProbe${COLOR_RESET}"
else
    echo -e "${COLOR_RED}✗ FAIL: Liveness probe returned $HTTP_STATUS (expected 200)${COLOR_RESET}"
fi
echo ""
echo -e "${COLOR_BLUE}----------------------------------------${COLOR_RESET}"
echo ""

# Test 3: Readiness Probe (/health/ready)
echo -e "${COLOR_YELLOW}Test 3: Readiness Probe (/health/ready)${COLOR_RESET}"
echo -e "${COLOR_BLUE}Expected: 200 if critical services (SQL Server, MongoDB) are healthy${COLOR_RESET}"
echo ""

RESPONSE=$(curl -s -w "\nHTTP_STATUS:%{http_code}" "${API_URL}/health/ready")
HTTP_STATUS=$(echo "$RESPONSE" | grep "HTTP_STATUS" | cut -d':' -f2)
BODY=$(echo "$RESPONSE" | sed '/HTTP_STATUS/d')

echo "HTTP Status: $HTTP_STATUS"
echo "Response Body: $BODY"
echo ""

if [ "$HTTP_STATUS" -eq 200 ]; then
    echo -e "${COLOR_GREEN}✓ PASS: Readiness probe returned 200 OK${COLOR_RESET}"
    echo -e "${COLOR_GREEN}  Critical services (SQL Server, MongoDB) are healthy${COLOR_RESET}"
    echo -e "${COLOR_GREEN}  This endpoint is suitable for K8s readinessProbe${COLOR_RESET}"
elif [ "$HTTP_STATUS" -eq 503 ]; then
    echo -e "${COLOR_YELLOW}⚠ WARNING: Readiness probe returned 503 Service Unavailable${COLOR_RESET}"
    echo -e "${COLOR_YELLOW}  One or more critical services are unhealthy${COLOR_RESET}"
    echo -e "${COLOR_YELLOW}  K8s will NOT route traffic to this pod${COLOR_RESET}"
else
    echo -e "${COLOR_RED}✗ FAIL: Readiness probe returned unexpected status $HTTP_STATUS${COLOR_RESET}"
fi
echo ""
echo -e "${COLOR_BLUE}========================================${COLOR_RESET}"
echo ""

# Summary
echo -e "${COLOR_YELLOW}Summary:${COLOR_RESET}"
echo "1. /health       - Full health check with detailed JSON (for monitoring dashboards)"
echo "2. /health/live  - Liveness probe (K8s restarts pod if fails)"
echo "3. /health/ready - Readiness probe (K8s routes traffic only if healthy)"
echo ""
echo -e "${COLOR_YELLOW}Kubernetes Deployment Configuration:${COLOR_RESET}"
echo "Update k8s/06-api-deployment.yaml:"
echo "  livenessProbe:"
echo "    httpGet:"
echo "      path: /health/live"
echo "      port: 80"
echo "  readinessProbe:"
echo "    httpGet:"
echo "      path: /health/ready"
echo "      port: 80"
echo ""
echo -e "${COLOR_GREEN}Testing complete!${COLOR_RESET}"
