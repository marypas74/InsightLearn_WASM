#!/bin/bash
################################################################################
# Fix and Test Restore - Wrapper Script
#
# Purpose: Fix restore issues and test the system
# Runs with sudo automatically
################################################################################

set -euo pipefail

# Check if running as root
if [[ $EUID -ne 0 ]]; then
   echo "Questo script richiede privilegi root."
   echo "Per eseguirlo: sudo $0"
   exit 1
fi

echo "=========================================="
echo "Fix Restore & Test - InsightLearn"
echo "=========================================="
echo ""

# Step 1: Run backup to create symlink
echo "[1/5] Esecuzione backup per creare symlink..."
/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/k8s/backup-cluster-state.sh
echo ""

# Step 2: Verify symlinks
echo "[2/5] Verifica symlink..."
if [[ -L /var/backups/k3s-cluster/k3s-cluster-snapshot.tar.gz ]]; then
    TARGET=$(readlink /var/backups/k3s-cluster/k3s-cluster-snapshot.tar.gz)
    echo "  ✅ Snapshot symlink: k3s-cluster-snapshot.tar.gz -> $TARGET"
else
    echo "  ❌ ERRORE: Snapshot symlink mancante!"
    exit 1
fi

if [[ -L /var/backups/k3s-cluster/latest-backup.tar.gz ]]; then
    TARGET=$(readlink /var/backups/k3s-cluster/latest-backup.tar.gz)
    echo "  ✅ Latest symlink: latest-backup.tar.gz -> $TARGET"
else
    echo "  ❌ ERRORE: Latest symlink mancante!"
    exit 1
fi
echo ""

# Step 3: Reload systemd
echo "[3/5] Reload systemd daemon..."
systemctl daemon-reload
echo "  ✅ Systemd ricaricato"
echo ""

# Step 4: Check service status
echo "[4/5] Verifica servizio auto-restore..."
ENABLED=$(systemctl is-enabled k3s-auto-restore.service 2>/dev/null || echo "disabled")
CONDITION=$(systemctl show k3s-auto-restore.service -p ConditionResult --value)

echo "  Status: $ENABLED"
echo "  Condition Result: $CONDITION"

if [[ "$CONDITION" == "yes" ]]; then
    echo "  ✅ Servizio correttamente configurato!"
else
    echo "  ⚠️  Condition non soddisfatta, verifica manuale necessaria"
fi
echo ""

# Step 5: List backups
echo "[5/5] Backup disponibili:"
ls -lh /var/backups/k3s-cluster/*.tar.gz | grep -v "^l" | awk '{print "  •", $9, "("$5")", $6, $7, $8}'
echo ""
ls -lh /var/backups/k3s-cluster/*.tar.gz | grep "^l" | while read line; do
    FILE=$(echo $line | awk '{print $9}')
    TARGET=$(echo $line | awk '{print $11}')
    echo "  → $(basename $FILE) -> $TARGET"
done
echo ""

# Final summary
echo "=========================================="
echo "RIEPILOGO"
echo "=========================================="
echo ""
echo "✅ Backup script aggiornato"
echo "✅ Symlink snapshot creato"
echo "✅ Symlink latest aggiornato"
echo "✅ Systemd ricaricato"
if [[ "$CONDITION" == "yes" ]]; then
    echo "✅ Auto-restore FUNZIONANTE"
else
    echo "⚠️  Auto-restore: condition non soddisfatta"
fi
echo ""
echo "Il restore automatico partirà al prossimo reboot se:"
echo "  1. Il cluster ha <5 deployment"
echo "  2. Non è già stato eseguito oggi"
echo ""
echo "Per testare il restore manualmente (ATTENZIONE: sovrascrive risorse):"
echo "  sudo /home/mpasqui/insightlearn_WASM/InsightLearn_WASM/k8s/restore-cluster-state.sh"
echo ""

exit 0
