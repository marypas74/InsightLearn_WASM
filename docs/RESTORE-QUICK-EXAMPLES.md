# 🚀 Ripristino Pod - Esempi Rapidi

**Quick Reference** per operazioni di ripristino più comuni

---

## 🎯 Ripristini Più Comuni

### 1. Ripristina InsightLearn API

```bash
# Metodo A: Script automatico (raccomandato)
sudo /home/mpasqui/insightlearn_WASM/InsightLearn_WASM/k8s/restore-single-resource.sh \
  --resource deployment \
  --name insightlearn-api \
  --namespace insightlearn

# Metodo B: Manuale
sudo tar -xzf /var/backups/k3s-cluster/latest-backup.tar.gz -C /tmp
kubectl apply -f /tmp/k3s-backup-*/resources/deployments.yaml \
  --namespace=insightlearn \
  --selector=app=insightlearn-api
```

### 2. Ripristina MongoDB StatefulSet

```bash
# Script automatico
sudo /home/mpasqui/insightlearn_WASM/InsightLearn_WASM/k8s/restore-single-resource.sh \
  --resource statefulset \
  --name mongodb \
  --namespace insightlearn

# Verifica
kubectl get pods -n insightlearn | grep mongodb
kubectl exec -n insightlearn mongodb-0 -- mongosh --eval "db.version()"
```

### 3. Ripristina Redis

```bash
# Script automatico
sudo /home/mpasqui/insightlearn_WASM/InsightLearn_WASM/k8s/restore-single-resource.sh \
  --resource statefulset \
  --name redis \
  --namespace insightlearn

# Test
kubectl exec -n insightlearn redis-0 -- redis-cli ping
```

### 4. Ripristina SQL Server

```bash
# Deployment
sudo /home/mpasqui/insightlearn_WASM/InsightLearn_WASM/k8s/restore-single-resource.sh \
  --resource deployment \
  --name sqlserver \
  --namespace insightlearn

# Verifica connessione
kubectl get pods -n insightlearn | grep sqlserver
kubectl logs -n insightlearn deployment/sqlserver --tail=20
```

### 5. Ripristina Secret Cancellato

```bash
# Esempio: secret SQL Server password
sudo tar -xzf /var/backups/k3s-cluster/latest-backup.tar.gz -C /tmp

kubectl apply -f - <<EOF
$(grep -A 20 "name: mssql-sa-password" /tmp/k3s-backup-*/resources/secrets.yaml)
EOF

# Verifica
kubectl get secret -n insightlearn mssql-sa-password
```

### 6. Ripristina Service (NodePort)

```bash
# Script automatico
sudo /home/mpasqui/insightlearn_WASM/InsightLearn_WASM/k8s/restore-single-resource.sh \
  --resource service \
  --name sqlserver-service-nodeport \
  --namespace insightlearn

# Verifica
kubectl get svc -n insightlearn sqlserver-service-nodeport
```

### 7. Ripristina ConfigMap

```bash
# Esempio: Prometheus config
sudo tar -xzf /var/backups/k3s-cluster/latest-backup.tar.gz -C /tmp

kubectl apply -f - <<EOF
$(sed -n '/name: prometheus-config/,/^---/p' /tmp/k3s-backup-*/resources/configmaps.yaml)
EOF

# Restart Prometheus per leggere nuova config
kubectl rollout restart deployment/prometheus -n insightlearn
```

### 8. Ripristina Ingress

```bash
# Script automatico
sudo /home/mpasqui/insightlearn_WASM/InsightLearn_WASM/k8s/restore-single-resource.sh \
  --resource ingress \
  --name insightlearn-ingress \
  --namespace insightlearn

# Verifica routes
kubectl get ingress -n insightlearn
kubectl describe ingress insightlearn-ingress -n insightlearn
```

---

## 🔥 Scenari Emergenza

### Pod Crashato - Ripristino Immediato

```bash
# 1. Identifica pod crashato
kubectl get pods -n insightlearn | grep -v Running

# 2. Ripristina deployment del pod
POD_NAME="insightlearn-api-xxxxx"
DEPLOYMENT=$(kubectl get pod $POD_NAME -n insightlearn -o jsonpath='{.metadata.labels.app}')

sudo /home/mpasqui/insightlearn_WASM/InsightLearn_WASM/k8s/restore-single-resource.sh \
  --resource deployment \
  --name $DEPLOYMENT \
  --namespace insightlearn

# 3. Forza ricreazione pod
kubectl rollout restart deployment/$DEPLOYMENT -n insightlearn
```

### Tutti i Pod del Namespace Crashati

