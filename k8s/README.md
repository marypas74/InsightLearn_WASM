# InsightLearn Kubernetes Deployment Guide

Questa directory contiene i manifesti Kubernetes per deployare InsightLearn su Debian 13 con minikube.

## Prerequisiti

- Docker installato e funzionante
- Kubernetes (minikube) installato
- kubectl configurato
- Minimo 8GB RAM disponibile per minikube
- 20GB spazio disco disponibile

## Struttura File

```
k8s/
├── 00-namespace.yaml              # Namespace insightlearn
├── 01-secrets.yaml                # Secrets per password e JWT
├── 02-configmap.yaml              # Configurazioni applicazione
├── 03-sqlserver-statefulset.yaml # SQL Server database
├── 04-redis-deployment.yaml       # Redis cache
├── 05-elasticsearch-deployment.yaml # Elasticsearch search
├── 06-api-deployment.yaml         # API backend
├── 07-web-deployment.yaml         # Web frontend Blazor
├── 08-ingress.yaml                # Ingress routing
├── build-images.sh                # Script per build immagini Docker
├── deploy.sh                      # Script per deployment completo
├── undeploy.sh                    # Script per rimuovere deployment
└── status.sh                      # Script per verificare stato
```

## Deployment Rapido

### 1. Avvia minikube

```bash
minikube start --memory=8192 --cpus=4
```

### 2. Abilita Ingress

```bash
minikube addons enable ingress
```

### 3. Costruisci le immagini Docker

```bash
cd /home/mpasqui/kubernetes/Insightlearn
./k8s/build-images.sh
```

Questo comando compila:
- `insightlearn/api:latest` - Backend API (.NET 8)
- `insightlearn/web:latest` - Frontend Web (Blazor Server)

### 4. Carica le immagini in minikube

```bash
minikube image load insightlearn/api:latest
minikube image load insightlearn/web:latest
```

### 5. Deploya l'applicazione

```bash
./k8s/deploy.sh
```

Questo script:
- Crea il namespace `insightlearn`
- Configura secrets e configmaps
- Deploya SQL Server, Redis, Elasticsearch
- Deploya API e Web application
- Configura Ingress per routing

### 6. Verifica lo stato

```bash
./k8s/status.sh
```

Oppure manualmente:
```bash
kubectl get all -n insightlearn
kubectl get pods -n insightlearn -w
```

### 7. Configura l'accesso

#### Opzione A: Via Ingress (Raccomandato)

1. Ottieni l'IP di minikube:
```bash
minikube ip
# Output: 192.168.49.2 (esempio)
```

2. Aggiungi al file `/etc/hosts` (richiede sudo):
```bash
sudo nano /etc/hosts
# Aggiungi la riga:
192.168.49.2 insightlearn.local
```

3. Accedi all'applicazione:
```
http://insightlearn.local
```

#### Opzione B: Via Port Forward

```bash
# Web application
kubectl port-forward -n insightlearn service/web-service 8080:80

# API
kubectl port-forward -n insightlearn service/api-service 8001:80
```

Accedi a:
- Web: http://localhost:8080
- API: http://localhost:8001

## Componenti Deployati

### Database Layer
- **SQL Server 2022**: Database principale (port 1433)
  - Storage: 20Gi persistent volume
  - Resources: 2-4Gi RAM, 0.5-2 CPU

- **Redis 7**: Cache e sessioni (port 6379)
  - Storage: 1Gi persistent volume
  - Resources: 256Mi-512Mi RAM

- **Elasticsearch 8.11**: Search engine (port 9200)
  - Storage: 5Gi persistent volume
  - Resources: 2-3Gi RAM

### Application Layer
- **InsightLearn API**: REST API backend
  - Replicas: 2 (auto-scaling 2-5)
  - Port: 80
  - Health checks: /health

- **InsightLearn Web**: Blazor Server frontend
  - Replicas: 2 (auto-scaling 2-5)
  - Port: 80
  - SignalR WebSocket support

### Networking
- **Ingress**: nginx-ingress-controller
  - Host: insightlearn.local
  - Routes: `/api` → API, `/` → Web

## Configurazione

