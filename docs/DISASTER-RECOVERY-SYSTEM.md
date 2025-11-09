# K3s Disaster Recovery System

**Version**: 1.0.0
**Status**: ✅ Production Ready
**Last Updated**: 2025-11-09

---

## 📋 Overview

Sistema automatico di **backup e restore** per cluster K3s InsightLearn, progettato per sistemi **non presidiati** che richiede **zero intervento manuale**.

### Funzionalità Principali

✅ **Backup automatico orario** con sovrascrittura (risparmio spazio)
✅ **Restore automatico al boot** in caso di crash del sistema
✅ **Detection intelligente** di crash del cluster (vs. riavvio normale)
✅ **Logging completo** per troubleshooting remoto
✅ **Non-interattivo** - nessuna richiesta di conferma

---

## 🏗️ Architettura

### Componenti

```
┌─────────────────────────────────────────────────────────────┐
│                    DISASTER RECOVERY SYSTEM                  │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  ┌──────────────────┐         ┌──────────────────┐          │
│  │  Cron Job        │         │  Systemd Service │          │
│  │  (Hourly @:05)   │         │  (Boot time)     │          │
│  └────────┬─────────┘         └────────┬─────────┘          │
│           │                            │                     │
│           ▼                            ▼                     │
│  ┌──────────────────┐         ┌──────────────────┐          │
│  │ backup-cluster-  │         │ restore-cluster- │          │
│  │ state.sh         │         │ state.sh         │          │
│  │                  │         │                  │          │
│  │ • ETCD snapshot  │         │ • Wait K3s ready│          │
│  │ • All K8s res.   │         │ • Check if empty│          │
│  │ • Compress       │         │ • Apply resources│         │
│  │ • Overwrite      │         │ • Verify health │          │
│  └────────┬─────────┘         └────────┬─────────┘          │
│           │                            │                     │
│           ▼                            ▼                     │
│  ┌──────────────────────────────────────────────┐           │
│  │    /var/backups/k3s-cluster/                 │           │
│  │    k3s-cluster-snapshot.tar.gz (~50-100MB)   │           │
│  └──────────────────────────────────────────────┘           │
│                                                               │
│  Logs:                                                       │
│  • /var/log/k3s-backup.log                                   │
│  • /var/log/k3s-restore.log                                  │
│                                                               │
└─────────────────────────────────────────────────────────────┘
```

### Flusso di Backup (Ogni ora)

```
1. Cron trigger (hourly at :05)
   ↓
2. backup-cluster-state.sh runs
   ↓
3. Create ETCD snapshot
   ↓
4. Export all Kubernetes resources (YAML)
   ↓
5. Compress to single tar.gz
   ↓
6. Overwrite /var/backups/k3s-cluster/k3s-cluster-snapshot.tar.gz
   ↓
7. Cleanup old ETCD snapshots (keep last 3)
   ↓
8. Done ✓
```

### Flusso di Restore (Al boot)

```
1. System boot
   ↓
2. k3s.service starts
   ↓
3. k3s-auto-restore.service triggers
   ↓
4. restore-cluster-state.sh runs
   ↓
5. Wait for K3s API ready (max 5 min)
   ↓
6. Check cluster state
   ├─ Healthy (>5 deployments) → Skip restore, exit ✓
   └─ Empty/Crashed → Continue restore
      ↓
7. Extract latest backup
   ↓
8. Apply resources in order:
   - Namespaces
   - StorageClasses, PVs, PVCs
   - ServiceAccounts, RBAC
   - ConfigMaps, Secrets
   - Services
   - Deployments, StatefulSets
   - Ingresses
   ↓
9. Verify health (nodes, pods)
   ↓
10. Done ✓
```

---

## 🌐 Cloudflare Tunnel Integration

Il sistema di disaster recovery include il **ripristino automatico del tunnel Cloudflare** per garantire che l'accesso esterno sia immediatamente disponibile dopo un riavvio.

### Componenti Cloudflare

- **Systemd Service**: `cloudflared-tunnel.service` - Auto-start al boot
- **Verification Script**: `verify-cloudflare-tunnel.sh` - Check e restart se necessario
- **Config File**: `/home/mpasqui/.cloudflared/config.yml`
- **Tunnel ID**: `4d4a2ce0-9133-4761-9886-90be465abc79`

