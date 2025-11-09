# Guida Aumento Swap Space - InsightLearn Server

## 🚨 Problema Rilevato

Il server sta esaurendo la memoria swap, causando rallentamenti e crash di Jenkins:

```
Swap Usage: 5.9GB / 6GB (98% utilizzato)
RAM Usage: 13GB / 18GB (72% utilizzato)
```

## 🛠️ Soluzione: Aumentare Swap a 14GB Totali

### Metodo 1: Script Automatico (Raccomandato)

```bash
# Esegui come root
sudo bash /tmp/increase-swap.sh
```

Lo script:
1. Crea un file swap da 8GB (`/swapfile2`)
2. Lo attiva immediatamente
3. Lo rende persistente in `/etc/fstab`

**Risultato finale**: Swap totale = 6GB (esistente) + 8GB (nuovo) = **14GB**

### Metodo 2: Comandi Manuali

```bash
# 1. Crea file swap da 8GB
sudo dd if=/dev/zero of=/swapfile2 bs=1M count=8192 status=progress

# 2. Imposta permessi corretti (IMPORTANTE per sicurezza)
sudo chmod 600 /swapfile2

# 3. Formatta come swap
sudo mkswap /swapfile2

# 4. Attiva swap
sudo swapon /swapfile2

# 5. Verifica
sudo swapon --show
free -h

# 6. Rendi persistente (sopravvive ai reboot)
echo '/swapfile2 none swap sw 0 0' | sudo tee -a /etc/fstab
```

### Verifica Post-Installazione

```bash
# Controlla swap attivo
swapon --show
# Output atteso:
# NAME       TYPE      SIZE  USED PRIO
# /dev/dm-1  partition   6G  X.XG   -2
# /swapfile2 file        8G  X.XG   -2

# Controlla memoria totale
free -h
# Swap totale dovrebbe essere ~14GB
```

## 📊 Benefici Attesi

- ✅ **Jenkins più stabile**: Niente più crash per OOM (Out Of Memory)
- ✅ **Kubernetes più fluido**: minikube con 14GB RAM + 14GB swap
- ✅ **Build più veloci**: Meno swap thrashing
- ✅ **Sistema più responsive**: Più margine per picchi di carico

## 🔧 Ottimizzazioni Aggiuntive (Opzionali)

### Ridurre Swappiness (Preferire RAM a Swap)

```bash
# Visualizza valore corrente
cat /proc/sys/vm/swappiness
# Default: 60

# Imposta a 10 (usa swap solo quando necessario)
echo 'vm.swappiness=10' | sudo tee -a /etc/sysctl.conf
sudo sysctl -p

# Questo preferisce usare RAM fisica e ricorre a swap solo in emergenza
```

### Ottimizzare Cache Pressure

```bash
# Mantieni dentry e inode in cache più a lungo
echo 'vm.vfs_cache_pressure=50' | sudo tee -a /etc/sysctl.conf
sudo sysctl -p
```

## 📝 Monitoraggio Swap

### Comandi Utili

```bash
# Utilizzo swap in tempo reale
watch -n 5 'free -h'

# Processi che usano più swap
for file in /proc/*/status ; do awk '/VmSwap|Name/{printf $2 " " $3}END{ print ""}' $file; done | sort -k 2 -n -r | head -10

# Grafico utilizzo memoria (se installato)
vmstat 5
```

### Alert Script (Opzionale)

```bash
#!/bin/bash
# Salva come /usr/local/bin/swap-alert.sh

SWAP_THRESHOLD=90

SWAP_USAGE=$(free | grep Swap | awk '{printf("%.0f", ($3/$2) * 100)}')

if [ "$SWAP_USAGE" -gt "$SWAP_THRESHOLD" ]; then
    echo "⚠️ ALERT: Swap usage at ${SWAP_USAGE}%!"
    # Aggiungi qui invio email o notifica
fi
```

## ⚠️ Note Importanti

1. **Spazio Disco**: Assicurati di avere almeno 10GB liberi su `/` prima di creare swap
   ```bash
   df -h /
   # Attualmente: 16GB disponibili ✅
   ```

2. **Performance**: Swap su file è leggermente più lento di swap su partizione, ma per uso occasionale va benissimo

3. **SSD Wearout**: Se hai SSD, lo swap intensivo può ridurne la vita. Con 14GB di swap + 18GB RAM dovrebbe essere usato raramente

4. **Kubernetes**: minikube beneficia molto da swap aggiuntivo, specialmente con molti pod attivi

## 🐛 Troubleshooting

### "swapon failed: Device or resource busy"
```bash
# Disattiva swap esistente
sudo swapoff /swapfile2
# Rimuovi file
sudo rm /swapfile2
# Ricrea da capo
```

### "mkswap: error: /swapfile2 is mounted; will not make swapspace"
```bash
sudo swapoff /swapfile2
sudo mkswap /swapfile2
sudo swapon /swapfile2
```

### Swap non persiste dopo reboot
```bash
# Verifica /etc/fstab
cat /etc/fstab | grep swapfile2
# Se manca, aggiungi:
echo '/swapfile2 none swap sw 0 0' | sudo tee -a /etc/fstab
```

## 🎯 Prossimi Passi

Dopo aver aumentato lo swap:

1. ✅ Riavvia Jenkins: `kubectl rollout restart deployment jenkins -n jenkins`
2. ✅ Verifica stabilità: Monitora per 30 minuti con `watch -n 10 'free -h'`
3. ✅ Completa configurazione Jenkins jobs
4. ✅ Test load automatici

---

**File Script**: `/tmp/increase-swap.sh`
**Esecuzione**: `sudo bash /tmp/increase-swap.sh`
**Durata**: ~2-3 minuti (creazione file 8GB)

**Ultima revisione**: 2025-11-08
**Maintainer**: marcello.pasqui@gmail.com
