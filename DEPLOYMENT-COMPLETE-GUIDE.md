# Guida Completa al Deployment - InsightLearn WASM

## Obiettivo

Questa guida garantisce che **frontend e backend siano completamente funzionanti** dopo il ripristino su una nuova piattaforma.

---

## Prerequisiti

### Software Richiesto

```bash
# Docker e Docker Compose
docker --version          # >= 24.0
docker-compose --version  # >= 2.20

# .NET SDK (per sviluppo)
dotnet --version          # >= 8.0

# Git
git --version             # >= 2.40

# Kubernetes (opzionale, per deployment k8s)
kubectl version --client  # >= 1.28
minikube version          # >= 1.32
```

### Requisiti Hardware

**Minimi:**
- CPU: 4 cores
- RAM: 16 GB
- Disk: 50 GB liberi
- Rete: 10 Mbps

**Raccomandati:**
- CPU: 8+ cores
- RAM: 32 GB
- Disk: 100 GB SSD
- Rete: 100 Mbps

---

## Metodi di Ripristino

### Metodo 1: Clone da GitHub (Raccomandato)

```bash
# 1. Clone repository
git clone https://github.com/marypas74/InsightLearn_WASM.git
cd InsightLearn_WASM

# 2. Verifica file
ls -la
# Dovrebbe mostrare: docker-compose.yml, src/, k8s/, monitoring/, etc.

# 3. Verifica submodules (se presenti)
git submodule update --init --recursive
```

### Metodo 2: Da Archivio Tar.gz

```bash
# 1. Estrai archivio
tar xzf insightlearn-wasm-clean-v1.4.29.tar.gz
cd insightlearn-wasm/

# 2. Verifica contenuto
ls -la

# 3. Inizializza Git (opzionale)
git init
git remote add origin https://github.com/marypas74/InsightLearn_WASM.git
```

### Metodo 3: Da Git Bundle

```bash
# 1. Clone da bundle
git clone insightlearn-wasm-clean.bundle insightlearn-wasm
cd insightlearn-wasm/

# 2. Aggiungi remote GitHub
git remote add origin https://github.com/marypas74/InsightLearn_WASM.git
git fetch origin
```

---

## Configurazione Passo-Passo

### Step 1: Configurare i Segreti

#### 1.1 Creare File `.env`

```bash
# Crea file .env nella root del progetto
cat > .env << 'EOF'
# Database Passwords
DB_PASSWORD=InsightLearn2024!SecureDB
MONGO_PASSWORD=InsightLearn2024!SecureMongo
REDIS_PASSWORD=InsightLearn2024!SecureRedis

# JWT Authentication
JWT_SECRET_KEY=InsightLearn2024SecureJwtSigningKeyMinimum32Characters!

# Admin Credentials
ADMIN_PASSWORD=Admin123!Secure

# Encryption Keys
ENCRYPTION_MASTER_KEY=InsightLearn2024PaymentEncryptionMasterKey32Chars!
VIDEO_ENCRYPTION_KEY=InsightLearn2024VideoEncryptionKey!

# Google OAuth (opzionale - sostituisci con valori reali)
GOOGLE_CLIENT_ID=YOUR_GOOGLE_CLIENT_ID_HERE.apps.googleusercontent.com
GOOGLE_CLIENT_SECRET=YOUR_GOOGLE_CLIENT_SECRET_HERE

# Stripe (opzionale - sostituisci con valori reali)
STRIPE_PUBLISHABLE_KEY=pk_test_your_stripe_publishable_key
STRIPE_SECRET_KEY=sk_test_your_stripe_secret_key
EOF

# Proteggi il file
chmod 600 .env
```

#### 1.2 Aggiornare `docker-compose.yml`

Sostituire **TUTTI** i placeholder `YOUR_*` con variabili d'ambiente:

