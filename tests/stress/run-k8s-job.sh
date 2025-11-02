#!/bin/bash

# InsightLearn Stress Testing - Kubernetes Job Runner
# Questo script crea un Kubernetes Job per eseguire i test k6

set -e

# Colori per output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Parametri
TEST_TYPE=${1:-smoke}
API_URL=${2:-http://192.168.49.2:31081}
WEB_URL=${3:-http://192.168.49.2:31080}
JOB_ID=$(date +%s)

echo -e "${BLUE}==========================================${NC}"
echo -e "${BLUE}  InsightLearn Stress Testing (K8s Job)${NC}"
echo -e "${BLUE}==========================================${NC}"
echo -e "Test Type: ${YELLOW}$TEST_TYPE${NC}"
echo -e "API URL: $API_URL"
echo -e "Web URL: $WEB_URL"
echo -e "Job ID: $JOB_ID"
echo ""

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

# Crea il Job Kubernetes
echo -e "${YELLOW}⚙️  Creando Kubernetes Job...${NC}"
cat <<EOF > /tmp/k6-job-${JOB_ID}.yaml
apiVersion: batch/v1
kind: Job
metadata:
  name: k6-test-${JOB_ID}
  namespace: insightlearn
  labels:
    app: k6-stress-test
    test-type: ${TEST_TYPE}
spec:
  backoffLimit: 0
  ttlSecondsAfterFinished: 3600
  template:
    metadata:
      labels:
        app: k6-stress-test
        job-name: k6-test-${JOB_ID}
    spec:
      restartPolicy: Never
      containers:
      - name: k6
        image: insightlearn/k6-tests:latest
        imagePullPolicy: Never
        command: ["k6", "run", "/tests/${TEST_TYPE}-test.js"]
        env:
        - name: API_URL
          value: "${API_URL}"
        - name: WEB_URL
          value: "${WEB_URL}"
        resources:
          requests:
            memory: "256Mi"
            cpu: "100m"
          limits:
            memory: "512Mi"
            cpu: "500m"
EOF

# Applica il Job
kubectl apply -f /tmp/k6-job-${JOB_ID}.yaml

# Aspetta che il pod sia creato
echo -e "${YELLOW}⏳ Aspettando che il pod sia creato...${NC}"
sleep 3

# Ottieni il nome del pod
POD_NAME=$(kubectl get pods -n insightlearn -l job-name=k6-test-${JOB_ID} -o jsonpath='{.items[0].metadata.name}' 2>/dev/null || echo "")

if [ -z "$POD_NAME" ]; then
    echo -e "${RED}❌ Errore: Pod non trovato${NC}"
    kubectl get jobs -n insightlearn -l app=k6-stress-test
    exit 1
fi

# Aspetta che il pod sia pronto
echo -e "${YELLOW}⏳ Aspettando che il pod '$POD_NAME' sia pronto...${NC}"
kubectl wait --for=condition=ready pod/$POD_NAME -n insightlearn --timeout=60s 2>/dev/null || echo "Timeout in attesa del pod (normale, il job potrebbe essere già completato)"

# Segui i log in tempo reale
echo ""
echo -e "${GREEN}📊 Log del test:${NC}"
echo -e "${BLUE}==========================================${NC}"
kubectl logs -f $POD_NAME -n insightlearn 2>/dev/null || kubectl logs $POD_NAME -n insightlearn

# Verifica lo stato finale
echo ""
echo -e "${BLUE}==========================================${NC}"
JOB_STATUS=$(kubectl get job k6-test-${JOB_ID} -n insightlearn -o jsonpath='{.status.conditions[0].type}' 2>/dev/null || echo "Unknown")

if [ "$JOB_STATUS" = "Complete" ]; then
    echo -e "${GREEN}✅ Test completato con successo!${NC}"
    echo ""
    echo "Per vedere di nuovo i log:"
    echo "  kubectl logs $POD_NAME -n insightlearn"
    echo ""
    echo "Per eliminare il job:"
    echo "  kubectl delete job k6-test-${JOB_ID} -n insightlearn"
    exit 0
else
    echo -e "${RED}❌ Test fallito o non completato${NC}"
    echo "Status: $JOB_STATUS"
    kubectl get job k6-test-${JOB_ID} -n insightlearn
    exit 1
fi
