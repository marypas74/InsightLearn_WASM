# 🎉 InsightLearn Kubernetes Deployment Status

**Data**: 2025-11-03 23:58  
**Piattaforma**: Rocky Linux 10 + minikube + Podman (rootless)  
**Versione**: InsightLearn v1.4.22-dev

---

## ✅ DEPLOYMENT COMPLETATO CON SUCCESSO

### Pods in Running
| Servizio | Status | Ready | IP | Note |
|----------|--------|-------|-----|------|
| **SQL Server** | ✅ Running | 1/1 | 10.244.0.53 | Database principale |
| **Redis** | ✅ Running | 1/1 | 10.244.0.160 | Cache e sessioni |
| **MongoDB** | ✅ Running | 1/1 | 10.244.0.178 | Video e chatbot |
| **API** | ✅ Running | 1/1 | 10.244.0.221 | **TESTATO E FUNZIONANTE** |
| **Ollama** | ✅ Running | 2/2 | - | LLM + nginx TLS proxy |

### Test API Eseguiti con Successo
```bash
curl http://127.0.0.1:40249/health
# Response: "Healthy" ✅

curl http://127.0.0.1:40249/
# Response: {"application":"InsightLearn API","version":"1.4.29","status":"running"} ✅
```

---

## 🔧 PROBLEMI RISOLTI

### 1. Secrets TLS Mancanti
- **Problema**: Pods API e Ollama bloccati in `ContainerCreating`
- **Causa**: Secrets `api-tls-cert` e `ollama-tls` non esistevano
- **Soluzione**: Creati da `nginx/certs/tls.crt` e `tls.key`
```bash
kubectl create secret tls api-tls-cert --cert=nginx/certs/tls.crt --key=nginx/certs/tls.key -n insightlearn
kubectl create secret tls ollama-tls --cert=nginx/certs/tls.crt --key=nginx/certs/tls.key -n insightlearn
```

### 2. Insufficient Memory
- **Problema**: Pods non schedulati per memoria insufficiente
- **Soluzione**: Ridotte repliche da 2 a 1 per tutti i deployment
```bash
kubectl scale deployment insightlearn-api --replicas=1 -n insightlearn
```

### 3. API CrashLoopBackOff (Microsoft.AspNetCore Assembly Missing)
- **Problema**: `FileNotFoundException: Microsoft.AspNetCore, Version=8.0.0.0`
- **Causa 1**: Dockerfile usava `mcr.microsoft.com/dotnet/sdk:8.0` invece di `aspnet:8.0`
- **Causa 2**: `InsightLearn.Application.runtimeconfig.json` riferiva solo `Microsoft.NETCore.App`
- **Soluzione**: 
  - Dockerfile: Cambiato base image da `sdk:8.0` a `aspnet:8.0`
  - runtimeconfig.json: Aggiunto `Microsoft.AspNetCore.App` ai frameworks
```json
"frameworks": [
  {"name": "Microsoft.NETCore.App", "version": "8.0.0"},
  {"name": "Microsoft.AspNetCore.App", "version": "8.0.0"}
]
```

### 4. API Listening on Wrong Port
- **Problema**: Readiness probe falliva su porta 80 (app ascoltava su 5000)
- **Soluzione**: Aggiunta variabile `ASPNETCORE_URLS=http://+:80;https://+:443` al deployment

### 5. Podman Short-Name Image Resolution
- **Problema**: `ImageInspectError` per immagini con nomi brevi (redis:7-alpine, mongo:7.0, etc.)
- **Causa**: Podman richiede fully-qualified registry names
- **Soluzione**: Aggiornati tutti i manifests:
  - `redis:7-alpine` → `docker.io/library/redis:7-alpine`
  - `mongo:7.0` → `docker.io/library/mongo:7.0`
  - `ollama/ollama:latest` → `docker.io/ollama/ollama:latest`
  - `nginx:alpine` → `docker.io/library/nginx:alpine`
  - `curlimages/curl:latest` → `docker.io/curlimages/curl:latest`

---

## ⚠️ LIMITAZIONE NOTA: Ingress Controller

### Problema
L'Ingress controller è bloccato in `ContainerCreating` dopo 75+ minuti:
```
modprobe: ERROR: could not insert 'ip_tables': Operation not permitted
iptables v1.8.7 (legacy): can't initialize iptables table `nat': Table does not exist
```

### Causa
**Incompatibilità con Podman rootless mode**:
- Ingress controller richiede permessi per configurare iptables
- Podman rootless non può modificare iptables del kernel
- Limitazione nota di minikube + Podman rootless

### Soluzioni Alternative

#### OPZIONE 1: NodePort (Già Configurato) ✅
```bash
# L'API è già accessibile via NodePort sulla porta 31081
minikube service api-service-nodeport -n insightlearn --url
# Output: http://127.0.0.1:40249

