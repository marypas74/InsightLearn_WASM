# CLAUDE.md - InsightLearn Blazor WebAssembly

Questo file fornisce guidance a Claude Code quando lavora con questa repository.

## Overview

Questa repository contiene **l'applicazione completa InsightLearn** con frontend, backend e infrastruttura.

**Versione**: v1.4.29-dev
**Stack**: .NET 8, Blazor WebAssembly + Blazor Server, C# 12

### Componenti Principali

1. **Frontend Blazor WebAssembly** - Client-side execution (`src/InsightLearn.WebAssembly/`)
2. **Backend API** - .NET 8 REST API (`src/InsightLearn.Application/`)
3. **Database Layer** - SQL Server, MongoDB, Redis, Elasticsearch
4. **AI/LLM** - Ollama con modello llama2 per chatbot
5. **Monitoring** - Grafana + Prometheus
6. **CI/CD** - Jenkins automation
7. **Reverse Proxy** - Nginx HTTPS

## Ripristino Rapido su Nuova Piattaforma

### Metodo 1: One-Click Deployment (Raccomandato)

```bash
# Clone repository
git clone https://github.com/marypas74/InsightLearn_WASM.git
cd InsightLearn_WASM

# Deploy automatico completo
./deploy-oneclick.sh

# L'applicazione sarà disponibile su:
# https://localhost (frontend + backend)
# http://localhost:3000 (Grafana monitoring)
# http://localhost:8080 (Jenkins CI/CD)
```

### Metodo 2: Docker Compose Manuale

```bash
# 1. Configura ambiente
cp .env.example .env
nano .env  # Configura password

# 2. Genera certificati SSL
mkdir -p nginx/certs
openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
  -keyout nginx/certs/tls.key -out nginx/certs/tls.crt \
  -subj "/C=IT/O=InsightLearn/CN=localhost"

# 3. Build e avvio
docker-compose build
docker-compose up -d

# 4. Inizializza database
docker exec insightlearn-ollama ollama pull llama2
docker exec -it insightlearn-mongodb mongosh -u admin -p PASSWORD << EOF
use insightlearn
db.createCollection("videos")
db.createCollection("chatbot_messages")
EOF

# 5. Verifica deployment
./test-chatbot.sh  # Test chatbot funzionante
```

### Metodo 3: Kubernetes

```bash
cd k8s/
./build-images.sh
./deploy.sh
./status.sh
```

## File Critici per il Ripristino

### Configurazione

- **[.env](/.env)** - Variabili d'ambiente (PASSWORD SICURE!)
- **[docker-compose.yml](/docker-compose.yml)** - Stack completo (11 servizi)
- **[config/appsettings.json](/config/appsettings.json)** - Configurazione .NET

### Frontend

- **[src/InsightLearn.WebAssembly/Program.cs](/src/InsightLearn.WebAssembly/Program.cs)** - Entry point WASM
- **[src/InsightLearn.WebAssembly/Models/Config/EndpointsConfig.cs](/src/InsightLearn.WebAssembly/Models/Config/EndpointsConfig.cs)** - API endpoints
- **[src/InsightLearn.WebAssembly/wwwroot/appsettings.json](/src/InsightLearn.WebAssembly/wwwroot/appsettings.json)** - Config runtime
- **[src/InsightLearn.WebAssembly/Components/ChatbotWidget.razor](/src/InsightLearn.WebAssembly/Components/ChatbotWidget.razor)** - Chatbot UI

### Backend

- **[src/InsightLearn.Application/Services/](/src/InsightLearn.Application/Services/)** - Business logic
- **[src/InsightLearn.Infrastructure/Data/InsightLearnDbContext.cs](/src/InsightLearn.Infrastructure/Data/InsightLearnDbContext.cs)** - EF Core context

### Docker & Deploy

- **[Dockerfile.wasm](/Dockerfile.wasm)** - Frontend WASM build
- **[Dockerfile](/Dockerfile)** - Backend API build
- **[Dockerfile.web](/Dockerfile.web)** - Blazor Server build
- **[deploy-oneclick.sh](/deploy-oneclick.sh)** - Deployment automatico
- **[test-chatbot.sh](/test-chatbot.sh)** - Test chatbot completo

### Monitoring

- **[monitoring/prometheus.yml](/monitoring/prometheus.yml)** - Metrics config
- **[monitoring/grafana-*.json](/monitoring/)** - 3 dashboard pre-configurate
- **[monitoring/grafana-provisioning-*.yml](/monitoring/)** - Auto-provisioning

## Regole Fondamentali

### Endpoint API

1. Tutti gli endpoint API DEVONO avere prefisso `api/`
2. HttpClient.BaseAddress usa `builder.HostEnvironment.BaseAddress`
3. Usare sempre `EndpointsConfig` invece di stringhe hardcoded
4. Test con Python prima del deploy per validare JSON response

### Segreti e Sicurezza

1. **MAI** committare file `.env` con password reali
2. Usare placeholder `YOUR_*` nei file di configurazione
3. Sostituire con variabili d'ambiente al deployment
4. Certificati SSL self-signed solo per sviluppo

### Database

1. SQL Server: database principale (relazionale)
2. MongoDB: video storage e chatbot messages
3. Redis: cache e sessioni utente
4. Elasticsearch: search engine

### Chatbot

1. Ollama serve il modello LLM (llama2)
2. MongoDB memorizza conversazioni
3. API endpoint: `/api/chat/message`
4. Widget frontend: `ChatbotWidget.razor`

