# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

**InsightLearn WASM** è una piattaforma LMS enterprise completa con frontend Blazor WebAssembly e backend ASP.NET Core.

**Versione corrente**: `1.4.22-dev` (definita in [Directory.Build.props](/Directory.Build.props))
**Stack**: .NET 8, Blazor WebAssembly, ASP.NET Core Web API, C# 12

### Architettura Soluzione

La solution [InsightLearn.WASM.sln](/InsightLearn.WASM.sln) è organizzata in 4 progetti:

1. **InsightLearn.Core** - Domain entities, interfaces, DTOs (layer condiviso)
2. **InsightLearn.Infrastructure** - Repository implementations, DbContext, external services
3. **InsightLearn.Application** - ASP.NET Core Web API backend
4. **InsightLearn.WebAssembly** - Blazor WebAssembly frontend (client-side)

### ⚠️ Problemi Noti Critici

1. **Program.cs mancante originariamente**
   - [src/InsightLearn.Application/Program.cs](/src/InsightLearn.Application/Program.cs) è stato **creato manualmente**
   - Il progetto originale era configurato come library (SDK: Microsoft.NET.Sdk)
   - Ora è configurato come Web app (SDK: Microsoft.NET.Sdk.Web, OutputType: Exe)
   - Se rebuild fallisce con "Entry point not found", verificare che Program.cs esista

2. **Dockerfile.web build failure**
   - [Dockerfile.web](/Dockerfile.web) fallisce con `NETSDK1082: no runtime pack for browser-wasm`
   - Problema noto di .NET SDK con RuntimeIdentifier 'browser-wasm' in container
   - **Workaround**: usare solo Dockerfile per API, deployare Web separatamente

3. **Rocky Linux 10: Podman vs Docker**
   - Su Rocky Linux 10, il sistema usa **Podman** nativo, non Docker
   - Gli script [k8s/build-images.sh](/k8s/build-images.sh) assumono Docker (non funzionano)
   - **Configurazione minikube richiesta**:
     - Driver: `podman` (non docker)
     - Runtime: `cri-o`
     - Base image: `gcr.io/k8s-minikube/kicbase-rocky:v0.0.48` (Rocky 10 kicbase)
     - Risorse: `--memory=9216 --cpus=6` (9GB RAM, 6 CPU)

## Build e Deploy

### Comandi Build Locali

```bash
# Build completa solution
dotnet build InsightLearn.WASM.sln

# Build solo API (funzionante)
dotnet build src/InsightLearn.Application/InsightLearn.Application.csproj

# Build WebAssembly (potrebbe fallire in Docker)
dotnet build src/InsightLearn.WebAssembly/InsightLearn.WebAssembly.csproj

# Publish API
dotnet publish src/InsightLearn.Application/InsightLearn.Application.csproj \
  -c Release -o ./publish

# Run API locale (porta 5000)
dotnet run --project src/InsightLearn.Application/InsightLearn.Application.csproj
```

### Deployment Docker Compose (Raccomandato)

```bash
# 1. Configura ambiente
cp .env.example .env
# Modifica .env con password sicure

# 2. Genera certificati SSL
mkdir -p nginx/certs
openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
  -keyout nginx/certs/tls.key -out nginx/certs/tls.crt \
  -subj "/C=IT/O=InsightLearn/CN=localhost"

# 3. Build SOLO API (Web ha problemi)
docker-compose build api

# 4. Start tutti i servizi (API + databases + monitoring)
docker-compose up -d sqlserver mongodb redis elasticsearch \
                    prometheus ollama jenkins api

# 5. Inizializza Ollama LLM
docker exec insightlearn-ollama ollama pull llama2

# 6. Verifica
docker-compose ps
curl http://localhost:7001/health
```

### Deployment Kubernetes (Rocky Linux con Podman)

