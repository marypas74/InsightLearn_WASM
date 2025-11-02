# InsightLearn - Monitoring e Grafana Guide

## 📊 Accesso a Grafana

### URL
**https://192.168.1.103/grafana/**

### Credenziali
- **Username**: `admin`
- **Password**: `admin`

⚠️ **Importante**: Al primo accesso, Grafana chiederà di cambiare la password predefinita.

## 🎯 Dashboard Disponibili

### 1. InsightLearn - Infrastructure Overview
Dashboard personalizzata per monitorare l'infrastruttura InsightLearn.

**Metriche Visualizzate:**
- **CPU Usage by Pod**: Utilizzo CPU per ogni pod
- **Memory Usage by Pod**: Utilizzo memoria RAM per ogni pod
- **Running Pods**: Numero di pod attivi (gauge)
- **Services Up**: Numero di servizi online (gauge)
- **Network I/O**: Traffico di rete in ingresso/uscita
- **Pod Restarts**: Monitoraggio riavvii anomali

**Come Accedere:**
1. Login su https://192.168.1.103/grafana/
2. Click su "Dashboards" nel menu laterale
3. Cerca "InsightLearn - Infrastructure Overview"

### 2. Dashboard Kubernetes Predefinite
Il sistema include anche dashboard predefinite per Kubernetes:

- **Kubernetes / Compute Resources / Cluster**: Risorse complessive del cluster
- **Kubernetes / Compute Resources / Namespace (Pods)**: Risorse per namespace
- **Kubernetes / Compute Resources / Pod**: Dettagli specifici pod
- **Kubernetes / Networking / Pod**: Metriche di rete per pod
- **Node Exporter / Nodes**: Metriche del nodo minikube

## 📈 Metriche Chiave da Monitorare

### Salute dell'Applicazione

#### 1. Pod Status
**Query Prometheus:**
```promql
kube_pod_status_phase{namespace="insightlearn"}
```

**Cosa Verificare:**
- Tutti i pod dovrebbero essere in fase "Running"
- Se vedi pod in "Pending" o "CrashLoopBackOff", c'è un problema

#### 2. Pod Restarts
**Query Prometheus:**
```promql
kube_pod_container_status_restarts_total{namespace="insightlearn"}
```

**Cosa Verificare:**
- Restart count dovrebbe rimanere stabile
- Incrementi frequenti indicano problemi di stabilità

#### 3. Container CPU Usage
**Query Prometheus:**
```promql
sum(rate(container_cpu_usage_seconds_total{namespace="insightlearn"}[5m])) by (pod)
```

**Soglie di Attenzione:**
- **< 50%**: OK
- **50-70%**: Monitorare
- **> 70%**: Considerare scaling

#### 4. Container Memory Usage
**Query Prometheus:**
```promql
sum(container_memory_usage_bytes{namespace="insightlearn"}) by (pod)
```

**Soglie di Attenzione:**
- **< 70%** del limit: OK
- **70-85%**: Monitorare
- **> 85%**: Rischio OOMKill

### Performance Database

#### SQL Server Metriche
**Query per connessioni attive:**
```promql
sqlserver_active_connections{namespace="insightlearn"}
```

**Nota**: Richiede SQL Server exporter (opzionale)

### Network Performance

#### Network I/O
**Query Prometheus:**
```promql
# Receive
sum(rate(container_network_receive_bytes_total{namespace="insightlearn"}[5m])) by (pod)

# Transmit
sum(rate(container_network_transmit_bytes_total{namespace="insightlearn"}[5m])) by (pod)
```

## 🔔 Alerting (Configurazione)

### Alert Rules Raccomandati

Per configurare alert in Prometheus, crea file in `/etc/prometheus/rules/`:

```yaml
groups:
- name: insightlearn
  interval: 30s
  rules:
  - alert: PodDown
    expr: kube_pod_status_phase{namespace="insightlearn", phase="Running"} < 7
    for: 5m
    labels:
      severity: critical
    annotations:
      summary: "InsightLearn pod down"
      description: "Meno di 7 pods running nel namespace insightlearn"

  - alert: HighMemoryUsage
    expr: container_memory_usage_bytes{namespace="insightlearn"} / container_spec_memory_limit_bytes{namespace="insightlearn"} > 0.85
    for: 5m
    labels:
      severity: warning
    annotations:
      summary: "High memory usage on {{ $labels.pod }}"
      description: "Memory usage is above 85%"

  - alert: HighCPUUsage
    expr: rate(container_cpu_usage_seconds_total{namespace="insightlearn"}[5m]) > 0.8
    for: 10m
    labels:
      severity: warning
    annotations:
      summary: "High CPU usage on {{ $labels.pod }}"
      description: "CPU usage is above 80%"

  - alert: PodRestarting
    expr: rate(kube_pod_container_status_restarts_total{namespace="insightlearn"}[15m]) > 0
    for: 5m
    labels:
      severity: warning
    annotations:
      summary: "Pod {{ $labels.pod }} is restarting"
      description: "Pod has restarted recently"
```

