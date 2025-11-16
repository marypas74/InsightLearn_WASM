# Grafana Alert Rules - Quick Reference Card

**Phase 4.3 Monitoring Configuration**
**Date**: 2025-11-16
**Status**: Ready for deployment

---

## Deployment Commands

```bash
# 1. Apply ConfigMap
kubectl apply -f k8s/22-grafana-alerts.yaml

# 2. Restart Grafana
kubectl rollout restart deployment/grafana -n insightlearn

# 3. Verify in UI
# URL: http://localhost:3000
# Navigate: Alerting → Alert rules
# Expected: 5 rules in "InsightLearn Critical Alerts"
```

---

## Alert Rules Summary

| # | Alert Name | Severity | Trigger | Duration | Action |
|---|---|---|---|---|---|
| 1 | API Health Check Failed | CRITICAL | API pod down | 2 min | Page on-call (15 min SLA) |
| 2 | High API Error Rate | WARNING | 5xx errors > 5% | 5 min | Review & investigate (30 min SLA) |
| 3 | Database Connection Failed | CRITICAL | SQL Server down | 1 min | Page on-call (15 min SLA) |
| 4 | High Memory Usage | WARNING | Memory > 85% | 5 min | Review logs (30 min SLA) |
| 5 | Slow API Response Time | WARNING | p95 > 2 seconds | 5 min | Check performance (30 min SLA) |

---

## Testing Each Alert

### Test 1: API Health (2 min)
```bash
API_POD=$(kubectl get pods -n insightlearn -l app=insightlearn-api -o jsonpath='{.items[0].metadata.name}')
kubectl delete pod $API_POD -n insightlearn
# Wait 2 minutes → Alert fires
# Pod restarts → Alert resolves
```

### Test 2: Error Rate (5 min)
```bash
for i in {1..100}; do
    curl -s -X POST http://localhost:7001/api/invalid &
done
wait
# Wait 5 minutes with elevated error rate → Alert fires
```

### Test 3: Database (1 min)
```bash
kubectl delete pod -n insightlearn $(kubectl get pods -n insightlearn -l app=sqlserver -o jsonpath='{.items[0].metadata.name}')
# Wait 1 minute → Alert fires
# Pod restarts → Alert resolves
```

### Test 4: Memory (5 min)
```bash
# Generate load
for i in {1..1000}; do
    curl -s http://localhost:7001/api/courses &
done
wait
# Monitor: kubectl top pods -n insightlearn
# If memory > 85% for 5 min → Alert fires
```

### Test 5: Response Time (5 min)
```bash
# Generate concurrent load
ab -n 1000 -c 50 http://localhost:7001/api/courses
# If p95 latency > 2s for 5 min → Alert fires
```

---

## Threshold Justification Summary

| Alert | Threshold | Why This Value? |
|---|---|---|
| API Health | 2 min down | Allows pod restart time, prevents false positives |
| Error Rate | 5% errors | Baseline 0-1%, 5% = clear systemic issue |
| Database | 1 min down | Shorter than API (1 cascading impact), faster detection |
| Memory | 85% of limit | Leaves 15% for GC spikes, before OOMKilled at 100% |
| Response Time | p95 > 2s | Baseline p95 400-500ms, 2s = 4x slowdown (user-noticeable) |

---

## Runbook Quick Links

| Alert | Check | Command |
|---|---|---|
| API Health | Pod status | `kubectl get pods -n insightlearn \| grep api` |
| API Health | Logs | `kubectl logs <pod> -n insightlearn \| tail -100` |
| Error Rate | Error timeline | Grafana → API Performance dashboard |
| Error Rate | Dependency health | `kubectl get pods -n insightlearn \| grep -E "sql\|redis\|mongo"` |
| Database | Pod status | `kubectl get pods -n insightlearn \| grep sqlserver` |
| Database | Disk space | `kubectl exec <pod> -it -- df -h` |
| Memory | Current usage | `kubectl top pods -n insightlearn` |
| Memory | Increase limit | `kubectl patch deployment ... --type json ...` |
| Response Time | By endpoint | Grafana → API Performance → breakdown by path |
| Response Time | Database queries | `kubectl logs <api-pod> -n insightlearn \| grep Duration` |

---

## Port-Forwarding Quick Setup

```bash
# Grafana (3000)
kubectl port-forward -n insightlearn svc/grafana 3000:3000 &

# Prometheus (9091)
kubectl port-forward -n insightlearn svc/prometheus 9091:9090 &

# Kill all port-forwards
pkill -f "kubectl port-forward"
```

---

## Manual Testing Script

```bash
# Run comprehensive test
chmod +x test-grafana-alerts.sh
./test-grafana-alerts.sh

# Output: /tmp/grafana-alerts-test-<timestamp>.log
# Shows: Config verification, metric availability, alert structure, testing instructions
```

---

## Notification Channels Configuration

**Current Status**: Console logging (development)

