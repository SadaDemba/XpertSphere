# Architecture — XpertSphere.MonolithApi

Ce document est établi à partir de l'inspection directe du code (csproj, `Program.cs`, extensions, services, configuration) et non du `README.md` du dossier, dont certains chiffres (couverture de tests, port, OWASP, perf) sont obsolètes ou non vérifiables dans le code : en cas de divergence, ce document donne la priorité à ce qui est observable dans le code.

## Stack technique

- **.NET 9** / ASP.NET Core Web API (`Microsoft.NET.Sdk.Web`), nullable et implicit usings activés.
- **Entity Framework Core 9** avec **SQL Server** comme base de données (`Microsoft.EntityFrameworkCore.SqlServer`, `.Design`, `.Tools`).
- **ASP.NET Core Identity** (`Microsoft.AspNetCore.Identity.EntityFrameworkCore`) pour la gestion des utilisateurs/mots de passe.
- **Authentification hybride** : JWT Bearer local (`Microsoft.AspNetCore.Authentication.JwtBearer`) et Entra ID B2B/B2C via `Microsoft.Identity.Web`.
- **FluentValidation** (12.x) pour la validation des DTOs, invoquée manuellement dans la couche service (voir plus bas).
- **AutoMapper 15** pour le mapping entités ↔ DTOs (profils dans `Mappings/`).
- **Logging** : le package `Serilog.AspNetCore` est référencé dans le `.csproj` mais n'est pas visiblement câblé dans `Program.cs` (pas de `UseSerilog`/`AddSerilog` observé) ; le logging effectif hors Development repose sur `Microsoft.Extensions.Logging` (`ClearProviders` + `AddConsole` + `AddApplicationInsights`). À vérifier si l'usage de Serilog est prévu ailleurs ou à retirer du `.csproj` s'il n'est plus utilisé.
- **Application Insights** activé hors Development (Staging/Production) pour la télémétrie et les logs.
- **Swashbuckle** (Swagger/OpenAPI) pour la documentation d'API, exposée en développement.
- Services Azure : **Key Vault** (secrets), **Blob Storage** (CV), **Application Insights** (télémétrie), configurés uniquement hors environnement Development.
- `DotNetEnv` pour charger un fichier `.env` local (`Env.Load()` en tout début de `Program.cs`).

## Structure du projet

```
Controllers/           Endpoints API (un contrôleur par ressource : Users, JobOffers, Applications, Auth, Roles, Permissions, Organizations, ...)
DTOs/                  Objets de transfert, organisés par feature (User, Auth, JobOffer, Application, ...)
Services/ + Interfaces/  Logique métier ; chaque service a son interface (IUserService, IJobOfferService, ...)
Models/                 Entités de domaine EF Core (User, Organization, JobOffer, Application, Role, Permission, ...)
Models/Base/            Classes de base : AuditableEntity, IAuditableEntity, Address, Filter
Data/                   XpertSphereDbContext + Data/Configurations (IEntityTypeConfiguration par entité, dont une base AuditableEntityConfiguration)
Migrations/             Migrations EF Core (une dizaine de migrations au moment de la rédaction, plus le snapshot de modèle)
Mappings/               Profils AutoMapper par feature
Validators/             Validators FluentValidation par feature (ex. Validators/User/CreateUserDtoValidator.cs)
Extensions/              Extensions de configuration DI (DatabaseExtensions, SecurityExtensions, FluentValidationExtensions, SwaggerExtensions, BlobStorageExtensions, KeyVaultExtensions, ControllerExtensions, EnumExtensions)
Extensions/DependencyInjections/  ApplicationServicesExtensions (enregistrement des services), EntraIdFallbackExtensions, EntraIdRateLimitExtensions
Config/                 Classes de configuration fortement typées pour Entra ID (EntraIdSettings, EntraIdB2BSettings, EntraIdB2CSettings, EntraIdUserInfo)
Middleware/             ClaimsEnrichmentMiddleware (enrichissement des claims, actif seulement hors Development quand Entra ID est utilisé)
Enums/                  Enums métier (ApplicationStatus, JobOfferStatus, ContractType, WorkMode, PermissionAction, PermissionScope, ...)
Utils/                  Constants, Roles (définitions de rôles RBAC), Results/ (ServiceResult, ServiceResult<T>, pagination)
uploads/                Répertoire local de stockage (usage à confirmer selon l'environnement)
```

