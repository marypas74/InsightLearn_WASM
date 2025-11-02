# InsightLearn - Kubernetes Deployment Summary

## ✅ Deployment Completato

InsightLearn è stato successfully deployato su Kubernetes (minikube) e configurato per l'accesso HTTPS dalla intranet.

### Accesso all'Applicazione

**URL Principale:** `https://192.168.1.103`

### Architettura Deployment

```
Intranet (192.168.1.0/24)
    ↓
nginx Reverse Proxy (192.168.1.103:443)
    ├── SSL Termination
    ├── WebSocket Support (SignalR)
    └── Proxy Headers
        ↓
    Minikube Cluster (192.168.49.2)
        ├── NodePort Services
        │   ├── web-service-nodeport:31080
        │   └── api-service-nodeport:31081
        ↓
    Kubernetes Pods (namespace: insightlearn)
        ├── Web (Blazor Server) - 2 replicas
        ├── API (.NET 8) - 2 replicas
        ├── SQL Server 2022 - StatefulSet
        ├── Redis 7 - Cache & Sessions
        └── Elasticsearch 8.11 - Search
```

## Componenti Deployati

### Application Layer
- **InsightLearn Web**: Blazor Server application
  - 2 replicas con auto-scaling (2-5)
  - WebSocket/SignalR per real-time features
  - ForwardedHeaders configurati per proxy HTTPS

- **InsightLearn API**: REST API backend
  - 2 replicas con auto-scaling (2-5)
  - JWT authentication
  - Health checks attivi

### Data Layer
- **SQL Server 2022**: Database principale
  - StatefulSet con 20Gi persistent storage
  - Health checks configurati

- **Redis 7**: Cache e sessioni
  - 1Gi persistent storage
  - Connection pooling ottimizzato

- **Elasticsearch 8.11**: Search engine
  - 5Gi persistent storage
  - Heap size: 1GB

### Networking & Security
- **Nginx Reverse Proxy**: Sul host Debian
  - Ports: 80 (redirect) → 443 (SSL)
  - Certificato TLS autofirmato (365 giorni)
  - WebSocket upgrade support
  - Security headers configurati

- **Kubernetes Ingress**: nginx-ingress-controller
  - Host: insightlearn.local
  - Routing interno al cluster

- **NodePort Services**: Accesso esterno
  - web-service-nodeport: 31080
  - api-service-nodeport: 31081

## File e Directory Principali

### Kubernetes Manifests
```
k8s/
├── 00-namespace.yaml              # Namespace insightlearn
├── 01-secrets.yaml                # Passwords, JWT keys
├── 02-configmap.yaml              # App configuration
├── 03-sqlserver-statefulset.yaml # SQL Server
├── 04-redis-deployment.yaml       # Redis cache
├── 05-elasticsearch-deployment.yaml # Elasticsearch
├── 06-api-deployment.yaml         # API backend
├── 07-web-deployment.yaml         # Web frontend
├── 08-ingress.yaml                # Ingress routing
└── 09-nodeport-services.yaml      # External access
```

### Scripts Disponibili
```bash
# Build Docker images
./k8s/build-images.sh

# Deploy to Kubernetes
./k8s/deploy.sh

# Check deployment status
./k8s/status.sh

# Remove deployment
./k8s/undeploy.sh

# Setup HTTPS access
./k8s/setup-https-access.sh
```

### Nginx Configuration
```
/etc/nginx/sites-available/insightlearn  # Main config
/etc/nginx/ssl/insightlearn/tls.crt     # SSL certificate
/etc/nginx/ssl/insightlearn/tls.key     # SSL private key
```

## Comandi Utili

### Verifica Stato
```bash
# Pods
kubectl get pods -n insightlearn

# Services
kubectl get services -n insightlearn

# Logs Web
kubectl logs -n insightlearn -l app=insightlearn-web -f

# Logs API
kubectl logs -n insightlearn -l app=insightlearn-api -f

# Eventi
kubectl get events -n insightlearn --sort-by='.lastTimestamp'
```

### Gestione Nginx
```bash
# Status
sudo systemctl status nginx

# Restart
sudo systemctl restart nginx

# Test configuration
sudo nginx -t

# Logs
sudo tail -f /var/log/nginx/insightlearn-access.log
sudo tail -f /var/log/nginx/insightlearn-error.log
```

### Accesso Database
```bash
# Port forward SQL Server
kubectl port-forward -n insightlearn sqlserver-0 1433:1433

# Connect with sqlcmd
kubectl exec -it sqlserver-0 -n insightlearn -- /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'ElevateLearning2024StrongPass!' -C
```

## Configurazioni Importanti

### Secrets (Kubernetes)
```yaml
mssql-sa-password: "ElevateLearning2024StrongPass!"
jwt-secret-key: "ElevateLearning2024SecureJwtSigningKey123456789!"
```

### Endpoints
- **Web UI**: https://192.168.1.103
- **API**: https://192.168.1.103/api
- **Health Check**: https://192.168.1.103/api/health

### Connection Strings (interno ai pod)
```
SQL Server: Server=sqlserver-service;Database=InsightLearnDb;User=sa;Password=...
Redis: redis-service:6379
Elasticsearch: http://elasticsearch-service:9200
```

## Troubleshooting

### Problema: Connection Issue / WebSocket Error
**Sintomo**: "We're having trouble connecting" nel browser

