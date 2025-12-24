# K3s Disaster Recovery System - Quick Start

Sistema automatico di backup e restore per cluster K3s InsightLearn **non presidiato**.

## 🚀 Quick Install

```bash
cd /home/mpasqui/insightlearn_WASM/InsightLearn_WASM/k8s
sudo ./install-disaster-recovery.sh
```

**Questo installa**:
- ✅ Backup orario automatico (cron job @ :05)
- ✅ Restore automatico al boot (systemd service)
- ✅ Cloudflare Tunnel auto-restart
- ✅ Logging completo

---

## 📊 Check Status

```bash
sudo ./k8s/check-disaster-recovery-status.sh
```

**Output esempio**:
```
✓ Latest backup: 87M (2025-11-09 14:05:23)
✓ Auto-restore service: enabled
✓ Cloudflared service: running
✓ External access: OK (https://www.insightlearn.cloud)
```

---

## 🔧 Manual Operations

```bash
# Backup manuale
sudo ./k8s/backup-cluster-state.sh

# Test restore (⚠️ sovrascrive cluster!)
sudo ./k8s/restore-cluster-state.sh

# Check Cloudflare tunnel
sudo ./k8s/verify-cloudflare-tunnel.sh

# View logs
sudo tail -f /var/log/k3s-backup.log
sudo tail -f /var/log/k3s-restore.log
sudo journalctl -u cloudflared-tunnel.service -f
```

---

## 📁 File Locations

| File | Purpose |
|------|---------|
| [backup-cluster-state.sh](backup-cluster-state.sh) | Backup script (runs hourly) |
| [restore-cluster-state.sh](restore-cluster-state.sh) | Restore script (runs at boot) |
| [verify-cloudflare-tunnel.sh](verify-cloudflare-tunnel.sh) | Cloudflare tunnel check |
| [k3s-auto-restore.service](k3s-auto-restore.service) | Systemd service for restore |
| [cloudflared-tunnel.service](cloudflared-tunnel.service) | Systemd service for Cloudflare |
| `/var/backups/k3s-cluster/k3s-cluster-snapshot.tar.gz` | Latest backup (50-100MB) |
| `/var/log/k3s-backup.log` | Backup logs |
| `/var/log/k3s-restore.log` | Restore logs |

---

## 🆘 Troubleshooting

### Backup non viene creato

```bash
# Check cron service
systemctl status crond

# Run manually per vedere errori
sudo ./k8s/backup-cluster-state.sh

# Check logs
sudo tail -50 /var/log/k3s-backup.log
```

### Restore non si attiva al boot

```bash
# Check service enabled
systemctl is-enabled k3s-auto-restore.service

# Check logs
sudo journalctl -u k3s-auto-restore.service -n 50

# Run manually
sudo ./k8s/restore-cluster-state.sh
```

### Cloudflare tunnel down dopo restore

```bash
# Check service
systemctl status cloudflared-tunnel.service

# Restart service
sudo systemctl restart cloudflared-tunnel.service

# Run verification script
sudo ./k8s/verify-cloudflare-tunnel.sh

# Check logs
sudo journalctl -u cloudflared-tunnel.service -f
```

---

## 📚 Full Documentation

Vedi [docs/DISASTER-RECOVERY-SYSTEM.md](../docs/DISASTER-RECOVERY-SYSTEM.md) per:
- Architettura completa
- Flussi di backup/restore
- Security considerations
- Performance & storage
- Testing procedures

---

## 🎯 Key Features

✅ **Zero-downtime recovery** - Cluster ripristinato in 2-5 minuti
✅ **Intelligent crash detection** - Skip restore se cluster healthy
✅ **Space-efficient** - Single snapshot overwrite (no disk bloat)
✅ **External access restored** - Cloudflare tunnel auto-restart
✅ **Complete logging** - Tutti gli eventi loggati per troubleshooting
✅ **Non-interactive** - Sistema completamente automatico

---

**Maintainer**: InsightLearn DevOps Team
**Version**: 1.0.0
**Date**: 2025-11-09
