# Instructions Claude — XpertSphere.IntegrationService

## Aperçu du service

D'après le `CLAUDE.md` racine et le `README.md` du monorepo, ce service est destiné à porter les intégrations avec les plateformes externes de l'écosystème XpertSphere (rôle prévu : « Plateformes externes »). **État actuel du code : squelette généré, aucune intégration implémentée** — `Program.cs` reste au stade du template ASP.NET Core par défaut (endpoint `weatherforecast`, health check `/health`, Swagger en développement), sans logique métier ni appel externe. Stack : **.NET 9** / ASP.NET Core Minimal API, avec Microsoft.AspNetCore.OpenApi + Swashbuckle pour la documentation OpenAPI/Swagger.

## Documentation

- Architecture, structure détaillée du projet, conventions de code et points d'attention (écart rôle prévu / code réel, CI, Docker) : voir [`.claude/docs/architecture.md`](.claude/docs/architecture.md)
- Spécifications fonctionnelles (une par fonctionnalité) : voir [`.claude/specifications/`](.claude/specifications/) (vide pour l'instant)

## Commandes

Depuis ce dossier (`src/backend/XpertSphere.IntegrationService/`) :

```bash
dotnet restore
dotnet build
dotnet run                     # démarre sur http://localhost:5003 (profil "http", voir launchSettings.json)
dotnet watch run               # rechargement à chaud en développement
dotnet test                    # exécuté par la CI, mais aucun projet de tests n'existe pour ce service à ce jour
```

Le projet cible **.NET 9** : un SDK .NET 9 est requis (vérifiable avec `dotnet --list-sdks`). Sur une machine n'ayant que le SDK .NET 8 installé, `dotnet build`/`dotnet run` échouent avec l'erreur `NETSDK1045`.