```bash
# Backup originale
cp docker-compose.yml docker-compose.yml.backup

# Sostituzioni automatiche
sed -i 's/YOUR_DB_PASSWORD/${DB_PASSWORD}/g' docker-compose.yml
sed -i 's/YOUR_MONGO_PASSWORD/${MONGO_PASSWORD}/g' docker-compose.yml
sed -i 's/YOUR_REDIS_PASSWORD/${REDIS_PASSWORD}/g' docker-compose.yml
sed -i 's/YOUR_JWT_SECRET_KEY_HERE_MIN_32_CHARS/${JWT_SECRET_KEY}/g' docker-compose.yml
sed -i 's/YOUR_ADMIN_PASSWORD/${ADMIN_PASSWORD}/g' docker-compose.yml
sed -i 's/YOUR_ENCRYPTION_MASTER_KEY_HERE_MIN_32_CHARS/${ENCRYPTION_MASTER_KEY}/g' docker-compose.yml
sed -i 's/YOUR_VIDEO_ENCRYPTION_KEY_HERE/${VIDEO_ENCRYPTION_KEY}/g' docker-compose.yml
sed -i 's/YOUR_GOOGLE_CLIENT_ID_HERE/${GOOGLE_CLIENT_ID}/g' docker-compose.yml
sed -i 's/YOUR_GOOGLE_CLIENT_SECRET_HERE/${GOOGLE_CLIENT_SECRET}/g' docker-compose.yml
```

#### 1.3 Aggiornare `config/appsettings.json`

```bash
# Crea appsettings.Production.json con valori reali
cat > config/appsettings.Production.json << 'EOF'
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=sqlserver;Database=InsightLearnDb;User=sa;Password=${DB_PASSWORD};TrustServerCertificate=true;MultipleActiveResultSets=true",
    "MongoDb": "mongodb://admin:${MONGO_PASSWORD}@mongodb:27017/insightlearn?authSource=admin"
  },
  "Redis": {
    "ConnectionString": "redis:6379,password=${REDIS_PASSWORD}"
  },
  "JwtSettings": {
    "SecretKey": "${JWT_SECRET_KEY}",
    "Issuer": "InsightLearn.Api",
    "Audience": "InsightLearn.Users",
    "ExpirationDays": 30,
    "RefreshTokenExpirationDays": 90
  },
  "DefaultAdmin": {
    "Email": "admin@insightlearn.cloud",
    "Password": "${ADMIN_PASSWORD}",
    "FirstName": "Admin",
    "LastName": "User"
  },
  "Encryption": {
    "MasterKey": "${ENCRYPTION_MASTER_KEY}",
    "CurrentKeyId": "key_default_20240929"
  },
  "Video": {
    "EncryptionKey": "${VIDEO_ENCRYPTION_KEY}",
    "StreamingBaseUrl": "/api/video/stream/"
  },
  "GoogleAuth": {
    "ClientId": "${GOOGLE_CLIENT_ID}",
    "ClientSecret": "${GOOGLE_CLIENT_SECRET}"
  },
  "Elasticsearch": {
    "Url": "http://elasticsearch:9200"
  },
  "Ollama": {
    "Url": "http://ollama:11434",
    "Model": "llama2"
  }
}
EOF
```

### Step 2: Certificati SSL (per HTTPS)

#### 2.1 Creare Certificati Self-Signed (Sviluppo)

```bash
# Crea directory per certificati
mkdir -p nginx/certs

# Genera certificato self-signed
openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
  -keyout nginx/certs/tls.key \
  -out nginx/certs/tls.crt \
  -subj "/C=IT/ST=Italy/L=Milan/O=InsightLearn/CN=localhost"

# Permessi corretti
chmod 600 nginx/certs/tls.key
chmod 644 nginx/certs/tls.crt
```

#### 2.2 Certificati Produzione (Let's Encrypt)

```bash
# Installa Certbot
sudo apt-get install certbot

# Genera certificato per dominio reale
sudo certbot certonly --standalone -d yourdomain.com -d www.yourdomain.com

# Copia certificati
sudo cp /etc/letsencrypt/live/yourdomain.com/fullchain.pem nginx/certs/tls.crt
sudo cp /etc/letsencrypt/live/yourdomain.com/privkey.pem nginx/certs/tls.key
```

### Step 3: Build e Avvio Stack Completo

#### 3.1 Verifica Configurazione

```bash
# Valida docker-compose.yml
docker-compose config

# Verifica che .env sia caricato
docker-compose config | grep DB_PASSWORD
# Dovrebbe mostrare il valore reale, non ${DB_PASSWORD}
```

#### 3.2 Build Immagini

```bash
# Build tutte le immagini
docker-compose build --no-cache

# Verifica immagini create
docker images | grep insightlearn
```

#### 3.3 Avvio Servizi (Ordine Corretto)

