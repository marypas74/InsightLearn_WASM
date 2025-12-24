# Deployment Guide - InsightLearn Student Learning Space v2.1.0-dev

**Date**: 2025-11-19
**Version**: 2.1.0-dev
**Status**: Frontend Ready for Deployment | Backend Phase 2 has compilation errors

## Overview

This guide covers the deployment of the **Student Learning Space v2.1.0-dev** frontend to the Kubernetes (K3s) cluster.

**What's Included**:
- ✅ Phase 1: Database schema (5 SQL Server entities, 3 MongoDB collections, 5 repositories)
- ✅ Phase 3: 31 API endpoints (Swagger documented)
- ✅ Phase 4: Frontend components (12 Blazor components, 6 API client services, responsive CSS)
- ⚠️ Phase 2: Backend services (21 compilation errors - NOT included in this deployment)

**Deployment Strategy**: Frontend-only deployment using v2.1.0-dev, backend remains on previous version.

---

## Prerequisites

1. **Docker image built**: `localhost/insightlearn/wasm:2.1.0-dev` ✅ (completed)
2. **Docker image tar file**: `/tmp/wasm-blazor-2.1.0-dev.tar` ✅ (created)
3. **Sudo access**: Required for K3s containerd import
4. **K3s cluster**: Running and accessible via kubectl ✅

---

## Deployment Steps

### Step 1: Import Docker Image into K3s Containerd

The image needs to be imported into K3s containerd before Kubernetes can use it (imagePullPolicy: Never).

**Command** (requires sudo password):
```bash
sudo /usr/local/bin/k3s ctr images import /tmp/wasm-blazor-2.1.0-dev.tar
```

**Expected Output**:
```
unpacking localhost/insightlearn/wasm:2.1.0-dev (sha256:...)... done
```

**Verification**:
```bash
sudo /usr/local/bin/k3s ctr images ls | grep "localhost/insightlearn/wasm"
```

**Expected Output**:
```
localhost/insightlearn/wasm:2.1.0-dev
localhost/insightlearn/wasm:latest
```

---

### Step 2: Restart WebAssembly Deployment

Force Kubernetes to pull the new image and restart pods:

```bash
kubectl rollout restart deployment/insightlearn-wasm-blazor-webassembly -n insightlearn
```

**Expected Output**:
```
deployment.apps/insightlearn-wasm-blazor-webassembly restarted
```

---

### Step 3: Wait for Rollout to Complete

Monitor the deployment rollout:

```bash
kubectl rollout status deployment/insightlearn-wasm-blazor-webassembly -n insightlearn --timeout=120s
```

**Expected Output**:
```
Waiting for deployment "insightlearn-wasm-blazor-webassembly" rollout to finish: 1 old replicas are pending termination...
Waiting for deployment "insightlearn-wasm-blazor-webassembly" rollout to finish: 1 of 1 updated replicas are available...
deployment "insightlearn-wasm-blazor-webassembly" successfully rolled out
```

---

### Step 4: Verify New Pod is Running

Check that the new pod is running with the updated image:

```bash
kubectl get pods -n insightlearn | grep wasm
```

**Expected Output**:
```
insightlearn-wasm-blazor-webassembly-xxxxxxxxxx-xxxxx   1/1     Running   0          30s
```

**Detailed Pod Information**:
```bash
kubectl describe pod -n insightlearn -l app=insightlearn-wasm-blazor-webassembly | grep Image:
```

**Expected Output**:
```
Image: localhost/insightlearn/wasm:latest
```

---

### Step 5: Access the Application

**NodePort Access** (direct):
```
http://localhost:31090
```

**Cloudflare Tunnel** (production):
```
https://www.insightlearn.cloud
```

---

## Verification Steps

### 1. Check Frontend Version

Open browser DevTools (F12) and check console for version logs:

Expected:
```
InsightLearn WebAssembly application starting...
Base Address: http://localhost:31090/
```

### 2. Test Student Learning Space Components

Navigate to a video lesson page and verify:

- ✅ Student Notes panel loads
- ✅ Video Progress indicator displays
- ✅ AI Takeaways panel loads (may show "No takeaways available" - backend Phase 2 not deployed)
- ✅ Video Transcript viewer loads (may show "No transcript available" - backend Phase 2 not deployed)

### 3. Check Browser Console for Errors

Expected: No errors related to frontend components
Possible: 404 errors for backend Phase 2 API endpoints (acceptable - not deployed yet)

### 4. Verify Responsive Design

Test on:
- Desktop (1024px+): 3-column layout
- Tablet (768px-1023px): 2-column layout with toggleable sidebars
- Mobile (< 768px): Single column with bottom nav

---

## Rollback Procedure

If the deployment fails or causes issues:

### Option 1: Rollback to Previous Version

```bash
kubectl rollout undo deployment/insightlearn-wasm-blazor-webassembly -n insightlearn
```

### Option 2: Scale Down and Investigate

```bash
kubectl scale deployment/insightlearn-wasm-blazor-webassembly -n insightlearn --replicas=0
# Investigate logs
kubectl logs -n insightlearn -l app=insightlearn-wasm-blazor-webassembly --tail=100
# Scale back up
kubectl scale deployment/insightlearn-wasm-blazor-webassembly -n insightlearn --replicas=1
```

