# XpertSphere - Infrastructure et Déploiement

Ce document décrit la configuration de l'infrastructure, les procédures de déploiement et la gestion des environnements pour XpertSphere.

## 🏗️ Architecture Infrastructure

### Vue d'ensemble

XpertSphere utilise une infrastructure cloud-native sur Microsoft Azure avec une approche DevOps complète :

```txt
┌─────────────────────────────────────────────────────────────────┐
│                          Azure Cloud                            │
├─────────────────────────────────────────────────────────────────┤
│  📱 Frontend Apps (Azure Container Apps)                        │
│  ├── RecruiterApp                                               │
│  └── CandidateApp                                               │
├─────────────────────────────────────────────────────────────────┤
│  🔀 Load Balancer & CDN                                         │
│  ├── Azure Front Door (CDN + WAF)                               │
│  └── Azure Application Gateway                                  │
├─────────────────────────────────────────────────────────────────┤
│  ⚙️  Backend Services (Azure Container Apps)                    │
│  ├── XpertSphere.MonolithApi (Port: 5000)                       │
│  ├── XpertSphere.CommunicationService (Port: 5001)              │
│  ├── XpertSphere.ReportingService (Port: 5002)                  │
│  ├── XpertSphere.IntegrationService (Port: 5003)                │
│  └── XpertSphere.ResumeAnalyzer (Port: 8000)                    │
├─────────────────────────────────────────────────────────────────┤
│  💾 Data Layer                                                  │
│  ├── Azure SQL Database (Primary + Read Replicas)               │
│  ├── Azure Redis Cache (Session + Caching)                      │
│  ├── Azure Blob Storage (Documents + Static Files)              │
│  └── Azure Service Bus (Message Queue)                          │
├─────────────────────────────────────────────────────────────────┤
│  🔐 Security & Identity                                         │
│  ├── Azure Entra ID (Authentication)                            │
│  └── Azure Key Vault (Secrets Management)                       │
├─────────────────────────────────────────────────────────────────┤
│  📊 Monitoring & Logging                                        │
│  ├── Azure Application Insights (APM)                           │
│  ├── Azure Monitor (Infrastructure)                             │
│  ├── Azure Log Analytics (Centralized Logging)                  │
│  └── Azure Alerts (Proactive Monitoring)                        │
└─────────────────────────────────────────────────────────────────┘
```

### Environnements

| Environnement | Utilisation | Ressources | URL |
|---------------|-------------|------------|-----|
| **Development** | Développement local | Docker Compose | localhost |
| **Staging** | Tests intégration | Azure (taille réduite) | staging.xpertsphere.azure.com |
| **Production** | Utilisateurs finaux | Azure (haute disponibilité) | app.xpertsphere.com |

## 🚀 Démarrage Rapide (Développement Local)

### 1. Prérequis

Vérifiez que vous avez installé :

```bash
# Vérifier les versions
docker --version          # 24.0+
docker-compose --version  # 2.20+
node --version            # 20.0+
dotnet --version          # 9.0+
python --version          # 3.10+
```

### 2. Configuration initiale

```bash
# Cloner le projet
git clone https://github.com/your-org/XpertSphere.git
cd XpertSphere

# Copier et configurer l'environnement
cp .env.example .env
```

### 3. Configuration des variables d'environnement

Éditez le fichier `.env` :

