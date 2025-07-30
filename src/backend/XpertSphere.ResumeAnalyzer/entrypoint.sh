#!/bin/bash

# Entrypoint script for XpertSphere Resume Analyzer

set -e

echo "🚀 Starting XpertSphere Resume Analyzer..."

# Vérification des variables d'environnement importantes
if [ -z "$OPENAI_API_KEY" ] && [ "$ENVIRONMENT" != "test" ]; then
    echo "⚠️ Warning: OPENAI_API_KEY not set"
fi

# Vérification de la santé du système
echo "🔍 Checking system health..."

# Vérifier que Python fonctionne
python --version

# Vérifier que les dépendances sont installées
python -c "import fastapi, uvicorn, pdfplumber; print('✅ Core dependencies OK')"

# Démarrer l'application
echo "🌟 Starting FastAPI application..."

if [ "$1" = "dev" ]; then
    echo "🔧 Development mode"
    exec uvicorn app.main:app --host 0.0.0.0 --port 8000 --reload
elif [ "$1" = "test" ]; then
    echo "🧪 Test mode"
    exec pytest tests/ -v
else
    echo "🚀 Production mode"
    exec uvicorn app.main:app --host 0.0.0.0 --port 8000 --workers 1
fi
