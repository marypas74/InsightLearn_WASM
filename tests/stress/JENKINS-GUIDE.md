# Jenkins Stress Testing Guide

## Accesso Jenkins

**URL**: http://192.168.49.2:32000

**Note**: L'autenticazione è disabilitata per ambiente di sviluppo.

## Job Disponibile

### InsightLearn-Stress-Test

**Descrizione**: Esegue test di stress k6 come Kubernetes Job

**Parametri**:
- `TEST_TYPE`: Tipo di test da eseguire
  - **smoke** (30 sec) - Verifica base che il sistema funzioni
  - **load** (9 min) - Test con carico normale (10 utenti)
  - **stress** (16 min) - Test con carico elevato (fino a 100 utenti)
  - **spike** (4.5 min) - Test con picco improvviso (fino a 200 utenti)
  - **soak** (3+ ore) - Test di durata estesa (20 utenti per ore)

- `API_URL`: URL dell'API InsightLearn (default: http://192.168.49.2:31081)
- `WEB_URL`: URL del Web InsightLearn (default: http://192.168.49.2:31080)

## Come Eseguire un Test

### Metodo 1: Via Jenkins UI (CONSIGLIATO)

1. Apri http://192.168.49.2:32000
2. Clicca su "InsightLearn-Stress-Test"
3. Clicca "Compila con parametri" (Build with Parameters)
4. Seleziona il tipo di test:
   - Per iniziare: **smoke** (veloce, 30 secondi)
   - Per test realistici: **load** (9 minuti)
   - Per stress estremo: **stress** (16 minuti)
5. Lascia gli URL di default (a meno che tu voglia testare endpoint diversi)
6. Clicca "Compila" (Build)
7. Clicca sul numero del build in "Build History"
8. Clicca "Output console" per vedere i log in tempo reale

### Metodo 2: Via Script Locale

```bash
# Smoke test (30 secondi)
/home/mpasqui/kubernetes/Insightlearn/tests/stress/run-test.sh smoke

# Load test (9 minuti)
/home/mpasqui/kubernetes/Insightlearn/tests/stress/run-test.sh load

# Stress test (16 minuti)
/home/mpasqui/kubernetes/Insightlearn/tests/stress/run-test.sh stress
```

### Metodo 3: Via Kubernetes Job Diretto

```bash
# Crea e esegui il test come K8s Job
/home/mpasqui/kubernetes/Insightlearn/tests/stress/run-k8s-job.sh smoke
```

## Interpretazione Risultati

### Output del Test

Il test k6 mostra:

```
✅ THRESHOLDS
    checks                ✓ 'rate>0.99' rate=100.00%
    http_req_duration     ✓ 'p(95)<500' p(95)=36.72ms
    http_req_failed       ✓ 'rate<0.01' rate=0.00%

✅ TOTAL RESULTS
    checks_succeeded...: 100.00% (42 out of 42)
    http_req_duration..: avg=14.58ms p(95)=36.72ms
    http_req_failed....: 0.00%
```

### Criteri di Successo

#### Smoke Test
- ✅ Tutti i check devono passare (rate > 99%)
- ✅ p(95) response time < 500ms
- ✅ Error rate < 1%
- ✅ NO requisito minimo di throughput

#### Load Test
- ✅ Tutti i check devono passare (rate > 99%)
- ✅ p(95) response time < 500ms
- ✅ Error rate < 1%
- ✅ Almeno 10 req/s

#### Stress Test
- ✅ Tutti i check devono passare (rate > 99%)
- ✅ p(95) response time < 500ms
- ✅ Error rate < 1%
- ✅ Almeno 50 req/s

#### Spike Test
- ✅ Tutti i check devono passare (rate > 99%)
- ✅ p(95) response time < 500ms
- ✅ Error rate < 1%
- ✅ Almeno 100 req/s durante il picco

## Visualizzazione Metriche

### Grafana Dashboard

**URL**: https://192.168.1.103:54403/dashboards

**Dashboard**: "InsightLearn - k6 Stress Testing"

**Metriche mostrate**:
- Response Time (p95, p99)
- Throughput (req/s)
- Success Rate
- Error Rate
- HTTP Status Codes
- Pod Availability

### Kubernetes Monitoring

```bash
# Verifica stato pods durante il test
kubectl get pods -n insightlearn -w

# Verifica risorse usate
kubectl top pods -n insightlearn

# Verifica i job k6
kubectl get jobs -n insightlearn -l app=k6-stress-test

# Vedi log di un test specifico
kubectl logs -n insightlearn -l app=k6-stress-test --tail=100
```

## Troubleshooting

### Test Fallisce con "Error: pod not found"

Il pod k6 non è stato creato. Verifica:
```bash
kubectl get jobs -n insightlearn
kubectl describe job <job-name> -n insightlearn
```

### Test Fallisce con "thresholds crossed"

Uno o più threshold non sono stati soddisfatti. Controlla l'output:
- Se `checks failed` > 1%: il sistema ha errori
- Se `http_req_duration p(95)` > 500ms: il sistema è lento
- Se `http_req_failed` > 1%: molte richieste falliscono

### Jenkins non riesce a creare Job

Verifica permessi RBAC:
```bash
kubectl exec -n jenkins deployment/jenkins -- /var/jenkins_home/bin/kubectl auth can-i create jobs -n insightlearn
```

Dovrebbe rispondere "yes".

### Immagine Docker non trovata

Ricostruisci e carica l'immagine:
```bash
cd /home/mpasqui/kubernetes/Insightlearn/tests/stress
docker build -t insightlearn/k6-tests:latest .
minikube image load insightlearn/k6-tests:latest
```

## Pulizia

### Elimina Job Completati

```bash
# Elimina tutti i job k6 completati
kubectl delete jobs -n insightlearn -l app=k6-stress-test --field-selector status.successful=1

# Elimina tutti i job k6 (anche falliti)
kubectl delete jobs -n insightlearn -l app=k6-stress-test
```

### Reset Jenkins

```bash
# Riavvia Jenkins
kubectl rollout restart deployment/jenkins -n jenkins

# Verifica stato
kubectl get pods -n jenkins
```

## Note Tecniche

### Architettura

1. **Jenkins** esegue uno script bash
2. Lo script crea un **Kubernetes Job** manifest
3. Il Job viene applicato al cluster
4. Kubernetes crea un **Pod** con l'immagine `insightlearn/k6-tests:latest`
5. Il pod esegue k6 con il test specificato
6. Jenkins segue i log in tempo reale
7. Al completamento, verifica il risultato

### Persistenza kubectl

kubectl è installato in `/var/jenkins_home/bin/kubectl` (persistente tra riavvii del pod Jenkins)

### Timeout Job

I Job k6 hanno un TTL di 2 ore dopo il completamento, poi vengono eliminati automaticamente.

### Risorse Allocate

Ogni pod k6 ha:
- **Request**: 256Mi RAM, 100m CPU
- **Limit**: 1Gi RAM, 1000m CPU

## Link Utili

- **Jenkins**: http://192.168.49.2:32000
- **Grafana**: https://192.168.1.103:54403/dashboards
- **API InsightLearn**: http://192.168.49.2:31081
- **Web InsightLearn**: http://192.168.49.2:31080
- **k6 Documentation**: https://k6.io/docs/
