# CORS Deployment Checklist

## Pre-Deployment Verification ✅

### 1. Code Changes
- [x] **Program.cs modified** with new CORS configuration
  - File: `/home/mpasqui/kubernetes/Insightlearn/src/InsightLearn.Api/Program.cs`
  - Lines 301-405: CORS configuration
  - Lines 485-517: CORS middleware

- [x] **Origins Added**
  - `https://new.insightlearn.cloud` (WASM production)
  - `https://192.168.1.103` (Direct IP)

- [x] **Security Features**
  - Explicit origin whitelist (no wildcards)
  - Credentials enabled (JWT support)
  - Preflight caching (24 hours)
  - Restricted methods (GET, POST, PUT, DELETE, PATCH, OPTIONS)
  - Restricted headers (Authorization, Content-Type, Accept)

- [x] **Middleware Order**
  - CORS → Forwarded Headers → Authentication → Authorization

### 2. Documentation Created
- [x] **CORS-IMPLEMENTATION.md** (22 KB) - Complete guide
- [x] **CORS-QUICK-REFERENCE.md** (6.4 KB) - Quick commands
- [x] **CORS-SUMMARY.md** (22 KB) - Executive summary
- [x] **test-cors.sh** (7.5 KB) - Testing script
- [x] **deploy-cors.sh** (8.1 KB) - Deployment script

### 3. Prerequisites Check
```bash
# Run these commands to verify prerequisites
docker --version          # Docker installed?
kubectl version --client  # kubectl installed?
minikube status          # minikube running?
```

- [ ] Docker installed and running
- [ ] kubectl installed and configured
- [ ] minikube running
- [ ] Access to `/home/mpasqui/kubernetes/Insightlearn` directory

---

## Deployment Steps

### Option A: Automated Deployment (Recommended)

```bash
cd /home/mpasqui/kubernetes/Insightlearn
./deploy-cors.sh
```

**What it does**:
1. Checks prerequisites
2. Backs up current Program.cs
3. Verifies CORS changes
4. Bumps version (patch)
5. Builds Docker image
6. Loads image to minikube
7. Deploys to Kubernetes
8. Verifies deployment
9. Optionally runs tests

**Time**: ~5-10 minutes

### Option B: Manual Deployment

If automated script fails, follow these manual steps:

#### Step 1: Backup Current Configuration
```bash
cd /home/mpasqui/kubernetes/Insightlearn/src/InsightLearn.Api
cp Program.cs Program.cs.backup-$(date +%Y%m%d-%H%M%S)
```
- [ ] Backup created

#### Step 2: Verify CORS Changes
```bash
grep -n "new.insightlearn.cloud" src/InsightLearn.Api/Program.cs
grep -n "SetPreflightMaxAge" src/InsightLearn.Api/Program.cs
```
- [ ] Origin `new.insightlearn.cloud` found
- [ ] Preflight caching configured

#### Step 3: Bump Version
```bash
cd /home/mpasqui/kubernetes/Insightlearn
./k8s/version.sh bump patch
```
- [ ] Version bumped (e.g., v1.2.17 → v1.2.18)

#### Step 4: Build Docker Image
```bash
cd /home/mpasqui/kubernetes/Insightlearn
export IMAGE_VERSION=$(grep -oP '(?<=<VersionPrefix>)[^<]+' Directory.Build.props)
echo "Building version: v$IMAGE_VERSION"

docker build -f Dockerfile --no-cache \
  -t insightlearn/api:v$IMAGE_VERSION \
  -t insightlearn/api:latest \
  . 2>&1 | tee /tmp/cors-build-$(date +%Y%m%d-%H%M%S).log
```
- [ ] Build started
- [ ] No errors in build log
- [ ] Image created successfully

**Check for build errors**:
```bash
grep -iE "error|failed|exception" /tmp/cors-build-*.log
```
- [ ] No errors found

