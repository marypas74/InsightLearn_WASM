# Istruzioni di Installazione Watchdog

## 🚀 Installazione in 1 Comando

Esegui questo comando per installare tutto:

```bash
sudo ./install-autostart.sh
```

## 📋 Oppure: Installazione Manuale Passo-Passo

Se preferisci installare manualmente:

### 1. Copia i file di servizio

```bash
sudo cp /tmp/insightlearn-startup.service /etc/systemd/system/
sudo cp /tmp/insightlearn-watchdog.service /etc/systemd/system/
```

### 2. Crea il file di log

```bash
sudo touch /var/log/insightlearn-watchdog.log
sudo chown mpasqui:mpasqui /var/log/insightlearn-watchdog.log
```

### 3. Ricarica systemd

```bash
sudo systemctl daemon-reload
```

### 4. Disabilita il vecchio servizio (se esiste)

```bash
sudo systemctl disable minikube-start.service 2>/dev/null || true
```

### 5. Abilita i nuovi servizi

```bash
sudo systemctl enable insightlearn-startup.service
sudo systemctl enable insightlearn-watchdog.service
```

### 6. Avvia i servizi ORA (opzionale)

```bash
sudo systemctl start insightlearn-startup.service
sudo systemctl start insightlearn-watchdog.service
```

## ✅ Verifica Installazione

### Check status servizi

```bash
sudo systemctl status insightlearn-startup.service
sudo systemctl status insightlearn-watchdog.service
```

### Guarda i logs in tempo reale

```bash
# Startup service
sudo journalctl -u insightlearn-startup.service -f

# Watchdog service
sudo journalctl -u insightlearn-watchdog.service -f

# Watchdog log file
tail -f /var/log/insightlearn-watchdog.log
```

## 🔄 Dopo il Prossimo Riavvio

Dopo aver riavviato il server, i servizi partiranno automaticamente:

1. **20 secondi dopo il boot**: parte `insightlearn-startup.service`
   - Avvia minikube
   - Avvia port-forwards
   - Avvia Cloudflare tunnel

2. **Subito dopo**: parte `insightlearn-watchdog.service`
   - Monitora ogni 30 secondi
   - Riavvia automaticamente tutto ciò che cade

## 📊 Cosa Monitora il Watchdog

- ✅ **Minikube**: Se si ferma, lo riavvia
- ✅ **Pod Critici**: sqlserver, mongodb, redis, api, ollama, wasm
- ✅ **Port-Forwards**: 8080 (WASM), 8081 (API)
- ✅ **Cloudflare Tunnel**: Se cade, lo riavvia
- ✅ **Health Checks**: API /health, WASM homepage

## ⏱️ Frequenza Controlli

Il watchdog controlla ogni **30 secondi** lo stato di tutti i servizi.

## 🛠️ Comandi Utili

```bash
# Stop watchdog temporaneamente
sudo systemctl stop insightlearn-watchdog.service

# Restart watchdog
sudo systemctl restart insightlearn-watchdog.service

# Disable watchdog (non parte più al boot)
sudo systemctl disable insightlearn-watchdog.service

# Re-enable watchdog
sudo systemctl enable insightlearn-watchdog.service
```

## 🎯 Test Rapido

Dopo l'installazione, testa che tutto funzioni:

```bash
# 1. Start services
sudo systemctl start insightlearn-startup.service
sudo systemctl start insightlearn-watchdog.service

# 2. Aspetta 30 secondi

# 3. Check logs
tail -20 /var/log/insightlearn-watchdog.log

# 4. Dovresti vedere messaggi come:
#    [2025-11-06 23:00:00] ℹ️  === InsightLearn Watchdog Started ===
```

## 🔥 Test Watchdog (Opzionale)

Vuoi testare che il watchdog funzioni? Uccidi un servizio:

```bash
# 1. Kill port-forward API
pkill -f "port-forward.*api-service"

# 2. Aspetta 30 secondi

# 3. Check log - dovrebbe vedere:
#    ❌ API port-forward not running, restarting...
#    ✅ API port-forward restarted on 8081

# 4. Verifica che sia ripartito
ps aux | grep port-forward | grep api
```

## 📞 Supporto

Se hai problemi:
1. Check logs: `sudo journalctl -u insightlearn-watchdog.service -n 100`
2. Check watchdog log: `tail -50 /var/log/insightlearn-watchdog.log`
3. Verifica permessi: `ls -l /var/log/insightlearn-watchdog.log`

---

**Sistema Completamente Autonomo** - Nessun intervento manuale richiesto! 🎉