### Flusso di Ripristino Cloudflare

```
1. Cluster restore completes
   ↓
2. verify-cloudflare-tunnel.sh runs
   ↓
3. Check if cloudflared systemd service exists
   ├─ Service exists & enabled → Check if running
   │  ├─ Running → ✓ Done
   │  └─ Not running → Start service
   └─ Service not exists → Start manual process
      ↓
4. Verify local NodePorts accessible (31081, 31090)
   ↓
5. Done ✓
```

### External Access Endpoints

Dopo restore, questi endpoint saranno accessibili via Cloudflare:

- **Frontend**: https://wasm.insightlearn.cloud/
- **API**: https://wasm.insightlearn.cloud/api/
- **Health Check**: https://wasm.insightlearn.cloud/health

**Note**: Propagazione DNS può richiedere 30-60 secondi dopo restart.

---

## 🚀 Installation

### Quick Install (Recommended)

```bash
cd /home/mpasqui/insightlearn_WASM/InsightLearn_WASM/k8s
sudo ./install-disaster-recovery.sh
```

**Questo script esegue**:
1. ✅ Crea directory di backup (`/var/backups/k3s-cluster`)
2. ✅ Installa e abilita systemd service (`k3s-auto-restore.service`)
3. ✅ Configura cron job orario (`/etc/cron.d/k3s-cluster-backup`)
4. ✅ Esegue primo backup iniziale
5. ✅ Crea script di monitoring

### Manual Install (Advanced)

```bash
# 1. Create backup directory
sudo mkdir -p /var/backups/k3s-cluster

# 2. Make scripts executable
chmod +x backup-cluster-state.sh
chmod +x restore-cluster-state.sh

# 3. Install systemd service
sudo cp k3s-auto-restore.service /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable k3s-auto-restore.service

# 4. Configure cron job
sudo tee /etc/cron.d/k3s-cluster-backup <<EOF
5 * * * * root /home/mpasqui/insightlearn_WASM/InsightLearn_WASM/k8s/backup-cluster-state.sh
EOF

# 5. Run initial backup
sudo ./backup-cluster-state.sh
```

---

## 📊 Monitoring & Maintenance

### Check System Status

```bash
# Quick status check (recommended)
sudo /home/mpasqui/insightlearn_WASM/InsightLearn_WASM/k8s/check-disaster-recovery-status.sh
```

**Output esempio**:
```
=== K3s Disaster Recovery Status ===

Backup Status:
  ✓ Latest backup: 87M (2025-11-09 14:05:23)

Systemd Service Status:
  ✓ Auto-restore service: enabled
  ℹ Auto-restore service: inactive (normal, runs at boot)

Cron Job Status:
  ✓ Hourly backup cron job configured
  5 * * * * root /home/mpasqui/.../backup-cluster-state.sh

Recent Logs: [...]
```

### Manual Operations

```bash
# Run backup manually (non sovrascrive cron schedule)
sudo /home/mpasqui/insightlearn_WASM/InsightLearn_WASM/k8s/backup-cluster-state.sh

# Test restore (ATTENZIONE: sovrascrive cluster corrente!)
sudo /home/mpasqui/insightlearn_WASM/InsightLearn_WASM/k8s/restore-cluster-state.sh

# View backup logs (live)
sudo tail -f /var/log/k3s-backup.log

# View restore logs (live)
sudo tail -f /var/log/k3s-restore.log

# Check systemd service status
sudo systemctl status k3s-auto-restore.service

# Check cron job
sudo cat /etc/cron.d/k3s-cluster-backup
```

### Backup Schedule Customization

Per cambiare la frequenza di backup, modificare `/etc/cron.d/k3s-cluster-backup`:

```bash
# Current: Every hour at :05
5 * * * * root /path/to/backup-cluster-state.sh

# Every 2 hours
5 */2 * * * root /path/to/backup-cluster-state.sh

# Every 30 minutes
*/30 * * * * root /path/to/backup-cluster-state.sh

# Daily at 2:00 AM
0 2 * * * root /path/to/backup-cluster-state.sh
```

