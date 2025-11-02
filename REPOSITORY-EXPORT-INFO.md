# InsightLearn WASM - Repository Export Info

## File Esportati

### 1. Git Bundle
**File**: `/home/mpasqui/insightlearn-wasm-complete.bundle`
**Dimensione**: ~2 MB
**Tipo**: Git repository completo con history

**Come usare:**
```bash
# Su nuovo sistema/server
git clone /path/to/insightlearn-wasm-complete.bundle insightlearn-wasm
cd insightlearn-wasm
git log  # Verifica 3 commit presenti
```

### 2. Tar.gz Archive  
**File**: `/home/mpasqui/insightlearn-wasm-complete-v1.4.29.tar.gz`
**Dimensione**: ~6 MB
**Tipo**: Archive compresso con .git incluso

**Come usare:**
```bash
# Su nuovo sistema
tar -xzf insightlearn-wasm-complete-v1.4.29.tar.gz
cd insightlearn-wasm
git status  # Repository Git intatta
```

## Contenuto Repository

**Totale file**: 348
**Totale commits**: 3
**Versione**: v1.4.29-dev
**Branch**: main

### Struttura

```
insightlearn-wasm/
├── src/                (260 file) - Codice sorgente .NET
├── k8s/                (35 file) - Kubernetes manifests + scripts
├── monitoring/         (3 file) - Grafana dashboards
├── jenkins/            (1 file) - Jenkins automation
├── tests/              (38 file) - Unit, Integration, Stress tests
├── docs/               (10 file) - Documentazione tecnica
├── config/             (1 file) - Configurazioni appsettings
├── .gitignore
├── .env.example
├── CLAUDE.md
├── README.md
├── MIGRATION-GUIDE.md
├── REPOSITORY-SUMMARY.md
├── Dockerfile.wasm
├── docker-compose.yml
└── Directory.Build.props
```

## Commits History

```
3ba9391 - feat: Add tests, configurations, and deployment tools
fced323 - feat: Add Kubernetes, Grafana, and Jenkins configurations  
8c32b44 - Initial commit: InsightLearn Blazor WebAssembly Frontend
```

## Verifica Integrità

### Checksum Files

```bash
# Git bundle
sha256sum /home/mpasqui/insightlearn-wasm-complete.bundle

# Tar.gz
sha256sum /home/mpasqui/insightlearn-wasm-complete-v1.4.29.tar.gz
```

### Verifica Contenuto

```bash
# Git bundle
git bundle verify /home/mpasqui/insightlearn-wasm-complete.bundle

# Tar.gz
tar -tzf /home/mpasqui/insightlearn-wasm-complete-v1.4.29.tar.gz | wc -l
# Dovrebbe mostrare ~350+ file
```

## Import su GitHub/GitLab

### Opzione 1: Da Git Bundle

```bash
# Clone bundle
git clone insightlearn-wasm-complete.bundle insightlearn-wasm
cd insightlearn-wasm

# Aggiungi remote GitHub
git remote remove origin  # Rimuovi bundle
git remote add origin https://github.com/YOUR_USERNAME/insightlearn-wasm.git

# Push
git push -u origin main

# Push tags (opzionale)
git tag -a v1.4.29-dev -m "Initial release"
git push origin v1.4.29-dev
```

### Opzione 2: Da Tar.gz

```bash
# Estrai
tar -xzf insightlearn-wasm-complete-v1.4.29.tar.gz
cd insightlearn-wasm

# Verifica Git
git status

# Aggiungi remote
git remote add origin https://github.com/YOUR_USERNAME/insightlearn-wasm.git

# Push
git push -u origin main
```

## Link Repository (Dopo Upload)

Una volta caricato su GitHub/GitLab:

- **Repository**: https://github.com/YOUR_USERNAME/insightlearn-wasm
- **Clone URL**: `git clone https://github.com/YOUR_USERNAME/insightlearn-wasm.git`
- **Raw files**: https://github.com/YOUR_USERNAME/insightlearn-wasm/tree/main
- **README**: https://github.com/YOUR_USERNAME/insightlearn-wasm/blob/main/README.md

## Trasferimento File

### Via SCP

```bash
# Da sistema corrente a server remoto
scp /home/mpasqui/insightlearn-wasm-complete.bundle user@server:/path/to/destination/
```

### Via USB

```bash
# Copia su USB
cp /home/mpasqui/insightlearn-wasm-complete.bundle /media/usb/

# Su nuovo sistema
cp /media/usb/insightlearn-wasm-complete.bundle ~/
git clone insightlearn-wasm-complete.bundle insightlearn-wasm
```

### Via HTTP/Web

Se hai server web:
```bash
# Metti in directory web accessible
cp /home/mpasqui/insightlearn-wasm-complete.tar.gz /var/www/html/downloads/

# Download da altro sistema
wget http://YOUR_SERVER/downloads/insightlearn-wasm-complete-v1.4.29.tar.gz
```

## Statistiche

- **Linee di codice**: ~82,000
- **Linguaggi**: C# (85%), JavaScript (8%), PowerShell (3%), YAML (2%), HTML/CSS (2%)
- **Framework**: .NET 8, Blazor WebAssembly
- **Infrastruttura**: Kubernetes, Docker, Grafana, Jenkins
- **Testing**: xUnit, K6, PowerShell

## Support

Per problemi:
1. Leggi MIGRATION-GUIDE.md
2. Leggi REPOSITORY-SUMMARY.md  
3. Verifica .git directory esiste
4. Controlla git log

---

**Generato**: 2025-11-02
**Versione**: v1.4.29-dev
**Commit**: 3ba9391
