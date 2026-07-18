# Fondation : envoi d'email templatisé synchrone (ticket A)

## Contexte et périmètre

Cette spec est le **ticket A** d'une fonctionnalité plus large ("système d'envoi de mail générique"), découpée en 2 tickets :
- **Ticket A (cette spec)** : rendre `XpertSphere.CommunicationService` capable d'envoyer un email templatisé de façon **synchrone**, et rendre le service **réellement exécutable** (il ne l'est pas aujourd'hui, voir diagnostic ci-dessous). Aucun consommateur n'appelle ce service à l'issue de ce ticket.
- **Ticket B (hors périmètre, spec séparée à écrire plus tard)** : brancher la création de compte du `MonolithApi` sur l'endpoint créé ici, pour déclencher l'email d'activation (génération réelle du lien/token, appel HTTP depuis `MonolithApi`). Le template `AccountActivation` est seedé ici par anticipation, mais son usage réel n'est pas dans ce ticket.

## Diagnostic — état actuel du service

- Scaffolding précoce : `Program.cs` est encore le template ASP.NET Core par défaut (`/weatherforecast`, `AddOpenApi`, Swashbuckle, `/health`). Aucun `Controllers/`, aucune implémentation de service, aucun package d'email dans le `.csproj` (seulement `Microsoft.AspNetCore.OpenApi` + `Swashbuckle.AspNetCore`).
- `Models/EmailMessage.cs`, `Models/MessageTemplate.cs` : modèles déjà scaffoldés, riches (statut, priorité, planification, retry, pièces jointes) mais **aucune implémentation** ne les consomme.
- `Services/Interfaces/IEmailService.cs` et `Services/Interfaces/ITemplateService.cs` : interfaces scaffoldées prévoyant respectivement l'envoi (unitaire/bulk/templatisé/statut/annulation/retry) et un CRUD complet de templates + rendu. **Aucune implémentation, aucun appelant existant** — donc aucune rupture de contrat à craindre en les adaptant (voir §Décisions).
- `Services/Interfaces/INotificationService.cs` : notifications in-app, **hors périmètre total** de ce ticket, non touché.
- `appsettings.json` référence des chaînes de connexion SQL Server + Redis via variables d'environnement, mais **aucun `DbContext` ni client Redis n'est câblé**. Ce ticket ne les câble pas non plus (voir décision templates).
- `docker/backend/communication-service/Dockerfile` existe déjà (build .NET 9 alpine multi-stage, expose 8080/8081, healthcheck sur `/health`) **mais contient un bug qui casse le build dans le contexte où ce ticket va l'utiliser** (voir juste en dessous). Le service n'est **pas présent** dans `docker-compose.yml` racine.

### Bug bloquant découvert dans `docker/backend/communication-service/Dockerfile` — à corriger dans ce ticket

