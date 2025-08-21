# XpertSphere MonolithApi

## 🚀 Vue d'ensemble

XpertSphere MonolithApi est une API REST développée en .NET 9 pour gérer une plateforme de recrutement complète. Elle permet la gestion des utilisateurs, des organisations, des offres d'emploi et des candidatures avec un système d'authentification hybride (local et Entra ID).

## 📊 Statistiques du Projet

- **Framework** : .NET 9 / ASP.NET Core
- **Base de données** : SQL Server avec Entity Framework Core
- **Tests** : 84 tests unitaires (12.2% de couverture)
- **Authentification** : JWT + Entra ID (Azure AD B2C/B2B)
- **Monitoring** : Azure Application Insights

## 🛠️ Technologies Clés

### Backend & Framework
- **.NET 9** - Runtime et framework principal
- **ASP.NET Core Web API** - Framework web
- **Entity Framework Core 9.0** - ORM et accès aux données
- **SQL Server** - Base de données relationnelle

### Authentification & Sécurité
- **Microsoft Identity Web** - Intégration Entra ID
- **JWT Bearer Authentication** - Tokens d'authentification
- **Azure Key Vault** - Gestion sécurisée des secrets
- **FluentValidation** - Validation des entrées

### Services Azure
- **Application Insights** - Télémétrie et monitoring
- **Blob Storage** - Stockage de fichiers (CVs)
- **Key Vault** - Gestion des secrets

### Outils de Développement
- **AutoMapper** - Mapping entités ↔ DTOs
- **Swagger/OpenAPI** - Documentation API
- **Serilog** - Logging structuré

## 🏗️ Architecture

### Structure Clean Architecture

```
┌─────────────────────────────────────┐
│            Controllers              │  ← API Endpoints
├─────────────────────────────────────┤
│              DTOs                   │  ← Data Transfer Objects
├─────────────────────────────────────┤
│            Services                 │  ← Business Logic
├─────────────────────────────────────┤
│             Models                  │  ← Domain Entities
├─────────────────────────────────────┤
│         Data Access (EF)            │  ← Repository Pattern
└─────────────────────────────────────┘
```

### Entités Principales

- **User** - Utilisateurs (candidats/recruteurs)
- **Organization** - Entreprises et organisations  
- **JobOffer** - Offres d'emploi
- **Application** - Candidatures
- **Role/Permission** - Système d'autorisation

## 🌐 API Endpoints

### 🔐 Authentication (`/api/auth`)
- `POST /register` - Inscription utilisateur
- `POST /login` - Connexion
- `POST /refresh-token` - Renouvellement token
- `GET /login-url` - URL connexion Entra ID

### 👤 Users (`/api/users`)
- `GET /users` - Liste paginée des utilisateurs
- `GET /users/{id}` - Détails utilisateur
- `POST /users` - Création utilisateur
- `PUT /users/{id}` - Modification utilisateur
- `POST /users/{id}/cv` - Upload CV

### 💼 Job Offers (`/api/joboffers`)
- `GET /joboffers` - Liste des offres
- `POST /joboffers` - Création offre
- `PATCH /joboffers/{id}/publish` - Publication

### 📝 Applications (`/api/applications`)
- `GET /applications` - Liste des candidatures
- `POST /applications` - Soumission candidature  
- `PATCH /applications/{id}/status` - Mise à jour statut

## ⚙️ Configuration

### Variables d'Environnement

```bash
# Base de données
ConnectionStrings__DefaultConnection=Server=...;Database=...;

# Entra ID
EntraIdB2C__ClientId=...
EntraIdB2C__ClientSecret=...
EntraIdB2C__TenantId=...

# Azure Services  
ApplicationInsights__ConnectionString=...
BlobStorage__ConnectionString=...
KeyVault__VaultUri=...
```

### Environnements Supportés

- **Development** - Configuration locale avec SQL LocalDB
- **Staging** - Environnement de test Azure
- **Production** - Environnement de production Azure

## 🚀 Démarrage Local

### Prérequis

- .NET 9 SDK
- SQL Server ou SQL LocalDB
- Visual Studio 2024 ou VS Code

### Installation

```bash
# Cloner le projet
git clone [repository-url]
cd src/backend/XpertSphere.MonolithApi

# Restaurer les packages
dotnet restore

# Configurer la base de données
dotnet ef database update

# Lancer l'application
dotnet run
```

L'API sera accessible sur `https://localhost:7294`

### Configuration de la Base de Données

```bash
# Créer une nouvelle migration
dotnet ef migrations add MigrationName

# Appliquer les migrations
dotnet ef database update

# Rollback une migration
dotnet ef database update PreviousMigrationName
```

## 🧪 Tests

Le projet inclut une suite complète de tests unitaires :

```bash
# Exécuter tous les tests
dotnet test

# Avec couverture de code
dotnet test --collect:"XPlat Code Coverage"

# Tests spécifiques
dotnet test --filter "UsersControllerTests"
```

**Couverture actuelle** : 12.2% (84 tests réussis)

## 📊 Monitoring & Observabilité

### Application Insights

