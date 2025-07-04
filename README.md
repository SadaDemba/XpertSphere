# XpertSphere

XpertSphere est un logiciel ATS (Applicant Tracking System) conçu pour optimiser et structurer le processus de recrutement. Développé dans le cadre d'un projet de fin d'études, ce système met l'accent sur l'expertise et l'innovation pour répondre aux besoins des équipes RH et des entreprises IT.

Fonctionnalités principales :

- Gestion des offres d'emploi et publication sur des plateformes externes (HelloWork, LinkedIn, etc.).
- Suivi des candidatures avec des statuts personnalisés (Nouvelle, Entretien RH, Entretien technique, Recruté, Rejeté).
- Notifications automatiques et modèles de messages personnalisables pour les candidats.
- Organisation des entretiens techniques avec feedback direct des employés.
- Analyse et reporting pour améliorer les processus de recrutement.

Technologies :
- Back-end : .NET 9 avec NSwag pour l'API.
- Front-end : Vue.js avec Pinia, Vee-Validate, et Quasar.
- Message Broker : RabbitMQ pour la gestion des notifications.
- Cloud : Azure

## 🛠️ Architecture technique

XpertSphere est construit sur une architecture hybride moderne :

- **Backend** :
  - API principale en **.NET 9** (Clean Architecture)
  - Microservice d'analyse de CV en **Python/FastAPI**
  - Services complémentaires en **.NET 9**
  - Communication événementielle via **Azure Service Bus**

- **Frontend** :
  - Applications **Vue.js 3** avec Composition API
  - Interface recruteur (RecruiterApp)
  - Interface candidat (CandidateApp)
  - Bibliothèque de composants partagés

- **Cloud** :
  - Déploiement sur **Azure Container Apps**
  - Base de données **Azure SQL**
  - Stockage de documents **Azure Blob Storage**
  - Cache distribué **Azure Redis Cache**
  - Authentification via **Azure Entra ID**

## 🚦 Pré-requis

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) (v18+)
- [Python](https://www.python.org/) (v3.10+)
- [Docker](https://www.docker.com/) et Docker Compose
- Un compte Azure avec les ressources nécessaires configurées

## 🔧 Installation

### Configurer les variables d'environnement

Créez un fichier `.env` à la racine du projet en vous basant sur le fichier `.env.example` :

```bash
cp .env.example .env
# Éditer le fichier .env avec vos propres valeurs
```text

### Démarrer l'environnement de développement

```bash
# Construire et démarrer tous les services
docker-compose up -d

# Ou démarrer un service spécifique
docker-compose up -d api
```

### Installation des dépendances frontend (sans Docker)

```bash
# Installer les dépendances communes
cd src/frontend/common
npm install

# Installer les dépendances de l'application recruteur
cd ../recruiter-app
npm install

# Installer les dépendances de l'application candidat
cd ../candidate-app
npm install
```

## 🧪 Exécution des tests

```bash
# Tests backend
dotnet test src/backend/XpertSphere.API/XpertSphere.API.sln

# Tests Python
cd src/backend/XpertSphere.CVAnalyzer
pytest

# Tests frontend
cd src/frontend/recruiter-app
npm run test
```

## 🌐 Structure du projet

```text
XpertSphere/
├── .github/                                    # Configuration GitHub Actions
├── docs/                                       # Documentation du projet
├── src/                                        # Code source
│   ├── backend/                                # Services backend
│   │   ├── XpertSphere.MonolithApi/            # 🏗️ Monolith (coeur du système)
│   │   ├── XpertSphere.CommunicationService/   # 📨 Emails, notifications, SignalR
│   │   ├── XpertSphere.ReportingService/       # 📊 Analytics et rapports
│   │   │── XpertSphere.IntegrationService/     # 🔌 LinkedIn, HelloWork, etc.
│   │   └── XpertSphere.CVAnalyzer/             # Service d'analyse de CV (FastAPI)
│   ├── frontend/                               # Applications frontend
│   │   ├── common/                             # Code partagé entre applications
│   │   ├── recruiter-app/                      # Application recruteur
│   │   └── candidate-app/                      # Application candidat
│   └── shared/                                 # Code partagé backend/frontend
├── deploy/                                     # Scripts et configurations de déploiement
└── tests/                                      # Tests automatisés
```

## 🤝 Contribution

Veuillez consulter le fichier [CONTRIBUTING.md](.github/CONTRIBUTING.md) pour les directives de contribution au projet.

## 📧 Contact

Pour toute question technique relative au projet, contactez l'équipe de développement à travers [Sada](mailto:sadadembat9@gmail.com).
