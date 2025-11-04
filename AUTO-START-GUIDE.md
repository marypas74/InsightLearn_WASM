# InsightLearn - Guida Auto-Avvio Sistema

## ✅ Configurazione Completata

Il sistema InsightLearn è ora completamente configurato per l'**auto-avvio al riavvio del server**.

---

## 📋 Servizi Systemd Configurati

### 1. **minikube-start.service**
Avvia automaticamente il cluster Kubernetes Minikube con Podman.

**Configurazione:**
- Driver: `podman`
- Runtime: `cri-o`
- RAM: `9GB` (9216MB)
- CPU: `6 cores`
- User: `mpasqui`

**File:** `/etc/systemd/system/minikube-start.service`

**Comandi utili:**
```bash
# Verifica stato
sudo systemctl status minikube-start

# Avvia manualmente (se non già avviato)
sudo systemctl start minikube-start

# Ferma
sudo systemctl stop minikube-start

# Log
sudo journalctl -u minikube-start -f
```

---

### 2. **insightlearn-port-forward.service**
Avvia automaticamente i port-forward HTTP (80) e HTTPS (443) verso i pod Kubernetes.

**Configurazione:**
- Ports: `80:80` (HTTP), `443:443` (HTTPS)
- Target: `service/api-service` in namespace `insightlearn`
- Listening: `0.0.0.0` (tutte le interfacce)
- Dependencies: Avvia **dopo** `minikube-start.service`
- Auto-wait: Attende che i pod siano `Ready` prima di inoltrare

**File:** `/etc/systemd/system/insightlearn-port-forward.service`

**Comandi utili:**
```bash
# Verifica stato
sudo systemctl status insightlearn-port-forward

# Riavvia (se necessario)
sudo systemctl restart insightlearn-port-forward

# Log real-time
sudo journalctl -u insightlearn-port-forward -f
```

---

### 3. **cloudflared-tunnel.service** (Opzionale - Da Configurare)
Tunnel Cloudflare per esporre il sito su https://wasm.insightlearn.cloud

**Installazione:**
```bash
/tmp/setup-permanent-tunnel.sh
```

**Comandi utili:**
```bash
# Verifica stato
sudo systemctl status cloudflared-tunnel

# Log
sudo journalctl -u cloudflared-tunnel -f
```

---

## 🔄 Sequenza di Avvio

Al riavvio del server, la sequenza è:

1. **Sistema Operativo** (Rocky Linux 10)
2. **Podman** (container runtime)
3. **minikube-start.service** (Kubernetes cluster)
   - Avvia minikube
   - Carica tutti i pod (API, SQL Server, MongoDB, Redis, Ollama, ecc.)
4. **insightlearn-port-forward.service** (Exposure dei servizi)
   - Attende che `api-service` sia `Ready`
   - Avvia port-forward su porte 80 e 443
5. **Sito accessibile** su http://localhost e https://192.168.1.114

---

## 🌐 Endpoint Accessibili

### Accesso Locale (LAN)

| Endpoint | Descrizione | Status |
|----------|-------------|--------|
| http://localhost | API HTTP | ✅ |
| https://localhost | API HTTPS | ✅ |
| http://192.168.1.114 | API HTTP (LAN) | ✅ |
| https://192.168.1.114 | API HTTPS (LAN) | ✅ |

### Health Check
```bash
curl http://localhost/health
# Output: {"application":"InsightLearn API","version":"1.4.29","status":"running"}
```

### Cloudflare Tunnel (Quando Configurato)
- https://wasm.insightlearn.cloud
- https://www.wasm.insightlearn.cloud

---

## 🐳 Pod Kubernetes Deployati

| Pod/Service | Replicas | Status | Port |
|-------------|----------|--------|------|
| insightlearn-api | 1 | Running | 80, 443 |
| mssql-server | 1 | Running | 1433 |
| mongodb | 1 | Running | 27017 |
| redis | 1 | Running | 6379 |
| ollama | 1 | Running | 11434 |
| elasticsearch | 1 | Init:CrashLoopBackOff* | 9200 |

*Elasticsearch non critico per il funzionamento dell'applicazione

---

## 🔧 Comandi Utili

### Kubernetes

```bash
# Status completo cluster
kubectl get pods -n insightlearn
kubectl get svc -n insightlearn
kubectl get deployments -n insightlearn

# Log API pod
kubectl logs -f deployment/insightlearn-api -n insightlearn

# Descrizione pod
kubectl describe pod -l app=insightlearn-api -n insightlearn

# Restart API deployment
kubectl rollout restart deployment/insightlearn-api -n insightlearn
```

### Minikube

```bash
# Status
minikube status

# Dashboard (aprire in browser)
minikube dashboard

# SSH nel cluster
minikube ssh

# IP del cluster
minikube ip
```

### Port-Forward Manuale (Se Servizi Disabilitati)

```bash
# HTTP + HTTPS contemporanee
sudo /home/mpasqui/.local/bin/kubectl port-forward \
  -n insightlearn --address 0.0.0.0 \
  service/api-service 80:80 443:443

# Solo HTTP
kubectl port-forward -n insightlearn service/api-service 8080:80

# Solo HTTPS
kubectl port-forward -n insightlearn service/api-service 8443:443
```

---