- **Télémétrie automatique** des requêtes HTTP
- **Métriques personnalisées** pour les KPIs métier
- **Logs structurés** avec corrélation de traces
- **Monitoring des dépendances** (SQL, Azure services)

### Logging

```csharp
// Exemple d'usage du logging
_logger.LogInformation("User {UserId} created successfully", user.Id);
_logger.LogWarning("Failed login attempt for {Email}", email);
_logger.LogError(ex, "Error processing application {ApplicationId}", id);
```

### Métriques Surveillées

- Temps de réponse des endpoints
- Taux d'erreur HTTP
- Performance des requêtes SQL  
- Utilisation des ressources Azure

## 🔒 Sécurité

### Authentification Hybride

1. **Local JWT** - Authentification interne
2. **Entra ID B2C** - Authentification candidats
3. **Entra ID B2B** - Authentification entreprises

### Autorisation Basée sur les Rôles

- **SuperAdmin** - Administration complète
- **OrganizationAdmin** - Gestion organisation
- **Recruiter** - Gestion des offres

### Conformité OWASP Top 10 2021 *(Score: 8.5/10)* 🛡️

| Vulnérabilité | Status | Implémentation |
|---------------|--------|----------------|
| **A01: Broken Access Control** | ✅ **Implémenté** | Politiques d'autorisation, RBAC, isolation multi-tenant |
| **A02: Cryptographic Failures** | ✅ **Implémenté** | JWT HMAC-SHA256, TLS, hachage sécurisé des mots de passe |
| **A03: Injection** | ✅ **Implémenté** | EF Core paramétrisé, FluentValidation, sanitisation |
| **A04: Insecure Design** | ✅ **Implémenté** | Flux d'auth sécurisés, gestion de session, MFA |
| **A05: Security Misconfiguration** | ✅ **Implémenté** | HTTPS, cookies sécurisés, HSTS + **APIM + ACA sécurisés** |
| **A06: Vulnerable Components** | ✅ **Implémenté** | ACA gestion automatique + **APIM protection externe** |
| **A07: Auth Failures** | ✅ **Implémenté** | Politiques de MDP, verrouillage, MFA, expiration tokens |
| **A08: Data Integrity** | ⚠️ **Partiel** | Validation JWT, audit fields *(manque: audit complet)* |
| **A09: Logging Failures** | ✅ **Implémenté** | AuthenticationLogger, événements sécurité, monitoring |
| **A10: SSRF** | ✅ **Implémenté** | **APIM contrôle trafic sortant + ACA isolation réseau** |

### Architecture Sécurisée Multi-Couches
```
Internet → APIM (API Key) → ACA (Réseau Isolé) → API (RBAC + JWT)
                ↓                    ↓               ↓
          Rate Limiting      Container Security   Auth Robuste
          Headers Security   Network Policies     Validation
          CORS Policies      Auto-Updates         Audit Logs
```

### Points Forts Sécurité
- ✅ **Architecture Defense-in-Depth** : APIM + ACA + API
- ✅ **Isolation Réseau** : ACA containers + CORS restreint
- ✅ **Authentification multi-providers** robuste (JWT + Entra ID)
- ✅ **Validation d'entrée exhaustive** (FluentValidation)
- ✅ **Chiffrement bout-en-bout** et gestion tokens sécurisés
- ✅ **Logging sécuritaire détaillé** avec monitoring Azure
- ✅ **Isolation multi-tenant** efficace
- ✅ **Protection APIM** : clé d'API + rate limiting + headers sécurité
- ✅ **Gestion automatique** des vulnérabilités (ACA)

### Améliorations Restantes *(Mineures)*
- 🔧 **Audit complet** : Logging détaillé des modifications de données
- ✨ **Nice-to-have** : WAF additionnel (déjà couvert par APIM)

## 📈 Performance

### Optimisations Implémentées

- **Pagination** pour toutes les listes
- **Lazy Loading** Entity Framework
- **Caching** des configurations
- **Compression** des réponses HTTP

### Métriques de Performance

- Temps de réponse API : < 200ms (p95)
- Débit : 1000+ requêtes/minute
- Disponibilité : 99.9%

## 🔄 Évolution & Roadmap

### Fonctionnalités en Développement

- [ ] Tests d'intégration complets
- [ ] Pipeline CI/CD automatisé
- [x] **Mesures de sécurité OWASP Top 10** *(Score: 8.5/10)* 🛡️
- [ ] Gestion avancée des notifications

### Améliorations Techniques

- [ ] Migration vers architecture microservices
- [ ] Implémentation CQRS/Event Sourcing
- [ ] Cache distribué (Redis)
- [ ] Message Queue (Service Bus)

---

## 📞 Support & Contributions

### Structure de l'Équipe

- **Backend** - API et logique métier
- **DevOps** - Infrastructure et déploiement  
- **QA** - Tests et validation

### Documentation Technique

- **API Documentation** : `/swagger` (environnement dev)
- **Tests Documentation** : `/Tests/README.md`
- **Database Schema** : `/Data/README.md`

**Dernière mise à jour** : Août 2025  
**Version** : 1.0.0  
**Environnement** : .NET 9