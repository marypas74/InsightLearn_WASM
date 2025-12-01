#!/bin/bash
# K8s Auto-Recovery Script
# Triggered 10 minutes after boot if cluster not healthy
# Implements disaster recovery from CLAUDE.md

LOG_FILE="/var/log/k8s-auto-recovery.log"
REQUIRED_PODS=8

log() {
    echo "[$(date)] $1" >> $LOG_FILE
}

log "=== K8s Auto-Recovery Check Started ==="

# 1. Check ZFS pool
if ! /usr/local/sbin/zpool status k3spool >/dev/null 2>&1; then
    log "ZFS pool not imported, importing..."
    /usr/local/sbin/zpool import -d /home k3spool 2>>$LOG_FILE
    /usr/local/sbin/zfs mount -a 2>>$LOG_FILE
    log "ZFS pool imported"
    sleep 5
fi

# 2. Check K3s service
if ! systemctl is-active k3s >/dev/null 2>&1; then
    log "K3s not running, starting..."
    systemctl start k3s
    sleep 60
fi

# 3. Wait for kubectl to be ready
for i in {1..30}; do
    if kubectl get nodes >/dev/null 2>&1; then
        break
    fi
    log "Waiting for kubectl... attempt $i"
    sleep 10
done

# 4. Add node label for PV affinity
kubectl label node insightlearn-k3s kubernetes.io/hostname=linux.fritz.box --overwrite 2>>$LOG_FILE
log "Node label applied"

# 5. Check CoreDNS
COREDNS_STATUS=$(kubectl get pods -n kube-system -l k8s-app=kube-dns -o jsonpath={.items[0].status.phase} 2>/dev/null)
if [ "$COREDNS_STATUS" != "Running" ]; then
    log "CoreDNS not healthy, restarting..."
    kubectl delete pod -n kube-system -l k8s-app=kube-dns --force --grace-period=0 2>>$LOG_FILE
    sleep 30
fi

# 6. Count running pods
RUNNING_PODS=$(kubectl get pods -n insightlearn --no-headers 2>/dev/null | grep "Running" | wc -l)
log "Running pods: $RUNNING_PODS (required: $REQUIRED_PODS)"

if [ "$RUNNING_PODS" -lt "$REQUIRED_PODS" ]; then
    log "Not enough running pods, cleaning up stuck pods..."
    
    # Delete pods in bad states
    kubectl delete pods -n insightlearn --field-selector=status.phase=Failed --force --grace-period=0 2>>$LOG_FILE
    kubectl get pods -n insightlearn -o name 2>/dev/null | while read pod; do
        STATUS=$(kubectl get $pod -n insightlearn -o jsonpath={.status.phase} 2>/dev/null)
        if [ "$STATUS" = "Unknown" ]; then
            kubectl delete $pod -n insightlearn --force --grace-period=0 2>>$LOG_FILE
        fi
    done
    
    log "Waiting for pods to recover..."
    sleep 120
    
    RUNNING_PODS=$(kubectl get pods -n insightlearn --no-headers 2>/dev/null | grep "Running" | wc -l)
    log "After cleanup: $RUNNING_PODS running pods"
fi

# 7. Restart socat tunnels
systemctl restart socat-api-tunnel.service socat-wasm-tunnel.service socat-grafana-3000.service 2>>$LOG_FILE
log "Socat tunnels restarted"

# 8. Final status
FINAL_PODS=$(kubectl get pods -n insightlearn --no-headers 2>/dev/null | grep -c "Running")
log "=== Recovery complete: $FINAL_PODS pods running ==="
