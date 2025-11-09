# 🔄 Ripristino Pod da Backup

**Data**: 2025-11-09
**Versione**: 1.0.0
**Sistema**: K3s Disaster Recovery

---

## 📊 Overview

Guida completa per **ripristinare pod e deployment** dal sistema di backup automatico.

**Backup Location**: `/var/backups/k3s-cluster/`
**Script Restore**: `k8s/restore-cluster-state.sh`
**Backup Frequency**: Ogni ora (alle :05)

---

## 🎯 Scenari di Ripristino

### Quando Usare il Ripristino

1. **Cluster crashato** → Ripristino automatico al boot
2. **Pod cancellato per errore** → Ripristino selettivo
3. **Deployment corrotto** → Ripristino da backup
4. **Test/rollback** → Ripristino manuale
5. **Disaster recovery completo** → Ripristino totale

---

## 🚀 Opzione 1: Ripristino Automatico (Completo)

**Quando**: Dopo crash del server o riavvio
**Cosa ripristina**: TUTTO il cluster (tutti i pod, deployments, services, secrets, etc.)

### Come Funziona

Il sistema **ripristina automaticamente** al riavvio del server:

```bash
# Il servizio systemd esegue automaticamente
systemctl status k3s-auto-restore.service
```

### Output Atteso

```
● k3s-auto-restore.service - K3s Auto-Restore from Backup
   Loaded: loaded (/etc/systemd/system/k3s-auto-restore.service; enabled)
   Active: inactive (dead) since Sat 2025-11-09 17:00:00 CET
```

### Crash Detection Intelligente

Il sistema **NON ripristina** se il cluster è già sano:
- Se ci sono **>5 deployments** attivi → **SKIP restore** (cluster OK)
- Se ci sono **<5 deployments** → **Esegue restore** (cluster crashato)

### Force Restore (Manuale)

Se vuoi forzare un ripristino completo:

```bash
# 1. Ferma K3s (opzionale, solo se vuoi reset completo)
sudo systemctl stop k3s

# 2. Esegui restore manualmente
sudo /home/mpasqui/insightlearn_WASM/InsightLearn_WASM/k8s/restore-cluster-state.sh

# 3. Riavvia K3s (se lo hai fermato)
sudo systemctl start k3s
```

**⚠️ ATTENZIONE**: Questo sovrascrive TUTTE le risorse nel cluster!

---

## 🎯 Opzione 2: Ripristino Selettivo (Singolo Pod/Deployment)

**Quando**: Hai cancellato per errore un pod o un deployment specifico
**Cosa ripristina**: Solo le risorse che selezioni

### Step 1: Lista Backup Disponibili

```bash
# Vedi backup disponibili
ls -lh /var/backups/k3s-cluster/*.tar.gz

# Output esempio:
# k3s-cluster-backup-1.tar.gz   6.1K  9 nov 17:00
# k3s-cluster-backup-2.tar.gz   6.1K  9 nov 17:04
# latest-backup.tar.gz          27   9 nov 17:04 -> backup-2.tar.gz
```

### Step 2: Estrai il Backup

```bash
# Crea directory temporanea
mkdir -p /tmp/k3s-restore

# Estrai l'ultimo backup
sudo tar -xzf /var/backups/k3s-cluster/latest-backup.tar.gz -C /tmp/k3s-restore

# Oppure usa backup specifico
sudo tar -xzf /var/backups/k3s-cluster/k3s-cluster-backup-1.tar.gz -C /tmp/k3s-restore
```

### Step 3: Trova la Risorsa da Ripristinare

```bash
# Naviga nella directory estratta
cd /tmp/k3s-restore/k3s-backup-*/resources/

# Lista file YAML disponibili
ls -1 *.yaml
```

**Output esempio**:
```
namespaces.yaml
deployments.yaml
services.yaml
secrets.yaml
configmaps.yaml
statefulsets.yaml
persistentvolumeclaims.yaml
ingresses.yaml
```

### Step 4: Ripristina Risorsa Specifica

#### Esempio A: Ripristina Deployment Singolo

```bash
# Visualizza deployments nel backup
kubectl get deployments --all-namespaces \
  -f /tmp/k3s-restore/k3s-backup-*/resources/deployments.yaml --dry-run=server

# Ripristina solo insightlearn-api deployment
kubectl apply -f - <<EOF
$(kubectl get deployment insightlearn-api -n insightlearn \
  -f /tmp/k3s-restore/k3s-backup-*/resources/deployments.yaml \
  -o yaml)
EOF
```

#### Esempio B: Ripristina Tutti i Deployment di un Namespace

```bash
# Ripristina tutti i deployment del namespace 'insightlearn'
kubectl apply -f /tmp/k3s-restore/k3s-backup-*/resources/deployments.yaml \
  --namespace=insightlearn
```

#### Esempio C: Ripristina Secret Specifico

