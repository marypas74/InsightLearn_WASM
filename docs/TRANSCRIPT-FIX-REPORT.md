# Report Tecnico: Fix Trascrizioni con Ollama/FasterWhisper

**Data**: 2026-01-21
**Versione**: 2.3.76-dev
**Autore**: Claude Code Analysis

---

## Problema Identificato

Il tab "Transcript" nella pagina Learn mostra "Transcript not available" perché:
1. La trascrizione non esiste per la lezione `56ba2a86-a006-44cf-a9ce-bdd917506d3a`
2. Il sistema è configurato per usare **OpenAI Whisper** di default, ma l'API key non è configurata
3. Il servizio **FasterWhisper** (locale) è attivo e funzionante ma NON viene usato come default

---

## Stato Attuale dei Servizi

| Servizio | Pod | Stato | Note |
|----------|-----|-------|------|
| faster-whisper | `faster-whisper-c9576847c-l5cxv` | Running | Health check OK, risponde a POST |
| ollama | `ollama-0` | Running | 2/2 containers |
| whisper-proxy | `whisper-proxy-85bc9577d-tcp2z` | Running | Proxy per routing |

### Log FasterWhisper (ultimi eventi)
```
POST /v1/audio/transcriptions HTTP/1.1" 200 OK
GET /health HTTP/1.1" 200 OK
```
**Il servizio FasterWhisper funziona correttamente.**

---

## Causa Root

### File: `src/InsightLearn.Application/Program.cs` (linea 667)

```csharp
// PROBLEMA: IWhisperTranscriptionService usa OpenAIWhisperService di default
builder.Services.AddScoped<IWhisperTranscriptionService, OpenAIWhisperService>();
```

Questo significa che:
- L'endpoint `/api/transcripts/generate` usa OpenAI (che richiede API key)
- Il job Hangfire `TranscriptGenerationJob` usa OpenAI
- FasterWhisper è disponibile ma non viene usato automaticamente

---

## Soluzione Proposta

### Opzione 1: Cambiare il Default a FasterWhisper (Consigliato)

**File**: `src/InsightLearn.Application/Program.cs`

```csharp
// PRIMA (linea 667):
builder.Services.AddScoped<IWhisperTranscriptionService, OpenAIWhisperService>();

// DOPO:
builder.Services.AddScoped<IWhisperTranscriptionService, WhisperTranscriptionService>();
```

**Pro**: Semplice, usa risorse locali, nessun costo API
**Contro**: Richiede rebuild e deploy API

---

### Opzione 2: Configurazione Dinamica via appsettings.json

**File**: `src/InsightLearn.Application/Program.cs`

```csharp
// Leggi provider da configurazione
var transcriptionProvider = builder.Configuration["Transcription:Provider"] ?? "FasterWhisper";

if (transcriptionProvider == "OpenAI")
{
    builder.Services.AddScoped<IWhisperTranscriptionService, OpenAIWhisperService>();
}
else
{
    builder.Services.AddScoped<IWhisperTranscriptionService, WhisperTranscriptionService>();
}
```

**File**: `appsettings.json`
```json
{
  "Transcription": {
    "Provider": "FasterWhisper"
  }
}
```

**Pro**: Configurabile senza rebuild
**Contro**: Più complesso

---

### Opzione 3: Usare l'Endpoint Parallel (Già Implementato)

L'endpoint `/api/transcripts/generate-parallel` usa entrambi i provider (OpenAI + FasterWhisper) con fallback automatico.

**Modifica richiesta**: Aggiornare il job Hangfire per usare `ParallelTranscriptionService` invece di `IWhisperTranscriptionService`.

**File**: `src/InsightLearn.Application/BackgroundJobs/TranscriptGenerationJob.cs`

```csharp
// Usare ParallelTranscriptionService con fallback a FasterWhisper se OpenAI non disponibile
```

---

## File da Modificare

| File | Azione |
|------|--------|
| `Program.cs:667` | Cambiare registrazione DI da OpenAI a FasterWhisper |
| `TranscriptGenerationJob.cs` | Usare ParallelTranscriptionService o WhisperTranscriptionService |
| `appsettings.json` | Aggiungere configurazione provider (opzionale) |

---

## Test di Verifica

### 1. Verificare FasterWhisper è raggiungibile
```bash
kubectl exec -n insightlearn deployment/insightlearn-api -- \
  wget -qO- http://faster-whisper-service:8000/health
```

### 2. Test manuale trascrizione (dopo fix)
```bash
curl -X POST "http://127.0.0.1:31081/api/transcripts/generate" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <TOKEN>" \
  -d '{"lessonId":"56ba2a86-a006-44cf-a9ce-bdd917506d3a","language":"en"}'
```

### 3. Verificare trascrizione creata
```bash
curl "http://127.0.0.1:31081/api/transcripts/56ba2a86-a006-44cf-a9ce-bdd917506d3a"
```

---

## Priorità Implementazione

1. **URGENTE**: Modificare `Program.cs` linea 667 per usare `WhisperTranscriptionService`
2. **MEDIO**: Aggiornare `TranscriptGenerationJob` per consistenza
3. **BASSO**: Aggiungere configurazione dinamica in appsettings.json

---

## Versione Deploy

Dopo le modifiche, incrementare versione a **2.3.77-dev** e fare deploy:

```bash
# API
podman build --no-cache -f Dockerfile -t localhost/insightlearn/api:2.3.77-dev .
podman save localhost/insightlearn/api:2.3.77-dev -o /tmp/api.tar
sudo k3s ctr images import /tmp/api.tar
kubectl set image deployment/insightlearn-api -n insightlearn api=localhost/insightlearn/api:2.3.77-dev
```

---

## Riepilogo

| Componente | Stato | Azione |
|------------|-------|--------|
| Video Playback | OK | Nessuna |
| FasterWhisper Service | OK (Running) | Nessuna |
| Transcription Default Provider | KO (usa OpenAI) | Cambiare a FasterWhisper |
| Transcript per lezione test | KO (non esiste) | Generare dopo fix |