```bash
# 1. Avvia database layer
docker-compose up -d sqlserver redis mongodb elasticsearch

# Attendi che siano healthy
docker-compose ps | grep healthy

# 2. Avvia AI/LLM
docker-compose up -d ollama

# 3. Avvia monitoring
docker-compose up -d prometheus

# 4. Avvia application layer
docker-compose up -d api web

# Attendi che API e Web siano healthy
docker-compose ps api web

# 5. Avvia reverse proxy
docker-compose up -d nginx

# 6. Avvia monitoring dashboards
docker-compose up -d grafana

# 7. Avvia CI/CD (opzionale)
docker-compose up -d jenkins

# Verifica tutto
docker-compose ps
```

### Step 4: Inizializzazione Database

#### 4.1 SQL Server - Creazione Database

```bash
# Attendi che SQL Server sia pronto (può richiedere 60+ secondi)
sleep 60

# Verifica connessione
docker exec -it insightlearn-sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "${DB_PASSWORD}" -C -Q "SELECT @@VERSION"

# Se serve, applica migrations (se presente script)
# docker exec -it insightlearn-api dotnet ef database update
```

#### 4.2 MongoDB - Creazione Collections

```bash
# Accedi a MongoDB
docker exec -it insightlearn-mongodb mongosh \
  -u admin -p "${MONGO_PASSWORD}" --authenticationDatabase admin

# Crea database e collections
use insightlearn
db.createCollection("videos")
db.createCollection("chatbot_contacts")
db.createCollection("chatbot_messages")
db.createCollection("video_metadata")

# Verifica
show collections
exit
```

#### 4.3 Ollama - Download Modello LLM

```bash
# Download modello llama2 (richiede ~4GB, 5-10 minuti)
docker exec -it insightlearn-ollama ollama pull llama2

# Verifica modello installato
docker exec -it insightlearn-ollama ollama list

# Test veloce
docker exec -it insightlearn-ollama ollama run llama2 "Hello!"
```

### Step 5: Verifica Funzionamento

#### 5.1 Health Checks Automatici

```bash
# Script di verifica completo
cat > verify-deployment.sh << 'EOF'
#!/bin/bash

echo "=== InsightLearn Deployment Verification ==="
echo ""

# Funzione per test endpoint
test_endpoint() {
  local name=$1
  local url=$2
  local expected=$3

  echo -n "Testing $name... "
  response=$(curl -sk -o /dev/null -w "%{http_code}" "$url" 2>/dev/null)

  if [ "$response" = "$expected" ]; then
    echo "✅ OK ($response)"
    return 0
  else
    echo "❌ FAIL (expected $expected, got $response)"
    return 1
  fi
}

# Database Layer
echo "--- Database Layer ---"
docker exec insightlearn-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "${DB_PASSWORD}" -C -Q "SELECT 1" > /dev/null 2>&1 && echo "✅ SQL Server OK" || echo "❌ SQL Server FAIL"
docker exec insightlearn-redis redis-cli -a "${REDIS_PASSWORD}" --no-auth-warning ping > /dev/null 2>&1 && echo "✅ Redis OK" || echo "❌ Redis FAIL"
docker exec insightlearn-mongodb mongosh -u admin -p "${MONGO_PASSWORD}" --quiet --eval "db.adminCommand('ping')" > /dev/null 2>&1 && echo "✅ MongoDB OK" || echo "❌ MongoDB FAIL"
test_endpoint "Elasticsearch" "http://localhost:9200" "200"

echo ""
echo "--- Application Layer ---"
test_endpoint "API Health" "http://localhost:7001/health" "200"
test_endpoint "Web Application" "http://localhost:7003" "200"
test_endpoint "Nginx HTTP" "http://localhost" "200"
test_endpoint "Nginx HTTPS" "https://localhost" "200"

echo ""
echo "--- AI/LLM Layer ---"
test_endpoint "Ollama API" "http://localhost:11434/api/tags" "200"

echo ""
echo "--- Monitoring Layer ---"
test_endpoint "Prometheus" "http://localhost:9090/-/healthy" "200"
test_endpoint "Grafana" "http://localhost:3000/api/health" "200"

echo ""
echo "--- CI/CD Layer ---"
test_endpoint "Jenkins" "http://localhost:8080/login" "200"

echo ""
echo "=== Container Status ==="
docker-compose ps

echo ""
echo "=== Verification Complete ==="
EOF

chmod +x verify-deployment.sh
./verify-deployment.sh
```

