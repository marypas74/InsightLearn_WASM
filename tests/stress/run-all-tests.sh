#!/bin/bash
#
# Run All k6 Stress Tests for InsightLearn
#
# Usage:
#   ./run-all-tests.sh [api_url] [web_url]
#
# Examples:
#   ./run-all-tests.sh
#   ./run-all-tests.sh http://192.168.49.2:31081 http://192.168.49.2:31080
#

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
API_URL="${1:-http://192.168.49.2:31081}"
WEB_URL="${2:-http://192.168.49.2:31080}"
RESULTS_DIR="./results"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)

# Create results directory
mkdir -p "$RESULTS_DIR/$TIMESTAMP"

echo -e "${BLUE}╔═══════════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║     InsightLearn Comprehensive Stress Testing Suite          ║${NC}"
echo -e "${BLUE}╚═══════════════════════════════════════════════════════════════╝${NC}"
echo ""
echo -e "${GREEN}API URL:${NC} $API_URL"
echo -e "${GREEN}Web URL:${NC} $WEB_URL"
echo -e "${GREEN}Results Directory:${NC} $RESULTS_DIR/$TIMESTAMP"
echo ""

# Function to run a test
run_test() {
    local test_name=$1
    local test_file=$2
    local duration=$3

    echo -e "\n${YELLOW}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo -e "${YELLOW}  Running: ${test_name}${NC}"
    echo -e "${YELLOW}  Estimated Duration: ${duration}${NC}"
    echo -e "${YELLOW}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}\n"

    if API_URL="$API_URL" WEB_URL="$WEB_URL" k6 run \
        --out json="$RESULTS_DIR/$TIMESTAMP/${test_file%.js}-results.json" \
        "$test_file" 2>&1 | tee "$RESULTS_DIR/$TIMESTAMP/${test_file%.js}-output.log"; then
        echo -e "\n${GREEN}✅ ${test_name} completed successfully!${NC}\n"
        return 0
    else
        echo -e "\n${RED}❌ ${test_name} failed!${NC}\n"
        return 1
    fi
}

# Check if k6 is installed
if ! command -v k6 &> /dev/null; then
    echo -e "${RED}❌ k6 is not installed!${NC}"
    echo -e "${YELLOW}Install k6:${NC}"
    echo -e "  macOS:   brew install k6"
    echo -e "  Ubuntu:  sudo gpg -k"
    echo -e "           sudo gpg --no-default-keyring --keyring /usr/share/keyrings/k6-archive-keyring.gpg --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys C5AD17C747E3415A3642D57D77C6C491D6AC1D69"
    echo -e "           echo 'deb [signed-by=/usr/share/keyrings/k6-archive-keyring.gpg] https://dl.k6.io/deb stable main' | sudo tee /etc/apt/sources.list.d/k6.list"
    echo -e "           sudo apt-get update"
    echo -e "           sudo apt-get install k6"
    echo -e "  Docker:  docker pull grafana/k6:latest"
    exit 1
fi

# Check if services are accessible
echo -e "${BLUE}🔍 Checking service availability...${NC}"

if curl -s -o /dev/null -w "%{http_code}" "$API_URL/health" | grep -q "200"; then
    echo -e "${GREEN}✅ API is accessible${NC}"
else
    echo -e "${YELLOW}⚠️  API health check failed, but continuing...${NC}"
fi

if curl -s -o /dev/null -w "%{http_code}" "$WEB_URL/health" | grep -q "200\|302"; then
    echo -e "${GREEN}✅ Web is accessible${NC}"
else
    echo -e "${YELLOW}⚠️  Web health check failed, but continuing...${NC}"
fi

echo ""

# Track results
declare -a test_results

# Run tests in sequence
echo -e "${BLUE}Starting test sequence...${NC}\n"

# 1. Smoke Test (fast validation)
if run_test "Smoke Test" "smoke-test.js" "~30 seconds"; then
    test_results+=("✅ Smoke Test")
else
    test_results+=("❌ Smoke Test")
    echo -e "${RED}Smoke test failed. Stopping further tests.${NC}"
    exit 1
fi

sleep 5

# 2. Load Test (normal load)
if run_test "Load Test" "load-test.js" "~9 minutes"; then
    test_results+=("✅ Load Test")
else
    test_results+=("❌ Load Test")
fi

sleep 10

# 3. Stress Test (extreme load)
if run_test "Stress Test" "stress-test.js" "~16 minutes"; then
    test_results+=("✅ Stress Test")
else
    test_results+=("❌ Stress Test")
fi

sleep 10

# 4. Spike Test (sudden spike)
if run_test "Spike Test" "spike-test.js" "~4.5 minutes"; then
    test_results+=("✅ Spike Test")
else
    test_results+=("❌ Spike Test")
fi

# Note: Soak test is commented out by default (3+ hours)
# Uncomment to run soak test
# sleep 10
# if run_test "Soak Test" "soak-test.js" "~3 hours"; then
#     test_results+=("✅ Soak Test")
# else
#     test_results+=("❌ Soak Test")
# fi

# Summary
echo -e "\n${BLUE}╔═══════════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║                    TEST SUITE SUMMARY                         ║${NC}"
echo -e "${BLUE}╚═══════════════════════════════════════════════════════════════╝${NC}\n"

for result in "${test_results[@]}"; do
    echo -e "  $result"
done

echo -e "\n${GREEN}Results saved to:${NC} $RESULTS_DIR/$TIMESTAMP"
echo -e "\n${BLUE}Available files:${NC}"
ls -lh "$RESULTS_DIR/$TIMESTAMP/"

# Generate combined report
echo -e "\n${BLUE}Generating combined report...${NC}"

cat > "$RESULTS_DIR/$TIMESTAMP/SUMMARY.md" << EOF
# InsightLearn Stress Testing Summary

**Date:** $(date)
**API URL:** $API_URL
**Web URL:** $WEB_URL

## Test Results

$(printf '%s\n' "${test_results[@]}")

## Files

\`\`\`
$(ls -lh "$RESULTS_DIR/$TIMESTAMP/")
\`\`\`

## Next Steps

1. Review individual test reports (*.log files)
2. Analyze JSON results for detailed metrics
3. Check HTML reports for visual analysis
4. Compare with previous test runs
5. Investigate any failures or performance degradation

## Recommendations

- **All tests passed:** System is ready for production
- **Some tests failed:** Review logs and optimize bottlenecks
- **Stress test failed:** Consider scaling resources
- **Spike test failed:** Improve auto-scaling configuration
- **Soak test failed:** Investigate memory leaks

EOF

echo -e "${GREEN}✅ Combined report generated: $RESULTS_DIR/$TIMESTAMP/SUMMARY.md${NC}"

echo -e "\n${BLUE}╔═══════════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║                  Testing Complete!                            ║${NC}"
echo -e "${BLUE}╚═══════════════════════════════════════════════════════════════╝${NC}\n"
