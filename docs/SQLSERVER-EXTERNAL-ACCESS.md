# 🗄️ Accesso Esterno a SQL Server su Kubernetes

**Data**: 2025-11-09
**Versione**: 1.0.0
**Status**: ✅ Configurato

---

## 📊 Overview

Guida completa per accedere al database SQL Server nel cluster Kubernetes da **SQL Server Management Studio (SSMS)** o altri client esterni.

**Cluster**: K3s su Rocky Linux 10
**Namespace**: insightlearn
**Service**: sqlserver-service
**Database**: InsightLearn_DB

---

## 🔐 Credenziali

**Username**: `sa`
**Password**: `dScIG9pSAbO5OAZka03nz0m79cT0p9OS`

⚠️ **Password memorizzata nel Secret**: `insightlearn-secrets.mssql-sa-password` (namespace: insightlearn)

---

## 🚀 Opzione 1: Port-Forward Temporaneo

**Quando usare**: Test rapidi, connessioni occasionali
**Vantaggi**: Veloce, nessuna configurazione
**Svantaggi**: Si disconnette se chiudi il terminale

### Comandi

```bash
# Avvia port-forward
kubectl port-forward -n insightlearn service/sqlserver-service 1433:1433
```

### Connessione SSMS

```
Server name: localhost,1433
Authentication: SQL Server Authentication
Login: sa
Password: dScIG9pSAbO5OAZka03nz0m79cT0p9OS
Encryption: Optional (o Trust server certificate)
```

**⚠️ Nota**: Lascia il terminale aperto mentre usi SSMS. Se chiudi il terminale, la connessione si interrompe.

---

## ⭐ Opzione 2: Port-Forward Persistente (Raccomandato)

**Quando usare**: Sviluppo quotidiano, accesso frequente
**Vantaggi**: Auto-restart, esecuzione background, logging
**Svantaggi**: Richiede script in esecuzione

### Script Disponibile

```bash
/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/k8s/sqlserver-port-forward-persistent.sh
```

### Avvio

```bash
# Avvia in background
cd /home/mpasqui/insightlearn_WASM/InsightLearn_WASM/k8s
./sqlserver-port-forward-persistent.sh &

# Verifica che sia partito
ps aux | grep sqlserver-port-forward

# Controlla log
tail -f /tmp/sqlserver-port-forward.log
```

### Output Atteso

```
[2025-11-09 17:30:00] ==========================================
[2025-11-09 17:30:00] SQL Server Port-Forward Starting
[2025-11-09 17:30:00] ==========================================
[2025-11-09 17:30:00] Service: sqlserver-service
[2025-11-09 17:30:00] Namespace: insightlearn
[2025-11-09 17:30:00] Local port: 1433
[2025-11-09 17:30:00] Remote port: 1433
[2025-11-09 17:30:00] Log file: /tmp/sqlserver-port-forward.log
[2025-11-09 17:30:00] PID: 12345
[2025-11-09 17:30:01] Starting port-forward (attempt 1)...
Forwarding from 127.0.0.1:1433 -> 1433
Forwarding from [::1]:1433 -> 1433
```

### Connessione SSMS

Stesso setup dell'Opzione 1:
```
Server name: localhost,1433
Authentication: SQL Server Authentication
Login: sa
Password: dScIG9pSAbO5OAZka03nz0m79cT0p9OS
```

### Stop Script

```bash
# Ferma il port-forward
pkill -f "sqlserver-port-forward-persistent"

# Verifica che sia terminato
ps aux | grep sqlserver-port-forward
```

### Features Script

- ✅ **Auto-restart**: Se la connessione cade, riavvia automaticamente
- ✅ **Background**: Esegue in background, non blocca il terminale
- ✅ **Logging**: Tutti gli eventi loggati in `/tmp/sqlserver-port-forward.log`
- ✅ **PID tracking**: File PID in `/tmp/sqlserver-port-forward.pid`
- ✅ **Graceful shutdown**: Cleanup automatico all'uscita

---

## 🌐 Opzione 3: NodePort (Accesso Permanente)

**Quando usare**: Accesso permanente senza script
**Vantaggi**: Sempre accessibile, nessuno script richiesto
**Svantaggi**: Espone porta sul nodo (meno sicuro)

### Deploy NodePort Service

```bash
# Applica configurazione
kubectl apply -f /home/mpasqui/insightlearn_WASM/InsightLearn_WASM/k8s/06-sqlserver-service-nodeport.yaml

# Verifica servizio
kubectl get svc -n insightlearn sqlserver-service-nodeport
```

### Output Atteso

```
NAME                          TYPE       CLUSTER-IP      EXTERNAL-IP   PORT(S)          AGE
sqlserver-service-nodeport    NodePort   10.43.123.45    <none>        1433:31433/TCP   5s
```

### Connessione SSMS

```
Server name: localhost,31433
Authentication: SQL Server Authentication
Login: sa
Password: dScIG9pSAbO5OAZka03nz0m79cT0p9OS
```

**⚠️ Nota**: La porta cambia da `1433` a `31433` (NodePort)

### Rimuovere NodePort (tornare a ClusterIP)

```bash
kubectl delete svc -n insightlearn sqlserver-service-nodeport
```

---