```bash
# 1. Start minikube con Podman (Rocky 10 kicbase)
minikube config set rootless true
minikube start --driver=podman --container-runtime=cri-o \
               --memory=9216 --cpus=6 \
               --base-image=gcr.io/k8s-minikube/kicbase-rocky:v0.0.48

# 2. Enable Ingress
minikube addons enable ingress

# 3. Build images con Podman in minikube context
# NOTA: Non usare ./k8s/build-images.sh (assume Docker)
# Usare Podman direttamente:
eval $(minikube podman-env)
podman build -t insightlearn/api:latest -f Dockerfile .

# 4. Load in minikube
minikube image load insightlearn/api:latest

# 5. Deploy manifests
kubectl apply -f k8s/

# 6. Verifica status
./k8s/status.sh
```

## Versioning e Build Metadata

Il versioning è gestito centralmente in [Directory.Build.props](/Directory.Build.props):

- **VersionPrefix**: `1.4.22` (semantic version Major.Minor.Patch)
- **VersionSuffix**: `dev` (default, rimosso in release)
- **Version finale**: `1.4.22-dev`

Build variables disponibili:
- `$(VERSION)` - da Directory.Build.props
- `$(GIT_COMMIT)` - short commit hash
- `$(BUILD_NUMBER)` - git commit count

Esempio modifica version:
```xml
<!-- Directory.Build.props -->
<VersionPrefix>1.5.0</VersionPrefix>
<VersionSuffix>beta</VersionSuffix>
```

## File Critici

### Configurazione Core

| File | Scopo |
|------|-------|
| [.env](/.env) | Password production (⚠️ MAI committare!) |
| [docker-compose.yml](/docker-compose.yml) | Stack completo 11 servizi |
| [Directory.Build.props](/Directory.Build.props) | Versioning centralizzato |
| [InsightLearn.WASM.sln](/InsightLearn.WASM.sln) | Visual Studio solution |

### Backend API

| File | Scopo |
|------|-------|
| [src/InsightLearn.Application/Program.cs](/src/InsightLearn.Application/Program.cs) | ⚠️ API entry point (creato manualmente) |
| [src/InsightLearn.Application/InsightLearn.Application.csproj](/src/InsightLearn.Application/InsightLearn.Application.csproj) | Project file (SDK.Web) |
| [src/InsightLearn.Infrastructure/Data/InsightLearnDbContext.cs](/src/InsightLearn.Infrastructure/Data/InsightLearnDbContext.cs) | EF Core DbContext |

### Docker

| File | Scopo | Stato |
|------|-------|-------|
| [Dockerfile](/Dockerfile) | API build | ✅ Funzionante |
| [Dockerfile.web](/Dockerfile.web) | Web WASM build | ❌ NETSDK1082 error |

## Regole Fondamentali

### API Endpoints

1. Prefisso **obbligatorio**: `/api/`
2. Base URL frontend: `builder.HostEnvironment.BaseAddress`
3. Usare `EndpointsConfig` per configurazione centralizzata
4. Health check: `/health` (per liveness probes)

### Sicurezza

1. **MAI** committare `.env` con password reali
2. Placeholder `YOUR_*` in file di config
3. Sostituire con env vars al deploy
4. TLS certs self-signed SOLO per dev

### Database Stack

| Database | Uso | Porta |
|----------|-----|-------|
| SQL Server 2022 | Dati relazionali principali | 1433 |
| MongoDB 7.0 | Video storage + chatbot messages | 27017 |
| Redis 7 | Cache + sessioni utente | 6379 |
| Elasticsearch 8.11 | Search engine | 9200 |

### AI/Chatbot

- **LLM Server**: Ollama (porta 11434)
- **Model**: llama2 (download: `ollama pull llama2`)
- **API endpoint**: `/api/chat/message`
- **Storage**: MongoDB collection `chatbot_messages`

## Testing Deployment

