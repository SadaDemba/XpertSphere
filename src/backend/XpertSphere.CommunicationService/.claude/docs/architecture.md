# Architecture — XpertSphere.CommunicationService

## Rôle et état actuel

`XpertSphere.CommunicationService` est un des microservices backend de la plateforme XpertSphere (ATS de recrutement). D'après le README racine du monorepo, son rôle prévu est la gestion des **notifications et des emails** ("Communication centralisée" : notifications temps réel, emails automatiques, modèles de messages personnalisables). Il fait partie d'une architecture hybride aux côtés de `XpertSphere.MonolithApi` (cœur métier), `XpertSphere.ReportingService`, `XpertSphere.IntegrationService` (plateformes externes) et `XpertSphere.ResumeAnalyzer` (Python/FastAPI).

**État actuel du code** : le service est à un stade de scaffolding précoce. On y trouve des modèles de données et des interfaces de service, mais aucune implémentation concrète de ces interfaces, aucun contrôleur, et `Program.cs` correspond encore au template par défaut ASP.NET Core (endpoint `/weatherforecast` de démonstration). Un futur agent devra probablement implémenter les services d'envoi d'email/notification et brancher un provider réel (SMTP, Azure Communication Services, etc.), rien de tout cela n'est câblé pour l'instant.

## Stack technique

- **.NET 9** (`TargetFramework: net9.0`), `Nullable` et `ImplicitUsings` activés.
- SDK : `Microsoft.NET.Sdk.Web` (application ASP.NET Core Web API minimal hosting, style `WebApplication.CreateBuilder`).
- Packages NuGet actuellement référencés (voir `.csproj`) :
  - `Microsoft.AspNetCore.OpenApi` (9.0.6)
  - `Swashbuckle.AspNetCore` (7.2.0) pour Swagger/Swagger UI en environnement Development.
- Aucun package d'envoi d'email (MailKit, SendGrid, etc.), de messagerie (Azure Service Bus) ou d'accès aux données n'est référencé pour l'instant, malgré ce que suggèrent les schémas de déploiement du mémoire (Azure Service Bus, Azure Email Communication Service, Azure SignalR) : ces éléments relèvent de l'architecture cible documentée, pas du code présent.

## Structure du projet

```
XpertSphere.CommunicationService/
├── Program.cs                          # Point d'entrée ; encore le template par défaut (weatherforecast, /health)
├── Models/
│   ├── EmailMessage.cs                 # Message email : destinataires, pièces jointes, statut, priorité, retry
│   ├── MessageTemplate.cs              # Modèle de message (TemplateType : WelcomeCandidate, InterviewInvitation, etc.)
│   └── NotificationMessage.cs          # Notification (Type, Channel : InApp/Email/Push/SMS/All, priorité)
├── Services/
│   └── Interfaces/
│       ├── IEmailService.cs            # Envoi email (unitaire, bulk, templated), statut, annulation, retry
│       ├── INotificationService.cs     # Envoi/lecture des notifications utilisateur
│       └── ITemplateService.cs         # CRUD et rendu de templates de message (multi-langue, défaut fr-FR)
├── Properties/launchSettings.json      # Profils de lancement (http/https)
├── appsettings.json                    # Config par défaut (chaînes de connexion SQL Server + Redis via variables d'env)
├── appsettings.Development.json        # Config dev (endpoints Kestrel)
└── XpertSphere.CommunicationService.http  # Requêtes HTTP de test manuel (endpoint /weatherforecast uniquement)
```

Il n'y a pas encore de dossier `Controllers/`, `Extensions/`, `Data/` ni de projet de tests dédié (contrairement à `XpertSphere.MonolithApi`, qui a son propre `XpertSphere.MonolithApi.Tests` référencé dans `XpertSphere.sln`).

## Conventions de code observées

- Namespaces à la file-scoped (`namespace XpertSphere.CommunicationService.Models;`) plutôt qu'avec accolades.
- Séparation claire interfaces (`Services/Interfaces/`) / implémentations (dossier `Services/` à la racine, pas encore peuplé).
- Modèles en classes avec propriétés auto-implémentées et valeurs par défaut inline (ex. `Guid.NewGuid().ToString()`, `DateTime.UtcNow`, enums pour les statuts/priorités/types).
- Enums utilisés systématiquement pour les états et catégories fermées (`EmailStatus`, `EmailPriority`, `TemplateType`, `NotificationType`, `NotificationChannel`, `NotificationPriority`).
- Toutes les méthodes de service sont asynchrones (`Task<...>`) et acceptent un `CancellationToken cancellationToken = default` en dernier paramètre.
- Langue par défaut des templates : `fr-FR` (paramètre par défaut dans `ITemplateService`).

## Dépendances externes

Aucun provider SMTP, service d'email transactionnel (SendGrid, Azure Communication Services) ou bus de messages n'est câblé dans le code à ce stade : seules des interfaces et modèles existent. Les chaînes de connexion présentes dans `appsettings.json` concernent une base SQL Server et un cache Redis (probablement partagés avec les autres microservices XpertSphere), mais aucun `DbContext` ni client Redis n'est référencé dans le projet pour l'instant.