### Secrets (01-secrets.yaml)

Modifica le password prima del deployment in produzione:

```yaml
mssql-sa-password: "YOUR_STRONG_PASSWORD"
jwt-secret-key: "YOUR_JWT_SECRET_KEY"
```

### ConfigMap (02-configmap.yaml)

Personalizza le configurazioni:
- Redis connection
- Elasticsearch URL
- JWT settings
- Log levels

## Monitoraggio

### Logs

```bash
# Logs API
kubectl logs -n insightlearn -l app=insightlearn-api -f

# Logs Web
kubectl logs -n insightlearn -l app=insightlearn-web -f

# Logs SQL Server
kubectl logs -n insightlearn sqlserver-0 -f

# Logs Redis
kubectl logs -n insightlearn -l app=redis -f
```

### Eventi

```bash
kubectl get events -n insightlearn --sort-by='.lastTimestamp'
```

### Risorse

```bash
kubectl top pods -n insightlearn
kubectl top nodes
```

## Scaling

### Manuale

```bash
# Scale API
kubectl scale deployment insightlearn-api -n insightlearn --replicas=3

# Scale Web
kubectl scale deployment insightlearn-web -n insightlearn --replicas=3
```

### Auto-scaling

L'auto-scaling è già configurato:
- CPU threshold: 70%
- Memory threshold: 80%
- Min replicas: 2
- Max replicas: 5

## Troubleshooting

### Pod non si avvia

```bash
# Controlla lo stato
kubectl describe pod <pod-name> -n insightlearn

# Controlla i logs
kubectl logs <pod-name> -n insightlearn --previous
```

### SQL Server non risponde

```bash
# Verifica health
kubectl exec -it sqlserver-0 -n insightlearn -- /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'YourPassword' -Q 'SELECT 1' -C

# Restart
kubectl delete pod sqlserver-0 -n insightlearn
```

### Problemi di connessione tra servizi

```bash
# Verifica DNS
kubectl exec -it <api-pod> -n insightlearn -- nslookup sqlserver-service

# Test connessione
kubectl exec -it <api-pod> -n insightlearn -- curl http://redis-service:6379
```

### Ingress non funziona

```bash
# Verifica ingress controller
kubectl get pods -n ingress-nginx

# Restart ingress
minikube addons disable ingress
minikube addons enable ingress
```

## Backup e Restore

### Backup Database

```bash
# Exec in SQL Server pod
kubectl exec -it sqlserver-0 -n insightlearn -- /bin/bash

# Dentro il pod, esegui backup
/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'YourPassword' -Q "BACKUP DATABASE InsightLearnDb TO DISK = '/var/opt/mssql/backup/InsightLearn.bak'" -C
```

### Backup Persistent Volumes

```bash
# Lista PVCs
kubectl get pvc -n insightlearn

# Backup manuale (esempio per Redis)
kubectl exec -it <redis-pod> -n insightlearn -- redis-cli SAVE
```

## Cleanup

### Rimozione completa

```bash
./k8s/undeploy.sh
```

Questo script rimuove:
1. Ingress
2. Deployments (Web, API)
3. Databases (Elasticsearch, Redis, SQL Server)
4. ConfigMaps e Secrets
5. Namespace (opzionale, include PVCs)

### Rimozione solo applicazione (mantiene database)

```bash
kubectl delete -f k8s/07-web-deployment.yaml
kubectl delete -f k8s/06-api-deployment.yaml
```

## Aggiornamento Applicazione

### Rolling Update

```bash
# 1. Ricostruisci immagini con nuovo tag
docker build -f Dockerfile -t insightlearn/api:v1.1 .
docker build -f Dockerfile.web -t insightlearn/web:v1.1 .

# 2. Carica in minikube
minikube image load insightlearn/api:v1.1
minikube image load insightlearn/web:v1.1

# 3. Aggiorna deployments
kubectl set image deployment/insightlearn-api api=insightlearn/api:v1.1 -n insightlearn
kubectl set image deployment/insightlearn-web web=insightlearn/web:v1.1 -n insightlearn

# 4. Monitora rollout
kubectl rollout status deployment/insightlearn-api -n insightlearn
kubectl rollout status deployment/insightlearn-web -n insightlearn
```