#### Step 5: Load Image to Minikube
```bash
# Remove old images
minikube image rm insightlearn/api:latest
minikube image rm insightlearn/api:v$IMAGE_VERSION

# Load new images
minikube image load insightlearn/api:v$IMAGE_VERSION
minikube image load insightlearn/api:latest

# Verify loaded
minikube image ls | grep insightlearn/api
```
- [ ] Old images removed
- [ ] New images loaded
- [ ] Images visible in minikube

#### Step 6: Deploy to Kubernetes
```bash
# Scale down
kubectl scale deployment insightlearn-api -n insightlearn --replicas=0

# Wait for termination
kubectl wait --for=delete pod -l app=insightlearn-api -n insightlearn --timeout=60s

# Update image
kubectl set image deployment/insightlearn-api -n insightlearn \
  api=insightlearn/api:v$IMAGE_VERSION

# Scale up
kubectl scale deployment insightlearn-api -n insightlearn --replicas=2

# Wait for ready
kubectl wait --for=condition=ready pod -l app=insightlearn-api -n insightlearn --timeout=120s
```
- [ ] Scaled down to 0
- [ ] Pods terminated
- [ ] Image updated
- [ ] Scaled up to 2
- [ ] Pods ready

#### Step 7: Verify Deployment
```bash
# Check pod status
kubectl get pods -n insightlearn -l app=insightlearn-api

# Check logs for CORS
kubectl logs -n insightlearn -l app=insightlearn-api --tail=50 | grep -i cors

# Check image version
kubectl get pods -n insightlearn -l app=insightlearn-api \
  -o jsonpath='{range .items[*]}{.metadata.name}{"\t"}{.spec.containers[0].image}{"\n"}{end}'
```
- [ ] All pods Running (not CrashLoopBackOff)
- [ ] CORS initialization logged
- [ ] Correct image version deployed

---

## Post-Deployment Testing

### Automated Testing

```bash
cd /home/mpasqui/kubernetes/Insightlearn
./test-cors.sh
```

**Tests Included**:
- [ ] Preflight request (OPTIONS) - WASM production origin
- [ ] Preflight request (OPTIONS) - Direct IP origin
- [ ] Actual POST request with CORS
- [ ] Blocked origin test (should fail)
- [ ] Health endpoint with CORS
- [ ] API logs verification

**Expected Result**: All tests PASSED ✅

### Manual Testing

#### Test 1: Preflight Request
```bash
curl -X OPTIONS https://192.168.1.103/api/auth/login \
  -H "Origin: https://new.insightlearn.cloud" \
  -H "Access-Control-Request-Method: POST" \
  -H "Access-Control-Request-Headers: Content-Type,Authorization" \
  -v 2>&1 | grep -i "access-control"
```

**Expected Headers**:
- [ ] `access-control-allow-origin: https://new.insightlearn.cloud`
- [ ] `access-control-allow-credentials: true`
- [ ] `access-control-max-age: 86400`

#### Test 2: Actual API Call
```bash
curl -X POST https://192.168.1.103/api/auth/login \
  -H "Origin: https://new.insightlearn.cloud" \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@insightlearn.cloud","password":"Admin123!"}' \
  -v 2>&1 | grep -i "access-control"
```

**Expected**:
- [ ] CORS headers present
- [ ] 200 OK or 401 Unauthorized (API working)

#### Test 3: Health Endpoint
```bash
curl https://192.168.1.103/api/health \
  -H "Origin: https://new.insightlearn.cloud" \
  -v
```

**Expected**:
- [ ] 200 OK
- [ ] JSON response with health status

#### Test 4: Old Blazor Server Site
```bash
curl https://192.168.1.103/health -v
curl https://192.168.1.103/api/health -v
```

**Expected**:
- [ ] Both return 200 OK
- [ ] Old site still works (no impact)

