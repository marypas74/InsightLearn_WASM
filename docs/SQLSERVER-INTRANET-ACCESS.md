# 🏢 Accesso SQL Server dalla Intranet (LAN)

**Data**: 2025-11-09
**Configurazione**: NodePort (sempre attivo)
**Status**: ✅ **OPERATIVO**

---

## 📊 Configurazione Attuale

**Server IP (Intranet)**: `192.168.1.114`
**Porta SQL Server**: `31433` (NodePort)
**Username**: `sa`
**Password**: `dScIG9pSAbO5OAZka03nz0m79cT0p9OS`
**Database**: `InsightLearn_DB`

⚠️ **Porta NodePort 31433** - NON è la porta standard 1433 di SQL Server!

---

## 🚀 Connessione da SSMS (Intranet)

### Da Questo Server (192.168.1.114)

```
Server name:    localhost,31433
Authentication: SQL Server Authentication
Login:          sa
Password:       dScIG9pSAbO5OAZka03nz0m79cT0p9OS
Encryption:     Optional (oppure spunta "Trust server certificate")
```

### Da Altri PC nella Intranet

```
Server name:    192.168.1.114,31433
Authentication: SQL Server Authentication
Login:          sa
Password:       dScIG9pSAbO5OAZka03nz0m79cT0p9OS
Encryption:     Optional (oppure spunta "Trust server certificate")
```

**⚠️ IMPORTANTE**:
- Usa la **virgola** prima della porta: `192.168.1.114,31433`
- Porta **31433** (non 1433)
- Tutti i PC nella tua rete locale possono connettersi

---

## ✅ Vantaggi NodePort per Intranet

| Feature | Status |
|---------|--------|
| **Sempre disponibile** | ✅ Nessuno script da avviare |
| **Accesso da tutti i PC LAN** | ✅ Via IP intranet |
| **Sopravvive ai riavvii** | ✅ Configurazione persistente |
| **NON esposto su Internet** | ✅ Solo rete locale |
| **Porta fissa** | ✅ 31433 (mai cambia) |

---

## 🔧 Test Connessione

### Da Questo Server

```bash
# Test con sqlcmd
sqlcmd -S localhost,31433 -U sa -P "dScIG9pSAbO5OAZka03nz0m79cT0p9OS" -Q "SELECT @@VERSION"
```

### Da Altri PC (Windows)

```powershell
# Test con PowerShell
Test-NetConnection -ComputerName 192.168.1.114 -Port 31433
```

### Output Atteso

```
ComputerName     : 192.168.1.114
RemoteAddress    : 192.168.1.114
RemotePort       : 31433
InterfaceAlias   : Ethernet
SourceAddress    : 192.168.1.XXX
TcpTestSucceeded : True
```

---

## 📱 Accesso da Dispositivi Mobili (Intranet)

Se hai app mobile connesse alla stessa rete Wi-Fi:

**Connection String**:
```
Server=192.168.1.114,31433;Database=InsightLearn_DB;User Id=sa;Password=dScIG9pSAbO5OAZka03nz0m79cT0p9OS;TrustServerCertificate=True;
```

---

## 🖥️ Configurazione Multi-PC

Se hai più sviluppatori che accedono al database:

### PC 1 (Server Kubernetes)
```
Hostname: rocky-k8s-server (192.168.1.114)
Connection: localhost,31433
```

### PC 2-N (Altri PC Sviluppo)
```
Connection: 192.168.1.114,31433
```

---

## 🔐 Sicurezza Intranet

### Firewall Rules (già configurato)

NodePort usa porte 30000-32767 che sono già aperte per Kubernetes.

### Verifica Firewall

```bash
# Controlla che porta 31433 sia accessibile
sudo firewall-cmd --list-all | grep 31433

# Se necessario, aggiungi regola (di solito non serve)
sudo firewall-cmd --permanent --add-port=31433/tcp
sudo firewall-cmd --reload
```

### IP Whitelisting (Opzionale)

Se vuoi limitare l'accesso solo a certi IP della intranet:

```bash
# Esempio: solo PC 192.168.1.10-20
sudo firewall-cmd --permanent --zone=internal --add-source=192.168.1.10/32
sudo firewall-cmd --permanent --zone=internal --add-source=192.168.1.20/32
sudo firewall-cmd --permanent --zone=internal --add-port=31433/tcp
sudo firewall-cmd --reload
```

---

## 📊 Connection Strings per Applicazioni

### .NET (Entity Framework)

```csharp
"Server=192.168.1.114,31433;Database=InsightLearn_DB;User Id=sa;Password=dScIG9pSAbO5OAZka03nz0m79cT0p9OS;TrustServerCertificate=True;MultipleActiveResultSets=True;"
```

### Python (pyodbc)

```python
import pyodbc

conn = pyodbc.connect(
    'DRIVER={ODBC Driver 17 for SQL Server};'
    'SERVER=192.168.1.114,31433;'
    'DATABASE=InsightLearn_DB;'
    'UID=sa;'
    'PWD=dScIG9pSAbO5OAZka03nz0m79cT0p9OS;'
    'TrustServerCertificate=yes;'
)
```

### Node.js (tedious)

```javascript
const config = {
  server: '192.168.1.114',
  options: {
    port: 31433,
    database: 'InsightLearn_DB',
    encrypt: false,
    trustServerCertificate: true
  },
  authentication: {
    type: 'default',
    options: {
      userName: 'sa',
      password: 'dScIG9pSAbO5OAZka03nz0m79cT0p9OS'
    }
  }
};
```

---