```bash
# Trova secret nel backup
grep -A 10 "name: mssql-sa-password" \
  /tmp/k3s-restore/k3s-backup-*/resources/secrets.yaml

# Estrai e applica
kubectl apply -f - <<EOF
$(sed -n '/name: mssql-sa-password/,/^---/p' \
  /tmp/k3s-restore/k3s-backup-*/resources/secrets.yaml)
EOF
```

### Step 5: Verifica Ripristino

```bash
# Verifica pod è running
kubectl get pods -n insightlearn

# Verifica deployment status
kubectl rollout status deployment/insightlearn-api -n insightlearn

# Vedi logs
kubectl logs -n insightlearn deployment/insightlearn-api --tail=50
```

---

## 📋 Opzione 3: Ripristino Step-by-Step (Manuale)

**Quando**: Vuoi controllare esattamente cosa viene ripristinato
**Vantaggio**: Massimo controllo, puoi escludere risorse specifiche

### Procedura Completa

```bash
# 1. Estrai backup
BACKUP_DIR="/tmp/k3s-restore-$(date +%s)"
mkdir -p "$BACKUP_DIR"
sudo tar -xzf /var/backups/k3s-cluster/latest-backup.tar.gz -C "$BACKUP_DIR"

# 2. Naviga nelle risorse
cd "$BACKUP_DIR"/k3s-backup-*/resources/

# 3. Ripristina in ordine corretto (importante!)

# Step A: Namespaces (primo!)
kubectl apply -f namespaces.yaml

# Step B: Secrets e ConfigMaps
kubectl apply -f secrets.yaml
kubectl apply -f configmaps.yaml

# Step C: PersistentVolumes e Claims
kubectl apply -f persistentvolumes.yaml
kubectl apply -f persistentvolumeclaims.yaml

# Step D: StatefulSets (prima dei Deployments)
kubectl apply -f statefulsets.yaml

# Step E: Deployments
kubectl apply -f deployments.yaml

# Step F: Services
kubectl apply -f services.yaml

# Step G: Ingresses
kubectl apply -f ingresses.yaml

# 4. Verifica tutto è running
kubectl get pods --all-namespaces
```

### Ordine di Restore (IMPORTANTE!)

**Devi rispettare questo ordine** per evitare errori di dipendenze:

```
1. Namespaces         ← Devono esistere prima di tutto
2. Secrets/ConfigMaps ← Riferiti da deployments
3. PersistentVolumes  ← Devono esistere prima dei claims
4. PVC                ← Prima di StatefulSets/Deployments
5. StatefulSets       ← Hanno requisiti PVC
6. Deployments        ← Possono riferire secrets/configmaps
7. Services           ← Espongono i pod
8. Ingresses          ← Routing esterno
```

---

## 🔍 Opzione 4: Ripristino con Filtro (Advanced)

**Quando**: Vuoi ripristinare solo risorse che matchano un pattern

### Esempi Filtri

#### Ripristina solo API pods

```bash
cd /tmp/k3s-restore/k3s-backup-*/resources/

# Estrai solo deployment API
yq eval '. | select(.metadata.name == "insightlearn-api")' deployments.yaml | \
  kubectl apply -f -
```

#### Ripristina tutti i database

```bash
# MongoDB + Redis + SQL Server
for db in mongodb redis sqlserver; do
  yq eval ". | select(.metadata.name | test(\"$db\"))" deployments.yaml | \
    kubectl apply -f -
done
```

#### Ripristina solo namespace specifico

```bash
# Tutto il namespace 'insightlearn'
kubectl apply -f deployments.yaml --namespace=insightlearn
kubectl apply -f services.yaml --namespace=insightlearn
kubectl apply -f secrets.yaml --namespace=insightlearn
```

---

## 🛠️ Troubleshooting

### Errore: "Namespace not found"

**Causa**: Hai applicato deployments prima di creare il namespace

**Fix**:
```bash
# Crea namespace prima
kubectl apply -f namespaces.yaml

# Poi riprova con deployment
kubectl apply -f deployments.yaml
```

### Errore: "PersistentVolumeClaim not found"

**Causa**: PVC non esistono ancora

**Fix**:
```bash
# Applica PV e PVC prima
kubectl apply -f persistentvolumes.yaml
kubectl apply -f persistentvolumeclaims.yaml

# Aspetta che siano Bound
kubectl get pvc -A --watch

# Poi applica StatefulSets/Deployments
kubectl apply -f deployments.yaml
```

### Errore: "Secret not found"

**Causa**: Deployment riferisce un secret che non esiste

**Fix**:
```bash
# Applica secrets prima
kubectl apply -f secrets.yaml

# Restart deployment per leggere secret
kubectl rollout restart deployment/insightlearn-api -n insightlearn
```

### Pod in CrashLoopBackOff dopo restore

**Causa**: Dipendenza non pronta (database, secret, PVC)

**Fix**:
```bash
# 1. Verifica cosa manca
kubectl describe pod <pod-name> -n insightlearn

# 2. Controlla logs
kubectl logs <pod-name> -n insightlearn --previous

# 3. Verifica dependencies
kubectl get secrets,configmaps,pvc -n insightlearn

# 4. Se manca qualcosa, applica dal backup
kubectl apply -f /tmp/k3s-restore/.../resources/<resource-type>.yaml
```

