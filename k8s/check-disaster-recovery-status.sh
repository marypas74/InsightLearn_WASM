#!/bin/bash
# Quick status check for disaster recovery system

echo "=== K3s Disaster Recovery Status ==="
echo ""

echo "Backup Status:"
if [[ -f /var/backups/k3s-cluster/k3s-cluster-snapshot.tar.gz ]]; then
    BACKUP_SIZE=$(du -h /var/backups/k3s-cluster/k3s-cluster-snapshot.tar.gz | cut -f1)
    BACKUP_DATE=$(stat -c %y /var/backups/k3s-cluster/k3s-cluster-snapshot.tar.gz | cut -d'.' -f1)
    echo "  ✓ Latest backup: $BACKUP_SIZE ($BACKUP_DATE)"
else
    echo "  ✗ No backup found"
fi

echo ""
echo "Systemd Service Status:"
systemctl is-enabled k3s-auto-restore.service >/dev/null 2>&1 && echo "  ✓ Auto-restore service: enabled" || echo "  ✗ Auto-restore service: disabled"
systemctl is-active k3s-auto-restore.service >/dev/null 2>&1 && echo "  ✓ Auto-restore service: active" || echo "  ℹ Auto-restore service: inactive (normal, runs at boot)"

echo ""
echo "Cloudflare Tunnel Status:"
if systemctl is-enabled cloudflared-tunnel.service >/dev/null 2>&1; then
    systemctl is-active cloudflared-tunnel.service >/dev/null 2>&1 && echo "  ✓ Cloudflared service: running" || echo "  ✗ Cloudflared service: not running"
elif pgrep -f "cloudflared tunnel" >/dev/null 2>&1; then
    echo "  ✓ Cloudflared: running (manual process)"
else
    echo "  ✗ Cloudflared: not running"
fi

# Check external access
if curl -s -m 5 https://www.insightlearn.cloud/health >/dev/null 2>&1; then
    echo "  ✓ External access: OK (https://www.insightlearn.cloud)"
else
    echo "  ℹ External access: not reachable (tunnel may be down or DNS propagating)"
fi

echo ""
echo "Cron Job Status:"
if [[ -f /etc/cron.d/k3s-cluster-backup ]]; then
    echo "  ✓ Hourly backup cron job configured"
    grep "^5 \* \* \* \*" /etc/cron.d/k3s-cluster-backup
else
    echo "  ✗ Cron job not found"
fi

echo ""
echo "Recent Logs:"
echo "  Backup log (last 10 lines):"
tail -n 10 /var/log/k3s-backup.log 2>/dev/null || echo "    (no logs yet)"

echo ""
echo "  Restore log (last 10 lines):"
tail -n 10 /var/log/k3s-restore.log 2>/dev/null || echo "    (no logs yet)"

echo ""
echo "K3s Cluster Status:"
kubectl get nodes 2>/dev/null || echo "  ✗ K3s not accessible"
kubectl get pods -n insightlearn 2>/dev/null | head -n 5 || echo "  ✗ No pods found"

echo ""
echo "==================================="
