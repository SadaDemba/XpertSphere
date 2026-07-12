# Architecture — XpertSphere.IntegrationService

## Rôle et état actuel

D'après le `CLAUDE.md` racine et le `README.md` du monorepo, ce service est destiné à porter les intégrations avec les plateformes externes de l'écosystème XpertSphere (rôle prévu : « Plateformes externes »).

**État actuel du code : squelette généré, aucune intégration implémentée.** Le contenu de `Program.cs` est celui du template par défaut ASP.NET Core (Minimal API) : un endpoint `GET /weatherforecast` de démonstration, un health check exposé sur `/health`, et Swagger/OpenAPI activés en développement. Aucune logique métier, aucun appel vers un service externe, aucune persistance de données n'est encore implémenté. Un futur agent qui reprend ce projet part donc d'une base vide à construire, pas d'un service partiellement fonctionnel.

## Stack technique

- **.NET 9** (`net9.0`), `Nullable` et `ImplicitUsings` activés (`XpertSphere.IntegrationService.csproj`)
- **ASP.NET Core Minimal API** (top-level statements dans `Program.cs`, pas de contrôleurs)
- **Microsoft.AspNetCore.OpenApi** (9.0.6) + **Swashbuckle.AspNetCore** (7.2.0) pour la documentation OpenAPI/Swagger
- Health checks ASP.NET Core natifs (`AddHealthChecks()` / `MapHealthChecks("/health")`)
- Conteneurisation : image `mcr.microsoft.com/dotnet/aspnet:9.0-alpine` en runtime, build via `mcr.microsoft.com/dotnet/sdk:9.0-alpine` (voir `docker/backend/integration-service/Dockerfile` à la racine du monorepo)

Aucun package d'accès aux données (Entity Framework, Dapper, etc.), aucun client HTTP typé, aucune bibliothèque de messagerie (Service Bus, etc.) n'est référencé à ce jour dans le `.csproj`.

## Structure du projet

```
XpertSphere.IntegrationService/
├── Program.cs                              # Point d'entrée, encore au stade template (weatherforecast + health check)
├── Properties/launchSettings.json          # Profils de lancement (http/https), port par défaut 5003/7003
├── XpertSphere.IntegrationService.csproj   # net9.0, OpenApi + Swashbuckle
├── XpertSphere.IntegrationService.http     # Requêtes HTTP de test manuel (fichier .http)
├── appsettings.json                        # Contient une chaîne de connexion SQL Server (placeholder, non consommée par le code)
└── appsettings.Development.json            # Overrides de logging pour l'environnement Development
```

Il n'existe pas encore de dossiers `Controllers/`, `Services/`, `Models/`, `Endpoints/` ou équivalents : l'arborescence reste à créer selon les besoins des futures intégrations.

Point d'attention : `appsettings.json` définit une `ConnectionStrings:DefaultConnection` (SQL Server, via variables `${DB_SERVER}`, `${DB_NAME}`, `${DB_USER}`, `${DB_PASSWORD}`) mais rien dans `Program.cs` ne l'utilise actuellement (pas de `DbContext`, pas de package EF Core référencé). C'est un placeholder hérité du template de service, pas une intégration active.

## CI / Docker

En développement, Swagger UI est disponible (route par défaut `/swagger`) et le endpoint OpenAPI brut via `MapOpenApi()`.

La CI (`.github/workflows/backend-integration-service.yml`) exécute, sur les pull requests touchant ce dossier : `dotnet restore`, `dotnet build --no-restore`, puis `dotnet test --no-build`. Sur push vers `main`/`develop`, elle construit et publie l'image Docker (`docker/backend/integration-service/Dockerfile`) vers l'Azure Container Registry `acrxpertspheredev` sous le nom `integration-service`.

Ce service n'est pour l'instant **pas déclaré dans `docker-compose.yml`** à la racine du monorepo : il n'est donc pas lancé automatiquement par `docker-compose up` en local.

## Conventions de code observées

Le code présent (template par défaut) suit les conventions standards ASP.NET Core :

- Minimal API avec top-level statements, pas de classe `Startup` séparée
- `Nullable` et `ImplicitUsings` activés au niveau du projet
- Un `record` (`WeatherForecast`) pour représenter une donnée immuable, conforme à la préférence pour les records/DTOs immuables mentionnée dans le `CONTRIBUTING.md` racine

Les standards de code plus larges décrits dans le `CONTRIBUTING.md` du monorepo (FluentValidation, MediatR pour CQRS, xUnit + Moq + FluentAssertions pour les tests, couverture minimale 85 % pour le backend .NET) sont les conventions **cibles du repo dans son ensemble** ; elles ne sont pas encore appliquées dans ce projet faute de code métier à valider. À suivre dès l'ajout de la première fonctionnalité réelle.

## Dépendances / intégrations externes observées dans le code

Aucune intégration externe n'est implémentée à ce jour. Concrètement, dans le code actuel :

- Pas d'appel HTTP sortant vers un service tiers
- Pas de client vers Azure Service Bus, Redis, Blob Storage ou tout autre composant Azure mentionné dans `INFRASTRUCTURE.md`
- Pas de package d'accès aux données malgré la chaîne de connexion SQL Server présente dans `appsettings.json` (voir remarque plus haut)
- Les seules dépendances externes réelles sont des outils de développement : `Microsoft.AspNetCore.OpenApi` et `Swashbuckle.AspNetCore` pour la documentation d'API
