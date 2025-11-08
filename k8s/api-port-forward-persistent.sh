#!/bin/bash
# Persistent API Port-Forward with Auto-Restart
# Keeps http://localhost:8081 always accessible

NAMESPACE="insightlearn"
SERVICE="api-service"
LOCAL_PORT=8081
REMOTE_PORT=80

echo "[$(date)] Starting persistent port-forward for API"
echo "Access at: http://localhost:${LOCAL_PORT}"

while true; do
    echo "[$(date)] Establishing port-forward..."
    kubectl port-forward -n "$NAMESPACE" "svc/$SERVICE" "${LOCAL_PORT}:${REMOTE_PORT}"

    # If kubectl exits, wait 3 seconds and restart
    echo "[$(date)] Port-forward disconnected, restarting in 3 seconds..."
    sleep 3
done