#### Test 5: Browser Testing
1. Open `https://new.insightlearn.cloud` in browser
2. Open DevTools (F12) → Console
3. Run:
```javascript
fetch('https://192.168.1.103/api/health', {
  method: 'GET',
  credentials: 'include'
})
.then(r => r.json())
.then(d => console.log('✓ CORS OK:', d))
.catch(e => console.error('✗ CORS Error:', e));
```

**Expected**:
- [ ] No CORS errors in console
- [ ] API response logged successfully

---

## Verification Checklist

### API Health
- [ ] API pods Running (not CrashLoopBackOff or Error)
- [ ] API responding to health checks
- [ ] No errors in API logs
- [ ] CORS initialization logged

### CORS Functionality
- [ ] Preflight OPTIONS returns CORS headers
- [ ] Actual requests include CORS headers
- [ ] Origin `https://new.insightlearn.cloud` allowed
- [ ] Origin `https://192.168.1.103` allowed
- [ ] Credentials support working
- [ ] Preflight caching enabled (24h)

### Compatibility
- [ ] Old Blazor Server site working
- [ ] Same-origin API calls working
- [ ] Health endpoints accessible
- [ ] No new errors in logs

### Security
- [ ] Explicit origins only (no wildcards)
- [ ] HTTPS origins in production
- [ ] Credentials with specific origins
- [ ] Methods restricted (GET/POST/PUT/DELETE/PATCH/OPTIONS)
- [ ] Headers restricted (Authorization, Content-Type, Accept)

### Performance
- [ ] Preflight caching configured (24h max age)
- [ ] No significant latency increase
- [ ] Reduced OPTIONS requests after first call

---

## Troubleshooting

### Issue: Pods Not Starting (CrashLoopBackOff)

**Diagnosis**:
```bash
kubectl logs -n insightlearn -l app=insightlearn-api --tail=100
kubectl describe pod -n insightlearn -l app=insightlearn-api
```

**Common Causes**:
- Syntax error in Program.cs
- Missing using statements
- Configuration issue

**Fix**:
1. Check logs for exact error
2. Fix code issue
3. Rebuild and redeploy

### Issue: CORS Headers Missing

**Diagnosis**:
```bash
# Check middleware order
grep -A 10 "app.UseCors" src/InsightLearn.Api/Program.cs

# Check environment
kubectl exec -n insightlearn deployment/insightlearn-api -- \
  env | grep ASPNETCORE_ENVIRONMENT
```

**Common Causes**:
- Middleware order incorrect (CORS after Auth)
- Wrong policy applied (Dev vs Prod)
- Environment not detected correctly

**Fix**:
1. Verify `app.UseCors()` is BEFORE `app.UseAuthentication()`
2. Check environment variable
3. Verify correct policy applied

### Issue: Origin Not Allowed

**Diagnosis**:
```bash
# Check which origins are configured
grep -A 10 "WithOrigins" src/InsightLearn.Api/Program.cs
```

**Fix**:
1. Add missing origin to `WithOrigins()` list
2. Rebuild and redeploy

### Issue: Old Site Broken

**Immediate Action**: ROLLBACK
```bash
kubectl rollout undo deployment/insightlearn-api -n insightlearn
kubectl rollout status deployment/insightlearn-api -n insightlearn
```

**Diagnosis**: Check compatibility
```bash
curl https://192.168.1.103/health -v
curl https://192.168.1.103/api/health -v
```

---

## Rollback Procedure

### Quick Rollback (Kubernetes)

```bash
# Rollback to previous deployment
kubectl rollout undo deployment/insightlearn-api -n insightlearn

# Wait for rollback
kubectl rollout status deployment/insightlearn-api -n insightlearn

# Verify pods
kubectl get pods -n insightlearn -l app=insightlearn-api
```

**Time**: ~2 minutes

### Manual Rollback (Restore Backup)