#### 5.2 Test Manuali

```bash
# 1. Test API diretta
curl -k http://localhost:7001/health
# Expected: {"status":"Healthy"}

# 2. Test Web diretta
curl -k http://localhost:7003
# Expected: HTML response

# 3. Test Nginx HTTPS
curl -k https://localhost/health
# Expected: {"status":"Healthy"}

# 4. Test Grafana login
curl -k http://localhost:3000/api/health
# Expected: {"commit":"...","database":"ok","version":"..."}

# 5. Test Prometheus targets
curl -s http://localhost:9090/api/v1/targets | jq '.data.activeTargets[] | {job: .labels.job, health: .health}'
# Expected: Lista di target con health="up"

# 6. Test Ollama
curl -s http://localhost:11434/api/tags | jq '.models[].name'
# Expected: ["llama2"]
```

#### 5.3 Test Funzionali End-to-End

```bash
# Test 1: Registrazione Nuovo Utente
curl -k -X POST https://localhost/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Test123!",
    "firstName": "Test",
    "lastName": "User"
  }'
# Expected: JSON con token

# Test 2: Login
curl -k -X POST https://localhost/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@insightlearn.cloud",
    "password": "'"${ADMIN_PASSWORD}"'"
  }'
# Expected: JSON con token e user info

# Test 3: Chatbot (Ollama integration)
TOKEN=$(curl -sk -X POST https://localhost/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@insightlearn.cloud","password":"'"${ADMIN_PASSWORD}"'"}' \
  | jq -r '.token')

curl -k -X POST https://localhost/api/chat/message \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"message":"Hello, how are you?"}'
# Expected: AI response from Ollama
```

### Step 6: Accesso alle Interfacce

#### 6.1 Applicazione Principale

- **URL**: https://localhost
- **Login Admin**:
  - Email: `admin@insightlearn.cloud`
  - Password: (valore di `${ADMIN_PASSWORD}` dal `.env`)

#### 6.2 Grafana Dashboards

- **URL**: http://localhost:3000
- **Login**:
  - Username: `admin`
  - Password: `admin`
- **Dashboard**:
  - InsightLearn Dashboard (home page)
  - InsightLearn App Metrics
  - Grafana Dashboard Fixed

#### 6.3 Prometheus

- **URL**: http://localhost:9090
- **Targets**: http://localhost:9090/targets
- **Graph**: http://localhost:9090/graph

#### 6.4 Jenkins

- **URL**: http://localhost:8080
- **Initial Password**:
  ```bash
  docker exec insightlearn-jenkins cat /var/jenkins_home/secrets/initialAdminPassword
  ```

#### 6.5 API Swagger (se abilitato)

- **URL**: http://localhost:7001/swagger
- **Documentazione**: Tutte le API disponibili

---

## Troubleshooting

### Problema: Container non si avvia

```bash
# Verifica log
docker-compose logs [service-name]

# Verifica risorse
docker stats

# Rebuild specifico
docker-compose build --no-cache [service-name]
docker-compose up -d [service-name]
```

### Problema: Database non accessibile

```bash
# SQL Server
docker exec -it insightlearn-sqlserver /bin/bash
/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "${DB_PASSWORD}" -C

# MongoDB
docker exec -it insightlearn-mongodb mongosh -u admin -p "${MONGO_PASSWORD}"

# Redis
docker exec -it insightlearn-redis redis-cli -a "${REDIS_PASSWORD}"
```

### Problema: Ollama non risponde

```bash
# Verifica modelli installati
docker exec insightlearn-ollama ollama list

# Re-pull modello
docker exec insightlearn-ollama ollama pull llama2

# Verifica log
docker logs insightlearn-ollama -f
```

### Problema: Grafana dashboard vuote

```bash
# Verifica provisioning
docker exec insightlearn-grafana ls -la /etc/grafana/provisioning/dashboards/

# Verifica datasource Prometheus
curl -s http://localhost:3000/api/datasources -u admin:admin | jq '.'

# Restart Grafana
docker-compose restart grafana
```

### Problema: Certificati SSL

```bash
# Rigenera certificati
rm -rf nginx/certs/*
openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
  -keyout nginx/certs/tls.key \
  -out nginx/certs/tls.crt \
  -subj "/C=IT/ST=Italy/L=Milan/O=InsightLearn/CN=localhost"

# Restart Nginx
docker-compose restart nginx
```

