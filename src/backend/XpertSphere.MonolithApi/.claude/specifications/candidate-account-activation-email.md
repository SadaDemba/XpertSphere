# Envoi reel de l'email d'activation de compte a l'inscription candidat (ticket B)

## Contexte et perimetre

Cette spec est le ticket B annonce par
`XpertSphere.CommunicationService/.claude/specifications/email-sending-foundation.md` (ticket A,
deja merge dans develop) : "brancher la creation de compte du MonolithApi sur l'endpoint cree ici,
pour declencher l'email d'activation (generation reelle du lien/token, appel HTTP depuis
MonolithApi)."

Le ticket A a livre, cote CommunicationService :
- `POST /api/emails/send` (auth `X-Api-Key` / `ApiKeyMiddleware`), acceptant
  `{ templateName, to, templateData, language }`, retournant `{ messageId, status }`.
- Un template `AccountActivation` seede en dur (fr-FR), variable `ActivationLink`, sujet et corps
  HTML deja rediges.
- Le service conteneurise (`docker-compose.yml`, service `communication-service`, reseau
  `xpertsphere-network`, port interne 8080) et son catcher SMTP local `smtp4dev`.

Conclusion de l'exploration cote CommunicationService : aucun changement n'y est necessaire.
Le contrat de `POST /api/emails/send` couvre exactement le besoin de ce ticket (envoi d'un email
`AccountActivation` avec une seule variable `ActivationLink`) ; ce ticket est un pur consommateur
HTTP de cet endpoint.

Le present ticket porte donc entierement cote MonolithApi :
1. Generation reelle d'un lien d'activation (token de confirmation email + URL absolue vers
   candidate-app).
2. Appel HTTP sortant vers CommunicationService a l'inscription candidat
   (`POST /api/auth/register/candidate`), pour declencher reellement l'envoi de cet email.
3. Correction d'un bug de configuration ASP.NET Core Identity qui rendrait le lien inutilisable en
   pratique (voir Constat, point 2).
4. **Activation stricte** (decision utilisateur, voir Decisions point 1) : `RequireConfirmedEmail`
   passe a `true` — un candidat ne peut plus se connecter tant qu'il n'a pas confirme son email.
   Consequence directe : un mecanisme de renvoi d'email d'activation
   (`POST /api/auth/resend-confirmation`) est introduit dans ce ticket, avec ses propres
   protections (cooldown par email + rate limiting par IP, voir Comportement cible).
5. Coordination frontend candidate-app : nouvelle page de confirmation consommant l'endpoint
   `POST /api/auth/confirm-email` deja existant cote backend, et adaptation de `LoginPage.vue`
   pour proposer un renvoi d'email quand la connexion est refusee pour cause de compte non
   confirme.

## Decisions de l'utilisateur (tranchees, remplacent la version precedente de cette spec)

L'utilisateur a tranche les deux points laisses ouverts dans la version precedente de cette spec :

1. **Activation stricte retenue** (et non l'activation "douce" initialement recommandee) :
   `RequireConfirmedEmail = true`. Un candidat ne peut pas se connecter tant qu'il n'a pas clique
   sur le lien d'activation recu par email. Consequences directement assumees par l'utilisateur et
   detaillees ci-dessous : mecanisme de renvoi d'email obligatoire, message de login explicite,
   politique d'echec d'envoi a l'inscription tranchee ci-dessous (point 11).
2. **Perimetre candidat uniquement confirme** : `RegisterAsync` (endpoint admin, utilisateurs
   d'organisation) reste inchange, dette technique documentee comme dans la version precedente de
   cette spec — voir §Constat point 5 pour l'analyse de l'interaction (importante) entre cette
   decision et l'activation stricte.

## Constat - etat actuel du code (MonolithApi)

Exploration de `Services/AuthenticationService.cs`, `Extensions/SecurityExtensions.cs`,
`Controllers/AuthController.cs`, `Models/User.cs`, `Mappings/AuthMappingProfile.cs`,
`XpertSphere.MonolithApi.Tests/Services/AuthenticationServiceTests.cs` :

1. Le flux existe deja partiellement mais n'aboutit jamais a un email envoye.
   `RegisterCandidateAsync` (`Services/AuthenticationService.cs:171-342`) cree l'utilisateur avec
   `EmailConfirmed = false` (ligne 221), puis, apres le commit de la transaction (lignes
   321-329), appelle `_userManager.GenerateEmailConfirmationTokenAsync(user)` et assigne le
   resultat a `user.EmailConfirmationToken` - mais cette assignation n'est jamais persistee
   (aucun `UpdateAsync` apres cette ligne) ni jamais transmise nulle part :
   `AuthMappingProfile.cs:17` ignore explicitement ce champ dans le mapping vers
   `AuthResponseDto`. Le token genere est donc immediatement perdu - c'est un vestige mort,
   present egalement a l'identique dans `RegisterAsync` (lignes 156-158, endpoint admin
   `POST /api/auth/register`). Aucun email n'est envoye aujourd'hui, dans aucun des deux flux
   d'inscription.
   - `Controllers/AuthController.cs:83-89` expose deja `POST /api/auth/confirm-email`, et
     `AuthenticationService.ConfirmEmailAsync` (lignes 546-578) appelle deja correctement
     `_userManager.ConfirmEmailAsync(user, token)` - ce endpoint de validation existe deja et
     n'est pas modifie par ce ticket. Le seul chainon manquant est l'emission reelle du lien
     (generation + envoi), pas sa validation.
2. Bug de configuration bloquant pour l'usage reel d'un lien envoye par email, a corriger dans
   ce ticket (sans lequel le lien serait quasi inutilisable en pratique) :
   `Extensions/SecurityExtensions.cs:75-78` configure
   `options.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultEmailProvider` (valeur
   "Email"). Ce provider (`EmailTokenProvider<TUser>`, encapsulant
   `TotpSecurityStampBasedTokenProvider<TUser>`) genere un code TOTP a fenetre de validite tres
   courte (de l'ordre de quelques minutes, independant de tout parametre de configuration
   applicative) - pas le `DataProtectorTokenProvider` ("Default") dont la duree de vie est
   pilotee par `DataProtectionTokenProviderOptions.TokenLifespan`, deja configure a 1 jour
   ailleurs dans le meme fichier (`SecurityExtensions.cs:770-774`). Cette configuration de 1 jour
   ne s'applique donc a rien cote confirmation email aujourd'hui.
   - Correctif retenu : changer uniquement `options.Tokens.EmailConfirmationTokenProvider` vers
     `TokenOptions.DefaultProvider` (valeur "Default", deja enregistre par
     `.AddDefaultTokenProviders()`, aucun nouveau provider a enregistrer).
     `options.Tokens.PasswordResetTokenProvider` n'est pas touche par ce ticket (reste sur
     "Email") : `ForgotPasswordAsync` ne declenche aujourd'hui l'envoi d'aucun email non plus,
     corriger ce point est hors perimetre de ce ticket, qui porte exclusivement sur l'activation
     de compte (voir Hors perimetre).
   - Consequence pratique du correctif : un lien d'activation reste valide 1 jour apres
     l'inscription, au lieu de quelques minutes. **Avec l'activation stricte retenue, ce
     correctif devient encore plus critique qu'avant** : sans lui, un candidat bloque a la
     connexion (`RequireConfirmedEmail = true`) et dont le token expire en quelques minutes se
     retrouverait bloque presque immediatement, sans autre recours que le nouveau mecanisme de
     renvoi (point 4 ci-dessous) — le correctif reste indispensable, le renvoi n'est qu'un filet de
     secours, pas un substitut.
   - Le token emis par `DataProtectorTokenProvider` contient des caracteres `+`/`/`/`=` (Base64) :
     il doit etre URL-encode dans le lien construit (voir Comportement cible).
