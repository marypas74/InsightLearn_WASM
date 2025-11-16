# InsightLearn Prometheus Metrics - Quick Reference

## Access Metrics

```bash
# Local testing
curl http://localhost:31081/metrics

# Kubernetes (port-forward)
kubectl port-forward -n insightlearn svc/insightlearn-api 8080:80
curl http://localhost:8080/metrics

# Filter custom metrics only
curl -s http://localhost:31081/metrics | grep "^insightlearn_"
```

---

## Metrics Catalog

### Counters (Always Increasing)

```promql
# API Requests
insightlearn_api_requests_total{method="GET",endpoint="/api/info",status="200"}

# Enrollments
insightlearn_enrollments_total

# Payments
insightlearn_payments_total{status="completed",payment_method="stripe"}
insightlearn_payment_revenue_total{status="completed",payment_method="stripe"}

# Chatbot
insightlearn_chatbot_messages_total{model="qwen2:0.5b"}

# Video Uploads
insightlearn_video_uploads_total{status="success"}

# User Registrations
insightlearn_user_registrations_total{user_type="Student"}

# Login Attempts
insightlearn_login_attempts_total{status="success"}
```

### Gauges (Current Values)

```promql
# Active Users
insightlearn_active_users

# Active Enrollments
insightlearn_active_enrollments

# Courses
insightlearn_courses{status="published"}

# Video Storage
insightlearn_video_storage_bytes

# Database Connections
insightlearn_database_connections
```

### Histograms (Latency Tracking)

```promql
# API Request Duration
insightlearn_api_request_duration_seconds_bucket{method="GET",endpoint="/api/info",le="0.1"}
insightlearn_api_request_duration_seconds_sum{method="GET",endpoint="/api/info"}
insightlearn_api_request_duration_seconds_count{method="GET",endpoint="/api/info"}

# Ollama AI Inference
insightlearn_ollama_inference_duration_seconds_bucket{model="qwen2:0.5b",le="2"}

# Database Queries
insightlearn_database_query_duration_seconds_bucket{operation="select",le="0.01"}
```

### Summaries (Statistical Distribution)

```promql
# Video Upload Sizes
insightlearn_video_upload_size_bytes{status="success",quantile="0.5"}  # Median
insightlearn_video_upload_size_bytes{status="success",quantile="0.9"}  # p90
insightlearn_video_upload_size_bytes{status="success",quantile="0.99"} # p99

# Payment Amounts
insightlearn_payment_amount_usd{payment_method="stripe",quantile="0.5"}
```

---

## Common PromQL Queries

### Rate & Growth

```promql
# API Request Rate (per second)
rate(insightlearn_api_requests_total[5m])

# Enrollment Growth (per hour)
increase(insightlearn_enrollments_total[1h])

# Revenue Rate (USD per minute)
rate(insightlearn_payment_revenue_total{status="completed"}[1m])
```

### Percentiles (Latency)

```promql
# API p50 latency
histogram_quantile(0.50, sum(rate(insightlearn_api_request_duration_seconds_bucket[5m])) by (le, endpoint))

# API p95 latency
histogram_quantile(0.95, sum(rate(insightlearn_api_request_duration_seconds_bucket[5m])) by (le, endpoint))

# API p99 latency
histogram_quantile(0.99, sum(rate(insightlearn_api_request_duration_seconds_bucket[5m])) by (le, endpoint))

# Ollama inference p95
histogram_quantile(0.95, sum(rate(insightlearn_ollama_inference_duration_seconds_bucket[5m])) by (le, model))
```

### Success Rates

```promql
# Login Success Rate
sum(rate(insightlearn_login_attempts_total{status="success"}[5m])) /
sum(rate(insightlearn_login_attempts_total[5m]))

# Payment Success Rate
sum(rate(insightlearn_payments_total{status="completed"}[5m])) /
sum(rate(insightlearn_payments_total[5m]))

# API Error Rate (4xx + 5xx)
sum(rate(insightlearn_api_requests_total{status=~"4..|5.."}[5m])) /
sum(rate(insightlearn_api_requests_total[5m]))
```

### Aggregations

```promql
# Total Revenue (all time)
sum(insightlearn_payment_revenue_total{status="completed"})

# Revenue by Payment Method
sum(insightlearn_payment_revenue_total{status="completed"}) by (payment_method)

# Top 10 Slowest Endpoints
topk(10, avg(rate(insightlearn_api_request_duration_seconds_sum[5m])) by (endpoint) /
         avg(rate(insightlearn_api_request_duration_seconds_count[5m])) by (endpoint))

# Requests by Status Code
sum(rate(insightlearn_api_requests_total[5m])) by (status)
```

---

## Alerts (Examples)

