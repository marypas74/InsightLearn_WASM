# MongoDB VS Code Extension - Setup Guide

**Status**: ✅ Configurato
**Data**: 2025-01-17
**Database**: `insightlearn_videos`
**User**: `insightlearn`
**Porta Locale**: 27017 (via port-forward)

## 📋 Prerequisiti

- VS Code installato
- Cluster Kubernetes (K3s) in esecuzione
- `kubectl` configurato con accesso al namespace `insightlearn`

## 🔧 Step 1: Installazione Estensione

1. Apri VS Code
2. Vai alla sezione Extensions (`Ctrl+Shift+X`)
3. Cerca: **"MongoDB for VS Code"**
4. Publisher: **MongoDB**
5. Clicca **Install**

Oppure da terminale:
```bash
code --install-extension mongodb.mongodb-vscode
```

## 🚀 Step 2: Avvia Port-Forward MongoDB

**IMPORTANTE**: MongoDB usa ClusterIP, quindi serve port-forward per accesso locale.

Avvia lo script persistente:
```bash
cd /home/mpasqui/insightlearn_WASM/InsightLearn_WASM
./k8s/mongodb-port-forward-persistent.sh &
```

Oppure manualmente:
```bash
kubectl port-forward -n insightlearn svc/mongodb-service 27017:27017
```

**Verifica**: Lascia il terminale aperto. Port-forward si riavvia automaticamente se disconnesso.

## 🔐 Step 3: Configurazione Connection String

### Metodo 1: Configurazione Automatica (Consigliato)

La connection string è già configurata in `.vscode/mongodb-connection.json`:

```json
{
  "id": "insightlearn-mongodb",
  "name": "InsightLearn MongoDB (Local Port-Forward)",
  "connectionString": "mongodb://insightlearn:GpYb2EZ3srVBb0Ziv0kG4Ual3hxaY9oT@localhost:27017/insightlearn_videos?authSource=admin"
}
```

1. Apri VS Code nella root del progetto
2. L'estensione MongoDB dovrebbe rilevare automaticamente la configurazione
3. Apri la sidebar MongoDB (icona foglia verde)
4. Clicca su "Connect" accanto a **"InsightLearn MongoDB (Local Port-Forward)"**

### Metodo 2: Configurazione Manuale

Se la configurazione automatica non funziona:

1. Apri la sidebar MongoDB in VS Code
2. Clicca su **"Add Connection"**
3. Scegli **"Advanced Connection Settings"**
4. Inserisci la connection string:

```
mongodb://insightlearn:GpYb2EZ3srVBb0Ziv0kG4Ual3hxaY9oT@localhost:27017/insightlearn_videos?authSource=admin
```

**Parametri Connection String**:
- **User**: `insightlearn`
- **Password**: `GpYb2EZ3srVBb0Ziv0kG4Ual3hxaY9oT`
- **Host**: `localhost:27017`
- **Database**: `insightlearn_videos`
- **Auth Source**: `admin` ⚠️ **CRITICO** - User è creato in admin database

5. Clicca **"Connect"**

## 📊 Step 4: Esplora Database e Collections

Dopo la connessione, dovresti vedere:

```
📁 insightlearn_videos
  📁 videos (GridFS)
    📄 videos.files      (Metadata video MongoDB GridFS)
    📄 videos.chunks     (Binary chunks video)
  📄 VideoTranscripts    (Trascrizioni video ASR)
  📄 VideoKeyTakeaways   (AI-extracted concepts)
  📄 AIConversationHistory (Chat history)
  📄 VideoTranslations   (Traduzioni video AI)
  📄 TranslatedSubtitles (Cache traduzioni sottotitoli)
```

## 🔍 Query di Esempio

### 1. Contare video totali in GridFS

```javascript
use insightlearn_videos
db.videos.files.countDocuments()
```

**Expected Output**: `140` (current test video count)

### 2. Listare primi 10 video con metadata

```javascript
use insightlearn_videos
db.videos.files.find({}, {
  _id: 1,
  filename: 1,
  length: 1,
  uploadDate: 1,
  metadata: 1
}).limit(10).sort({ uploadDate: -1 })
```

### 3. Query trascrizioni per lessonId

```javascript
use insightlearn_videos
db.VideoTranscripts.find({
  lessonId: "28d88850-81c8-4628-a022-d98378d883e3"
})
```

### 4. Aggregation: Video per formato

```javascript
use insightlearn_videos
db.videos.files.aggregate([
  {
    $group: {
      _id: "$metadata.format",
      count: { $sum: 1 },
      totalSize: { $sum: "$length" }
    }
  },
  {
    $project: {
      format: "$_id",
      count: 1,
      totalSizeMB: { $round: [{ $divide: ["$totalSize", 1048576] }, 2] }
    }
  }
])
```