3. `User.EmailConfirmationToken`/`EmailConfirmationTokenExpiry` (colonnes persistees,
   `Models/User.cs:60-62`) sont un vestige deja mort avant ce ticket : jamais lus par
   `ConfirmEmailAsync` (qui valide via le provider Identity, pas via cette colonne), jamais
   persistes (voir point 1), jamais exposes (`AuthMappingProfile.cs:17` les ignore). Ce ticket ne
   modifie pas le schema pour les retirer (nettoyage hors perimetre, voir Hors perimetre) - se
   contente de ne plus assigner ce champ mort dans `RegisterCandidateAsync`.
4. Aucun mecanisme d'appel HTTP sortant vers un autre microservice n'existe aujourd'hui dans
   MonolithApi en dehors d'un `HttpClient` nomme pour Entra ID
   (`builder.Services.AddHttpClient("EntraId", ...)`, `Program.cs:80`) et d'un `new HttpClient()`
   ad hoc pour l'echange de code OAuth (`AuthenticationService.cs:1099`). Ce ticket introduit donc
   le premier client HTTP type vers un microservice interne du monorepo.
5. **`RegisterAsync` (endpoint admin) et interaction critique avec l'activation stricte, a bien
   comprendre avant de considerer la decision 2 comme anodine** : cet endpoint
   (`Authorize(Policy = "CanCreateUsers")`, utilisateurs d'organisation) cree deja ses utilisateurs
   avec `EmailConfirmed = false` (ligne 137) et contient le meme code mort de generation de token
   (point 1). Ce ticket ne branche **pas** l'envoi d'email sur ce endpoint (decision 2). Or,
   `RequireConfirmedEmail` est un reglage **global** d'ASP.NET Core Identity (pas scope par
   endpoint) : une fois active, il s'applique a **tout** utilisateur local non confirme, y compris
   ceux crees via `RegisterAsync`. Consequence, a documenter explicitement pour ne pas la laisser
   paraitre comme un oubli :
   - Un utilisateur d'organisation cree en mode JWT local (Development, ou fallback hors Entra ID)
     via `RegisterAsync` se retrouve **cree avec `EmailConfirmed = false` et aucun email jamais
     envoye automatiquement** — mais **n'est pas bloque de facon permanente** : le nouveau endpoint
     `POST /api/auth/resend-confirmation` (point 4 ci-dessous) est **generique par email**, pas
     restreint aux comptes issus de `RegisterCandidateAsync`. Le compte peut donc etre debloque a
     tout moment (par l'utilisateur concerne, ou par l'admin qui l'a cree, en appelant ce endpoint
     avec l'email du compte), sans qu'aucun code supplementaire ne soit necessaire cote
     `RegisterAsync`.
   - Cela **ne dispense pas** de documenter que `RegisterAsync` ne declenche toujours aucun email
     automatique a la creation (dette technique inchangee, decision 2) — seulement que le blocage
     qui en resulte n'est plus permanent grace au mecanisme de renvoi introduit par ce ticket.
   - En Staging/Production, les utilisateurs d'organisation passent par Entra ID
     (`ShouldUseEntraId`), qui gere sa propre confirmation
     (`ClaimsEnrichmentMiddleware` positionne deja `EmailConfirmed = true`, lignes 233/286) — non
     concernes par ce changement. Seul le mode JWT local (essentiellement Development) est
     concerne.
6. **Comptes existants non confirmes avant ce ticket — risque de blocage retroactif, a traiter
   explicitement** : avec l'activation "douce" actuelle (`RequireConfirmedEmail = false`), tout
   compte candidat deja enregistre avec `EmailConfirmed = false` (tous les candidats qui se sont
   deja inscrits sans jamais confirmer, puisque cela n'a jamais ete necessaire jusqu'ici) pouvait
   se connecter normalement. **Activer `RequireConfirmedEmail = true` bloquerait immediatement et
   retroactivement tous ces comptes existants**, sans qu'aucun d'eux n'ait jamais recu de lien
   d'activation fonctionnel (rappel : aucun email n'a jamais ete reellement envoye avant ce
   ticket, point 1). Ce point est traite par une migration de "grand-pere" (grandfathering), voir
   Comportement cible, section Migration — decision necessaire pour ne pas casser silencieusement
   des comptes deja utilises (tests locaux, demos, ou tout compte candidat reel deja cree en
   Staging/Production si applicable).
7. `ForgotPasswordAsync`/`ResetPasswordAsync` : meme lacune (token genere, jamais envoye par
   email) - hors perimetre, non traite par ce ticket (voir Hors perimetre).
