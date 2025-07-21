# 🚀 XpertSphere - Guide de démarrage rapide

## Configuration initiale

### 1. Copier le fichier de configuration
```bash
cp .env.example .env
```

### 2. Personnaliser les variables (optionnel)
Éditez le fichier `.env` si vous voulez changer les mots de passe par défaut.

### 3. Démarrer l'infrastructure
```bash
# Rendre les scripts exécutables
chmod +x start-infrastructure.sh setup-env.sh

# Démarrer l'infrastructure Docker
./start-infrastructure.sh

# Configurer les variables d'environnement pour .NET
./setup-env.sh
```

### 4. Créer la base de données
```bash
cd src/backend

# Créer la migration (première fois seulement)
dotnet ef migrations add InitialCreate --project XpertSphere.MonolithApi

# Appliquer la migration
dotnet ef database update --project XpertSphere.MonolithApi
```

## Services disponibles

- **SQL Server**: `localhost:1433`
- **Redis**: `localhost:6379`  
- **Adminer (DB UI)**: http://localhost:8080

## Connexion base de données

- **Server**: localhost,1433
- **User**: sa
- **Password**: Voir fichier `.env`
- **Database**: XpertSphereDb

## Sécurité

⚠️ **Important**: Le fichier `.env` contient des secrets et ne doit jamais être commité.
✅ Utilisez `.env.example` comme template pour les nouveaux développeurs.
