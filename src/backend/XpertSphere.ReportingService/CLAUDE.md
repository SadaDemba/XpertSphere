# CLAUDE.md — XpertSphere.ReportingService

## Aperçu du service

`XpertSphere.ReportingService` est l'un des services spécialisés qui gravitent autour du noyau monolithique (`XpertSphere.MonolithApi`) dans l'architecture hybride de XpertSphere. D'après le README racine du monorepo et le chapitre 3 du mémoire (`doc/memoire/04-Chapitre-3-conception-architecture.md`), son rôle prévu est de fournir les fonctionnalités d'analytics et de reporting à destination des recruteurs (tableaux de bord, métriques de recrutement).

**État actuel du code** : à ce jour, le projet est encore au stade de squelette généré par le template ASP.NET Web API par défaut. `Program.cs` ne contient que la configuration Swagger/OpenAPI et l'endpoint d'exemple `GET /weatherforecast` fourni par le template ; aucune logique de reporting, aucun contrôleur, service ou modèle métier n'est encore implémenté. Il n'y a pas non plus de dossiers `Controllers/`, `Services/` ou `Models/` créés (contrairement à `XpertSphere.CommunicationService`, qui a déjà cette structure en place). Un futur agent travaillant ici devra donc construire les fonctionnalités de reporting depuis zéro, en s'inspirant si besoin des conventions déjà adoptées dans `XpertSphere.MonolithApi` et `XpertSphere.CommunicationService`.

## Stack technique

- **.NET 9** (`TargetFramework` = `net9.0`), SDK `Microsoft.NET.Sdk.Web`
- Nullable reference types activé (`<Nullable>enable</Nullable>`)
- Implicit usings activé (`<ImplicitUsings>enable</ImplicitUsings>`)
- Packages NuGet référencés (`XpertSphere.ReportingService.csproj`) :
  - `Microsoft.AspNetCore.OpenApi` (9.0.6)
  - `Swashbuckle.AspNetCore` (7.2.0) — génération Swagger/OpenAPI et UI Swagger en développement
- Style d'API : Minimal API (top-level statements dans `Program.cs`), pas encore de contrôleurs

## Structure du projet

```
XpertSphere.ReportingService/
├── Program.cs                              # Point d'entrée, endpoint de démo /weatherforecast
├── Properties/
│   └── launchSettings.json                 # Profils de lancement (http/https)
├── appsettings.json                        # Configuration par défaut (Logging, AllowedHosts)
├── appsettings.Development.json            # Configuration développement (Logging)
├── XpertSphere.ReportingService.csproj     # Fichier projet (.NET 9, Web SDK)
└── XpertSphere.ReportingService.http       # Requêtes HTTP d'exemple (REST Client)
```

Le projet fait partie de la solution `XpertSphere.sln` (à la racine de `src/backend/`), aux côtés de `XpertSphere.MonolithApi`, `XpertSphere.CommunicationService` et `XpertSphere.IntegrationService`.

## Commandes de build / test / run

Depuis le dossier du projet (`src/backend/XpertSphere.ReportingService/`) :

```bash
dotnet restore
dotnet build
dotnet run                # profil "http" par défaut, écoute sur http://localhost:5004
dotnet run --launch-profile https   # écoute sur https://localhost:7004 et http://localhost:5004
```

Il n'existe pas encore de projet de tests dédié à `XpertSphere.ReportingService` (contrairement à `XpertSphere.MonolithApi.Tests` pour le monolithe). `dotnet test` n'a donc rien à exécuter pour ce service à l'heure actuelle.

**Point d'attention environnement** : le SDK `dotnet` par défaut détecté dans cet environnement peut résoudre vers .NET 8 (`dotnet@8` dans le PATH Homebrew) alors que le projet cible `net9.0`. Un SDK .NET 9 est disponible en parallèle sur la machine (Homebrew `dotnet` classique) ; en cas d'erreur `NETSDK1045`, vérifier quel `dotnet` est utilisé (`which dotnet`, `dotnet --list-sdks`) et au besoin sélectionner le SDK 9 (`global.json` ou variable `PATH`).

Le fichier `XpertSphere.ReportingService.http` référence `http://localhost:5164` (port différent de `launchSettings.json`) et documente une requête `GET /weatherforecast/` — utile comme exemple de format de requêtes REST Client, pas comme source de vérité sur le port réel.

Aucune entrée dédiée à `XpertSphere.ReportingService` n'existe dans `docker-compose.yml` à la racine du monorepo : celui-ci ne définit que l'infrastructure partagée (SQL Server, Redis, Adminer). Le service n'est donc pas encore conteneurisé.

## Conventions de code observées

- Minimal API avec top-level statements dans `Program.cs` (pas de classe `Startup` séparée).
- Enregistrement des services suit l'ordre : OpenAPI/Swagger d'abord, puis `var app = builder.Build();`, puis pipeline HTTP (`MapOpenApi`, `UseSwagger`, `UseSwaggerUI` uniquement en environnement `Development`), puis `UseHttpsRedirection`, puis mapping des endpoints, puis `app.Run()`.
- Les endpoints minimal API utilisent `.WithName(...)` pour nommer l'opération (visible dans Swagger).
- Les modèles simples sont déclarés en `record` (voir `WeatherForecast` en fin de `Program.cs`).
- Nullable reference types activé : utiliser des types annotés (`string?`, etc.) plutôt que de désactiver les avertissements.
- Ce projet ne comporte pas encore de health check (`AddHealthChecks()` / `MapHealthChecks`) alors que `XpertSphere.CommunicationService` en a un sur `/health` : à envisager par cohérence si le service est complété.

## Sources de données / dépendances observées

- Aucune chaîne de connexion à une base de données n'est configurée dans `appsettings.json` ou `appsettings.Development.json` pour ce service (contrairement au monolithe, qui utilise une chaîne de connexion SQL Server paramétrée par variables d'environnement `DB_SERVER`, `DB_NAME`, `DB_USER`, `DB_PASSWORD`).
- Aucun package d'accès aux données (Entity Framework Core, Dapper, etc.) n'est référencé dans le `.csproj`.
- Aucune dépendance à Redis, à un bus de messages (Azure Service Bus) ou à un client HTTP vers `XpertSphere.MonolithApi` n'est présente dans le code à ce stade, bien que l'architecture documentée dans le mémoire prévoie une communication asynchrone entre services via Azure Service Bus.
- `AllowedHosts` est à `"*"` dans `appsettings.json` (valeur par défaut du template, non restreinte).

## Spécifications

_À compléter : les spécifications fonctionnelles de ce service seront ajoutées ici au fur et à mesure._
