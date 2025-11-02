# Seed Data - Dati di Esempio per Ripristino

Questa directory contiene i dati di esempio (seed data) che vengono automaticamente caricati durante il primo deployment.

## Contenuto

Se vuoi includere una **fotografia completa** del tuo sito con dati reali:

1. Esegui backup sul sistema sorgente:
   ```bash
   ./backup-data.sh
   ```

2. Copia i file di backup qui:
   ```bash
   cp backups/data_*/InsightLearnDb.bak seed-data/
   cp -r backups/data_*/mongodb_dump seed-data/
   cp backups/data_*/redis_dump.rdb seed-data/
   cp backups/data_*/api-files.tar.gz seed-data/
   ```

3. Commit e push:
   ```bash
   git add seed-data/
   git commit -m "feat: Add production data snapshot for deployment"
   git push
   ```

4. Sul nuovo sistema, questi dati saranno automaticamente ripristinati dal `deploy-oneclick.sh`

## File da Includere

- `InsightLearnDb.bak` - Database SQL Server completo
- `mongodb_dump/` - MongoDB collections dump
- `redis_dump.rdb` - Redis snapshot
- `api-files.tar.gz` - File caricati utenti (opzionale, può essere grande)

## Nota Sicurezza

⚠️ **ATTENZIONE**: Se includi dati production qui, assicurati che:
- Non ci siano dati sensibili (password, carte di credito, etc.)
- Il repository sia privato su GitHub
- I backup siano cifrati se necessario

Per dati production sensibili, usa invece il metodo manuale con `backups/` (non committato).