# Test:
curl http://127.0.0.1:40249/health  # ✅ Funziona
curl http://127.0.0.1:40249/        # ✅ Funziona
```

#### OPZIONE 2: minikube tunnel (Richiede sudo)
```bash
# Crea tunnel per esporre LoadBalancer services
sudo minikube tunnel

# Poi configura /etc/hosts:
echo "$(minikube ip) www.insightlearn.cloud" | sudo tee -a /etc/hosts

# Accedi a: http://www.insightlearn.cloud
```

#### OPZIONE 3: Passare a Docker (Non Consigliato)
- Richiede riconfigurazione completa
- Rocky Linux usa Podman come default

---

## 📊 RISORSE UTILIZZATE

### Minikube Configuration
```
Driver: podman
Container Runtime: cri-o
Memory: 9216 MB (9 GB)
CPUs: 6 cores
```

### Pod Resource Allocation
| Pod | Requests (CPU/Mem) | Limits (CPU/Mem) |
|-----|-------------------|------------------|
| SQL Server | 500m / 2Gi | 2000m / 4Gi |
| MongoDB | 500m / 512Mi | 2000m / 2Gi |
| Redis | 100m / 256Mi | 500m / 512Mi |
| API | 250m / 512Mi | 1000m / 1Gi |
| Ollama (main) | 500m / 3Gi | 1500m / 5Gi |
| Ollama (nginx) | 100m / 64Mi | 200m / 128Mi |

---

## 🚀 ACCESSO ALL'APPLICAZIONE

### API Endpoints (via NodePort)
```bash
# 1. Ottieni URL NodePort
minikube service api-service-nodeport -n insightlearn --url
# Output: http://127.0.0.1:40249 (la porta può variare)

# 2. Test endpoints
curl http://127.0.0.1:40249/health         # Health check
curl http://127.0.0.1:40249/                # App info
curl http://127.0.0.1:40249/api/info       # API info
curl http://127.0.0.1:40249/swagger        # Swagger UI (se abilitato)
```

### Monitoraggio Kubernetes
```bash
# Status pods
kubectl get pods -n insightlearn

# Logs API
kubectl logs -f -n insightlearn deployment/insightlearn-api

# Accesso shell API
kubectl exec -it -n insightlearn deployment/insightlearn-api -- /bin/bash

# Metriche (richiede metrics-server)
kubectl top pods -n insightlearn
```

---

## 📝 FILE MODIFICATI

1. **Dockerfile** - Base image cambiata da sdk a aspnet
2. **InsightLearn.Application.runtimeconfig.json** - Aggiunto Microsoft.AspNetCore.App
3. **k8s/06-api-deployment.yaml** - Aggiunto ASPNETCORE_URLS, immagine fully-qualified
4. **k8s/04-redis-deployment.yaml** - Immagine fully-qualified
5. **k8s/13-mongodb-statefulset.yaml** - Immagine fully-qualified
6. **k8s/12-ollama-deployment.yaml** - Immagini nginx e curl fully-qualified
7. **k8s/08-ingress.yaml** - Host cambiato a www.insightlearn.cloud
8. **k8s/01-secrets.yaml** - Password generate con openssl rand

---

## ✨ PROSSIMI PASSI RACCOMANDATI

### 1. Deploy Frontend (WASM o Blazor Server)
- Build immagini Web/WASM con Podman
- Correggere Dockerfile.web (attualmente ha errori NETSDK1082)
- Deploy con replicas > 0

### 2. Fix Elasticsearch (Opzionale)
- Attualmente in Init:CrashLoopBackOff
- Non critico per funzionamento base

### 3. Configurare DNS Reale
- Puntare www.insightlearn.cloud all'IP pubblico del server
- Configurare Ingress con certificati Let's Encrypt

### 4. Backup Automatico
- Configurare CronJobs per backup database
- PersistentVolumes già configurati

### 5. Monitoring Production
- Prometheus: Deploy in namespace insightlearn
- Grafana: Configurare dashboards

---

## 📞 SUPPORTO

Repository: https://github.com/marypas74/InsightLearn_WASM  
Email: marcello.pasqui@gmail.com

---

**Generato**: 2025-11-03 23:58 UTC  
**Tool**: Claude Code v4.5
