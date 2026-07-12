# Instructions Claude — XpertSphere.CommunicationService

## Aperçu du service

`XpertSphere.CommunicationService` est un des microservices backend de la plateforme XpertSphere (ATS de recrutement). D'après le README racine du monorepo, son rôle prévu est la gestion des **notifications et des emails** ("Communication centralisée" : notifications temps réel, emails automatiques, modèles de messages personnalisables).

**État actuel du code** : le service est à un stade de scaffolding précoce (modèles et interfaces de service présents, mais aucune implémentation, aucun contrôleur, et `Program.cs` encore au template ASP.NET Core par défaut).

Stack technique en bref : .NET 9, ASP.NET Core Web API (`Microsoft.NET.Sdk.Web`), Swagger/Swashbuckle en Development, aucun package d'envoi d'email/messagerie/accès aux données référencé pour l'instant.

## Documentation

- Architecture, structure détaillée, conventions de code et points d'attention *à charger que si nécessaire (lazy-loading)* : voir [`.claude/docs/architecture.md`](.claude/docs/architecture.md). 
- Spécifications fonctionnelles *à charger que si nécessaire (lazy-loading)* (une par fonctionnalité) : voir [`.claude/specifications/`](.claude/specifications/) (vide pour l'instant).

## Commandes de build / run / test

Depuis le dossier du service :

```bash
dotnet restore
dotnet build
dotnet run                 # démarre sur les profils définis dans launchSettings.json
```

Profils `launchSettings.json` :
- `http` : `http://localhost:5002`
- `https` : `https://localhost:7002` (+ `http://localhost:5002`)
- `ASPNETCORE_ENVIRONMENT=Development` dans les deux profils.

En Development, `appsettings.Development.json` fixe aussi Kestrel sur les mêmes ports (5002/7002). Le fichier `.http` référence en revanche un autre port (`5193`), probablement un profil IIS Express implicite non présent dans `launchSettings.json` : à vérifier/corriger si utilisé.

Swagger UI est disponible en Development (`app.UseSwaggerUI()`), ainsi qu'un endpoint de santé `/health` (`AddHealthChecks()` / `MapHealthChecks`).

Aucune commande `dotnet test` n'est applicable : il n'existe pas de projet de tests pour ce service à ce jour.

Variables d'environnement attendues (déduites de `appsettings.json`, sans lecture de fichier `.env`) : `DB_SERVER`, `DB_NAME`, `DB_USER`, `DB_PASSWORD` (SQL Server) et `REDIS_HOST`, `REDIS_PORT`, `REDIS_PASSWORD` (Redis). Noter que la substitution `${VAR}` n'est pas un mécanisme natif de `appsettings.json` en .NET : elle suppose un traitement externe (ex. docker-compose ou script de démarrage) non présent dans ce dossier.
