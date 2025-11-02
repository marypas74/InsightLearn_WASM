# Guida Upload Repository su GitHub

## Opzione 1: Upload Manuale via GitHub Web (PIÙ SEMPLICE)

### Passo 1: Crea Repository su GitHub.com

1. Vai su: https://github.com/new
2. **Repository name**: `insightlearn-wasm`
3. **Description**: `Complete Blazor WebAssembly frontend with Kubernetes infrastructure for InsightLearn LMS`
4. **Visibility**: Scegli Public o Private
5. **⚠️ IMPORTANTE**: NON selezionare "Initialize with README" (abbiamo già tutto!)
6. Click **"Create repository"**

### Passo 2: Push da Terminale

Dopo la creazione, GitHub ti mostrerà dei comandi. Esegui questi:

```bash
cd /home/mpasqui/insightlearn-wasm

# Aggiungi remote (sostituisci TUO_USERNAME)
git remote add origin https://github.com/TUO_USERNAME/insightlearn-wasm.git

# Verifica remote
git remote -v

# Push main branch
git push -u origin main

# Push eventuali tag (opzionale)
git tag -a v1.4.29-dev -m "Initial release v1.4.29-dev"
git push origin v1.4.29-dev
```

### Passo 3: Verifica su Web

Vai su: `https://github.com/TUO_USERNAME/insightlearn-wasm`

Dovresti vedere:
- ✅ 4 commits
- ✅ 350 file
- ✅ README.md visualizzato
- ✅ Tutte le directory (src/, k8s/, monitoring/, tests/, docs/)

---

## Opzione 2: Upload via GitHub CLI (gh)

Se hai installato GitHub CLI:

```bash
# Login (se necessario)
gh auth login

# Vai nella directory
cd /home/mpasqui/insightlearn-wasm

# Crea repository e push automatico
gh repo create insightlearn-wasm \
  --private \
  --source=. \
  --remote=origin \
  --push

# Visualizza nel browser
gh repo view --web
```

---

## Opzione 3: Upload da Git Bundle

Se non hai accesso Git dal sistema corrente:

### Sul sistema corrente:

```bash
# Bundle già creato
ls -lh /home/mpasqui/insightlearn-wasm-complete.bundle
```

### Su sistema con accesso GitHub:

```bash
# 1. Trasferisci bundle (USB, SCP, etc.)
# 2. Clone bundle
git clone /path/to/insightlearn-wasm-complete.bundle insightlearn-wasm
cd insightlearn-wasm

# 3. Rimuovi remote bundle
git remote remove origin

# 4. Crea repository su GitHub (via web o gh CLI)

# 5. Aggiungi nuovo remote
git remote add origin https://github.com/TUO_USERNAME/insightlearn-wasm.git

# 6. Push
git push -u origin main
```

---

## Opzione 4: Upload da Tar.gz Archive

### Sul sistema corrente:

```bash
# Archive già creato
ls -lh /home/mpasqui/insightlearn-wasm-complete-v1.4.29.tar.gz
```

### Su sistema con accesso GitHub:

```bash
# 1. Trasferisci archive
# 2. Estrai
tar -xzf insightlearn-wasm-complete-v1.4.29.tar.gz
cd insightlearn-wasm

# 3. Verifica Git
git status
git log

# 4. Aggiungi remote GitHub
git remote add origin https://github.com/TUO_USERNAME/insightlearn-wasm.git

# 5. Push
git push -u origin main
```

---

## Configurazione Repository Post-Upload

### 1. Aggiungi Topics (GitHub Web)

Settings → Topics:
- `blazor-webassembly`
- `dotnet`
- `csharp`
- `kubernetes`
- `docker`
- `grafana`
- `jenkins`
- `lms`
- `learning-management-system`

### 2. Configura About Section

Description:
> Complete Blazor WebAssembly frontend with Kubernetes deployment infrastructure, Grafana monitoring, Jenkins CI/CD, and comprehensive testing suite for InsightLearn Learning Management System.

### 3. Crea Release (Opzionale)

