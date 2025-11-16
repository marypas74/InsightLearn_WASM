# InsightLearn HA Auto-Healing System - Complete Documentation

**Version**: 2.0.0  
**Date**: 2025-11-16  
**Status**: ✅ Ready for Installation

---

## 📋 Executive Summary

Il sistema HA (High Availability) di InsightLearn fornisce **auto-healing completo** del cluster Kubernetes K3s:

- ✅ **3 backup rotanti** invece di 2 (requisito soddisfatto)
- ✅ **Auto-restore automatico** in caso di crash del server
- ✅ **Watchdog ogni 2 minuti** che monitora e ripristina
- ✅ **Verifiche post-restore** fino al corretto funzionamento
- ✅ **Zero downtime** dopo riavvio o crash

---

## 🎯 Funzionalità Implementate

### 1. Backup Rotante (3 copie)

**File**: `k8s/backup-cluster-state.sh`

Il backup script ora mantiene **3 backup** invece di 2:

```
/var/backups/k3s-cluster/
  ├── k3s-cluster-backup-1.tar.gz  (più vecchio)
  ├── k3s-cluster-backup-2.tar.gz  (medio)
  ├── k3s-cluster-backup-3.tar.gz  (più recente)
  ├── latest-backup.tar.gz -> backup-3.tar.gz (symlink)
  └── k3s-cluster-snapshot.tar.gz -> backup-3.tar.gz (symlink per restore)
```

**Rotazione Automatica**:
- Ogni backup sovrascrive il file più vecchio dei 3
- Garantisce 3 punti di ripristino temporali
- Eseguito automaticamente ogni ora (cron)

### 2. HA Watchdog Auto-Healing

**File**: `k8s/insightlearn-ha-watchdog.sh`

Watchdog che monitora e ripristina il cluster automaticamente:

**Health Checks** (ogni 2 minuti):
1. ✓ K3s service status
2. ✓ kubectl connectivity
3. ✓ Deployment count (min: 5)
4. ✓ Running pods (min: 8 nel namespace insightlearn)
5. ✓ Critical services (API, MongoDB, Redis, SQL Server)

**Auto-Restore Workflow**:
```
Health Check FAIL
  ↓
Verifica backup exists
  ↓
Execute restore script
  ↓
Wait 60s for pods
  ↓
Verify restoration (5 tentativi)
  ↓
SUCCESS o FAIL (log + notifica)
```

**Log**: `/var/log/insightlearn-watchdog.log`

### 3. Systemd Timer

**Files**:
- `k8s/insightlearn-ha-watchdog.service`
- `k8s/insightlearn-ha-watchdog.timer`

**Configurazione**:
- **Primo check**: 2 minuti dopo boot
- **Intervallo**: Ogni 2 minuti
- **Persistente**: Sì (esegue immediatamente se mancato durante shutdown)
- **Auto-start**: Sì (abilitato al boot)

---

## 🚀 Installazione Sistema HA

### Step 1: Installare il Watchdog

```bash
cd /home/mpasqui/insightlearn_WASM/InsightLearn_WASM/k8s
sudo ./install-ha-watchdog.sh
```

Output atteso:
```
==========================================
InsightLearn HA Watchdog Installation
==========================================

[1/5] Installing watchdog script...
  ✓ Installed: /usr/local/bin/insightlearn-ha-watchdog.sh

[2/5] Installing systemd service...
  ✓ Installed: /etc/systemd/system/insightlearn-ha-watchdog.service

[3/5] Installing systemd timer...
  ✓ Installed: /etc/systemd/system/insightlearn-ha-watchdog.timer

[4/5] Enabling services...
  ✓ Services enabled

[5/5] Starting timer...
  ✓ Timer started

==========================================
Installation Complete!
==========================================
```

### Step 2: Eseguire il Fix del Sistema di Restore

```bash
sudo ./fix-and-test-restore.sh
```

Questo script:
1. Esegue un backup (creando il 3° backup e i symlink)
2. Verifica che tutti i symlink esistano
3. Ricarica systemd
4. Verifica che k3s-auto-restore.service sia pronto

