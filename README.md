# InsightLearn WASM

Enterprise Learning Management System con frontend Blazor WebAssembly e backend ASP.NET Core.

**Versione**: 1.6.0-dev
**Stack**: .NET 8, Blazor WebAssembly, ASP.NET Core Web API, SQL Server, MongoDB, Redis
**Deploy**: Kubernetes su Rocky Linux 10 con Podman
**Monitoring**: Grafana + Prometheus con dashboards personalizzati

## 🎯 Caratteristiche Principali

### Frontend & UX
- **Blazor WebAssembly** con componenti interattivi e routing client-side
- **VideoUpload Component** con drag & drop, validazione file, progress tracking
- **VideoPlayer Component** con streaming MongoDB GridFS
- **AI Chatbot Widget** con Ollama (qwen2:0.5b) per supporto real-time
- **Cookie Consent Wall** GDPR-compliant con UI moderna
- **Responsive Design** mobile-first con breakpoints ottimizzati

### Backend & API
- **ASP.NET Core 8** con Minimal APIs pattern
- **Endpoints Database-Driven**: 50+ endpoint configurabili da SQL Server senza rebuild
- **MongoDB GridFS**: Video storage con GZip compression (20-40% riduzione dimensione)
- **Automatic Database Migrations**: EF Core migrations al startup (no downtime)
- **JWT Authentication**: Token-based auth con refresh token support
- **Health Checks**: `/health` endpoint per Kubernetes liveness/readiness probes

### Database & Storage
- **SQL Server 2022**: Dati relazionali (utenti, corsi, iscrizioni, chatbot messages)
- **MongoDB 7.0**: Video storage GridFS con compression, metadata indexing
- **Redis 7**: Cache distribuita, session storage
- **Elasticsearch 8.11**: Full-text search (configurato)

### AI & Machine Learning
- **Ollama LLM Server**: Modello qwen2:0.5b (1.7s avg response time)
- **Chatbot persistente**: Conversazioni salvate in SQL Server
- **Context-aware responses**: History-based conversation flow

### Monitoring & Observability
- **Grafana Dashboards**: "InsightLearn Platform Monitoring" con metriche real-time
  - Pod CPU/Memory usage
  - Network I/O statistics
  - Pod health status
  - Service availability
