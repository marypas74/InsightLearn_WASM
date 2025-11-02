# Docker Compose Guide - InsightLearn Full Stack

## Overview

Questo file `docker-compose.yml` contiene lo **stack completo** di InsightLearn con tutti i servizi necessari per lo sviluppo locale.

## Componenti Inclusi

### ✅ Tutti i Servizi (11 totali)

1. **SQL Server 2022** - Database relazionale principale
2. **Redis 7** - Cache e session management
3. **Elasticsearch 8.11** - Search engine
4. **MongoDB 7.0** - NoSQL database per video storage
5. **API** - .NET 8 REST API backend
6. **Web** - Blazor Server frontend
7. **Nginx** - Reverse proxy HTTPS
8. **Grafana 10.2** - Dashboard di monitoraggio
9. **Prometheus 2.48** - Metrics collection
10. **Jenkins LTS** - CI/CD automation
11. **Ollama** - Local LLM server (AI features)

---

## Porte Esposte

| Servizio | Porta | Descrizione |
|----------|-------|-------------|
| Nginx | 80 | HTTP (redirect to HTTPS) |
| Nginx | 443 | HTTPS (main entry point) |
| SQL Server | 1433 | Database connections |
| Redis | 6379 | Cache connections |
| Elasticsearch | 9200 | Search API |
| MongoDB | 27017 | NoSQL database |
| API | 7001 | API HTTP |
| API | 7002 | API HTTPS |
| Web | 7003 | Web HTTP |
| Grafana | 3000 | Monitoring dashboards |
| Prometheus | 9090 | Metrics API |
| Jenkins | 8080 | CI/CD web interface |
| Jenkins | 50000 | Jenkins agent port |
| Ollama | 11434 | LLM API |

---

## Configurazione Segreti

Prima di avviare i container, configurare i seguenti segreti:

### 1. File `.env`

Creare un file `.env` nella root del progetto:

```bash
# Database Passwords
DB_PASSWORD=YourSecureDBPassword123!
MONGO_PASSWORD=YourSecureMongoPassword123!
REDIS_PASSWORD=YourSecureRedisPassword123!

# JWT Authentication
JWT_SECRET_KEY=YourJwtSigningKeyMinimum32CharactersLong!

# Admin Credentials
ADMIN_PASSWORD=YourAdminPassword123!

# Encryption Keys
ENCRYPTION_MASTER_KEY=YourEncryptionMasterKeyMin32Chars!
VIDEO_ENCRYPTION_KEY=YourVideoEncryptionKey123!

# Google OAuth (optional)
GOOGLE_CLIENT_ID=your-client-id.apps.googleusercontent.com
GOOGLE_CLIENT_SECRET=GOCSPX-YourClientSecret
```

### 2. Aggiornare docker-compose.yml

Sostituire i placeholder con variabili d'ambiente:

```yaml
# Esempio per SQL Server
MSSQL_SA_PASSWORD: "${DB_PASSWORD}"

# Esempio per MongoDB
MONGO_INITDB_ROOT_PASSWORD: "${MONGO_PASSWORD}"

# Esempio per API
- ConnectionStrings__DefaultConnection=Server=sqlserver;Database=InsightLearnDb;User=sa;Password=${DB_PASSWORD};...
- JwtSettings__SecretKey=${JWT_SECRET_KEY}
```

---

## Avvio dello Stack

### 1. Build e Avvio Completo

```bash
# Build di tutte le immagini
docker-compose build

# Avvio di tutti i servizi
docker-compose up -d

# Visualizza i log
docker-compose logs -f
```

### 2. Avvio Servizi Specifici

```bash
# Solo database
docker-compose up -d sqlserver redis mongodb elasticsearch

# Solo applicazione
docker-compose up -d api web nginx

# Solo monitoring
docker-compose up -d prometheus grafana

# Solo CI/CD
docker-compose up -d jenkins
```

### 3. Verifica Stato

```bash
# Stato di tutti i container
docker-compose ps

# Health check
docker-compose ps | grep healthy

# Log di un servizio specifico
docker-compose logs -f api
docker-compose logs -f web
```

---

## Ordine di Avvio

I servizi si avviano automaticamente nell'ordine corretto grazie a `depends_on`:

1. **SQL Server** (foundational database)
2. **Redis** (cache)
3. **Elasticsearch** (search)
4. **MongoDB** (NoSQL)
5. **Ollama** (LLM)
6. **Prometheus** (metrics collection)
7. **API** (backend, depends on databases)
8. **Web** (frontend, depends on API)
9. **Nginx** (reverse proxy, depends on API + Web)
10. **Grafana** (monitoring, depends on Prometheus)
11. **Jenkins** (CI/CD, independent)

---

## Configurazione Post-Avvio

### 1. SQL Server - Inizializzazione Database

```bash
# Accesso al container SQL Server
docker exec -it insightlearn-sqlserver /bin/bash

# Connessione SQL
/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'YOUR_DB_PASSWORD' -C

# Verifica database
SELECT name FROM sys.databases;
GO
```

### 2. MongoDB - Creazione Database

```bash
# Accesso al container MongoDB
docker exec -it insightlearn-mongodb mongosh -u admin -p YOUR_MONGO_PASSWORD

# Creazione database e collection
use insightlearn
db.createCollection("videos")
db.createCollection("chatbot_messages")
show collections
```

