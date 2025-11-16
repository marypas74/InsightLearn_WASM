# InsightLearn Backup & Restore - Analisi DevOps

**Data Analisi**: 2025-11-16
**Versione Sistema**: 1.6.7-dev
**DevOps Engineer**: Claude Code Architect Review

---

## 📋 Executive Summary

Il sistema di backup funziona correttamente, ma sono stati identificati **3 problemi critici** che impediscono il restore automatico.

**Status Backup**: ✅ Operativo
**Status Restore**: ❌ Non Funzionante (richiede fix)

---

## ✅ Componenti Funzionanti

### 1. Script di Backup
**File**: `/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/k8s/backup-cluster-state.sh`

**Status**: ✅ **OPERATIVO**

- Ultimo backup: 2025-11-16 10:05:03
- Dimensione: 112 KB
- Rotazione: Mantiene 2 backup (backup-1.tar.gz, backup-2.tar.gz)
- Contenuto: 19 file YAML con tutte le risorse Kubernetes

**Risorse Backuppate**:
- ✅ Namespaces (5.4 KB)
- ✅ Deployments (85 KB)
- ✅ StatefulSets (13.7 KB)
- ✅ ConfigMaps (263 KB)
- ✅ Secrets (25 KB)
- ✅ Services (31 KB)
- ✅ PersistentVolumes (14 KB)
- ✅ PersistentVolumeClaims (10 KB)
- ✅ Ingresses, Roles, RoleBindings, ServiceAccounts
- ✅ ClusterRoles, ClusterRoleBindings
- ✅ Custom Resource Definitions

**Log Backup**: `/var/log/k3s-backup.log` - tutti i backup completati con successo

---

## ❌ Problemi Identificati

### Problema 1: Symlink Snapshot Mancante (CRITICO)

**Severità**: 🔴 CRITICO
**Impatto**: Auto-restore non funziona

**Descrizione**:
Il servizio systemd `k3s-auto-restore.service` ha una condizione:
```ini
ConditionPathExists=/var/backups/k3s-cluster/k3s-cluster-snapshot.tar.gz
```

Ma lo script di backup crea:
- `k3s-cluster-backup-1.tar.gz`
- `k3s-cluster-backup-2.tar.gz`
- `latest-backup.tar.gz` (symlink)

Il file `k3s-cluster-snapshot.tar.gz` **NON ESISTE**, quindi il servizio non parte mai.

**Fix**:
```bash
sudo /home/mpasqui/insightlearn_WASM/InsightLearn_WASM/k8s/fix-restore-symlink.sh
```

Questo crea:
```bash
ln -sf /var/backups/k3s-cluster/latest-backup.tar.gz \
       /var/backups/k3s-cluster/k3s-cluster-snapshot.tar.gz
```

**Verifica Fix**:
```bash
systemctl show k3s-auto-restore.service -p ConditionResult
# Output atteso: ConditionResult=yes
```

---

### Problema 2: Servizio Auto-Restore Non Trovato

**Severità**: 🔴 CRITICO
**Impatto**: Nessun restore automatico al boot

**Descrizione**:
```bash
systemctl list-unit-files | grep k3s-auto-restore
# Output: (vuoto o service disabled)
```