## 🔒 Certificati SSL

**Posizione:** `nginx/certs/`

I certificati TLS sono self-signed e utilizzati per HTTPS locale:
- `tls.crt` - Certificato pubblico
- `tls.key` - Chiave privata

I certificati sono già configurati in Kubernetes:
```bash
kubectl get secrets -n insightlearn | grep tls
# api-tls-cert
# ollama-tls
```

---

## 🔥 Firewall

**Porte aperte:**
```bash
firewall-cmd --list-all
# Services: http, https, ssh
# Ports: 3389/tcp, 8080/tcp
```

---

## 🛠️ Troubleshooting

### Problema: Sito non risponde dopo riavvio

**1. Verifica servizi:**
```bash
sudo systemctl status minikube-start
sudo systemctl status insightlearn-port-forward
```

**2. Verifica minikube:**
```bash
minikube status
# Dovrebbe mostrare: Running
```

**3. Verifica pod:**
```bash
kubectl get pods -n insightlearn
# Tutti dovrebbero essere Running o Completed
```

**4. Verifica port-forward:**
```bash
ps aux | grep "kubectl port-forward"
# Dovrebbero esserci processi attivi

# Verifica porte in ascolto
ss -tlnp | grep -E ":(80|443) "
```

**5. Test manuale:**
```bash
curl http://localhost/health
curl -k https://localhost/health
```

### Problema: Minikube non si avvia

```bash
# Verifica log
sudo journalctl -u minikube-start -n 50

# Riavvia manualmente
minikube stop
minikube start --driver=podman --container-runtime=cri-o --memory=9216 --cpus=6
```

### Problema: Port-forward fallisce

```bash
# Verifica che i pod siano pronti
kubectl get pods -n insightlearn

# Riavvia servizio
sudo systemctl restart insightlearn-port-forward

# Avvia manualmente
sudo /home/mpasqui/.local/bin/kubectl port-forward \
  -n insightlearn --address 0.0.0.0 \
  service/api-service 80:80 443:443
```

### Problema: Cloudflare 502

```bash
# Verifica che localhost:80 risponda
curl http://localhost/health

# Se localhost funziona, il problema è Cloudflare Tunnel
# Configura tunnel permanente:
/tmp/setup-permanent-tunnel.sh
```

---

## 📊 Monitoring

### Verifica Stato Completo

```bash
#!/bin/bash
echo "=== InsightLearn Status Check ==="
echo ""

echo "1. Minikube:"
minikube status | head -5
echo ""

echo "2. Kubernetes Pods:"
kubectl get pods -n insightlearn
echo ""

echo "3. Services:"
kubectl get svc -n insightlearn
echo ""

echo "4. Port-Forward Active:"
ps aux | grep "kubectl port-forward" | grep -v grep
echo ""

echo "5. Listening Ports:"
ss -tlnp | grep -E ":(80|443|8080) "
echo ""

echo "6. Health Check:"
curl -s http://localhost/health && echo " ✅"
curl -s -k https://localhost/health && echo " ✅"
echo ""
```

Salva come `/home/mpasqui/check-status.sh` e rendi eseguibile:
```bash
chmod +x /home/mpasqui/check-status.sh
```

---

## 📦 Backup e Ripristino

### Backup Configurazione

File critici da backuppare:
```
/home/mpasqui/.kube/config
/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/k8s/
/etc/systemd/system/minikube-start.service
/etc/systemd/system/insightlearn-port-forward.service
/etc/systemd/system/cloudflared-tunnel.service
/home/mpasqui/.cloudflared/
nginx/certs/
```

### Ripristino su Nuovo Server

1. Clona repository:
```bash
git clone https://github.com/marypas74/InsightLearn_WASM.git
cd InsightLearn_WASM
```

2. Copia file di backup in posizioni corrette

3. Installa servizi:
```bash
sudo bash /tmp/install-autostart-services.sh
```

4. Riavvia server:
```bash
sudo reboot
```

---

## 📝 Note Importanti

1. **Password Sudo:** Conservata solo in memoria, non su disco (SS1-Temp1234)
2. **Kubernetes Secrets:** Password generate e salvate in `k8s/01-secrets.yaml`
3. **Auto-Start:** Tutti i servizi si avviano automaticamente al boot
4. **Resilienza:** I servizi hanno `Restart=always` configurato
5. **Logs:** Tutti i log sono disponibili tramite `journalctl`

---

## 🎯 Quick Commands Reference

```bash
# Verifica stato completo
sudo systemctl status minikube-start insightlearn-port-forward

# Riavvia tutto
sudo systemctl restart minikube-start insightlearn-port-forward

# Test applicazione
curl http://localhost/health

# Logs real-time
sudo journalctl -u insightlearn-port-forward -f

# Accesso dashboard Kubernetes
minikube dashboard

# Status pod
kubectl get pods -n insightlearn -w
```

---

## 📞 Supporto

- **Repository:** https://github.com/marypas74/InsightLearn_WASM
- **Issues:** https://github.com/marypas74/InsightLearn_WASM/issues
- **Email:** marcello.pasqui@gmail.com

---

**Versione:** 1.4.29
**Data Configurazione:** 2025-11-04
**Sistema:** Rocky Linux 10 + Podman + Kubernetes (Minikube)
