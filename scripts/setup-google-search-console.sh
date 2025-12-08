#!/bin/bash
#
# Google Search Console Setup Script for InsightLearn
#
# This script helps you verify your domain with Google Search Console
# and configure it for SEO monitoring.
#
# Usage:
#   ./scripts/setup-google-search-console.sh <verification_code>
#
# Example:
#   ./scripts/setup-google-search-console.sh "google1234567890abcdef"
#

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
WWWROOT_DIR="$PROJECT_ROOT/src/InsightLearn.WebAssembly/wwwroot"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}  Google Search Console Setup Script${NC}"
echo -e "${BLUE}  InsightLearn SEO Configuration${NC}"
echo -e "${BLUE}========================================${NC}"
echo ""

# Check if verification code provided
if [ -z "$1" ]; then
    echo -e "${YELLOW}INSTRUCTIONS:${NC}"
    echo ""
    echo "1. Go to: ${GREEN}https://search.google.com/search-console${NC}"
    echo "2. Click 'Add Property'"
    echo "3. Select 'URL prefix' and enter: ${GREEN}https://wasm.insightlearn.cloud${NC}"
    echo "4. Choose 'HTML file' verification method"
    echo "5. Download the HTML file (e.g., google1234567890abcdef.html)"
    echo "6. Run this script with the filename (without .html):"
    echo ""
    echo -e "   ${GREEN}./scripts/setup-google-search-console.sh google1234567890abcdef${NC}"
    echo ""
    exit 0
fi

VERIFICATION_CODE="$1"

# Remove .html extension if provided
VERIFICATION_CODE="${VERIFICATION_CODE%.html}"

echo -e "${GREEN}Creating verification file: ${VERIFICATION_CODE}.html${NC}"

# Create the verification file
VERIFICATION_FILE="$WWWROOT_DIR/${VERIFICATION_CODE}.html"
echo "google-site-verification: ${VERIFICATION_CODE}.html" > "$VERIFICATION_FILE"

echo -e "${GREEN}Verification file created at:${NC}"
echo "  $VERIFICATION_FILE"
echo ""

# Update Dockerfile.wasm to include the file
echo -e "${YELLOW}Updating Dockerfile.wasm...${NC}"
DOCKERFILE="$PROJECT_ROOT/Dockerfile.wasm"

# Check if verification file line already exists
if grep -q "google.*\.html" "$DOCKERFILE"; then
    echo -e "${YELLOW}Google verification file already configured in Dockerfile${NC}"
else
    # Add line after IndexNow file
    sed -i "/ebd57a262cfe8ff8de852eba65288c19.txt/a\\
# Copy Google Search Console verification file\\
COPY --from=build /src/src/InsightLearn.WebAssembly/wwwroot/${VERIFICATION_CODE}.html ./${VERIFICATION_CODE}.html\\
RUN chmod 644 ${VERIFICATION_CODE}.html" "$DOCKERFILE"
    echo -e "${GREEN}Dockerfile.wasm updated${NC}"
fi

# Update nginx config to serve the file
echo -e "${YELLOW}Updating nginx config...${NC}"
NGINX_CONF="$PROJECT_ROOT/docker/wasm-nginx.conf"

if grep -q "google.*\.html" "$NGINX_CONF"; then
    echo -e "${YELLOW}Google verification already configured in nginx${NC}"
else
    # Add location block for Google verification
    sed -i "/IndexNow verification file/a\\
\\
    # Google Search Console verification file\\
    location = /${VERIFICATION_CODE}.html {\\
        try_files \$uri =404;\\
        add_header Content-Type \"text/html; charset=utf-8\";\\
        add_header Cache-Control \"public, max-age=86400\";\\
        add_header X-Content-Type-Options \"nosniff\";\\
    }" "$NGINX_CONF"
    echo -e "${GREEN}Nginx config updated${NC}"
fi

echo ""
echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}  SETUP COMPLETE!${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo -e "${YELLOW}Next steps:${NC}"
echo "1. Build and deploy the WASM image:"
echo -e "   ${GREEN}cd $PROJECT_ROOT${NC}"
echo -e "   ${GREEN}podman build -f Dockerfile.wasm -t localhost/insightlearn/wasm:gsc .${NC}"
echo -e "   ${GREEN}podman save localhost/insightlearn/wasm:gsc -o /tmp/wasm-gsc.tar${NC}"
echo -e "   ${GREEN}echo 'PASSWORD' | sudo -S /usr/local/bin/k3s ctr images import /tmp/wasm-gsc.tar${NC}"
echo -e "   ${GREEN}kubectl set image deployment/insightlearn-wasm-blazor-webassembly wasm-blazor=localhost/insightlearn/wasm:gsc -n insightlearn${NC}"
echo ""
echo "2. Verify the file is accessible:"
echo -e "   ${GREEN}curl https://wasm.insightlearn.cloud/${VERIFICATION_CODE}.html${NC}"
echo ""
echo "3. Go back to Google Search Console and click 'Verify'"
echo ""
echo "4. After verification, submit sitemap:"
echo "   - Go to Sitemaps in Search Console"
echo -e "   - Add: ${GREEN}https://wasm.insightlearn.cloud/sitemap.xml${NC}"
echo ""