- **Prometheus**: Metrics collection con 30s scrape interval
- **Service Watchdog**: Auto-healing system che monitora e riavvia servizi failed
- **Persistent Port-Forwarding**: Script auto-restart per Grafana (http://localhost:3000)

### Security & Compliance
- ✅ **Security Patches Applied** (2025-01-08):
  - CVE-2024-43483 (HIGH) - Microsoft.Extensions.Caching.Memory patched
  - CVE-2024-43485 (HIGH) - System.Text.Json patched
  - Tutte le dipendenze transitive aggiornate
- **GDPR Compliance**: Cookie consent wall, privacy policy
- **TLS/SSL**: Certificati self-signed per dev, Let's Encrypt ready per prod
- **JWT Secret Rotation**: Configurabile via environment variables

### Deployment & DevOps
- **Kubernetes-Native**: StatefulSets, Deployments, Services, ConfigMaps, Secrets
- **Horizontal Pod Autoscaling**: CPU/Memory-based scaling (min 1, max 5 replicas)
- **Health Probes**: Liveness, Readiness, Startup probes per tutti i servizi
- **Rolling Updates**: Zero-downtime deployments
- **Podman Support**: Native container runtime su Rocky Linux 10

## Quick Start

### Prerequisiti

- .NET 8 SDK
- Podman (su Rocky Linux) o Docker
- kubectl
- minikube
- Git

### Setup Automatico (Raccomandato)

```bash
# 1. Clone repository
git clone https://github.com/marypas74/InsightLearn_WASM.git
cd InsightLearn_WASM

# 2. Setup iniziale (crea .env, build, avvia minikube, crea secrets)
./setup.sh

# 3. Modifica .env con password sicure
nano .env

# 4. Deploy su Kubernetes
./deploy.sh

# 5. Avvia port-forwards
./start-all.sh
```

### 🌐 Accesso Applicazione

Dopo il deployment, i servizi sono accessibili su:

#### Applicazione
- **Frontend WASM**: http://localhost:8080 (o https://wasm.insightlearn.cloud via Cloudflare)
- **Backend API**: http://localhost:31081 (NodePort) o http://localhost:8081 (port-forward)
- **API Health**: http://localhost:31081/health
- **Swagger**: http://localhost:31081/swagger

#### Monitoring & Admin
- **Grafana**: http://localhost:3000 (port-forward persistente) o http://localhost:31300 (NodePort)
  - Login: `admin/admin` (cambiare al primo accesso)
  - Dashboard: http://localhost:3000/d/insightlearn-main/insightlearn-platform-monitoring
- **Prometheus**: http://localhost:9091 (⚠️ porta 9091, non 9090)

#### Port-Forward Persistente

Per avere Grafana sempre accessibile su http://localhost:3000:

```bash
# Avvia port-forward persistente con auto-restart
./k8s/grafana-port-forward-persistent.sh &
```

#### Service Watchdog

Per monitorare e riavviare automaticamente i servizi failed:

```bash
# Avvia watchdog in background
./k8s/service-watchdog.sh &

# Verifica log
tail -f /tmp/insightlearn-watchdog.log
```

#### Cloudflare Tunnel (Produzione)

Per esporre l'applicazione su Internet con HTTPS:

```bash
# Installa cloudflared
# https://developers.cloudflare.com/cloudflare-one/connections/connect-apps/install-and-setup/installation

# Avvia tunnel
cloudflared tunnel run insightlearn
```

**Accesso pubblico**: https://wasm.insightlearn.cloud

## Architettura

### Progetti

La solution [InsightLearn.WASM.sln](InsightLearn.WASM.sln) è organizzata in 4 progetti:

1. **InsightLearn.Core** - Domain entities, interfaces, DTOs
2. **InsightLearn.Infrastructure** - Repository implementations, DbContext
3. **InsightLearn.Application** - ASP.NET Core Web API backend
4. **InsightLearn.WebAssembly** - Blazor WebAssembly frontend

### Endpoints Database-Driven

Tutti gli endpoint API sono memorizzati nel database (`SystemEndpoints` table) per permettere modifiche senza rebuild:

- **Backend**: `/api/system/endpoints` ritorna tutti gli endpoint attivi
- **Caching**: MemoryCache con expiration 60 minuti
- **Frontend**: `EndpointConfigurationService` carica da API con fallback a appsettings.json
- **Seed Data**: 50+ endpoint predefiniti in 9 categorie

Categorie endpoint: Auth, Courses, Categories, Enrollments, Users, Dashboard, Reviews, Payments, Chat

### Stack Servizi

| Servizio | Porta | Descrizione |
|----------|-------|-------------|
| WASM Frontend | 8080 | Blazor WebAssembly client |
| API Backend | 8081 | ASP.NET Core Web API |
| SQL Server | 1433 | Dati relazionali principali |
| MongoDB | 27017 | Video storage, chat messages |
| Redis | 6379 | Cache, sessioni |
| Ollama | 11434 | LLM server per chatbot |

## Setup Manuale

### 1. Configura Ambiente

```bash
# Copia .env.example
cp .env.example .env

# Modifica con password sicure
nano .env
```

### 2. Build Solution

```bash
# Restore packages
dotnet restore InsightLearn.WASM.sln

# Build
dotnet build InsightLearn.WASM.sln -c Release
```

### 3. Avvia Minikube

```bash
# Rocky Linux con Podman
minikube config set rootless true
minikube start --driver=podman --container-runtime=cri-o \
               --memory=14336 --cpus=6 \
               --base-image=gcr.io/k8s-minikube/kicbase-rocky:v0.0.48

# Abilita Ingress
minikube addons enable ingress
```

### 4. Crea Kubernetes Namespace e Secrets

```bash
# Crea namespace
kubectl create namespace insightlearn

# Source environment variables
source .env

# Crea secrets
kubectl create secret generic sqlserver-secret \
  --from-literal=SA_PASSWORD="${MSSQL_SA_PASSWORD}" \
  -n insightlearn

kubectl create secret generic mongodb-secret \
  --from-literal=MONGO_INITDB_ROOT_PASSWORD="${MONGO_PASSWORD}" \
  -n insightlearn

kubectl create secret generic redis-secret \
  --from-literal=REDIS_PASSWORD="${REDIS_PASSWORD}" \
  -n insightlearn
```

### 5. Build e Deploy Docker Images

```bash
# Set minikube podman environment
eval $(minikube podman-env)

# Build API image
podman build -t localhost/insightlearn/api:latest -f Dockerfile .

# Load in minikube
minikube image load localhost/insightlearn/api:latest

# Deploy Kubernetes manifests
kubectl apply -f k8s/ -n insightlearn

# Wait for pods
kubectl wait --for=condition=ready pod -l app=sqlserver -n insightlearn --timeout=300s
kubectl wait --for=condition=ready pod -l app=api -n insightlearn --timeout=300s
kubectl wait --for=condition=ready pod -l component=wasm -n insightlearn --timeout=300s
```

### 6. Start Port-Forwards

```bash
# WASM
kubectl port-forward -n insightlearn svc/wasm-blazor-webassembly-service 8080:80 &

# API
kubectl port-forward -n insightlearn svc/api-service 8081:80 &
```

## Testing

### API Health Check

```bash
curl http://localhost:8081/health
```

### Chatbot Test

```bash
curl -X POST http://localhost:8081/api/chat/message \
  -H "Content-Type: application/json" \
  -d '{"message":"Ciao","contactEmail":"test@example.com"}'
```

### System Endpoints

```bash
curl http://localhost:8081/api/system/endpoints | jq
```

## Troubleshooting

### Browser Cache

Se il frontend mostra errori dopo un deploy:

1. **Hard Refresh**: `Ctrl + Shift + R` (Windows/Linux) o `Cmd + Shift + R` (Mac)
2. **Clear Cache**: F12 → Application → Clear storage
3. **Incognito Mode**: `Ctrl + Shift + N`

### Kubernetes Pods

```bash
# Check pod status
kubectl get pods -n insightlearn

# Check pod logs
kubectl logs -n insightlearn <pod-name>

# Describe pod
kubectl describe pod -n insightlearn <pod-name>

# Restart deployment
kubectl rollout restart deployment/<deployment-name> -n insightlearn
```

### Port-Forward Issues

```bash
# Kill existing port-forwards
pkill -f "kubectl port-forward"

# Restart
./start-all.sh
```

## Documentazione

- [CLAUDE.md](CLAUDE.md) - Guida per Claude Code con architettura dettagliata
- [DEPLOYMENT-COMPLETE-GUIDE.md](DEPLOYMENT-COMPLETE-GUIDE.md) - Guida deployment completa
- [DOCKER-COMPOSE-GUIDE.md](DOCKER-COMPOSE-GUIDE.md) - Docker Compose setup
- [k8s/README.md](k8s/README.md) - Kubernetes manifests
- [NETWORK-REPORT-2025-11-06.md](/tmp/NETWORK-REPORT-2025-11-06.md) - Report network engineer

## Scripts Utility

| Script | Descrizione |
|--------|-------------|
| [setup.sh](setup.sh) | Setup iniziale completo (prerequisites, build, minikube, secrets) |
| [deploy.sh](deploy.sh) | Deploy su Kubernetes (build images, apply manifests) |
| [start-all.sh](start-all.sh) | Avvia port-forwards e mostra URL accesso |

## Credenziali Default

### Application
- **Admin Email**: admin@insightlearn.cloud
- **Admin Password**: da file `.env` (`ADMIN_PASSWORD`)

### Services
- **SQL Server**: sa / `${MSSQL_SA_PASSWORD}`
- **MongoDB**: admin / `${MONGO_PASSWORD}`
- **Redis**: `${REDIS_PASSWORD}`

## Sviluppo

### Struttura Directory

```
InsightLearn_WASM/
├── src/
│   ├── InsightLearn.Core/              # Domain entities, interfaces
│   ├── InsightLearn.Infrastructure/    # Repositories, DbContext
│   ├── InsightLearn.Application/       # ASP.NET Core Web API
│   └── InsightLearn.WebAssembly/       # Blazor WASM frontend
├── k8s/                                 # Kubernetes manifests
├── nginx/                               # Nginx config e certificati
├── docker-compose.yml                   # Stack completo
├── Dockerfile                           # API build
├── .env                                 # Environment variables (non committare!)
└── InsightLearn.WASM.sln               # Visual Studio solution
```

### Build Commands

```bash
# Build solution
dotnet build InsightLearn.WASM.sln

# Build specific project
dotnet build src/InsightLearn.Application/InsightLearn.Application.csproj

# Publish API
dotnet publish src/InsightLearn.Application/InsightLearn.Application.csproj \
  -c Release -o ./publish

# Run API locally
dotnet run --project src/InsightLearn.Application/InsightLearn.Application.csproj
```

### Versioning

Il versioning è gestito in [Directory.Build.props](Directory.Build.props):

```xml
<VersionPrefix>1.4.22</VersionPrefix>
<VersionSuffix>dev</VersionSuffix>
```

Versione finale: `1.4.22-dev`

## Contribuire

1. Fork il repository
2. Crea un branch per la feature: `git checkout -b feature/amazing-feature`
3. Commit le modifiche: `git commit -m 'feat: Add amazing feature'`
4. Push al branch: `git push origin feature/amazing-feature`
5. Apri una Pull Request

## Licenza

Proprietario: marcello.pasqui@gmail.com

## Repository

- **GitHub**: https://github.com/marypas74/InsightLearn_WASM
- **Issues**: https://github.com/marypas74/InsightLearn_WASM/issues
- **Maintainer**: marcello.pasqui@gmail.com
