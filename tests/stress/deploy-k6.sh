#!/bin/bash
#
# Deploy k6 Stress Testing Infrastructure for InsightLearn
#
# This script sets up k6 and deploys the Grafana dashboard
# for monitoring stress test results
#

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}╔═══════════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║     InsightLearn k6 Stress Testing Deployment                ║${NC}"
echo -e "${BLUE}╚═══════════════════════════════════════════════════════════════╝${NC}\n"

# Check if k6 is installed
echo -e "${BLUE}🔍 Checking k6 installation...${NC}"
if command -v k6 &> /dev/null; then
    echo -e "${GREEN}✅ k6 is already installed: $(k6 version)${NC}"
else
    echo -e "${YELLOW}⚠️  k6 is not installed${NC}"
    echo -e "${BLUE}Installing k6...${NC}"

    # Detect OS
    if [[ "$OSTYPE" == "linux-gnu"* ]]; then
        # Linux
        echo -e "${BLUE}Detected Linux system${NC}"

        # Check if running as root or with sudo
        if [[ $EUID -eq 0 ]]; then
            SUDO=""
        else
            SUDO="sudo"
        fi

        # Install k6 on Debian/Ubuntu
        if command -v apt-get &> /dev/null; then
            echo -e "${BLUE}Installing k6 via apt...${NC}"
            $SUDO gpg -k || true
            $SUDO gpg --no-default-keyring \
                --keyring /usr/share/keyrings/k6-archive-keyring.gpg \
                --keyserver hkp://keyserver.ubuntu.com:80 \
                --recv-keys C5AD17C747E3415A3642D57D77C6C491D6AC1D69 || true

            echo "deb [signed-by=/usr/share/keyrings/k6-archive-keyring.gpg] https://dl.k6.io/deb stable main" | \
                $SUDO tee /etc/apt/sources.list.d/k6.list

            $SUDO apt-get update
            $SUDO apt-get install -y k6

            echo -e "${GREEN}✅ k6 installed successfully${NC}"
        else
            echo -e "${RED}❌ Unsupported Linux distribution${NC}"
            echo -e "${YELLOW}Please install k6 manually: https://k6.io/docs/get-started/installation/${NC}"
            exit 1
        fi

    elif [[ "$OSTYPE" == "darwin"* ]]; then
        # macOS
        echo -e "${BLUE}Detected macOS system${NC}"

        if command -v brew &> /dev/null; then
            echo -e "${BLUE}Installing k6 via Homebrew...${NC}"
            brew install k6
            echo -e "${GREEN}✅ k6 installed successfully${NC}"
        else
            echo -e "${RED}❌ Homebrew not found${NC}"
            echo -e "${YELLOW}Install Homebrew first: https://brew.sh/${NC}"
            exit 1
        fi
    else
        echo -e "${RED}❌ Unsupported operating system: $OSTYPE${NC}"
        echo -e "${YELLOW}Please install k6 manually: https://k6.io/docs/get-started/installation/${NC}"
        exit 1
    fi
fi

# Build k6 Docker image
echo -e "\n${BLUE}🐳 Building k6 Docker image...${NC}"
cd "$(dirname "$0")"

if docker build -t insightlearn/k6-tests:latest .; then
    echo -e "${GREEN}✅ k6 Docker image built successfully${NC}"
else
    echo -e "${RED}❌ Failed to build k6 Docker image${NC}"
    exit 1
fi

# Deploy Grafana dashboard
echo -e "\n${BLUE}📊 Deploying Grafana dashboard...${NC}"

if kubectl get namespace monitoring &> /dev/null; then
    echo -e "${GREEN}✅ Monitoring namespace exists${NC}"

    # Apply dashboard ConfigMap
    if kubectl apply -f ../../k8s/16-k6-grafana-dashboard.yaml; then
        echo -e "${GREEN}✅ k6 Grafana dashboard deployed${NC}"
    else
        echo -e "${YELLOW}⚠️  Failed to deploy Grafana dashboard (may not be critical)${NC}"
    fi
else
    echo -e "${YELLOW}⚠️  Monitoring namespace does not exist${NC}"
    echo -e "${YELLOW}   Dashboard will be skipped. Deploy monitoring stack first if needed.${NC}"
fi

# Create results directory
echo -e "\n${BLUE}📁 Creating results directory...${NC}"
mkdir -p ./results
echo -e "${GREEN}✅ Results directory created: ./results${NC}"

# Summary
echo -e "\n${BLUE}╔═══════════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║                    Deployment Complete!                       ║${NC}"
echo -e "${BLUE}╚═══════════════════════════════════════════════════════════════╝${NC}\n"

echo -e "${GREEN}k6 Stress Testing Infrastructure Ready!${NC}\n"

echo -e "${YELLOW}Next Steps:${NC}"
echo -e "  1. Run smoke test:      ${BLUE}k6 run smoke-test.js${NC}"
echo -e "  2. Run load test:       ${BLUE}k6 run load-test.js${NC}"
echo -e "  3. Run stress test:     ${BLUE}k6 run stress-test.js${NC}"
echo -e "  4. Run all tests:       ${BLUE}./run-all-tests.sh${NC}"
echo -e "  5. View in Jenkins:     ${BLUE}http://192.168.49.2:32000${NC}"
echo -e "  6. View Grafana:        ${BLUE}https://192.168.1.103/grafana${NC}"

echo -e "\n${YELLOW}Configuration:${NC}"
echo -e "  API URL (default):      ${BLUE}http://192.168.49.2:31081${NC}"
echo -e "  Web URL (default):      ${BLUE}http://192.168.49.2:31080${NC}"
echo -e "  Results directory:      ${BLUE}./results${NC}"

echo -e "\n${GREEN}Happy Stress Testing! 🚀${NC}\n"
