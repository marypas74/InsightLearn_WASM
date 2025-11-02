# Guida Migrazione InsightLearn WASM su Nuovo Sistema

## Riepilogo Repository

**Repository locale**: `/home/mpasqui/insightlearn-wasm`
**Commit**: 8c32b44 - Initial commit
**Versione**: v1.4.29-dev
**File totali**: 271 file
**Linee codice**: 72,303

## Verifica Contenuto Repository

```bash
cd /home/mpasqui/insightlearn-wasm

# Verifica commit
git log --oneline

# Verifica file
git ls-files | wc -l  # Dovrebbe mostrare 271

# Verifica dimensione
du -sh .

# Lista progetti
ls -la src/

# Lista documentazione
ls -la docs/
```

## Metodi di Migrazione

### Metodo 1: Tar Archive (CONSIGLIATO per offline)

```bash
# Crea archive completo
cd /home/mpasqui
tar -czf insightlearn-wasm-v1.4.29.tar.gz insightlearn-wasm/

# Verifica archive
tar -tzf insightlearn-wasm-v1.4.29.tar.gz | head -20

# Copia su nuovo sistema (USB, SCP, etc.)
# Esempio SCP:
scp insightlearn-wasm-v1.4.29.tar.gz user@new-system:/path/to/destination/

# Sul nuovo sistema:
cd /path/to/destination
tar -xzf insightlearn-wasm-v1.4.29.tar.gz
cd insightlearn-wasm
git status  # Verifica repository Git intatta
```

### Metodo 2: Git Remote Repository (CONSIGLIATO per online)

```bash
cd /home/mpasqui/insightlearn-wasm

# Aggiungi remote GitHub/GitLab
git remote add origin https://github.com/YOUR_USERNAME/insightlearn-wasm.git

# Push a remote
git push -u origin main

# Sul nuovo sistema:
git clone https://github.com/YOUR_USERNAME/insightlearn-wasm.git
cd insightlearn-wasm
git log  # Verifica commit history
```

### Metodo 3: Git Bundle (Per sistemi non connessi)

```bash
# Sistema sorgente
cd /home/mpasqui/insightlearn-wasm
git bundle create insightlearn-wasm.bundle --all

# Copia insightlearn-wasm.bundle su nuovo sistema

# Sistema destinazione
git clone insightlearn-wasm.bundle insightlearn-wasm
cd insightlearn-wasm
git remote remove origin  # Rimuovi bundle come remote
```

## Checklist Verifica Post-Migrazione

Sul **nuovo sistema**, esegui questi comandi per verificare:

```bash
cd insightlearn-wasm

# 1. Verifica Git repository
[ -d .git ] && echo "✅ Git repository presente" || echo "❌ Git repository mancante"

# 2. Verifica commit
git log --oneline | head -1 | grep "8c32b44" && echo "✅ Commit corretto" || echo "❌ Commit diverso"

# 3. Verifica file count
FILE_COUNT=$(git ls-files | wc -l)
[ "$FILE_COUNT" -eq 271 ] && echo "✅ Tutti i 271 file presenti" || echo "⚠️  File count: $FILE_COUNT (atteso 271)"

# 4. Verifica struttura directory
for dir in src docs; do
    [ -d "$dir" ] && echo "✅ Directory $dir presente" || echo "❌ Directory $dir mancante"
done

# 5. Verifica file critici
for file in README.md CLAUDE.md Dockerfile.wasm Directory.Build.props InsightLearn.WASM.sln; do
    [ -f "$file" ] && echo "✅ $file presente" || echo "❌ $file mancante"
done

# 6. Verifica progetti .NET
for proj in WebAssembly Core Infrastructure Application; do
    CSPROJ="src/InsightLearn.$proj/InsightLearn.$proj.csproj"
    [ -f "$CSPROJ" ] && echo "✅ Progetto $proj presente" || echo "❌ Progetto $proj mancante"
done

# 7. Test build .NET (se .NET SDK installato)
if command -v dotnet &> /dev/null; then
    dotnet restore && echo "✅ dotnet restore OK" || echo "❌ dotnet restore FAIL"
    dotnet build src/InsightLearn.WebAssembly/InsightLearn.WebAssembly.csproj && \
        echo "✅ Build OK" || echo "❌ Build FAIL"
else
    echo "⚠️  .NET SDK non installato - skipping build test"
fi
```