Le projet suit une architecture en couches classique (Controllers → Services/Interfaces → EF Core DbContext), organisée par feature plutôt qu'une séparation stricte Domain/Application/Infrastructure de type Clean Architecture.

Le projet de tests associé, `XpertSphere.MonolithApi.Tests` (dossier voisin `src/backend/XpertSphere.MonolithApi.Tests/`), a accès aux membres internes via `InternalsVisibleTo` déclaré dans le `.csproj`. Il utilise xUnit, Moq, FluentAssertions et Entity Framework InMemory ; sa structure distingue les tests de Services et de Controllers, avec des helpers (`TestDbContextFactory`, `AutoMapperHelper`, `MockHelper`). Le nombre exact de tests évolue avec le code : ne pas s'appuyer sur un chiffre figé, `dotnet test` fait foi.

## Base de données et variables d'environnement

- La chaîne de connexion par défaut en développement (`appsettings.Development.json`) pointe vers un SQL Server local sur `localhost,1433` (pas SQL LocalDB) ; un serveur SQL Server doit donc être disponible sur ce port (conteneur Docker ou instance locale, à mettre en place soi-même — aucun `docker-compose` n'est présent dans `src/backend/`).
- Les migrations et le seed de données (organisation XpertSphere, rôles par défaut, compte PlatformSuperAdmin) s'exécutent **automatiquement au démarrage** de l'application, via `UseDatabaseAsync()` appelé dans `Program.cs` (`context.Database.MigrateAsync()` puis seed). Il n'est donc pas nécessaire de lancer `dotnet ef database update` séparément pour démarrer en local, mais la base doit être joignable au démarrage.
- Un fichier `.env` est chargé via `DotNetEnv` (`Env.Load()`) avant la construction du host ; il n'a pas été lu ni son contenu documenté ici (secrets). Les fichiers `.env.example`, `.env.production`, `.env.staging` existent à la racine du projet comme gabarits.
- Variables d'environnement notables observées dans le code (noms uniquement, sans valeurs) :
  - `USE_ENTRA_ID` : active l'authentification Entra ID en plus du JWT local (`Program.cs`).
  - `CORS__ALLOWED_ORIGINS` : origines autorisées en CORS, uniquement appliqué en Development.
  - `ConnectionStrings__DefaultConnection` (et variantes `__Production` / `__Staging`) : fallback si absent de la configuration/Key Vault (`DatabaseExtensions`).
  - `JWT__KEY` : fallback pour la clé de signature JWT si absente de la configuration (`SecurityExtensions`).
  - `Admin:Email` / `Admin:Password` : nécessaires pour le seed du compte PlatformSuperAdmin (`DatabaseExtensions`), l'absence lève une exception au démarrage.
- Hors Development, Azure Key Vault, Application Insights et le logging associé sont activés automatiquement (`Program.cs`).

## Conventions de code observées

- **Résultats de service uniformes** : les services renvoient `ServiceResult` / `ServiceResult<T>` (dans `Utils/Results/ServiceResult.cs`), avec des factory methods explicites (`Success`, `Failure`, `NotFound`, `Unauthorized`, `Forbidden`, `Conflict`, `ValidationError`, `InternalError`). Les contrôleurs convertissent ce résultat en `ActionResult` via les extensions `ToActionResult` / `ToPaginatedActionResult` (`Extensions/ControllerExtensions.cs`), qui mappent chaque `StatusCode` du `ServiceResult` vers la réponse HTTP correspondante.
- **Validation FluentValidation invoquée manuellement** : les validators sont enregistrés globalement via `AddValidatorsFromAssembly` (`Extensions/FluentValidationExtensions.cs`, cascade `Stop` au niveau règle, `Continue` au niveau classe), mais il n'y a pas d'auto-validation sur le model binding. Chaque service injecte les `IValidator<TDto>` dont il a besoin et appelle explicitement `ValidateAsync(...)` avant d'exécuter la logique métier (vu par exemple dans `Services/UserService.cs`). Un futur endpoint doit suivre ce même pattern (injecter le validator, l'appeler dans le service, pas dans le contrôleur).
- **DI organisée par extension method** : chaque grand bloc d'infrastructure (base de données, sécurité, blob storage, AutoMapper, FluentValidation, Swagger) a sa propre méthode d'extension statique sur `IServiceCollection` / `WebApplication`, appelée depuis `Program.cs`. Les enregistrements de services applicatifs eux-mêmes sont centralisés dans `Extensions/DependencyInjections/ApplicationServicesExtensions.cs`.
- **Un service = une interface** : chaque service métier dans `Services/` a son interface correspondante dans `Interfaces/` (`IUserService`, `IJobOfferService`, etc.), injectée dans les contrôleurs et testée via mocks (Moq) côté tests.
- **DTOs et Validators organisés par feature**, pas par type technique : un sous-dossier par ressource (`DTOs/User/`, `Validators/User/`, `Mappings/UserMappingProfile.cs`), à répliquer pour toute nouvelle ressource.
- **Rôles RBAC centralisés** dans `Utils/Roles.cs` (`RoleDefinition` avec `Name`/`Description`/`DisplayName`, regroupements `PlatformRoles`, `OrganizationRoles`, `InternalRoles`, etc.), utilisés à la fois dans le seed (`DatabaseExtensions`) et dans les policies d'autorisation (`SecurityExtensions.AddAuthorizationPolicies`).
- **Autorisation par policies nommées** plutôt que par rôle brut dans les contrôleurs : `[Authorize(Policy = "...")]` (ex. `RequireInternalUser`, `CanCreateUsers`, `CandidateOwnDataAccess`, `OrganizationIsolation`). Les policies combinent souvent rôle + claims personnalisées (`OrganizationId`, `OrganizationName`, claims `group` Entra ID) pour l'isolation multi-organisation.
- **Configurations EF Core séparées** dans `Data/Configurations/` (une classe `IEntityTypeConfiguration<T>` par entité), avec une base commune `AuditableEntityConfiguration` pour les champs d'audit (`CreatedAt`, etc., voir `Models/Base/AuditableEntity.cs` et `IAuditableEntity.cs`).