```bash
# Container health
docker-compose ps | grep healthy

# Database connectivity
docker exec insightlearn-sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "${MSSQL_SA_PASSWORD}" -Q "SELECT 1" -C

docker exec insightlearn-mongodb mongosh \
  -u admin -p "${MONGO_PASSWORD}" --eval "db.version()"

docker exec insightlearn-redis redis-cli \
  -a "${REDIS_PASSWORD}" ping

# API health
curl http://localhost:7001/health

# API info endpoint
curl http://localhost:7001/api/info

# Chatbot test
curl -X POST http://localhost:7001/api/chat/message \
  -H "Content-Type: application/json" \
  -d '{"message":"Hello","contactEmail":"test@example.com"}'

# Monitoring
curl http://localhost:3000/api/health  # Grafana
curl http://localhost:9091/-/healthy   # Prometheus (porta 9091!)
```

## Porte Servizi

| Servizio | Porta(e) | HTTPS |
|----------|----------|-------|
| Nginx Reverse Proxy | 80, 443 | ✅ |
| API | 7001 (HTTP), 7002 (HTTPS) | ✅ |
| Web | 7003 | ❌ |
| SQL Server | 1433 | ❌ |
| MongoDB | 27017 | ❌ |
| Redis | 6379 | ❌ |
| Elasticsearch | 9200 | ❌ |
| Ollama | 11434 | ❌ |
| Prometheus | **9091** (⚠️ non 9090!) | ❌ |
| Grafana | 3000 | ❌ |
| Jenkins | 8080, 50000 | ❌ |

**Nota**: Prometheus usa porta 9091 per evitare conflitto con systemd su porta 9090.

## Credenziali Default

### Application Admin
- URL: https://localhost
- Email: `admin@insightlearn.cloud`
- Password: da file `.env` (`ADMIN_PASSWORD`)

### Services
- **Grafana**: admin / admin
- **SQL Server**: sa / `${MSSQL_SA_PASSWORD}`
- **MongoDB**: admin / `${MONGO_PASSWORD}`
- **Redis**: password: `${REDIS_PASSWORD}`

## Scripts Kubernetes

| Script | Funzione |
|--------|----------|
| [k8s/build-images.sh](/k8s/build-images.sh) | Build Docker images con versioning |
| [k8s/deploy.sh](/k8s/deploy.sh) | Deploy completo K8s |
| [k8s/status.sh](/k8s/status.sh) | Status pods/services |
| [k8s/undeploy.sh](/k8s/undeploy.sh) | Remove deployment |

⚠️ **Rocky Linux**: Gli script assumono Docker, sostituire con `podman` manualmente.

## Note per Claude Code

Quando lavori con questa repository:

1. **Leggere SEMPRE questo file** all'inizio del task
2. **Verificare versione** in [Directory.Build.props](/Directory.Build.props) (non hardcodare)
3. **Program.cs esiste?** Se manca in src/InsightLearn.Application/, il build fallirà
4. **Non usare Dockerfile.web** - ha un bug noto (NETSDK1082)
5. **Password da .env** - non committare mai password reali
6. **Prometheus porta 9091** - non 9090 (conflitto systemd)
7. **Rocky Linux 10 = Podman + kicbase-rocky** - non Docker standard
   - Minikube: `--driver=podman --base-image=gcr.io/k8s-minikube/kicbase-rocky:v0.0.48`
   - Risorse: `--memory=9216 --cpus=6` (9GB RAM, 6 CPU)
   - Build: `eval $(minikube podman-env)` poi `podman build`
8. **Test chatbot** dopo modifiche AI: `docker exec insightlearn-ollama ollama list`

## Documentazione Aggiuntiva

- [DEPLOYMENT-COMPLETE-GUIDE.md](/DEPLOYMENT-COMPLETE-GUIDE.md) - Guida deploy step-by-step
- [DOCKER-COMPOSE-GUIDE.md](/DOCKER-COMPOSE-GUIDE.md) - Docker Compose dettagliato
- [k8s/README.md](/k8s/README.md) - Kubernetes deployment

## Repository

- **URL**: https://github.com/marypas74/InsightLearn_WASM
- **Issues**: https://github.com/marypas74/InsightLearn_WASM/issues
- **Maintainer**: marcello.pasqui@gmail.com
