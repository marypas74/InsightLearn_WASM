#!/bin/bash
# Persistent Grafana Port-Forward with Auto-Restart
# Keeps http://localhost:3000 always accessible

NAMESPACE="insightlearn"
SERVICE="grafana"
LOCAL_PORT=3000
REMOTE_PORT=3000

echo "[$(date)] Starting persistent port-forward for Grafana"
echo "Access at: http://localhost:${LOCAL_PORT}"

while true; do
    echo "[$(date)] Establishing port-forward..."
    kubectl port-forward -n "$NAMESPACE" "svc/$SERVICE" "${LOCAL_PORT}:${REMOTE_PORT}"

    # If kubectl exits, wait 3 seconds and restart
    echo "[$(date)] Port-forward disconnected, restarting in 3 seconds..."
    sleep 3
done