```bash
# ===== BASE DE DONNÉES =====
CONNECTION_STRING=Server=localhost,1433;Database=<YourDbName>;User Id=sa;Password=<YourStrong@Password>;TrustServerCertificate=true;

# ===== REDIS CACHE =====
REDIS_CONNECTION_STRING=localhost:6379

# ===== AZURE CONFIGURATION =====
AZURE_CLIENT_ID=your-azure-client-id
AZURE_CLIENT_SECRET=your-azure-client-secret
AZURE_TENANT_ID=your-azure-tenant-id
AZURE_SUBSCRIPTION_ID=your-azure-subscription-id

# ===== AUTHENTIFICATION =====
JWT_SECRET=your-super-secret-jwt-key-min-32-chars
JWT_ISSUER=https://localhost:5000
JWT_AUDIENCE=https://localhost:5000

# ===== SERVICES EXTERNES =====
OPENAI_API_KEY=your-openai-api-key
LINKEDIN_API_KEY=your-linkedin-api-key
HELLOWORK_API_KEY=your-hellowork-api-key
WELCOME_TO_THE_JUNGLE_API_KEY=your-wttj-api-key

# ===== EMAIL CONFIGURATION =====
SMTP_HOST=<smtp.gmail.com>
SMTP_PORT=<YourPort>
SMTP_USER=<YourEmailAddress>
SMTP_PASSWORD=<YourAppPassword>

# ===== STOCKAGE =====
AZURE_STORAGE_CONNECTION_STRING=your-azure-storage-connection
AZURE_STORAGE_CONTAINER_NAME=xpertsphere-documents

# ===== MONITORING =====
APPLICATION_INSIGHTS_CONNECTION_STRING=your-app-insights-connection

# ===== FRONTEND =====
VITE_API_BASE_URL=https://localhost:5000/api
VITE_CV_ANALYZER_URL=http://localhost:8000/api
VITE_AZURE_CLIENT_ID=${AZURE_CLIENT_ID}
VITE_AZURE_TENANT_ID=${AZURE_TENANT_ID}

# ===== DEVELOPPEMENT =====
ASPNETCORE_ENVIRONMENT=Development
NODE_ENV=development
PYTHON_ENV=development
```

### 4. Démarrage des services

```bash
# Rendre les scripts exécutables
chmod +x start-infrastructure.sh setup-env.sh

# Démarrer l'infrastructure (SQL Server, Redis, etc.)
./start-infrastructure.sh

# Configurer les variables d'environnement pour .NET
./setup-env.sh

# Vérifier que les services sont démarrés
docker-compose ps
```

### 5. Configuration de la base de données

```bash
cd src/backend

# Créer et appliquer les migrations
dotnet ef migrations add InitialCreate --project XpertSphere.MonolithApi --verbose
dotnet ef database update --project XpertSphere.MonolithApi --verbose

# Vérifier la connexion
dotnet ef database update --project XpertSphere.MonolithApi --dry-run
```

### 6. Démarrage des applications

```bash
# Terminal 1 - API Backend
cd src/backend/XpertSphere.MonolithApi
dotnet watch run

# Terminal 2 - Service d'analyse CV
cd src/backend/XpertSphere.ResumeAnalyzer
source venv/bin/activate  # ou venv\Scripts\activate sur Windows
uvicorn main:app --reload --host 0.0.0.0 --port 8000

# Terminal 3 - Frontend Recruteur
cd src/frontend
npm run dev:recruiter

# Terminal 4 - Frontend Candidat
npm run dev:candidate
```

### 7. Vérification du déploiement

```bash
# Vérifier les services backend
curl https://localhost:5000/health
curl http://localhost:8000/api/health

# Vérifier les applications web
open http://localhost:3001  # RecruiterApp
open http://localhost:3000  # CandidateApp

# Vérifier la base de données (Adminer)
open http://localhost:8080
# Server: sql, Username: voir .env, Password: voir .env, Database: XpertSphereDb
```

## 🐳 Docker et Conteneurisation

### Docker Compose (Développement)

Le fichier `docker-compose.yml` configure l'infrastructure locale :

```yaml
services:
  # Base de données SQL Server
  sql:
    image: mcr.microsoft.com/mssql/server:2022-latest
    container_name: xpertsphere-sql
    environment:
      SA_PASSWORD: ${SA_PASSWORD:-YourStrongPassword}
      ACCEPT_EULA: Y
    ports:
      - "1433:1433"
    volumes:
      - sql_data:/var/opt/mssql
    healthcheck:
      test: /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "${SA_PASSWORD:-YourStrongPassword}"