Dopo la modifica: `sudo systemctl restart crond` (o `cron`)

---

## 🔧 Troubleshooting

### Problema: Backup non viene creato

**Sintomi**: File `/var/backups/k3s-cluster/k3s-cluster-snapshot.tar.gz` non esiste o è vecchio

**Diagnosi**:
```bash
# Check cron job exists
cat /etc/cron.d/k3s-cluster-backup

# Check cron service running
systemctl status crond  # or 'cron' on some systems

# Check backup script permissions
ls -la /home/mpasqui/insightlearn_WASM/InsightLearn_WASM/k8s/backup-cluster-state.sh

# Run backup manually to see errors
sudo /home/mpasqui/insightlearn_WASM/InsightLearn_WASM/k8s/backup-cluster-state.sh
```

**Soluzioni**:
1. ✅ Verificare che cron service sia attivo: `sudo systemctl start crond`
2. ✅ Verificare permessi script: `chmod +x backup-cluster-state.sh`
3. ✅ Verificare spazio disco: `df -h /var/backups`
4. ✅ Controllare log: `sudo tail -50 /var/log/k3s-backup.log`

---

### Problema: Restore non si attiva al boot

**Sintomi**: Dopo riavvio, cluster è vuoto ma restore non è stato eseguito

**Diagnosi**:
```bash
# Check systemd service enabled
systemctl is-enabled k3s-auto-restore.service

# Check service status
systemctl status k3s-auto-restore.service

# Check restore logs
sudo cat /var/log/k3s-restore.log

# Check if backup exists
ls -lh /var/backups/k3s-cluster/k3s-cluster-snapshot.tar.gz
```

**Soluzioni**:
1. ✅ Abilitare service: `sudo systemctl enable k3s-auto-restore.service`
2. ✅ Verificare backup esiste (senza backup, restore viene skippato)
3. ✅ Run manualmente: `sudo /home/mpasqui/insightlearn_WASM/InsightLearn_WASM/k8s/restore-cluster-state.sh`
4. ✅ Controllare journal: `sudo journalctl -u k3s-auto-restore.service -n 50`

---

### Problema: Restore applicato ma pods non partono

**Sintomi**: `kubectl get pods` mostra pods in `Pending`, `ImagePullBackOff`, o `CrashLoopBackOff`

**Diagnosi**:
```bash
# Check pod status details
kubectl get pods --all-namespaces
kubectl describe pod <pod-name> -n <namespace>

# Check node status
kubectl get nodes

# Check PVC status (Persistent Volume Claims)
kubectl get pvc --all-namespaces

# Check events
kubectl get events --all-namespaces --sort-by='.lastTimestamp' | tail -20
```

**Soluzioni**:
1. ✅ **ImagePullBackOff**: Verificare che immagini Docker siano presenti in K3s:
   ```bash
   sudo k3s ctr images ls | grep insightlearn
   # Se mancanti, rebuild e import
   docker-compose build api
   docker save localhost/insightlearn/api:latest | sudo k3s ctr images import -
   ```

2. ✅ **PVC Pending**: Verificare StorageClass e PV availability:
   ```bash
   kubectl get storageclass
   kubectl get pv
   # Se mancanti, riapplicare manifests
   kubectl apply -f k8s/03-sql-server-deployment.yaml
   ```

3. ✅ **CrashLoopBackOff**: Controllare logs del container:
   ```bash
   kubectl logs <pod-name> -n <namespace>
   kubectl logs <pod-name> -n <namespace> --previous  # logs prima del crash
   ```

---

### Problema: Backup file troppo grande

**Sintomi**: `/var/backups/k3s-cluster/k3s-cluster-snapshot.tar.gz` > 500MB

**Cause comuni**:
- Persistent Volumes con molti dati (database, logs)
- ETCD snapshot molto grande (molte risorse)

**Soluzioni**:
1. ✅ **Escludere PV data** (se già backuppati altrove):
   Modificare `backup-cluster-state.sh` e commentare backup di PV data.

2. ✅ **Cleanup ETCD snapshots**:
   ```bash
   # Script già pulisce automaticamente (mantiene last 3)
   ls -lh /var/lib/rancher/k3s/server/db/snapshots/
   ```