---

## Backup e Restore

### Backup Completo

```bash
# Script di backup automatico
cat > backup.sh << 'EOF'
#!/bin/bash

BACKUP_DIR="./backups/$(date +%Y%m%d_%H%M%S)"
mkdir -p "$BACKUP_DIR"

echo "Backing up to $BACKUP_DIR..."

# Backup volumi Docker
for volume in sqlserver-data mongodb-data redis-data elasticsearch-data grafana-data jenkins-data ollama-data; do
  echo "Backing up $volume..."
  docker run --rm -v insightlearn-wasm_$volume:/data -v $BACKUP_DIR:/backup \
    ubuntu tar czf /backup/$volume.tar.gz /data
done

# Backup configurazioni
cp .env "$BACKUP_DIR/"
cp docker-compose.yml "$BACKUP_DIR/"
cp -r config "$BACKUP_DIR/"
cp -r nginx "$BACKUP_DIR/"
cp -r monitoring "$BACKUP_DIR/"

echo "Backup completed: $BACKUP_DIR"
EOF

chmod +x backup.sh
./backup.sh
```

### Restore da Backup

```bash
# Script di restore
cat > restore.sh << 'EOF'
#!/bin/bash

BACKUP_DIR=$1

if [ -z "$BACKUP_DIR" ]; then
  echo "Usage: $0 <backup_directory>"
  exit 1
fi

echo "Restoring from $BACKUP_DIR..."

# Stop servizi
docker-compose down

# Restore volumi
for volume in sqlserver-data mongodb-data redis-data elasticsearch-data grafana-data jenkins-data ollama-data; do
  echo "Restoring $volume..."
  docker volume rm insightlearn-wasm_$volume 2>/dev/null
  docker volume create insightlearn-wasm_$volume
  docker run --rm -v insightlearn-wasm_$volume:/data -v $BACKUP_DIR:/backup \
    ubuntu tar xzf /backup/$volume.tar.gz -C /
done

# Restore configurazioni
cp "$BACKUP_DIR/.env" ./
cp "$BACKUP_DIR/docker-compose.yml" ./
cp -r "$BACKUP_DIR/config" ./
cp -r "$BACKUP_DIR/nginx" ./
cp -r "$BACKUP_DIR/monitoring" ./

# Restart servizi
docker-compose up -d

echo "Restore completed!"
EOF

chmod +x restore.sh
# Uso: ./restore.sh ./backups/20251102_151200
```

---

## Deployment Kubernetes (Alternativo)

Per deployment su Kubernetes invece di Docker Compose:

```bash
# Vedi guida completa
cat k8s/README.md

# Quick start
cd k8s/
./build-images.sh
./deploy.sh
./status.sh
```

---

## Checklist Finale di Verifica

Prima di considerare il deployment completato, verificare:

- [ ] Tutti i container sono in stato `healthy`
- [ ] SQL Server accetta connessioni
- [ ] MongoDB accetta connessioni
- [ ] Redis accetta connessioni
- [ ] Elasticsearch è raggiungibile
- [ ] Ollama ha il modello llama2 caricato
- [ ] API `/health` risponde 200
- [ ] Web application carica correttamente
- [ ] Nginx serve HTTPS correttamente
- [ ] Grafana mostra le 3 dashboard
- [ ] Prometheus raccoglie metriche da tutti i servizi
- [ ] Login admin funziona
- [ ] Registrazione nuovo utente funziona
- [ ] Chatbot (Ollama) risponde
- [ ] Jenkins è accessibile (se necessario)
- [ ] Backup script funziona

---

## Supporto

### Documentazione

- [README.md](README.md) - Panoramica progetto
- [CLAUDE.md](CLAUDE.md) - Guida Claude Code
- [DOCKER-COMPOSE-GUIDE.md](DOCKER-COMPOSE-GUIDE.md) - Guida Docker Compose
- [GITHUB-PUSH-SUCCESS.md](GITHUB-PUSH-SUCCESS.md) - Report push GitHub
- [k8s/README.md](k8s/README.md) - Guida Kubernetes

### Link Utili

- **Repository**: https://github.com/marypas74/InsightLearn_WASM
- **Issues**: https://github.com/marypas74/InsightLearn_WASM/issues

### Contatti

- **Autore**: marypas74
- **Email**: marcello.pasqui@gmail.com

---

**Deployment completato con successo!** 🎉