Il servizio esiste in `/etc/systemd/system/k3s-auto-restore.service` ma:
1. ConditionPathExists fallisce (problema #1)
2. Potrebbe non essere enabled

**File Servizio**:
```ini
[Unit]
Description=K3s Cluster Auto-Restore Service
After=k3s.service
Wants=k3s.service
ConditionPathExists=/var/backups/k3s-cluster/k3s-cluster-snapshot.tar.gz

[Service]
Type=oneshot
ExecStart=/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/k8s/restore-cluster-state.sh
TimeoutStartSec=600

[Install]
WantedBy=multi-user.target
```

**Fix**:
```bash
# Dopo aver creato il symlink (problema #1)
sudo systemctl daemon-reload
sudo systemctl enable k3s-auto-restore.service
sudo systemctl status k3s-auto-restore.service
```

---

### Problema 3: Script di Restore Non Testato

**Severità**: 🟡 MEDIO
**Impatto**: Comportamento restore non verificato

**Descrizione**:
Lo script `restore-cluster-state.sh` contiene logica complessa:

1. **Controllo "Cluster Vuoto"**:
   ```bash
   DEPLOYMENT_COUNT=$(kubectl get deployments --all-namespaces --no-headers | wc -l)
   if [[ $DEPLOYMENT_COUNT -gt 5 ]]; then
       info "Cluster appears healthy, skipping restore"
       exit 0
   fi
   ```

   Problema: Se il cluster ha >5 deployment, il restore viene skippato. Questo potrebbe impedire il restore anche quando necessario.

2. **Restore Una Volta Al Giorno**:
   ```bash
   RESTORE_DATE=$(date +%Y%m%d)
   if [[ -f "$STATE_FILE" ]]; then
       LAST_RESTORE=$(cat "$STATE_FILE")
       if [[ "$LAST_RESTORE" == "$RESTORE_DATE" ]]; then
           exit 0
       fi
   fi
   ```

   Problema: Se serve un secondo restore nella stessa giornata (es: dopo un secondo crash), non viene eseguito.

**Raccomandazione**: Testare il restore in ambiente di staging prima di usarlo in produzione.

---

## 🧪 Testing Eseguito

### Test Suite
Script: `/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/k8s/test-backup-restore.sh`

**Risultati**:
- ✅ TEST 1/8: Backup files - PASS (2 file trovati)
- ✅ TEST 2/8: Latest symlink - PASS
- ❌ TEST 3/8: Snapshot symlink - FAIL (mancante)
- ✅ TEST 4/8: Backup contents - PASS (19 YAML files)
- ✅ TEST 5/8: Backup script - PASS
- ✅ TEST 6/8: Restore script - PASS
- ❌ TEST 7/8: Auto-restore service - FAIL (condition not met)
- ✅ TEST 8/8: Backup logs - PASS

**Score**: 6/8 (75%)

---

## 📊 Metriche Prometheus

**Endpoint**: http://192.168.1.114:9101/metrics

**Metriche Disponibili**:
- `insightlearn_dr_backup_last_success_timestamp_seconds`: 1763046303 (16 Nov 10:05)
- `insightlearn_dr_backup_size_bytes`: 111616 (~109 KB)
- `insightlearn_dr_backup_last_status`: 1 (success)
- `insightlearn_dr_backup_count`: 2
- `insightlearn_dr_backup_age_seconds`: ~20000 (5.5 ore)
- `insightlearn_dr_next_backup_seconds`: ~300 (prossimo backup tra 5 minuti)

**Dashboard Grafana**: http://localhost:3000
→ Dashboards → InsightLearn → "Disaster Recovery & Backups"

---

## 🔧 Piano di Remediation

### Azione Immediata (Ora)

```bash
# 1. Creare symlink snapshot
sudo /home/mpasqui/insightlearn_WASM/InsightLearn_WASM/k8s/fix-restore-symlink.sh

# 2. Verificare servizio
sudo systemctl daemon-reload
sudo systemctl enable k3s-auto-restore.service
systemctl show k3s-auto-restore.service -p ConditionResult

# 3. Testare backup manuale
sudo /home/mpasqui/insightlearn_WASM/InsightLearn_WASM/k8s/backup-cluster-state.sh

# 4. Verificare metriche
curl -s http://192.168.1.114:9101/metrics | grep insightlearn_dr_backup
```

### Azione Breve Termine (Prossimi Giorni)

1. **Test Restore in Staging**:
   - Creare cluster di test
   - Testare restore completo
   - Verificare che tutti i pod ripartano
   - Documentare problemi

2. **Modificare Logica Restore** (opzionale):
   - Aggiungere flag `--force` per bypassare controllo deployment count
   - Permettere multiple restore nella stessa giornata se richiesto esplicitamente

3. **Automatizzare Test**:
   - Schedulare test mensile del restore
   - Creare alert Grafana se backup age > 2 ore

### Azione Lungo Termine (Prossime Settimane)

1. **Migliorare Strategia Backup**:
   - Valutare backup incrementali
   - Aggiungere backup database esterni (dump SQL Server, MongoDB)
   - Implementare backup offsite (S3, cloud storage)

2. **Disaster Recovery Plan Completo**:
   - Documentare RTO (Recovery Time Objective): Target 15 minuti
   - Documentare RPO (Recovery Point Objective): Target 1 ora
   - Creare runbook operativo per disaster recovery

---

## 📝 File Modificati/Creati

1. **Creato**: `/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/k8s/fix-restore-symlink.sh`
   - Script per creare symlink snapshot

2. **Creato**: `/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/k8s/test-backup-restore.sh`
   - Test suite completa backup/restore

3. **Creato**: `/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/BACKUP-RESTORE-REPORT.md`
   - Questo documento

---

## ✅ Checklist Post-Fix

Dopo aver applicato i fix, verificare:

- [ ] Symlink snapshot esiste: `ls -l /var/backups/k3s-cluster/k3s-cluster-snapshot.tar.gz`
- [ ] Servizio auto-restore enabled: `systemctl is-enabled k3s-auto-restore.service`
- [ ] Condition servizio OK: `systemctl show k3s-auto-restore.service -p ConditionResult`
- [ ] Backup script eseguito manualmente con successo
- [ ] Metriche Prometheus aggiornate
- [ ] Dashboard Grafana mostra backup recente (<1 ora)
- [ ] Test suite passa 8/8 test

---

## 🚨 Note Importanti

1. **NON testare il restore in produzione** senza aver verificato prima in staging
2. Il restore sovrascrive tutte le risorse Kubernetes esistenti
3. I backup NON includono i dati nei PersistentVolumes (solo i claim)
4. Per backup completo serve anche:
   - Dump SQL Server database
   - Backup GridFS MongoDB (video)
   - Snapshot ZFS pool (se usato)

---

## 📞 Contatti

**Problemi/Domande**: Aprire issue su GitHub
**Emergency**: Consultare runbook disaster recovery (da creare)

---

**Fine Report**