### Step 3: Testare il Watchdog

```bash
sudo ./test-ha-watchdog.sh
```

Verifica che il watchdog funzioni correttamente senza aspettare il timer.

---

## 📊 Verifiche Post-Installazione

### Verifica Timer Attivo

```bash
systemctl status insightlearn-ha-watchdog.timer
```

Output atteso:
```
● insightlearn-ha-watchdog.timer - InsightLearn HA Watchdog Timer
     Loaded: loaded (/etc/systemd/system/insightlearn-ha-watchdog.timer; enabled)
     Active: active (waiting)
```

### Verifica Prossima Esecuzione

```bash
systemctl list-timers insightlearn-ha-watchdog.timer
```

Output atteso:
```
NEXT                         LEFT          LAST  PASSED  UNIT
Sat 2025-11-16 14:52:00 CET  1min 30s left -     -       insightlearn-ha-watchdog.timer
```

### Verifica Log Watchdog

```bash
tail -f /var/log/insightlearn-watchdog.log
```

Output atteso (cluster healthy):
```
[2025-11-16 14:50:00] INFO: ===========================================
[2025-11-16 14:50:00] INFO: HA Watchdog - Health Check Started
[2025-11-16 14:50:00] INFO: ===========================================
[2025-11-16 14:50:01] ✓ K3s service is running
[2025-11-16 14:50:01] ✓ kubectl connectivity OK
[2025-11-16 14:50:02] ✓ Deployment count OK: 13
[2025-11-16 14:50:02] ✓ Running pods OK: 13
[2025-11-16 14:50:03] ✓ Service 'insightlearn-api' is running
[2025-11-16 14:50:03] ✓ Service 'mongodb' is running
[2025-11-16 14:50:03] ✓ Service 'redis' is running
[2025-11-16 14:50:03] ✓ Service 'sqlserver' is running
[2025-11-16 14:50:03] ===========================================
[2025-11-16 14:50:03] ✓✓✓ Cluster is HEALTHY - No action needed
[2025-11-16 14:50:03] ===========================================
```

### Verifica Backup Rotazione (3 file)

```bash
ls -lh /var/backups/k3s-cluster/
```

Output atteso:
```
-rw-r--r--. k3s-cluster-backup-1.tar.gz (109K)
-rw-r--r--. k3s-cluster-backup-2.tar.gz (109K)
-rw-r--r--. k3s-cluster-backup-3.tar.gz (109K)
lrwxrwxrwx. latest-backup.tar.gz -> k3s-cluster-backup-3.tar.gz
lrwxrwxrwx. k3s-cluster-snapshot.tar.gz -> k3s-cluster-backup-3.tar.gz
```

---

## 🔥 Scenario di Crash e Recovery

### Cosa succede quando il server crasha

1. **Server riavvio** (t=0s)
2. **K3s parte** (t=30s)
3. **Watchdog timer triggered** (t=120s - 2 minuti dopo boot)
4. **Health check eseguito**:
   - Se cluster healthy (>5 deployment, >8 pods): ✅ Nessuna azione
   - Se cluster unhealthy: ⚠️ Trigger auto-restore

5. **Auto-Restore Workflow** (se triggered):
   ```
   [t+120s] Watchdog rileva cluster unhealthy
   [t+121s] Verifica backup exists: /var/backups/k3s-cluster/k3s-cluster-snapshot.tar.gz
   [t+122s] Execute: /home/.../restore-cluster-state.sh
   [t+180s] Restore completo, wait 60s for pods
   [t+240s] Verification attempt 1/5
   [t+270s] Verification attempt 2/5 (se fallita)
   ...
   [t+450s max] SUCCESS o MANUAL INTERVENTION REQUIRED
   ```

6. **Verification Loop**:
   - Massimo 5 tentativi di verifica
   - 30 secondi tra ogni tentativo
   - Se tutti falliscono: Log ERROR, richiede intervento manuale