8. `RegisterPage.vue` (candidate-app) affiche aujourd'hui, apres une inscription reussie, le
   message "Compte cree avec succes ! Vous etes maintenant connecte." et redirige immediatement
   vers `/` (`src/pages/RegisterPage.vue:61-66`). Deja trompeur avant ce ticket (aucun
   `AccessToken` n'est jamais renvoye par `RegisterCandidateAsync`) — **d'autant plus faux avec
   l'activation stricte** : desormais, meme une future tentative de connexion echouerait tant que
   l'email n'est pas confirme. Corrige dans la coordination frontend (voir plus bas).
9. **Test existant a corriger, pas seulement a ne pas casser** :
   `AuthenticationServiceTests.cs:222-256`
   (`LoginAsync_WithUnconfirmedEmail_ShouldReturnFailure`) existe deja mais **ne teste pas
   reellement le comportement "email non confirme"** : il ne configure aucune valeur de retour
   pour `_mockSignInManager.CheckPasswordSignInAsync(...)`, donc l'appel retourne `null` (valeur
   par defaut Moq pour un type non configure), ce qui provoque une `NullReferenceException`
   interne, capturee par le `try/catch` englobant de `LoginAsync`, produisant le message generique
   "An error occurred during login" — c'est ce message que le test verifie, pas le message
   "email non confirme". Le test passe donc aujourd'hui **par coincidence**, sans jamais exercer
   le vrai chemin `SignInResult.NotAllowed`. A corriger par le developpeur (voir Criteres
   d'acceptation) : mocker explicitement `CheckPasswordSignInAsync` pour retourner
   `SignInResult.NotAllowed`, et mettre a jour l'assertion pour verifier le nouveau message/le
   nouveau flag `RequiresEmailConfirmation` (voir Comportement cible).
10. **Bonne nouvelle decouverte a l'exploration** : `AuthenticationService.LoginAsync`
    (`Services/AuthenticationService.cs:443-456`) gere **deja** correctement la branche
    `result.IsNotAllowed` (issue de `SignInManager.CheckPasswordSignInAsync`, qui appelle en
    interne `CanSignInAsync`, qui verifie `Options.SignIn.RequireConfirmedEmail` contre
    `user.EmailConfirmed`) : elle retourne deja un message explicite et ne passe **pas** par
    l'incrementation du compteur d'echecs de connexion (`user.IncrementFailedLogin()`, place apres
    ce bloc) — donc pas de risque de verrouillage de compte injuste pour un candidat qui tente de
    se connecter avec les bons identifiants mais un email non confirme. Ce code etait
    **entierement mort/inatteignable avant ce ticket** (avec `RequireConfirmedEmail = false`,
    `CanSignInAsync` ne pouvait jamais retourner `false` pour ce motif) : ce ticket le rend
    reellement vivant, sans avoir besoin de le reecrire — seul le message retourne et
    l'enrichissement du DTO (voir Comportement cible) sont modifies.

## Comportement cible

### 1. Activation Identity stricte (`Extensions/SecurityExtensions.cs`)

```csharp
private static void ConfigureSignInOptions(IdentityOptions options, IWebHostEnvironment environment)
{
    options.SignIn.RequireConfirmedEmail = true; // etait false
    options.SignIn.RequireConfirmedAccount = false; // inchange - RequireConfirmedEmail suffit, pas besoin du contrat plus large "confirmed account" (qui inclurait aussi la confirmation telephone, non utilisee dans ce produit)
}
```

```csharp
private static void ConfigureTokenOptions(IdentityOptions options)
{
    options.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultProvider; // etait DefaultEmailProvider — correctif bug token, Constat point 2
    options.Tokens.PasswordResetTokenProvider = TokenOptions.DefaultEmailProvider; // inchange, hors perimetre
}
```

**Point d'attention pour le developpeur, a verifier empiriquement avant de considerer ce point
termine** (comportement runtime, non certifiable par simple lecture de code) : apres ce
correctif, un token genere a l'inscription doit rester valide lors d'un appel a
`POST /api/auth/confirm-email` effectue plusieurs minutes plus tard (ex. attendre 10-15 minutes
entre inscription et clic).

### 2. Migration EF Core — grandfathering des comptes existants (Constat point 6)

Nouvelle migration **purement de donnees** (pas de changement de schema), qui s'applique via
`dotnet ef database update` au demarrage de l'API comme toute autre migration, donc avant que
l'API ne commence a accepter des connexions avec le nouveau reglage :

```csharp
// Migrations/<timestamp>_GrandfatherExistingUnconfirmedAccounts.cs
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.Sql("UPDATE Users SET EmailConfirmed = 1 WHERE EmailConfirmed = 0;");
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    // Intentionnellement vide : un rollback ne doit jamais re-verrouiller des comptes
    // qui ont ete legitimement grand-peres — non reversible par conception.
}
```

Justification : tout compte `EmailConfirmed = false` existant **avant** ce ticket l'est devenu
sans qu'aucun email d'activation reel n'ait jamais ete envoye (Constat point 1) — bloquer ces
comptes retroactivement des l'activation du nouveau reglage serait punir des utilisateurs pour un
defaut qui ne leur est pas imputable. Seuls les comptes crees **apres** cette migration (donc apres
le deploiement de ce ticket) passent reellement par le nouveau flux strict (email reellement envoye
a la creation, confirmation reellement necessaire). `Down()` volontairement vide (pas de
regression symmetrique souhaitable ici, a la difference d'une migration de schema classique).

### 3. `DTOs/Auth/AuthResponseDto.cs` — nouveau champ

```csharp
public bool RequiresEmailConfirmation { get; init; } = false;
```

### 4. `Mappings/AuthMappingProfile.cs` — nouvelle regle de mapping

```csharp
.ForMember(dest => dest.RequiresEmailConfirmation, opt => opt.MapFrom(src => !src.EmailConfirmed))
```

Ajoutee au `CreateMap<User, AuthResponseDto>()` existant (a cote de la regle deja presente pour
`EmailConfirmationToken`, ligne 17). Consequence volontaire et positive de ce choix : **toute**
projection `User -> AuthResponseDto` dans ce fichier (login refuse, inscription, confirmation
d'email, geter l'utilisateur courant, etc.) porte desormais automatiquement la bonne valeur, sans
avoir a l'assigner manuellement a chaque site d'appel de `AuthenticationService`.

### 5. `Utils/Results/AuthResult.cs` — extension du factory `Failure`

```csharp
public static AuthResult Failure(string message, List<string>? errors = null, int statusCode = 400, AuthResponseDto? data = null)
{
    return new AuthResult
    {
        IsSuccess = false,
        Message = message,
        Errors = errors ?? [message],
        StatusCode = statusCode,
        Data = data
    };
}
```

Parametre `data` optionnel, ajoute en dernier — **tous les appels existants a `AuthResult.Failure(...)`
dans le reste du code restent valides sans modification** (changement additif, non cassant).
Necessaire pour transmettre `RequiresEmailConfirmation = true` au client lors d'un login refuse
pour cause de compte non confirme (voir point 6 ci-dessous). Alternative ecartee : un nouveau
factory method dedie (`EmailConfirmationRequired(...)`) — moins reutilisable, alors que ce
parametre optionnel reste generique pour tout futur besoin d'attacher des donnees partielles a un
echec.

### 6. `AuthenticationService.LoginAsync` — branche `IsNotAllowed`

Remplacer (lignes 452-456) :

```csharp
if (result.IsNotAllowed)
{
    _logger.LogWarning("Login not allowed for user: {Email}", loginDto.Email);
    var authResponseDto = _mapper.Map<AuthResponseDto>(user);
    return AuthResult.Failure(
        "Votre compte n'est pas encore activé. Veuillez consulter l'email de confirmation envoyé lors de votre inscription, ou demandez un nouvel envoi.",
        data: authResponseDto);
}
```

`user` est deja charge a ce point de la methode (chargement en debut de `LoginAsync`, avec
`Include(u => u.Organization)` etc.) — pas de requete supplementaire necessaire.
`_mapper.Map<AuthResponseDto>(user)` positionne automatiquement `RequiresEmailConfirmation = true`
grace a la regle du point 4 (puisque `user.EmailConfirmed` est necessairement `false` dans cette
branche — c'est la condition meme qui l'a declenchee). Le message est ecrit directement en
francais : ce chemin etait **entierement mort avant ce ticket** (Constat point 10), ce n'est donc
pas une regression de traduction a laisser a `french-message-consistency.md` (qui traite des
messages *existants*, pas des messages nouvellement rendus atteignables par ce ticket-ci) — le
developpeur peut neanmoins verifier aupres de cette autre spec si son perimetre `Services/*.cs`
est deja confirme, pour eviter un double travail marginal.

**Aucun autre changement necessaire dans `LoginAsync`** : le reste du flux (succes, verrouillage de
compte, identifiants invalides) est inchange.

### 7. Nouveau mecanisme de renvoi — `POST /api/auth/resend-confirmation`

#### DTO et validator

```csharp
// DTOs/Auth/ResendConfirmationDto.cs
public record ResendConfirmationDto
{
    [Required] [EmailAddress] public required string Email { get; init; }
}
```

```csharp
// Validators/Auth/ResendConfirmationDtoValidator.cs — mirroir exact de ForgotPasswordDtoValidator
public class ResendConfirmationDtoValidator : AbstractValidator<ResendConfirmationDto>
{
    public ResendConfirmationDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");
    }
}
```

#### `IAuthenticationService`/`AuthenticationService`

```csharp
Task<AuthResult> ResendConfirmationEmailAsync(ResendConfirmationDto dto);
```

Comportement, **volontairement symetrique a `ForgotPasswordAsync` sur la non-divulgation
d'information** (meme principe deja applique dans ce fichier, ligne 591-595 : ne jamais reveler si
un compte existe) :

```csharp
private const string ResendConfirmationGenericMessage =
    "Si un compte existe pour cet email et n'est pas encore confirmé, un nouvel email d'activation vient d'être envoyé.";

public async Task<AuthResult> ResendConfirmationEmailAsync(ResendConfirmationDto dto)
{
    try
    {
        var validationResult = await _resendConfirmationValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return AuthResult.ValidationError(errors);
        }

        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null || user.EmailConfirmed)
        {
            // Ne jamais reveler si le compte existe ou est deja confirme.
            return AuthResult.Success(ResendConfirmationGenericMessage);
        }

        // Cooldown applicatif par email (voir §8) : protection additionnelle contre le
        // bombardement d'une boite mail via plusieurs IP differentes, en complement du rate
        // limiting par IP au niveau du endpoint (§9). Le resultat du cooldown n'est jamais
        // revele au client — meme message generique dans tous les cas.
        if (!ResendConfirmationCooldown.TryStart(user.Email!, TimeSpan.FromSeconds(60)))
        {
            return AuthResult.Success(ResendConfirmationGenericMessage);
        }

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var activationLink = BuildActivationLink(user.Email!, token);
        var sent = await _emailNotificationService.SendAccountActivationEmailAsync(user.Email!, activationLink);
        if (!sent)
        {
            _logger.LogError("Failed to resend account activation email to {Email}", user.Email);
        }

        return AuthResult.Success(ResendConfirmationGenericMessage);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error occurred while resending confirmation email for {Email}", dto.Email);
        // Meme message generique en cas d'exception inattendue - ne jamais reveler d'information
        // par une difference de message (enumeration-safety).
        return AuthResult.Success(ResendConfirmationGenericMessage);
    }
}
```

`BuildActivationLink(string email, string token)` : methode privee extraite de
`RegisterCandidateAsync` (refactor d'extraction, voir §10 ci-dessous), partagee entre
`RegisterCandidateAsync` et `ResendConfirmationEmailAsync` - evite de dupliquer la construction de
l'URL a deux endroits.

**Reponse toujours `200`/succes cote HTTP**, quel que soit le cas reel (compte inexistant, deja
confirme, dans la fenetre de cooldown, ou reellement renvoye) — coherent avec le principe
d'enumeration-safety deja applique par `ForgotPasswordAsync`. Un echec d'envoi SMTP/
`CommunicationService` (branche `if (!sent)`) est **logge cote serveur uniquement**, jamais
expose differemment au client : le candidat peut simplement retenter (sous reserve du cooldown/
rate limiting), sans qu'aucune information sur la cause exacte de l'echec ne lui soit donnee.

#### `Controllers/AuthController.cs`

```csharp
[HttpPost("resend-confirmation")]
[AllowAnonymous]
[EnableRateLimiting("resend-confirmation")]
public async Task<ActionResult<AuthResponseDto>> ResendConfirmation([FromBody] ResendConfirmationDto dto)
{
    var result = await _authService.ResendConfirmationEmailAsync(dto);
    return this.ToActionResult(result);
}
```

### 8. Cooldown applicatif par email (protection anti-bombardement)

Nouvelle classe utilitaire statique, sur le meme principe que le `ConcurrentDictionary` statique
deja utilise par `EntraIdRateLimitService` (`Services/EntraIdRateLimitService.cs:25`, precedent
deja etabli dans ce codebase pour du throttling en memoire, process-local) :

```csharp
// Utils/ResendConfirmationCooldown.cs
using System.Collections.Concurrent;

namespace XpertSphere.MonolithApi.Utils;

internal static class ResendConfirmationCooldown
{
    private static readonly ConcurrentDictionary<string, DateTime> LastSentAt =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Retourne true si aucun renvoi n'a eu lieu pour cet email dans la fenetre de cooldown
    /// (et enregistre l'instant present comme nouveau dernier envoi) ; false si un renvoi a deja
    /// eu lieu recemment (aucun nouvel envoi ne doit avoir lieu).
    /// </summary>
    public static bool TryStart(string email, TimeSpan cooldown)
    {
        var now = DateTime.UtcNow;
        var recorded = LastSentAt.AddOrUpdate(
            email,
            now,
            (_, last) => now - last < cooldown ? last : now);

        return recorded == now;
    }
}
```

**Limite connue, deja acceptee ailleurs dans ce codebase pour le meme type de mecanisme** : ce
cooldown est **process-local** (un `ConcurrentDictionary` statique en memoire) — s'il devait y
avoir plusieurs instances de `MonolithApi` derriere un load balancer (non le cas aujourd'hui,
aucun cache distribue/Redis n'est cable dans ce projet malgre une section `"Cache"` presente dans
`appsettings.json` mais non consommee par aucun code — verifie), le cooldown ne serait pas partage
entre instances. Meme limite deja assumee par `EntraIdRateLimitService`, non traitee ici non plus
— extension future si le projet passe a un deploiement multi-instance.

### 9. Rate limiting par IP sur l'endpoint (`Program.cs`, `Controllers/AuthController.cs`)

Utilise le middleware de rate limiting **natif d'ASP.NET Core** (`Microsoft.AspNetCore.RateLimiting`,
deja inclus dans le shared framework `Microsoft.AspNetCore.App` depuis .NET 7 — **aucun nouveau
package NuGet a ajouter**, premiere utilisation de ce mecanisme dans ce projet) :

```csharp
// Program.cs — nouvel enregistrement de service
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("resend-confirmation", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(15),
                QueueLimit = 0
            }));
});
```

```csharp
// Program.cs — pipeline, apres app.UseAuthorization() et avant app.MapControllers()
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();
```

Partitionnement explicite par adresse IP distante (`RemoteIpAddress`), pas de limite globale
partagee entre tous les appelants (qui bloquerait injustement tous les utilisateurs des qu'un seul
depasse le seuil). `PermitLimit = 5` requetes par fenetre de `15 minutes` par IP — valeurs choisies
comme un seuil raisonnable et non documentees comme figees definitivement par l'utilisateur ; le
developpeur peut les ajuster si besoin, ce ne sont pas des valeurs contractuelles testees par un
critere d'acceptation precis au nombre pres (voir Criteres d'acceptation, qui verifie seulement
qu'une limite existe et produit bien un `429`).

`[EnableRateLimiting("resend-confirmation")]` sur l'action controller (voir §7). Cette protection
par IP est **complementaire**, pas redondante, avec le cooldown par email (§8) : l'IP protege
contre un seul client abusant de l'endpoint (peu importe l'email cible), le cooldown par email
protege contre un bombardement d'une boite mail cible via plusieurs IP differentes (contournement
trivial d'une limite par IP seule).