```yaml
groups:
  - name: insightlearn_alerts
    rules:
      # API Performance
      - alert: HighAPILatency
        expr: histogram_quantile(0.95, rate(insightlearn_api_request_duration_seconds_bucket[5m])) > 1
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "API p95 latency > 1 second"

      - alert: HighErrorRate
        expr: sum(rate(insightlearn_api_requests_total{status=~"5.."}[5m])) / sum(rate(insightlearn_api_requests_total[5m])) > 0.01
        for: 5m
        labels:
          severity: critical
        annotations:
          summary: "API error rate > 1%"

      # Business Metrics
      - alert: LowEnrollmentRate
        expr: rate(insightlearn_enrollments_total[1h]) < 1
        for: 2h
        labels:
          severity: warning
        annotations:
          summary: "Enrollment rate < 1/hour for 2 hours"

      - alert: HighPaymentFailureRate
        expr: sum(rate(insightlearn_payments_total{status="failed"}[5m])) / sum(rate(insightlearn_payments_total[5m])) > 0.05
        for: 10m
        labels:
          severity: critical
        annotations:
          summary: "Payment failure rate > 5%"

      # Infrastructure
      - alert: HighVideoStorage
        expr: insightlearn_video_storage_bytes > 100 * 1024 * 1024 * 1024  # 100 GB
        for: 1h
        labels:
          severity: warning
        annotations:
          summary: "Video storage > 100 GB"

      - alert: OllamaSlowInference
        expr: histogram_quantile(0.95, rate(insightlearn_ollama_inference_duration_seconds_bucket[5m])) > 10
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "Ollama inference p95 > 10 seconds"
```

---

## Grafana Dashboard Panels

### Panel 1: API Request Rate
```json
{
  "targets": [
    {
      "expr": "sum(rate(insightlearn_api_requests_total[5m])) by (endpoint)",
      "legendFormat": "{{endpoint}}"
    }
  ],
  "type": "graph",
  "title": "API Request Rate by Endpoint"
}
```

### Panel 2: API Latency (p50, p95, p99)
```json
{
  "targets": [
    {
      "expr": "histogram_quantile(0.50, sum(rate(insightlearn_api_request_duration_seconds_bucket[5m])) by (le))",
      "legendFormat": "p50"
    },
    {
      "expr": "histogram_quantile(0.95, sum(rate(insightlearn_api_request_duration_seconds_bucket[5m])) by (le))",
      "legendFormat": "p95"
    },
    {
      "expr": "histogram_quantile(0.99, sum(rate(insightlearn_api_request_duration_seconds_bucket[5m])) by (le))",
      "legendFormat": "p99"
    }
  ],
  "type": "graph",
  "title": "API Response Time"
}
```

### Panel 3: Enrollments & Revenue
```json
{
  "targets": [
    {
      "expr": "sum(insightlearn_enrollments_total)",
      "legendFormat": "Total Enrollments"
    },
    {
      "expr": "sum(insightlearn_payment_revenue_total{status='completed'})",
      "legendFormat": "Total Revenue (USD)"
    }
  ],
  "type": "stat",
  "title": "Business Metrics"
}
```

### Panel 4: Active Users
```json
{
  "targets": [
    {
      "expr": "insightlearn_active_users",
      "legendFormat": "Active Users"
    }
  ],
  "type": "gauge",
  "title": "Active Users (Last 15 min)"
}
```

---

## Testing Script

```bash
# Run automated test
./test-prometheus-metrics.sh

# Manual test - Generate traffic
for i in {1..100}; do
  curl -s http://localhost:31081/api/info > /dev/null
done

# Verify metrics updated
curl -s http://localhost:31081/metrics | grep "insightlearn_api_requests_total"
```

---

## Troubleshooting

### Metrics not showing up?

```bash
# 1. Check endpoint
curl http://localhost:31081/metrics | head

# 2. Verify service registered
kubectl logs -n insightlearn deployment/insightlearn-api | grep MetricsService

# 3. Generate sample traffic
curl http://localhost:31081/api/info

# 4. Check again
curl -s http://localhost:31081/metrics | grep insightlearn_api_requests_total
```

### Prometheus not scraping?

```bash
# Check targets in Prometheus
http://prometheus:9090/targets

# Verify connectivity
kubectl exec -it prometheus-pod -- wget -O- http://insightlearn-api/metrics
```

---

## Files Reference

- **MetricsService**: `/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/src/InsightLearn.Application/Services/MetricsService.cs`
- **Test Script**: `/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/test-prometheus-metrics.sh`
- **Full Report**: `/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/PHASE-4.2-PROMETHEUS-METRICS-REPORT.md`

---

**Last Updated**: 2025-11-16
**Phase**: 4.2 - Custom Application Metrics