7. **Cluster Operativo**:
   - Watchdog continua ogni 2 minuti
   - Mantiene cluster healthy automaticamente

---

## 📝 File del Sistema HA

### Script Principali

| File | Scopo | Location |
|------|-------|----------|
| **backup-cluster-state.sh** | Backup script (3 copie rotanti) | `/home/.../k8s/` |
| **restore-cluster-state.sh** | Restore script | `/home/.../k8s/` |
| **insightlearn-ha-watchdog.sh** | Watchdog auto-healing | `/usr/local/bin/` (installed) |
| **fix-and-test-restore.sh** | Fix completo symlink e test | `/home/.../k8s/` |
| **install-ha-watchdog.sh** | Installa watchdog system | `/home/.../k8s/` |
| **test-ha-watchdog.sh** | Test manuale watchdog | `/home/.../k8s/` |

### Systemd Units

| File | Scopo |
|------|-------|
| **insightlearn-ha-watchdog.service** | Systemd service unit |
| **insightlearn-ha-watchdog.timer** | Systemd timer unit (2 min) |
| **k3s-auto-restore.service** | Auto-restore al boot (legacy) |

### Log Files

| File | Contenuto |
|------|-----------|
| **/var/log/insightlearn-watchdog.log** | Watchdog execution log |
| **/var/log/k3s-backup.log** | Backup script log |

### Backup Files

| File | Descrizione |
|------|-------------|
| **k3s-cluster-backup-1.tar.gz** | Backup più vecchio |
| **k3s-cluster-backup-2.tar.gz** | Backup medio |
| **k3s-cluster-backup-3.tar.gz** | Backup più recente |
| **latest-backup.tar.gz** | Symlink al più recente |
| **k3s-cluster-snapshot.tar.gz** | Symlink per restore service |

---

## 🔧 Comandi Utili

### Gestione Watchdog

```bash
# Start watchdog timer
sudo systemctl start insightlearn-ha-watchdog.timer

# Stop watchdog timer
sudo systemctl stop insightlearn-ha-watchdog.timer

# Restart watchdog timer
sudo systemctl restart insightlearn-ha-watchdog.timer

# Check timer status
systemctl status insightlearn-ha-watchdog.timer

# List next execution
systemctl list-timers insightlearn-ha-watchdog.timer

# Disable watchdog (prevent auto-start)
sudo systemctl disable insightlearn-ha-watchdog.timer

# Enable watchdog
sudo systemctl enable insightlearn-ha-watchdog.timer
```

### Esecuzione Manuale

```bash
# Test watchdog manualmente (richiede sudo)
sudo /usr/local/bin/insightlearn-ha-watchdog.sh

# Oppure usa lo script wrapper
sudo /home/mpasqui/insightlearn_WASM/InsightLearn_WASM/k8s/test-ha-watchdog.sh
```

### Monitoraggio Log

```bash
# Follow watchdog log in real-time
tail -f /var/log/insightlearn-watchdog.log

# Last 50 lines
tail -50 /var/log/insightlearn-watchdog.log

# Search for errors
grep ERROR /var/log/insightlearn-watchdog.log

# Search for auto-restore events
grep "Auto-Restore" /var/log/insightlearn-watchdog.log
```

### Backup Management

```bash
# List backups
ls -lh /var/backups/k3s-cluster/

# Check backup age
stat /var/backups/k3s-cluster/latest-backup.tar.gz

# Manual backup execution
sudo /home/mpasqui/insightlearn_WASM/InsightLearn_WASM/k8s/backup-cluster-state.sh

# Verify backup contents
tar -tzf /var/backups/k3s-cluster/latest-backup.tar.gz | head -20
```

---

## ⚙️ Configurazione Avanzata

### Modificare Intervallo Watchdog

Modificare `/etc/systemd/system/insightlearn-ha-watchdog.timer`:

```ini
[Timer]
# Cambia da 2min a 5min
OnUnitActiveSec=5min
```