### 10. `AuthenticationService.RegisterCandidateAsync` — construction et envoi du lien (extraction de `BuildActivationLink`)

Nouvelles dependances injectees dans le constructeur : `IEmailNotificationService
emailNotificationService`, `IOptions<FrontendSettings> frontendSettings`,
`IValidator<ResendConfirmationDto> resendConfirmationValidator` (pour §7).

Nouvelle methode privee partagee :

```csharp
private string BuildActivationLink(string email, string token)
{
    return $"{_frontendSettings.CandidateAppBaseUrl.TrimEnd('/')}/confirm-email" +
           $"?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";
}
```

Remplacer le bloc mort existant dans `RegisterCandidateAsync` (lignes 327-329) par, **apres**
`await transaction.CommitAsync();` (l'envoi ne doit jamais pouvoir provoquer un rollback d'un
compte deja cree avec succes — voir §11 pour la justification detaillee de ce choix face a
l'activation stricte) :

```csharp
var emailConfirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
var activationLink = BuildActivationLink(user.Email!, emailConfirmationToken);

var emailSent = await _emailNotificationService.SendAccountActivationEmailAsync(user.Email!, activationLink);
if (!emailSent)
{
    _logger.LogWarning(
        "Account activation email could not be sent to {Email}; account was created successfully but the candidate did not receive an activation link and must use the resend-confirmation endpoint.",
        user.Email);
}

var authResponseDto = _mapper.Map<AuthResponseDto>(user);
return AuthResult.SuccessWithUser(authResponseDto,
    "Registration successful. Please check your email to confirm your account.");
```

`authResponseDto.RequiresEmailConfirmation` vaut automatiquement `true` (regle AutoMapper §4,
`user.EmailConfirmed` est `false` a ce stade) — utile au frontend pour piloter l'ecran
post-inscription (voir Coordination frontend).

L'assignation morte `user.EmailConfirmationToken = emailConfirmationToken;` (Constat point 1) est
retiree.

### 11. Politique d'echec d'envoi a l'inscription — decision tranchee (question posee par l'utilisateur)

**Question posee** : l'echec d'envoi de l'email a l'inscription doit-il desormais faire echouer
l'inscription elle-meme (rollback), ou le compte doit-il etre cree non confirme avec possibilite
de renvoyer plus tard ?

**Decision retenue : pas de rollback — le compte est cree normalement, meme si l'envoi de l'email
echoue.** Justification, tenant compte explicitement de l'activation stricte (ce qui change par
rapport au raisonnement equivalent sous activation "douce") :

