# Includere Dati nel Backup - Fotografia Speculare Completa

## Obiettivo

Creare una **fotografia speculare** del sito attuale **CON TUTTI I DATI** per ripristino completo su un'altra piattaforma.

---

## Procedura Completa

### Step 1: Backup Dati Correnti

Sul sistema **SORGENTE** (dove hai i dati attuali):

```bash
cd /home/mpasqui/insightlearn-wasm

# Esegui backup completo
./backup-data.sh

# Output esempio:
# ✅ SQL Server backup saved: 250MB
# ✅ MongoDB backup saved: 1.2GB
# ✅ Redis backup saved: 45MB
# ✅ API files backup saved: 3.5GB
# ✅ Backup complete: ./backups/data_20251102_223000
```

Questo crea:
- `InsightLearnDb.bak` - Database SQL Server COMPLETO con tutti i dati
- `mongodb_dump/` - Tutti i dati MongoDB (video, chatbot messages)
- `redis_dump.rdb` - Cache Redis
- `api-files.tar.gz` - Tutti i file caricati dagli utenti
- `restore-data.sh` - Script di ripristino automatico

### Step 2: Preparare Pacchetto Completo

```bash
# Crea archivio completo con codice + dati
tar -czf insightlearn-complete-$(date +%Y%m%d).tar.gz \
  --exclude='.git' \
  --exclude='*.log' \
  --exclude='obj' \
  --exclude='bin' \
  .

# Risultato: archivio con TUTTO
# - Codice sorgente
# - Configurazioni
# - Docker Compose
# - Kubernetes manifests
# - Script deployment
# - Backup dati (backups/)
```

### Step 3: Trasferire su Nuovo Sistema

**Metodo A: USB/Storage Esterno**
```bash
# Copia su USB
cp insightlearn-complete-20251102.tar.gz /media/usb/

# Sul nuovo sistema
cp /media/usb/insightlearn-complete-20251102.tar.gz ~/
cd ~
tar xzf insightlearn-complete-20251102.tar.gz
cd insightlearn-wasm/
```

**Metodo B: Network Transfer**
```bash
# Sul sistema sorgente
scp insightlearn-complete-20251102.tar.gz user@new-server:~/

# Sul nuovo sistema
cd ~
tar xzf insightlearn-complete-20251102.tar.gz
cd insightlearn-wasm/
```

**Metodo C: Cloud Storage**
```bash
# Upload su cloud
# (Google Drive, Dropbox, S3, etc.)

# Download su nuovo sistema
wget https://drive.google.com/...
tar xzf insightlearn-complete-20251102.tar.gz
```

### Step 4: Deploy su Nuovo Sistema

```bash
cd insightlearn-wasm/

# 1. Deploy automatico (crea container vuoti)
./deploy-oneclick.sh

# Attendi che tutti i servizi siano healthy (5-10 minuti)
# L'applicazione funziona ma senza dati ancora

# 2. Ripristina i dati
cd backups/data_20251102_223000/
./restore-data.sh

# Output:
# ✅ SQL Server database restored (250MB)
# ✅ MongoDB database restored (1.2GB)
# ✅ Redis data restored (45MB)
# ✅ User files restored (3.5GB)
# ✅ Data restore complete!

# 3. Restart applicazione
cd ../..
docker-compose restart api web

# 4. Verifica
curl -k https://localhost/api/health
```

### Step 5: Verifica Dati Ripristinati

```bash
# SQL Server - Conta utenti
docker exec insightlearn-sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "${DB_PASSWORD}" -C \
  -Q "SELECT COUNT(*) AS TotalUsers FROM InsightLearnDb.dbo.Users"

# MongoDB - Conta video
docker exec insightlearn-mongodb mongosh \
  -u admin -p "${MONGO_PASSWORD}" \
  --eval "use insightlearn; db.videos.countDocuments()"

# MongoDB - Conta messaggi chatbot
docker exec insightlearn-mongodb mongosh \
  -u admin -p "${MONGO_PASSWORD}" \
  --eval "use insightlearn; db.chatbot_messages.countDocuments()"

# Verifica login
curl -k -X POST https://localhost/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@insightlearn.cloud","password":"PASSWORD"}'
```

---

## Script Automatici Inclusi

### `backup-data.sh`

**Cosa fa:**
1. Esporta database SQL Server completo (`.bak` file)
2. Esporta tutte le collections MongoDB
3. Esporta snapshot Redis
4. Esporta file caricati dagli utenti
5. Crea script `restore-data.sh` automatico
6. Genera report backup con dimensioni

**Output:**
```
backups/data_20251102_223000/
├── InsightLearnDb.bak          # Database SQL Server completo
├── mongodb_dump/               # MongoDB dump
│   └── insightlearn/
│       ├── videos.bson
│       ├── chatbot_messages.bson
│       └── ...
├── redis_dump.rdb              # Redis snapshot
├── api-files.tar.gz            # File caricati utenti
├── restore-data.sh             # Script ripristino automatico
├── backup-info.txt             # Informazioni backup
└── sql_tables_list.txt         # Lista tabelle SQL
```

### `restore-data.sh` (Auto-generato)

**Cosa fa:**
1. Ripristina database SQL Server da `.bak`
2. Ripristina MongoDB con `mongorestore`
3. Ripristina Redis da snapshot
4. Ripristina file caricati dagli utenti
5. Verifica integrità dati

**Uso:**
```bash
cd backups/data_YYYYMMDD_HHMMSS/
./restore-data.sh
```

---

## Verifica Fotografia Speculare

