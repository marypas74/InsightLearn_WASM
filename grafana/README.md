# Grafana Dashboards - InsightLearn Testing

## 📊 **Dashboard Creato**

### InsightLearn - Automated Testing Metrics
**File**: [dashboards/insightlearn-testing-dashboard.json](dashboards/insightlearn-testing-dashboard.json)

Dashboard completo per monitoring test automatizzati e performance del sito.

---

## 🚀 **Import Dashboard in Grafana**

### Access Grafana:
```
URL: http://localhost:3000
Default credentials: admin / admin
```

### Metodo 1: Import via UI (Raccomandato)

1. **Login** a Grafana: http://localhost:3000
2. Click **"+"** (sidebar sinistra) → **"Import"**
3. Click **"Upload JSON file"**
4. Seleziona: `grafana/dashboards/insightlearn-testing-dashboard.json`
5. **Select Prometheus datasource**: Prometheus
6. Click **"Import"**

### Metodo 2: Import via API

```bash
# Set Grafana credentials
GRAFANA_URL="http://localhost:3000"
GRAFANA_USER="admin"
GRAFANA_PASS="admin"

# Import dashboard
curl -X POST "$GRAFANA_URL/api/dashboards/db" \
  -u "$GRAFANA_USER:$GRAFANA_PASS" \
  -H "Content-Type: application/json" \
  -d @grafana/dashboards/insightlearn-testing-dashboard.json
```

### Metodo 3: Docker Volume Mount (Automatic)

Add to `docker-compose.yml`:

```yaml
grafana:
  volumes:
    - ./grafana/dashboards:/var/lib/grafana/dashboards
    - ./grafana/provisioning:/etc/grafana/provisioning
```

---

## 📈 **Panels Included**

### 1. Overall System Status
- **Type**: Stat
- **Metric**: Aggregated health score (0-100%)
- **Colors**: Green (>99%), Yellow (80-99%), Red (<80%)

### 2. Frontend Response Time
- **Type**: Time Series Graph
- **Metrics**:
  - Homepage response time (ms)
  - Courses page response time (ms)
- **Threshold**: 200ms warning line

### 3. API Response Status
- **Type**: Time Series Graph
- **Metrics**: HTTP status codes distribution (2xx, 4xx, 5xx)
- **Unit**: Requests/second

### 4. Success Rate
- **Type**: Stat with gauge
- **Metric**: Percentage of 2xx responses
- **Thresholds**: >99% green, 95-99% yellow, <95% red

### 5. Uptime
- **Type**: Stat
- **Metric**: System uptime (last 24h)
- **Target**: 99.9% uptime

### 6. Load Test Results
- **Type**: Time Series Graph
- **Metrics**:
  - Requests/second during load tests
  - P95 latency (ms)

### 7. Test Results Distribution
- **Type**: Pie Chart
- **Metrics**: Jenkins test pass/fail ratio

### 8. Last Jenkins Build
- **Type**: Stat
- **Metric**: Last build status (SUCCESS/UNSTABLE/FAILURE)
- **Colors**: Green/Yellow/Red

### 9. Page Availability Status
- **Type**: Table
- **Metrics**: HTTP status for each page
- **Includes**: /, /login, /register, /courses, /dashboard, etc.

### 10. Asset Load Times
- **Type**: Bar Gauge
- **Metrics**: Load time for CSS, JS, images
- **Threshold**: <500ms green, <1000ms yellow, >1000ms red

### 11. Pod Health Status
- **Type**: Stats (vertical)
- **Metrics**: Kubernetes pod status (API, Ollama, MongoDB, Redis)

---

## 🔧 **Configurazione Prometheus (Prerequisiti)**

### 1. Aggiungi Targets a Prometheus

Edit `prometheus/prometheus.yml`:

```yaml
scrape_configs:
  # InsightLearn API metrics
  - job_name: 'insightlearn-api'
    static_configs:
      - targets: ['api-service.insightlearn.svc.cluster.local:80']
    metrics_path: /metrics

  # InsightLearn WASM frontend metrics
  - job_name: 'insightlearn-wasm'
    static_configs:
      - targets: ['wasm-service.insightlearn.svc.cluster.local:80']
    metrics_path: /metrics

  # Jenkins metrics (requires Prometheus plugin)
  - job_name: 'insightlearn-automated-tests'
    static_configs:
      - targets: ['localhost:32000']
    metrics_path: /prometheus

  # Kubernetes metrics
  - job_name: 'kubernetes-pods'
    kubernetes_sd_configs:
      - role: pod
        namespaces:
          names:
            - insightlearn
```