## Verifica Deployment Completo

### Checklist

```bash
# 1. Tutti i container healthy
docker-compose ps | grep healthy

# 2. Database accessibili
docker exec insightlearn-sqlserver sqlcmd -S localhost -U sa -P PASSWORD -Q "SELECT 1"
docker exec insightlearn-mongodb mongosh -u admin -p PASSWORD --eval "db.version()"
docker exec insightlearn-redis redis-cli -a PASSWORD ping

# 3. API risponde
curl http://localhost:7001/health

# 4. Frontend carica
curl http://localhost:7003

# 5. Nginx HTTPS funziona
curl -k https://localhost/health

# 6. Chatbot funzionante
./test-chatbot.sh

# 7. Grafana dashboard
curl http://localhost:3000/api/health

# 8. Prometheus metrics
curl http://localhost:9090/-/healthy
```

### Test Funzionali

```bash
# Login admin
curl -k -X POST https://localhost/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@insightlearn.cloud","password":"PASSWORD"}'

# Chatbot message
curl -k -X POST https://localhost/api/chat/message \
  -H "Content-Type: application/json" \
  -d '{"message":"Ciao, come stai?","contactEmail":"test@example.com"}'

# Verifica risposta AI da Ollama
```

## Documentazione Completa

Per informazioni dettagliate, consulta:

### Guide Principali

- **[README.md](/README.md)** - Panoramica generale progetto
- **[DEPLOYMENT-COMPLETE-GUIDE.md](/DEPLOYMENT-COMPLETE-GUIDE.md)** - **⭐ Guida deployment completa step-by-step**
- **[DOCKER-COMPOSE-GUIDE.md](/DOCKER-COMPOSE-GUIDE.md)** - Guida Docker Compose dettagliata
- **[GITHUB-PUSH-SUCCESS.md](/GITHUB-PUSH-SUCCESS.md)** - Report push GitHub

### Guide Specifiche

- **[k8s/README.md](/k8s/README.md)** - Deployment Kubernetes
- **[docs/](/docs/)** - Documentazione tecnica WASM
- **[monitoring/](/monitoring/)** - Guide Grafana e Prometheus
- **[jenkins/](/jenkins/)** - Setup CI/CD

### Script Utili

- **[deploy-oneclick.sh](/deploy-oneclick.sh)** - Deploy automatico completo
- **[test-chatbot.sh](/test-chatbot.sh)** - Test chatbot con Ollama
- **[k8s/build-images.sh](/k8s/build-images.sh)** - Build Docker images
- **[k8s/deploy.sh](/k8s/deploy.sh)** - Deploy Kubernetes
- **[k8s/status.sh](/k8s/status.sh)** - Status deployment

## Accesso Applicazione

### Applicazione Principale

- **URL**: https://localhost
- **Admin**:
  - Email: `admin@insightlearn.cloud`
  - Password: (da `.env` file, default: `Admin123!Secure`)

### Servizi di Supporto

| Servizio | URL | Credenziali |
|----------|-----|-------------|
| Grafana | http://localhost:3000 | admin / admin |
| Prometheus | http://localhost:9090 | - |
| Jenkins | http://localhost:8080 | (vedi password iniziale) |
| API Diretta | http://localhost:7001 | - |
| Swagger API | http://localhost:7001/swagger | - |

### Porte Esposte

- **80, 443**: Nginx (HTTP/HTTPS)
- **1433**: SQL Server
- **6379**: Redis
- **9200**: Elasticsearch
- **27017**: MongoDB
- **7001, 7002**: API (HTTP/HTTPS)
- **7003**: Web (HTTP)
- **3000**: Grafana
- **9090**: Prometheus
- **8080, 50000**: Jenkins
- **11434**: Ollama LLM

## Stack Tecnologico Completo

### Frontend
- Blazor WebAssembly (.NET 8)
- Blazor Server (.NET 8)
- Bootstrap 5
- SignalR (real-time)

### Backend
- ASP.NET Core 8 Web API
- Entity Framework Core 8
- JWT Authentication
- Google OAuth 2.0

### Database
- SQL Server 2022 (relazionale)
- MongoDB 7.0 (NoSQL - video, chatbot)
- Redis 7 (cache, sessioni)
- Elasticsearch 8.11 (search)

### AI/LLM
- Ollama (LLM server)
- llama2 (modello AI default)
- Chatbot integration

### DevOps
- Docker & Docker Compose
- Kubernetes (minikube)
- Jenkins (CI/CD)
- Nginx (reverse proxy)

### Monitoring
- Prometheus (metrics)
- Grafana (dashboards)
- Serilog (logging)

## Supporto

- **Repository**: https://github.com/marypas74/InsightLearn_WASM
- **Issues**: https://github.com/marypas74/InsightLearn_WASM/issues
- **Email**: marcello.pasqui@gmail.com

## Note per Claude Code

Quando lavori con questa repository:

1. **Sempre leggere questo file prima** di iniziare qualsiasi task
2. **Utilizzare gli script automatici** (`deploy-oneclick.sh`, `test-chatbot.sh`)
3. **Verificare file `.env`** prima di deployment
4. **Consultare [DEPLOYMENT-COMPLETE-GUIDE.md](/DEPLOYMENT-COMPLETE-GUIDE.md)** per procedure complete
5. **Testare chatbot** con `./test-chatbot.sh` dopo modifiche AI/LLM
6. **Non modificare** placeholder `YOUR_*` senza documentazione
7. **Usare sempre** variabili d'ambiente per segreti