**Soluzione**:
1. Verifica nginx logs: `sudo tail -f /var/log/nginx/insightlearn-error.log`
2. Verifica pod logs: `kubectl logs -n insightlearn -l app=insightlearn-web`
3. Test diretta ai pod: `curl http://192.168.49.2:31080`

### Problema: 502 Bad Gateway
**Causa**: Pod non raggiungibili da nginx

**Soluzione**:
```bash
# Verifica pods running
kubectl get pods -n insightlearn

# Verifica NodePort services
kubectl get svc -n insightlearn | grep nodeport

# Test connettività minikube
ping 192.168.49.2
curl http://192.168.49.2:31080
```

### Problema: Certificato SSL Non Valido
**Causa**: Certificato autofirmato

**Soluzione**: Nel browser, clicca "Advanced" e "Proceed to site" oppure installa certificato valido:
```bash
# Genera nuovo certificato con CA interna
openssl req -new -key /etc/nginx/ssl/insightlearn/tls.key -out /tmp/insightlearn.csr
# Invia CSR alla tua CA per firma
```

### Problema: Pods in CrashLoopBackOff
**Causa**: Errori di configurazione o dipendenze non pronte

**Soluzione**:
```bash
# Verifica logs
kubectl logs <pod-name> -n insightlearn --previous

# Describe pod per eventi
kubectl describe pod <pod-name> -n insightlearn

# Restart deployment
kubectl rollout restart deployment/<deployment-name> -n insightlearn
```

## Aggiornamento Applicazione

### Rolling Update
```bash
# 1. Ricostruisci immagine
cd /home/mpasqui/kubernetes/Insightlearn
docker build -f Dockerfile.web -t insightlearn/web:v1.2 .

# 2. Carica in minikube
minikube image load insightlearn/web:v1.2

# 3. Update deployment
kubectl set image deployment/insightlearn-web web=insightlearn/web:v1.2 -n insightlearn

# 4. Monitora rollout
kubectl rollout status deployment/insightlearn-web -n insightlearn
```

### Rollback
```bash
kubectl rollout undo deployment/insightlearn-web -n insightlearn
```

## Backup

### Database
```bash
# Exec nel pod SQL Server
kubectl exec -it sqlserver-0 -n insightlearn -- /bin/bash

# Backup database
/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'YourPassword' -Q "BACKUP DATABASE InsightLearnDb TO DISK = '/var/opt/mssql/backup/InsightLearn_$(date +%Y%m%d).bak'" -C
```

### Configurazioni
```bash
# Backup manifests
tar czf ~/backups/k8s-manifests-$(date +%Y%m%d).tar.gz k8s/

# Backup nginx config
sudo cp /etc/nginx/sites-available/insightlearn ~/backups/nginx-config-$(date +%Y%m%d).conf

# Backup certificati
sudo tar czf ~/backups/ssl-certs-$(date +%Y%m%d).tar.gz /etc/nginx/ssl/insightlearn/
```

## Monitoring & Logs

### Application Logs
```bash
# Web logs
kubectl logs -n insightlearn -l app=insightlearn-web --tail=100

# API logs
kubectl logs -n insightlearn -l app=insightlearn-api --tail=100

# SQL Server logs
kubectl logs -n insightlearn sqlserver-0 --tail=100
```

### Nginx Logs
```bash
# Access logs
sudo tail -f /var/log/nginx/insightlearn-access.log

# Error logs
sudo tail -f /var/log/nginx/insightlearn-error.log

# Grep per errori
sudo grep "error" /var/log/nginx/insightlearn-error.log | tail -20
```

### Resource Usage
```bash
# Pod resources
kubectl top pods -n insightlearn

# Node resources
kubectl top nodes

# Storage
kubectl get pvc -n insightlearn
```

## Performance Tuning

### Aumentare Risorse SQL Server
Modifica `k8s/03-sqlserver-statefulset.yaml`:
```yaml
resources:
  requests:
    memory: "4Gi"
    cpu: "2000m"
  limits:
    memory: "8Gi"
    cpu: "4000m"
```

### Scaling Manuale
```bash
# Scale web pods
kubectl scale deployment insightlearn-web -n insightlearn --replicas=3

# Scale API pods
kubectl scale deployment insightlearn-api -n insightlearn --replicas=3
```

## Sicurezza

### Raccomandazioni Produzione
1. ✅ Cambia password in `01-secrets.yaml`
2. ✅ Usa certificato SSL valido (non self-signed)
3. ⚠️ Configura firewall (ufw/iptables)
4. ⚠️ Abilita audit logging
5. ⚠️ Configura backup automatici
6. ⚠️ Imposta rate limiting più restrittivo
7. ⚠️ Usa registry privato per immagini Docker

### Firewall Setup
```bash
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
sudo ufw enable
```

## Contatti e Supporto

- **Documentazione Completa**: `k8s/README.md`
- **Logs**: `/var/log/nginx/insightlearn-*.log`
- **Repository**: `/home/mpasqui/kubernetes/Insightlearn`

## Note Finali

- **IP Host**: 192.168.1.103
- **IP Minikube**: 192.168.49.2
- **Namespace K8s**: insightlearn
- **Certificato SSL**: Valido 365 giorni (generato: 2025-09-30)
- **Versione .NET**: 8.0
- **Versione SQL Server**: 2022 Developer

**Status**: ✅ Deployment attivo e funzionante
**Ultimo update**: 2025-09-30 23:20

---

Per domande o problemi, consulta i logs o esegui `./k8s/status.sh` per un report completo dello stato del sistema.