---

## Troubleshooting

### Issue 1: Image Not Found (ImagePullBackOff)

**Symptom**:
```
kubectl get pods -n insightlearn | grep wasm
insightlearn-wasm-blazor-webassembly-xxx   0/1     ImagePullBackOff
```

**Cause**: Image not imported into K3s containerd

**Solution**:
```bash
# Verify image exists in Docker
docker images | grep insightlearn/wasm

# Re-import into K3s
sudo /usr/local/bin/k3s ctr images import /tmp/wasm-blazor-2.1.0-dev.tar
```

---

### Issue 2: Pod CrashLoopBackOff

**Symptom**:
```
kubectl get pods -n insightlearn | grep wasm
insightlearn-wasm-blazor-webassembly-xxx   0/1     CrashLoopBackOff
```

**Investigation**:
```bash
# Check pod logs
kubectl logs -n insightlearn -l app=insightlearn-wasm-blazor-webassembly --tail=50

# Check pod events
kubectl describe pod -n insightlearn -l app=insightlearn-wasm-blazor-webassembly
```

**Common Causes**:
- Nginx configuration error
- Missing static files
- Port binding conflict

---

### Issue 3: 404 Errors for API Endpoints

**Expected Behavior**: Frontend deployed, backend Phase 2 NOT deployed

**Acceptable 404 Endpoints**:
- `/api/transcripts/*` - VideoTranscriptService (Phase 2)
- `/api/takeaways/*` - AIAnalysisService (Phase 2)
- `/api/notes/*` - StudentNoteService (Phase 2)
- `/api/bookmarks/*` - VideoBookmarkService (Phase 2)
- `/api/ai-conversations/*` - AIConversationService (Phase 2)

**Mitigation**: These features will display "Not Available" until backend Phase 2 is fixed and deployed.

---

## Monitoring

### Pod Logs (Real-time)

```bash
kubectl logs -n insightlearn -l app=insightlearn-wasm-blazor-webassembly -f
```

### Deployment Status

```bash
kubectl get deployment insightlearn-wasm-blazor-webassembly -n insightlearn
```

### Resource Usage

```bash
kubectl top pod -n insightlearn | grep wasm
```

---

## Next Steps

### 1. Fix Backend Phase 2 Compilation Errors (21 errors)

**Files to Fix**:
- `src/InsightLearn.Application/Services/VideoTranscriptService.cs`
- `src/InsightLearn.Application/Services/AIAnalysisService.cs`
- `src/InsightLearn.Application/Services/VideoProgressService.cs`
- `src/InsightLearn.Application/BackgroundJobs/TranscriptGenerationJob.cs`
- `src/InsightLearn.Application/BackgroundJobs/AITakeawayGenerationJob.cs`

**Error Categories**:
- DTO property mismatches (Segments, FullTranscript, ProcessingModel, Status, Progress)
- Repository method signature mismatch (GetByIdAsync parameters)
- Type conversion (decimal to double)

**Priority**: HIGH - Required for full feature functionality

### 2. Database Migration Deployment

Once backend is fixed, apply database migration:

```bash
# Via API startup (automatic)
# OR manual:
dotnet ef database update --project src/InsightLearn.Infrastructure --startup-project src/InsightLearn.Application
```

### 3. MongoDB Collections Setup

Execute MongoDB setup job:

```bash
kubectl apply -f k8s/18-mongodb-setup-job.yaml
kubectl wait --for=condition=complete job/mongodb-setup -n insightlearn --timeout=300s
kubectl logs -n insightlearn job/mongodb-setup
```

### 4. Full Backend Deployment

After fixing Phase 2 errors:

```bash
# Build API Docker image
docker build -f Dockerfile -t localhost/insightlearn/api:2.1.0-dev .

# Import into K3s
docker save localhost/insightlearn/api:2.1.0-dev | sudo /usr/local/bin/k3s ctr images import -

# Restart API deployment
kubectl rollout restart deployment/insightlearn-api -n insightlearn
```

---

## Summary

**Current Status**:
- ✅ Frontend v2.1.0-dev: Built and ready to deploy
- ✅ Docker image: Created (`localhost/insightlearn/wasm:2.1.0-dev`)
- ⏸️ Deployment: Awaiting sudo password for K3s import
- ⚠️ Backend Phase 2: 21 compilation errors, not deployable

**Manual Deployment Required**:
Run the command in Step 1 with sudo password, then proceed with Steps 2-5.

**Estimated Deployment Time**: 3-5 minutes (excluding sudo password entry)

**Risk Assessment**:
- **Low Risk**: Frontend-only deployment, backend remains on previous stable version
- **Limited Functionality**: Student Learning Space features will show "Not Available" until backend Phase 2 deployed
- **Rollback**: Easy - single kubectl rollout undo command

---

## Contact

For deployment issues:
- Email: marcello.pasqui@gmail.com
- GitHub Issues: https://github.com/marypas74/InsightLearn_WASM/issues

---

**Document Version**: 1.0
**Last Updated**: 2025-11-19
**Author**: Claude Code (AI Assistant)