### Checklist Completa

Dopo il ripristino, verifica che sia una **copia esatta**:

- [ ] **Utenti**: Stesso numero di utenti registrati
- [ ] **Corsi**: Stessi corsi disponibili
- [ ] **Video**: Stessi video caricati
- [ ] **Messaggi Chatbot**: Storico conversazioni preservato
- [ ] **File Upload**: Tutti i file caricati presenti
- [ ] **Sessioni Utente**: Login funzionante
- [ ] **Dashboard**: Stesse metriche visualizzate
- [ ] **Grafana**: Stesso storico metrics (se ripristinato)
- [ ] **Admin**: Login admin funzionante con stessi permessi

### Comandi di Verifica

```bash
# 1. Conta tabelle SQL Server
docker exec insightlearn-sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "${DB_PASSWORD}" -C \
  -Q "SELECT COUNT(*) FROM InsightLearnDb.INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE'"

# 2. Lista collections MongoDB
docker exec insightlearn-mongodb mongosh \
  -u admin -p "${MONGO_PASSWORD}" \
  --eval "use insightlearn; db.getCollectionNames()"

# 3. Verifica ultimo utente registrato
docker exec insightlearn-sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "${DB_PASSWORD}" -C \
  -Q "SELECT TOP 5 Email, CreatedAt FROM InsightLearnDb.dbo.Users ORDER BY CreatedAt DESC"

# 4. Verifica ultimo messaggio chatbot
docker exec insightlearn-mongodb mongosh \
  -u admin -p "${MONGO_PASSWORD}" \
  --eval "use insightlearn; db.chatbot_messages.find().sort({createdAt:-1}).limit(5)"
```

---

## Backup Automatico Programmato

### Cron Job (Backup Notturno)

```bash
# Aggiungi al crontab
crontab -e

# Backup ogni notte alle 2:00 AM
0 2 * * * cd /home/mpasqui/insightlearn-wasm && ./backup-data.sh >> /var/log/insightlearn-backup.log 2>&1

# Pulizia backup vecchi (mantieni ultimi 7 giorni)
0 3 * * * find /home/mpasqui/insightlearn-wasm/backups -type d -name "data_*" -mtime +7 -exec rm -rf {} \;
```

---

## Dimensioni Tipiche

**Database SQL Server**: 100MB - 10GB (dipende da numero utenti/corsi)
**MongoDB**: 500MB - 50GB (dipende da video/chatbot)
**Redis**: 10MB - 1GB (cache temporanea)
**API Files**: 1GB - 100GB (file caricati utenti)

**Totale stimato**: 2GB - 160GB

---

## Sicurezza Backup

⚠️ **IMPORTANTE**: I backup contengono dati sensibili!

### Protezione File

```bash
# Cripta backup
tar czf - backups/data_20251102_223000/ | gpg --symmetric --cipher-algo AES256 > backup-encrypted.tar.gz.gpg

# Decripta backup
gpg --decrypt backup-encrypted.tar.gz.gpg | tar xz
```

### Storage Sicuro

1. **Non committare su Git** (`.gitignore` già configurato)
2. **Cripta prima di caricare su cloud**
3. **Usa storage con encryption at rest**
4. **Backup off-site** (diversa location fisica)
5. **Test restore** periodici

---

## Restore su Sistema Diverso

### Scenario: Da Ubuntu a Debian

```bash
# Sul nuovo sistema (Debian)
1. Installa Docker e Docker Compose
2. Estrai archivio completo
3. cd insightlearn-wasm/
4. ./deploy-oneclick.sh
5. cd backups/data_*/
6. ./restore-data.sh
7. docker-compose restart api web
```

### Scenario: Da Locale a Cloud Server

```bash
# Upload backup su S3/Cloud Storage
aws s3 cp insightlearn-complete-20251102.tar.gz s3://my-bucket/

# Su cloud server
aws s3 cp s3://my-bucket/insightlearn-complete-20251102.tar.gz ~/
tar xzf insightlearn-complete-20251102.tar.gz
cd insightlearn-wasm/
./deploy-oneclick.sh
# ... ripristina dati
```

---

## Troubleshooting

### Backup Failed

```bash
# Verifica spazio disco
df -h

# Verifica container running
docker ps | grep insightlearn

# Verifica password in .env
cat .env | grep PASSWORD
```

### Restore Failed

```bash
# SQL Server restore error
docker logs insightlearn-sqlserver --tail=100

# MongoDB restore error
docker exec insightlearn-mongodb mongosh --eval "db.serverStatus()"

# Ripeti restore
cd backups/data_*/
./restore-data.sh
```

### Dati Mancanti

```bash
# Verifica integrità backup
cd backups/data_*/
ls -lh
cat backup-info.txt

# Verifica database dopo restore
docker exec insightlearn-sqlserver sqlcmd -S localhost -U sa -P PASSWORD -C -Q "SELECT name FROM sys.databases"
```

---

## Documentazione Correlata

- **[backup-data.sh](/backup-data.sh)** - Script backup dati
- **[DEPLOYMENT-COMPLETE-GUIDE.md](/DEPLOYMENT-COMPLETE-GUIDE.md)** - Deployment completo
- **[deploy-oneclick.sh](/deploy-oneclick.sh)** - Deploy automatico
- **[DOCKER-COMPOSE-GUIDE.md](/DOCKER-COMPOSE-GUIDE.md)** - Guida Docker Compose

---

**Con questa procedura hai una fotografia speculare COMPLETA del sito con TUTTI i dati!** 📸✅