**Expected Output**:
```json
[
  { "_id": "mp4", "count": 130, "totalSizeMB": 2891.45 },
  { "_id": "webm", "count": 10, "totalSizeMB": 10.35 }
]
```

### 5. Full-text search nelle trascrizioni

```javascript
use insightlearn_videos
db.VideoTranscripts.find({
  $text: { $search: "ASP.NET Core" }
}, {
  lessonId: 1,
  language: 1,
  score: { $meta: "textScore" }
}).sort({ score: { $meta: "textScore" } })
```

## 🛠️ MongoDB Playground (.mongodb files)

L'estensione supporta file `.mongodb` per salvare query:

**Crea file**: `queries/video-stats.mongodb`
```javascript
// MongoDB Playground - Video Statistics
use('insightlearn_videos');

// Total videos
db.videos.files.countDocuments()

// Videos by format
db.videos.files.aggregate([
  { $group: { _id: "$metadata.format", count: { $sum: 1 } } }
])

// Latest 10 uploads
db.videos.files.find({}, { filename: 1, uploadDate: 1, length: 1 })
  .sort({ uploadDate: -1 })
  .limit(10)
```

**Esegui**: Clicca sul play button accanto alla query oppure `Ctrl+Alt+R`

## ⚙️ Impostazioni VS Code Raccomandate

Aggiungi a `.vscode/settings.json`:

```json
{
  "mongodb.showMongoDBConnectionExplorer": true,
  "mongodb.showMongoDBPlaygrounds": true,
  "mongodb.maxNumberOfProblemsInDiagnostics": 100,
  "mongodb.confirmRunAll": true,
  "mongodb.defaultLimit": 10,
  "files.associations": {
    "*.mongodb": "mongodb"
  }
}
```

## 🔐 Security Best Practices

1. **NON committare password**: `.vscode/mongodb-connection.json` è in `.gitignore`
2. **Rotate password**: Ogni 90 giorni (script: `scripts/rotate-secrets-production-safe.sh`)
3. **Port-forward solo su localhost**: Non esporre porta 27017 esternamente
4. **Use read-only user**: Per operazioni di sola lettura, crea user dedicato

## 🐛 Troubleshooting

### Problema: "Connection timeout"
**Causa**: Port-forward non attivo
**Fix**:
```bash
# Verifica port-forward attivo
ps aux | grep "kubectl port-forward.*mongodb"

# Riavvia se necessario
./k8s/mongodb-port-forward-persistent.sh &
```

### Problema: "Authentication failed"
**Causa 1**: Password errata
**Fix**: Verifica password da Kubernetes Secret
```bash
kubectl get secret insightlearn-secrets -n insightlearn -o jsonpath='{.data.mongodb-password}' | base64 -d
```

**Causa 2**: AuthSource errato
**Fix**: Usa `authSource=admin` (user creato in admin database)
```
mongodb://insightlearn:PASSWORD@localhost:27017/insightlearn_videos?authSource=admin
```

### Problema: "Database not found"
**Causa**: Connessione al database sbagliato
**Fix**: Verifica database name
```javascript
// In MongoDB Playground
show dbs  // Lista tutti i database
use insightlearn_videos  // Switch al database corretto
```

### Problema: "Collection empty"
**Causa**: Nessun video uploadato ancora
**Fix**: Verifica con API
```bash
curl http://localhost:31081/api/video/metadata/693bd380a633a1ccf7f519e7
```

## 📚 Documentazione Aggiuntiva

- **MongoDB GridFS**: [docs/MONGODB-GRIDFS.md](MONGODB-GRIDFS.md)
- **Video Streaming**: [docs/VIDEO-TEST-LINKS.md](VIDEO-TEST-LINKS.md)
- **Transcript System**: [CLAUDE.md](../CLAUDE.md) - Sezione "Hybrid MongoDB + Qdrant Video Transcription"

## 🎯 Quick Reference

| Operazione | Comando |
|------------|---------|
| **Avvia port-forward** | `./k8s/mongodb-port-forward-persistent.sh &` |
| **Verifica connessione** | `mongosh "mongodb://insightlearn:PASSWORD@localhost:27017/insightlearn_videos?authSource=admin"` |
| **Get password** | `kubectl get secret insightlearn-secrets -n insightlearn -o jsonpath='{.data.mongodb-password}' \| base64 -d` |
| **List databases** | `show dbs` (in MongoDB Playground) |
| **Count videos** | `db.videos.files.countDocuments()` |
| **View collections** | `show collections` (in MongoDB Playground) |

---

**Last Updated**: 2025-01-17
**MongoDB Version**: 7.0
**VS Code Extension**: MongoDB for VS Code (latest)
**Status**: ✅ Fully Configured
