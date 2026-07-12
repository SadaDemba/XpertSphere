# CLAUDE.md — XpertSphere.ReportingService

## Aperçu du service

`XpertSphere.ReportingService` est l'un des services spécialisés qui gravitent autour du noyau monolithique (`XpertSphere.MonolithApi`) dans l'architecture hybride de XpertSphere. D'après le README racine du monorepo et le chapitre 3 du mémoire (`doc/memoire/04-Chapitre-3-conception-architecture.md`), son rôle prévu est de fournir les fonctionnalités d'analytics et de reporting à destination des recruteurs (tableaux de bord, métriques de recrutement).

**État actuel du code** : à ce jour, le projet est encore au stade de squelette généré par le template ASP.NET Web API par défaut, sans logique de reporting, contrôleur, service ou modèle métier implémenté.

Stack technique en bref : .NET 9 (Minimal API, `Microsoft.NET.Sdk.Web`), Swagger/OpenAPI via Swashbuckle, pas encore de base de données ni de dépendances externes configurées.

## Documentation

- Structure détaillée, conventions de code et points d'attention : voir `.claude/docs/architecture.md`
- Spécifications fonctionnelles : voir `.claude/specifications/` (vide pour l'instant)

## Commandes

Depuis le dossier du projet (`src/backend/XpertSphere.ReportingService/`) :

```bash
dotnet restore
dotnet build
dotnet run                # profil "http" par défaut, écoute sur http://localhost:5004
dotnet run --launch-profile https   # écoute sur https://localhost:7004 et http://localhost:5004
```

Il n'existe pas encore de projet de tests dédié à `XpertSphere.ReportingService` (contrairement à `XpertSphere.MonolithApi.Tests` pour le monolithe). `dotnet test` n'a donc rien à exécuter pour ce service à l'heure actuelle.