### Rollback

```bash
# Rollback API
kubectl rollout undo deployment/insightlearn-api -n insightlearn

# Rollback Web
kubectl rollout undo deployment/insightlearn-web -n insightlearn
```

## Performance Tuning

### SQL Server

Modifica `03-sqlserver-statefulset.yaml`:
```yaml
resources:
  requests:
    memory: "4Gi"  # Aumenta per produzione
    cpu: "2000m"
  limits:
    memory: "8Gi"
    cpu: "4000m"
```

### Elasticsearch

Modifica `05-elasticsearch-deployment.yaml`:
```yaml
env:
- name: ES_JAVA_OPTS
  value: "-Xms2g -Xmx2g"  # Aumenta heap size
```

### Redis

Modifica `04-redis-deployment.yaml`:
```yaml
- --maxmemory
- "1gb"  # Aumenta cache size
```

## Produzione vs Sviluppo

### Per produzione:

1. **Modifica secrets** con password forti
2. **Aumenta risorse** per database
3. **Abilita TLS** per Ingress (aggiungi certificati)
4. **Configura backup automatici**
5. **Imposta resource limits** appropriati
6. **Configura monitoring** (Prometheus/Grafana)
7. **Usa registry privato** per immagini Docker

### Per sviluppo:

- Usa configurazioni default
- Port-forward invece di Ingress
- Risorse minime

## Accesso al Database

### Da fuori del cluster

```bash
# Port forward SQL Server
kubectl port-forward -n insightlearn sqlserver-0 1433:1433

# Connetti con Azure Data Studio o SSMS
Server: localhost,1433
Username: sa
Password: ElevateLearning2024StrongPass!
Database: InsightLearnDb
```

### Da dentro il cluster

Connection string (già configurato nei pods):
```
Server=sqlserver-service;Database=InsightLearnDb;User=sa;Password=...;TrustServerCertificate=true
```

## Supporto

Per problemi o domande:
1. Verifica i logs: `./k8s/status.sh`
2. Controlla gli eventi: `kubectl get events -n insightlearn`
3. Verifica risorse: `kubectl top pods -n insightlearn`

## Note su Debian 13

- Kubernetes versione minima: 1.28+
- Docker versione minima: 24.0+
- Minikube configurato per driver docker
- Filesystem: supporta persistent volumes locali

## Miglioramenti Futuri

- [ ] HTTPS/TLS per Ingress
- [ ] Monitoring con Prometheus
- [ ] Logging centralizzato (ELK/Loki)
- [ ] GitOps con ArgoCD
- [ ] Service Mesh (Istio/Linkerd)
- [ ] Backup automatici
- [ ] Multi-tenancy support

## Accesso HTTPS dalla Intranet

### Configurazione Completata

L'applicazione InsightLearn è ora accessibile via HTTPS dalla intranet all'indirizzo:

**https://192.168.1.103**

### Architettura

```
Internet/Intranet (192.168.1.0/24)
    ↓
Host Debian (192.168.1.103)
    ↓ nginx reverse proxy (ports 80/443)
    ↓
Minikube (192.168.49.2)
    ↓ NodePort services (31080, 31081)
    ↓
Kubernetes Pods
    ├── Web (Blazor Server) - 2 replicas
    ├── API (REST API) - 2 replicas  
    ├── SQL Server
    ├── Redis
    └── Elasticsearch
```

### Componenti