### 3. Ollama - Download Modello LLM

```bash
# Accesso al container Ollama
docker exec -it insightlearn-ollama ollama pull llama2

# Verifica modelli installati
docker exec -it insightlearn-ollama ollama list

# Test LLM
docker exec -it insightlearn-ollama ollama run llama2 "Hello, how are you?"
```

### 4. Grafana - Accesso Dashboard

1. Aprire browser: http://localhost:3000
2. Login: `admin` / `admin`
3. Le dashboard sono auto-configurate in `/etc/grafana/provisioning/dashboards/`
4. Datasource Prometheus già configurato

### 5. Jenkins - Setup Iniziale

1. Aprire browser: http://localhost:8080
2. Ottenere password iniziale:
   ```bash
   docker exec insightlearn-jenkins cat /var/jenkins_home/secrets/initialAdminPassword
   ```
3. Installare plugin suggeriti
4. Creare admin user

---

## Volumi Persistenti

Tutti i dati sono persistiti in volumi Docker:

```bash
# Lista volumi
docker volume ls | grep insightlearn

# Backup di un volume
docker run --rm -v insightlearn-wasm_sqlserver-data:/data -v $(pwd):/backup ubuntu tar czf /backup/sqlserver-backup.tar.gz /data

# Restore di un volume
docker run --rm -v insightlearn-wasm_sqlserver-data:/data -v $(pwd):/backup ubuntu tar xzf /backup/sqlserver-backup.tar.gz -C /
```

### Volumi Creati

- `sqlserver-data` - SQL Server database
- `mongodb-data` - MongoDB database
- `mongodb-config` - MongoDB config
- `redis-data` - Redis cache
- `elasticsearch-data` - Elasticsearch indices
- `grafana-data` - Grafana dashboards e config
- `prometheus-data` - Prometheus metrics
- `jenkins-data` - Jenkins jobs e config
- `ollama-data` - Ollama models
- `api-files`, `api-logs`, `api-temp`, `api-sessions`
- `web-logs`, `web-temp`, `web-sessions`
- `nginx-logs`
- `dataprotection-keys` - .NET Data Protection

---

## Monitoraggio e Debugging

### 1. Prometheus Metrics

```bash
# Verifica targets
curl http://localhost:9090/api/v1/targets

# Query metrics
curl 'http://localhost:9090/api/v1/query?query=up'
```

### 2. Grafana Dashboards

Le seguenti dashboard sono pre-configurate:

1. **insightlearn-dashboard.json** - Dashboard principale
2. **insightlearn-app-metrics.json** - Metriche applicazione
3. **grafana-dashboard-fixed.json** - Dashboard fissa

### 3. Health Checks

```bash
# API health
curl http://localhost:7001/health

# Web health
curl http://localhost:7003/

# Nginx health
curl http://localhost/health

# Grafana health
curl http://localhost:3000/api/health

# Prometheus health
curl http://localhost:9090/-/healthy
```

---

## Troubleshooting

### Container non si avvia

```bash
# Verifica log
docker-compose logs [service-name]

# Rebuild forzato
docker-compose build --no-cache [service-name]
docker-compose up -d [service-name]
```

### Problemi di connessione

```bash
# Verifica network
docker network inspect insightlearn-wasm_insightlearn-network

# Test connettività tra container
docker exec insightlearn-api ping mongodb
docker exec insightlearn-web ping api
```

### Pulizia completa

```bash
# Stop tutti i container
docker-compose down

# Rimozione volumi (ATTENZIONE: cancella tutti i dati!)
docker-compose down -v

# Rimozione immagini
docker-compose down --rmi all

# Pulizia completa
docker system prune -a --volumes
```

---

## Requisiti di Sistema

### Minimi

- CPU: 4 cores
- RAM: 16 GB
- Disk: 50 GB free space
- Docker: 24.0+
- Docker Compose: 2.20+

### Raccomandati

- CPU: 8+ cores
- RAM: 32 GB
- Disk: 100 GB SSD
- Docker: latest
- Docker Compose: latest

---

## Configurazione Produzione

Per deployment in produzione:

1. **Cambiare tutti i segreti** con valori sicuri
2. **Usare certificati SSL validi** (non self-signed)
3. **Abilitare autenticazione** su tutti i servizi
4. **Configurare backup automatici** dei volumi
5. **Usare Docker secrets** invece di variabili d'ambiente
6. **Limitare resource limits** appropriatamente
7. **Configurare logging centralizzato**
8. **Abilitare monitoring alerts**
9. **Implementare rate limiting** su Nginx
10. **Usare private Docker registry**

---

## Link Utili

- **Application**: https://localhost (Nginx HTTPS)
- **API Docs**: http://localhost:7001/swagger
- **Grafana**: http://localhost:3000 (admin/admin)
- **Prometheus**: http://localhost:9090
- **Jenkins**: http://localhost:8080
- **Ollama**: http://localhost:11434

---

## Support

Per problemi o domande:
- **GitHub Issues**: https://github.com/marypas74/InsightLearn_WASM/issues
- **Documentation**: [README.md](README.md), [CLAUDE.md](CLAUDE.md)
- **Kubernetes Guide**: [k8s/README.md](k8s/README.md)