3. ✅ **Compressione ZFS** (se abilitata, aiuta):
   ```bash
   sudo zfs get compressratio k3spool
   ```

---

### Problema: Cloudflare Tunnel non si riavvia dopo restore

**Sintomi**: Cluster ripristinato ma `https://wasm.insightlearn.cloud` non accessibile (504 Gateway Timeout)

**Diagnosi**:
```bash
# Check cloudflared service
systemctl status cloudflared-tunnel.service

# Check manual cloudflared process
pgrep -a cloudflared

# Check Cloudflare config
cat /home/mpasqui/.cloudflared/config.yml

# Test local NodePorts
curl http://localhost:31081/health  # API
curl http://localhost:31090/        # Web
```

**Soluzioni**:
1. ✅ **Restart cloudflared service**:
   ```bash
   sudo systemctl restart cloudflared-tunnel.service
   sudo journalctl -u cloudflared-tunnel.service -f
   ```

2. ✅ **Run verification script manualmente**:
   ```bash
   sudo /home/mpasqui/insightlearn_WASM/InsightLearn_WASM/k8s/verify-cloudflare-tunnel.sh
   ```

3. ✅ **Start cloudflared manually** (se service non funziona):
   ```bash
   # Kill stale processes
   pkill -f cloudflared

   # Start in foreground for debugging
   cloudflared tunnel --config /home/mpasqui/.cloudflared/config.yml run

   # Or start in background
   nohup cloudflared tunnel --config /home/mpasqui/.cloudflared/config.yml run > /tmp/cloudflared.log 2>&1 &

   # Check logs
   tail -f /tmp/cloudflared.log
   ```

4. ✅ **Verify Cloudflare Tunnel config**:
   ```bash
   cloudflared tunnel validate /home/mpasqui/.cloudflared/config.yml
   ```

5. ✅ **Check NodePorts are accessible** (prerequisito per tunnel):
   ```bash
   kubectl get svc -n insightlearn | grep NodePort
   # Should show api-service-nodeport (31081) and insightlearn-wasm-blazor-webassembly-nodeport (31090)

   netstat -tlnp | grep -E "31081|31090"
   ```

---

## 📈 Performance & Storage

### Backup Size Estimates

| Cluster Size | Backup Size (compressed) |
|--------------|--------------------------|
| Small (< 20 pods) | 20-50 MB |
| Medium (20-50 pods) | 50-150 MB |
| Large (50-100 pods) | 150-300 MB |
| Very Large (> 100 pods) | 300-500 MB |

### Disk Space Requirements

**Minimum**: 500 MB free in `/var/backups`
**Recommended**: 2 GB free (buffer for temporary files during backup)

**Check space**:
```bash
df -h /var/backups
```

### Backup Duration

- **Backup time**: 1-3 minuti (dipende da cluster size)
- **Restore time**: 2-5 minuti (+ tempo pod startup)

---

## 🔐 Security Considerations

### Sensitive Data in Backups

⚠️ **WARNING**: I backup contengono **Secrets Kubernetes** in chiaro (base64 encoded, facilmente decodificabili).

**File sensibili nel backup**:
- `resources/secrets.yaml` - Contiene tutte le password (DB, JWT, Redis, etc.)
- `k3s-config/k3s.yaml` - Contiene kubeconfig con certificati cluster

### Best Practices

1. ✅ **Proteggere directory backup**:
   ```bash
   sudo chmod 700 /var/backups/k3s-cluster
   sudo chown root:root /var/backups/k3s-cluster
   ```

2. ✅ **Encrypt backup** (opzionale, per produzione):
   ```bash
   # Modificare backup-cluster-state.sh per aggiungere encryption
   gpg --symmetric --cipher-algo AES256 k3s-cluster-snapshot.tar.gz
   ```

3. ✅ **Remote backup copy** (opzionale):
   ```bash
   # Aggiungere a cron job per sync remoto
   rsync -avz /var/backups/k3s-cluster/ remote-server:/backups/k3s/
   ```

---

## 🧪 Testing & Validation

### Test Backup Process

