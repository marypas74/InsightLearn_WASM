#!/bin/bash
# MongoDB Port-Forward Persistente per VS Code
# Espone MongoDB su localhost:27017

PORT=27017
NAMESPACE=insightlearn

echo "🔗 Avvio port-forward persistente MongoDB..."
echo "📍 Porta locale: $PORT"
echo "🎯 Namespace: $NAMESPACE"
echo ""
echo "⚠️  IMPORTANTE:"
echo "   - Mantieni questo terminale aperto"
echo "   - Connection string VS Code: mongodb://insightlearn:PASSWORD@localhost:27017/insightlearn_videos?authSource=admin"
echo "   - Password disponibile in Kubernetes Secret: insightlearn-secrets"
echo ""

# Loop infinito con auto-restart
while true; do
    echo "🚀 Connessione in corso..."

    # Port-forward con auto-restart on failure
    kubectl port-forward -n $NAMESPACE svc/mongodb-service $PORT:27017

    # Se il processo termina, attendi 5 secondi prima di riavviare
    EXIT_CODE=$?

    if [ $EXIT_CODE -ne 0 ]; then
        echo "❌ Port-forward terminato con exit code $EXIT_CODE"
        echo "⏳ Riavvio tra 5 secondi..."
        sleep 5
    else
        echo "ℹ️  Port-forward chiuso normalmente"
        break
    fi
done
