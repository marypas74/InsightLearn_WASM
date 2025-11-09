#!/bin/bash
# QUESTO SCRIPT RISOLVE IL 502 BAD GATEWAY
# Esegui con: sudo ./ESEGUI-QUESTO-COMANDO.sh

echo "═══════════════════════════════════════════════════════════"
echo "  FIX 502 BAD GATEWAY - Import Nuova API con Auth Endpoints"
echo "═══════════════════════════════════════════════════════════"
echo ""

# Verifica che sia eseguito con sudo
if [ "$EUID" -ne 0 ]; then
    echo "❌ ERRORE: Questo script deve essere eseguito con sudo"
    echo ""
    echo "Esegui: sudo ./ESEGUI-QUESTO-COMANDO.sh"
    exit 1
fi

echo "✅ Running as root"
echo ""

# Step 1: Import image
echo "📦 Importing API image to K3s..."
if [ ! -f "/tmp/insightlearn-api.tar" ]; then
    echo "❌ Errore: /tmp/insightlearn-api.tar non trovata"
    echo "   L'immagine dovrebbe essere stata creata da Claude."
    echo "   Verifica che esista con: ls -lh /tmp/insightlearn-api.tar"
    exit 1
fi

/usr/local/bin/k3s ctr images import /tmp/insightlearn-api.tar

if [ $? -ne 0 ]; then
    echo "❌ Import fallito!"
    exit 1
fi

echo ""
echo "✅ Image imported successfully!"
echo ""

# Verify
echo "📋 Verifico immagine importata..."
/usr/local/bin/k3s ctr images ls | grep insightlearn

echo ""
echo "═══════════════════════════════════════════════════════════"
echo "  Import completato! Ora riavvia i pod API"
echo "═══════════════════════════════════════════════════════════"
echo ""
echo "Esegui (come utente normale, NON sudo):"
echo ""
echo "  kubectl delete pod -n insightlearn -l app=insightlearn-api"
echo ""
echo "Poi attendi che i nuovi pod siano pronti:"
echo ""
echo "  kubectl get pods -n insightlearn -w"
echo ""