## 🆘 Troubleshooting

### Errore: "Network error" o "Connection timeout"

**Causa 1**: Firewall blocca porta
```bash
# Verifica firewall
sudo firewall-cmd --list-ports

# Aggiungi porta se manca
sudo firewall-cmd --permanent --add-port=31433/tcp
sudo firewall-cmd --reload
```

**Causa 2**: IP server cambiato
```bash
# Verifica IP attuale
ip addr show | grep "inet " | grep -v "127.0.0.1"
```

**Causa 3**: NodePort service non attivo
```bash
# Verifica servizio
kubectl get svc -n insightlearn sqlserver-service-nodeport

# Se manca, riapplica
kubectl apply -f /home/mpasqui/insightlearn_WASM/InsightLearn_WASM/k8s/06-sqlserver-service-nodeport.yaml
```

### Errore: "Login failed for user 'sa'"

Password errata. Recupera password corretta:
```bash
kubectl get secret -n insightlearn insightlearn-secrets \
  -o jsonpath='{.data.mssql-sa-password}' | base64 -d
```

### SQL Server pod non risponde

```bash
# Verifica pod status
kubectl get pods -n insightlearn | grep sqlserver

# Se non è Running
kubectl describe pod -n insightlearn <sqlserver-pod-name>

# Restart se necessario
kubectl rollout restart deployment/sqlserver -n insightlearn
```

---

## 🔄 Manutenzione

### Verifica Status Servizio

```bash
# Ogni giorno/settimana
kubectl get svc -n insightlearn sqlserver-service-nodeport
```

**Output atteso**:
```
NAME                         TYPE       CLUSTER-IP    EXTERNAL-IP   PORT(S)          AGE
sqlserver-service-nodeport   NodePort   10.43.7.201   <none>        1433:31433/TCP   1d
```

### Rimuovere NodePort (se necessario)

```bash
# Solo se vuoi tornare a port-forward
kubectl delete svc -n insightlearn sqlserver-service-nodeport
```

---

## 📋 Configurazione Kubernetes

**File Manifest**: [k8s/06-sqlserver-service-nodeport.yaml](../k8s/06-sqlserver-service-nodeport.yaml)

**Contenuto**:
```yaml
apiVersion: v1
kind: Service
metadata:
  name: sqlserver-service-nodeport
  namespace: insightlearn
spec:
  type: NodePort
  selector:
    app: sqlserver
  ports:
    - port: 1433
      targetPort: 1433
      nodePort: 31433  # Porta esterna accessibile
      protocol: TCP
```

**Riapplica dopo modifiche**:
```bash
kubectl apply -f k8s/06-sqlserver-service-nodeport.yaml
```

---

## 📊 Architettura Rete

```
┌─────────────────────────────────────────────────────────────┐
│                    Intranet LAN (192.168.1.x)               │
│                                                             │
│  ┌────────────┐    ┌────────────┐    ┌────────────┐       │
│  │  PC Dev 1  │    │  PC Dev 2  │    │  PC Dev N  │       │
│  │            │    │            │    │            │       │
│  │   SSMS     │    │   SSMS     │    │  VS Code   │       │
│  └─────┬──────┘    └─────┬──────┘    └─────┬──────┘       │
│        │                 │                  │               │
│        └─────────────────┴──────────────────┘               │
│                          │                                  │
│                    Port 31433                               │
│                          │                                  │
│        ┌─────────────────▼─────────────────┐               │
│        │   K8s Node (192.168.1.114)        │               │
│        │                                    │               │
│        │  ┌──────────────────────────────┐ │               │
│        │  │   NodePort Service           │ │               │
│        │  │   (31433 → 1433)             │ │               │
│        │  └──────────────┬───────────────┘ │               │
│        │                 │                  │               │
│        │  ┌──────────────▼───────────────┐ │               │
│        │  │   SQL Server Pod             │ │               │
│        │  │   (Port 1433)                │ │               │
│        │  └──────────────────────────────┘ │               │
│        └───────────────────────────────────┘               │
│                                                             │
└─────────────────────────────────────────────────────────────┘

Internet ❌ (NON accessibile da fuori)
```

---

## ✅ Quick Reference Card

**Per stampare e tenere vicino alla scrivania**:

```
═══════════════════════════════════════════════════════════
   SQL SERVER INSIGHTLEARN - ACCESSO INTRANET
═══════════════════════════════════════════════════════════

   SERVER:     192.168.1.114,31433
   LOGIN:      sa
   PASSWORD:   dScIG9pSAbO5OAZka03nz0m79cT0p9OS
   DATABASE:   InsightLearn_DB

   ⚠️  PORTA 31433 (non 1433)
   ⚠️  Usa VIRGOLA prima della porta

═══════════════════════════════════════════════════════════
   SSMS: Seleziona "Trust server certificate"
═══════════════════════════════════════════════════════════
```

---

## 📞 Supporto

**Maintainer**: InsightLearn DevOps Team
**Contact**: marcello.pasqui@gmail.com
**Repository**: https://github.com/marypas74/InsightLearn_WASM

**Service Status**: ✅ **ALWAYS ON** (NodePort)
**Last Updated**: 2025-11-09
**Version**: 1.0.0

---

## 🎯 Conclusione

✅ **SQL Server ora sempre accessibile dalla intranet**
✅ **Nessuno script da avviare**
✅ **Tutti i PC della LAN possono connettersi**
✅ **NON esposto su Internet** (sicuro)
✅ **Configurazione persistente** (sopravvive ai riavvii)

**Pronto per l'uso!** 🚀
