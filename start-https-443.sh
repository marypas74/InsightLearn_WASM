#!/bin/bash
# Script per avviare il port-forward HTTPS sulla porta 443 (richiede sudo)

echo "Avvio port-forward HTTPS su porta 443 (richiede privilegi root)..."

# Ferma il servizio systemd esistente se attivo
systemctl stop insightlearn-wasm-proxy.service 2>/dev/null

# Uccidi tutti i port-forward WASM esistenti
pkill -f "kubectl.*port-forward.*insightlearn-wasm" 2>/dev/null
sleep 2

# Avvia port-forward per HTTP (porta 80)
nohup /home/mpasqui/.local/bin/kubectl port-forward -n insightlearn --address 0.0.0.0 \
    service/insightlearn-wasm-blazor-webassembly 80:80 \
    > /tmp/pf-wasm-80.log 2>&1 &
PID_80=$!
echo "Port-forward HTTP avviato (porta 80, PID: $PID_80)"

# Avvia port-forward per HTTPS (porta 443)
nohup /home/mpasqui/.local/bin/kubectl port-forward -n insightlearn --address 0.0.0.0 \
    service/insightlearn-wasm-blazor-webassembly 443:443 \
    > /tmp/pf-wasm-443.log 2>&1 &
PID_443=$!
echo "Port-forward HTTPS avviato (porta 443, PID: $PID_443)"

# Attendi e verifica
sleep 3
echo ""
echo "Verifica porte attive:"
ss -tlnp | grep -E ":(80|443) "

echo ""
echo "Port-forward attivi. Per verificare:"
echo "  - HTTP:  curl http://localhost"
echo "  - HTTPS: curl -k https://localhost"
echo "  - LAN HTTP:  curl http://192.168.1.114"
echo "  - LAN HTTPS: curl -k https://192.168.1.114"
