# CLAUDE.md — XpertSphere.MonolithApi

## Aperçu du service

XpertSphere.MonolithApi est l'API REST principale (monolithe) de la plateforme de recrutement XpertSphere. Elle expose les endpoints pour la gestion des utilisateurs, organisations, offres d'emploi, candidatures, expériences/formations, et le système de rôles/permissions (RBAC). Elle gère aussi l'authentification (JWT local et Entra ID) et le stockage des CV (Azure Blob Storage).

Le service cohabite dans le même monorepo (`src/backend/`) avec d'autres projets : `XpertSphere.MonolithApi.Tests` (tests unitaires), `XpertSphere.CommunicationService`, `XpertSphere.IntegrationService`, `XpertSphere.ReportingService`, `XpertSphere.ResumeAnalyzer`.

Stack : .NET 9 / ASP.NET Core Web API, Entity Framework Core 9 + SQL Server, ASP.NET Core Identity, authentification hybride JWT local / Entra ID, FluentValidation, AutoMapper, Swashbuckle.

## Documentation

- Architecture, structure du projet, conventions de code, points d'attention : `.claude/docs/architecture.md` — à lire avant toute modification non triviale.
- Spécifications fonctionnelles : `.claude/specifications/` (un fichier par fonctionnalité). Actuellement : `azurite-blob-storage-local.md` (stockage Blob local via Azurite, coexistant avec Azure Storage en Staging/Production), `secure-cv-download.md` (endpoint proxy authentifié pour consulter/télécharger un CV déjà uploadé, avec coordination frontend `candidate-app`/`recruiter-app`), `candidate-registration-experience-description-error.md` (blocage effectif + message d'erreur explicite sur une description d'expérience vide à l'inscription candidat, avec coordination frontend `candidate-app`), `login-response-missing-experiences-trainings.md` (`LoginAsync`/`RefreshTokenAsync` ne chargent pas `Experiences`/`Trainings`/`Address` contrairement à `GetCurrentUserAsync` — profil candidat visuellement incomplet jusqu'à un reload, avec coordination frontend `candidate-app`), `seed-demo-organizations-users-joboffers.md` (extension du seeder existant, bornée à Development, pour 3 organisations clientes de démonstration, leur roster d'utilisateurs, 30 offres d'emploi, 4 candidats et leurs candidatures), `enrich-seed-candidate-profiles.md` (extension de `SeedDemoCandidatesAsync` : expériences/formations, champs scalaires candidat, adresse et complétude de profil pour les 4 candidats de démonstration — upload de CV explicitement hors périmètre), `configurable-salary-currency.md` (enum `Currency` contrôlé EUR/XOF, devise configurable par organisation figée par offre à la création — non rétroactive —, devise auto-déclarée du candidat, nouvel endpoint self-service `organizations/me/currency` réservé à `Organization.Admin`, avec coordination frontend `candidate-app`/`recruiter-app`), `role-user-count-organization-scope-fix.md` (`RoleDto.UsersCount` non scopé par organisation sur `GET /api/Roles/paginated` — bug visible pour `Organization.Admin` — et même correctif de cohérence sur `UserRoleService.GetRoleUsersAsync`, bug latent car endpoint restreint aux profils plateforme et dialog non câblé côté `recruiter-app`) `french-message-consistency.md` (traduction en français des messages utilisateur du backend — `Constants.cs`, `Validators/**`, et sous réserve de confirmation `Services/*.cs`/`Controllers/*.cs` — et correction de résidus anglais côté frontend, avec coordination `candidate-app`/`recruiter-app`) et `candidate-account-activation-email.md` (ticket B de `email-sending-foundation.md` côté `CommunicationService` : envoi réel de l'email d'activation à l'inscription candidat, génération du lien/token, appel HTTP vers `CommunicationService`, correctif du provider de token Identity, activation stricte `RequireConfirmedEmail = true` avec endpoint de renvoi `resend-confirmation` et migration de grandfathering, avec coordination `candidate-app`).

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
