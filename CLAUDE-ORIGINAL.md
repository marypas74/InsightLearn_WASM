# CLAUDE.md - InsightLearn Blazor WebAssembly

Questo file fornisce guidance a Claude Code quando lavora con questa repository.

## Overview

Questa repository contiene **solo il frontend Blazor WebAssembly** di InsightLearn Learning Management System.

**Tipo progetto**: .NET 8 Blazor WebAssembly  
**Versione corrente**: v1.4.29-dev  
**Linguaggio principale**: C# 12, Razor, HTML/CSS/JavaScript

## Struttura Repository

```
insightlearn-wasm/
├── CLAUDE.md                          # Questo file
├── README.md                          # Documentazione principale
├── Dockerfile.wasm                    # Docker build per WASM
├── Directory.Build.props              # Configurazione build condivisa
├── InsightLearn.WASM.sln             # Visual Studio solution
├── src/                               # Codice sorgente
│   ├── InsightLearn.WebAssembly/     # ⭐ Progetto principale WASM
│   │   ├── Pages/                     # Pagine Razor (.razor)
│   │   ├── Components/                # Componenti riutilizzabili
│   │   ├── Services/                  # Servizi (Auth, HTTP, Config)
│   │   ├── Models/                    # DTO e modelli
│   │   ├── wwwroot/                   # Asset statici + appsettings.json
│   │   └── Program.cs                 # Entry point WASM
│   ├── InsightLearn.Core/             # Domain models condivisi
│   ├── InsightLearn.Infrastructure/   # Implementazioni infrastruttura
│   └── InsightLearn.Application/      # Business logic e DTOs
└── docs/                              # Documentazione tecnica
    ├── WASM-*.md                      # Guide specifiche WASM
    ├── DEPLOYMENT-SUMMARY.md          # Deployment su Kubernetes
    └── MONITORING-GUIDE.md            # Monitoring e logging
```

## Comandi Comuni

### Development Locale

```bash
# Restore NuGet packages
dotnet restore

# Build progetto
dotnet build src/InsightLearn.WebAssembly/InsightLearn.WebAssembly.csproj -c Release

# Run in development (con hot reload)
dotnet watch run --project src/InsightLearn.WebAssembly/InsightLearn.WebAssembly.csproj

# Publish per deployment
dotnet publish src/InsightLearn.WebAssembly/InsightLearn.WebAssembly.csproj -c Release -o ./publish
```

### Docker Build

```bash
# Build immagine Docker
docker build -f Dockerfile.wasm -t insightlearn/wasm:v1.4.29-dev .

# Tag versione latest
docker tag insightlearn/wasm:v1.4.29-dev insightlearn/wasm:latest

# Run container localmente
docker run -p 8080:80 insightlearn/wasm:latest

# Test container
curl http://localhost:8080
```

### Kubernetes Deployment

```bash
# Load immagine in minikube (development)
minikube image load insightlearn/wasm:v1.4.29-dev

# Deploy a Kubernetes
kubectl apply -f k8s/12-wasm-deployment.yaml

# Update deployment con nuova versione
kubectl set image deployment/insightlearn-wasm-blazor-webassembly \
  -n insightlearn wasm=insightlearn/wasm:v1.4.29-dev

# Check rollout status
kubectl rollout status deployment/insightlearn-wasm-blazor-webassembly -n insightlearn

# View logs
kubectl logs -n insightlearn -l app=insightlearn-wasm-blazor-webassembly -f

# Get pod status
kubectl get pods -n insightlearn -l app=insightlearn-wasm-blazor-webassembly
```

## File Importanti

### [src/InsightLearn.WebAssembly/Program.cs](src/InsightLearn.WebAssembly/Program.cs)
Entry point dell'applicazione WASM. Configura:
- HttpClient con BaseAddress
- Dependency Injection per servizi
- Caricamento EndpointsConfig da appsettings.json
- Authentication state provider

**CRITICAL**: HttpClient.BaseAddress usa `builder.HostEnvironment.BaseAddress` (URL corrente), NON un URL hardcoded.

### [src/InsightLearn.WebAssembly/Models/Config/EndpointsConfig.cs](src/InsightLearn.WebAssembly/Models/Config/EndpointsConfig.cs)
Configurazione centralizzata di TUTTI gli endpoint API. Ogni modifica agli endpoint deve essere fatta qui.

**REGOLA CRITICA**: Tutti gli endpoint DEVONO includere il prefisso `api/`:
```csharp
public string Login { get; set; } = "api/auth/login";  // ✅ CORRETTO
public string Login { get; set; } = "auth/login";      // ❌ SBAGLIATO
```

### [src/InsightLearn.WebAssembly/wwwroot/appsettings.json](src/InsightLearn.WebAssembly/wwwroot/appsettings.json)
File JSON caricato runtime dal browser. Contiene:
- ApiSettings (timeout, version)
- Endpoints (tutti gli endpoint API)
- Authentication (token keys, expiration)