## 🔧 Connessione da Altri Tools

### Azure Data Studio

```
Connection type: Microsoft SQL Server
Server: localhost,1433  (o localhost,31433 per NodePort)
Authentication type: SQL Login
User name: sa
Password: dScIG9pSAbO5OAZka03nz0m79cT0p9OS
Encrypt: Optional
Trust server certificate: Yes
```

### Command Line (sqlcmd)

```bash
# Via port-forward (porta 1433)
sqlcmd -S localhost,1433 -U sa -P "dScIG9pSAbO5OAZka03nz0m79cT0p9OS" -Q "SELECT @@VERSION"

# Via NodePort (porta 31433)
sqlcmd -S localhost,31433 -U sa -P "dScIG9pSAbO5OAZka03nz0m79cT0p9OS" -Q "SELECT @@VERSION"
```

### Connection String (.NET)

```csharp
// Via port-forward
"Server=localhost,1433;Database=InsightLearn_DB;User Id=sa;Password=dScIG9pSAbO5OAZka03nz0m79cT0p9OS;TrustServerCertificate=True;"

// Via NodePort
"Server=localhost,31433;Database=InsightLearn_DB;User Id=sa;Password=dScIG9pSAbO5OAZka03nz0m79cT0p9OS;TrustServerCertificate=True;"
```

---

## 🆘 Troubleshooting

### Errore: "Cannot connect to localhost,1433"

**Causa 1**: Port-forward non attivo
```bash
# Verifica se è in esecuzione
ps aux | grep "kubectl port-forward"

# Se non c'è, avvialo
kubectl port-forward -n insightlearn service/sqlserver-service 1433:1433
```

**Causa 2**: Porta 1433 già in uso
```bash
# Verifica cosa usa la porta
netstat -tulpn | grep 1433

# Usa porta alternativa
kubectl port-forward -n insightlearn service/sqlserver-service 14330:1433
# Connetti a: localhost,14330
```

### Errore: "Login failed for user 'sa'"

**Causa**: Password errata
```bash
# Recupera password corretta dal Secret
kubectl get secret -n insightlearn insightlearn-secrets \
  -o jsonpath='{.data.mssql-sa-password}' | base64 -d
```

### Errore: "Connection timeout"

**Causa**: SQL Server pod non running
```bash
# Verifica pod
kubectl get pods -n insightlearn | grep sqlserver

# Se non è Running, verifica logs
kubectl logs -n insightlearn <sqlserver-pod-name>
```

### Port-forward si disconnette continuamente

**Soluzione**: Usa lo script persistente (Opzione 2)
```bash
./k8s/sqlserver-port-forward-persistent.sh &
```

---

## 📊 Status Attuale

**Service Type**: ClusterIP (Headless)
**Porta interna**: 1433
**Accesso esterno**: Port-forward o NodePort
**Pod status**: ✅ Running

```bash
# Verifica status completo
kubectl get all -n insightlearn | grep sqlserver
```

---

## 🔒 Sicurezza

### Best Practices

1. **Port-forward raccomandato** per sviluppo (più sicuro di NodePort)
2. **Non esporre SQL Server direttamente su Internet**
3. **Cambia password SA** in produzione (usa Secret Kubernetes)
4. **Usa certificati SSL/TLS** per connessioni crittografate
5. **Limita IP source** se usi NodePort (firewall rules)

### Cambiare Password SA

```bash
# 1. Genera nuova password
NEW_PASSWORD=$(openssl rand -base64 32)

# 2. Aggiorna Secret
kubectl patch secret -n insightlearn insightlearn-secrets \
  --type='json' \
  -p="[{'op': 'replace', 'path': '/data/mssql-sa-password', 'value': '$(echo -n "$NEW_PASSWORD" | base64)'}]"

# 3. Restart SQL Server pod
kubectl rollout restart deployment/sqlserver -n insightlearn
```

---

## 📚 File Correlati

- **Script port-forward**: [k8s/sqlserver-port-forward-persistent.sh](../k8s/sqlserver-port-forward-persistent.sh)
- **NodePort manifest**: [k8s/06-sqlserver-service-nodeport.yaml](../k8s/06-sqlserver-service-nodeport.yaml)
- **Service attuale**: [k8s/03-sqlserver-service.yaml](../k8s/03-sqlserver-service.yaml)
- **Deployment**: [k8s/03-sqlserver-deployment.yaml](../k8s/03-sqlserver-deployment.yaml)

---

## ✅ Quick Start (TL;DR)

**Metodo più veloce per sviluppo**:

```bash
# 1. Avvia port-forward persistente
cd /home/mpasqui/insightlearn_WASM/InsightLearn_WASM/k8s
./sqlserver-port-forward-persistent.sh &

# 2. Connetti da SSMS
#    Server: localhost,1433
#    Login: sa
#    Password: dScIG9pSAbO5OAZka03nz0m79cT0p9OS

# 3. Stop quando finisci
pkill -f "sqlserver-port-forward-persistent"
```

---

**Maintainer**: InsightLearn DevOps Team
**Contact**: marcello.pasqui@gmail.com
**Repository**: https://github.com/marypas74/InsightLearn_WASM
**Version**: 1.0.0
**Date**: 2025-11-09