---

## 📊 Verifica Ripristino

### Health Check Completo

```bash
# 1. Tutti i pod running?
kubectl get pods --all-namespaces | grep -v Running

# Se vuoto → tutto OK, altrimenti vedi pod problematici

# 2. Deployments ready?
kubectl get deployments --all-namespaces

# 3. Services funzionanti?
kubectl get svc --all-namespaces

# 4. PVC bound?
kubectl get pvc --all-namespaces

# 5. Test endpoint API
curl http://localhost:31081/api/info
```

### Check Specifici per InsightLearn

```bash
# API funzionante?
curl http://localhost:31081/health
# Expected: {"status": "Healthy"}

# MongoDB OK?
kubectl exec -n insightlearn mongodb-0 -- mongosh \
  -u admin -p "$MONGO_PASSWORD" --eval "db.version()"

# Redis OK?
kubectl exec -n insightlearn redis-0 -- redis-cli ping
# Expected: PONG

# SQL Server OK?
kubectl exec -n insightlearn <sqlserver-pod> -- \
  /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa \
  -P "$MSSQL_PASSWORD" -Q "SELECT @@VERSION" -C
```

---

## 🔄 Rollback (Torna a Stato Precedente)

Se il ripristino ha causato problemi:

### Opzione A: Usa Backup Precedente

```bash
# Lista backup disponibili
ls -lht /var/backups/k3s-cluster/*.tar.gz

# Usa il backup precedente
sudo tar -xzf /var/backups/k3s-cluster/k3s-cluster-backup-1.tar.gz \
  -C /tmp/k3s-rollback

# Applica risorse dal backup vecchio
cd /tmp/k3s-rollback/k3s-backup-*/resources/
kubectl apply -f .
```

### Opzione B: Delete e Restart

```bash
# Delete deployment corrotto
kubectl delete deployment insightlearn-api -n insightlearn

# Applica da backup
kubectl apply -f /tmp/k3s-restore/.../resources/deployments.yaml
```

---

## 📁 Struttura Backup

Quando estrai un backup, trovi questa struttura:

```
k3s-backup-20251109-170421/
├── etcd/
│   └── snapshot-20251109-170421.zip      # ETCD database snapshot
├── resources/
│   ├── namespaces.yaml                   # Tutti i namespaces
│   ├── deployments.yaml                  # Tutti i deployments
│   ├── statefulsets.yaml                 # StatefulSets (MongoDB, Redis, etc.)
│   ├── services.yaml                     # Services
│   ├── secrets.yaml                      # Secrets (passwords, certs)
│   ├── configmaps.yaml                   # ConfigMaps
│   ├── persistentvolumes.yaml            # PVs
│   ├── persistentvolumeclaims.yaml       # PVCs
│   ├── ingresses.yaml                    # Ingress routes
│   ├── roles.yaml                        # RBAC roles
│   ├── rolebindings.yaml                 # RBAC bindings
│   └── customresourcedefinitions.yaml    # CRDs (se presenti)
├── k3s-config/
│   ├── k3s.yaml                          # Kubeconfig
│   └── manifests/                        # K3s auto-deploy manifests
└── backup-metadata.txt                   # Info backup (version, timestamp, etc.)
```

---

## 🎯 Quick Reference Card

**Ripristino Rapido Singolo Pod**:

```bash
# 1. Estrai backup
sudo tar -xzf /var/backups/k3s-cluster/latest-backup.tar.gz -C /tmp

# 2. Trova deployment
cd /tmp/k3s-backup-*/resources/

# 3. Applica
kubectl apply -f deployments.yaml --namespace=insightlearn

# 4. Verifica
kubectl get pods -n insightlearn --watch
```

**Ripristino Completo**:

```bash
sudo /home/mpasqui/insightlearn_WASM/InsightLearn_WASM/k8s/restore-cluster-state.sh
```

---

## 📞 Supporto

**Script Restore**: [k8s/restore-cluster-state.sh](../k8s/restore-cluster-state.sh)
**Backup Script**: [k8s/backup-cluster-state.sh](../k8s/backup-cluster-state.sh)
**Docs Completa**: [docs/DISASTER-RECOVERY-SYSTEM.md](DISASTER-RECOVERY-SYSTEM.md)

**Maintainer**: InsightLearn DevOps Team
**Contact**: marcello.pasqui@gmail.com
**Version**: 1.0.0
**Date**: 2025-11-09

---

## ✅ Checklist Ripristino

Prima di eseguire un ripristino, verifica:

- [ ] Hai fatto un backup recente?
- [ ] Sai quale backup vuoi usare?
- [ ] Hai verificato il contenuto del backup?
- [ ] Conosci lo stato attuale del cluster?
- [ ] Hai un piano di rollback se qualcosa va storto?
- [ ] Hai documentato cosa stai per fare?
- [ ] Hai notificato il team (se in produzione)?

**Solo dopo questi check** → procedi con il ripristino.

---

**🎉 Sistema di ripristino pronto per l'uso!**
