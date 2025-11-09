# Jenkins Jobs - Configurazione UI (2 Minuti)

## 🚀 Setup Rapido

### 1. Apri Jenkins
```
URL: http://localhost:32000
```

### 2. Job #1: Test Automatici (Hourly)

**Step-by-Step**:
1. Click **"New Item"** (alto sinistra)
2. Nome: `insightlearn-automated-tests`
3. Tipo: **Pipeline** ✅
4. Click **OK**

**Configuration**:
- **Build Triggers**: ✅ Build periodically
  - Schedule: `H * * * *`

- **Pipeline**:
  - Definition: **Pipeline script from SCM**
  - SCM: **Git**
  - Repository URL: `https://github.com/marypas74/InsightLearn_WASM.git`
  - Branch: `*/main`
  - Script Path: `Jenkinsfile`

- Click **SAVE**

### 3. Job #2: Weekly Load Tests

**Repeat Step 2 con**:
- Nome: `insightlearn-weekly-heavy-load-test`
- Schedule: `0 2 * * 0` (Domenica 2 AM)
- Script Path: `jenkins/pipelines/weekly-heavy-load-test.Jenkinsfile`

### 4. Test

Click **"Build Now"** su primo job → Attendi 1-2 min → Verifica SUCCESS ✅

---

**Fatto!** Jenkins ora esegue test automatici ogni ora + load test settimanali.