- **Le nouveau mecanisme de renvoi (§7) rend cette politique sure sous activation stricte.** Avant
  ce ticket, un candidat dont l'inscription reussirait mais dont l'email echouerait n'aurait eu
  aucun recours (pas de renvoi) — c'est precisement ce gap que le renvoi comble. Un compte cree
  sans email reussi n'est donc **pas perdu** : le candidat (ou un support/l'admin, si le candidat
  contacte le support) peut declencher `POST /api/auth/resend-confirmation` avec son email des
  qu'il constate ne pas avoir recu de lien.
- **Un rollback serait plus couteux et plus fragile que le probleme qu'il resoudrait.** Au moment
  ou l'envoi d'email est tente, la transaction est **deja commitee** (upload de CV, experiences,
  formations, attribution du role Candidate, tout est deja en base). Deplacer l'appel HTTP
  *dans* la transaction, avant le commit, pour permettre un rollback en cas d'echec,
  introduirait un couplage fort et fragile : une panne transitoire de `CommunicationService`
  (ex. redemarrage du conteneur, pic de latence SMTP) ferait echouer **toute l'inscription**,
  forcant le candidat a ressaisir l'integralite d'un formulaire multi-etapes (profil,
  experiences, formations, CV) pour une cause totalement etrangere a la validite de ses donnees.
  Ce cout UX est disproportionne par rapport au benefice (un compte non confirme, recuperable via
  le renvoi, n'est pas une donnee corrompue ni un risque de securite).
- **Alternative ecartee** : bloquer l'inscription (rollback) si l'envoi echoue. Ecartee pour les
  raisons ci-dessus — uniquement defendable en l'absence de tout mecanisme de renvoi, ce qui
  n'est plus le cas dans ce ticket.
- Consequence pour le message retourne au client : **inchange par rapport a la version precedente
  de cette spec** — le message de succes de `RegisterCandidateAsync` ne varie pas selon que
  l'email ait pu etre envoye ou non (le compte est de toute facon cree). Le frontend informe
  neanmoins le candidat qu'un email a ete envoye et qu'il doit confirmer avant de pouvoir se
  connecter (voir Coordination frontend) — si l'email n'arrive jamais, le candidat decouvrira le
  probleme au moment de tenter de se connecter (message explicite + bouton de renvoi, voir §6 et
  Coordination frontend), pas au moment de l'inscription elle-meme.

### 12. `Program.cs` — recapitulatif des enregistrements DI (reprend et etend la version precedente de cette spec)

```csharp
builder.Services.Configure<FrontendSettings>(builder.Configuration.GetSection("Frontend"));

builder.Services.AddHttpClient<IEmailNotificationService, EmailNotificationService>((sp, client) =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var baseUrl = configuration["CommunicationService:BaseUrl"];
    if (!string.IsNullOrWhiteSpace(baseUrl))
    {
        client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
    }

    var apiKey = configuration["CommunicationService:ApiKey"];
    if (!string.IsNullOrWhiteSpace(apiKey))
    {
        client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
    }

    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("resend-confirmation", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(15),
                QueueLimit = 0
            }));
});
```

Interface/implementation (`IEmailNotificationService`/`EmailNotificationService`) et DTO
(`SendTemplatedEmailRequestDto`) : **inchanges par rapport a la version precedente de cette
spec** — voir le recapitulatif complet ci-dessous (§Fichiers a creer/modifier) pour eviter toute
duplication.

Pas de fail-fast au demarrage de `MonolithApi` si `CommunicationService:BaseUrl`/`ApiKey` sont
absents — l'envoi d'email est une fonctionnalite annexe parmi beaucoup d'autres dans
`MonolithApi`, contrairement a `CommunicationService` lui-meme.

### 13. Configuration par environnement et `docker-compose.yml`

**Inchange par rapport a la version precedente de cette spec** :

`appsettings.json` :
```json
"CommunicationService": {
  "BaseUrl": "",
  "ApiKey": ""
},
"Frontend": {
  "CandidateAppBaseUrl": ""
}
```

`appsettings.Development.json` :
```json
"CommunicationService": {
  "BaseUrl": "http://localhost:5051",
  "ApiKey": "dev-only-shared-key"
},
"Frontend": {
  "CandidateAppBaseUrl": "http://localhost:3000"
}
```
`ApiKey` doit etre strictement identique a la valeur `ApiKey` de
`appsettings.Development.json` de CommunicationService (deja "dev-only-shared-key").

`docker-compose.yml` (racine) — service `monolith-api` :
```yaml
  monolith-api:
    environment:
      # ... variables existantes inchangees ...
      CommunicationService__BaseUrl: "http://communication-service:8080"
      CommunicationService__ApiKey: ${COMMUNICATION_API_KEY}
      Frontend__CandidateAppBaseUrl: "http://localhost:${CANDIDATE_APP_PORT:-3000}"
    depends_on:
      sqlserver:
        condition: service_healthy
      azurite:
        condition: service_started
      communication-service:
        condition: service_started
```

Points d'attention (deux URLs distinctes, ne pas les confondre) : `CommunicationService__BaseUrl`
= nom de service Docker interne (serveur-a-serveur) ; `Frontend__CandidateAppBaseUrl` = adresse
navigateur exposee sur l'hote (jamais le nom Docker interne, sous peine de liens d'activation
casses pour le candidat).