### 2. Reload Prometheus

```bash
# If running in Docker
docker-compose restart prometheus

# If running in Kubernetes
kubectl rollout restart deployment prometheus -n insightlearn
```

---

## 📊 **Metrics Esposte (Required)**

### Backend API (`/metrics` endpoint):

Add to `Program.cs`:
```csharp
using Prometheus;

// In Program.cs
app.UseMetricServer();  // Exposes /metrics
app.UseHttpMetrics();   // HTTP request metrics
```

### Custom Metrics Example:

```csharp
// Counter for test results
var testsPassed = Metrics.CreateCounter(
    "jenkins_job_success_total",
    "Total successful test runs"
);

var testsFailed = Metrics.CreateCounter(
    "jenkins_job_failure_total",
    "Total failed test runs"
);

// Histogram for response times
var responseTime = Metrics.CreateHistogram(
    "http_request_duration_seconds",
    "HTTP request duration in seconds",
    new HistogramConfiguration
    {
        Buckets = Histogram.ExponentialBuckets(0.001, 2, 10),
        LabelNames = new[] { "path", "method", "status" }
    }
);
```

---

## 🎯 **Dashboard Features**

### Auto-Refresh:
- **Default**: 30 seconds
- **Options**: 5s, 10s, 30s, 1m, 5m, 15m, 30m, 1h

### Time Range:
- **Default**: Last 6 hours
- **Picker**: Custom range selection

### Annotations:
- 🔵 **Deployments**: Blue markers for new deployments
- 🟢 **Test Runs**: Green markers for Jenkins builds

### Variables:
- **$namespace**: Kubernetes namespace filter
- **$pod**: Pod name filter
- **$endpoint**: API endpoint filter

---

## 🔍 **Alerting (Optional)**

### Configure Alerts in Grafana:

1. **Navigate** to dashboard
2. **Edit panel** → **Alert** tab
3. **Create alert rule**:

**Example**: High Response Time Alert
```
Condition: avg() of query(A, 5m) IS ABOVE 500

Message: ⚠️ Response time >500ms for 5 minutes
Send to: Email / Slack / PagerDuty
```

**Example**: Test Failure Alert
```
Condition: last() of query(jenkins_job_failure_total) IS ABOVE 0

Message: ❌ Jenkins test failed!
Send to: Email / Slack
```

---

## 📝 **Dashboard Maintenance**

### Export Updated Dashboard:

```bash
# Get dashboard JSON
curl -u admin:admin http://localhost:3000/api/dashboards/uid/insightlearn-testing \
  | jq '.dashboard' > grafana/dashboards/insightlearn-testing-dashboard.json
```

### Version Control:
- ✅ Dashboard JSON è in Git
- ✅ Auto-sync con provisioning folder
- ✅ Backup automatico

---

## 🆘 **Troubleshooting**

### "No data" in panels:

**Causa**: Prometheus non sta ricevendo metrics

**Fix**:
1. Verifica `/metrics` endpoint: `curl http://localhost:7001/metrics`
2. Check Prometheus targets: http://localhost:9091/targets
3. Verifica job names in prometheus.yml

### Panel shows error:

**Causa**: Query Prometheus non valida

**Fix**: Verifica espressione PromQL nel panel editor

### Dashboard non si importa:

**Causa**: JSON malformato o datasource mancante

**Fix**:
1. Valida JSON: `jq . grafana/dashboards/insightlearn-testing-dashboard.json`
2. Verifica Prometheus datasource configurato

---

## 📚 **Next Steps**

1. ✅ Import dashboard in Grafana
2. [ ] Configure Prometheus targets
3. [ ] Add /metrics endpoint to API
4. [ ] Enable Jenkins Prometheus plugin
5. [ ] Setup alerting rules
6. [ ] Configure notification channels (Email/Slack)

---

**Grafana URL**: http://localhost:3000
**Prometheus URL**: http://localhost:9091
**Dashboard UID**: `insightlearn-testing`

**Last Updated**: 2025-11-08
