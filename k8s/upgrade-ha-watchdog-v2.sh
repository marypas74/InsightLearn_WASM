#!/bin/bash
################################################################################
# Upgrade InsightLearn HA Watchdog da v1.0.0 a v2.0.0
#
# Nuove funzionalità v2.0.0:
# - Auto-restore automatico da backup
# - Verifica deployment/pod count
# - Loop di retry fino a successo
# - Intervallo ridotto: 2 minuti
#
# Usage: sudo ./upgrade-ha-watchdog-v2.sh
################################################################################

set -euo pipefail

# Check if running as root
if [[ $EUID -ne 0 ]]; then
   echo "ERROR: This script must be run as root"
   echo "Usage: sudo $0"
   exit 1
fi

echo "╔════════════════════════════════════════════════════════════╗"
echo "║    InsightLearn HA Watchdog Upgrade: v1.0.0 → v2.0.0      ║"
echo "╚════════════════════════════════════════════════════════════╝"
echo ""

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
BACKUP_DIR="/var/backups/ha-watchdog"

# Step 1: Backup versione corrente
echo "[1/7] Backup versione corrente..."
mkdir -p "$BACKUP_DIR"
cp /usr/local/bin/insightlearn-ha-watchdog.sh "$BACKUP_DIR/insightlearn-ha-watchdog.sh.v1.0.0.backup" 2>/dev/null || true
cp /etc/systemd/system/insightlearn-ha-watchdog.service "$BACKUP_DIR/insightlearn-ha-watchdog.service.v1.0.0.backup" 2>/dev/null || true
cp /etc/systemd/system/insightlearn-ha-watchdog.timer "$BACKUP_DIR/insightlearn-ha-watchdog.timer.v1.0.0.backup" 2>/dev/null || true
echo "  ✓ Backup salvato in: $BACKUP_DIR"
echo ""

# Step 2: Stop timer corrente
echo "[2/7] Stop watchdog corrente..."
systemctl stop insightlearn-ha-watchdog.timer || true
echo "  ✓ Timer fermato"
echo ""

# Step 3: Install nuovo watchdog script
echo "[3/7] Installazione watchdog v2.0.0..."
cp "$SCRIPT_DIR/insightlearn-ha-watchdog.sh" /usr/local/bin/insightlearn-ha-watchdog.sh
chmod +x /usr/local/bin/insightlearn-ha-watchdog.sh
echo "  ✓ Installato: /usr/local/bin/insightlearn-ha-watchdog.sh"
echo ""

# Step 4: Install nuovo systemd service
echo "[4/7] Installazione systemd service..."
cp "$SCRIPT_DIR/insightlearn-ha-watchdog.service" /etc/systemd/system/
echo "  ✓ Installato: /etc/systemd/system/insightlearn-ha-watchdog.service"
echo ""

# Step 5: Install nuovo systemd timer (2 min invece di 5 min)
echo "[5/7] Installazione systemd timer (intervallo: 2 min)..."
cp "$SCRIPT_DIR/insightlearn-ha-watchdog.timer" /etc/systemd/system/
echo "  ✓ Installato: /etc/systemd/system/insightlearn-ha-watchdog.timer"
echo ""

# Step 6: Reload systemd e restart timer
echo "[6/7] Reload systemd e restart timer..."
systemctl daemon-reload
systemctl enable insightlearn-ha-watchdog.timer
systemctl start insightlearn-ha-watchdog.timer
echo "  ✓ Timer riavviato"
echo ""

# Step 7: Verifica installazione
echo "[7/7] Verifica installazione..."
TIMER_STATUS=$(systemctl is-active insightlearn-ha-watchdog.timer)
echo "  Timer status: $TIMER_STATUS"

if [[ "$TIMER_STATUS" == "active" ]]; then
    echo "  ✅ Timer attivo"
else
    echo "  ❌ Timer non attivo!"
    exit 1
fi

echo ""
echo "╔════════════════════════════════════════════════════════════╗"
echo "║              UPGRADE COMPLETATO CON SUCCESSO!             ║"
echo "╚════════════════════════════════════════════════════════════╝"
echo ""
echo "Versione installata: 2.0.0"
echo "Intervallo watchdog: 2 minuti"
echo "Backup v1.0.0: $BACKUP_DIR"
echo ""
echo "Nuove funzionalità:"
echo "  ✅ Auto-restore automatico da backup"
echo "  ✅ Verifica deployment count (min: 5)"
echo "  ✅ Verifica pod count (min: 8)"
echo "  ✅ Loop di retry (5 tentativi)"
echo "  ✅ Intervallo ridotto: 2 min (da 5 min)"
echo ""
echo "Prossima esecuzione:"
systemctl list-timers insightlearn-ha-watchdog.timer --no-pager
echo ""
echo "Per testare immediatamente:"
echo "  sudo /home/mpasqui/insightlearn_WASM/InsightLearn_WASM/k8s/test-ha-watchdog.sh"
echo ""
echo "Log watchdog:"
echo "  tail -f /var/log/insightlearn-watchdog.log"
echo ""

exit 0