## Coordination frontend - candidate-app

### Nouvelle page de confirmation d'email (inchangee par rapport a la version precedente)

- `src/pages/ConfirmEmailPage.vue` (nouveau) : lit `route.query.email`/`route.query.token`, appelle
  `authService.confirmEmail({ email, token })` (`POST /api/auth/confirm-email`, deja existant cote
  backend). Affiche succes (avec lien vers `/login`) ou echec explicite (token invalide/expire),
  sans bouton de renvoi integre a cette page (le renvoi est propose depuis `LoginPage.vue`, voir
  ci-dessous, la ou le candidat decouvre concretement le blocage).
- `src/router/routes.ts` : nouvelle route `confirm-email` -> `ConfirmEmailPage.vue`, sans
  `meta: { requiresAuth: true }`.
- `src/services/authService.ts` : nouvelle methode `confirmEmail(dto: ConfirmEmailDto)`.
- `src/models/auth.ts` : nouveau type `ConfirmEmailDto { email: string; token: string }`.

### Nouveau : renvoi d'email depuis `LoginPage.vue` (impose par l'activation stricte)

- `src/models/auth.ts` :
  - Ajouter `requiresEmailConfirmation?: boolean` sur `AuthResponseDto` (a cote de
    `emailConfirmationToken`/`redirectUrl`, memes conventions de nommage camelCase deja en place
    dans ce fichier).
  - Nouveau type `ResendConfirmationDto { email: string }`.
- `src/services/authService.ts` : nouvelle methode
  ```ts
  async resendConfirmation(dto: ResendConfirmationDto): Promise<AuthResult | null> {
    return this.post<AuthResult>('/resend-confirmation', dto, "Erreur lors du renvoi de l'email de confirmation");
  }
  ```
- `src/stores/authStore.ts` :
  - Nouveaux refs d'etat : `requiresEmailConfirmation` (`boolean`, reinitialise a `false` au debut
    de chaque tentative de `login`) et `unconfirmedEmail` (`string | null`, memorise l'email
    saisi lors d'une tentative refusee pour ce motif, afin que le bouton de renvoi n'ait pas besoin
    que le candidat ressaisisse son email).
  - `login(...)` : lorsque `response?.isSuccess === false` **et**
    `response?.data?.requiresEmailConfirmation === true`, positionner
    `requiresEmailConfirmation.value = true` et `unconfirmedEmail.value = loginDto.email` avant de
    construire le message d'erreur habituel (`extractApiErrorMessages`, inchange).
  - Nouvelle action `resendConfirmationEmail(email: string): Promise<boolean>`, appelant
    `authService.resendConfirmation({ email })` et retournant `response?.isSuccess ?? false` (le
    backend renvoie toujours `isSuccess: true` de facon enumeration-safe, voir §7 — cette action
    ne sert donc qu'a signaler un succes d'appel HTTP, pas a distinguer "compte existant" de
    "compte inexistant").