## 📊 Query Utili Prometheus

### Accesso Diretto a Prometheus
**URL**: https://192.168.1.103/prometheus/ (se configurato in nginx)
**Oppure**: Port-forward locale
```bash
kubectl port-forward -n monitoring svc/kube-prometheus-kube-prome-prometheus 9090:9090
```

### Query Essenziali

#### 1. Lista Tutti i Pods
```promql
kube_pod_info{namespace="insightlearn"}
```

#### 2. Pods Non Running
```promql
kube_pod_status_phase{namespace="insightlearn", phase!="Running"}
```

#### 3. Top 5 Pods per CPU
```promql
topk(5, sum(rate(container_cpu_usage_seconds_total{namespace="insightlearn"}[5m])) by (pod))
```

#### 4. Top 5 Pods per Memoria
```promql
topk(5, sum(container_memory_usage_bytes{namespace="insightlearn"}) by (pod))
```

#### 5. Network Bandwidth Totale
```promql
sum(rate(container_network_transmit_bytes_total{namespace="insightlearn"}[5m]))
```

#### 6. Disk I/O
```promql
sum(rate(container_fs_writes_bytes_total{namespace="insightlearn"}[5m])) by (pod)
```

## 🛠️ Troubleshooting Monitoring

### Problema: Grafana Non Mostra Dati

**Verifica Prometheus:**
```bash
kubectl get pods -n monitoring | grep prometheus
```

**Verifica ServiceMonitors:**
```bash
kubectl get servicemonitor -n insightlearn
```

**Verifica Targets in Prometheus:**
1. Accedi a Prometheus: http://192.168.49.2:30090
2. Vai su Status → Targets
3. Verifica che i target "insightlearn" siano "UP"

### Problema: Dashboard Non Carica

**Verifica Grafana Logs:**
```bash
kubectl logs -n monitoring -l app.kubernetes.io/name=grafana -f
```

**Verifica ConfigMap:**
```bash
kubectl get configmap insightlearn-dashboard -n monitoring -o yaml
```

### Problema: Metriche Mancanti

**Le applicazioni .NET devono esporre metriche su `/metrics`.**

Per abilitare metriche in .NET, aggiungi a `Program.cs`:

```csharp
// Install: dotnet add package prometheus-net.AspNetCore

using Prometheus;

var app = builder.Build();

// Expose metrics endpoint
app.UseMetricServer(); // Espone /metrics
app.UseHttpMetrics(); // Traccia richieste HTTP

app.Run();
```

## 📱 Dashboard Mobile

Grafana è accessibile anche da mobile:
- **URL**: https://192.168.1.103/grafana/
- L'interfaccia è responsive

## 🔐 Sicurezza Grafana

### Cambiare Password Admin
1. Login con `admin/admin`
2. Grafana richiederà di cambiare password
3. Inserisci nuova password sicura

### Creare Utenti Aggiuntivi
1. Menu → Configuration → Users
2. Click "Invite" o "New user"
3. Assegna ruoli:
   - **Admin**: Accesso completo
   - **Editor**: Può modificare dashboard
   - **Viewer**: Solo visualizzazione

### Configurare LDAP/OAuth (Opzionale)
Modifica ConfigMap Grafana per integrare con Active Directory o OAuth providers.

## 📊 Esportare Grafici

### Export PNG/PDF
1. Apri dashboard
2. Click su titolo pannello
3. Share → Direct link rendered image
4. O usa Reporting (richiede Grafana Enterprise)

### Export Dashboard JSON
1. Dashboard → Settings (icona ingranaggio)
2. JSON Model
3. Copy to clipboard o Download

## 🔄 Retention e Performance

### Prometheus Retention
Di default: **15 giorni**

Per modificare:
```bash
kubectl edit statefulset prometheus-kube-prometheus-kube-prome-prometheus -n monitoring
```

Aggiungi arg:
```yaml
- --storage.tsdb.retention.time=30d
```

### Grafana Dashboard Auto-Refresh
1. Apri dashboard
2. Top-right: Click su icona refresh
3. Seleziona intervallo (es. 30s, 1m, 5m)

## 📈 Metriche Avanzate

### Custom Exporters (Opzionali)

#### Redis Exporter
```bash
kubectl apply -f - <<EOF
apiVersion: v1
kind: Service
metadata:
  name: redis-exporter
  namespace: insightlearn
  labels:
    app: redis-exporter
spec:
  ports:
  - port: 9121
    name: metrics
  selector:
    app: redis-exporter
---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: redis-exporter
  namespace: insightlearn
spec:
  replicas: 1
  selector:
    matchLabels:
      app: redis-exporter
  template:
    metadata:
      labels:
        app: redis-exporter
    spec:
      containers:
      - name: redis-exporter
        image: oliver006/redis_exporter:latest
        env:
        - name: REDIS_ADDR
          value: "redis-service:6379"
        ports:
        - containerPort: 9121
