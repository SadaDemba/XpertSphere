# CLAUDE.md — XpertSphere.MonolithApi

## Aperçu du service

XpertSphere.MonolithApi est l'API REST principale (monolithe) de la plateforme de recrutement XpertSphere. Elle expose les endpoints pour la gestion des utilisateurs, organisations, offres d'emploi, candidatures, expériences/formations, et le système de rôles/permissions (RBAC). Elle gère aussi l'authentification (JWT local et Entra ID) et le stockage des CV (Azure Blob Storage).

Le service cohabite dans le même monorepo (`src/backend/`) avec d'autres projets : `XpertSphere.MonolithApi.Tests` (tests unitaires), `XpertSphere.CommunicationService`, `XpertSphere.IntegrationService`, `XpertSphere.ReportingService`, `XpertSphere.ResumeAnalyzer`.

Stack : .NET 9 / ASP.NET Core Web API, Entity Framework Core 9 + SQL Server, ASP.NET Core Identity, authentification hybride JWT local / Entra ID, FluentValidation, AutoMapper, Swashbuckle.

## Documentation

- Architecture, structure du projet, conventions de code, points d'attention : `.claude/docs/architecture.md` — à lire avant toute modification non triviale.
- Spécifications fonctionnelles : `.claude/specifications/` (un fichier par fonctionnalité). Actuellement vide.

## Commandes

Toutes les commandes ci-dessous s'exécutent depuis `src/backend/XpertSphere.MonolithApi/` (ou en ciblant le projet avec `--project`).

```bash
# Restaurer les packages
dotnet restore

# Build
dotnet build

# Lancer l'API en local (profil "https" par défaut dans launchSettings.json)
dotnet run
# API accessible sur https://localhost:7001 (et http://localhost:5001)

# Migrations EF Core
dotnet ef migrations add <NomMigration>
dotnet ef database update
dotnet ef database update <MigrationPrecedente>   # rollback

# Tests (depuis src/backend/XpertSphere.MonolithApi.Tests/, ou avec --project)
dotnet test
dotnet test --collect:"XPlat Code Coverage"
dotnet test --filter "UserServiceTests"
```

Swagger est exposé en environnement Development (voir `SwaggerExtensions`/`Program.cs`).
