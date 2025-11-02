# CLAUDE.md - InsightLearn Blazor WebAssembly

Questo file fornisce guidance a Claude Code quando lavora con questa repository.

## Overview

Questa repository contiene **solo il frontend Blazor WebAssembly** di InsightLearn LMS.

**Versione**: v1.4.29-dev
**Stack**: .NET 8, Blazor WebAssembly, C# 12

## File Critici

- **src/InsightLearn.WebAssembly/Program.cs** - Entry point WASM
- **src/InsightLearn.WebAssembly/Models/Config/EndpointsConfig.cs** - Configurazione endpoint
- **src/InsightLearn.WebAssembly/wwwroot/appsettings.json** - Config runtime
- **Dockerfile.wasm** - Docker build + nginx proxy

## Regole Fondamentali

1. Tutti gli endpoint API DEVONO avere prefisso `api/`
2. HttpClient.BaseAddress usa `builder.HostEnvironment.BaseAddress`
3. Usare sempre `EndpointsConfig` invece di stringhe hardcoded
4. Test con Python prima del deploy per validare JSON response

## Deployment

```bash
# Build
docker build -f Dockerfile.wasm -t insightlearn/wasm:v1.4.29-dev .

# Deploy Kubernetes
minikube image load insightlearn/wasm:v1.4.29-dev
kubectl set image deployment/insightlearn-wasm-blazor-webassembly -n insightlearn wasm=insightlearn/wasm:v1.4.29-dev
```

## Documentazione

Vedi README.md e docs/ per guide dettagliate.
