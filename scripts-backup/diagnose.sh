#!/bin/bash

echo "🔍 Diagnostic de l'infrastructure XpertSphere..."

# Charger les variables d'environnement
if [ -f ".env" ]; then
    export $(cat .env | grep -v '^#' | xargs)
fi

echo ""
echo "📊 État des conteneurs :"
docker-compose ps

echo ""
echo "🔍 Santé des services :"
docker inspect --format='{{.Name}}: {{.State.Health.Status}}' $(docker-compose ps -q) 2>/dev/null || echo "Health check non disponible"

echo ""
echo "📝 Derniers logs SQL Server :"
docker logs --tail 20 xpertsphere-sqlserver

echo ""
echo "🔧 Test de connexion SQL Server :"
if docker exec xpertsphere-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U ${DB_USER} -P ${DB_PASSWORD} -Q "SELECT 1" > /dev/null 2>&1; then
    echo "✅ Connexion SQL Server OK"
else
    echo "❌ Connexion SQL Server échouée"
fi

echo ""
echo "🔧 Test de connexion Redis :"
if docker exec xpertsphere-redis redis-cli -a ${REDIS_PASSWORD} ping > /dev/null 2>&1; then
    echo "✅ Connexion Redis OK"
else
    echo "❌ Connexion Redis échouée"
fi

echo ""
echo "💡 Conseils si problème :"
echo "  1. Vérifier les logs : docker logs xpertsphere-sqlserver"
echo "  2. Redémarrer : docker-compose restart sqlserver"
echo "  3. Reset complet : docker-compose down -v && ./start-infrastructure.sh"
