# 🖥️ GUI Ripristino Pod - Guida Utente

**Data**: 2025-11-09
**Versione**: 1.0.0
**Sistema**: K3s Pod Restore Web Interface

---

## 📊 Overview

Interfaccia web grafica per **ripristinare pod Kubernetes** da backup senza usare la riga di comando.

**Accesso**:
- **URL locale** (dal server): http://localhost:9102
- **URL intranet** (da altri PC): http://192.168.1.114:9102
- **Porta**: 9102
- **Autenticazione**: Nessuna (solo intranet)

---

## 🚀 Accesso alla GUI

### Dal Server (192.168.1.114)

```bash
# Apri browser e vai a:
http://localhost:9102
```

### Da Altri PC nella Rete Locale

```bash
# Apri browser e vai a:
http://192.168.1.114:9102
```

**Requisiti**: Essere nella stessa rete locale (intranet) del server Kubernetes.

---

## 🎯 Come Utilizzare la GUI

### Step 1: Seleziona Backup

![Step 1]

1. Apri la pagina web http://192.168.1.114:9102
2. Vedi il dropdown **"Select Backup"**
3. Seleziona uno dei backup disponibili:
   - `k3s-cluster-backup-1.tar.gz` (penultimo backup)
   - `k3s-cluster-backup-2.tar.gz` (ultimo backup)
   - `latest-backup.tar.gz` (sempre punta all'ultimo)

**Info visualizzate**:
- Nome file backup
- Dimensione (es. 100K)
- Timestamp creazione

### Step 2: Seleziona Tipo Risorsa

![Step 2]

1. Dopo aver selezionato il backup, il sistema **automaticamente carica** le risorse disponibili
2. Apparirà il dropdown **"Select Resource Type"**
3. Seleziona il tipo di risorsa da ripristinare:
   - **deployments** - Deployment applicazioni
   - **statefulsets** - StatefulSets (MongoDB, Redis, etc.)
   - **services** - Services (NodePort, ClusterIP, etc.)
   - **secrets** - Secrets (password, certificati)
   - **configmaps** - ConfigMaps (configurazioni)
   - **persistentvolumeclaims** - PVC (storage)
   - **ingresses** - Ingress (routing esterno)
   - **namespaces** - Namespaces

**Numero risorse**: Visualizzato accanto al tipo (es. "deployments (5)")

### Step 3: Seleziona Risorsa Specifica

![Step 3]

1. Dopo aver selezionato il tipo, appare il dropdown **"Select Resource"**
2. Seleziona la risorsa specifica da ripristinare:
   - `insightlearn-api` (deployment API)
   - `mongodb` (statefulset database)
   - `redis` (statefulset cache)
   - `sqlserver` (deployment SQL Server)
   - etc.

### Step 4: Specifica Namespace

![Step 4]

1. Nel campo **"Namespace"**, inserisci il namespace target
2. **Default**: `insightlearn` (pre-compilato)
3. Altri namespace comuni:
   - `default`
   - `kube-system`
   - `jenkins`
   - `grafana`

### Step 5: Restore!

![Step 5]

1. Clicca sul pulsante **"🔄 Restore Pod"**
2. Appare una **finestra di conferma**:
   ```
   Conferma Ripristino

   Stai per ripristinare:
   • Backup: k3s-cluster-backup-2.tar.gz
   • Risorsa: deployment/insightlearn-api
   • Namespace: insightlearn

   Questa operazione sovrascriverà la risorsa esistente!
   ```
3. Clicca **"Conferma"** per procedere o **"Annulla"** per tornare indietro

### Step 6: Risultato

**Successo**:
```
✅ Restore completato con successo!

deployment.apps/insightlearn-api configured

Pod ripristinato correttamente.
```

**Errore**:
```
❌ Errore durante il restore

Error: namespace "wrong-ns" not found

Verifica namespace e riprova.
```

---

## 📋 Esempi Pratici

### Esempio 1: Ripristina API Deployment

1. **Backup**: `latest-backup.tar.gz`
2. **Resource Type**: `deployments`
3. **Resource**: `insightlearn-api`
4. **Namespace**: `insightlearn`
5. **Risultato**: Deployment API ripristinato da backup

### Esempio 2: Ripristina MongoDB StatefulSet

1. **Backup**: `k3s-cluster-backup-1.tar.gz` (backup precedente)
2. **Resource Type**: `statefulsets`
3. **Resource**: `mongodb`
4. **Namespace**: `insightlearn`
5. **Risultato**: MongoDB ripristinato a stato precedente

### Esempio 3: Ripristina Secret Cancellato

1. **Backup**: `latest-backup.tar.gz`
2. **Resource Type**: `secrets`
3. **Resource**: `mssql-sa-password`
4. **Namespace**: `insightlearn`
5. **Risultato**: Secret password ripristinato

### Esempio 4: Ripristina Ingress

1. **Backup**: `latest-backup.tar.gz`
2. **Resource Type**: `ingresses`
3. **Resource**: `insightlearn-ingress`
4. **Namespace**: `insightlearn`
5. **Risultato**: Routing ingress ripristinato

---

## ⚠️ Avvertenze

### Sovrascrittura Risorse

**ATTENZIONE**: Il restore **sovrascrive** la risorsa esistente con quella dal backup!

**Esempio**:
- Hai modificato `insightlearn-api` deployment aggiungendo una nuova variabile d'ambiente
- Fai restore da backup di ieri
- **Risultato**: La modifica viene persa, torna alla configurazione di ieri

### Backup Orari

I backup vengono creati **ogni ora alle :05 minuti** con rotazione:
- `backup-1.tar.gz` - Penultimo backup
- `backup-2.tar.gz` - Ultimo backup
- `latest-backup.tar.gz` - Symlink all'ultimo

**Massima perdita dati**: 1 ora (se ripristini backup-1)

### Namespace Non Esistente

Se specifichi un namespace che non esiste, ottieni errore:
```
Error: namespace "wrong-namespace" not found
```

**Fix**: Crea prima il namespace:
```bash
kubectl create namespace my-namespace
```

Oppure ripristina il namespace da backup usando la GUI:
1. Resource Type: `namespaces`
2. Resource: `my-namespace`

---

## 🛠️ Troubleshooting

### GUI Non Accessibile

**Problema**: Browser mostra "Impossibile raggiungere il sito"

**Verifica**:
```bash
# Da server, controlla se server è attivo
netstat -tuln | grep 9102

# Expected output:
# tcp  0  0.0.0.0:9102  0.0.0.0:*  LISTEN
```

**Fix**:
```bash
# Riavvia GUI server
cd /home/mpasqui/insightlearn_WASM/InsightLearn_WASM/k8s
python3 restore-gui-server.py > /tmp/restore-gui.log 2>&1 &
```

### Dropdown Vuoti

**Problema**: Dopo aver selezionato backup, dropdown "Resource Type" rimane vuoto

**Causa**: Backup corrotto o non accessibile

**Fix**:
```bash
# Verifica backup è leggibile
sudo tar -tzf /var/backups/k3s-cluster/latest-backup.tar.gz | head

# Se errore, usa backup precedente o forza nuovo backup:
sudo /home/mpasqui/insightlearn_WASM/InsightLearn_WASM/k8s/backup-cluster-state.sh
```

### Restore Fallito

**Problema**: Errore durante restore

**Cause comuni**:

1. **Namespace mancante**:
   ```
   Error: namespace "xyz" not found
   ```
   **Fix**: Crea namespace o usa `insightlearn`

2. **Permessi insufficienti**:
   ```
   Error: User cannot patch resources
   ```
   **Fix**: GUI gira come root, non dovrebbe succedere

3. **Risorsa duplicata**:
   ```
   Error: resource already exists
   ```
   **Fix**: Normale, kubectl applica comunque (update)

### Performance Lente

**Problema**: GUI impiega molto tempo a rispondere

**Causa**: Backup molto grande (>500MB) o server sotto carico

**Verifica**:
```bash
# Check dimensione backup
ls -lh /var/backups/k3s-cluster/*.tar.gz

# Check CPU/RAM server
top
```

---

## 🔒 Sicurezza

### Accesso Solo Intranet

**Configurazione attuale**:
- ✅ GUI **NON esposta su Internet**
- ✅ Accessibile solo da rete locale (192.168.1.x)
- ✅ Porta 9102 **NON aperta su firewall esterno**

**Verifica**:
```bash
# Da PC esterno (NON nella rete locale), deve fallire:
curl http://192.168.1.114:9102

# Expected: Connection timeout
```

### Nessuna Autenticazione

**⚠️ ATTENZIONE**: La GUI **non ha autenticazione**.

**Implicazioni**:
- Chiunque nella rete locale può fare restore
- Nessun log di chi ha fatto cosa
- Nessuna conferma password

**Raccomandazioni**:
1. Usare GUI solo da amministratori fidati
2. Firewall limiti accesso a IP specifici
3. Considerare aggiunta autenticazione in futuro

### Permessi Backup

I backup contengono **secrets** (password, certificati):
```bash
# Backup protetti con permessi root-only
ls -l /var/backups/k3s-cluster/
# -rw-r--r--. root root ...
```

**Solo root** può leggere i backup direttamente dal filesystem.

---

## 📊 Log e Monitoraggio

### Log GUI Server

**Location**: `/tmp/restore-gui.log`

**Contenuto**: Tutte le richieste HTTP e operazioni restore

```bash
# Visualizza log in real-time
tail -f /tmp/restore-gui.log
```

**Output esempio**:
```
192.168.1.100 - - [09/Nov/2025 22:35:01] "GET /api/backups HTTP/1.1" 200 -
192.168.1.100 - - [09/Nov/2025 22:35:15] "GET /api/backup/latest-backup.tar.gz HTTP/1.1" 200 -
192.168.1.100 - - [09/Nov/2025 22:35:42] "POST /api/restore HTTP/1.1" 200 -
```

### Verifica Restore Riuscito

Dopo un restore, verifica da kubectl:

```bash
# Check pod status
kubectl get pods -n insightlearn

# Check deployment
kubectl get deployment insightlearn-api -n insightlearn

# Check logs
kubectl logs -n insightlearn deployment/insightlearn-api --tail=50
```

---

## 🔄 Servizio Persistente (Opzionale)

Per rendere la GUI **persistente** anche dopo reboot, configurare systemd:

### Creazione Service

Già creato in: `/tmp/k3s-restore-gui.service`

**Contenuto**:
```ini
[Unit]
Description=K3s Pod Restore GUI Server
After=network.target k3s.service

[Service]
Type=simple
User=root
WorkingDirectory=/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/k8s
ExecStart=/usr/bin/python3 /home/mpasqui/insightlearn_WASM/InsightLearn_WASM/k8s/restore-gui-server.py
Restart=on-failure
RestartSec=10s

[Install]
WantedBy=multi-user.target
```

### Installazione (Richiede Sudo)

```bash
# Copia service file
sudo cp /tmp/k3s-restore-gui.service /etc/systemd/system/

# Ricarica systemd
sudo systemctl daemon-reload

# Abilita auto-start
sudo systemctl enable k3s-restore-gui.service

# Avvia servizio
sudo systemctl start k3s-restore-gui.service

# Verifica status
sudo systemctl status k3s-restore-gui.service
```

**Benefici**:
- GUI sempre disponibile
- Auto-start al boot
- Auto-restart in caso di crash

---

## 📞 Supporto

**Script GUI**: [k8s/restore-gui-server.py](../k8s/restore-gui-server.py)
**Documentazione Restore**: [RESTORE-PODS-FROM-BACKUP.md](RESTORE-PODS-FROM-BACKUP.md)
**Quick Examples**: [RESTORE-QUICK-EXAMPLES.md](RESTORE-QUICK-EXAMPLES.md)

**Maintainer**: InsightLearn DevOps Team
**Contact**: marcello.pasqui@gmail.com
**Version**: 1.0.0
**Date**: 2025-11-09

---

## ✅ Checklist Uso GUI

Prima di usare la GUI, verifica:

- [ ] Sei connesso alla rete locale (intranet)
- [ ] La GUI è accessibile su http://192.168.1.114:9102
- [ ] Sai quale backup vuoi usare (latest = ultimo)
- [ ] Conosci il nome della risorsa da ripristinare
- [ ] Sai quale namespace target
- [ ] Hai verificato che il backup esista
- [ ] Sei consapevole che il restore **sovrascrive** la risorsa esistente

**Solo dopo questi check** → procedi con il restore.

---

**🎉 GUI Ripristino Pod pronta per l'uso!**

**URL**: http://192.168.1.114:9102
