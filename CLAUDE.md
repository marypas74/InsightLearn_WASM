# CLAUDE.md - InsightLearn WASM

## Quick Info
- **Versione**: 2.3.23-dev (in Directory.Build.props)
- **Stack**: .NET 8, Blazor WASM, ASP.NET Core Web API
- **Deploy**: K3s Kubernetes su Rocky Linux 10
- **Docs completa**: CLAUDE-FULL.md

## Regole Critiche

### Build (OBBLIGATORIO)
1. Incrementare SEMPRE VersionPrefix in Directory.Build.props prima di build
2. Chiedere conferma all utente prima di build/deploy

### Comandi Build
```
podman build -f Dockerfile.wasm -t localhost/insightlearn/wasm:X.X.X-dev .
podman save localhost/insightlearn/wasm:X.X.X-dev -o /tmp/wasm.tar
sudo /usr/local/bin/k3s ctr images import /tmp/wasm.tar
kubectl set image deployment/insightlearn-wasm-blazor-webassembly -n insightlearn wasm-blazor=localhost/insightlearn/wasm:X.X.X-dev
```

## Struttura Progetto
- src/InsightLearn.Core/ - Entities, DTOs, Interfaces
- src/InsightLearn.Infrastructure/ - Repositories, DbContext
- src/InsightLearn.Application/ - API (Program.cs Minimal APIs)
- src/InsightLearn.WebAssembly/ - Blazor WASM Frontend
- k8s/ - Kubernetes manifests

## File Chiave
- Directory.Build.props - Versione centralizzata
- src/InsightLearn.Application/Program.cs - API endpoints
- docker/wasm-nginx.conf - Nginx config WASM
- k8s/06-api-deployment.yaml - API deployment

## Database
- SQL Server: porta 1433
- MongoDB: porta 27017 (video GridFS)
- Redis: porta 6379 (cache)

## Porte K8s NodePort
- API: 31081
- WASM: 31090
- Grafana: 31300

## Comandi Utili
```
kubectl get pods -n insightlearn
kubectl logs -n insightlearn deployment/insightlearn-api --tail=50
kubectl rollout restart deployment/insightlearn-api -n insightlearn
```

## Docs completa
Per info dettagliate chiedi di leggere CLAUDE-FULL.md
