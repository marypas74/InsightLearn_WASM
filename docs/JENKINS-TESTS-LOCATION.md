# 📍 Dove Sono i Test per Jenkins

## 🎯 Panoramica

Tutti i file di test sono presenti e funzionanti nel repository. Ecco la mappa completa:

---

## 📂 Struttura File di Test

### **1. Script di Testing** (`jenkins/scripts/`)

Tutti gli script sono eseguibili (chmod 755) e pronti all'uso:

```
jenkins/scripts/
├── load-test.sh                    ✅ Script principale load testing
├── site-monitor.sh                 ✅ Monitoring continuo sito
├── test-email-notification.sh      ✅ Test notifiche email
├── create-jenkins-job.sh           🔧 Utility creazione job
└── setup-jenkins-job-api.sh        🔧 Setup via API
```

#### **load-test.sh** - Load Testing con 4 Modalità

```bash
# Uso:
./jenkins/scripts/load-test.sh light    # 10 concurrent, 50 req
./jenkins/scripts/load-test.sh medium   # 25 concurrent, 100 req
./jenkins/scripts/load-test.sh heavy    # 50 concurrent, 200 req
./jenkins/scripts/load-test.sh stress   # 100 concurrent, 500 req

# Output:
# - Console: Real-time risultati
# - File: load-test-report-YYYYMMDD_HHMMSS.txt
```

**Features**:
- Concurrent request simulation
- Response time tracking (min/max/avg)
- Success rate calculation
- Requests per second (RPS)
- Detailed report generation

#### **site-monitor.sh** - Monitoring Continuo

```bash
# Uso:
./jenkins/scripts/site-monitor.sh        # Check ogni 60s
./jenkins/scripts/site-monitor.sh 30     # Check ogni 30s

# CTRL+C per fermare
```

**Monitora**:
- Endpoint health (/, /courses, /api/info, etc.)
- Response times (threshold: 1.0s)
- K8s pod status (se kubectl disponibile)
- SSL certificate expiry
- Uptime tracking
- Alert automatici (3 consecutive failures)

---

### **2. Pipeline Jenkins** (`Jenkinsfile` + `jenkins/pipelines/`)

#### **Jenkinsfile** (Root Directory)

**Path**: `/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/Jenkinsfile`

**Pipeline Completo con 9 Stage**:

1. **Preparation** - Setup environment
2. **Health Check** - Test main site + API endpoints
3. **Page Availability Test** - Check per 404 errors su tutte le pagine
4. **Performance Benchmarking** - 10 test di response time
5. **Load Testing** - Simula 50 concurrent users
6. **Asset Validation** - Verifica CSS/JS/images
7. **Security Headers Check** - Valida security headers
8. **Backend API Monitoring** - K8s pod status
9. **Generate Report** - Summary report completo

**Schedule**: Ogni ora (`H * * * *`)

#### **weekly-heavy-load-test.Jenkinsfile**

**Path**: `jenkins/pipelines/weekly-heavy-load-test.Jenkinsfile`

**Pipeline per Load Test Settimanali**:

1. Preparation
2. Pre-Test Health Check
3. Heavy Load Testing (parametrizzato)
4. Performance Analysis
5. Post-Test Health Check
6. Generate Summary Report

**Schedule**: Domenica 2:00 AM (`0 2 * * 0`)

