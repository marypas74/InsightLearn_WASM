# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

**InsightLearn WASM** è una piattaforma LMS enterprise completa con frontend Blazor WebAssembly e backend ASP.NET Core.

**Versione corrente**: `1.6.0-dev` (definita in [Directory.Build.props](/Directory.Build.props))
**Stack**: .NET 8, Blazor WebAssembly, ASP.NET Core Web API, C# 12

✅ **Versioning Unificato**: [Program.cs](src/InsightLearn.Application/Program.cs) legge la versione dinamicamente dall'assembly usando `System.Reflection`, sincronizzato con [Directory.Build.props](Directory.Build.props). Tutti i riferimenti ora usano `1.6.0-dev`.

### Architettura Soluzione

La solution [InsightLearn.WASM.sln](/InsightLearn.WASM.sln) è organizzata in 4 progetti:

1. **InsightLearn.Core** - Domain entities, interfaces, DTOs (layer condiviso)
2. **InsightLearn.Infrastructure** - Repository implementations, DbContext, external services
3. **InsightLearn.Application** - ASP.NET Core Web API backend
4. **InsightLearn.WebAssembly** - Blazor WebAssembly frontend (client-side)

### API Architecture

- **Pattern**: ASP.NET Core **Minimal APIs** (NOT traditional Controllers)
- **Endpoint Definition**: Inline in [Program.cs:154-220](src/InsightLearn.Application/Program.cs#L154-L220) using `app.MapPost()`, `app.MapGet()`
- **Dependency Injection**: Services injected via `[FromServices]` attribute
- **Automatic Swagger**: Available at `/swagger` when running API
- **Health Check**: `/health` endpoint for Kubernetes liveness/readiness probes
- **Info Endpoint**: `/api/info` returns version and feature list

### Frontend Architecture (Blazor WebAssembly)

**Service Layer Pattern**:
- **Centralized API Client**: [ApiClient.cs](src/InsightLearn.WebAssembly/Services/Http/ApiClient.cs) - base HTTP client
- **Endpoint Configuration**: [appsettings.json](src/InsightLearn.WebAssembly/wwwroot/appsettings.json) defines all API routes with placeholders
- **Service Interfaces**: IChatService, ICourseService, IAuthService, IDashboardService, IPaymentService, etc.
- **Authentication**: [TokenService.cs](src/InsightLearn.WebAssembly/Services/Auth/TokenService.cs) manages JWT storage in browser localStorage

**Key Components**:
- [ChatbotWidget.razor](src/InsightLearn.WebAssembly/Components/ChatbotWidget.razor) - AI chatbot UI with qwen2:0.5b integration
- [GoogleSignInButton.razor](src/InsightLearn.WebAssembly/Components/GoogleSignInButton.razor) - OAuth Google login
- [CookieConsent.razor](src/InsightLearn.WebAssembly/Components/CookieConsent.razor) - GDPR compliance
- [AuthenticationStateHandler.razor](src/InsightLearn.WebAssembly/Components/AuthenticationStateHandler.razor) - Auth state management
- [VideoPlayer.razor](src/InsightLearn.WebAssembly/Components/VideoPlayer.razor) - HTML5 video player con MongoDB streaming
- [VideoUpload.razor](src/InsightLearn.WebAssembly/Components/VideoUpload.razor) - Video upload placeholder (backend completo)

### Authentication & Authorization

- **JWT-based authentication**: Tokens stored in browser localStorage
- **OAuth Support**: Google Sign-In (optional, requires `GOOGLE_CLIENT_ID` in `.env`)
- **Environment Variables** (configure in `.env`):
  - `JWT_SECRET_KEY`: Signing key (**MUST match** between API and Web)
  - `JWT_ISSUER`: `InsightLearn.Api`
  - `JWT_AUDIENCE`: `InsightLearn.Users`
  - `JWT_EXPIRATION_DAYS`: Token lifetime (default: 7 days)
- **API Endpoints**:
  - `/api/auth/login` - User login
  - `/api/auth/register` - New user registration
  - `/api/auth/refresh` - Refresh JWT token
  - `/api/auth/me` - Get current user info
  - `/api/auth/oauth-callback` - Google OAuth callback

### 🔴 CRITOCO: Endpoint Configuration (Database-Driven Architecture)

⚠️ **TUTTI GLI ENDPOINT SONO MEMORIZZATI NEL DATABASE** ⚠️

**REGOLA FONDAMENTALE**: Per modificare URL di endpoint, **NON toccare il codice**. Modificare SOLO il database.

#### Architettura
- **Database**: SQL Server tabella `SystemEndpoints` (seed data in [InsightLearnDbContext.cs:166-227](src/InsightLearn.Infrastructure/Data/InsightLearnDbContext.cs#L166-L227))
- **Backend API**: `/api/system/endpoints` endpoint ([Program.cs:160-190](src/InsightLearn.Application/Program.cs#L160-L190))
- **Caching**: MemoryCache 60 minuti con `IEndpointService`
- **Frontend**: `EndpointConfigurationService` carica da API con fallback a appsettings.json

#### Come Modificare un Endpoint

```sql
-- Esempio: Cambiare endpoint chatbot
UPDATE SystemEndpoints
SET EndpointPath = 'api/v2/chat/message', LastModified = GETUTCDATE()
WHERE Category = 'Chat' AND EndpointKey = 'SendMessage';

-- Refresh cache backend (chiamare da API)
-- oppure attendere scadenza cache (60 minuti)
```

#### File Coinvolti
- **Entity**: [SystemEndpoint.cs](src/InsightLearn.Core/Entities/SystemEndpoint.cs) - `Id, Category, EndpointKey, EndpointPath, HttpMethod, IsActive`
- **Repository**: [SystemEndpointRepository.cs](src/InsightLearn.Infrastructure/Repositories/SystemEndpointRepository.cs) - CRUD operations
- **Backend Service**: [EndpointService.cs](src/InsightLearn.Infrastructure/Services/EndpointService.cs) - caching + GetCategoryEndpointsAsync()
- **Frontend Service**: [EndpointConfigurationService.cs](src/InsightLearn.WebAssembly/Services/EndpointConfigurationService.cs) - LoadEndpointsAsync()
- **Seed Data**: [InsightLearnDbContext.cs:166-227](src/InsightLearn.Infrastructure/Data/InsightLearnDbContext.cs#L166-L227) - 50+ endpoints iniziali

#### ⚠️ Troubleshooting Endpoint Problems
1. **Frontend mostra 404/405**: Verificare `SystemEndpoints` nel database
2. **Endpoint non aggiornato**: Cache 60 min, attendere o restart API pod
3. **Errore deserializzazione**: Verificare che backend usi PascalCase (Program.cs:38-41)
4. **Deadlock Blazor WASM**: EndpointsConfig è Scoped, non Singleton (Program.cs:49-62)

### ⚠️ Problemi Noti e Soluzioni

1. **Program.cs mancante originariamente** (✅ Risolto)
   - [src/InsightLearn.Application/Program.cs](/src/InsightLearn.Application/Program.cs) è stato **creato manualmente**
   - Il progetto originale era configurato come library (SDK: Microsoft.NET.Sdk)
   - Ora è configurato come Web app (SDK: Microsoft.NET.Sdk.Web, OutputType: Exe)
   - Se rebuild fallisce con "Entry point not found", verificare che Program.cs esista

2. **Dockerfile.web build failure** (⚠️ Workaround)
   - [Dockerfile.web](/Dockerfile.web) fallisce con `NETSDK1082: no runtime pack for browser-wasm`
   - Problema noto di .NET SDK con RuntimeIdentifier 'browser-wasm' in container
   - **Workaround**: usare solo Dockerfile per API, deployare Web separatamente

3. **Rocky Linux 10: K3s Deployment** (✅ Configurato)
   - Sistema usa **K3s** Kubernetes con containerd runtime
   - Deploy images con: `docker save image:tag | sudo /usr/local/bin/k3s ctr images import -`
   - Password sudo: Configurata in ambiente production
   - **Non usare** [k8s/build-images.sh](/k8s/build-images.sh) (assume Docker standard)

3bis. **🔥 ZFS File System per K3s Storage** (✅ Implementato 2025-11-09)
   - **Pool**: `k3spool` (50GB file-based pool in `/home/zfs-k3s-pool.img`)
   - **Mountpoint**: `/k3s-zfs` (symlink da `/var/lib/rancher/k3s`)
   - **Compression**: **lz4** (compression ratio medio: **1.37x**, fino a **4.14x** su server data)
   - **Datasets**:
     - `k3spool/data` → K3s containerd data (compression 2.01x)
     - `k3spool/server` → K3s server config/certs (compression 6.07x)
     - `k3spool/storage` → Persistent volumes (compression 4.10x)
   - **Storage Savings**: 7.44GB logical → 5.24GB physical (**risparmio ~30%**)
   - **Autoload**: Systemd service `zfs-import-k3spool.service` (enabled at boot)
   - **Version**: OpenZFS 2.4.99-1 (compilato da sorgente per kernel 6.12.0)
   - **Comandi ZFS**:
     ```bash
     # Check pool status
     sudo /usr/local/sbin/zpool status k3spool

     # Check compression ratio
     sudo /usr/local/sbin/zfs get compressratio -r k3spool

     # List all datasets
     sudo /usr/local/sbin/zfs list -r k3spool

     # Monitor I/O performance
     sudo /usr/local/sbin/zpool iostat k3spool 5
     ```
   - **⚠️ IMPORTANTE**: ZFS binaries sono in `/usr/local/sbin/` (non nel PATH standard)
   - **Backup Original Data**: Rimosso `/var/lib/rancher/k3s.backup-old` (6.5GB liberati)

4. **MongoDB CreateContainerConfigError** (✅ Risolto v1.6.0)
   - **Problema**: Pod falliva con "couldn't find key mongodb-password in Secret"
   - **Causa**: Secret mancante in cluster
   - **Fix**: `kubectl patch secret insightlearn-secrets --type='json' -p='[{"op":"add","path":"/data/mongodb-password","value":"BASE64_PASSWORD"}]'`
   - **Status**: MongoDB ora operativo (1/1 Ready)

5. **Redis Pod Not Ready** (✅ Risolto v1.6.0)
   - **Problema**: Readiness probe failing con "container breakout detected"
   - **Causa**: K3s security policies blocca `exec` probes
   - **Fix**: Cambiato probe da `exec: redis-cli ping` a `tcpSocket: port 6379` in [k8s/04-redis-deployment.yaml](/k8s/04-redis-deployment.yaml)
   - **Status**: Redis ora operativo (1/1 Ready)

6. **Ollama Chatbot 404 Errors** (✅ Risolto v1.6.0)
   - **Problema**: `/api/chat/message` returning 404, model non caricato
   - **Causa**: Ollama pod running ma model non inizializzato in memoria
   - **Fix**: `kubectl delete pod ollama-0 -n insightlearn` (StatefulSet ricrea)
   - **Status**: Chatbot operativo (~1.7s response con qwen2:0.5b)

7. **Razor Compiler - Complex @code Blocks** (⚠️ Workaround)
   - **Problema**: .NET 8 Razor Source Generator genera codice C# invalido con nested DTOs in @code
   - **Errore**: `CS0116: A namespace cannot directly contain members`
   - **Workaround**: Componenti semplificati (VideoPlayer funzionale, VideoUpload placeholder)
   - **Backend**: Completamente funzionale (5 API endpoints video storage)
   - **Soluzione futura**: Code-behind pattern (.razor.cs) o upgrade .NET 9

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

# 5. Inizializza Ollama LLM (phi3:mini per performance migliori)
docker exec insightlearn-ollama ollama pull phi3:mini

# 6. Verifica
docker-compose ps
curl http://localhost:7001/health
```

### Deployment Kubernetes (Rocky Linux 10 con K3s)

```bash
# 1. Verifica K3s status
sudo systemctl status k3s
kubectl cluster-info

# 2. Build Docker image (locale)
docker-compose build api
# Output: localhost/insightlearn/api:1.6.0-dev

# 3. Tag come latest
docker tag localhost/insightlearn/api:1.6.0-dev localhost/insightlearn/api:latest

# 4. Import in K3s containerd (richiede sudo)
echo "SUDO_PASSWORD" | sudo -S sh -c \
  'docker save localhost/insightlearn/api:latest | /usr/local/bin/k3s ctr images import -'

# 5. Deploy manifests Kubernetes
kubectl apply -f k8s/00-namespace.yaml
kubectl apply -f k8s/01-secrets.yaml
kubectl apply -f k8s/02-configmaps.yaml
kubectl apply -f k8s/03-*.yaml  # SQL Server, MongoDB, Redis, Elasticsearch, etc.
kubectl apply -f k8s/05-*.yaml  # Deployments
kubectl apply -f k8s/06-*.yaml  # Services
kubectl apply -f k8s/08-ingress.yaml

# 6. Restart deployment con nuova immagine
kubectl rollout restart deployment/insightlearn-api -n insightlearn
kubectl rollout status deployment/insightlearn-api -n insightlearn --timeout=120s

# 7. Verifica status
kubectl get pods -n insightlearn
kubectl get svc -n insightlearn
kubectl get ingress -n insightlearn

# 8. Test API
curl http://localhost:31081/api/info
curl http://localhost:31081/health
```

**Note K3s**:
- K3s usa containerd, NON Docker runtime
- Import images: `k3s ctr images import` (non `docker load`)
- List images: `sudo k3s ctr images ls | grep insightlearn`
- Ingress: K3s Traefik controller (non Nginx Ingress)
- NodePort: Ports 30000-32767 disponibili per Services

## Database Initialization

⚠️ **Automatic EF Core migrations on API startup**:
- **Location**: [Program.cs:93-116](src/InsightLearn.Application/Program.cs#L93-L116)
- **Behavior**: `dbContext.Database.MigrateAsync()` runs on every startup
- **Retry Policy**: 5 retries, 30-second delay between attempts (configured on line 52-57)
- **Command Timeout**: 120 seconds for long-running migrations
- **Error Handling**: Logs errors but **does NOT fail startup** if DB unavailable (health checks handle it)

**Important**:
- Migrations are applied automatically in production (Kubernetes best practice)
- No need to run `dotnet ef database update` manually
- Ensure migrations are tested before deployment

## Versioning e Build Metadata

Il versioning è gestito centralmente in [Directory.Build.props](/Directory.Build.props):

- **VersionPrefix**: `1.6.0` (semantic version Major.Minor.Patch)
- **VersionSuffix**: `dev` (default, rimosso in release)
- **Version finale**: `1.6.0-dev`

**Versioning Dinamico**:
- [Program.cs](src/InsightLearn.Application/Program.cs) legge la versione dall'assembly via `System.Reflection`
- [Constants.cs](src/InsightLearn.WebAssembly/Shared/Constants.cs) sincronizzato con `1.6.0-dev`
- **Non hardcodare mai versioni** - usare Assembly.GetName().Version

Build variables disponibili:
- `$(VERSION)` - da Directory.Build.props
- `$(GIT_COMMIT)` - short commit hash
- `$(BUILD_NUMBER)` - git commit count

Esempio modifica version:
```xml
<!-- Directory.Build.props -->
<VersionPrefix>1.7.0</VersionPrefix>
<VersionSuffix>beta</VersionSuffix>
```

**Versioni Changelog**:
- Vedi [CHANGELOG.md](/CHANGELOG.md) per la storia completa delle release
- v1.6.0-dev: MongoDB video storage, course pages, versioning unificato (2025-11-08)

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

#### 📋 Endpoint Completi (46 totali, 45 implementati)

**Legenda**: ✅ = Implementato | ❌ = NON Implementato (solo configurato in DB)

**⚠️ Aggiornamento 2025-11-10**: Phase 3 completata - 31 nuovi endpoint API implementati in [Program.cs](src/InsightLearn.Application/Program.cs). La piattaforma LMS è ora completamente funzionale.

##### Authentication (6 endpoint - 5 implementati)

| Endpoint | Metodo | Stato | Note |
|----------|--------|-------|------|
| `api/auth/login` | POST | ✅ | Login funzionante |
| `api/auth/register` | POST | ✅ | Registrazione utente |
| `api/auth/refresh` | POST | ✅ | Refresh JWT token |
| `api/auth/me` | GET | ✅ | Current user info |
| `api/auth/oauth-callback` | POST | ✅ | Google OAuth |
| `api/auth/complete-registration` | POST | ❌ | Complete OAuth registration |

##### Chat (4 endpoint - 4 implementati)

| Endpoint | Metodo | Stato | Note |
|----------|--------|-------|------|
| `api/chat/message` | POST | ✅ | Send message to AI chatbot |
| `api/chat/history` | GET | ✅ | Get chat history |
| `api/chat/history/{sessionId}` | DELETE | ✅ | Delete session history |
| `api/chat/health` | GET | ✅ | Chatbot health check |

##### Video (5 endpoint - 5 implementati)

| Endpoint | Metodo | Stato | Note |
|----------|--------|-------|------|
| `api/video/upload` | POST | ✅ | Upload video (max 500MB) |
| `api/video/stream/{fileId}` | GET | ✅ | Stream video with range support |
| `api/video/metadata/{fileId}` | GET | ✅ | Get video metadata |
| `api/video/{videoId}` | DELETE | ✅ | Delete video |
| `api/video/upload/progress/{uploadId}` | GET | ✅ | Get upload progress |

##### System (4 endpoint - 4 implementati)

| Endpoint | Metodo | Stato | Note |
|----------|--------|-------|------|
| `api/system/endpoints` | GET | ✅ | Get all endpoints (cached 60min) |
| `api/system/endpoints/{category}` | GET | ✅ | Get endpoints by category |
| `api/system/endpoints/{category}/{key}` | GET | ✅ | Get specific endpoint |
| `api/system/endpoints/refresh-cache` | POST | ✅ | Refresh endpoint cache |

##### Categories (5 endpoint - 5 implementati) ✅

| Endpoint | Metodo | Stato | Note |
|----------|--------|-------|------|
| `api/categories` | GET | ✅ | List all categories |
| `api/categories` | POST | ✅ | Create category (Admin/Instructor) |
| `api/categories/{id}` | GET | ✅ | Get category by ID |
| `api/categories/{id}` | PUT | ✅ | Update category (Admin) |
| `api/categories/{id}` | DELETE | ✅ | Delete category (Admin) |

##### Courses (7 endpoint - 7 implementati) ✅

| Endpoint | Metodo | Stato | Note |
|----------|--------|-------|------|
| `api/courses` | GET | ✅ | List all courses (paginated) |
| `api/courses` | POST | ✅ | Create course (Admin/Instructor) |
| `api/courses/{id}` | GET | ✅ | Get course by ID |
| `api/courses/{id}` | PUT | ✅ | Update course (Admin/Instructor) |
| `api/courses/{id}` | DELETE | ✅ | Delete course (Admin) |
| `api/courses/category/{id}` | GET | ✅ | Get courses by category |
| `api/courses/search` | GET | ✅ | Search courses with filters |

##### Enrollments (5 endpoint - 5 implementati) ✅

| Endpoint | Metodo | Stato | Note |
|----------|--------|-------|------|
| `api/enrollments` | GET | ✅ | List all enrollments (Admin - returns 501) |
| `api/enrollments` | POST | ✅ | Enroll user to course |
| `api/enrollments/{id}` | GET | ✅ | Get enrollment by ID (Admin or self) |
| `api/enrollments/course/{id}` | GET | ✅ | Get enrollments for course (Admin/Instructor) |
| `api/enrollments/user/{id}` | GET | ✅ | Get user enrollments (Admin or self) |

##### Payments (3 endpoint - 3 implementati) ✅

| Endpoint | Metodo | Stato | Note |
|----------|--------|-------|------|
| `api/payments/create-checkout` | POST | ✅ | Create Stripe checkout session |
| `api/payments/transactions` | GET | ✅ | List transactions (Admin sees all) |
| `api/payments/transactions/{id}` | GET | ✅ | Get transaction by ID |

##### Reviews (4 endpoint - 4 implementati) ✅

| Endpoint | Metodo | Stato | Note |
|----------|--------|-------|------|
| `api/reviews/course/{id}` | GET | ✅ | Get course reviews (paginated) |
| `api/reviews` | POST | ✅ | Create review (authenticated user) |
| `api/reviews/{id}` | GET | ✅ | Get review by ID |
| `api/reviews/course/{id}` | GET | ✅ | Get course reviews |

##### Users (5 endpoint - 5 implementati) ✅

| Endpoint | Metodo | Stato | Note |
|----------|--------|-------|------|
| `api/users` | GET | ✅ | List all users (Admin only) |
| `api/users/{id}` | GET | ✅ | Get user by ID (Admin or self) |
| `api/users/{id}` | PUT | ✅ | Update user (Admin or self) |
| `api/users/{id}` | DELETE | ✅ | Delete user (Admin only) |
| `api/users/profile` | GET | ✅ | Get current user profile |

##### Dashboard (2 endpoint - 2 implementati) ✅

| Endpoint | Metodo | Stato | Note |
|----------|--------|-------|------|
| `api/dashboard/stats` | GET | ✅ | Get dashboard statistics (Admin only) |
| `api/dashboard/recent-activity` | GET | ✅ | Get recent activity (Admin only) |

**✅ PHASE 3 COMPLETATA (2025-11-10)**: Tutti i 31 endpoint LMS critici sono stati implementati. La piattaforma è ora completamente funzionale come LMS enterprise con:
- Gestione completa dei corsi (Courses, Categories)
- Sistema di iscrizioni (Enrollments)
- Sistema di pagamenti (Payments con Stripe)
- Sistema di recensioni (Reviews)
- Gestione utenti (Users Admin)
- Dashboard amministrativa (Dashboard Stats)

**Unico endpoint mancante**: `api/auth/complete-registration` (1/46 endpoint totali).

### Sicurezza

1. **MAI** committare `.env` con password reali
2. Placeholder `YOUR_*` in file di config
3. Sostituire con env vars al deploy
4. TLS certs self-signed SOLO per dev

#### 🛡️ Security Patches Applied (2025-01-08)

**Status**: ✅ Tutte le vulnerabilità HIGH risolte

**Vulnerabilità Patched**:
- ✅ **CVE-2024-43483** (HIGH): Microsoft.Extensions.Caching.Memory 8.0.0 → 8.0.1
  - Hash flooding DoS attack vulnerability
  - Applicato a: Core, Infrastructure, Application, WebAssembly
- ✅ **CVE-2024-43485** (HIGH): System.Text.Json 8.0.4 → 8.0.5
  - JsonExtensionData vulnerability
  - Applicato a: Infrastructure, Application
- ✅ System.Formats.Asn1 8.0.0 → 8.0.1 (security patch)
- ✅ System.IO.Packaging 8.0.0 → 8.0.1 (security patch)
- ✅ Azure.Identity 1.10.3 → 1.13.1 (MODERATE severity patches)
- ✅ Microsoft.Extensions.DependencyInjection.Abstractions 8.0.1 → 8.0.2 (dependency requirement)

**Remaining Vulnerabilities**:
- ⚠️ BouncyCastle.Cryptography 2.2.1 (3x MODERATE) - Transitive da itext7, non critico

**File Modificati**:
- [src/InsightLearn.Core/InsightLearn.Core.csproj](src/InsightLearn.Core/InsightLearn.Core.csproj) - 1 patch
- [src/InsightLearn.Infrastructure/InsightLearn.Infrastructure.csproj](src/InsightLearn.Infrastructure/InsightLearn.Infrastructure.csproj) - 5 patches
- [src/InsightLearn.Application/InsightLearn.Application.csproj](src/InsightLearn.Application/InsightLearn.Application.csproj) - 5 patches
- [src/InsightLearn.WebAssembly/InsightLearn.WebAssembly.csproj](src/InsightLearn.WebAssembly/InsightLearn.WebAssembly.csproj) - 1 patch

**Verifica Security Patches**:
```bash
# Check vulnerabilities
dotnet list package --vulnerable --include-transitive

# Expected output: No HIGH or CRITICAL vulnerabilities
# Only 3 MODERATE (BouncyCastle) remain
```

### Database Stack

| Database | Uso | Porta | Status |
|----------|-----|-------|--------|
| SQL Server 2022 | Dati relazionali principali + **ChatbotMessages** entity via EF Core | 1433 | ✅ Operativo |
| MongoDB 7.0 | Video storage (GridFS via [MongoVideoStorageService.cs](src/InsightLearn.Application/Services/MongoVideoStorageService.cs)) | 27017 | ✅ Operativo |
| Redis 7 | Cache + sessioni utente | 6379 | ✅ Operativo |
| Elasticsearch 8.11 | Search engine | 9200 | ✅ Operativo |

**Note**:
- Chatbot messages sono salvati in SQL Server via [ChatbotService.cs:84](src/InsightLearn.Application/Services/ChatbotService.cs#L84), NON in MongoDB
- MongoDB è ora pienamente operativo per video storage con GridFS e GZip compression
- Redis configurato con tcpSocket health probes (K3s security compliance)
- EF Core gestisce migrations automatiche al startup (vedi sezione Database Initialization)

**MongoDB Video Storage**:
- **Database**: `insightlearn_videos`
- **User**: `insightlearn` (password in Secret `mongodb-password`)
- **GridFS Bucket**: Default bucket per video files
- **Compression**: GZip CompressionLevel.Optimal (20-40% size reduction)
- **API Endpoints**: 5 endpoints per upload, streaming, metadata, list, delete
  - `POST /api/video/upload` - Upload con validazione (max 500MB)
  - `GET /api/video/stream/{fileId}` - Streaming con range support
  - `GET /api/video/metadata/{fileId}` - Metadata retrieval
  - `DELETE /api/video/{videoId}` - Delete video
  - `GET /api/video/upload/progress/{uploadId}` - Upload progress
- **Connection String**: Configurata in [k8s/06-api-deployment.yaml](k8s/06-api-deployment.yaml) come env var `MongoDb__ConnectionString`
- **E2E Test Status** (2025-01-08): ✅ GridFS operativo, validazioni funzionanti
  - ✅ Content-type validation: MP4, WebM, OGG, MOV
  - ✅ File size validation: max 500MB
  - ✅ UUID validation: lessonId, userId
  - ⚠️ Business logic validation: require existing Lesson in SQL Server

### AI/Chatbot

- **LLM Server**: Ollama (porta 11434)
- **Model**: `qwen2:0.5b` (piccolo, veloce, ~1.7s response time)
- **Download model**: `kubectl exec -it ollama-0 -c ollama -n insightlearn -- ollama pull qwen2:0.5b`
- **API Endpoints**:
  - `POST /api/chat/message` - Send message and get AI response (see [Program.cs:154-188](src/InsightLearn.Application/Program.cs#L154-L188))
  - `GET /api/chat/history?sessionId={id}&limit={n}` - Get chat history
- **Services**:
  - [OllamaService.cs](src/InsightLearn.Application/Services/OllamaService.cs) - HTTP client for Ollama API
  - [ChatbotService.cs](src/InsightLearn.Application/Services/ChatbotService.cs) - Business logic + persistence
- **Storage**: SQL Server `ChatbotMessages` table (via EF Core DbContext), NOT MongoDB
- **Background Cleanup**: [ChatbotCleanupBackgroundService.cs](src/InsightLearn.Application/Services/ChatbotCleanupBackgroundService.cs) - deletes old messages
- **Configuration** (in appsettings.json or env vars):
  - `Ollama:BaseUrl` or `Ollama:Url` - default: `http://ollama-service:11434`
  - `Ollama:Model` - default: `qwen2:0.5b`

**Ollama Troubleshooting** (v1.6.0 fix):
- Se il chatbot restituisce 404 errors, il modello potrebbe non essere caricato in memoria
- **Fix**: `kubectl delete pod ollama-0 -n insightlearn` (StatefulSet ricrea il pod)
- Verifica: `kubectl logs -n insightlearn ollama-0 -c ollama | grep "llama runner started"`
- Test: `curl -X POST http://localhost:31081/api/chat/message -H "Content-Type: application/json" -d '{"message":"Test","sessionId":"test"}'`

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

### 📊 Grafana Dashboard Configuration (2025-01-08)

**Status**: ✅ Dashboard "InsightLearn Platform Monitoring" disponibile

**Accesso**:
- **URL locale**: http://localhost:3000 (port-forward persistente ✅)
- **Script persistente**: `./k8s/grafana-port-forward-persistent.sh` (auto-restart se disconnesso)
- **NodePort alternativo**: http://localhost:31300 (no port-forward richiesto)
- **Credenziali default**: admin/admin (cambiare al primo login)

**Dashboard Incluse**:
1. **InsightLearn Platform Monitoring** (uid: `insightlearn-main`)
   - API Request Rate (req/s per endpoint)
   - API Response Time (p50, p95 latency)
   - Health Status: API, MongoDB, SQL Server, Redis
   - Ollama Inference Time (p50, p95)
   - Pod CPU/Memory Usage
   - MongoDB Video Storage Size

**File**:
- [k8s/grafana-dashboard-insightlearn.json](k8s/grafana-dashboard-insightlearn.json) - Dashboard JSON
- [k8s/17-grafana-dashboard-configmap.yaml](k8s/17-grafana-dashboard-configmap.yaml) - ConfigMap auto-load

**Import Dashboard**:
```bash
# Via ConfigMap (già applicato)
kubectl apply -f k8s/17-grafana-dashboard-configmap.yaml

# Via Grafana UI
# 1. Login: http://localhost:3000 (admin/admin)
# 2. Dashboards → Import → Upload JSON file
# 3. Seleziona: k8s/grafana-dashboard-insightlearn.json
```

**Data Source**: Prometheus (http://prometheus:9090) - configurato automaticamente

### 🔧 Service Watchdog (2025-01-08)

**Status**: ✅ Watchdog automatico attivo

**Funzionalità**:
- Monitora health di tutti i servizi ogni 60 secondi
- Riavvia automaticamente i pod che falliscono health checks
- Verifica HTTP endpoints (API, Grafana)
- Verifica Kubernetes pod readiness (MongoDB, SQL Server, Redis, Ollama, WebAssembly)
- Log dettagliato in `/tmp/insightlearn-watchdog.log`

**Servizi monitorati**:
1. API (pod + HTTP /health)
2. Grafana (pod + HTTP /api/health)
3. Prometheus (pod)
4. MongoDB (pod)
5. SQL Server (pod)
6. Redis (pod)
7. Ollama (pod)
8. WebAssembly frontend (pod)

**Avvio watchdog**:
```bash
# Avvio manuale
./k8s/service-watchdog.sh &

# Verifica log
tail -f /tmp/insightlearn-watchdog.log
```

**File**: [k8s/service-watchdog.sh](k8s/service-watchdog.sh)

### 📹 VideoUpload Component (2025-01-08)

**Status**: ✅ Implementazione completa con code-behind pattern

**Funzionalità**:
- File selection con drag & drop
- Validazione tipo file (MP4, WebM, OGG, MOV)
- Validazione dimensione (max 500MB configurabile)
- Upload progress tracking
- Error handling con retry
- Success/error notifications
- MongoDB GridFS integration via API

**File**:
- [src/InsightLearn.WebAssembly/Components/VideoUpload.razor](src/InsightLearn.WebAssembly/Components/VideoUpload.razor) - UI Markup
- [src/InsightLearn.WebAssembly/Components/VideoUpload.razor.cs](src/InsightLearn.WebAssembly/Components/VideoUpload.razor.cs) - Code-behind logic
- [src/InsightLearn.WebAssembly/wwwroot/css/video-components.css](src/InsightLearn.WebAssembly/wwwroot/css/video-components.css) - Styling

**Uso**:
```razor
<VideoUpload
    LessonId="@lessonGuid"
    UserId="@userGuid"
    Title="Upload Video Lesson"
    MaxFileSize="524288000"
    OnUploadComplete="@HandleUploadComplete"
    OnUploadError="@HandleUploadError" />
```

**API Backend**: Completamente funzionale (MongoDB GridFS configurato)

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
| Grafana | 3000 (port-forward), 31300 (NodePort) | ❌ |
| Jenkins | **32000** (NodePort), 50000 (JNLP) | ❌ |

**Note**:
- Prometheus usa porta 9091 per evitare conflitto con systemd su porta 9090
- Jenkins: Porta corretta è **32000** (NON 8080 come da docs obsolete)

## Credenziali Default

### Application Admin
- URL: https://localhost
- Email: `admin@insightlearn.cloud`
- Password: da file `.env` (`ADMIN_PASSWORD`)

### Services
- **Grafana**: admin / admin
- **SQL Server**: sa / `${MSSQL_SA_PASSWORD}` (da Secret `mssql-sa-password`)
- **MongoDB**:
  - Root: admin / `${MONGO_PASSWORD}` (Docker Compose)
  - App User: insightlearn / (da Secret `mongodb-password`) (Kubernetes)
  - Database: `insightlearn_videos`
- **Redis**: password: `${REDIS_PASSWORD}` (da Secret `redis-password`)
- **Jenkins**: Nessuna autenticazione (development mode)

## Jenkins CI/CD & Automated Testing

**Status**: ✅ Deployato su Kubernetes (namespace: jenkins)

### Configurazione

- **Versione**: Jenkins 2.528.1 (Alpine LTS)
- **Deployment**: Lightweight (384Mi-768Mi RAM, 250m-500m CPU)
- **Storage**: PVC 20Gi (local-path StorageClass)
- **Autenticazione**: **Disabilitata** per ambiente sviluppo (`authorizationStrategy: Unsecured`)
- **Porte**:
  - HTTP UI: **NodePort 32000** (⚠️ NON 8080!)
  - JNLP Agents: 50000

### Accesso

```bash
# Via NodePort (raccomandato)
http://localhost:32000

# Via minikube IP
http://$(minikube ip):32000
```

**⚠️ IMPORTANTE**: Documentazione obsoleta indica porta 8080, ma la porta corretta è **32000** (NodePort).

### Pipeline & Testing Scripts

**Jenkinsfile** - 9 stage di test automatici:
1. Preparation - Inizializzazione environment
2. Health Check - Verifica endpoints principali
3. Page Availability Test - Controllo 404 errors
4. Performance Benchmarking - Average response time
5. Load Testing - Simulazione 50 concurrent users
6. Asset Validation - CSS/JS/images integrity
7. Security Headers Check - Security headers validation
8. Backend API Monitoring - Kubernetes pod status
9. Generate Report - Summary report

**Testing Scripts** ([jenkins/scripts/](jenkins/scripts/)):
- `load-test.sh` - Load testing con 4 profili (light/medium/heavy/stress)
- `site-monitor.sh` - Continuous monitoring con alerting
- `test-email-notification.sh` - Email notification testing

### Deployment

```bash
# Deploy Jenkins (con fix PVC per local-path)
kubectl apply -f k8s/12-jenkins-namespace.yaml
kubectl apply -f k8s/13-jenkins-rbac.yaml
kubectl apply -f k8s/14-jenkins-pvc.yaml
kubectl apply -f k8s/15-jenkins-deployment-lightweight.yaml

# Oppure usa lo script (richiede minikube)
./k8s/deploy-jenkins.sh
```

**Problemi Comuni**:
- ❌ PVC Pending: Cambiare `storageClassName: standard` → `local-path` in 14-jenkins-pvc.yaml
- ❌ Porta 8080 non risponde: Usare porta **32000** (NodePort)

### Documentazione

- [jenkins/README.md](jenkins/README.md) - Setup completo e guida configurazione
- [Jenkinsfile](Jenkinsfile) - Pipeline definition

## Scripts Kubernetes

| Script | Funzione |
|--------|----------|
| [k8s/build-images.sh](/k8s/build-images.sh) | Build Docker images con versioning |
| [k8s/deploy.sh](/k8s/deploy.sh) | Deploy completo K8s |
| [k8s/status.sh](/k8s/status.sh) | Status pods/services |
| [k8s/undeploy.sh](/k8s/undeploy.sh) | Remove deployment |
| [k8s/deploy-jenkins.sh](/k8s/deploy-jenkins.sh) | Deploy Jenkins CI/CD (automated testing) |
| [k8s/grafana-port-forward-persistent.sh](/k8s/grafana-port-forward-persistent.sh) | Port-forward persistente Grafana (localhost:3000) |
| [k8s/api-port-forward-persistent.sh](/k8s/api-port-forward-persistent.sh) | Port-forward persistente API (localhost:8081) |
| [k8s/service-watchdog.sh](/k8s/service-watchdog.sh) | Service monitoring & auto-healing (60s check interval) |

⚠️ **Rocky Linux**: Gli script assumono Docker, sostituire con `podman` manualmente.

### Port-Forward Persistenti

Per mantenere i servizi sempre accessibili su localhost:

```bash
# Grafana (background, auto-restart)
./k8s/grafana-port-forward-persistent.sh &

# API (background, auto-restart)
./k8s/api-port-forward-persistent.sh &

# Verifica port-forwards attivi
ps aux | grep "kubectl port-forward"
```

## Note per Claude Code

Quando lavori con questa repository:

1. **Leggere SEMPRE questo file** all'inizio del task
2. 🔴 **ENDPOINT NEL DATABASE** - **MAI** modificare endpoint nel codice. Tutti gli URL endpoint sono in SQL Server tabella `SystemEndpoints`. Per problemi 404/405, controllare/aggiornare database, NON codice.
3. 🔴🔴🔴 **LEGGE FONDAMENTALE - SINCRONIZZAZIONE AUTOMATICA ENDPOINT** 🔴🔴🔴
   - **OGNI VOLTA** che aggiungi/modifichi/elimini endpoint nel database `SystemEndpoints`, **DEVI AGGIORNARE CLAUDE.md** nella sezione "📋 Endpoint Completi"
   - **NON È OPZIONALE** - È una **LEGGE ASSOLUTA**
   - Verificare SEMPRE coerenza: DB ↔ CLAUDE.md ↔ Program.cs
   - Usare script: `./scripts/sync-endpoints-to-claude.sh` (se esiste) oppure manuale
   - **NESSUNA ECCEZIONE PERMESSA**
4. **Versione**: Sempre `1.6.0-dev` da [Directory.Build.props](/Directory.Build.props) - **mai hardcodare versioni**
5. **Program.cs esiste?** Se manca in src/InsightLearn.Application/, il build fallirà
6. **Non usare Dockerfile.web** - ha un bug noto (NETSDK1082)
7. **Password da .env** - non committare mai password reali
8. **Prometheus porta 9091** - non 9090 (conflitto systemd)
9. **Rocky Linux 10 = K3s Kubernetes** - non minikube/Docker standard
   - K3s: containerd runtime (non Docker)
   - Import images: `docker save | sudo k3s ctr images import`
   - Deploy: `kubectl apply -f k8s/` poi `kubectl rollout restart`
10. **MongoDB Secret**: Password in `mongodb-password` Secret key (Kubernetes)
    - Se pod in CreateContainerConfigError, verificare Secret esiste
    - Fix: `kubectl patch secret insightlearn-secrets --type='json' -p='[{"op":"add","path":"/data/mongodb-password","value":"BASE64_PWD"}]'`
11. **Redis Health Probes**: Usa `tcpSocket` (non `exec`) per K3s security compliance
    - File: [k8s/04-redis-deployment.yaml](/k8s/04-redis-deployment.yaml)
12. **Ollama Model**: `qwen2:0.5b` (non phi3:mini o llama2)
    - Se chatbot returns 404: `kubectl delete pod ollama-0 -n insightlearn`
    - Test: `curl -X POST http://localhost:31081/api/chat/message -d '{"message":"Test","sessionId":"test"}'`
13. **Automatic Database Migrations**: L'API applica migrations automaticamente al startup (vedi [Program.cs:93-116](src/InsightLearn.Application/Program.cs#L93-L116))
14. **Minimal APIs**: L'applicazione usa Minimal APIs, NON Controllers tradizionali - tutti gli endpoints sono definiti in Program.cs
15. **Endpoint API**: `/api/system/endpoints` ritorna tutti gli endpoints organizzati per categoria (cache 60 min) - **TOTALE: 46 endpoint configurati, 27 implementati**
16. **MongoDB Video Storage**: 5 API endpoints per upload/streaming video (GridFS + GZip compression)
    - Upload: `POST /api/video/upload` (max 500MB)
    - Stream: `GET /api/video/stream/{fileId}` (range support)
    - Metadata: `GET /api/video/metadata/{fileId}`
17. **Content-Security-Policy**: Configurato in [k8s/08-ingress.yaml](/k8s/08-ingress.yaml) per Blazor WASM security
18. **Razor Components**: Evitare nested DTOs in @code blocks (.NET 8 compiler bug)
    - Preferire code-behind pattern (.razor.cs) per componenti complessi
19. **Status Deployment**: 10/10 pods healthy in production (v1.6.0-dev)
    - API: ✅ Running (NodePort 31081)
    - MongoDB: ✅ Running (video storage operativo)
    - Redis: ✅ Running (tcpSocket probes)
    - Ollama: ✅ Running (qwen2:0.5b ~1.7s)
20. **🔥 ZFS File System**: K3s storage ora su ZFS per compression e reliability (implementato 2025-11-09)
    - Pool: `k3spool` (50GB in `/home/zfs-k3s-pool.img`)
    - K3s data: `/k3s-zfs` (symlink da `/var/lib/rancher/k3s`)
    - Compression: **lz4** (ratio 1.37x medio, fino a 6.07x su certs)
    - ZFS comandi: `/usr/local/sbin/zpool` e `/usr/local/sbin/zfs` (non nel PATH standard)
    - Autoload: `systemctl status zfs-import-k3spool.service`
    - ⚠️ **NON modificare /var/lib/rancher/k3s direttamente** - è un symlink a ZFS mountpoint

## File Kubernetes Modificati (v1.6.0)

### k8s/04-redis-deployment.yaml
**Modifiche**: Cambiato health probes da `exec` a `tcpSocket` per K3s security compliance

**Prima**:
```yaml
livenessProbe:
  exec:
    command:
    - redis-cli
    - ping
```

**Dopo**:
```yaml
livenessProbe:
  tcpSocket:
    port: 6379
  initialDelaySeconds: 30
  periodSeconds: 10
```

**Motivo**: K3s security policies bloccano `exec` probes con errore "container breakout detected"

### k8s/08-ingress.yaml
**Modifiche**: Aggiunto Content-Security-Policy header per Blazor WASM security

```yaml
metadata:
  annotations:
    nginx.ingress.kubernetes.io/configuration-snippet: |
      add_header Content-Security-Policy "default-src 'self';
        script-src 'self' 'unsafe-eval' 'wasm-unsafe-eval';
        style-src 'self' 'unsafe-inline';
        img-src 'self' data: https:;
        font-src 'self' data:;
        connect-src 'self' wss: https:;
        frame-ancestors 'self';
        base-uri 'self';
        form-action 'self';" always;
```

**Motivo**: Security best practice per applicazioni Blazor WebAssembly

### k8s/01-secrets.yaml
**Note**: Verificare che contenga tutti i secrets richiesti:
- `mssql-sa-password`: Password SQL Server
- `jwt-secret-key`: JWT signing key
- `connection-string`: SQL Server connection string
- `mongodb-password`: MongoDB password (aggiunto in v1.6.0)
- `redis-password`: Redis password

**Se mongodb-password mancante**, applicare patch:
```bash
kubectl patch secret -n insightlearn insightlearn-secrets \
  --type='json' \
  -p='[{"op": "add", "path": "/data/mongodb-password", "value": "BASE64_ENCODED_PASSWORD"}]'
```

### nginx/nginx.conf
**Modifiche**: Aggiunto Content-Security-Policy header per Docker Compose deployment

```nginx
# Nella sezione server SSL (porta 443)
add_header Content-Security-Policy "default-src 'self';
  script-src 'self' 'unsafe-eval' 'wasm-unsafe-eval';
  style-src 'self' 'unsafe-inline';
  img-src 'self' data: https:;
  font-src 'self' data:;
  connect-src 'self' wss: https:;
  frame-ancestors 'self';
  base-uri 'self';
  form-action 'self';" always;
```

## File Frontend Modificati (v1.6.0)

### src/InsightLearn.WebAssembly/wwwroot/index.html
**Modifiche**: Aggiunto link a `video-components.css` (linea 41)

```html
<link rel="stylesheet" href="css/courses.css" />
<link rel="stylesheet" href="css/video-components.css" />
```

### src/InsightLearn.WebAssembly/wwwroot/css/video-components.css
**Nuovo file** (390 linee): Styling completo per video upload e player
- Upload zone con drag & drop
- Progress bar con animazioni
- Custom video player controls
- Responsive design (mobile-first)

### src/InsightLearn.WebAssembly/Components/VideoPlayer.razor
**Nuovo componente** (157 linee): HTML5 video player funzionale
- Streaming da MongoDB GridFS
- Metadata display (size, format, compression)
- Error handling con retry
- Standard HTML5 controls

### src/InsightLearn.WebAssembly/Components/VideoUpload.razor
**Nuovo componente** (52 linee): Placeholder per upload video
- Backend API completamente funzionale
- UI semplificata per evitare Razor compiler bugs
- Pronto per implementazione code-behind (.razor.cs)

### src/InsightLearn.WebAssembly/Shared/Constants.cs
**Modifiche**: Aggiornato `AppVersion` da `1.0.0` a `1.6.0-dev` (linea 121)

## File Backend Modificati (v1.6.0)

### src/InsightLearn.Application/Program.cs
**Modifiche principali**:

1. **Versioning dinamico** (linee 12-18):
```csharp
using System.Reflection;

var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.6.0.0";
var versionShort = version.Substring(0, version.LastIndexOf('.')) + "-dev";
```

2. **MongoDB services registration** (linee 94-98):
```csharp
// Register MongoDB Video Storage Services
builder.Services.AddSingleton<IMongoVideoStorageService, MongoVideoStorageService>();
builder.Services.AddScoped<IVideoProcessingService, VideoProcessingService>();

Console.WriteLine("[CONFIG] MongoDB Video Storage Services registered");
```

3. **5 nuovi Video API endpoints** (linee 348-538):
   - `POST /api/video/upload` - Upload con validazione
   - `GET /api/video/stream/{fileId}` - Streaming con range support
   - `GET /api/video/metadata/{fileId}` - Metadata retrieval
   - `DELETE /api/video/{videoId}` - Delete video
   - `GET /api/video/upload/progress/{uploadId}` - Upload progress

4. **Aggiornato /api/info endpoint** (linee 185, 197):
```csharp
version = versionShort,  // Dynamic invece di hardcoded "1.4.29"
features = new[] {
  "chatbot", "auth", "courses", "payments",
  "mongodb-video-storage",      // NUOVO
  "gridfs-compression",          // NUOVO
  "video-streaming",             // NUOVO
  "browse-courses-page",         // NUOVO
  "course-detail-page"           // NUOVO
}
```

### src/InsightLearn.Application/Services/MongoVideoStorageService.cs
**Già esistente** (243 linee): Registrato in DI container (v1.6.0)
- GridFS operations con GZip compression
- Upload/Download/Delete video files
- Metadata management

### src/InsightLearn.Application/Services/VideoProcessingService.cs
**Già esistente** (244 linee): Registrato in DI container (v1.6.0)
- High-level video processing logic
- Progress tracking per upload
- Integration con database

### Directory.Build.props
**Modifiche**: Aggiornato `VersionPrefix` da `1.4.22` a `1.6.0` (linea 4)

```xml
<VersionPrefix>1.6.0</VersionPrefix>
<VersionSuffix Condition="'$(VersionSuffix)' == ''">dev</VersionSuffix>
```

## Documentazione Aggiuntiva

- [CHANGELOG.md](/CHANGELOG.md) - Storia completa delle release e features (aggiunto v1.6.0)
- [DEPLOYMENT-COMPLETE-GUIDE.md](/DEPLOYMENT-COMPLETE-GUIDE.md) - Guida deploy step-by-step
- [DOCKER-COMPOSE-GUIDE.md](/DOCKER-COMPOSE-GUIDE.md) - Docker Compose dettagliato
- [k8s/README.md](/k8s/README.md) - Kubernetes deployment

## SaaS Subscription Model (v2.0.0 - Design Phase)

**Status**: Architecture design completed, implementation pending

InsightLearn is planning a major business model transition from **pay-per-course** to **SaaS subscription** with engagement-based instructor payouts.

### Key Changes

**Current Model**:
- Users pay €49.99 per course
- Instructors receive 80% of course price
- One-time revenue

**New SaaS Model**:
- Users pay €4.00/month (Basic), €8.00/month (Pro), or €12.00/month (Premium)
- Unlimited access to ALL courses
- Instructors paid based on engagement time: `payout = (platform_revenue * 0.80) * (instructor_engagement / total_engagement)`
- Recurring revenue (MRR)

### Documentation

1. **Complete Architecture**: [/docs/SAAS-SUBSCRIPTION-ARCHITECTURE.md](/docs/SAAS-SUBSCRIPTION-ARCHITECTURE.md)
   - Database schema (7 new tables)
   - Entity models (.NET)
   - Service interfaces
   - API endpoint specifications (23 new endpoints)
   - Stripe integration guide
   - Migration strategy

2. **Database Migration**: [/docs/SAAS-MIGRATION-SCRIPT.sql](/docs/SAAS-MIGRATION-SCRIPT.sql)
   - Complete SQL migration script
   - Grandfather existing users (free trial based on purchase history)
   - Triggers for auto-enrollment
   - Views for reporting
   - Stored procedures for payout calculation

3. **Implementation Roadmap**: [/docs/SAAS-IMPLEMENTATION-ROADMAP.md](/docs/SAAS-IMPLEMENTATION-ROADMAP.md)
   - 4-week implementation timeline
   - Team assignments
   - Testing strategy
   - Monitoring & metrics
   - Rollback plan

### New Entities (v2.0.0)

- **SubscriptionPlan**: Basic/Pro/Premium tiers
- **UserSubscription**: User subscription status and billing
- **CourseEngagement**: Track video watch, quiz, assignment time
- **InstructorPayout**: Monthly payout calculations
- **SubscriptionRevenue**: Revenue tracking per billing period
- **SubscriptionEvent**: Audit log for subscription changes
- **InstructorConnectAccount**: Stripe Connect integration

### New API Endpoints (23 total)

**Subscriptions** (9 endpoints):
- `GET /api/subscriptions/plans` - List plans
- `POST /api/subscriptions/subscribe` - Create subscription
- `GET /api/subscriptions/my-subscription` - Current user subscription
- `POST /api/subscriptions/cancel` - Cancel subscription
- `POST /api/subscriptions/resume` - Resume subscription
- `POST /api/subscriptions/upgrade` - Upgrade plan
- `POST /api/subscriptions/downgrade` - Downgrade plan
- `POST /api/subscriptions/create-checkout-session` - Stripe checkout
- `POST /api/subscriptions/create-portal-session` - Stripe portal

**Engagement** (3 endpoints):
- `POST /api/engagement/track` - Track engagement event
- `POST /api/engagement/video-progress` - Update video progress
- `GET /api/engagement/my-stats` - User engagement stats

**Instructor** (4 endpoints):
- `GET /api/instructor/earnings/preview` - Preview earnings
- `GET /api/instructor/payouts` - Payout history
- `GET /api/instructor/payouts/{id}` - Payout details
- `POST /api/instructor/connect/onboard` - Stripe Connect onboarding

**Admin** (6 endpoints):
- `POST /api/admin/payouts/calculate/{year}/{month}` - Calculate payouts
- `POST /api/admin/payouts/process/{id}` - Process payout
- `GET /api/admin/payouts/pending` - Pending payouts
- `GET /api/admin/engagement/course/{id}` - Course engagement
- `GET /api/admin/engagement/monthly-summary` - Monthly summary
- `GET /api/admin/subscriptions/metrics` - Subscription metrics

**Webhook** (1 endpoint):
- `POST /api/webhooks/stripe` - Handle Stripe events

### Implementation Status

- [x] Architecture design complete
- [x] Database schema designed
- [x] Entity models specified
- [x] API endpoints specified
- [x] Migration script complete
- [ ] Backend services implementation
- [ ] Frontend components
- [ ] Stripe integration
- [ ] Testing suite
- [ ] Production deployment

**Target Go-Live**: 2025-02-10 (4 weeks from design completion)

## Repository

- **URL**: https://github.com/marypas74/InsightLearn_WASM
- **Issues**: https://github.com/marypas74/InsightLearn_WASM/issues
- **Maintainer**: marcello.pasqui@gmail.com