## Points d'attention

- **Authentification hybride complexe** : en Development ou si `USE_ENTRA_ID` est absent/faux, seul le JWT local est actif. En production avec Entra ID activé, un `PolicyScheme` (`MultiScheme`) route dynamiquement chaque requête vers le schéma B2B, B2C ou JWT selon l'inspection de l'issuer du token (`SecurityExtensions.AddEntraIdAuthentication`). Toute modification du pipeline d'authentification doit être testée dans les deux modes.
- **Migrations et seed automatiques au démarrage** : `UseDatabaseAsync()` applique les migrations en attente et seed l'organisation XpertSphere, les rôles par défaut et le compte PlatformSuperAdmin à chaque démarrage de l'application (pas seulement en développement). Une erreur de connexion à la base fait échouer le démarrage (l'exception est loguée puis relancée).
- **Compte PlatformSuperAdmin de seed** : créé avec un mot de passe issu de la configuration (`Admin:Password`), avec un avertissement explicite dans les logs à changer après la première connexion (`DatabaseExtensions.SeedPlatformSuperAdminAsync`).
- **Isolation multi-organisation portée par les claims** : plusieurs policies (`OrganizationIsolation`, `OrganizationAccess`, `CanCreateUsers`, `CanResetPasswords`) accordent des accès élargis aux utilisateurs rattachés à l'organisation "XpertSphere" (par claim `OrganizationName` ou claim `group` Entra ID). Une partie de la validation (ex. appartenance effective à l'organisation cible) est explicitement déléguée à la couche service et non entièrement couverte par la policy elle-même — à garder en tête en cas d'ajout d'un nouvel endpoint sensible.
- **CORS restreint au développement** : la politique CORS n'est enregistrée et appliquée que si `app.Environment.IsDevelopment()` est vrai ; en Staging/Production, CORS n'est pas configuré dans ce fichier (vérifier la configuration au niveau infrastructure/API Management si un accès cross-origin est nécessaire).
- **Secrets non lus** : les fichiers `.env`, `.env.production`, `.env.staging` à la racine du projet n'ont pas été ouverts lors de la rédaction de ce document ; ne pas les committer ni en divulguer le contenu.