## File e Directory Principali

```
insightlearn-wasm/
├── .git/                                  # Git repository
├── .gitignore                             # Git ignore rules
├── CLAUDE.md                              # Claude Code guidance
├── CLAUDE-ORIGINAL.md                     # Original CLAUDE.md backup
├── README.md                              # Documentazione principale
├── MIGRATION-GUIDE.md                     # Questo file
├── Directory.Build.props                  # MSBuild config
├── Dockerfile.wasm                        # Docker build
├── InsightLearn.WASM.sln                 # Visual Studio solution
├── docs/                                  # Documentazione (10 file)
│   ├── WASM-*.md
│   ├── DEPLOYMENT-SUMMARY.md
│   └── MONITORING-GUIDE.md
└── src/                                   # Codice sorgente (260 file)
    ├── InsightLearn.WebAssembly/         # Progetto principale WASM
    ├── InsightLearn.Core/                # Domain models
    ├── InsightLearn.Infrastructure/      # Infrastruttura
    └── InsightLearn.Application/         # Business logic
```

## Requisiti Nuovo Sistema

### Software Necessario

- **Git** (per repository version control)
- **.NET 8 SDK** (per build e development)
- **Docker** (opzionale, per containerizzazione)
- **Editor**: Visual Studio 2022, VS Code, o Rider

### Installazione .NET 8 SDK

```bash
# Ubuntu/Debian
wget https://dot.net/v1/dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 8.0

# Windows
# Download da: https://dotnet.microsoft.com/download/dotnet/8.0

# Verifica installazione
dotnet --version  # Dovrebbe mostrare 8.0.x
```

### Installazione Git

```bash
# Ubuntu/Debian
sudo apt-get update
sudo apt-get install git

# Windows
# Download da: https://git-scm.com/download/win

# Verifica
git --version
```

## Primo Build sul Nuovo Sistema

```bash
cd insightlearn-wasm

# 1. Restore dependencies
dotnet restore

# 2. Build progetto
dotnet build src/InsightLearn.WebAssembly/InsightLearn.WebAssembly.csproj -c Release

# 3. (Opzionale) Build Docker image
docker build -f Dockerfile.wasm -t insightlearn/wasm:v1.4.29-dev .

# 4. (Opzionale) Run locale
dotnet run --project src/InsightLearn.WebAssembly/InsightLearn.WebAssembly.csproj
```

## Verifica Hash Integrità

```bash
# Sul sistema sorgente, crea hash dei file importanti
cd /home/mpasqui/insightlearn-wasm
find src -name "*.cs" -o -name "*.csproj" | sort | xargs cat | sha256sum > /tmp/source-hash.txt

# Sul sistema destinazione, verifica hash
cd insightlearn-wasm
find src -name "*.cs" -o -name "*.csproj" | sort | xargs cat | sha256sum
# Confronta con hash da /tmp/source-hash.txt
```

## Troubleshooting

### Problema: "Not a git repository"

```bash
# Verifica .git directory
ls -la | grep .git

# Se mancante, re-init Git
git init
git add .
git commit -m "Initial commit (restored)"
```

### Problema: "NuGet packages missing"

```bash
# Clear NuGet cache
dotnet nuget locals all --clear

# Restore
dotnet restore --force
```

### Problema: "Build failed"

```bash
# Verifica .NET SDK version
dotnet --version  # Deve essere >= 8.0

# Pulizia build
dotnet clean
rm -rf src/*/bin src/*/obj

# Rebuild
dotnet build
```

## Informazioni Dimensioni

- **Repository Git**: ~15 MB
- **Archive tar.gz**: ~5 MB
- **Con node_modules** (se presenti): +50-100 MB
- **Con bin/obj dopo build**: +200-300 MB

## Contatto e Support

Per problemi durante la migrazione:
1. Verifica checklist sopra
2. Controlla git log e git status
3. Verifica file critici esistano
4. Testa build .NET

---

**Creato**: 2025-11-02
**Versione Repository**: v1.4.29-dev
**Commit**: 8c32b44