1. **Nginx Reverse Proxy** (sull'host Debian)
   - Ascolta su: 80 (HTTP) e 443 (HTTPS)
   - Redirect automatico da HTTP a HTTPS
   - Certificate TLS autofirmato
   - WebSocket support per SignalR (Blazor Server)
   - Proxy verso minikube NodePort services

2. **Kubernetes NodePort Services**
   - `web-service-nodeport`: 31080 → Web pods
   - `api-service-nodeport`: 31081 → API pods

3. **Certificato TLS**
   - Posizione: `/etc/nginx/ssl/insightlearn/`
   - Tipo: Self-signed certificate
   - Validità: 365 giorni
   - Subject Alternative Names: IP:192.168.1.103, DNS:insightlearn.local

### File di Configurazione

#### Nginx Configuration
```bash
/etc/nginx/sites-available/insightlearn
/etc/nginx/ssl/insightlearn/tls.crt
/etc/nginx/ssl/insightlearn/tls.key
```

#### Kubernetes Manifests
```bash
k8s/09-nodeport-services.yaml  # NodePort services
```

### Accesso da Client sulla Intranet

#### Browser Web
1. Apri browser su qualsiasi dispositivo nella intranet (192.168.1.0/24)
2. Vai su: `https://192.168.1.103`
3. Accetta l'avviso di sicurezza (certificato autofirmato)
   - Chrome/Edge: Click "Advanced" → "Proceed to 192.168.1.103 (unsafe)"
   - Firefox: Click "Advanced" → "Accept the Risk and Continue"
4. L'applicazione si apre

#### API REST
```bash
# Da qualsiasi client sulla intranet
curl -k https://192.168.1.103/api/health
curl -k https://192.168.1.103/api/auth/status
```

### Certificato per Produzione

Per produzione, sostituisci il certificato autofirmato con uno valido:

#### Opzione 1: Certificato da CA interna
Se la tua organizzazione ha una CA interna:
```bash
# Genera CSR
openssl req -new -key /etc/nginx/ssl/insightlearn/tls.key \
    -out /tmp/insightlearn.csr \
    -subj "/C=IT/O=YourOrg/CN=192.168.1.103"

# Invia CSR alla CA interna per firma
# Installa certificato firmato
sudo cp certified.crt /etc/nginx/ssl/insightlearn/tls.crt
sudo systemctl reload nginx
```

#### Opzione 2: Let's Encrypt (se hai dominio pubblico)
```bash
# Installa certbot
sudo apt-get install certbot python3-certbot-nginx

# Ottieni certificato (richiede dominio DNS pubblico)
sudo certbot --nginx -d yourdomain.com

# Auto-renewal è configurato automaticamente
```

### Firewall

Se hai un firewall, assicurati che le porte siano aperte:

```bash
# UFW
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp

# iptables
sudo iptables -A INPUT -p tcp --dport 80 -j ACCEPT
sudo iptables -A INPUT -p tcp --dport 443 -j ACCEPT
sudo iptables-save > /etc/iptables/rules.v4
```

### Monitoraggio

#### Logs Nginx
```bash
# Access logs
sudo tail -f /var/log/nginx/insightlearn-access.log

# Error logs  
sudo tail -f /var/log/nginx/insightlearn-error.log

# Tutti i logs
sudo tail -f /var/log/nginx/*.log
```

#### Test Connettività

```bash
# Test HTTPS locale
curl -k https://192.168.1.103

# Test da remoto (dalla intranet)
curl -k https://192.168.1.103

# Test con headers
curl -k -I https://192.168.1.103

# Test API
curl -k https://192.168.1.103/api/health

# Test WebSocket (per Blazor SignalR)
wscat -c wss://192.168.1.103/_blazor -n
```

#### Verifica SSL/TLS

```bash
# Info certificato
openssl s_client -connect 192.168.1.103:443 -showcerts

# Test SSL configuration
nmap --script ssl-enum-ciphers -p 443 192.168.1.103
```

### Troubleshooting HTTPS

#### Errore: Connection Refused
```bash
# Verifica nginx è attivo
sudo systemctl status nginx

# Verifica porte in ascolto
sudo ss -tlnp | grep -E ':(80|443)'

# Restart nginx
sudo systemctl restart nginx
```

#### Errore: 502 Bad Gateway
```bash
# Verifica NodePort services
kubectl get svc -n insightlearn | grep nodeport

# Verifica pods sono running
kubectl get pods -n insightlearn

# Test connessione diretta a minikube
curl http://192.168.49.2:31080
curl http://192.168.49.2:31081/api/health
```

#### Errore: 504 Gateway Timeout
```bash
# Aumenta timeouts in nginx
sudo nano /etc/nginx/sites-available/insightlearn

# Modifica:
proxy_connect_timeout 600s;
proxy_send_timeout 600s;
proxy_read_timeout 600s;

sudo nginx -t && sudo systemctl reload nginx
```

#### Problemi WebSocket/SignalR
```bash
# Verifica headers WebSocket in nginx logs
sudo tail -f /var/log/nginx/insightlearn-access.log | grep Upgrade

# Test WebSocket upgrade
curl -k -i -N -H "Connection: Upgrade" \
    -H "Upgrade: websocket" \
    -H "Sec-WebSocket-Version: 13" \
    -H "Sec-WebSocket-Key: test" \
    https://192.168.1.103/
```

### Performance Tuning

#### Nginx Caching
```nginx
# Aggiungi in /etc/nginx/sites-available/insightlearn

proxy_cache_path /var/cache/nginx/insightlearn 
    levels=1:2 
    keys_zone=insightlearn_cache:10m 
    max_size=1g 
    inactive=60m;

# Nei location blocks:
location ~* \.(css|js|jpg|jpeg|png|gif|ico)$ {
    proxy_cache insightlearn_cache;
    proxy_cache_valid 200 1h;
    # ... resto config
}
```

#### Rate Limiting
```nginx
# Protezione contro DDoS
limit_req_zone $binary_remote_addr zone=mylimit:10m rate=10r/s;

server {
    limit_req zone=mylimit burst=20 nodelay;
    # ... resto config
}
```

### Script Automatici

#### Setup HTTPS
```bash
./k8s/setup-https-access.sh
```
Questo script automaticamente:
- Installa nginx se necessario
- Genera certificati TLS
- Configura nginx reverse proxy
- Abilita e avvia i servizi

#### Check Status
```bash
# Status completo
./k8s/status.sh

# Solo nginx
sudo systemctl status nginx
```

### Accesso da Domini

Per usare un nome dominio invece dell'IP:

#### Opzione 1: DNS locale (consigliato per intranet)
Configura il tuo DNS server interno:
```
insightlearn.local.    A    192.168.1.103
```

#### Opzione 2: File hosts sui client
Su ogni client che deve accedere, modifica `/etc/hosts` (Linux/Mac) o `C:\Windows\System32\drivers\etc\hosts` (Windows):
```
192.168.1.103  insightlearn.local
```

Poi accedi via: `https://insightlearn.local`

### Backup Configurazione

```bash
# Backup nginx config
sudo cp /etc/nginx/sites-available/insightlearn \
    /home/mpasqui/kubernetes/Insightlearn/backup/nginx-config-$(date +%Y%m%d).conf

# Backup certificates
sudo tar czf /home/mpasqui/kubernetes/Insightlearn/backup/ssl-certs-$(date +%Y%m%d).tar.gz \
    /etc/nginx/ssl/insightlearn/
```

### Ripristino dopo Reboot

Dopo un riavvio del server:

```bash
# 1. Start minikube
minikube start

# 2. Nginx si avvia automaticamente (se enabled)
# Oppure manualmente:
sudo systemctl start nginx

# 3. Verifica tutto sia up
kubectl get pods -n insightlearn
sudo systemctl status nginx
curl -k https://192.168.1.103
```

### Sicurezza

#### Raccomandazioni per Produzione

1. **Usa certificato valido** (non self-signed)
2. **Abilita firewall** e permetti solo porte necessarie
3. **Configura rate limiting** in nginx
4. **Abilita fail2ban** per protezione brute-force
5. **Usa password forti** nei secrets Kubernetes
6. **Abilita audit logging** 
7. **Mantieni sistema aggiornato**

```bash
# Fail2ban per nginx
sudo apt-get install fail2ban
sudo systemctl enable fail2ban
sudo systemctl start fail2ban
```

### Monitoraggio Avanzato

Per monitoraggio in produzione, considera:

1. **Prometheus + Grafana** per metriche
2. **ELK Stack** per log centralizzati
3. **Uptime monitoring** (UptimeRobot, Pingdom)
4. **Alerting** via email/Slack

---

**Congratulazioni!** InsightLearn è ora accessibile via HTTPS dalla tua intranet! 🎉