- `src/pages/LoginPage.vue` :
  - Exposer `requiresEmailConfirmation`/`unconfirmedEmail` via `storeToRefs(authStore)`.
  - Nouveau bloc conditionnel (a cote du `q-banner` d'erreur existant, ligne 67-72), par exemple :
    ```html
    <q-banner v-if="requiresEmailConfirmation" class="text-white bg-warning q-mb-md rounded-borders">
      <template #avatar><q-icon name="mail" /></template>
      Votre compte n'est pas encore activé.
      <template #action>
        <q-btn flat color="white" no-caps label="Renvoyer l'email d'activation" @click="handleResend" />
      </template>
    </q-banner>
    ```
  - Nouvelle methode `handleResend`, appelant `authStore.resendConfirmationEmail(unconfirmedEmail.value)`
    puis affichant le meme message generique enumeration-safe que le backend (import de
    `useNotification`, deja utilise dans `RegisterPage.vue` sur le meme modele) :
    ```ts
    const { showSuccessNotification } = useNotification();

    const handleResend = async () => {
      if (!unconfirmedEmail.value) return;
      await authStore.resendConfirmationEmail(unconfirmedEmail.value);
      showSuccessNotification(
        "Si un compte existe pour cet email et n'est pas encore confirmé, un nouvel email d'activation vient d'être envoyé.",
      );
    };
    ```
  - `requiresEmailConfirmation` doit etre reinitialise (`false`) au debut de chaque nouvelle
    tentative de connexion (`handleLogin`), pour ne pas laisser le bandeau affiche apres une
    correction reussie.

### Message post-inscription a ajuster (`RegisterPage.vue`)

- Remplacer *"Compte cree avec succes ! Vous etes maintenant connecte."* (ligne 63) — deja faux
  avant ce ticket, **desormais franchement incorrect** avec l'activation stricte — par un message
  qui reflete la realite, ex. *"Compte cree avec succes ! Un email d'activation vous a ete
  envoye. Vous devez cliquer sur le lien recu par email avant de pouvoir vous connecter."*
- **Redirection modifiee** : `router.push('/')` (ligne 65) devient `router.push('/login')` —
  contrairement a la version precedente de cette spec (qui gardait `/`, coherent avec
  l'activation "douce" ou la connexion restait immediatement possible), l'activation stricte rend
  la prochaine etape logique explicitement "aller se connecter (une fois l'email confirme)", pas
  "parcourir les offres" : rediriger vers `/login` reduit la confusion plutot que de laisser le
  candidat sur la page d'accueil sans indication de la marche a suivre.

## Criteres d'acceptation verifiables

1. `dotnet build` reussit apres l'ensemble des changements.
2. `docker compose up` (stack complete, ou a minima `sqlserver azurite monolith-api smtp4dev
   communication-service`) demarre sans erreur.
3. **Migration de grandfathering** : creer manuellement (ou via un seed anterieur) un utilisateur
   avec `EmailConfirmed = false` **avant** d'appliquer la nouvelle migration ; apres
   `dotnet ef database update`, cet utilisateur a `EmailConfirmed = true` et peut se connecter
   normalement (non-regression pour les comptes preexistants).
4. `POST /api/auth/register/candidate` (payload valide, candidat inexistant) : `200`, compte cree
   avec `EmailConfirmed = false`, et un email apparait dans smtp4dev
   (`http://localhost:${SMTP_UI_PORT:-5080}`) avec un lien
   `http://localhost:3000/confirm-email?email=...&token=...` valide et URL-encode.
5. **Activation stricte — cas central de ce ticket** : tenter `POST /api/auth/login` avec les
   identifiants du compte cree au critere 4 (email confirme non encore fait) : `400`,
   `isSuccess: false`, `data.requiresEmailConfirmation: true`, message explicite mentionnant la
   necessite de confirmer l'email. Aucun jeton n'est retourne.
6. Copier le lien recu dans smtp4dev, appeler `POST /api/auth/confirm-email` avec `email`/`token` :
   `200`, `EmailConfirmed = true` en base. Reproduire ensuite le critere 5 (meme identifiants) :
   `200`, connexion reussie avec jeton, cette fois `requiresEmailConfirmation` absent/`false`.
7. Reproduire le critere 6 en attendant au moins 10 minutes entre l'inscription et la confirmation :
   toujours `200` (demontre le correctif du provider de token).
8. **Renvoi — compte existant non confirme** : `POST /api/auth/resend-confirmation` avec l'email
   d'un compte cree mais non confirme : `200`, message generique, un **nouvel** email apparait
   dans smtp4dev avec un token valide (different du precedent) ; confirmer avec ce nouveau token
   fonctionne (`POST /api/auth/confirm-email` -> `200`).
9. **Renvoi — enumeration-safety** : `POST /api/auth/resend-confirmation` avec (a) un email qui
   n'existe dans aucun compte, et (b) l'email d'un compte deja confirme : dans les deux cas,
   `200` avec exactement le meme message generique qu'au critere 8, et **aucun email n'est envoye**
   dans smtp4dev pour ces deux cas (verifiable par l'absence de nouvel email dans l'UI web).
10. **Cooldown par email** : deux appels consecutifs a `POST /api/auth/resend-confirmation` avec le
    meme email (compte existant non confirme), moins de 60 secondes d'intervalle : le premier
    declenche un envoi reel (verifiable dans smtp4dev), le second retourne `200` avec le meme
    message generique mais **n'envoie pas** de second email.
11. **Rate limiting par IP** : plus de 5 appels a `POST /api/auth/resend-confirmation` en moins de
    15 minutes depuis la meme origine : le(s) appel(s) suivant(s) retourne(nt) `429`.
12. **Interaction `RegisterAsync` (Constat point 5)** : un utilisateur cree via
    `POST /api/auth/register` (mode JWT local, `EmailConfirmed = false`) ne peut pas se connecter
    tant qu'il n'a pas ete confirme (`400`, `requiresEmailConfirmation: true`, meme comportement
    que pour un candidat) ; appeler `POST /api/auth/resend-confirmation` avec son email declenche
    bien un envoi reel et le debloque une fois le lien recu confirme — demontre que ce type de
    compte n'est pas bloque de facon permanente malgre l'absence de branchement automatique dans
    `RegisterAsync` (decision 2).
13. Arreter le conteneur `communication-service` (ou rendre `CommunicationService:BaseUrl`
    injoignable), puis rappeler `POST /api/auth/register/candidate` : toujours `200`, le compte
    est cree normalement en base (aucun rollback, voir §11), erreur loggee cote serveur.
14. `candidate-app` : apres inscription reussie, le message affiche ne pretend plus une connexion
    automatique inexistante et redirige vers `/login` ; sur `/login`, une tentative de connexion
    avec un compte non confirme affiche un bandeau explicite avec un bouton "Renvoyer l'email
    d'activation", dont le clic affiche un message generique de confirmation d'envoi.
15. `POST /api/auth/register` (endpoint admin) : comportement de creation **inchange** par ce
    ticket (toujours aucun email envoye automatiquement a la creation) — non-regression
    volontaire, voir decision 2 et critere 12 pour l'interaction avec l'activation stricte.
16. Aucune regression sur les champs/comportements de `RegisterCandidateDto` non concernes par ce
    ticket (validation des experiences, transaction, upload de CV, attribution du role Candidate,
    etc.).
17. **Test unitaire existant corrige** (Constat point 9) :
    `AuthenticationServiceTests.LoginAsync_WithUnconfirmedEmail_ShouldReturnFailure` est mis a jour
    pour mocker explicitement `CheckPasswordSignInAsync(...)` avec `SignInResult.NotAllowed`, et
    l'assertion verifie le nouveau message/`Data.RequiresEmailConfirmation == true` plutot que le
    message generique d'exception. Le helper `CreateAuthenticationService` (et tout autre test
    instanciant `AuthenticationService` directement) est mis a jour pour fournir les nouvelles
    dependances du constructeur (`IEmailNotificationService`, `IOptions<FrontendSettings>`,
    `IValidator<ResendConfirmationDto>`).

## Hors perimetre

- CommunicationService : aucun changement, le contrat du ticket A couvre exactement ce besoin.
- Blocage de la connexion tant que l'email n'est pas confirme : **n'est plus hors perimetre** —
  c'est desormais le comportement retenu (decision 1). Cette section ne liste que ce qui reste
  reellement hors perimetre.
- `RegisterAsync` (endpoint admin, utilisateurs d'organisation) : la creation de compte elle-meme
  reste inchangee (pas d'envoi d'email automatique a la creation, decision 2) — dette technique
  documentee, attenuee par le fait que le mecanisme de renvoi generique (§7) permet de debloquer
  ces comptes sans code supplementaire (voir Constat point 5, Critere d'acceptation 12).
- `ForgotPasswordAsync`/`ResetPasswordAsync` : meme lacune preexistante (token genere, jamais
  envoye par email), non traitee ici — reutiliserait `IEmailNotificationService` si un futur
  ticket l'aborde, mais necessiterait un nouveau template cote CommunicationService (seul
  `AccountActivation` existe aujourd'hui).
- Suppression des colonnes mortes `User.EmailConfirmationToken`/`EmailConfirmationTokenExpiry` :
  pas de migration de nettoyage de schema dans ce ticket.
- Correction du jeton d'authentification manquant apres inscription candidat
  (`AuthResponseDto.AccessToken` toujours `null` apres `RegisterCandidateAsync`) : comportement
  preexistant, non corrige ici — sans consequence pratique nouvelle puisque, de toute facon, la
  connexion immediate n'est plus possible avec l'activation stricte.
- Multi-langue du template d'activation : seul `fr-FR` existe cote CommunicationService ; ce
  ticket envoie systematiquement `language: "fr-FR"`, y compris pour le renvoi.
- Verrouillage de compte specifique au renvoi (ex. blocage permanent d'une IP recidiviste au-dela
  du rate limiting en fenetre glissante) : le rate limiting fixed-window + le cooldown par email
  (§8-9) sont juges suffisants pour ce ticket ; un dispositif plus sophistique (ex. CAPTCHA,
  blocage IP persistant, alerte de securite) est hors perimetre, a envisager seulement si un abus
  reel est constate.
- Deploiement multi-instance de `MonolithApi` avec cooldown/rate-limiting partages (Redis ou
  equivalent) : le cooldown par email (§8) est process-local, limite deja acceptee pour
  `EntraIdRateLimitService` dans ce meme codebase, non traitee differemment ici.
- Tests automatises au-dela du point 17 (Criteres d'acceptation) : l'ecriture exhaustive de
  nouveaux tests (`ResendConfirmationEmailAsync`, rate limiting, migration) releve de l'agent
  developer ; cette spec liste les cas a couvrir sans en imposer l'implementation exacte.

## Fichiers a creer/modifier (recapitulatif)

Backend (`XpertSphere.MonolithApi`) :
- `Extensions/SecurityExtensions.cs` : `ConfigureSignInOptions` (`RequireConfirmedEmail = true`),
  `ConfigureTokenOptions` (`EmailConfirmationTokenProvider = DefaultProvider`).
- Nouvelle migration EF Core `Migrations/<timestamp>_GrandfatherExistingUnconfirmedAccounts.cs`
  (donnees uniquement, voir §2).
- `DTOs/Auth/AuthResponseDto.cs` : nouveau champ `RequiresEmailConfirmation`.
- `Mappings/AuthMappingProfile.cs` : nouvelle regle de mapping pour ce champ.
- `Utils/Results/AuthResult.cs` : `Failure(...)` — nouveau parametre optionnel `data`.
- `Interfaces/IEmailNotificationService.cs` (nouveau).
- `Services/EmailNotificationService.cs` (nouveau).
- `DTOs/Communication/SendTemplatedEmailRequestDto.cs` (nouveau).
- `Config/FrontendSettings.cs` (nouveau).
- `DTOs/Auth/ResendConfirmationDto.cs` (nouveau).
- `Validators/Auth/ResendConfirmationDtoValidator.cs` (nouveau).
- `Utils/ResendConfirmationCooldown.cs` (nouveau).
- `Interfaces/IAuthenticationService.cs` : nouvelle methode `ResendConfirmationEmailAsync`.
- `Services/AuthenticationService.cs` :
  - `LoginAsync` : branche `IsNotAllowed` mise a jour (message + `Data`).
  - `RegisterCandidateAsync` : construction du lien via `BuildActivationLink`, appel
    `IEmailNotificationService`, retrait de l'assignation morte `user.EmailConfirmationToken`.
  - Nouvelle methode `ResendConfirmationEmailAsync` + methode privee `BuildActivationLink`.
  - Nouvelles dependances constructeur : `IEmailNotificationService`,
    `IOptions<FrontendSettings>`, `IValidator<ResendConfirmationDto>`.
- `Controllers/AuthController.cs` : nouvelle action `POST /api/auth/resend-confirmation`
  (`[AllowAnonymous]`, `[EnableRateLimiting("resend-confirmation")]`).
- `Program.cs` : `Configure<FrontendSettings>`, `AddHttpClient<IEmailNotificationService,
  EmailNotificationService>`, `AddRateLimiter(...)`, `app.UseRateLimiter()`.
- `appsettings.json`, `appsettings.Development.json` : sections `CommunicationService`,
  `Frontend`.
- `docker-compose.yml` (racine) : service `monolith-api`.
- `XpertSphere.MonolithApi.Tests/Services/AuthenticationServiceTests.cs` : correction du test
  existant (Constat point 9, Critere d'acceptation 17), mise a jour du helper d'instanciation pour
  les nouvelles dependances constructeur.

Frontend `candidate-app` :
- `src/pages/ConfirmEmailPage.vue` (nouveau).
- `src/router/routes.ts` (nouvelle route `confirm-email`).
- `src/services/authService.ts` : `confirmEmail` + nouvelle methode `resendConfirmation`.
- `src/models/auth.ts` : `ConfirmEmailDto`, nouveau champ `requiresEmailConfirmation?: boolean`
  sur `AuthResponseDto`, nouveau type `ResendConfirmationDto`.
- `src/stores/authStore.ts` : nouveaux refs `requiresEmailConfirmation`/`unconfirmedEmail`,
  nouvelle action `resendConfirmationEmail`, `login(...)` mis a jour.
- `src/pages/LoginPage.vue` : nouveau bandeau conditionnel + bouton de renvoi.
- `src/pages/RegisterPage.vue` : message post-inscription et redirection (`/login` au lieu de
  `/`) mis a jour.

Aucun changement dans `XpertSphere.CommunicationService`, ni dans `recruiter-app`,
`ReportingService`, `IntegrationService`, `ResumeAnalyzer`.
