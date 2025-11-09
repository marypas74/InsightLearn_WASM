#!/bin/bash
# PASSO 2: Riavvia i pod API con la nuova immagine
# Esegui come utente normale (NON sudo)

echo "═══════════════════════════════════════════════════════════"
echo "  PASSO 2: Riavvio Pod API con Nuova Immagine"
echo "═══════════════════════════════════════════════════════════"
echo ""

echo "🔄 Eliminando i vecchi pod API..."
kubectl delete pod -n insightlearn -l app=insightlearn-api --grace-period=5

echo ""
echo "⏳ Attendo 5 secondi per la ricreazione dei pod..."
sleep 5

echo ""
echo "⏳ Attendo che i nuovi pod siano pronti (max 120 secondi)..."
kubectl wait --for=condition=ready pod -l app=insightlearn-api -n insightlearn --timeout=120s

if [ $? -ne 0 ]; then
    echo ""
    echo "⚠️  Warning: I pod non sono pronti dopo 120 secondi"
    echo ""
    echo "Verifica lo stato con:"
    echo "  kubectl get pods -n insightlearn -l app=insightlearn-api"
    echo ""
    echo "Verifica i log con:"
    echo "  kubectl logs -n insightlearn -l app=insightlearn-api --tail=100"
    exit 1
fi

echo ""
echo "✅ Nuovi pod pronti!"
echo ""

echo "📊 Stato dei pod:"
kubectl get pods -n insightlearn -l app=insightlearn-api -o wide

echo ""
echo "📋 Verifico i log per la creazione dell'utente admin..."
echo ""
sleep 3

kubectl logs -n insightlearn -l app=insightlearn-api --tail=200 | grep -E '\[SEED\]|\[DATABASE\]' --color=never | tail -25

echo ""
echo "═══════════════════════════════════════════════════════════"
echo "  ✅ DEPLOYMENT COMPLETATO!"
echo "═══════════════════════════════════════════════════════════"
echo ""
echo "🔐 Credenziali Admin:"
echo "   Email: admin@insightlearn.cloud"
echo "   Password: Admin@InsightLearn2025!"
echo ""
echo "🌐 Testa il login a: https://wasm.insightlearn.cloud/login"
echo ""
echo "🧪 Oppure testa l'endpoint manualmente:"
echo ""
echo "   kubectl port-forward -n insightlearn svc/api-service 8081:80 &"
echo "   curl -X POST http://localhost:8081/api/auth/login \\"
echo "     -H 'Content-Type: application/json' \\"
echo "     -d '{\"email\":\"admin@insightlearn.cloud\",\"password\":\"Admin@InsightLearn2025!\"}'"
echo ""