GitHub Web → Releases → Create Release:
- **Tag**: v1.4.29-dev
- **Title**: Initial Release v1.4.29-dev
- **Description**:
  ```markdown
  ## InsightLearn Blazor WASM - Initial Release
  
  Complete Blazor WebAssembly frontend with production-ready infrastructure.
  
  ### Features
  - ✅ Blazor WebAssembly .NET 8 frontend
  - ✅ Kubernetes deployment (22 YAML manifests)
  - ✅ Grafana monitoring dashboards
  - ✅ Jenkins CI/CD automation
  - ✅ Comprehensive test suite (Unit, Integration, K6 stress)
  - ✅ Docker containerization
  - ✅ Complete documentation
  
  ### Files
  - 350 source files
  - 82,000+ lines of code
  - 4 commits with clean history
  
  ### Quick Start
  See [README.md](README.md) for deployment instructions.
  ```

### 4. Proteggi Branch Main (Opzionale)

Settings → Branches → Add rule:
- Branch name pattern: `main`
- ✅ Require pull request reviews before merging
- ✅ Require status checks to pass before merging
- Save changes

---

## Troubleshooting

### Errore: Authentication failed

Usa Personal Access Token invece di password:

1. GitHub → Settings → Developer settings → Personal access tokens → Tokens (classic)
2. Generate new token (classic)
3. Scopes: repo (all), workflow
4. Copia token
5. Usa token come password quando Git richiede credenziali

Oppure salva token nel remote URL:
```bash
git remote set-url origin https://USERNAME:TOKEN@github.com/USERNAME/insightlearn-wasm.git
```

### Errore: Repository already exists

Se hai già creato la repo vuota:
```bash
git remote add origin https://github.com/TUO_USERNAME/insightlearn-wasm.git
git push -u origin main --force  # Solo se repo è veramente vuota!
```

### Errore: Updates were rejected

Se ci sono conflitti:
```bash
git pull origin main --rebase
git push -u origin main
```

---

## Verifica Upload Completato

### Checklist Post-Upload

```bash
# 1. Clone in directory temporanea per test
cd /tmp
git clone https://github.com/TUO_USERNAME/insightlearn-wasm.git test-clone
cd test-clone

# 2. Verifica commit
git log --oneline
# Dovrebbe mostrare 4 commit

# 3. Verifica file
git ls-files | wc -l
# Dovrebbe mostrare 350

# 4. Verifica build (se .NET installato)
dotnet restore
dotnet build src/InsightLearn.WebAssembly/InsightLearn.WebAssembly.csproj

# 5. Pulizia
cd .. && rm -rf test-clone
```

### Web Verification

Vai su: `https://github.com/TUO_USERNAME/insightlearn-wasm`

Verifica:
- ✅ README.md visualizzato correttamente
- ✅ File CLAUDE.md presente
- ✅ Directories: src/, k8s/, monitoring/, jenkins/, tests/, docs/
- ✅ Commit history visibile
- ✅ Branch: main (default)

---

## Link Utili Post-Upload

- **Repository**: https://github.com/TUO_USERNAME/insightlearn-wasm
- **Clone HTTPS**: `git clone https://github.com/TUO_USERNAME/insightlearn-wasm.git`
- **Clone SSH**: `git clone git@github.com:TUO_USERNAME/insightlearn-wasm.git`
- **Issues**: https://github.com/TUO_USERNAME/insightlearn-wasm/issues
- **Actions**: https://github.com/TUO_USERNAME/insightlearn-wasm/actions
- **Releases**: https://github.com/TUO_USERNAME/insightlearn-wasm/releases

---

## Next Steps

Dopo l'upload:

1. **Aggiungi Collaboratori** (se necessario)
   - Settings → Collaborators → Add people

2. **Setup GitHub Actions** (opzionale)
   - Aggiungi `.github/workflows/build.yml` per CI/CD automatico

3. **Aggiungi Badge al README** (opzionale)
   ```markdown
   ![Build](https://github.com/TUO_USERNAME/insightlearn-wasm/actions/workflows/build.yml/badge.svg)
   ```

4. **Clone su Sistemi Produzione**
   ```bash
   git clone https://github.com/TUO_USERNAME/insightlearn-wasm.git
   cd insightlearn-wasm
   ./k8s/deploy.sh
   ```

---

**Nota**: Sostituisci `TUO_USERNAME` con il tuo username GitHub effettivo!

**File Pronti**:
- `/home/mpasqui/insightlearn-wasm/` - Repository locale
- `/home/mpasqui/insightlearn-wasm-complete.bundle` - Git bundle (2 MB)
- `/home/mpasqui/insightlearn-wasm-complete-v1.4.29.tar.gz` - Archive (11 MB)
