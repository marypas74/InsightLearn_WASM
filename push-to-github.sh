#!/bin/bash

echo "═══════════════════════════════════════════════════════════════"
echo "  PUSH INSIGHTLEARN WASM TO GITHUB"
echo "═══════════════════════════════════════════════════════════════"
echo ""

# Verifica di essere nella directory corretta
if [ ! -d ".git" ]; then
    echo "❌ Errore: Non sei nella directory del repository Git!"
    echo "   Esegui: cd /home/mpasqui/insightlearn-wasm"
    exit 1
fi

# Verifica remote
echo "📡 Verifica remote GitHub..."
git remote -v | grep origin

if [ $? -ne 0 ]; then
    echo "❌ Remote 'origin' non configurato!"
    echo "   Esegui: git remote add origin https://github.com/marypas74/InsightLearn_WASM.git"
    exit 1
fi

echo ""
echo "📊 Stato repository locale:"
echo "   Commits: $(git log --oneline | wc -l)"
echo "   File: $(git ls-files | wc -l)"
echo "   Branch: $(git branch --show-current)"
echo ""

# Mostra ultimi commit
echo "📝 Ultimi 5 commit da pushare:"
git log --oneline -5
echo ""

# Conferma
read -p "⚠️  Vuoi procedere con il push? (y/n) " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo "❌ Push annullato"
    exit 1
fi

# Push
echo ""
echo "🚀 Push in corso..."
git push -u origin main

if [ $? -eq 0 ]; then
    echo ""
    echo "✅ PUSH COMPLETATO CON SUCCESSO!"
    echo ""
    echo "🌐 Repository disponibile su:"
    echo "   https://github.com/marypas74/InsightLearn_WASM"
    echo ""
    echo "📋 Prossimi passi:"
    echo "   1. Verifica su GitHub che tutti i file siano presenti"
    echo "   2. Aggiungi topics alla repository"
    echo "   3. Crea un release tag (opzionale)"
    echo ""
else
    echo ""
    echo "❌ ERRORE DURANTE IL PUSH!"
    echo ""
    echo "Possibili cause:"
    echo "   • Autenticazione non configurata"
    echo "   • Token scaduto o invalido"
    echo "   • Problemi di connessione"
    echo ""
    echo "Soluzioni:"
    echo "   1. Verifica autenticazione GitHub"
    echo "   2. Rigenera Personal Access Token"
    echo "   3. Verifica remote URL: git remote -v"
    exit 1
fi