### [Dockerfile.wasm](Dockerfile.wasm)
Multi-stage Dockerfile:
1. **Stage build**: Compila .NET WASM
2. **Stage final**: Nginx Alpine con WASM + configurazione proxy

**Configurazione nginx embedded**:
- `/api/*` → Proxy verso API backend
- `/*` → Serve file statici WASM + fallback a index.html

## Architettura Tecnica

### Flusso Richieste HTTP

```
Browser
  ↓ HTTP Request: /api/auth/login
HttpClient (BaseAddress: http://192.168.49.2:31090/)
  ↓ Full URL: http://192.168.49.2:31090/api/auth/login
Nginx (nel container WASM)
  ↓ location /api/ { proxy_pass ... }
API Backend Service (Kubernetes ClusterIP)
  ↓ Response JSON
HttpClient
  ↓ Deserialize a AuthResponse
Blazor Component
```

### Authentication Flow

1. **Login Page** → `AuthService.LoginAsync()`
2. **HTTP POST** → `/api/auth/login`
3. **API Response** → `{ isSuccess: true, token: "...", user: {...} }`
4. **Token Storage** → `LocalStorage.SetItemAsync("InsightLearn.AuthToken", token)`
5. **State Update** → `JwtAuthenticationStateProvider.NotifyUserAuthentication(token)`
6. **Redirect** → `/dashboard`

### Configuration Loading

```
Browser carica index.html
  ↓
Blazor WASM inizializza
  ↓
Program.cs: builder.Build()
  ↓
Fetch /appsettings.json via HTTP
  ↓
Deserialize a EndpointsConfig
  ↓
Register as Singleton in DI container
  ↓
Services inject EndpointsConfig
```

## Regole di Sviluppo

### 1. Endpoint Configuration

**SEMPRE** usare `EndpointsConfig` invece di stringhe hardcoded:

```csharp
// ❌ SBAGLIATO
await _httpClient.PostAsJsonAsync("/api/auth/login", request);

// ✅ CORRETTO
await _httpClient.PostAsJsonAsync(_endpoints.Auth.Login, request);
```

### 2. HttpClient BaseAddress

**NON modificare** `HttpClient.BaseAddress` in Program.cs. Deve sempre usare:

```csharp
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress),  // ✅ CORRETTO
    Timeout = TimeSpan.FromSeconds(apiTimeout)
});
```

### 3. API Prefix

Tutti gli endpoint in `EndpointsConfig` e `appsettings.json` DEVONO avere prefisso `api/`:

```json
{
  "Endpoints": {
    "Auth": {
      "Login": "api/auth/login",     // ✅ CORRETTO
      "Register": "api/auth/register" // ✅ CORRETTO
    }
  }
}
```

### 4. Versioning

Quando cambi versione:
1. Aggiorna `Dockerfile.wasm` → `"Version": "v1.x.x-dev"`
2. Aggiorna `Directory.Build.props` → `<Version>1.x.x</Version>`
3. Build con nuovo tag: `docker build ... -t insightlearn/wasm:v1.x.x-dev`

### 5. Error Handling

Usa sempre try-catch con logging dettagliato:

```csharp
try
{
    _logger.LogInformation("Calling {Endpoint}", _endpoints.Auth.Login);
    var response = await _httpClient.PostAsJsonAsync(_endpoints.Auth.Login, request);
    
    if (!response.IsSuccessStatusCode)
    {
        var error = await response.Content.ReadAsStringAsync();
        _logger.LogError("HTTP {Status} from {Endpoint}: {Error}", 
            response.StatusCode, _endpoints.Auth.Login, error);
    }
}
catch (HttpRequestException ex)
{
    _logger.LogError(ex, "HTTP error calling {Endpoint}", _endpoints.Auth.Login);
    throw;
}
```

## Known Issues e Soluzioni

### Issue: "No response from server" dopo login

**Causa**: Endpoint configurato senza prefisso `api/`  
**Fix**: Verificare che endpoint in `appsettings.json` abbia `api/auth/login`

### Issue: Redirect a /complete-registration invece di /dashboard

**Causa**: Campo `IsProfileComplete` mancante in `UserDto` API  
**Fix**: Già risolto in v1.4.29-dev rimuovendo controllo in Login.razor:186

### Issue: Double `/api/api/` nelle richieste

**Causa**: BaseUrl="/api" + endpoint="api/auth/login"  
**Fix**: NON mettere BaseUrl in appsettings.json, solo negli endpoint

### Issue: CORS errors da browser

**Causa**: API backend non configurata per CORS  
**Fix**: Configurare CORS nell'API per accettare origin del frontend

## Testing

### Test Endpoint Locali

```bash
# Test con curl (simula richiesta browser)
curl -X POST http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@insightlearn.cloud","password":"Admin123!"}'

# Test con Python
python3 << 'EOF'
import requests
response = requests.post("http://localhost:8080/api/auth/login",
    json={"email":"admin@insightlearn.cloud","password":"Admin123!"})
print(f"Status: {response.status_code}")
print(f"Content-Type: {response.headers.get('Content-Type')}")
print(f"Body: {response.text[:200]}")