```bash
# Find backup
ls -lt /home/mpasqui/kubernetes/Insightlearn/src/InsightLearn.Api/Program.cs.backup-*

# Restore
cd /home/mpasqui/kubernetes/Insightlearn/src/InsightLearn.Api
cp Program.cs.backup-YYYYMMDD-HHMMSS Program.cs

# Rebuild and redeploy (follow deployment steps)
```

**Time**: ~10 minutes (full rebuild)

---

## Success Criteria

### Deployment Success ✅

**All checks must pass**:
- [x] Code changes applied to Program.cs
- [x] Documentation created
- [ ] Version bumped
- [ ] Docker image built without errors
- [ ] Image loaded to minikube
- [ ] Deployment updated to new version
- [ ] All pods Running (not CrashLoopBackOff)
- [ ] CORS initialization logged in API logs

### Testing Success ✅

**All tests must pass**:
- [ ] Preflight OPTIONS returns correct CORS headers
- [ ] Actual requests include CORS headers
- [ ] Browser shows no CORS errors
- [ ] Old Blazor Server site still works
- [ ] Health endpoints accessible
- [ ] No new errors in logs

### Production Readiness ✅

**All criteria met**:
- [x] Security: OWASP CORS compliant
- [x] Performance: Preflight caching enabled
- [x] Monitoring: CORS logs present
- [x] Documentation: Complete guides available
- [x] Testing: Automated tests available
- [x] Rollback: Backup and rollback plan ready
- [ ] Deployed: Changes live in Kubernetes
- [ ] Verified: All tests passed

---

## Post-Deployment Actions

### Immediate (First Hour)

```bash
# Monitor API logs
kubectl logs -n insightlearn -l app=insightlearn-api -f | grep -iE "cors|error"

# Watch pod status
kubectl get pods -n insightlearn -l app=insightlearn-api -w
```

- [ ] No errors in logs
- [ ] Pods stable (no restarts)

### Short-Term (First 24 Hours)

```bash
# Check for CORS errors
kubectl logs -n insightlearn -l app=insightlearn-api --tail=5000 | \
  grep -iE "cors.*error|origin.*blocked"

# Monitor preflight caching effectiveness
kubectl logs -n insightlearn -l app=insightlearn-api --tail=5000 | \
  grep -i "OPTIONS" | wc -l
```

- [ ] No CORS errors
- [ ] Preflight caching working (fewer OPTIONS after first request)

### Long-Term (Ongoing)

- [ ] Document any issues encountered
- [ ] Update documentation if needed
- [ ] Monitor performance metrics
- [ ] Plan for mobile app origins (if applicable)

---

## Documentation References

After deployment, refer to these documents:

1. **CORS-QUICK-REFERENCE.md** - Quick commands and troubleshooting
2. **CORS-IMPLEMENTATION.md** - Complete implementation details
3. **CORS-SUMMARY.md** - Executive summary and architecture
4. **test-cors.sh** - Automated testing script
5. **deploy-cors.sh** - Automated deployment script

---

## Final Sign-Off

### Pre-Deployment
- [x] Code changes reviewed and verified
- [x] Documentation complete
- [ ] Prerequisites checked
- [ ] Backup created

### Deployment
- [ ] Automated deployment completed OR
- [ ] Manual deployment steps followed
- [ ] No errors during deployment
- [ ] Pods Running successfully

### Testing
- [ ] Automated tests passed OR
- [ ] Manual tests completed successfully
- [ ] Browser testing verified
- [ ] Old site compatibility verified

### Production Ready
- [ ] All success criteria met
- [ ] Monitoring in place
- [ ] Rollback plan tested
- [ ] Team notified of changes

---

**Deployment Date**: _____________
**Deployed By**: _____________
**API Version**: v________ (after CORS)
**Status**: ⬜ Pending | ⬜ In Progress | ⬜ Complete | ⬜ Rolled Back

---

**Notes**:
_____________________________________________________________________________
_____________________________________________________________________________
_____________________________________________________________________________
