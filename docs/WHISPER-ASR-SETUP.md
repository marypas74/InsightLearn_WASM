# Whisper ASR Service Setup Guide

**Automatic Speech Recognition** for video transcript generation (Student Learning Space v2.1.0)

## Overview

InsightLearn uses **Whisper** (OpenAI's open-source ASR model) for automatic video transcription. This document explains how to deploy and configure the Whisper service.

---

## Deployment Options

### Option 1: Self-Hosted Whisper (Recommended)

**Docker Compose** deployment with GPU support:

```yaml
# docker-compose.yml (add to existing services)
services:
  whisper:
    image: onerahmet/openai-whisper-asr-webservice:latest
    container_name: whisper-service
    ports:
      - "9000:9000"
    environment:
      - ASR_MODEL=large-v3  # Best accuracy (6GB VRAM required)
      # Alternative models: base, small, medium, large-v2
      - ASR_ENGINE=faster_whisper  # Optimized inference engine
    deploy:
      resources:
        reservations:
          devices:
            - driver: nvidia
              count: 1
              capabilities: [gpu]
    volumes:
      - whisper-models:/root/.cache/whisper
    networks:
      - insightlearn

volumes:
  whisper-models:

networks:
  insightlearn:
    external: true
```

**Start the service**:
```bash
docker-compose up -d whisper
```

**Test the service**:
```bash
curl -X POST http://localhost:9000/asr \
  -H "Content-Type: application/json" \
  -d '{
    "audio_url": "https://example.com/sample.mp4",
    "language": "en",
    "task": "transcribe",
    "word_timestamps": true
  }'
```

---

### Option 2: Kubernetes Deployment

**Create Whisper deployment**:

```yaml
# k8s/19-whisper-deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: whisper
  namespace: insightlearn
spec:
  replicas: 1
  selector:
    matchLabels:
      app: whisper
  template:
    metadata:
      labels:
        app: whisper
    spec:
      containers:
      - name: whisper
        image: onerahmet/openai-whisper-asr-webservice:latest
        env:
        - name: ASR_MODEL
          value: "large-v3"
        - name: ASR_ENGINE
          value: "faster_whisper"
        ports:
        - containerPort: 9000
        resources:
          limits:
            nvidia.com/gpu: 1  # Requires NVIDIA GPU
            memory: "8Gi"
          requests:
            memory: "4Gi"
            cpu: "2"
        volumeMounts:
        - name: whisper-models
          mountPath: /root/.cache/whisper
      volumes:
      - name: whisper-models
        persistentVolumeClaim:
          claimName: whisper-models-pvc
---
apiVersion: v1
kind: Service
metadata:
  name: whisper-service
  namespace: insightlearn
spec:
  selector:
    app: whisper
  ports:
  - port: 9000
    targetPort: 9000
  type: ClusterIP
---
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: whisper-models-pvc
  namespace: insightlearn
spec:
  accessModes:
    - ReadWriteOnce
  resources:
    requests:
      storage: 10Gi
  storageClassName: local-path
```

**Deploy to Kubernetes**:
```bash
kubectl apply -f k8s/19-whisper-deployment.yaml

# Wait for pod to be ready (may take 5-10 minutes to download model)
kubectl wait --for=condition=ready pod -l app=whisper -n insightlearn --timeout=600s

# Check logs
kubectl logs -f -l app=whisper -n insightlearn
```

---

### Option 3: OpenAI Whisper API (Cloud)

**No self-hosting required** - use OpenAI's hosted API.

**Requirements**:
- OpenAI API key
- Costs: $0.006 per minute of audio

**Configuration** (`appsettings.json`):
```json
{
  "Whisper": {
    "BaseUrl": "https://api.openai.com/v1/audio/transcriptions",
    "ApiKey": "REPLACE_WITH_OPENAI_API_KEY",
    "Model": "whisper-1",
    "Timeout": 600
  }
}
```

**Or via environment variable**:
```bash
export WHISPER_BASE_URL="https://api.openai.com/v1/audio/transcriptions"
export WHISPER_API_KEY="sk-..."
```

**⚠️ Note**: Requires modifying `VideoTranscriptService.cs` to add Authorization header.

---

## Configuration

### Application Settings

**File**: `src/InsightLearn.Application/appsettings.json`

```json
{
  "Whisper": {
    "BaseUrl": "http://whisper-service:9000",
    "Timeout": 600
  }
}
```

### Environment Variables (Production)

**Kubernetes Secret**:
```bash
kubectl create secret generic whisper-config \
  --from-literal=base-url=http://whisper-service.insightlearn.svc.cluster.local:9000 \
  -n insightlearn
```

**Update API deployment** to inject env var:
```yaml
# k8s/06-api-deployment.yaml
env:
- name: Whisper__BaseUrl
  valueFrom:
    secretKeyRef:
      name: whisper-config
      key: base-url
```

---

## Performance Tuning

### Model Selection

| Model | VRAM | Speed | Accuracy | Use Case |
|-------|------|-------|----------|----------|
| **tiny** | 1 GB | 32x realtime | 80% | Testing only |
| **base** | 1 GB | 16x realtime | 85% | Quick previews |
| **small** | 2 GB | 6x realtime | 90% | Fast transcription |
| **medium** | 5 GB | 2x realtime | 95% | Balanced (recommended for most) |
| **large-v2** | 10 GB | 1x realtime | 98% | High accuracy |
| **large-v3** | 10 GB | 1x realtime | 99% | Best accuracy (SOTA) |

### Hardware Requirements

**Minimum (CPU-only)**:
- CPU: 4 cores
- RAM: 8 GB
- Disk: 10 GB
- Speed: ~0.2x realtime (5min video = 25min processing)

**Recommended (GPU)**:
- GPU: NVIDIA GTX 1660 Ti or better (6GB VRAM)
- RAM: 16 GB
- Disk: 20 GB
- Speed: ~5x realtime (5min video = 1min processing)

**Production (GPU)**:
- GPU: NVIDIA RTX 4090 or A100 (24GB VRAM)
- RAM: 32 GB
- Disk: 50 GB (for model cache + logs)
- Speed: ~15x realtime (5min video = 20sec processing)

---

## API Endpoint Documentation

### POST /asr

Generate transcript from audio/video URL.

**Request**:
```json
{
  "audio_url": "https://example.com/video.mp4",
  "language": "en",
  "task": "transcribe",
  "word_timestamps": true
}
```

**Response**:
```json
{
  "text": "Full transcript text...",
  "segments": [
    {
      "start": 0.0,
      "end": 2.5,
      "text": "Hello and welcome to this video.",
      "confidence": 0.95
    }
  ],
  "language": "en"
}
```

**Parameters**:
- `audio_url` (string, required): URL to audio/video file
- `language` (string, optional): ISO 639-1 language code (e.g., "en", "it", "es")
- `task` (string, optional): "transcribe" or "translate" (default: "transcribe")
- `word_timestamps` (boolean, optional): Include word-level timestamps (default: false)

**Supported Formats**: MP3, MP4, WAV, M4A, FLAC, OGG, WEBM

---

## Troubleshooting

### Issue: Whisper service not responding

**Check logs**:
```bash
kubectl logs -f -l app=whisper -n insightlearn
```

**Common causes**:
1. Model still downloading (check logs for "Downloading model...")
2. Out of memory (increase pod resources)
3. No GPU available (check `nvidia-smi`)

**Fix**:
```bash
# Restart pod
kubectl delete pod -l app=whisper -n insightlearn

# Check resources
kubectl describe pod -l app=whisper -n insightlearn
```

---

### Issue: Transcription fails with timeout

**Cause**: Video too long or server too slow.

**Fix**: Increase timeout in `appsettings.json`:
```json
{
  "Whisper": {
    "Timeout": 1200  // 20 minutes
  }
}
```

---

### Issue: Poor transcription quality

**Causes**:
1. Wrong language specified
2. Poor audio quality
3. Model too small

**Fix**:
1. Specify correct language: `"language": "it"` for Italian
2. Use larger model: `ASR_MODEL=large-v3`
3. Pre-process audio (noise reduction)

---

## Monitoring

### Prometheus Metrics

Whisper service exposes metrics at `/metrics`:

```yaml
# Example metrics
whisper_transcription_duration_seconds{model="large-v3"} 12.5
whisper_transcription_total{status="success"} 150
whisper_transcription_total{status="failed"} 3
whisper_model_memory_bytes{model="large-v3"} 6442450944
```

### Grafana Dashboard

Add panel to track transcription performance:

```promql
# Average transcription time
rate(whisper_transcription_duration_seconds_sum[5m]) / rate(whisper_transcription_duration_seconds_count[5m])

# Success rate
100 * rate(whisper_transcription_total{status="success"}[5m]) / rate(whisper_transcription_total[5m])
```

---

## Cost Analysis

### Self-Hosted (GPU)

**Hardware**: NVIDIA RTX 4090 ($1,600) or cloud GPU instance
- **Cloud GPU** (AWS g5.xlarge): $1.006/hour = $~730/month (continuous)
- **Electricity**: ~350W * $0.15/kWh * 730h = $~40/month
- **Total**: $770/month for 24/7 availability

**Transcription capacity**: ~5,000 hours/month (assuming 10x realtime speed)
**Cost per hour**: $0.15/hour of video

### OpenAI Whisper API

**Pricing**: $0.006/minute = $0.36/hour of audio
**No infrastructure**: Pay-as-you-go

**Break-even**: ~2,140 hours/month (beyond this, self-hosting is cheaper)

---

## Alternative: Azure Speech Services

**If Whisper is not an option**, use Azure:

```json
{
  "AzureSpeech": {
    "SubscriptionKey": "REPLACE_WITH_AZURE_KEY",
    "Region": "westeurope",
    "Language": "en-US"
  }
}
```

**Pricing**: $1.00/hour for standard, $2.50/hour for custom models

**⚠️ Note**: Requires implementing `AzureSpeechService` class (not included).

---

## Support

**Whisper Docker Image**: https://github.com/ahmetoner/whisper-asr-webservice
**Whisper Model**: https://github.com/openai/whisper
**InsightLearn Docs**: https://github.com/marypas74/InsightLearn_WASM

**Issues**: Report at https://github.com/marypas74/InsightLearn_WASM/issues
