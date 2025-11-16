# Grafana Alert Rules Configuration - Phase 4.3
**Proactive Monitoring of Critical System Components**

**Date**: 2025-11-16
**Status**: IMPLEMENTATION READY
**Priority**: MEDIUM (proactive monitoring)
**Estimated Time**: 2 hours deployment + testing

---

## Table of Contents

1. [Overview](#overview)
2. [Alert Rules Detail](#alert-rules-detail)
3. [PromQL Queries Explained](#promql-queries-explained)
4. [Threshold Justification](#threshold-justification)
5. [Deployment Instructions](#deployment-instructions)
6. [Testing & Verification](#testing--verification)
7. [Monitoring & Maintenance](#monitoring--maintenance)
8. [Troubleshooting](#troubleshooting)

---

## Overview

### Purpose

The InsightLearn platform requires proactive monitoring to:
- Detect service failures before users are impacted
- Identify performance degradation early
- Enable rapid incident response
- Meet SLA commitments (15-30 minute response times)

### Architecture

```
┌─────────────────┐
│   Prometheus    │ (Metrics Collection)
│   Port 9090     │
└────────┬────────┘
         │ Scrape targets (15s interval)
         │
    ┌────v──────┐
    │ Targets   │
    │ - API     │
    │ - DB      │
    │ - Redis   │
    └────┬──────┘
         │ Metrics: up, http_*, container_*
         │
    ┌────v─────────────────┐
    │  Alert Rules (1m)    │
    │  5 Rules Evaluating  │
    └────┬─────────────────┘
         │ Alert Status
         │ PENDING → FIRING → RESOLVED
         │
    ┌────v──────────────┐
    │ Notification      │
    │ Channels          │
    │ - Webhook         │
    │ - Email           │
    │ - Slack (optional)│
    │ - PagerDuty (opt) │
    └───────────────────┘
```

### Components Deployed

1. **ConfigMap: grafana-alert-rules**
   - 5 alert rules with YAML configuration
   - Auto-loaded by Grafana via label `grafana_dashboard: "1"`

2. **ConfigMap: grafana-alert-notifications**
   - Notification channel definitions
   - Webhook URLs, email settings
   - Slack/PagerDuty integration (optional)

3. **ConfigMap: grafana-alert-policy**
   - AlertManager routing rules
   - Escalation and grouping logic
   - Alert inhibition rules

---

## Alert Rules Detail

### Alert 1: API Health Check Failed

**Severity**: CRITICAL
**SLA**: 15-minute response time
**Impact**: Platform completely inaccessible

| Property | Value |
|----------|-------|
| **Rule UID** | api-health-check-failed |
| **PromQL Query** | `up{job="insightlearn-api"} == 0` |
| **Trigger Condition** | API pod DOWN for 2+ minutes |
| **For Duration** | 2 minutes |
| **Escalation** | Page on-call engineer immediately |

**Annotations**:
```yaml
description: "InsightLearn API pod is down or unreachable for more than 2 minutes.
             Platform is inaccessible. Immediate action required to restore service."
summary: "API Health Check Failed - Service Unavailable"
runbook_url: "https://insightlearn.atlassian.net/wiki/spaces/OPS/pages/runbooks/api-health-check"
```

**Runbook Steps**:
1. Verify API pod status: `kubectl get pods -n insightlearn | grep api`
2. Check pod events: `kubectl describe pod <api-pod> -n insightlearn`
3. Check logs: `kubectl logs <api-pod> -n insightlearn`
4. Restart pod: `kubectl delete pod <api-pod> -n insightlearn` (auto-restarts)
5. If restart fails, check PVC/disk space: `kubectl get pvc -n insightlearn`

---

### Alert 2: High API Error Rate (5xx Errors)

**Severity**: WARNING
**SLA**: 30-minute response time
**Impact**: Application errors affecting user experience

| Property | Value |
|----------|-------|
| **Rule UID** | api-high-error-rate |
| **PromQL Query** | `(sum(rate(http_requests_total{job="insightlearn-api",status=~"5.."}[5m])) / sum(rate(http_requests_total{job="insightlearn-api"}[5m]))) * 100` |
| **Trigger Condition** | > 5% of requests return 5xx errors |
| **For Duration** | 5 minutes |
| **Evaluation Window** | 5 minutes (covers ~10-20 requests/sec) |

**Why 5% Threshold?**

Baseline error rate analysis:
- Production healthy: 0-1% errors (brief failures, transient issues)
- Acceptable degradation: 1-3% errors (isolated issues, few users impacted)
- **Warning threshold: 5% errors** (systemic issue, 50+ errors/min at 1000 req/sec)
- Critical threshold: 10%+ errors (severe outage, widespread impact)

**Example Calculation**:
```
If API receives 1000 req/sec average:
- 0.5% errors = 5 errors/sec (acceptable)
- 5% errors = 50 errors/sec (warning)
- 10% errors = 100 errors/sec (critical)
```

**Annotations**:
```yaml
description: "API error rate is exceeding 5% threshold. This indicates a
             systemic issue with request processing. Check application logs,
             database connectivity, and external service dependencies."
summary: "High API Error Rate - {{ $value | humanizePercentage }} 5xx errors"
runbook_url: "https://insightlearn.atlassian.net/wiki/spaces/OPS/pages/runbooks/api-error-rate"
dashboard: "http://grafana:3000/d/insightlearn-main/api-metrics"
```

**Runbook Steps**:
1. Identify error spike: Check Grafana dashboard for timeline
2. Check API logs: `kubectl logs <api-pod> -n insightlearn --tail=100`
3. Verify dependencies:
   - Database: `kubectl get pods -n insightlearn | grep sqlserver`
   - Redis: `kubectl get pods -n insightlearn | grep redis`
   - MongoDB: `kubectl get pods -n insightlearn | grep mongodb`
4. Check error patterns: Are all endpoints failing or specific ones?
5. Review recent deployments: Did error rate increase after deployment?
6. If database issue: See **Alert 3 runbook**

---

### Alert 3: Database Connection Failed

**Severity**: CRITICAL
**SLA**: 15-minute response time
**Impact**: Data unavailable, application cannot function

| Property | Value |
|----------|-------|
| **Rule UID** | database-connection-failed |
| **PromQL Query** | `up{job="sqlserver"} == 0` |
| **Trigger Condition** | SQL Server unhealthy for 1+ minute |
| **For Duration** | 1 minute |
| **Check Type** | Direct metric scraping |

**Why 1 Minute Duration?**

- Database failure is critical: every second of downtime = lost transactions
- 1 minute allows for brief transient issues (query timeout recovery)
- Shorter than API health check (2 min) due to cascading impact

**Annotations**:
```yaml
description: "SQL Server database is unreachable or unhealthy. API cannot
             process requests that require database access. Check database pod
             status, volume mounts, and connection strings."
summary: "Database Connection Failed - SQL Server Unavailable"
runbook_url: "https://insightlearn.atlassian.net/wiki/spaces/OPS/pages/runbooks/database-failure"
kubectl: "kubectl get pods -n insightlearn | grep sqlserver"
```

**Runbook Steps**:
1. Verify pod status: `kubectl get pods -n insightlearn | grep sqlserver`
2. If pod is CrashLoopBackOff:
   - Check events: `kubectl describe pod <sqlserver-pod> -n insightlearn`
   - Check logs: `kubectl logs <sqlserver-pod> -n insightlearn --tail=50`
3. If pod is stuck initializing:
   - Check PVC: `kubectl get pvc -n insightlearn | grep sqlserver`
   - Check disk space: `kubectl exec <sqlserver-pod> -it -- df -h`
4. Check connection string in API config:
   - `kubectl get configmap -n insightlearn`
   - Verify `ConnectionStrings__DefaultConnection` is correct
5. If password issue: Check Secret:
   - `kubectl get secret insightlearn-secrets -n insightlearn -o yaml`
6. Last resort: Restart pod:
   - `kubectl delete pod <sqlserver-pod> -n insightlearn`
   - Wait for auto-restart (usually 2-3 minutes)

---

### Alert 4: High Memory Usage (API Pod)

**Severity**: WARNING
**SLA**: 30-minute response time
**Impact**: Performance degradation, potential OOMKill

| Property | Value |
|----------|-------|
| **Rule UID** | high-memory-usage |
| **PromQL Query** | `(container_memory_usage_bytes{pod=~"insightlearn-api.*",namespace="insightlearn"} / container_spec_memory_limit_bytes{pod=~"insightlearn-api.*",namespace="insightlearn"}) * 100` |
| **Trigger Condition** | Memory > 85% of limit for 5+ minutes |
| **For Duration** | 5 minutes |
| **Memory Limit** | 512Mi (from deployment spec) |

**Why 85% Threshold?**

Memory threshold strategy:
- 100% = OOMKilled (pod crashes)
- 95% = Very risky (GC pressure high)
- **85% = Warning threshold** (15% headroom, ~440 MB used)
- 70% = Healthy baseline (with normal spike headroom)

**Example Calculation** (512 MB limit):
```
85% of 512 MB = 435.2 MB = WARNING
If sustained > 435 MB for 5 minutes → Memory leak likely
```

**Annotations**:
```yaml
description: "API pod memory usage is {{ $value | humanize }}% of limit.
             High memory usage can cause performance degradation. Check for
             memory leaks, excessive caching, or increase pod resource limits."
summary: "High Memory Usage - API Pod at {{ $value | humanizePercentage }} of limit"
runbook_url: "https://insightlearn.atlassian.net/wiki/spaces/OPS/pages/runbooks/memory-usage"
kubectl: "kubectl top pods -n insightlearn --sort-by=memory"
```

**Runbook Steps**:
1. Check current memory usage: `kubectl top pods -n insightlearn --sort-by=memory`
2. Identify memory spike timeline: Check Grafana dashboard
3. Check for memory leaks:
   - Is memory usage constantly increasing?
   - Or is it spike + gradual recovery (GC happening)?
4. Check application logs for heap size warnings:
   - `kubectl logs <api-pod> -n insightlearn | grep -i "heap\|memory\|gc"`
5. Options:
   - **Increase pod memory limit**: Edit deployment, increase `memory: 512Mi` to `1Gi`
   - **Restart pod**: Clears memory leaks (temporary fix)
   - **Reduce cache TTL**: Limits cache growth
   - **Enable memory profiling**: `dotnet trace collect`

**Preventive Measures**:
- Set memory request = 256Mi, limit = 512Mi
- Enable Vertical Pod Autoscaler (VPA)
- Monitor memory trends weekly
- Implement cache size limits

---

### Alert 5: Slow API Response Time

**Severity**: WARNING
**SLA**: 30-minute response time
**Impact**: Poor user experience, potential timeout issues

| Property | Value |
|----------|-------|
| **Rule UID** | slow-api-response |
| **PromQL Query** | `histogram_quantile(0.95, sum(rate(http_request_duration_seconds_bucket{job="insightlearn-api"}[5m])) by (le, endpoint))` |
| **Trigger Condition** | p95 latency > 2 seconds for 5+ minutes |
| **For Duration** | 5 minutes |
| **Percentile** | 95th (excludes top 5% outliers) |

**Why 2 Seconds Threshold?**

Response time baseline analysis:
- Healthy API: p50 = 50-100ms, p95 = 200-500ms
- **Warning threshold: 2 seconds** (4x slowdown, user-noticeable)
- Critical threshold: 5+ seconds (10x slowdown, timeout risk)

**Example Response Time Distribution**:
```
Healthy (baseline):
  p50:  100 ms (median)
  p95:  400 ms (95% fast)
  p99:  800 ms (99% fast)

Degraded (alert triggers):
  p50: 1000 ms
  p95: 2500 ms (> 2s threshold)
  p99: 5000 ms

Severe (critical):
  p50: 5000 ms
  p95: 10000+ ms (timeout approaching)
```

**Annotations**:
```yaml
description: "API 95th percentile response time is {{ $value | humanizeDuration }}.
             Users are experiencing slow responses. Check for database performance
             issues, slow external API calls, or high CPU/memory usage."
summary: "Slow API Response Time - p95: {{ $value | humanizeDuration }}"
runbook_url: "https://insightlearn.atlassian.net/wiki/spaces/OPS/pages/runbooks/response-time"
dashboard: "http://grafana:3000/d/insightlearn-main/api-performance"
```

**Runbook Steps**:
1. Check response time by endpoint: Grafana dashboard → API Performance
2. Identify slow endpoints:
   - Is it all endpoints or specific ones? (e.g., /api/courses is slow)
   - Database-heavy endpoint? (courses, enrollments, payments)
3. Check database performance:
   - `kubectl exec <sqlserver-pod> -- sqlcmd -Q "sp_who2"` (active queries)
   - `kubectl logs <api-pod> -n insightlearn | grep "Duration\|timeout"`
4. Check resource utilization:
   - CPU: `kubectl top nodes` (node overloaded?)
   - Memory: `kubectl top pods -n insightlearn` (memory pressure?)
   - Disk I/O: `kubectl node logs` (excessive disk activity?)
5. Check external services:
   - Redis latency: `redis-cli --latency`
   - MongoDB latency: `mongosh --eval "db.adminCommand('serverStatus')"`
6. Investigation sequence:
   - If database slow: See Alert 3 or optimize queries
   - If CPU high: Scale horizontally (add replicas)
   - If memory high: See Alert 4
   - If specific endpoint slow: Check application logs for that endpoint

---

## PromQL Queries Explained

### Query 1: API Health Check
```promql
up{job="insightlearn-api"} == 0
```

**Explanation**:
- `up{...}` = Prometheus scrape success metric (1 = success, 0 = failure)
- `job="insightlearn-api"` = Filter to API service only
- `== 0` = Trigger when API is unreachable

**Behavior**:
- Every 15 seconds, Prometheus tries to scrape `/metrics` from API
- If scrape succeeds: `up` = 1 (stored in TSDB)
- If scrape fails (pod down, port closed): `up` = 0

---

### Query 2: Error Rate Calculation
```promql
(sum(rate(http_requests_total{job="insightlearn-api",status=~"5.."}[5m])) /
 sum(rate(http_requests_total{job="insightlearn-api"}[5m]))) * 100
```

**Step-by-step breakdown**:

1. **`http_requests_total{job="insightlearn-api",status=~"5.."}`**
   - Cumulative counter of requests with 5xx status codes
   - `status=~"5.."` = Regex matching 500-599 status codes (5xx errors)

2. **`rate(...[5m])`**
   - Calculates per-second rate over 5-minute window
   - Formula: `(value_at_5m - value_now) / 300_seconds`
   - Returns: errors per second

3. **`sum(...)`**
   - Aggregates across all instances/paths
   - Total errors/sec = sum of all error rates

4. **`/ sum(rate(http_requests_total{job="insightlearn-api"}[5m]))`**
   - Divides errors by total requests
   - Returns: error ratio (0.05 = 5%)

5. **`* 100`**
   - Converts to percentage (5 instead of 0.05)

**Example Calculation** (1000 req/sec average):
```
At T=0: http_requests_total{status="500"} = 1000
At T=5m: http_requests_total{status="500"} = 1015
Rate = (1015 - 1000) / 300 = 0.05 errors/sec
Total request rate = 1000 req/sec
Error % = (0.05 / 1000) * 100 = 0.005%
```

---

### Query 3: Database Health Check
```promql
up{job="sqlserver"} == 0
```

**Explanation**:
- Same as API health check, but for SQL Server job
- Prometheus attempts to connect to SQL Server port
- Success = 1, Failure = 0

**Note**: Requires SQL Server exporter or direct Prometheus scrape job configured

---

### Query 4: Memory Percentage
```promql
(container_memory_usage_bytes{pod=~"insightlearn-api.*",namespace="insightlearn"} /
 container_spec_memory_limit_bytes{pod=~"insightlearn-api.*",namespace="insightlearn"}) * 100
```

**Explanation**:
- `container_memory_usage_bytes` = Current memory used by container (from cgroup)
- `container_spec_memory_limit_bytes` = Memory limit set in pod spec
- `pod=~"insightlearn-api.*"` = Match pods starting with "insightlearn-api"
- Ratio * 100 = Percentage of limit used

**Example**:
```
current_memory = 400 MB
limit = 512 MB
percentage = (400 / 512) * 100 = 78.1%
```

---

### Query 5: Response Time Percentile
```promql
histogram_quantile(0.95, sum(rate(http_request_duration_seconds_bucket{job="insightlearn-api"}[5m])) by (le, endpoint))
```

**Explanation**:

1. **`http_request_duration_seconds_bucket`**
   - Histogram metric with buckets: [0.001, 0.01, 0.1, 0.5, 1, 5, 10]
   - Each bucket = count of requests <= bucket size

2. **`rate(...[5m])`**
   - Per-second rate of requests in each bucket over 5 minutes

3. **`sum(...) by (le, endpoint)`**
   - Group by histogram bucket (le) and endpoint
   - Summing rates across all instances

4. **`histogram_quantile(0.95, ...)`**
   - Calculates 95th percentile latency
   - Formula: Linear interpolation between buckets

**Example** (1000 requests in 5 minutes):
```
bucket le=0.1s:  900 requests (90%)
bucket le=0.5s:  950 requests (95%)
bucket le=1.0s:  990 requests (99%)

p95 = 0.5s (between 0.1s and 0.5s buckets)
p99 = 1.0s (between 0.5s and 1.0s buckets)
```

---

## Threshold Justification

### Alert 1: API Health - 2 Minutes Duration

| Component | Baseline | Alert Threshold | Rationale |
|-----------|----------|-----------------|-----------|
| Pod startup time | 10-20s | 2 minutes | Allow for slow restart/recovery |
| k8s scheduler delay | 5-10s | 2 minutes | Allow pod to reach Running state |
| Initial probe | 1-5s | 2 minutes | Health check has startup period |
| **SLA impact** | N/A | 2 min before escalation | Critical - 15 min SLA |

**Justification**:
- Less than 2 minutes: Too sensitive (normal restarts trigger false alarms)
- More than 5 minutes: Too late (users already impacted)
- 2 minutes balances: Detection speed vs. false positive rate

---

### Alert 2: Error Rate - 5% Threshold

| Request Volume | Error % | Requests/sec | Impact |
|---|---|---|---|
| 100 req/sec | 1% | 1 error/sec | Normal (transient issues) |
| 100 req/sec | 5% | 5 errors/sec | **WARNING** ← Threshold |
| 100 req/sec | 10% | 10 errors/sec | Critical |
| 1000 req/sec | 1% | 10 errors/sec | Normal |
| 1000 req/sec | 5% | 50 errors/sec | **WARNING** ← Threshold |
| 1000 req/sec | 10% | 100 errors/sec | Critical |

**Justification**:
- 0-1%: Typical production rate (brief timeouts, retries succeed)
- 1-3%: Degraded but acceptable (few users notice)
- **5%: Clear systemic issue** (many users affected)
- 10%+: Critical outage (most users affected)

**Decision Logic**:
- If we set too low (1%): 50+ false alarms/day
- If we set too high (10%): Miss real issues until critical
- 5% provides: ~0-2 alarms/day, detects issues early

---

### Alert 3: Database - 1 Minute Duration

| Service | Healthy Timeout | Alert Duration | Rationale |
|---------|-----------------|-----------------|-----------|
| API | > 120s downtime critical | 2 min | API can failover, retry |
| Database | > 60s downtime critical | 1 min | No failover, cascading impact |
| Cache | > 30s downtime acceptable | N/A | Graceful degradation |

**Justification**:
- Database failure blocks ALL operations (not just some endpoints)
- Shorter duration than API check (1 min vs 2 min)
- After 1 minute: ~60-100 failed transactions (unacceptable)
- Transient connection issues resolve within 30 seconds

---

### Alert 4: Memory - 85% Threshold

| Memory % | Available | Risk Level | Action |
|----------|-----------|-----------|--------|
| 50% | 256 MB | Healthy | None |
| 70% | 154 MB | Normal | Monitor |
| 80% | 102 MB | Elevated | Review logs |
| **85%** | **77 MB** | **WARNING** | Investigate |
| 90% | 51 MB | High | Urgent action |
| 95% | 26 MB | Critical | Restart pod |
| 100% | 0 MB | OOMKilled | Pod crashes |

**Justification**:
- 85% threshold leaves 77 MB for spike headroom
- GC can add 10-15% memory temporarily
- At 85%: ~15% buffer before OOMKilled (safe)
- At 95%: <5% buffer (very risky)

**Memory Pressure Timeline**:
```
85% triggered → 5 min monitoring → escalation
  ↓
During 5 min wait period:
  - Is memory still growing? (leak suspected)
  - Or spike + recovery? (normal GC)
  - Check logs for error messages
  ↓
Alert fires → Runbook execution:
  - Restart pod (temporary)
  - Increase limit (permanent)
  - Profile heap (diagnose)
```

---

### Alert 5: Response Time - 2 Seconds Threshold

| Scenario | p50 | p95 | p99 | Alert Status |
|----------|-----|-----|-----|--------------|
| Healthy baseline | 100ms | 400ms | 800ms | OK |
| Slight degradation | 200ms | 700ms | 1500ms | OK |
| Moderate slowdown | 500ms | 1500ms | 2500ms | OK |
| **High slowdown** | **1000ms** | **2500ms** | **5000ms** | **FIRING** ← p95 > 2s |
| Severe outage | 3000ms | 8000ms | 10000ms | FIRING |

**Justification**:
- Users start noticing: > 1 second latency
- Users feel frustrated: > 2 seconds latency
- Timeout approaching: > 5 seconds (default HTTP timeout)
- **2 seconds = 4x slowdown** = clear performance issue
- Avoids false positives from: network blips, brief spikes

**5-Minute Window Rationale**:
- Detection window = 5 minutes
- Allows brief slow requests (1-2 per minute at 100 req/sec)
- But catches sustained slowness (ongoing issue)
- Example: "p95 exceeded for 5 consecutive minutes" = real issue

---

## Deployment Instructions

### Step 1: Create ConfigMap

```bash
# Apply the alert rules ConfigMap
kubectl apply -f k8s/22-grafana-alerts.yaml

# Verify creation
kubectl get configmap -n insightlearn | grep alert

# Expected output:
# grafana-alert-rules               1      XXs
# grafana-alert-notifications       1      XXs
# grafana-alert-policy              1      XXs
```

### Step 2: Restart Grafana to Load Rules

```bash
# Restart Grafana pod to trigger provisioning
kubectl rollout restart deployment/grafana -n insightlearn

# Wait for pod to be ready
kubectl rollout status deployment/grafana -n insightlearn

# Check logs to verify rules loaded
kubectl logs -n insightlearn -l app=grafana | grep -i alert
```

### Step 3: Verify Rules in Grafana UI

```bash
# Setup port-forward (if not already running)
kubectl port-forward -n insightlearn svc/grafana 3000:3000 &

# Access Grafana
# URL: http://localhost:3000
# Login: admin/admin
# Navigate: Alerting → Alert rules
# Should see: 5 rules in "InsightLearn Critical Alerts" group
```

### Step 4: Configure Notification Channels (Optional)

For production deployments, configure notification channels:

```bash
# In Grafana UI:
# 1. Alerting → Notification channels
# 2. Create new channel:
#    a) Webhook: http://api:80/api/webhooks/alerts/critical
#    b) Email: platform-team@insightlearn.com
#    c) Slack: Add webhook URL
#    d) PagerDuty: Add service key

# Test notification:
# Click "Send test notification" button
```

### Step 5: Implement Webhook Receiver (Optional)

In the API, create webhook endpoint to receive alerts:

```csharp
// Suggested endpoint in Program.cs
app.MapPost("/api/webhooks/alerts/{severity}", async (string severity, HttpContext context) =>
{
    var payload = await context.Request.ReadAsStringAsync();
    _logger.LogWarning($"Alert [{severity}]: {payload}");

    // TODO: Implement alert handling logic:
    // - Send to PagerDuty
    // - Create Jira incident
    // - Notify Slack channel
    // - Update status page

    return Results.Accepted();
});
```

---

## Testing & Verification

### Manual Test 1: API Health Alert

**Objective**: Verify API health alert triggers when pod is down

**Steps**:
```bash
# 1. Get API pod name
API_POD=$(kubectl get pods -n insightlearn -l app=insightlearn-api -o jsonpath='{.items[0].metadata.name}')
echo "API Pod: $API_POD"

# 2. Simulate failure (delete pod)
kubectl delete pod $API_POD -n insightlearn
echo "Pod deleted - waiting for 2 minutes..."

# 3. Check Prometheus: Should show up{job="insightlearn-api"} = 0
# Grafana → Explore → Prometheus
# Query: up{job="insightlearn-api"}
# Should show: 0 (DOWN) for 2 minutes

# 4. Wait 2 minutes (alert_for duration)
sleep 120

# 5. Check Grafana: Alert rules page
# Should see: api-health-check-failed = FIRING (red icon)

# 6. Pod should auto-restart
kubectl get pods -n insightlearn | grep api

# 7. After pod comes back
# Alert should transition: FIRING → RESOLVED

# Expected timeline:
# T+0min: Pod deleted
# T+0min: Prometheus detects up=0
# T+2min: Alert fires
# T+2-3min: Pod restarts
# T+3min: Prometheus detects up=1
# T+3min: Alert resolves
```

### Manual Test 2: Error Rate Alert

**Objective**: Verify error rate alert triggers when 5% threshold exceeded

**Steps**:
```bash
# 1. Generate 5xx errors
# Use invalid endpoint to trigger 404/500 errors
for i in {1..100}; do
    curl -s -X POST http://localhost:7001/api/invalid -H "Content-Type: application/json" &
done
wait
echo "Generated 100 error requests"

# 2. Repeat to exceed 5% error rate
# If baseline is 1000 req/sec, need 50+ errors/sec
# For testing: Generate many requests to increase denominator

# 3. Monitor in Grafana
# Prometheus query: rate(http_requests_total{job="insightlearn-api",status=~"5.."}[5m]) / rate(http_requests_total{job="insightlearn-api"}[5m])
# Should climb toward 5%+

# 4. Wait 5 minutes (alert_for duration)
# Alert should fire if error % > 5%

# Expected behavior:
# T+0min: Start generating errors
# T+5min: If error % > 5%, alert FIRING
# T+X-5min: After 5 min without errors, alert RESOLVES
```

### Manual Test 3: Database Alert

**Objective**: Verify database alert triggers when DB is unreachable

**Steps**:
```bash
# 1. Get database pod
DB_POD=$(kubectl get pods -n insightlearn -l app=sqlserver -o jsonpath='{.items[0].metadata.name}')

# 2. Simulate database failure
kubectl delete pod $DB_POD -n insightlearn

# 3. Wait 1 minute (shorter alert_for = faster detection)
sleep 60

# 4. Check Grafana: Alert rules
# Should see: database-connection-failed = FIRING

# 5. Wait for pod restart
# Alert resolves when pod comes back and up{job="sqlserver"} = 1

# Expected timeline:
# T+0min: DB pod deleted
# T+1min: Alert fires
# T+2-3min: DB pod restarts (statefulsets auto-restart)
# T+3min: Alert resolves
```

### Manual Test 4: Memory Alert

**Objective**: Verify memory alert triggers when usage > 85%

**Steps**:
```bash
# 1. Check current memory usage
kubectl top pods -n insightlearn | grep api

# 2. Generate load to increase memory
# Option A: Run load test
for i in {1..1000}; do
    curl -s http://localhost:7001/api/courses &
done
wait

# 3. Monitor memory in Grafana
# Query: (container_memory_usage_bytes{pod=~"insightlearn-api.*"} / container_spec_memory_limit_bytes{pod=~"insightlearn-api.*"}) * 100
# Watch value climb

# 4. If memory reaches 85% for 5 minutes
# Alert: high-memory-usage = FIRING

# 5. Option: Increase pod limit to resolve
# kubectl patch deployment insightlearn-api -n insightlearn --type='json' -p='[{"op":"replace","path":"/spec/template/spec/containers/0/resources/limits/memory","value":"1Gi"}]'

# Alert resolves when memory < 85% or pod restarts
```

### Manual Test 5: Response Time Alert

**Objective**: Verify response time alert triggers when p95 > 2s

**Steps**:
```bash
# 1. Generate concurrent requests to slow down API
# Use load testing tool (k6, Apache Bench, etc.)
ab -n 1000 -c 50 http://localhost:7001/api/courses
# or
for i in {1..100}; do
    curl -s http://localhost:7001/api/courses &
done
wait

# 2. Monitor p95 latency in Grafana
# Query: histogram_quantile(0.95, sum(rate(http_request_duration_seconds_bucket{job="insightlearn-api"}[5m])) by (le))
# Watch p95 climb

# 3. If sustained > 2 seconds for 5 minutes
# Alert: slow-api-response = FIRING

# 4. Resolve by:
# - End load test (latency drops)
# - Optimize slow endpoints (database queries)
# - Scale horizontally (more replicas)

# Expected:
# Immediate load → p95 spikes to 3-5 seconds → Alert fires (p95 > 2s for 5 min)
```

### Automated Testing

Run the comprehensive test script:

```bash
# Make script executable
chmod +x test-grafana-alerts.sh

# Run all tests
./test-grafana-alerts.sh

# Output: Test report with 10 phases
# - ConfigMap verification
# - Prometheus connectivity
# - Metrics availability
# - Alert rules structure
# - Thresholds verification
# - Grafana access
# - Manual testing instructions
# - Performance impact analysis
# - Troubleshooting guide
# - Summary report

# Output location: /tmp/grafana-alerts-test-<timestamp>.log
```

---

## Monitoring & Maintenance

### Weekly Maintenance

**Monday 9 AM (Weekly Alert Review)**:

1. **Alert Summary Report**:
   ```bash
   # Get alert statistics for past week
   kubectl logs -n insightlearn -l app=grafana | grep -i alert | tail -100
   ```

2. **Tuning Thresholds**:
   - High false positive rate? → Increase thresholds
   - Missing real issues? → Decrease thresholds
   - Adjust 1 parameter at a time, test for 1 week

3. **Update Documentation**:
   - Record any threshold changes and rationale
   - Update runbooks if procedures changed

### Monthly Maintenance

**First Friday of Month (Comprehensive Review)**:

1. **Alert Effectiveness Report**:
   - Total alerts fired: Target = 0-2 per week
   - If > 10/week: High false positive rate, need tuning
   - If = 0/month: May be too lenient

2. **Response Time Analysis**:
   - Get baseline p50, p95, p99 for each endpoint
   - If p95 approaching 1.5s: Consider lowering threshold to 1s
   - If always < 500ms: Consider raising threshold to 3s

3. **Incident Correlation**:
   - Did alerts predict user outages?
   - Did we get notified before users reported issues?
   - If not: Lower thresholds/durations

### Quarterly Maintenance

**Every 3 Months (Capacity Planning)**:

1. **Trend Analysis**:
   - Is error rate increasing over time?
   - Is response time degrading?
   - Is memory usage trending up?

2. **Capacity Forecasting**:
   - If error rate increasing 5%/month: Optimize or scale
   - If memory increasing 10 MB/week: Likely memory leak
   - If response time increasing 50ms/month: Database query optimization needed

3. **Alert Rule Review**:
   - Remove unused rules
   - Add new rules for emerging patterns
   - Update thresholds based on new baselines

---

## Troubleshooting

### Issue 1: Alerts Not Appearing in Grafana

**Symptoms**: ConfigMap exists but alerts not visible in Grafana UI

**Solutions**:

```bash
# 1. Verify ConfigMap
kubectl get configmap grafana-alert-rules -n insightlearn

# 2. Check Grafana logs for errors
kubectl logs -n insightlearn -l app=grafana | grep -i "alert\|error\|provisioning"

# 3. Verify provisioning configuration
kubectl get configmap grafana-alert-provisioning -n insightlearn

# 4. Restart Grafana pod
kubectl rollout restart deployment/grafana -n insightlearn
kubectl rollout status deployment/grafana -n insightlearn

# 5. Verify provisioning directory exists in pod
kubectl exec -n insightlearn -it pod/grafana-xxx -- ls -la /etc/grafana/provisioning/alerting/

# 6. If still not appearing: Manually import in Grafana UI
# Grafana → Alerting → Alert rules → Import rule
# Paste JSON from ConfigMap
```

### Issue 2: "Metric Not Found" Error

**Symptoms**: Alert evaluation fails with "no timeseries found" error

**Solutions**:

```bash
# 1. Verify Prometheus scrape targets
kubectl port-forward -n insightlearn svc/prometheus 9091:9090 &
# Navigate to: http://localhost:9091/targets

# 2. Check if jobs are Up
# Expected: insightlearn-api, sqlserver, mongodb, etc. = UP

# 3. Verify metrics are being scraped
# Prometheus → Graph
# Query: up{job="insightlearn-api"}
# Should return results with timestamp

# 4. If metric missing: Check job configuration
kubectl get configmap prometheus -n insightlearn -o yaml | grep -A5 "job_name"

# 5. If scrape failing: Check connectivity
kubectl exec -n insightlearn pod/prometheus-xxx -- curl http://api:80/metrics

# 6. Fix: Add missing exporter or fix scrape URL
# Edit prometheus.yml and restart
```

### Issue 3: Alerts Keep Firing and Resolving (Flapping)

**Symptoms**: Alert status changes every minute between FIRING and OK

**Solutions**:

```bash
# 1. Increase "for" duration to reduce sensitivity
# In ConfigMap, increase duration:
# - API Health: 2m → 3m
# - Error Rate: 5m → 10m
# - Database: 1m → 2m

# 2. Adjust thresholds to be less strict
# - Error Rate: 5% → 10%
# - Memory: 85% → 90%
# - Response Time: 2s → 3s

# 3. Investigate root cause
# Is system genuinely flapping (cascading failures)?
# Or is alert threshold set wrongly?

# 4. Apply changes
kubectl apply -f k8s/22-grafana-alerts.yaml
kubectl rollout restart deployment/grafana -n insightlearn

# 5. Monitor for 1 week to verify flapping stopped
```

### Issue 4: False Positive Rate Too High

**Symptoms**: Too many alerts (10+ per week) that don't indicate real problems

**Solutions**:

```bash
# 1. Review fired alerts in Grafana
# Alerting → Alert instances
# Filter by severity=WARNING (often too sensitive)

# 2. Correlate with user impact
# Did alert firing correspond to user complaints?
# If no user impact: Threshold too low

# 3. Increase thresholds based on distribution
# Check baseline over past month:
kubectl exec -n insightlearn pod/prometheus-xxx -- \
  curl 'http://localhost:9090/api/v1/query_range?query=...'

# Example: If error rate never exceeds 2%
# Increase from 5% to 8%

# 4. Increase "for" duration
# Allow more time before alerting (reduces noise)

# 5. Implement alert deduplication
# Group similar alerts, suppress secondary ones
```

### Issue 5: Missing Alerts (Never Fires Even During Outages)

**Symptoms**: Real incidents occur but alerts don't fire

**Solutions**:

```bash
# 1. Verify alert rule is enabled
# Grafana → Alert rules
# Check if toggle is ON

# 2. Check thresholds are realistic
# Are thresholds higher than ever seen in production?
# Example: If error rate peaks at 3%, threshold at 5% never fires

# 3. Lower thresholds based on baseline
# Query Prometheus for distribution:
# P50, P95, P99 of error rate over past month

# 4. Reduce "for" duration for faster detection
# API Health: 2m → 1m (faster paging)
# Error Rate: 5m → 3m (earlier warning)

# 5. Verify Prometheus is collecting data
# Grafana → Explore → Prometheus
# Query: http_requests_total{job="insightlearn-api"}
# Should show data in last 5 minutes

# 6. Test with manual failure
# See "Manual Testing" section above
```

---

## Summary

This Grafana Alert Rules configuration provides comprehensive monitoring of the InsightLearn platform with:

- **5 alert rules** covering critical infrastructure components
- **Production-ready thresholds** based on realistic baselines
- **Clear runbooks** for incident response
- **Flexible notification channels** for alerting
- **Easy testing** via provided scripts

**Next Steps**:
1. Deploy ConfigMap: `kubectl apply -f k8s/22-grafana-alerts.yaml`
2. Verify installation: `./test-grafana-alerts.sh`
3. Configure notifications in Grafana UI
4. Test each alert manually
5. Monitor false positive rate for 2 weeks
6. Adjust thresholds if needed based on real data
7. Integrate with incident management system (Jira, PagerDuty, etc.)

**Maintenance**: Weekly reviews, monthly comprehensive analysis, quarterly capacity planning

---

**Document Version**: 1.0
**Last Updated**: 2025-11-16
**Next Review**: 2025-12-16