**Parameters**:
- `SITE_URL` - Target URL (default: https://wasm.insightlearn.cloud)
- `LOAD_LEVEL` - heavy/medium/light/stress
- `EMAIL_RECIPIENTS` - Email per notifiche

---

### **3. Job Configuration Files** (`jenkins/jobs/`, `jenkins/config/`)

```
jenkins/
├── config/
│   └── job-config.xml              ✅ Config job principale
├── jobs/
│   └── weekly-heavy-load-test.xml  ✅ Config weekly load test
└── jcasc/
    └── jenkins.yaml                ✅ Jenkins Configuration as Code
```

---

### **4. Documentazione** (`jenkins/` + `docs/`)

```
jenkins/
├── README.md                       ✅ Guida completa Jenkins
└── QUICK-SETUP.md                  ✅ Setup rapido 5 minuti

docs/
├── JENKINS-UI-SETUP-2MIN.md        ✅ Setup UI 2 minuti
└── JENKINS-TESTS-LOCATION.md       ✅ Questo file
```

---

## 🚀 Come Usare i Test

### **Metodo 1: Direttamente da Bash**

```bash
# Vai nella directory repository
cd /home/mpasqui/insightlearn_WASM/InsightLearn_WASM

# Esegui load test
./jenkins/scripts/load-test.sh medium

# Output:
# Testing https://wasm.insightlearn.cloud
# Mode: medium (25 concurrent, 100 total requests)
#
# Results:
# ✅ Successful: 100/100 (100%)
# ❌ Failed: 0/100 (0%)
# ⏱️  Response Times:
#    - Average: 0.543s
#    - Min: 0.102s
#    - Max: 1.234s
# 📊 Requests/sec: 18.4
#
# Report saved: load-test-report-20251108_230045.txt

# Monitoring continuo
./jenkins/scripts/site-monitor.sh
# (Ctrl+C per fermare)
```

### **Metodo 2: Via Jenkins (Automazione)**

**Step 1: Verifica Jenkins è Running**
```bash
kubectl get pods -n jenkins
# NAME                       READY   STATUS
# jenkins-XXXXX-XXXXX        1/1     Running

curl http://localhost:32000/
# Deve restituire pagina Jenkins
```

**Step 2: Crea i Job Jenkins**

I job Jenkins sono configurati per eseguire automaticamente i test usando `Jenkinsfile` dal repository GitHub:

```bash
# I job dovrebbero essere già configurati, ma se mancano:
# Vai su http://localhost:32000

# Job 1: insightlearn-automated-tests
# - Type: Pipeline
# - Schedule: H * * * * (ogni ora)
# - Pipeline from SCM: Git
#   - Repo: https://github.com/marypas74/InsightLearn_WASM.git
#   - Branch: */main
#   - Script Path: Jenkinsfile

# Job 2: insightlearn-weekly-heavy-load-test
# - Type: Pipeline
# - Schedule: 0 2 * * 0 (domenica 2 AM)
# - Pipeline from SCM: Git
#   - Repo: https://github.com/marypas74/InsightLearn_WASM.git
#   - Branch: */main
#   - Script Path: jenkins/pipelines/weekly-heavy-load-test.Jenkinsfile
```

**Step 3: Triggera Build Manuale**

```bash
# Via API
curl -X POST http://localhost:32000/job/insightlearn-automated-tests/build

# Via UI
# http://localhost:32000/job/insightlearn-automated-tests/
# Click "Build Now"
```

**Step 4: Visualizza Risultati**

```bash
# Via API
curl -s http://localhost:32000/job/insightlearn-automated-tests/lastBuild/consoleText

# Via UI
# http://localhost:32000/job/insightlearn-automated-tests/1/console
```

---

## 📊 Test Report Location

### **Load Test Reports**

Tutti i report vengono salvati nella directory root del repository:

```bash
ls -lh load-test-report-*.txt

# Esempio output:
# -rw-r--r--. 1 mpasqui mpasqui 2.1K Nov  8 18:20 load-test-report-20251108_182007.txt
```

**Contenuto Report**:
```
=========================================
Load Test Report - InsightLearn Platform
=========================================
Date: 2025-11-08 18:20:07
Mode: medium (25 concurrent, 100 total requests)
Site: https://wasm.insightlearn.cloud
Endpoint: /

Test Results:
-------------------------------------------
✅ Successful requests: 100/100 (100.0%)
❌ Failed requests: 0/100 (0.0%)

Performance Metrics:
-------------------------------------------
⏱️  Average response time: 0.636s
⚡ Min response time: 0.108s
🐌 Max response time: 2.341s
📊 Requests/sec: 8.33

Status Code Distribution:
-------------------------------------------
200 OK: 100 (100%)

=========================================
```

### **Jenkins Build Artifacts**

I report vengono archiviati anche in Jenkins:

- **URL**: http://localhost:32000/job/insightlearn-automated-tests/{BUILD_NUMBER}/artifact/
- **Files**: `test-report-{TIMESTAMP}.txt`, `load-test-report-{TIMESTAMP}.txt`

---

## 🔍 Troubleshooting

### **"Script not found" Error**

```bash
# Verifica che script esistano
ls -la jenkins/scripts/

# Se mancano permessi esecuzione:
chmod +x jenkins/scripts/*.sh
```

### **"Jenkins jobs non visibili"**

```bash
# Verifica Jenkins è online
curl http://localhost:32000/api/json

# Lista job
curl -s http://localhost:32000/api/json | jq '.jobs[] | .name'

# Se empty [], ricrea i job (vedi sopra)
```

### **"Test falliscono con errore rete"**

```bash
# Verifica site è accessibile
curl -I https://wasm.insightlearn.cloud

# Verifica DNS
nslookup wasm.insightlearn.cloud

# Test diretto API (bypassa CDN)
curl http://localhost:31090/api/info
```

---

## 📁 File di Configurazione Importante

### **Environment Variables**

I test usano queste variabili (configurate in Jenkinsfile):

```bash
SITE_URL=https://wasm.insightlearn.cloud
EMAIL_RECIPIENTS=marcello.pasqui@gmail.com
SLACK_CHANNEL=#insightlearn-alerts  # Optional
```

### **Script Groovy per Creazione Job**

**Path**: `/tmp/create-jenkins-jobs.groovy`

Questo script crea i 2 job Jenkins automaticamente. Se i job si perdono (es. restart Jenkins senza PersistentVolume), esegui:

```bash
kubectl exec -n jenkins $(kubectl get pod -n jenkins -o name | grep jenkins | head -1) -- sh -c '
wget -q -O /tmp/jenkins-cli.jar http://localhost:8080/jnlpJars/jenkins-cli.jar && \
java -jar /tmp/jenkins-cli.jar -s http://localhost:8080/ -webSocket groovy = < /tmp/create-jobs.groovy
'
```

---

## ✅ Checklist Quick Test

```bash
# 1. Verifica tutti i file esistono
cd /home/mpasqui/insightlearn_WASM/InsightLearn_WASM
ls Jenkinsfile jenkins/pipelines/weekly-heavy-load-test.Jenkinsfile jenkins/scripts/*.sh

# 2. Test manuale rapido
./jenkins/scripts/load-test.sh light

# 3. Verifica Jenkins
curl http://localhost:32000/api/json | jq '.jobs[] | .name'

# 4. Triggera build test
curl -X POST http://localhost:32000/job/insightlearn-automated-tests/build

# 5. Aspetta 1 minuto e controlla risultato
sleep 60
curl -s http://localhost:32000/job/insightlearn-automated-tests/lastBuild/api/json | jq '.result'
# Output atteso: "SUCCESS"
```

---

**Tutti i file di test sono presenti e funzionanti!** ✅

Se Jenkins perde i job (restart senza PersistentVolume), i file di test rimangono intatti nel repository e possono essere ri-eseguiti manualmente o i job possono essere ricreati.

**Ultima verifica**: 2025-11-08 23:45 UTC
**Maintainer**: marcello.pasqui@gmail.com
