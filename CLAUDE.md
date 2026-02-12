# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Quick Reference

- **Version**: Defined in `Directory.Build.props` → `<VersionPrefix>` (currently 2.5.9-dev)
- **Stack**: .NET 8, Blazor WebAssembly, ASP.NET Core Minimal APIs, C# 12
- **Deploy**: K3s Kubernetes on Rocky Linux 10 via Podman
- **Domain**: insightlearn.cloud (behind Cloudflare)
- **Detailed docs**: CLAUDE-FULL.md

## Critical Rules

### Build & Deploy (MANDATORY)

1. **Always** increment `VersionPrefix` in `Directory.Build.props` before building
2. **Always** ask user for confirmation before build/deploy
3. Build commands (run from repo root):

```bash
# WASM Frontend
podman build -f Dockerfile.wasm -t localhost/insightlearn/wasm:X.X.X-dev .
podman save localhost/insightlearn/wasm:X.X.X-dev -o /tmp/wasm.tar
sudo /usr/local/bin/k3s ctr images import /tmp/wasm.tar
kubectl set image deployment/insightlearn-wasm-blazor-webassembly -n insightlearn wasm-blazor=localhost/insightlearn/wasm:X.X.X-dev

# API Backend
podman build -f Dockerfile.api -t localhost/insightlearn/api:X.X.X-dev .
podman save localhost/insightlearn/api:X.X.X-dev -o /tmp/api.tar
sudo /usr/local/bin/k3s ctr images import /tmp/api.tar
kubectl set image deployment/insightlearn-api -n insightlearn api=localhost/insightlearn/api:X.X.X-dev
```

### Endpoint Configuration (Database-Driven)

**All API endpoint paths are stored in SQL Server `SystemEndpoints` table**, not in code. To change an endpoint URL, update the database — not the source. The frontend `EndpointConfigurationService` loads endpoints from the API with fallback to `appsettings.json`.

## Architecture

### Solution Structure (InsightLearn.WASM.sln)

```text
src/
├── InsightLearn.Core/           # Domain layer: Entities, DTOs, Interfaces, Validation attributes
├── InsightLearn.Shared/         # Shared DTOs between WASM and API (references Core)
├── InsightLearn.Infrastructure/ # Data layer: EF DbContext, Repositories, external service integrations
├── InsightLearn.Application/    # API backend: Minimal APIs, Services, BackgroundJobs, Middleware
└── InsightLearn.WebAssembly/    # Blazor WASM frontend: Pages, Components, Services
```

### Request Flow

```text
Browser → Cloudflare → Nginx (in WASM pod) → Backend API (api-service.insightlearn.svc.cluster.local)
                                            ↘ Static Blazor WASM files (/_framework/, css/, js/)
```

The WASM pod runs Nginx which serves both static Blazor files AND proxies `/api/*` requests to the backend API pod. This is configured in `docker/wasm-nginx.conf`.

### Backend API (InsightLearn.Application)

- **Pattern**: ASP.NET Core Minimal APIs (NOT controllers)
- **Main endpoint registration**: `Program.cs` (inline `app.MapPost()`/`app.MapGet()` calls)
- **Additional endpoints**: `Endpoints/` folder for Subscriptions, Payouts, Stripe Webhooks, Engagement, Video Rendering
- **Background jobs**: Hangfire (`BackgroundJobs/`) — transcription, translation, subtitle generation, SEO metrics
- **AI services**: Ollama (chat, translations, AI takeaways), faster-whisper-server (video transcription), Qdrant (vector search)
- **Video storage**: MongoDB GridFS for video files, streaming via `/api/video/` endpoints
- **Health check**: `/health` endpoint, `/api/info` for version info

### Frontend (InsightLearn.WebAssembly)

- **Service layer**: Interface-based DI (`ICourseService`→`CourseService`), registered in `Program.cs`
- **HTTP client**: `Services/Http/ApiClient.cs` as base, uses `HostEnvironment.BaseAddress` (relative to nginx)
- **Auth**: JWT tokens in localStorage via `TokenService.cs`, `JwtAuthenticationStateProvider`
- **Component areas**: `Components/LearningSpace/` (video player, transcripts, notes, AI chat), `Components/Admin/` (charts, KPIs), `Pages/Admin/` (dashboard, user/course management)
- **SEO**: Nginx serves pre-rendered HTML snapshots from `wwwroot/seo-snapshots/` to crawlers; dynamic course snapshots proxied to API `/api/seo/course-snapshot/{id}`

### Databases

| Database   | Port  | Purpose                                         |
| ---------- | ----- | ----------------------------------------------- |
| SQL Server | 1433  | Main relational data, Hangfire, SystemEndpoints |
| MongoDB    | 27017 | Video files (GridFS), document storage          |
| Redis      | 6379  | Cache layer                                     |
| Qdrant     | 6334  | Vector search for AI features                   |

### K8s NodePort Services

- API: 31081
- WASM: 31090
- Grafana: 31300

## Key Files

- `Directory.Build.props` — Centralized version (all projects inherit)
- `src/InsightLearn.Application/Program.cs` — API DI setup + most endpoint definitions
- `src/InsightLearn.WebAssembly/Program.cs` — WASM DI setup (all service registrations)
- `docker/wasm-nginx.conf` — Nginx config: API proxy, static caching, SEO crawler routing
- `src/InsightLearn.Infrastructure/Data/InsightLearnDbContext.cs` — EF context + seed data
- `Dockerfile.wasm` / `Dockerfile.api` — Multi-stage production builds

## Useful Commands

```bash
# Pod status
kubectl get pods -n insightlearn

# API logs
kubectl logs -n insightlearn deployment/insightlearn-api --tail=50

# WASM logs
kubectl logs -n insightlearn deployment/insightlearn-wasm-blazor-webassembly --tail=50

# Restart deployments
kubectl rollout restart deployment/insightlearn-api -n insightlearn
kubectl rollout restart deployment/insightlearn-wasm-blazor-webassembly -n insightlearn

# Secret rotation (automatic with rollback)
sudo /home/mpasqui/insightlearn_WASM/InsightLearn_WASM/scripts/rotate-secrets-production-safe.sh
```

## Known Patterns & Pitfalls

- **Razor @code complexity**: .NET 8 Razor Source Generator can break with nested DTOs in `@code` blocks. Use code-behind (`.razor.cs`) for complex components.
- **Blazor WASM Scoped services**: `EndpointsConfig` must be Scoped (not Singleton) to avoid deadlocks with async loading.
- **K3s node name**: Must stay `insightlearn-k3s` — changing it loses all pods.
- **JSON casing**: Backend API uses PascalCase (`Program.cs` serializer config). Frontend expects this.
- **HA Watchdog**: Auto-heals cluster every 2 min via systemd timer. Logs at `/var/log/insightlearn-watchdog.log`.
