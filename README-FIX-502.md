# 🔧 Fix 502 Bad Gateway - Istruzioni

## ❌ Problema Attuale

Il backend API mostra **502 Bad Gateway** perché i pod Kubernetes stanno usando un'**immagine vecchia** che:
- ❌ NON ha gli endpoint di autenticazione (`/api/auth/login`, `/api/auth/register`, ecc.)
- ❌ NON crea l'utente admin automaticamente al startup
- ❌ La tabella `Users` nel database è vuota (0 utenti)

## ✅ Soluzione Preparata

Ho già:
1. ✅ Aggiunto 5 endpoint di autenticazione in `Program.cs`
2. ✅ Aggiunto seed automatico per creare l'utente admin
3. ✅ Compilato e creato l'immagine Docker nuova
4. ✅ Salvato l'immagine in `/tmp/insightlearn-api.tar`

**Manca solo**: Importare l'immagine in K3s e riavviare i pod

## 🚀 Esegui Questi 2 Comandi

### PASSO 1: Import Immagine (richiede sudo)

```bash
sudo ./ESEGUI-QUESTO-COMANDO.sh
```

Questo script:
- Importa `/tmp/insightlearn-api.tar` in K3s containerd
- Verifica che l'import sia andato a buon fine

### PASSO 2: Riavvia Pod API (utente normale)

```bash
./PASSO-2-riavvia-pod.sh
```

Questo script:
- Elimina i vecchi pod API
- Attende che i nuovi pod siano pronti
- Mostra i log di creazione dell'utente admin
- Fornisce le credenziali di accesso

## 🔐 Credenziali Admin

Dopo il riavvio dei pod, potrai fare login con:

- **Email**: `admin@insightlearn.cloud`
- **Password**: `Admin@InsightLearn2025!`

## 📋 Verifica Manuale

Se vuoi verificare manualmente che tutto funzioni:

```bash
# 1. Controlla che i pod siano pronti
kubectl get pods -n insightlearn -l app=insightlearn-api

# 2. Verifica i log di creazione utente admin
kubectl logs -n insightlearn -l app=insightlearn-api --tail=100 | grep SEED

# 3. Testa l'endpoint di login
kubectl port-forward -n insightlearn svc/api-service 8081:80 &
curl -X POST http://localhost:8081/api/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"email":"admin@insightlearn.cloud","password":"Admin@InsightLearn2025!"}'
```

Dovresti vedere una risposta JSON con un token JWT.

## 🌐 Test Login Web

1. Vai a: https://wasm.insightlearn.cloud/login
2. Inserisci:
   - Email: `admin@insightlearn.cloud`
   - Password: `Admin@InsightLearn2025!`
3. Premi "Login"

Dovresti essere autenticato e reindirizzato alla dashboard.

## ⚠️ Troubleshooting

### Se continua a dare 502:

```bash
# Verifica che i pod siano realmente riavviati
kubectl get pods -n insightlearn -l app=insightlearn-api -o jsonpath='{.items[0].status.startTime}'

# Dovrebbe mostrare un timestamp RECENTE (pochi minuti fa)
```

### Se l'utente admin non viene creato:

```bash
# Controlla i log completi
kubectl logs -n insightlearn -l app=insightlearn-api --tail=200

# Cerca errori come:
# - [SEED] ⚠ Failed to create admin user
# - [DATABASE] ⚠ Warning: Could not connect
```

### Se serve ricreare l'utente admin manualmente:

```sql
-- Connettiti al database SQL Server
kubectl exec -it -n insightlearn statefulset/sqlserver -- /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'InsightLearn123@#' -C

-- Verifica che l'utente esista
USE InsightLearnDb;
SELECT Email, UserName FROM Users;
GO
```

---

**Creato da**: Claude (Backend Architect)
**Data**: 2025-11-09 00:00
**Versione API**: 1.6.0-dev