```bash
# Run manual backup
sudo /home/mpasqui/insightlearn_WASM/InsightLearn_WASM/k8s/backup-cluster-state.sh

# Verify backup created
ls -lh /var/backups/k3s-cluster/k3s-cluster-snapshot.tar.gz

# Check backup contents
mkdir /tmp/test-backup
tar -xzf /var/backups/k3s-cluster/k3s-cluster-snapshot.tar.gz -C /tmp/test-backup
ls -R /tmp/test-backup/
rm -rf /tmp/test-backup
```

### Test Restore Process (NON-DESTRUCTIVE)

```bash
# Dry-run restore (check script logic without applying)
# Modificare temporaneamente restore-cluster-state.sh:
# Cambiare "kubectl apply" → "kubectl apply --dry-run=client"

sudo /home/mpasqui/insightlearn_WASM/InsightLearn_WASM/k8s/restore-cluster-state.sh

# Check logs
sudo tail -50 /var/log/k3s-restore.log
```

### Disaster Recovery Drill (DESTRUCTIVE!)

⚠️ **ATTENZIONE**: Questo test **distrugge il cluster corrente**. Solo per ambienti di test!

```bash
# 1. Backup current state
sudo ./k8s/backup-cluster-state.sh

# 2. Simulate crash: delete all deployments
kubectl delete deployments --all -n insightlearn
kubectl delete statefulsets --all -n insightlearn

# 3. Verify cluster is "crashed"
kubectl get pods -n insightlearn  # Should show no pods

# 4. Trigger restore
sudo ./k8s/restore-cluster-state.sh

# 5. Wait for pods to come back
watch kubectl get pods -n insightlearn

# 6. Verify application works
curl http://localhost:31081/health
```

---

## 📚 Related Documentation

- [CLAUDE.md](../CLAUDE.md) - Main project documentation
- [DEPLOYMENT-COMPLETE-GUIDE.md](../DEPLOYMENT-COMPLETE-GUIDE.md) - Deployment guide
- [k8s/README.md](../k8s/README.md) - Kubernetes deployment details

---

## 🆘 Support & Maintenance

### Log Locations

```bash
/var/log/k3s-backup.log          # Backup operations
/var/log/k3s-restore.log         # Restore operations (includes Cloudflare tunnel check)
/var/lib/k3s-restore-state       # Last restore date (prevents duplicate restores)
/tmp/cloudflared.log             # Cloudflare Tunnel logs (if running manually)
```

**Check Cloudflare service logs**:
```bash
sudo journalctl -u cloudflared-tunnel.service -f
```

### Uninstall Disaster Recovery System

```bash
# Disable and remove K3s auto-restore systemd service
sudo systemctl disable k3s-auto-restore.service
sudo rm /etc/systemd/system/k3s-auto-restore.service

# Disable and remove Cloudflare tunnel systemd service (if installed)
sudo systemctl disable cloudflared-tunnel.service
sudo systemctl stop cloudflared-tunnel.service
sudo rm /etc/systemd/system/cloudflared-tunnel.service

# Reload systemd
sudo systemctl daemon-reload

# Remove cron job
sudo rm /etc/cron.d/k3s-cluster-backup

# Remove backups (optional)
sudo rm -rf /var/backups/k3s-cluster

# Remove logs (optional)
sudo rm /var/log/k3s-backup.log /var/log/k3s-restore.log

# Kill cloudflared process (if running manually)
pkill -f cloudflared
```

---

## 📝 Changelog

### Version 1.0.0 (2025-11-09)

- ✅ Initial release
- ✅ Automatic hourly backup with overwrite
- ✅ Automatic restore at boot with intelligent crash detection
- ✅ Cloudflare Tunnel auto-restore integration
- ✅ Systemd services for K3s restore and Cloudflare tunnel
- ✅ Complete logging system with dedicated log files
- ✅ Non-interactive operation for unattended systems
- ✅ Installation script with Cloudflare tunnel setup
- ✅ Comprehensive troubleshooting documentation

---

**Maintainer**: InsightLearn DevOps Team
**Contact**: marcello.pasqui@gmail.com
**Repository**: https://github.com/marypas74/InsightLearn_WASM
