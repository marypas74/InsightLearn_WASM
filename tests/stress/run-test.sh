#!/bin/bash

# InsightLearn Stress Testing Runner
# Questo script esegue i test k6 usando Docker

set -e

# Colori per output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Parametri di default
TEST_TYPE=${1:-smoke}
API_URL=${2:-http://192.168.49.2:31081}
WEB_URL=${3:-http://192.168.49.2:31080}

echo -e "${BLUE}==========================================${NC}"
echo -e "${BLUE}  InsightLearn Stress Testing${NC}"
echo -e "${BLUE}==========================================${NC}"
echo -e "Test Type: ${YELLOW}$TEST_TYPE${NC}"
echo -e "API URL: $API_URL"
echo -e "Web URL: $WEB_URL"
echo ""

# Verifica che l'immagine Docker esista
if ! docker image inspect insightlearn/k6-tests:latest > /dev/null 2>&1; then
    echo -e "${YELLOW}⚙️  Building Docker image...${NC}"
    cd "$(dirname "$0")"
    docker build -t insightlearn/k6-tests:latest .
    echo -e "${GREEN}✅ Docker image built successfully${NC}"
    echo ""
fi

# Verifica che il file di test esista
if [ ! -f "$(dirname "$0")/${TEST_TYPE}-test.js" ]; then
    echo -e "${RED}❌ Errore: Test file '${TEST_TYPE}-test.js' non trovato!${NC}"
    echo ""
    echo "Test disponibili:"
    echo "  - smoke   (30 secondi, 1 utente)"
    echo "  - load    (9 minuti, 0-10 utenti)"
    echo "  - stress  (16 minuti, 0-100 utenti)"
    echo "  - spike   (4.5 minuti, 10-200 utenti)"
    echo "  - soak    (3+ ore, 0-20 utenti)"
    echo ""
    echo "Uso: $0 <test-type> [api-url] [web-url]"
    exit 1
fi

# Esegui il test
echo -e "${GREEN}🚀 Eseguendo test $TEST_TYPE...${NC}"
echo ""

docker run --rm \
  --network host \
  -e API_URL="$API_URL" \
  -e WEB_URL="$WEB_URL" \
  insightlearn/k6-tests:latest \
  run /tests/${TEST_TYPE}-test.js

echo ""
echo -e "${GREEN}✅ Test completato!${NC}"