**Production Setup** (requires manual configuration):

1. **Webhook** (recommended for API integration):
   - Create endpoint: POST `/api/webhooks/alerts/{severity}`
   - Receives JSON payload with alert details
   - Useful for custom alerting logic

2. **Email** (for WARNING alerts):
   - SMTP server configured
   - Recipients: platform-team@insightlearn.com
   - Digest: All alerts grouped, sent hourly

3. **Slack** (for critical incidents):
   - Webhook URL: Insert your Slack webhook
   - Channel: #insightlearn-critical (CRITICAL alerts)
   - Channel: #insightlearn-alerts (WARNING alerts)

4. **PagerDuty** (for on-call escalation):
   - Service key: Insert PagerDuty service key
   - Severity mapping: CRITICAL → page, WARNING → incident
   - Auto-resolve when alert resolves

**Configuration Steps**:
```
Grafana UI:
1. Alerting → Notification channels
2. New channel
3. Type: Webhook/Email/Slack/PagerDuty
4. Configure details
5. Test notification
6. Save
7. Assign to alert rule
```

---

## Performance Impact

- **CPU**: +1% (evaluation of 5 rules every minute)
- **Memory**: <10 MB (rule metadata + query cache)
- **Network**: <1 MB/min (Prometheus scraping)
- **Latency**: <200ms evaluation time per rule cycle

**Safe to scale to**: 50+ alert rules before considering optimization

---

## Maintenance Checklist

### Weekly (Every Monday 9 AM)
- [ ] Review alert summary in Grafana
- [ ] Check false positive rate (target: 0-2 per week)
- [ ] Update runbooks if procedures changed

### Monthly (First Friday)
- [ ] Comprehensive effectiveness report
- [ ] Analyze response time baselines
- [ ] Correlate alerts with user outages
- [ ] Adjust thresholds if needed

### Quarterly (Every 3 months)
- [ ] Trend analysis (error rate, response time, memory)
- [ ] Capacity forecasting
- [ ] Review and update alert rules

---

## Common Issues & Quick Fixes

| Issue | Cause | Fix |
|---|---|---|
| Alerts not showing | ConfigMap not loaded | `kubectl rollout restart deployment/grafana -n insightlearn` |
| "Metric not found" | Job not scraped | Verify Prometheus targets: http://localhost:9091/targets |
| Alerts flapping | Threshold too close to baseline | Increase threshold by 20-50% |
| Too many false positives | Threshold too low | Increase "for" duration or threshold value |
| No alerts firing | Threshold too high | Lower threshold to 1.5x baseline |
| Notifications not working | Channel not configured | Setup in Grafana UI + test notification |

---

## Files Reference

| File | Purpose |
|---|---|
| `k8s/22-grafana-alerts.yaml` | Kubernetes ConfigMaps with alert rules |
| `test-grafana-alerts.sh` | Comprehensive testing & verification script |
| `docs/GRAFANA-ALERTS-CONFIGURATION.md` | Detailed configuration documentation |
| `ALERT-RULES-QUICK-REFERENCE.md` | This file (quick reference) |

---

## Key Metrics Used

| Metric | Source | Update Interval |
|---|---|---|
| `up{job="..."}` | Prometheus scrape success | 15 seconds |
| `http_requests_total{status="..."}` | API metrics | Every request |
| `http_request_duration_seconds_bucket` | API metrics histogram | Every request |
| `container_memory_usage_bytes` | cgroup metrics | 15 seconds |
| `container_spec_memory_limit_bytes` | cgroup metrics | Static |

**Note**: Metrics depend on `/metrics` endpoint exposure in API or sidecar exporters

---

## Important Notes

1. **Prometheus Retention**: Currently undefined (uses default ~15 days)
   - Consider: `--storage.tsdb.retention.time=2w` for 2-week history

2. **Alert State**: Stored in Grafana database (not persistent across pod restarts)
   - For production: Use AlertManager for persistence

3. **PromQL Limitations**: No alert for "metric missing"
   - Implement: Separate metric availability check

4. **Alert Deduplication**: Recommend implementing inhibit rules
   - Example: If DB down, suppress "API error rate" alert

5. **Notification Retry**: Webhook failures are not retried
   - Implement: Message queue (RabbitMQ, Kafka) for reliability

---

## Next Steps

1. **Deploy**: `kubectl apply -f k8s/22-grafana-alerts.yaml`
2. **Test**: `./test-grafana-alerts.sh`
3. **Configure**: Setup notification channels in Grafana UI
4. **Monitor**: Watch for false positives over 2 weeks
5. **Tune**: Adjust thresholds based on baseline data
6. **Document**: Create team runbooks for each alert
7. **Automate**: Implement webhook receiver in API
8. **Integrate**: Connect to incident management (Jira, PagerDuty)

---

**Questions?** See `docs/GRAFANA-ALERTS-CONFIGURATION.md` for detailed explanations