Ligne 10 :
```dockerfile
COPY ["XpertSphere.sln", "./"]
```
Le fichier solution est en réalité à `src/backend/XpertSphere.sln` (vérifié : `find` ne le trouve qu'à cet emplacement). Le Dockerfile de `monolith-api` (qui sert de modèle explicite pour ce ticket, décision §6) utilise correctement `COPY ["src/backend/XpertSphere.sln", "./"]` avec `context: .` (racine du monorepo) dans `docker-compose.yml`. Avec le chemin actuel, `docker compose up` sur `communication-service` échouerait dès l'étape `COPY` (fichier introuvable dans le contexte de build).

Correctif retenu dans ce ticket : **supprimer la ligne 10**, plutôt que corriger son chemin. Justification : aucune des étapes suivantes du Dockerfile (`dotnet restore`, `dotnet build`, `dotnet publish`, lignes 13/18/22) ne référence le `.sln` — toutes ciblent directement le `.csproj`. Copier le fichier solution ne sert donc à rien dans ce Dockerfile précis. Le Dockerfile de `monolith-api` copie lui aussi son `.sln` (avec le bon chemin, `src/backend/XpertSphere.sln`) sans non plus s'en servir dans ses étapes `restore`/`build`/`publish` (qui ciblent également directement les `.csproj`) — c'est donc un vestige déjà présent là-bas aussi, mais corriger ce point sur `monolith-api` est hors périmètre de cette spec. Alternative documentée mais non retenue ici : corriger le chemin en `src/backend/XpertSphere.sln` pour rester visuellement cohérent avec `monolith-api` — acceptable également, au choix du développeur si une cohérence stricte entre Dockerfiles est jugée préférable ; mais la suppression est la correction minimale et suffisante.

## Décisions déjà validées (à documenter comme telles, ne pas rouvrir)

1. **Portée fonctionnelle minimale** : seul le chemin synchrone `SendEmailAsync`/`SendTemplatedEmailAsync` est implémenté, avec retour synchrone succès/échec à l'appelant HTTP. `SendBulkEmailsAsync`, `GetEmailStatusAsync`, `CancelScheduledEmailAsync`, `RetryFailedEmailAsync` sont **retirés de `IEmailService`** dans ce ticket (pas de persistance, pas de worker d'arrière-plan, aucun besoin actuel — ces méthodes n'ont jamais été appelées). Extension future non garantie, à réintroduire seulement si un besoin réel apparaît (ex. envois en masse pour des campagnes).
2. **Templates en code, pas de DB** : `ITemplateService` prévoyait un CRUD complet supposant une persistance. Décision : pas de `DbContext` dans ce ticket. `CreateTemplateAsync`, `UpdateTemplateAsync`, `DeleteTemplateAsync`, `GetAllTemplatesAsync` sont **retirés de `ITemplateService`**. Un seul template est seedé en dur : `AccountActivation` (`fr-FR`), avec une variable `ActivationLink`. Aucun autre `TemplateType` n'est seedé (pas de développement spéculatif).
3. **Provider SMTP** :
   - **Development** : catcher SMTP local **smtp4dev** (image `rnwood/smtp4dev:v3`), retenu plutôt que MailHog — MailHog est un projet archivé/non maintenu, smtp4dev est activement maintenu, propose une UI web plus complète (visualisation MIME brute, recherche) et est largement utilisé dans l'écosystème .NET. Aucun email n'est réellement envoyé à l'extérieur en dev ; UI web pour consultation, même principe qu'Azurite pour le stockage blob.
   - **Staging** (et potentiellement Production plus tard, non tranché ici) : **Brevo** (ex-Sendinblue) comme provider SMTP réel, décision figée par l'utilisateur — offre gratuite à vie (300 emails/jour, sans carte bancaire), relais SMTP standard : host `smtp-relay.brevo.com`, port `587` (STARTTLS), credentials = login SMTP + clé SMTP générés manuellement depuis un compte Brevo (étape hors du périmètre du code, comme l'obtention d'une clé Groq/Azure OpenAI pour `ResumeAnalyzer`). Aucun secret Brevo réel n'est committé dans le repo.
   - Configuration SMTP (host/port/credentials) pilotée entièrement par `appsettings.{Environment}.json` + variables d'environnement, jamais codée en dur dans le code applicatif — le catcher local n'est qu'une *valeur de configuration* parmi d'autres, pas un branchement conditionnel par code.
4. **Endpoint générique** : `POST /api/emails/send`, acceptant nom de template + destinataire + données de template, pour que le ticket B (futur) puisse l'appeler en HTTP synchrone depuis `MonolithApi`. Aucun appelant construit dans ce ticket.
5. **Sécurité minimale pragmatique** : clé partagée via header `X-Api-Key`, vérifiée contre une valeur configurée. Documenté explicitement comme **non définitif** — une authentification inter-services plus robuste (mTLS, Entra ID service-à-service) est un sujet séparé, non traité ici.
6. **docker-compose** : ajout de `communication-service` (sur le modèle de l'entrée `monolith-api`) et du catcher SMTP, sur `xpertsphere-network`, ports configurables par variable d'environnement avec valeur par défaut.

## Comportement cible

### 1. `XpertSphere.CommunicationService.csproj` — nouveau package

Ajouter le package **MailKit** (dernière version stable 4.x — le développeur pin la version exacte via `dotnet add package MailKit`). MailKit est retenu plutôt que `System.Net.Mail.SmtpClient` (déprécié par Microsoft pour du nouveau code) et est le choix standard .NET pour SMTP, supportant nativement STARTTLS (nécessaire pour Brevo sur le port 587). Aucun autre package n'est nécessaire (`Microsoft.Extensions.Options` fait partie du shared framework ASP.NET Core, pas de `PackageReference` séparé).

### 2. Adaptation des interfaces existantes

#### `Services/Interfaces/IEmailService.cs`

```csharp
public interface IEmailService
{
    // Retourne true si l'envoi SMTP a réussi. En cas d'échec, une exception
    // (EmailSendException) est levée — cette méthode ne retourne jamais false :
    // c'est une simplification assumée du contrat bool d'origine, ambigu.
    Task<bool> SendEmailAsync(EmailMessage message, CancellationToken cancellationToken = default);

    // Retourne le MessageId de l'EmailMessage effectivement envoyé.
    // Lève TemplateNotFoundException / MissingTemplateVariableException (échec de rendu, mappé 400 côté controller)
    // ou EmailSendException (échec SMTP, mappé 502 côté controller).
    Task<string> SendTemplatedEmailAsync(
        string templateName,
        string to,
        Dictionary<string, string> templateData,
        string language = "fr-FR",
        CancellationToken cancellationToken = default);
}
```

Changements par rapport à l'interface scaffoldée : suppression de `SendBulkEmailsAsync`, `GetEmailStatusAsync`, `CancelScheduledEmailAsync`, `RetryFailedEmailAsync` (voir décision §1) ; ajout du paramètre `language` sur `SendTemplatedEmailAsync` (cohérent avec `ITemplateService.GetTemplateAsync`, absent de la version scaffoldée) ; changement du type de retour de `SendTemplatedEmailAsync` de `Task<bool>` à `Task<string>` (le `MessageId`, plus utile à l'appelant HTTP qu'un simple booléen, sans complexifier avec une classe `Result` dédiée).

#### `Services/Interfaces/ITemplateService.cs`

```csharp
public interface ITemplateService
{
    Task<MessageTemplate?> GetTemplateAsync(string templateName, string language = "fr-FR", CancellationToken cancellationToken = default);

    // Valide l'existence du template et la présence des variables requises (template.Variables),
    // puis retourne le sujet/corps rendus (placeholders substitués).
    Task<RenderedTemplate> RenderTemplateAsync(string templateName, Dictionary<string, string> data, string language = "fr-FR", CancellationToken cancellationToken = default);
}
```

Changements : suppression de `CreateTemplateAsync`, `UpdateTemplateAsync`, `DeleteTemplateAsync`, `GetAllTemplatesAsync` (CRUD nécessitant une persistance, voir décision §2) ; `RenderTemplateAsync` change de type de retour (`Task<string>` → `Task<RenderedTemplate>`) car un email a besoin d'un sujet **et** d'un corps rendus séparément, pas d'une seule chaîne.

Nouvelle classe `RenderedTemplate` (ajoutée à la suite de l'interface dans le même fichier, ou dans `Models/MessageTemplate.cs` à la suite de `MessageTemplate`/`TemplateType` — au choix du développeur, convention déjà suivie par `EmailMessage.cs` qui regroupe `EmailMessage`/`EmailAttachment`/enums dans un seul fichier) :
```csharp
public class RenderedTemplate
{
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsHtml { get; set; } = true;
}
```
`IsHtml` est fixé à `true` dans ce ticket (le seul template seedé est HTML) — `MessageTemplate` n'est pas modifié pour ajouter un champ `IsHtml` propre, une future extension pourra le faire si des templates texte brut deviennent nécessaires.

### 3. Nouvelles exceptions

Fichier `Exceptions/EmailExceptions.cs` (nouveau dossier) :
```csharp
public class TemplateNotFoundException(string templateName, string language)
    : Exception($"Template '{templateName}' not found for language '{language}'.");

public class MissingTemplateVariableException(string templateName, IEnumerable<string> missingVariables)
    : Exception($"Template '{templateName}' is missing required variable(s): {string.Join(", ", missingVariables)}.");

public class EmailSendException(string message, Exception innerException)
    : Exception(message, innerException);
```
(Syntaxe de constructeur primaire C# 12, cohérente avec .NET 9 ; le développeur peut utiliser des constructeurs classiques si préféré, le contrat public — nom, message, type d'exception — est ce qui compte pour le mapping HTTP.)

### 4. `Services/TemplateService.cs` (nouvelle implémentation)

- Stockage : dictionnaire statique en mémoire, initialisé au démarrage (dans le constructeur ou un champ `static readonly`), clé = `(Name, Language)`. Un seul template seedé :

| Champ | Valeur |
|---|---|
| `Name` | `"AccountActivation"` |
| `Language` | `"fr-FR"` |
| `Type` | `TemplateType.AccountActivation` |
| `Subject` | `"Activez votre compte XpertSphere"` |
| `Body` (HTML, placeholder générique réaliste) | `"<p>Bonjour,</p><p>Merci de votre inscription sur XpertSphere. Pour activer votre compte, cliquez sur le lien ci-dessous :</p><p><a href=\"{{ActivationLink}}\">Activer mon compte</a></p><p>Si vous n'êtes pas à l'origine de cette demande, ignorez cet email.</p>"` |
| `Variables` | `{ "ActivationLink": "Lien absolu vers la page d'activation du compte" }` |
| `IsActive` | `true` |
| `Category` | `"Account"` |

- Syntaxe de placeholder retenue : `{{NomVariable}}` (double accolades), substitution par simple `string.Replace("{{" + key + "}}", value)` pour chaque entrée de `templateData`. **Aucun moteur de templating externe** (Scriban, Handlebars.NET, RazorLight) n'est ajouté — un seul template avec une seule variable ne justifie pas cette dépendance. Point d'extension documenté si les templates gagnent en complexité (conditions, boucles) plus tard.
- **Point d'attention sécurité, à ne pas oublier si le périmètre s'étend** : les valeurs de `templateData` sont insérées telles quelles dans un corps HTML, **sans encodage HTML**. Dans ce ticket, la seule variable (`ActivationLink`) est une URL générée côté serveur (pas une saisie libre d'utilisateur final), le risque est donc jugé négligeable ici. Si un futur template accepte une variable de texte libre potentiellement fournie par un utilisateur non fiable, l'encodage HTML (`System.Net.WebUtility.HtmlEncode`) devra être ajouté à ce moment — non fait ici pour ne pas complexifier un besoin qui n'existe pas encore.
- `GetTemplateAsync(name, language)` : retourne le template du dictionnaire ou `null` si absent.
- `RenderTemplateAsync(name, data, language)` : appelle `GetTemplateAsync` ; si `null`, lève `TemplateNotFoundException` ; calcule les clés de `template.Variables` absentes de `data`, lève `MissingTemplateVariableException` si non vide ; substitue les placeholders dans `Subject`/`Body` ; retourne un `RenderedTemplate`.
- Durée de vie DI : `Singleton` (dictionnaire statique en lecture seule après initialisation, thread-safe).

### 5. `Services/EmailService.cs` (nouvelle implémentation)

- Dépendances injectées : `ITemplateService`, `IOptions<SmtpOptions>`, `ILogger<EmailService>`.
- `SendTemplatedEmailAsync` :
  1. `var rendered = await _templateService.RenderTemplateAsync(templateName, templateData, language, ct);` (laisse remonter `TemplateNotFoundException`/`MissingTemplateVariableException`).
  2. Construit un `EmailMessage { To = to, Subject = rendered.Subject, Body = rendered.Body, IsHtml = rendered.IsHtml, TemplateName = templateName, TemplateData = templateData }` (les autres champs — `Priority`, `ScheduledFor`, `Attachments`, `Cc`, `Bcc` — restent aux valeurs par défaut, non utilisés dans ce ticket).
  3. Appelle `await SendEmailAsync(message, ct);`.
  4. Retourne `message.MessageId`.
- `SendEmailAsync` : construit un `MimeMessage`/`MimeKit.BodyBuilder` (HTML si `message.IsHtml`, texte sinon) à partir de `EmailMessage`, `From` = `SmtpOptions.From`, `To` = `message.To` (destinataire unique — `EmailMessage.To` est une chaîne simple, pas une liste ; multi-destinataires via `Cc`/`Bcc` non exercé dans ce ticket). Ouvre une connexion MailKit `SmtpClient` **par envoi** (pas de client SMTP partagé/long-lived, pour éviter les problèmes de réutilisation de connexion) :
  ```csharp
  using var client = new MailKit.Net.Smtp.SmtpClient();
  var secureOptions = smtpOptions.EnableSsl
      ? MailKit.Security.SecureSocketOptions.StartTls
      : MailKit.Security.SecureSocketOptions.None;
  await client.ConnectAsync(smtpOptions.Host, smtpOptions.Port, secureOptions, cancellationToken);
  if (!string.IsNullOrEmpty(smtpOptions.Username))
  {
      await client.AuthenticateAsync(smtpOptions.Username, smtpOptions.Password, cancellationToken);
  }
  await client.SendAsync(mimeMessage, cancellationToken);
  await client.DisconnectAsync(true, cancellationToken);
  ```
  Toute exception MailKit (connexion refusée, timeout, échec d'authentification) est capturée et relevée sous forme d'`EmailSendException("Failed to send email via SMTP", ex)` — pas de retry automatique (hors périmètre, décision §1). Retourne `true` si aucune exception.
- Durée de vie DI : `Scoped` (ou `Transient` — pas d'état partagé mutable, chaque appel crée son propre `SmtpClient`).

### 6. `Options/SmtpOptions.cs` (nouveau)

```csharp
public class SmtpOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 25;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string From { get; set; } = "no-reply@xpertsphere.local";
    public bool EnableSsl { get; set; } = false;
}
```
Section de config `"Smtp"`. **Fail-fast au démarrage** si `Smtp:Host` est vide/absent, quel que soit l'environnement (`InvalidOperationException`, même principe que le fail-fast déjà en place pour `ConnectionStrings:BlobStorage` dans `MonolithApi`, voir `azurite-blob-storage-local.md`) — pas de vérification de connectivité réseau réelle au démarrage (le premier échec de connexion SMTP ne se manifeste qu'au premier envoi, comme pour `BlobServiceClient`).

### 7. Configuration par environnement

`appsettings.json` (base, valeurs vides/neutres, pas de secret) :
```json
"Smtp": {
  "Host": "",
  "Port": 25,
  "Username": "",
  "Password": "",
  "From": "no-reply@xpertsphere.local",
  "EnableSsl": false
},
"ApiKey": ""
```

`appsettings.Development.json` (ajout, valeurs du catcher local, ports détaillés en §9) :
```json
"Smtp": {
  "Host": "localhost",
  "Port": 2525,
  "EnableSsl": false
},
"ApiKey": "dev-only-shared-key"
```
(`Username`/`Password` restent vides : smtp4dev n'exige pas d'authentification.)

`ApiKey` a une valeur non secrète connue en Development (même principe que `azurite-blob-storage-local.md` : une valeur de dev committée n'est pas un secret, contrairement à une clé Staging/Production). Sans cette valeur, `dotnet run` en local (hors Docker) échouerait immédiatement au démarrage à cause du fail-fast (§6/§8), sans qu'aucun `.env` de service n'existe pour la fournir autrement (voir décision de ne pas créer de `.env` propre à ce service, plus bas). Le service conteneurisé (`docker-compose.yml`, §11) surcharge cette valeur via la variable d'environnement `ApiKey` (priorité des variables d'environnement sur `appsettings.Development.json`, comportement standard ASP.NET Core) — la valeur ci-dessus ne sert donc qu'à l'exécution locale via `dotnet run`.

`appsettings.Staging.json` (**nouveau fichier**, n'existe pas encore pour ce service — valeurs non secrètes uniquement) :
```json
"Smtp": {
  "Host": "smtp-relay.brevo.com",
  "Port": 587,
  "EnableSsl": true
}
```
`Username`/`Password` restent absents de ce fichier (secrets réels) : ils doivent être injectés via variables d'environnement (`Smtp__Username`, `Smtp__Password`) au moment du déploiement. **Note importante, à documenter comme telle** : aucun pipeline de déploiement Staging n'existe aujourd'hui pour `CommunicationService` (`.github/workflows/backend-communication-service.yml` ne fait que CI + push d'image ACR, il n'existe pas de `deploy-communication-service-to-aca.yml` contrairement à `monolith-api`/`resume-analyzer`/les deux frontends). Ce ticket **ne crée pas** ce pipeline ni n'exerce réellement Brevo : la config Staging est structurellement prête (host/port/TLS corrects) mais non testée en conditions réelles — c'est une extension volontairement laissée pour quand un pipeline de déploiement sera mis en place pour ce service. La création d'un compte Brevo et la génération de la clé SMTP restent une étape manuelle hors du périmètre du code, au même titre que l'obtention d'une clé Groq/Azure OpenAI pour `ResumeAnalyzer`.

**Décision explicite sur l'absence de nouveau `.env.example` pour ce service** : contrairement à `MonolithApi`/`ResumeAnalyzer`, `CommunicationService` ne charge aujourd'hui aucun fichier `.env` local (pas de `DotNetEnv.Env.Load()` dans `Program.cs`, pas de package `DotNetEnv` référencé). Créer un `.env.example` à la racine de ce service sans câbler ce mécanisme produirait une documentation morte — l'anti-pattern déjà repéré et corrigé ailleurs dans ce monorepo (`VITE_STORAGE_BASE_URL`, voir `secure-cv-download.md` de `MonolithApi`). Introduire `DotNetEnv` pour ce seul besoin serait un changement d'architecture plus large que ce que ce ticket justifie (le seul chemin réellement testé par ce ticket est le catcher local via `docker-compose`, qui reçoit ses variables d'environnement directement du bloc `environment:`, pas d'un `.env` de service). Les credentials Brevo (quand ils existeront) sont donc documentés ici, dans cette spec, et dans `appsettings.Staging.json` (valeurs non secrètes uniquement) — pas dans un nouveau fichier `.env.example` mort.

### 8. `Program.cs` — réécriture

- Supprimer le endpoint `/weatherforecast`, le `record WeatherForecast` et le tableau `summaries`.
- Conserver `AddOpenApi()`/`MapOpenApi()`, `AddEndpointsApiExplorer()`, `AddHealthChecks()`/`MapHealthChecks("/health")`, `UseHttpsRedirection()` (comportement actuel inchangé).
- Ajouter `builder.Services.AddControllers();` et `app.MapControllers();` (le service n'a aujourd'hui aucun contrôleur).
- Ajouter `builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));`.
- Enregistrer `builder.Services.AddSingleton<ITemplateService, TemplateService>();` et `builder.Services.AddScoped<IEmailService, EmailService>();`.
- Étendre `AddSwaggerGen(...)` pour déclarer un schéma de sécurité `ApiKey` (`SecuritySchemeType.ApiKey`, `In = ParameterLocation.Header`, `Name = "X-Api-Key"`) avec une `SecurityRequirement` globale — même pattern que `SwaggerExtensions.cs` de `MonolithApi` pour le Bearer JWT (fichier de référence à consulter, pas à copier tel quel : ce service n'a pas de JWT).
- Ajouter `app.UseMiddleware<ApiKeyMiddleware>();`, positionné après le mapping de `/health` et avant `app.MapControllers()` — le middleware ne doit **pas** intercepter `/health`, `/swagger`, `/openapi` (voir §9).
- Fail-fast au démarrage si `ApiKey` (config) est vide/absent (`InvalidOperationException`), quel que soit l'environnement — même logique que pour `Smtp:Host` (§6), pour éviter de démarrer silencieusement avec la protection désactivée.

### 9. `Middleware/ApiKeyMiddleware.cs` (nouveau)

- S'applique uniquement aux requêtes dont le chemin commence par `/api` (`context.Request.Path.StartsWithSegments("/api")`) — laisse passer `/health`, `/swagger`, `/openapi` sans vérification.
- Compare le header `X-Api-Key` de la requête à la valeur de configuration `ApiKey` (comparaison exacte, sensible à la casse).
- Si absent ou différent : répond `401` avec un corps `{ "message": "Missing or invalid API key" }`, sans appeler `next()`.
- Si valide : appelle `next()`.

### 10. `Controllers/EmailsController.cs` (nouveau)

```csharp
[ApiController]
[Route("api/emails")]
public class EmailsController(IEmailService emailService, ILogger<EmailsController> logger) : ControllerBase
{
    [HttpPost("send")]
    [ProducesResponseType(typeof(SendEmailResponseDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(502)]
    public async Task<IActionResult> Send([FromBody] SendTemplatedEmailRequestDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var messageId = await emailService.SendTemplatedEmailAsync(dto.TemplateName, dto.To, dto.TemplateData, dto.Language, cancellationToken);
            return Ok(new SendEmailResponseDto { MessageId = messageId, Status = "Sent" });
        }
        catch (TemplateNotFoundException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (MissingTemplateVariableException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (EmailSendException ex)
        {
            logger.LogError(ex, "Failed to send templated email {TemplateName} to {To}", dto.TemplateName, dto.To);
            return StatusCode(502, new { message = "Failed to send email" });
        }
    }
}
```
Toute exception non listée ci-dessus (bug inattendu) n'est **pas** capturée ici et suit le comportement par défaut d'ASP.NET Core (500) — volontaire, pour ne pas masquer un vrai bug sous un 502.

Nouveaux DTOs (`Dto/EmailDtos.cs`, ou deux fichiers séparés au choix du développeur) :
```csharp
public class SendTemplatedEmailRequestDto
{
    [Required]
    public string TemplateName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string To { get; set; } = string.Empty;

    public Dictionary<string, string> TemplateData { get; set; } = new();

    public string Language { get; set; } = "fr-FR";
}

public class SendEmailResponseDto
{
    public string MessageId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
```
`[ApiController]` génère automatiquement une réponse `400` (ProblemDetails) si `TemplateName`/`To` sont absents ou si `To` n'a pas un format d'email valide — aucun code de validation manuel requis pour ce cas.

### 11. `docker-compose.yml` (racine) — nouveaux services

Ports déjà occupés dans le fichier actuel : `1433` (sqlserver), `6379` (redis), `10000` (azurite), `8080` (adminer), `5050` (monolith-api, `API_PORT`), `8001` (resume-analyzer), `3000`/`3001` (frontends). Nouveaux ports proposés, tous configurables par variable d'environnement avec défaut, cohérent avec le pattern `${API_PORT:-5050}` déjà utilisé :

```yaml
  smtp4dev:
    image: rnwood/smtp4dev:v3
    container_name: xpertsphere-smtp4dev
    ports:
      - "${SMTP_PORT:-2525}:25"
      - "${SMTP_UI_PORT:-5080}:80"
    networks:
      - xpertsphere-network
    restart: unless-stopped

  communication-service:
    build:
      context: .
      dockerfile: docker/backend/communication-service/Dockerfile
    container_name: xpertsphere-communication-service
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      Smtp__Host: smtp4dev
      Smtp__Port: "25"
      Smtp__EnableSsl: "false"
      ApiKey: ${COMMUNICATION_API_KEY}
    ports:
      - "${COMMUNICATION_API_PORT:-5051}:8080"
    depends_on:
      smtp4dev:
        condition: service_started
    networks:
      - xpertsphere-network
    restart: unless-stopped
```

- `smtp4dev` : pas de volume nommé (boîte de réception éphémère, acceptable pour un outil de dev — décision assumée, pas un oubli, cohérent avec le fait que `redis` n'a pas non plus de healthcheck dans ce fichier).
- `communication-service` : pas de `condition: service_healthy` sur `smtp4dev` (`smtp4dev` n'a pas de `HEALTHCHECK` Docker propre) — `service_started` suffit, la connexion SMTP n'est de toute façon tentée qu'au premier envoi (même principe que `BlobServiceClient`/Azurite : pas de vérification de connectivité au démarrage).
- Pas de dépendance vers `sqlserver`/`redis` : les chaînes de connexion présentes dans `appsettings.json` restent définies mais **non utilisées** par ce ticket (aucun `DbContext`/client Redis câblé) — ne pas les câbler par réflexe en ajoutant ce service à `docker-compose.yml`.
- Nouvelles variables à ajouter au `.env.example` racine (fichier existant, modifié par le développeur — pas par cette spec) : `COMMUNICATION_API_PORT` (défaut `5051`), `SMTP_PORT` (défaut `2525`), `SMTP_UI_PORT` (défaut `5080`), `COMMUNICATION_API_KEY` (pas de défaut, à l'image de `JWT_KEY`/`ADMIN_PASSWORD` — obligatoire, aucune valeur en dur committée).

## Rétrocompatibilité

- Aucun endpoint, DTO ou comportement existant n'est modifié : ce service n'a aujourd'hui aucun consommateur ni endpoint réel au-delà du template ASP.NET Core par défaut, qui est retiré.
- `Models/EmailMessage.cs`, `Models/MessageTemplate.cs` : structure inchangée (aucun champ ajouté/retiré), seuls certains champs (`ScheduledFor`, `Status`, `RetryCount`, `ErrorMessage`, `Priority`, `Attachments`, `Cc`, `Bcc`) restent à leurs valeurs par défaut, non exploités par ce ticket.
- `INotificationService` : non touché.
- `KeyVaultExtensions`/Key Vault : n'existe pas pour ce service aujourd'hui, non introduit par ce ticket (les secrets Staging/Production Brevo restent, pour l'instant, à injecter via variables d'environnement au moment où un pipeline de déploiement sera créé pour ce service — hors périmètre).

## Critères d'acceptation / tests

1. `docker compose up communication-service smtp4dev` (ou `docker compose up` complet) construit et démarre `xpertsphere-communication-service` sans erreur — le bug de Dockerfile (§ Diagnostic) est corrigé et vérifiable ici.
2. Le healthcheck Docker du conteneur `communication-service` passe (`curl -f http://localhost:8080/health` répond 200) une fois démarré.
3. Swagger UI (`/swagger`, environnement Development) expose `POST /api/emails/send`, avec un bouton "Authorize" permettant de saisir `X-Api-Key`.
4. Appel `POST /api/emails/send` avec un `X-Api-Key` valide, `templateName: "AccountActivation"`, un `to` valide, `templateData: { "ActivationLink": "https://example.com/activate/abc123" }` : retourne `200` avec un `messageId` non vide, **et** l'email est visible dans l'UI web de smtp4dev (`http://localhost:${SMTP_UI_PORT:-5080}`), avec le sujet attendu et le lien correctement substitué (aucune occurrence résiduelle de `{{ActivationLink}}` dans le corps reçu).
5. Même appel sans header `X-Api-Key` (ou avec une valeur incorrecte) : `401`.
6. Même appel avec un `templateName` inconnu (ex. `"DoesNotExist"`) : `400`.
7. Même appel omettant `ActivationLink` de `templateData` : `400`.
8. Même appel avec un `to` qui n'a pas un format d'email valide : `400` (validation de modèle automatique `[ApiController]`).
9. En arrêtant/rendant injoignable le service SMTP configuré (ex. `Smtp:Host` invalide) : l'appel retourne `502`, pas un `500` non géré ni un succès trompeur.
10. `dotnet restore`/`dotnet build` réussissent sur `XpertSphere.CommunicationService.csproj` après ajout de `MailKit`. **Ce critère porte uniquement sur `restore`/`build`, pas sur le CI complet** — voir le point d'attention CI ci-dessous (Hors périmètre / risque à vérifier).
11. Démarrer le service (Development) en retirant explicitement `Smtp:Host` (`appsettings.Development.json`) ou `ApiKey` (`appsettings.Development.json`) de la configuration : échec au démarrage (`InvalidOperationException`), pas de démarrage silencieux avec la protection désactivée ou l'envoi impossible. Avec les valeurs par défaut de `appsettings.Development.json` telles que définies dans cette spec (§7), le démarrage réussit sans configuration supplémentaire.

## Hors périmètre

- **Envoi en masse, planification, statut, annulation, retry d'un email** (`SendBulkEmailsAsync`, `GetEmailStatusAsync`, `CancelScheduledEmailAsync`, `RetryFailedEmailAsync`) : retirés de `IEmailService` dans ce ticket (décision §1), impliqueraient persistance + worker d'arrière-plan, aucun besoin actuel.
- **CRUD de templates persistant en base** (`CreateTemplateAsync`, `UpdateTemplateAsync`, `DeleteTemplateAsync`, `GetAllTemplatesAsync`) : retirés de `ITemplateService` (décision §2). Seul `AccountActivation`/`fr-FR` est seedé en dur ; aucun autre `TemplateType` n'est seedé.
- **`INotificationService`** (notifications in-app) : non touché, hors périmètre total.
- **Appel depuis `MonolithApi`** (déclenchement réel de l'email d'activation à l'inscription, génération du lien/token) : ticket B, spec séparée à écrire plus tard. Cette spec ne construit que le côté serveur (`CommunicationService`).
- **Authentification inter-services avancée** au-delà de la clé partagée `X-Api-Key` (mTLS, Entra ID service-à-service) : non traitée ici, sujet séparé (décision §5).
- **Pipeline de déploiement Staging/Production pour `CommunicationService`** (`deploy-communication-service-to-aca.yml` ou équivalent) : n'existe pas aujourd'hui, non créé par ce ticket. La config Brevo (§7) est structurellement prête mais non exercée/testée en conditions réelles par ce ticket.
- **Création du compte Brevo et génération de la clé SMTP** : étape manuelle hors du périmètre du code.
- **Encodage HTML des variables de template** : non ajouté dans ce ticket (voir point d'attention sécurité §4), à réévaluer si un futur template accepte du texte libre non fiable.
- **`.env`/`.env.example` propre à ce service, package `DotNetEnv`** : décision explicite de ne pas les introduire dans ce ticket (voir §7, justification anti-pattern de documentation morte).
- **Tests automatisés** : cette spec définit des critères d'acceptation vérifiables manuellement/par appel HTTP direct ; l'écriture effective de tests relève de l'agent `developer`. Il n'existe aujourd'hui aucun projet de tests dédié pour ce service (contrairement à `XpertSphere.MonolithApi.Tests`).
- **Risque CI préexistant, à vérifier explicitement par le développeur avant de considérer le ticket terminé** : le workflow `backend-communication-service.yml` (job `validate`, déclenché sur PR) exécute `dotnet test src/backend/XpertSphere.CommunicationService/XpertSphere.CommunicationService.csproj --no-build`, directement sur le projet applicatif — qui ne référence aucun SDK de test (`Microsoft.NET.Test.Sdk`) ni aujourd'hui, ni après ce ticket. Ce comportement est **préexistant** (indépendant de tout ce qui est ajouté ici) mais n'a probablement jamais été exercé par une vraie PR touchant ce service jusqu'ici ; ce ticket sera potentiellement la première PR à le déclencher réellement. Si `dotnet test` sur un projet sans SDK de test fait échouer le job (comportement à vérifier empiriquement par le développeur — non confirmé dans cette spec faute de SDK .NET 9 disponible pour le vérifier ici), corriger ce point est **hors périmètre fonctionnel de cette spec** mais bloquerait la CI de la PR d'implémentation : le développeur doit remonter cette trouvaille (comme toute trouvaille en cours de route) plutôt que la corriger silencieusement ou l'ignorer — l'agent principal tranchera (ex. ajuster le workflow pour ne lancer `dotnet test` que si un projet de tests existe, ou accepter le job rouge en l'état le temps qu'un projet de tests soit créé).
- **Healthcheck Docker Compose sur le service `smtp4dev`** : non ajouté, cohérent avec l'absence de healthcheck sur `redis` dans le fichier actuel.

## Fichiers à créer/modifier (récapitulatif)

- `docker/backend/communication-service/Dockerfile` : suppression de la ligne `COPY ["XpertSphere.sln", "./"]` (bug bloquant, voir Diagnostic).
- `XpertSphere.CommunicationService.csproj` : ajout du package `MailKit`.
- `Services/Interfaces/IEmailService.cs` : interface réduite/adaptée (voir §2).
- `Services/Interfaces/ITemplateService.cs` : interface réduite/adaptée (voir §2), nouvelle classe `RenderedTemplate`.
- `Exceptions/EmailExceptions.cs` (nouveau) : `TemplateNotFoundException`, `MissingTemplateVariableException`, `EmailSendException`.
- `Services/TemplateService.cs` (nouveau) : implémentation avec seed statique `AccountActivation`/`fr-FR`.
- `Services/EmailService.cs` (nouveau) : implémentation MailKit.
- `Options/SmtpOptions.cs` (nouveau).
- `Middleware/ApiKeyMiddleware.cs` (nouveau).
- `Dto/EmailDtos.cs` (nouveau) : `SendTemplatedEmailRequestDto`, `SendEmailResponseDto`.
- `Controllers/EmailsController.cs` (nouveau) : `POST /api/emails/send`.
- `Program.cs` : réécriture (voir §8).
- `appsettings.json` : nouvelles sections `Smtp`, `ApiKey`.
- `appsettings.Development.json` : nouvelle section `Smtp` (valeurs smtp4dev).
- `appsettings.Staging.json` (**nouveau fichier**) : nouvelle section `Smtp` (valeurs Brevo non secrètes).
- `docker-compose.yml` (racine) : nouveaux services `smtp4dev`, `communication-service`, aucun nouveau volume.
- `.env.example` (racine, modifié par le développeur) : nouvelles variables `COMMUNICATION_API_PORT`, `SMTP_PORT`, `SMTP_UI_PORT`, `COMMUNICATION_API_KEY`.

Aucun changement dans `Models/NotificationMessage.cs`, `Services/Interfaces/INotificationService.cs`, ni dans les autres services du monorepo (`MonolithApi`, `ReportingService`, `IntegrationService`, `ResumeAnalyzer`).

## Questions résiduelles

Aucune question bloquante ne subsiste : le provider SMTP (smtp4dev en dev, Brevo en Staging) est désormais figé par l'utilisateur, et les points laissés à l'appréciation du développeur (syntaxe de placeholder, découpage exact des fichiers DTO/exceptions, durée de vie DI Scoped vs Transient) sont documentés comme des décisions assumées dans cette spec plutôt que comme des questions ouvertes.