```bash
# Ripristino completo namespace insightlearn
sudo tar -xzf /var/backups/k3s-cluster/latest-backup.tar.gz -C /tmp
cd /tmp/k3s-backup-*/resources/

# Applica tutto in ordine
kubectl apply -f secrets.yaml --namespace=insightlearn
kubectl apply -f configmaps.yaml --namespace=insightlearn
kubectl apply -f persistentvolumeclaims.yaml --namespace=insightlearn
kubectl apply -f statefulsets.yaml --namespace=insightlearn
kubectl apply -f deployments.yaml --namespace=insightlearn
kubectl apply -f services.yaml --namespace=insightlearn

# Monitoraggio
kubectl get pods -n insightlearn --watch
```

### Secret Cancellato per Errore

```bash
# Recupera TUTTI i secrets del namespace
sudo tar -xzf /var/backups/k3s-cluster/latest-backup.tar.gz -C /tmp

kubectl apply -f /tmp/k3s-backup-*/resources/secrets.yaml \
  --namespace=insightlearn

# Restart tutti i pod per leggere nuovi secrets
kubectl rollout restart deployment --namespace=insightlearn
kubectl rollout restart statefulset --namespace=insightlearn
```

---

## 📊 Verifica Post-Ripristino

### Check Rapido

```bash
# Uno-liner per verificare salute cluster
kubectl get pods -A | grep -v Running | grep -v Completed || echo "✓ All pods running"
```

### Check Dettagliato

```bash
# Status deployments
kubectl get deployments -n insightlearn

# Status statefulsets
kubectl get statefulsets -n insightlearn

# PVC bound?
kubectl get pvc -n insightlearn

# Services OK?
kubectl get svc -n insightlearn

# Endpoints funzionanti?
kubectl get endpoints -n insightlearn
```

### Test Funzionalità

```bash
# API Health
curl http://localhost:31081/health

# Database checks
kubectl exec -n insightlearn mongodb-0 -- mongosh --eval "db.version()"
kubectl exec -n insightlearn redis-0 -- redis-cli ping

# Test connessione SQL Server da fuori
sqlcmd -S 192.168.1.114,31433 -U sa -P "dScIG9pSAbO5OAZka03nz0m79cT0p9OS" -Q "SELECT @@VERSION"
```

---

## 🛠️ Comandi Utili

### Lista Backup Disponibili

```bash
ls -lht /var/backups/k3s-cluster/*.tar.gz
```

### Vedi Contenuto Backup

```bash
# Estrai in temp
sudo tar -xzf /var/backups/k3s-cluster/latest-backup.tar.gz -C /tmp

# Lista risorse disponibili
ls -lh /tmp/k3s-backup-*/resources/

# Conta risorse per tipo
for f in /tmp/k3s-backup-*/resources/*.yaml; do
  echo "$(basename $f): $(grep -c '^kind: ' $f 2>/dev/null || echo 0) resources"
done
```

### Confronta Backup vs Stato Attuale

```bash
# Estrai backup
sudo tar -xzf /var/backups/k3s-cluster/latest-backup.tar.gz -C /tmp

# Deployments in backup
kubectl get -f /tmp/k3s-backup-*/resources/deployments.yaml \
  --all-namespaces -o custom-columns=NAME:.metadata.name --no-headers

# Deployments attuali
kubectl get deployments --all-namespaces -o custom-columns=NAME:.metadata.name --no-headers

# Diff (cosa manca?)
diff <(kubectl get -f /tmp/k3s-backup-*/resources/deployments.yaml --all-namespaces -o custom-columns=NAME:.metadata.name --no-headers | sort) \
     <(kubectl get deployments --all-namespaces -o custom-columns=NAME:.metadata.name --no-headers | sort)
```

---

## 📞 Help

**Documentazione completa**: [RESTORE-PODS-FROM-BACKUP.md](RESTORE-PODS-FROM-BACKUP.md)

**Script disponibili**:
- `k8s/restore-cluster-state.sh` - Ripristino completo automatico
- `k8s/restore-single-resource.sh` - Ripristino singola risorsa interattivo
- `k8s/backup-cluster-state.sh` - Backup manuale

**Logs**:
- Backup: `/var/log/k3s-backup.log`
- Restore: `/var/log/k3s-restore.log`

**Troubleshooting**:
```bash
# Check backup cron
crontab -l | grep backup

# Check restore service
sudo systemctl status k3s-auto-restore.service

# Logs dettagliati
tail -f /var/log/k3s-backup.log
tail -f /var/log/k3s-restore.log
```

---

**Last Updated**: 2025-11-09
**Maintainer**: marcello.pasqui@gmail.com
