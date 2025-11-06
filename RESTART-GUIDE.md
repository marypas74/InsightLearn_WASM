# InsightLearn - Guida Riavvio Automatico

## Panoramica

Questo sistema è configurato per ripartire **automaticamente** dopo un riavvio del server senza intervento manuale.

## Componenti

### 1. Script di Restart
- **`restart-after-reboot.sh`** - Script principale che gestisce l'avvio completo

### 2. Systemd Service
- **`insightlearn-startup.service`** - Servizio systemd che esegue lo script all'avvio

### 3. Kubernetes Restart Policies
- Tutti i pod hanno `RestartPolicy: Always`
- I pod ripartono automaticamente quando minikube si riavvia

## Installazione Servizio Autostart

```bash
# Esegui come root
sudo ./install-autostart.sh
```

## Sequenza di Avvio Automatico

Quando il server si riavvia, automaticamente:

1. **Systemd avvia il servizio** `insightlearn-startup.service`
2. **Lo script verifica minikube**
   - Se non è running, lo avvia
   - Parametri: podman driver, 13GB RAM, 6 CPU
3. **Attende che i pod siano pronti**
   - API pods: `app=insightlearn-api`
   - Ollama pod: `app=ollama`
4. **Avvia i port-forward**
   - WASM: localhost:8080 → service/insightlearn-wasm-blazor-webassembly:80
   - API: localhost:8081 → service/api-service:80
5. **Avvia Cloudflare Tunnel**
   - Se cloudflared è disponibile
   - Tunnel name: `insightlearn-wasm`
6. **Verifica che tutto funzioni**
   - Test API health: http://localhost:8081/health
   - Test Chatbot: http://localhost:8081/api/chat/health
   - Test WASM: http://localhost:8080

## Test Manuale (Senza Riavvio)

```bash
# Test completo
./restart-after-reboot.sh

# Verifica status
minikube status
kubectl get pods -n insightlearn
ps aux | grep port-forward
```

## Verifica Servizio Autostart

```bash
# Status servizio
sudo systemctl status insightlearn-startup.service

# Logs in tempo reale
sudo journalctl -u insightlearn-startup.service -f

# Abilita servizio (se non già fatto)
sudo systemctl enable insightlearn-startup.service

# Test avvio servizio
sudo systemctl start insightlearn-startup.service
```

## Riavvio Server - Procedura

### Prima del Riavvio

**NESSUNA AZIONE RICHIESTA** - tutto è automatico!

### Dopo il Riavvio

1. **Attendi 3-5 minuti** per l'avvio completo
2. **Verifica servizi**:
   ```bash
   # Check minikube
   minikube status
   
   # Check pods
   kubectl get pods -n insightlearn
   
   # Check port-forwards
   ps aux | grep port-forward
   
   # Test endpoints
   curl http://localhost:8081/health
   curl http://localhost:8080
   ```

3. **Se tutto OK**, accedi a:
   - https://wasm.insightlearn.cloud (via Cloudflare)
   - http://localhost:8080 (WASM locale)
   - http://localhost:8081 (API locale)

## Troubleshooting

### Servizio Non Parte

```bash
# Check logs
sudo journalctl -u insightlearn-startup.service --no-pager -n 50

# Restart manuale
sudo systemctl restart insightlearn-startup.service
```

### Minikube Non Parte

```bash
# Stop minikube
minikube stop

# Start manuale
minikube start --driver=podman --container-runtime=cri-o \
               --memory=9216 --cpus=6 \
               --base-image=gcr.io/k8s-minikube/kicbase-rocky:v0.0.48
```

### Port-Forward Non Attivi

```bash
# Kill esistenti
pkill -f "kubectl port-forward"

# Restart con script
./start-all.sh
```

### Pod Non Pronti

```bash
# Check pod status
kubectl get pods -n insightlearn

# Describe problematic pod
kubectl describe pod -n insightlearn <pod-name>

# Check logs
kubectl logs -n insightlearn <pod-name>

# Restart pod
kubectl delete pod -n insightlearn <pod-name>
# Kubernetes ricrea automaticamente il pod
```

## Logs Importanti

```bash
# Service logs
sudo journalctl -u insightlearn-startup.service

# Port-forward logs
tail -f /tmp/port-forward-api.log
tail -f /tmp/port-forward-wasm.log

# Cloudflare tunnel logs
tail -f /tmp/cloudflared.log

# Minikube logs
minikube logs
```

## Files Coinvolti

```
/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/
├── restart-after-reboot.sh              # Script principale
├── install-autostart.sh                 # Installazione servizio
├── start-all.sh                         # Port-forward e tunnel
├── setup.sh                             # Setup iniziale
└── deploy.sh                            # Deploy Kubernetes

/etc/systemd/system/
└── insightlearn-startup.service         # Systemd service

/tmp/
├── port-forward-api.log                 # Logs port-forward API
├── port-forward-wasm.log                # Logs port-forward WASM
└── cloudflared.log                      # Logs tunnel Cloudflare
```

## Note Importanti

1. **Tempo di Avvio**: 3-5 minuti dopo il boot del server
2. **Cloudflare Tunnel**: Richiede cloudflared installato e configurato
3. **Resources**: 9GB RAM, 6 CPU assegnati a minikube
4. **Persistence**: I dati dei database (SQL, Mongo, Redis) sono persistenti
5. **No Manual Intervention**: Tutto riparte automaticamente

## Comandi Rapidi

```bash
# Verifica completa sistema
./restart-after-reboot.sh

# Solo port-forwards
./start-all.sh

# Status completo
minikube status && \
kubectl get pods -n insightlearn && \
ps aux | grep port-forward | grep -v grep

# Restart completo (se necessario)
pkill -f port-forward
minikube stop
minikube start --driver=podman --memory=9216 --cpus=6
./start-all.sh
```

## Supporto

In caso di problemi persistenti:
1. Controlla i logs: `sudo journalctl -u insightlearn-startup.service`
2. Esegui manualmente: `./restart-after-reboot.sh`
3. Verifica risorse: `free -h && df -h`
