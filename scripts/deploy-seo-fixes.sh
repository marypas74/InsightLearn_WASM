#!/bin/bash
# =============================================================================
# SEO Fixes Deployment Script
# Created: 2025-12-05
# Purpose: Deploy updated WASM image with corrected sitemap.xml and robots.txt
# =============================================================================

set -e  # Exit on any error

echo "========================================="
echo " SEO Fixes Deployment"
echo " Date: $(date)"
echo "========================================="

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${YELLOW}[1/4] Importing WASM Docker image to K3s containerd...${NC}"
docker save localhost/insightlearn/wasm:latest | sudo /usr/local/bin/k3s ctr images import -

echo -e "${GREEN}✓ Image imported successfully${NC}"

echo -e "${YELLOW}[2/4] Verifying image in K3s...${NC}"
sudo /usr/local/bin/k3s ctr images ls | grep insightlearn/wasm

echo -e "${YELLOW}[3/4] Rolling out updated WASM deployment...${NC}"
kubectl rollout restart deployment/insightlearn-wasm-blazor-webassembly -n insightlearn

echo -e "${YELLOW}[4/4] Waiting for deployment to complete...${NC}"
kubectl rollout status deployment/insightlearn-wasm-blazor-webassembly -n insightlearn --timeout=300s

echo ""
echo -e "${GREEN}=========================================${NC}"
echo -e "${GREEN} ✓ SEO Fixes Deployed Successfully!${NC}"
echo -e "${GREEN}=========================================${NC}"

echo ""
echo "Verifying SEO files..."
sleep 10  # Wait for pod to be ready

# Get new pod name
POD_NAME=$(kubectl get pods -n insightlearn -l app=insightlearn-wasm-blazor-webassembly -o jsonpath='{.items[0].metadata.name}')

echo "Checking files in pod: $POD_NAME"
kubectl exec -n insightlearn $POD_NAME -- ls -la /usr/share/nginx/html/ | grep -E "(sitemap|robots)"

echo ""
echo "Testing public accessibility..."
echo ""
echo "sitemap.xml:"
curl -I https://wasm.insightlearn.cloud/sitemap.xml 2>/dev/null | grep -E "(HTTP|Content-Type|Cache-Control)"

echo ""
echo "robots.txt:"
curl -I https://wasm.insightlearn.cloud/robots.txt 2>/dev/null | grep -E "(HTTP|Content-Type|Cache-Control)"

echo ""
echo -e "${GREEN}=========================================${NC}"
echo -e "${GREEN} Deployment Complete!${NC}"
echo -e "${GREEN}=========================================${NC}"
echo ""
echo "Next steps:"
echo "1. Submit sitemap to Google Search Console:"
echo "   https://search.google.com/search-console/sitemaps"
echo "   Sitemap URL: https://wasm.insightlearn.cloud/sitemap.xml"
echo ""
echo "2. Verify sitemap is accessible:"
echo "   https://wasm.insightlearn.cloud/sitemap.xml"
echo ""
echo "3. Verify robots.txt is accessible:"
echo "   https://wasm.insightlearn.cloud/robots.txt"
echo ""
echo "4. Test with Google Search Console URL Inspection Tool:"
echo "   https://search.google.com/search-console/inspect"
echo ""
