# Piano Stress Test - 100 Video TEST FREE

## Obiettivo
Caricare 100 video di test su MongoDB GridFS e creare corsi FREE con tag TEST per stress testing.

## Configurazione
- **Video totali**: 100
- **Dimensione file**: 10MB ciascuno
- **Spazio disco**: ~1GB (sicuro)
- **Corsi**: 10 corsi × 10 lezioni = 100 video
- **Tag**: TEST, FREE, STRESS-TEST, AUTOMATED
- **Upload paralleli**: 20 simultanei

## Piano di Esecuzione

### Step 1: Autenticazione ✅
- Login con admin@insightlearn.cloud
- Recupero JWT token

### Step 2: Generazione File ✅
- 100 file da 10MB in /home/mpasqui/test-videos-temp/
- File binari random (simulano video)

### Step 3: Creazione Categoria
- Verifica/crea categoria "Stress Testing"

### Step 4: Creazione Corsi
- 10 corsi FREE con tag TEST
- Ogni corso con 1 sezione e 10 lezioni

### Step 5: Upload Video
- 100 upload su MongoDB GridFS
- 20 upload simultanei per stress

### Step 6: Pulizia
- Rimozione file temporanei
- Report finale

## Output Atteso
- 100 video in MongoDB GridFS
- 10 corsi visibili con filtro TEST
- Success rate > 90%

## Comandi
```bash
cd /home/mpasqui/insightlearn_WASM/InsightLearn_WASM/tests/load-testing
./create-100-test-videos.sh
```

## Monitoraggio
- Log: /tmp/create-100-test-videos.log
- MongoDB: `kubectl exec mongodb-0 -n insightlearn -- mongosh ... --eval "db.fs.files.countDocuments()"`
