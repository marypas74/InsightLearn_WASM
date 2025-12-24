# Jenkins Quick Setup - InsightLearn Automated Testing

## ✅ **Stato Attuale**

### Completato:
- ✅ Jenkins attivo su **http://localhost:32000**
- ✅ Jenkinsfile creato e pronto
- ✅ Script di load testing funzionanti
- ✅ Script di monitoring funzionanti
- ✅ Email notifications configurate
- ✅ Test manuali eseguiti con successo (70/70 requests OK)

### Da Completare (5 minuti):
1. Creare job Jenkins tramite UI
2. Triggare primo build
3. Verificare output console

---

## 🚀 **Setup Rapido (5 minuti)**

### Step 1: Apri Jenkins
```
URL: http://localhost:32000
```

### Step 2: Crea Nuovo Job

1. Click **"New Item"** (in alto a sinistra)
2. Nome job: `insightlearn-automated-tests`
3. Seleziona: **"Pipeline"**
4. Click **"OK"**

### Step 3: Configura Pipeline

Nella pagina di configurazione:

**General** → Descrizione:
```
InsightLearn WASM - Automated Testing Pipeline
Runs comprehensive tests including performance, load testing, and monitoring.
```

**Build Triggers** → ✅ Check:
- [ ] Build periodically
  - Schedule: `H * * * *` (ogni ora)

**Pipeline** → Definition:
- Seleziona: **"Pipeline script from SCM"**
- SCM: **Git**
- Repository URL: `https://github.com/marypas74/InsightLearn_WASM.git`
- Branch: `*/main`
- Script Path: `Jenkinsfile`

Click **"Save"**

### Step 4: Run Primo Build

1. Click **"Build Now"** (sidebar sinistra)
2. Aspetta che appaia #1 sotto "Build History"
3. Click su **#1**
4. Click **"Console Output"**

---

## 📊 **Cosa Aspettarsi**

### Output del Build:

```
[Pipeline] Start
[Pipeline] stage: Preparation
✅ Testing site: https://www.insightlearn.cloud

[Pipeline] stage: Health Check
✅ Main site: 200
⚠️  API Health: 502 (known issue - being fixed)

[Pipeline] stage: Page Availability Test
✅ /: 200
✅ /login: 200
✅ /register: 200
✅ /courses: 200
✅ /dashboard: 200
✅ /admin: 200
✅ /profile: 200
✅ /about: 200
✅ /contact: 200

[Pipeline] stage: Performance Benchmarking
✅ Average response time: ~110ms (excellent!)

[Pipeline] stage: Load Testing
✅ Simulating 50 concurrent users...
✅ All requests successful

[Pipeline] stage: Asset Validation
✅ All CSS files: 4/4
✅ All JS files: 3/3
✅ All images: 2/2

[Pipeline] stage: Security Headers Check
✅ X-Frame-Options found
✅ X-XSS-Protection found
⚠️  Content-Security-Policy missing (recommended)

[Pipeline] stage: Backend API Monitoring
✅ API pods: 2/2 Running
✅ MongoDB: Running
✅ Ollama: Running
✅ Redis: Running

[Pipeline] SUCCESS
```

### Durata Build:
- **Tempo stimato**: 1-2 minuti
- **Risultato atteso**: **SUCCESS** (con warning per backend API)

---

## 🔧 **Alternative: Configurazione Manuale**

Se preferisci configurare senza Git:

1. **General**: Come sopra
2. **Pipeline** → Definition:
   - Seleziona: **"Pipeline script"**
   - Copia/incolla il contenuto di [Jenkinsfile](../Jenkinsfile) direttamente nel campo "Script"

---

## 📝 **Test Manuali (già eseguiti)**

### Load Test Results:
```
Endpoint: /
- Concurrent users: 10
- Total requests: 50
- Successful: 50/50 (100%)
- Avg response time: 0.636s
- Requests/sec: 8.33

Endpoint: /courses
- Concurrent users: 5
- Total requests: 20
- Successful: 20/20 (100%)
- Avg response time: 0.111s
- Requests/sec: 20.00
```

**Report salvato in**: `load-test-report-20251108_182007.txt`

---

## 🎯 **Next Steps Dopo Setup**

### Immediate (oggi):
- [x] Configura job Jenkins
- [ ] Run primo build
- [ ] Verifica console output
- [ ] Fix backend API (502 errors) - in corso

### High Priority (questa settimana):
- [ ] Update Jenkinsfile per testare backend quando fixed
- [ ] Aggiungi Content-Security-Policy header
- [ ] Setup Slack notifications (optional)

### Medium Priority (prossime 2 settimane):
- [ ] Crea Grafana dashboard per metriche
- [ ] Schedule weekly heavy load tests
- [ ] Integra con Prometheus

### Low Priority (backlog):
- [ ] Add regression tests
- [ ] Setup canary deployments

---

## 📧 **Email Notifications**

Configurate per inviare email a: **marcello.pasqui@gmail.com**

Email sarà inviata automaticamente su:
- ❌ Build failed
- ⚠️  Performance degradation
- 🔴 API endpoint failures

---

## 🆘 **Troubleshooting**

### "403 No valid crumb" error
**Causa**: CSRF protection attivo
**Soluzione**: Usa Jenkins UI invece di API REST

### Job non trovato
**Causa**: Job non ancora creato
**Soluzione**: Segui Step 2-3 sopra

### Build fails immediatamente
**Causa**: Repository non accessibile
**Soluzione**: Verifica URL GitHub o usa "Pipeline script" invece di "Pipeline script from SCM"

---

## 📚 **File di Riferimento**

- [Jenkinsfile](../Jenkinsfile) - Pipeline definition
- [jenkins/README.md](README.md) - Documentazione completa
- [jenkins/scripts/load-test.sh](scripts/load-test.sh) - Load testing script
- [jenkins/scripts/site-monitor.sh](scripts/site-monitor.sh) - Monitoring script

---

**Ultimo aggiornamento**: 2025-11-08
**Maintainer**: marcello.pasqui@gmail.com