Poi ricaricare:
```bash
sudo systemctl daemon-reload
sudo systemctl restart insightlearn-ha-watchdog.timer
```

### Modificare Soglie Health Check

Modificare `/usr/local/bin/insightlearn-ha-watchdog.sh`:

```bash
# Configuration
MIN_DEPLOYMENTS=5    # Cambia soglia deployment
MIN_RUNNING_PODS=8   # Cambia soglia pod
NAMESPACE="insightlearn"
```

### Aggiungere Critical Services

Modificare `/usr/local/bin/insightlearn-ha-watchdog.sh`:

```bash
CRITICAL_SERVICES=("insightlearn-api" "mongodb" "redis" "sqlserver" "nuovo-servizio")
```

---

## 🚨 Troubleshooting

### Watchdog Non Parte

**Problema**: `systemctl status insightlearn-ha-watchdog.timer` mostra "inactive"

**Soluzione**:
```bash
sudo systemctl start insightlearn-ha-watchdog.timer
sudo systemctl enable insightlearn-ha-watchdog.timer
```

### Watchdog Fallisce Permission Denied

**Problema**: Log mostra "Permission denied" su kubectl commands

**Soluzione**: Verificare che lo script abbia accesso a kubectl:
```bash
sudo -i
export KUBECONFIG=/etc/rancher/k3s/k3s.yaml
kubectl get nodes
```

Se funziona, aggiungere KUBECONFIG al service file.

### Auto-Restore Non Funziona

**Problema**: Cluster rimane unhealthy dopo restore

**Verifica**:
1. Check backup file exists:
   ```bash
   ls -lh /var/backups/k3s-cluster/k3s-cluster-snapshot.tar.gz
   ```

2. Check restore script executable:
   ```bash
   test -x /home/mpasqui/insightlearn_WASM/InsightLearn_WASM/k8s/restore-cluster-state.sh && echo "OK"
   ```

3. Execute restore manually:
   ```bash
   sudo /home/mpasqui/insightlearn_WASM/InsightLearn_WASM/k8s/restore-cluster-state.sh
   ```

4. Check log:
   ```bash
   tail -100 /var/log/insightlearn-watchdog.log
   ```

### Backup Rotazione Non Funziona

**Problema**: Non vengono creati 3 backup

**Verifica**:
1. Check backup script:
   ```bash
   grep "BACKUP_3" /home/mpasqui/insightlearn_WASM/InsightLearn_WASM/k8s/backup-cluster-state.sh
   ```

2. Execute backup manually:
   ```bash
   sudo /home/mpasqui/insightlearn_WASM/InsightLearn_WASM/k8s/backup-cluster-state.sh
   ```

3. Verify 3 files created:
   ```bash
   ls -lh /var/backups/k3s-cluster/k3s-cluster-backup-*.tar.gz | wc -l
   # Expected: 3
   ```

---

## ✅ Checklist Post-Installazione

- [ ] Watchdog timer attivo: `systemctl is-active insightlearn-ha-watchdog.timer`
- [ ] Watchdog enabled: `systemctl is-enabled insightlearn-ha-watchdog.timer`
- [ ] Prossima esecuzione schedulata: `systemctl list-timers`
- [ ] Log file creato: `ls -lh /var/log/insightlearn-watchdog.log`
- [ ] 3 backup esistono: `ls /var/backups/k3s-cluster/k3s-cluster-backup-*.tar.gz`
- [ ] Symlink snapshot esiste: `ls -lh /var/backups/k3s-cluster/k3s-cluster-snapshot.tar.gz`
- [ ] Health check passes: `sudo test-ha-watchdog.sh`
- [ ] Cluster healthy: `kubectl get pods -n insightlearn`

---

## 📞 Support

**Problemi/Domande**: Aprire issue su GitHub  
**Log Analysis**: Inviare `/var/log/insightlearn-watchdog.log` per troubleshooting  
**Emergency**: Consultare BACKUP-RESTORE-REPORT.md

---

**Fine Documentazione HA System v2.0.0**
