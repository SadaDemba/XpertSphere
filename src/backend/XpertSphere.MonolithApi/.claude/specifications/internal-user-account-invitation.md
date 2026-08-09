# Activation par invitation des comptes utilisateurs internes (recruteurs, org-admins, admins plateforme)

## Contexte et périmètre

`candidate-account-activation-email.md` a livré l'activation par email pour le flux candidat
(`POST /api/auth/register/candidate`) : email réellement envoyé, `RequireConfirmedEmail = true`
(réglage **global** ASP.NET Core Identity, pas scopé par type d'utilisateur), renvoi via
`POST /api/auth/resend-confirmation`. Cette spec **ne rouvre aucune de ces décisions côté
candidat** — le flux candidat reste inchangé à tous égards.

Le trou qu'elle a documenté sans le combler (§Constat point 5 et §Hors périmètre de cette spec) :
un compte interne (recruteur, `Organization.Admin`, `Organization.Manager`,
`Organization.TechnicalEvaluator`, `XpertSphere.Admin`, `XpertSphere.SuperAdmin`) créé via
`POST /api/users` (`UserService.CreateAsync`) est **déjà bloqué à la connexion** par l'activation
stricte globale, mais ne reçoit **aucun email** pour en sortir. Pire, contrairement à ce
qu'affirmait `candidate-account-activation-email.md` §Constat 5 pour `RegisterAsync`, le mécanisme
de renvoi générique existant (`POST /api/auth/resend-confirmation`) **n'est pas non plus une
échappatoire viable pour ces comptes** dans son état actuel : voir §Constat point 6 ci-dessous.

Cette spec couvre :
1. La création d'un compte interne sans mot de passe utilisable (`POST /api/users`), avec envoi
   réel d'un email d'**invitation** (pas d'activation — vocabulaire volontairement distinct, voir
   Décisions).
2. Un nouveau endpoint public `POST /api/auth/accept-invitation` : le compte définit lui-même son
   mot de passe, ce qui active/confirme le compte du même coup.
3. Une nouvelle page `recruiter-app` consommant ce endpoint, avec durcissement anti-scanner
   (activation sur clic explicite, pas au chargement).
4. Le nouveau template `AccountInvitation` côté `CommunicationService`.
5. La correction du comportement de `POST /api/auth/resend-confirmation` pour les comptes internes
   (branche différente de celle des candidats — voir §Constat point 6 et §Comportement cible §7).
6. Le sort de `RegisterAsync` (`POST /api/auth/register`) : unifié dans ce flux (voir §Constat
   point 5 et §Comportement cible §8).

Hors périmètre : Entra ID (voir Décisions point 1), tout ce qui concerne le flux candidat
(inchangé), `ForgotPasswordAsync`/`ResetPasswordAsync` côté candidat ou interne au-delà du
changement de provider de token nécessaire à cette spec (voir §Constat point 2 — changement
partagé, mais l'usage fonctionnel de `ForgotPasswordAsync` reste hors périmètre).

## Décisions déjà prises par l'utilisateur (ne pas les rouvrir)

1. UX = lien d'**invitation**. L'admin crée le compte sans mot de passe utilisable ; l'invité
   reçoit un email avec un lien qui lui permet de définir lui-même son mot de passe, ce qui
   active/confirme le compte du même coup.
2. Service email = Brevo (déjà branché, voir `email-sending-foundation.md`). Aucun nouveau service
   payant.
3. Les comptes seedés restent exemptés (`EmailConfirmed = true`, créés directement via
   `UserManager.CreateAsync` dans `Extensions/DatabaseExtensions.cs`/`DatabaseExtensions.DemoData.cs`,
   jamais via `UserService.CreateAsync` — aucun changement à faire là, confirmé à l'exploration).
4. Durcissement anti-scanner : préférer un clic explicite (bouton) à toute action déclenchée au
   chargement de la page.

## Constat — état actuel du code

Exploration de `Services/UserService.cs`, `Services/AuthenticationService.cs`,
`Extensions/SecurityExtensions.cs`, `DTOs/User/CreateUserDto.cs`,
`Validators/User/CreateUserDtoValidator.cs`, `Mappings/UserMappingProfile.cs`,
`Config/FrontendSettings.cs`, `Controllers/AuthController.cs`, `Controllers/UsersController.cs`,
`Utils/Roles.cs`, `Extensions/DatabaseExtensions*.cs`, ainsi que `recruiter-app`
(`quasar.config.ts`, `src/router/index.ts`, `src/router/guards/authGuard.ts`,
`src/pages/admin/UsersPage.vue`, `src/composables/passwordRules.ts`) et `CommunicationService`
(`Models/MessageTemplate.cs`, `Services/TemplateService.cs`) :

1. **Modèle d'auth réel des utilisateurs internes en local — le cas prioritaire de cette spec.**
   `AuthenticationService.ShouldUseEntraId` (`!_environment.IsDevelopment() &&
   USE_ENTRA_ID == "true"`) est **toujours `false` en Development**, quel que soit le type
   d'utilisateur (organisationnel ou candidat) — `AddMultiModeAuthentication`
   (`SecurityExtensions.cs`) force également l'authentification JWT pure dès que
   `environment.IsDevelopment()`. **En local (démos de l'utilisateur, Azure non payé
   actuellement), tout utilisateur — interne ou candidat — s'authentifie donc en JWT local par
   mot de passe, jamais via Entra ID.** Le flux « définir son mot de passe » de cette spec a donc
   un sens plein et immédiat dans ce mode, qui est le mode réellement exercé aujourd'hui.
   En Staging/Production avec `USE_ENTRA_ID=true`, un utilisateur organisationnel
   (`OrganizationId` non nul) serait redirigé vers Entra ID B2B (`GenerateEntraIdLoginUrl`) et son
   compte serait provisionné par `ClaimsEnrichmentMiddleware` (qui positionne déjà
   `EmailConfirmed = true`) — cette spec **ne change rien à ce chemin**, elle ne s'applique qu'aux
   comptes internes en mode JWT local (Development, et tout déploiement où `USE_ENTRA_ID` resterait
   `false`, y compris potentiellement Staging tant qu'Entra ID n'est pas configuré/payé).
2. **Bug bloquant à corriger dans cette spec, sur le même principe que
   `candidate-account-activation-email.md` §Constat 2** : `ConfigureTokenOptions`
   (`SecurityExtensions.cs:87-90`) laisse `options.Tokens.PasswordResetTokenProvider =
   TokenOptions.DefaultEmailProvider` (TOTP, fenêtre de quelques minutes) — explicitement documenté
   comme hors périmètre par ce ticket précédent. **Cette spec-ci en dépend directement** : la
   stratégie de token retenue (§Comportement cible §3) est `GeneratePasswordResetTokenAsync`, donc
   tant que ce provider reste sur `DefaultEmailProvider`, un lien d'invitation envoyé par email
   serait quasiment mort en quelques minutes — exactement le bug que
   `candidate-account-activation-email.md` a corrigé pour la confirmation d'email, non corrigé pour
   la réinitialisation de mot de passe. Correctif retenu ici : basculer
   `PasswordResetTokenProvider` sur `TokenOptions.DefaultProvider` (déjà enregistré par
   `.AddDefaultTokenProviders()`, aucun nouveau provider à créer — même correctif mécanique que
   celui déjà fait pour `EmailConfirmationTokenProvider`).
   - **Conséquence assumée, à documenter explicitement** : `ForgotPasswordAsync`/`ResetPasswordAsync`
     (candidat et interne, tous deux inchangés fonctionnellement par cette spec — ils ne
     déclenchent toujours aucun email, voir Hors périmètre) voient la durée de vie de leur jeton
     passer de quelques minutes à `DataProtectionTokenProviderOptions.TokenLifespan` (1 jour, déjà
     configuré). C'est une amélioration silencieuse sans régression visible (le jeton n'est de
     toute façon jamais envoyé aujourd'hui par ces deux méthodes), pas une réouverture du périmètre
     de `ForgotPasswordAsync` lui-même.
   - Durée de vie du lien d'invitation avec ce choix : **1 jour** (`TokenLifespan` global, partagé
     avec la confirmation d'email candidat et le reset de mot de passe — pas de provider dédié plus
     long dans cette spec, voir §Points à confirmer).
3. **`CreateUserDto.EmailConfirmed` est un champ client-modifiable qui sabote silencieusement
   l'objectif de cette spec s'il n'est pas neutralisé.** `CreateUserDto.EmailConfirmed` (défaut
   `false`) est mappé automatiquement (nom de propriété identique, aucune règle `ForMember`
   dédiée dans `UserMappingProfile.cs`) vers `User.EmailConfirmed`. Un appelant de
   `POST /api/users` peut aujourd'hui envoyer `emailConfirmed: true` et créer un compte interne
   déjà confirmé sans jamais passer par l'invitation — contournement total de cette spec. Avec la
   valeur par défaut actuelle (`false`), c'est déjà le bug initial constaté par l'utilisateur (compte
   créé non confirmé, jamais débloqué).
4. **`UserService.CreateAsync` appelle `_userManager.CreateAsync(user, dto.Password)` avec un mot
   de passe obligatoire (`[Required]` sur `CreateUserDto.Password`).** Le flux d'invitation exige de
   créer un compte **sans mot de passe utilisable**. `UserManager<TUser>` expose une surcharge
   `CreateAsync(TUser user)` (sans mot de passe) qui crée l'utilisateur avec `PasswordHash = null` —
   c'est cette surcharge qu'il faut utiliser pour ce flux, pas un mot de passe aléatoire jamais
   communiqué (qui laisserait un hash valide inutilement en base sans bénéfice).
5. **`RegisterAsync` (`POST /api/auth/register`, `Authorize(Policy = "CanCreateUsers")`) est un
   second chemin de création de compte interne, actuellement mort côté frontend** : aucune
   occurrence de son usage n'existe dans `recruiter-app` ni `candidate-app` (recherche exhaustive
   des deux packages) — seul `UsersPage.vue` (`POST /api/users` via `UserService.CreateAsync`) est
   réellement utilisé pour créer un utilisateur interne depuis l'UI. `RegisterAsync` crée déjà
   l'utilisateur avec `EmailConfirmed = false` et un mot de passe fourni par l'appelant
   (`registerDto.Password`), génère un jeton de confirmation d'email jamais utilisé
   (`user.EmailConfirmationToken = emailConfirmationToken` — assignation mort-née, jamais
   persistée ni transmise, même vestige que documenté dans `candidate-account-activation-email.md`
   §Constat point 1) et n'envoie aucun email. `candidate-account-activation-email.md` §Constat
   point 5 avait qualifié cet endpoint de dette technique « inoffensive » car
   `resend-confirmation` permettait de débloquer ces comptes après coup — **ce raisonnement ne
   tient plus** une fois combiné au constat suivant.
6. **`POST /api/auth/resend-confirmation` est aujourd'hui incorrect pour tout compte interne, et le
   restera pour tout compte créé via l'invitation de cette spec si rien n'est changé** — ceci
   **contredit directement** `candidate-account-activation-email.md` §Constat point 5 et son
   critère d'acceptation 12, qui présentaient cet endpoint comme un filet de sécurité générique
   « par email », indifférent au type de compte. En réalité :
   - `ResendConfirmationEmailAsync` envoie systématiquement le template `AccountActivation` avec un
     lien construit par `BuildActivationLink` vers **`candidate-app`** (`/#/confirm-email`), jamais
     vers `recruiter-app` — un recruteur qui cliquerait sur ce lien atterrirait sur l'application
     candidat.
   - Le jeton envoyé est un jeton de **confirmation d'email** (`GenerateEmailConfirmationTokenAsync`
     + `ConfirmEmailAsync`), pas un jeton de **réinitialisation de mot de passe**. Pour un compte
     créé sans mot de passe par cette spec (§Constat point 4, `PasswordHash = null`), confirmer
     l'email positionnerait `EmailConfirmed = true` **sans jamais permettre de définir un mot de
     passe** — le compte resterait définitivement inutilisable (un utilisateur avec `PasswordHash
     = null` et `EmailConfirmed = true` ne peut toujours pas se connecter, faute de mot de passe à
     vérifier), et pire, une fois `EmailConfirmed = true`, le filtre `user.EmailConfirmed` de
     `ResendConfirmationEmailAsync` (`if (user == null || user.EmailConfirmed) { ... }`) empêcherait
     tout nouveau renvoi — le compte serait bloqué **définitivement**, sans aucun recours restant.
   - **Correctif retenu dans cette spec** (§Comportement cible §7) : `ResendConfirmationEmailAsync`
     doit brancher sur `user.OrganizationId.HasValue` (même discriminant déjà utilisé par
     `AuthenticationService.GetUserTypeAsync`, et déjà garanti `null` pour tout candidat par
     `RegisterCandidateAsync`) — si vrai, renvoyer une **invitation** (jeton de reset de mot de
     passe + lien `recruiter-app`) ; si faux, comportement candidat inchangé (jeton de confirmation
     d'email + lien `candidate-app`). Ce correctif s'applique rétroactivement à **tout** compte
     interne existant non confirmé, y compris ceux déjà créés avant cette spec via
     `UserService.CreateAsync` ou `RegisterAsync` — cohérent avec l'objectif énoncé par
     l'utilisateur (« tout compte ajouté, hors seed, doit être activé par email »).
7. **`Roles.InternalRoles`** (`Utils/Roles.cs`) liste exactement les rôles concernés par cette
   spec : `XpertSphere.SuperAdmin`, `XpertSphere.Admin`, `Organization.Admin`,
   `Organization.Manager`, `Organization.Recruiter`, `Organization.TechnicalEvaluator`. Le
   discriminant technique retenu n'est cependant pas le rôle (non encore assigné à la création,
   voir point 8) mais `OrganizationId.HasValue` — tout utilisateur créé via `POST /api/users` a par
   construction `OrganizationId` renseigné pour un profil organisationnel, ou non renseigné pour un
   profil plateforme (`XpertSphere.Admin`/`XpertSphere.SuperAdmin`, qui n'appartiennent à aucune
   organisation cliente). **Point d'attention** : ce discriminant ne distingue donc pas nativement
   les comptes plateforme des comptes candidats potentiels créés par erreur via `POST /api/users` —
   voir §Points à confirmer, point (a).
8. **L'attribution du rôle a lieu après la création**, via un endpoint séparé
   (`UserRoleService`/`UserRolesController`, hors périmètre de cette spec) — au moment où
   l'invitation est envoyée, l'utilisateur invité n'a encore **aucun rôle actif**. Une fois
   l'invitation acceptée, l'utilisateur peut se connecter mais n'a accès à rien tant qu'un rôle ne
   lui a pas été assigné séparément par l'admin. Comportement **inchangé** par cette spec (déjà le
   cas aujourd'hui pour tout compte créé via `POST /api/users`, invitation ou non) — documenté ici
   pour clarté, pas un défaut introduit par cette spec.
9. **`UsersPage.vue` (`recruiter-app`) expose aujourd'hui un champ mot de passe obligatoire** à la
   création d'un utilisateur (`userForm.password`, ligne 274, règle `!!val || 'Le mot de passe est
   requis'`), envoyé tel quel dans le payload `POST /api/users` (ligne 759). Ce champ doit
   disparaître du formulaire de création (voir Coordination frontend) — le dialog « Reset
   Password » (`AdminResetPasswordDto`, `POST /api/auth/admin-reset-password`) est un flux distinct,
   inchangé, qui reste pertinent pour un compte déjà activé.
10. **Le garde de routes `recruiter-app` (`src/router/guards/authGuard.ts`) n'utilise pas
    `route.meta.requiresAuth`** contrairement à ce qu'une lecture rapide pourrait laisser supposer
    par analogie avec `candidate-app` : les pages publiques y sont une liste blanche **en dur**,
    `const publicPages = ['/auth/login', '/auth/forgot-password']`. **Toute nouvelle route
    publique doit être ajoutée explicitement à ce tableau**, sous peine d'un redirect silencieux
    vers `/auth/login` dès l'ouverture du lien d'invitation (voir Coordination frontend).
11. **`recruiter-app` tourne aussi en mode routeur HASH** (`quasar.config.ts:48`,
    `vueRouterMode: 'hash'`, identique à `candidate-app`) — même piège que documenté dans
    `candidate-account-activation-email.md` pour `BuildActivationLink` : le lien d'invitation vers
    `recruiter-app` doit être construit avec `/#/` sous peine d'atterrir sur la page par défaut de
    la SPA sans jamais afficher la page d'acceptation.
12. **`FrontendSettings` (`Config/FrontendSettings.cs`) n'expose aujourd'hui que
    `CandidateAppBaseUrl`** — aucune URL pour `recruiter-app` n'existe côté configuration. Nécessite
    l'ajout de `RecruiterAppBaseUrl`, sur le même modèle exact (valeur = adresse navigateur exposée,
    jamais un nom de service Docker interne).
13. **`CommunicationService` n'a aucun template autre que `AccountActivation`** (`Services/
    TemplateService.cs`, dictionnaire statique en mémoire) et `Models/MessageTemplate.cs` n'a pas
    de valeur d'enum `TemplateType.AccountInvitation` — **contrairement à ce que concluait
    `email-sending-foundation.md`** (« le contrat de `POST /api/emails/send` couvre exactement le
    besoin » ne visait que le besoin du ticket B candidat) — cette spec-ci **nécessite un
    changement réel côté `CommunicationService`** (nouveau template), voir §Coordination
    CommunicationService.
14. **Validateurs FluentValidation enregistrés par scan d'assembly**
    (`Extensions/FluentValidationExtensions.cs:18`,
    `services.AddValidatorsFromAssembly(typeof(FluentValidationExtensions).Assembly)`) — le nouveau
    validator de cette spec n'a besoin d'aucune ligne d'enregistrement manuelle, juste d'exister
    dans l'assembly.
15. **Sites de test à mettre à jour** : `UserService` est instancié directement dans
    `XpertSphere.MonolithApi.Tests/Services/UserServiceProfileCurrencyTests.cs` et
    `UserServiceCvDownloadTests.cs` (au-delà des mocks `Mock<IUserService>` utilisés ailleurs, qui
    n'ont pas besoin de changement) — ces deux fichiers devront fournir la nouvelle dépendance
    constructeur `IEmailNotificationService`. Trois fichiers instancient
    `AuthenticationService` directement et devront tous fournir la nouvelle dépendance
    `IValidator<AcceptInvitationDto>` (voir §Comportement cible §5) :
    `AuthenticationServiceTests.cs` (déjà identifié par `candidate-account-activation-email.md`
    pour un autre correctif), et **deux fichiers non couverts par cette spec précédente** —
    `AuthenticationServiceProfileCompletenessTests.cs` (méthode d'aide
    `CreateAuthenticationService`, même liste de paramètres) et
    `SimpleAuthenticationTests.cs`, ce dernier ne construisant pas `AuthenticationService`
    lui-même mais contenant un test `RegisterDto_ShouldHaveRequiredProperties` qui **instancie un
    `RegisterDto` avec `Password`/`ConfirmPassword`** (lignes ~140-148) : ce test cesse de compiler
    une fois ces deux propriétés retirées de `RegisterDto` (voir Constat point 16) et doit être
    réécrit sans elles. `Helpers/AutoMapperHelper.cs` a été vérifié à l'exploration : aucune
    référence à `CreateUserDto`/`RegisterDto`/leurs champs `Password`/`EmailConfirmed`, donc aucun
    changement nécessaire de ce côté malgré le retrait de ces propriétés.
16. **`Validators/Auth/RegisterUserDtoValidator.cs` (nom de fichier trompeur : il valide bien
    `RegisterDto`, pas un type `RegisterUserDto` distinct qui n'existe pas) contient des règles sur
    `Password`/`ConfirmPassword`** (`RuleFor(x => x.Password)...`, `RuleFor(x => x.ConfirmPassword)...`,
    lignes 15-20). Ce sont des expressions lambda résolues à la compilation contre `RegisterDto` :
    **retirer `RegisterDto.Password`/`ConfirmPassword` (§8) sans retirer ces deux `RuleFor` dans ce
    validator provoque une erreur de compilation** (`x => x.Password` ne compile plus une fois la
    propriété absente du type). Les deux retraits doivent être faits dans le même changement. Les
    autres règles de ce validator (email, prénom/nom, téléphone,
    `AcceptTerms`/`AcceptPrivacyPolicy`) restent inchangées.
17. **Chevauchement avec `localize-identity-error-messages.md`** : cette spec précédente a traduit
    les `DataAnnotations` de `DTOs/Auth/RegisterDto.cs` (dont les messages sur `Password`/
    `ConfirmPassword`, déjà en français) et de `DTOs/User/CreateUserDto.cs` (dont le message sur
    `Password`) — retirer ces deux propriétés (§3, §8) retire de facto les `DataAnnotations`
    correspondantes déjà traduites par ce ticket précédent. C'est une **suppression volontaire de
    code déjà traité**, pas une régression de traduction : à documenter comme telle si le
    `validator` compare les deux specs ligne à ligne sur ces fichiers.
18. **Chevauchement avec `passwordRules.ts` (`recruiter-app`)** : son docblock énumère
    explicitement « 3 emplacements actifs (`UsersPage.vue` création et dialog "Reset Password",
    `ProfilePage.vue`) ». Après cette spec, le formulaire de création de `UsersPage.vue` ne
    consomme plus ces règles (champ retiré) mais `AcceptInvitationPage.vue` (nouveau) les consomme
    à sa place — toujours 3 emplacements, mais un ensemble différent. Le commentaire doit être mis
    à jour (voir Coordination frontend).
19. **Cas limite non anodin : le dialog admin "Reset Password" existant peut être utilisé sur un
    compte dont l'invitation n'a pas encore été acceptée**, sans que rien ne l'en empêche
    aujourd'hui (`AdminResetPasswordAsync`, `Services/AuthenticationService.cs:742-820`,
    `POST /api/auth/admin-reset-password`, policy `CanResetPasswords`) — c'est une action adjacente,
    sur la même ligne de tableau que la ligne créée par cette spec. `AdminResetPasswordAsync`
    appelle `RemovePasswordAsync` + `AddPasswordAsync` mais **ne touche jamais `EmailConfirmed`**.
    Sans correctif, un admin qui définirait un mot de passe via ce dialog sur un compte encore non
    confirmé croirait avoir débloqué l'utilisateur, qui resterait pourtant bloqué à la connexion
    (`RequireConfirmedEmail = true`) avec le même message qu'avant — exactement l'échec que cette
    spec vise à éliminer, atteignable par un bouton déjà en place. **Correctif retenu** (voir
    §Comportement cible §9) : `AdminResetPasswordAsync` positionne aussi `EmailConfirmed = true`
    en cas de succès — un admin qui définit lui-même un mot de passe hors-bande est une garantie au
    moins aussi forte qu'un aller-retour email, donc au moins aussi légitime pour confirmer le
    compte.

## Comportement cible

### 1. `Extensions/SecurityExtensions.cs` — correctif du provider de token de reset

```csharp
private static void ConfigureTokenOptions(IdentityOptions options)
{
    options.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultProvider; // inchangé
    // Correctif (voir Constat point 2) : DefaultEmailProvider (TOTP, fenêtre de quelques minutes)
    // rendrait un lien d'invitation envoyé par email quasi inutilisable en pratique, exactement le
    // bug déjà corrigé pour la confirmation d'email par candidate-account-activation-email.md.
    options.Tokens.PasswordResetTokenProvider = TokenOptions.DefaultProvider; // était DefaultEmailProvider
}
```

**Point d'attention pour le développeur, à vérifier empiriquement** : un jeton de reset de mot de
passe généré doit rester valide au moins 10-15 minutes plus tard (même vérification que celle déjà
faite pour la confirmation d'email par le ticket précédent).

### 2. `Config/FrontendSettings.cs` — nouvelle URL

```csharp
public class FrontendSettings
{
    public string CandidateAppBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Adresse navigateur exposée de recruiter-app (jamais un nom de service Docker interne),
    /// utilisée pour construire le lien d'invitation des comptes internes.
    /// </summary>
    public string RecruiterAppBaseUrl { get; set; } = string.Empty;
}
```

`appsettings.json` : ajouter `"RecruiterAppBaseUrl": ""`. `appsettings.Development.json` : ajouter
`"RecruiterAppBaseUrl": "http://localhost:3001"` (port `recruiter-app` en dev, cohérent avec
`RECRUITER_APP_PORT:-3001` de `docker-compose.yml`). `appsettings.Staging.json` (non lisible depuis
cet environnement d'exploration, contenu non vérifié directement) : ajouter la même clé selon le
même schéma que `CandidateAppBaseUrl` y figure déjà, si c'est le cas — sinon l'ajouter en cohérence
avec le reste du fichier, le développeur doit vérifier ce fichier avant de le modifier à l'aveugle.

### 3. `DTOs/User/CreateUserDto.cs` — retrait du mot de passe côté appelant, verrouillage d'`EmailConfirmed`

```csharp
// Password retiré : le compte est créé sans mot de passe utilisable (PasswordHash = null),
// l'invité définit lui-même son mot de passe via POST /api/auth/accept-invitation. Voir Constat
// points 3 et 4.
// [Required(ErrorMessage = "Le mot de passe est obligatoire")]
// public string? Password { get; set; }   <-- supprimé

// EmailConfirmed retiré du DTO exposé à l'appelant (Constat point 3) : ce n'est jamais à
// l'appelant de POST /api/users de décider si le compte est confirmé - seul le flux
// d'acceptation d'invitation peut le faire passer à true.
// public bool EmailConfirmed { get; set; } = false;   <-- supprimé
```

Alternative écartée : garder `EmailConfirmed` dans le DTO mais l'ignorer côté mapping
(`.ForMember(dest => dest.EmailConfirmed, opt => opt.Ignore())` dans `UserMappingProfile.cs`, puis
fixer `user.EmailConfirmed = false;` explicitement dans `UserService.CreateAsync`). Retenue en
alternative documentée si le développeur préfère ne pas casser un éventuel appelant existant qui
enverrait ce champ (aucun appelant frontend ne l'envoie aujourd'hui, vérifié dans `UsersPage.vue`)
— dans les deux cas, le résultat observable doit être : **`EmailConfirmed` est toujours `false` à
la création via `POST /api/users`, sans exception, quel que soit ce que l'appelant HTTP envoie.**

### 4. `Services/UserService.cs` — `CreateAsync` : création sans mot de passe + envoi de l'invitation

Nouvelle dépendance injectée : `IEmailNotificationService emailNotificationService`,
`IOptions<FrontendSettings> frontendSettings`.

Remplacer :
```csharp
var result = await _userManager.CreateAsync(user, dto.Password);
```
par :
```csharp
// Compte créé sans mot de passe utilisable (PasswordHash = null) : l'invité définit lui-même son
// mot de passe via l'acceptation de l'invitation (voir Constat point 4).
var result = await _userManager.CreateAsync(user);
```

Après le succès de la création (avant le `return ServiceResult<UserDto>.Success(...)` existant),
sur le même modèle « pas de rollback en cas d'échec d'envoi » que
`candidate-account-activation-email.md` §11 :
```csharp
var invitationToken = await _userManager.GeneratePasswordResetTokenAsync(user);
var invitationLink = BuildInvitationLink(user.Email!, invitationToken);

var invitationSent = await _emailNotificationService.SendAccountInvitationEmailAsync(user.Email!, invitationLink);
if (!invitationSent)
{
    _logger.LogWarning(
        "Account invitation email could not be sent to {Email}; the account was created successfully but the invited user did not receive an invitation link and must be re-invited via POST /api/auth/resend-confirmation.",
        user.Email);
}
```

Nouvelle méthode privée `BuildInvitationLink`, sur le modèle exact de
`AuthenticationService.BuildActivationLink` (même piège hash-mode, voir Constat point 11) :
```csharp
private string BuildInvitationLink(string email, string token)
{
    return $"{_frontendSettings.RecruiterAppBaseUrl.TrimEnd('/')}/#/auth/accept-invitation" +
           $"?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";
}
```
(Chemin `/auth/accept-invitation` choisi par cohérence avec les routes déjà existantes
`recruiter-app` sous `/auth/*`, ex. `/auth/login`, `/auth/forgot-password` — voir Coordination
frontend pour la route réelle à créer.)

### 5. Nouveau endpoint `POST /api/auth/accept-invitation`

#### DTO et validator

```csharp
// DTOs/Auth/AcceptInvitationDto.cs
public record AcceptInvitationDto
{
    [Required(ErrorMessage = "L'email est obligatoire")]
    [EmailAddress(ErrorMessage = "Format d'email invalide")]
    public required string Email { get; init; }

    [Required(ErrorMessage = "Le jeton est obligatoire")]
    public required string Token { get; init; }

    [Required(ErrorMessage = "Le mot de passe est obligatoire")]
    public required string NewPassword { get; init; }

    [Required(ErrorMessage = "La confirmation du mot de passe est obligatoire")]
    [Compare(nameof(NewPassword), ErrorMessage = "Les mots de passe ne correspondent pas")]
    public required string ConfirmPassword { get; init; }
}
```

```csharp
// Validators/Auth/AcceptInvitationDtoValidator.cs — mirroir de ResetPasswordDtoValidator
public class AcceptInvitationDtoValidator : AbstractValidator<AcceptInvitationDto>
{
    public AcceptInvitationDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("L'email est obligatoire")
            .EmailAddress().WithMessage("Format d'email invalide");

        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Le jeton d'invitation est obligatoire");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Le nouveau mot de passe est obligatoire");
        // Pas de règle de longueur/complexité dupliquée ici : comme pour ResetPasswordDtoValidator,
        // la validation réelle de la politique de mot de passe (majuscule/minuscule/chiffre/
        // caractère spécial/longueur) est déléguée à UserManager.ResetPasswordAsync via les
        // IPasswordValidator<User> déjà configurés (ConfigurePasswordOptions,
        // SecurityExtensions.cs) - éviter une double source de vérité sur ces règles.

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.NewPassword).WithMessage("Les mots de passe ne correspondent pas");
    }
}
```

#### `IAuthenticationService`/`AuthenticationService`

```csharp
Task<AuthResult> AcceptInvitationAsync(AcceptInvitationDto dto);
```

```csharp
public async Task<AuthResult> AcceptInvitationAsync(AcceptInvitationDto dto)
{
    try
    {
        var validationResult = await _acceptInvitationValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return AuthResult.ValidationError(errors);
        }

        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null)
        {
            // Message explicite ici (contrairement à ResendConfirmationEmailAsync) : ce endpoint
            // n'est pas un point d'énumération public autodéclaratif (l'appelant possède déjà un
            // lien contenant un jeton, l'existence du compte ne lui apprend rien qu'il ne
            // sache déjà) - même principe que ConfirmEmailAsync/ResetPasswordAsync existants,
            // qui renvoient déjà un message explicite en cas d'utilisateur introuvable.
            return AuthResult.Failure("Requête invalide");
        }

        var result = await _userManager.ResetPasswordAsync(user, dto.Token, dto.NewPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return AuthResult.Failure($"Échec de l'acceptation de l'invitation : {errors}");
        }

        // Le jeton étant valide et le mot de passe défini, le compte est désormais activement
        // utilisable : le confirmer explicitement (l'invitation initiale ne l'avait jamais
        // confirmé, voir §4 - contrairement à RegisterCandidateAsync qui envoie un jeton de
        // confirmation d'email dédié, ce flux réutilise le jeton de reset de mot de passe comme
        // preuve unique de possession de la boîte mail, voir §Constat / justification ci-dessous).
        if (!user.EmailConfirmed)
        {
            user.EmailConfirmed = true;
        }
        user.LastPasswordChangeAt = DateTime.UtcNow;
        // Symétrie avec AdminResetPasswordAsync (§9), qui appelle déjà ClearRefreshToken() après
        // avoir redéfini un mot de passe : sans effet pour une première invitation (aucun jeton
        // n'existe encore), mais nécessaire pour un compte réinvité via resend-confirmation
        // (§7) qui aurait pu être utilisé avant - ne pas laisser un refresh token émis avant
        // l'acceptation rester valide après un changement de mot de passe.
        user.ClearRefreshToken();
        await _userManager.UpdateAsync(user);

        _logger.LogInformation("Invitation accepted and password set for user: {Email}", dto.Email);
        return AuthResult.Success("Invitation acceptée avec succès. Vous pouvez maintenant vous connecter.");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error occurred while accepting invitation for {Email}", dto.Email);
        return AuthResult.Failure("Une erreur est survenue lors de l'acceptation de l'invitation");
    }
}
```

**Justification du choix « un seul jeton de reset de mot de passe », retenue plutôt que deux jetons
séparés (confirmation d'email + reset de mot de passe) :**
- Le jeton de reset de mot de passe (`GeneratePasswordResetTokenAsync`) prouve déjà, à lui seul, la
  possession de la boîte mail (il n'a été transmis qu'à cette adresse) — il n'apporte aucune
  garantie de sécurité supplémentaire à faire cliquer sur *deux* liens/jetons distincts pour
  atteindre le même niveau de confiance.
- `UserManager.ResetPasswordAsync(user, token, newPassword)` fonctionne indépendamment de la valeur
  actuelle de `PasswordHash` (y compris `null`, voir Constat point 4) — aucune contrainte
  technique ne l'empêche d'être utilisé sur un compte n'ayant jamais eu de mot de passe. Point à
  vérifier empiriquement par le développeur avant de considérer ce point acquis (voir Critères
  d'acceptation).
- Deux jetons séparés obligeraient soit à consommer les deux dans un seul appel (payload plus
  complexe, deux validations Identity au lieu d'une), soit à accepter un état intermédiaire
  « email confirmé mais mot de passe non défini » ou l'inverse — complexité non justifiée par un
  gain de sécurité réel ici.

#### `Controllers/AuthController.cs`

```csharp
[HttpPost("accept-invitation")]
[AllowAnonymous]
public async Task<ActionResult<AuthResponseDto>> AcceptInvitation([FromBody] AcceptInvitationDto dto)
{
    var result = await _authService.AcceptInvitationAsync(dto);
    return this.ToActionResult(result);
}
```

**Pas de endpoint de validation de jeton séparé** (ex. `GET /api/auth/validate-invitation-token`) :
décision assumée, pas un oubli. Le jeton `DataProtectorTokenProvider` n'est pas « consommé » par une
vérification ratée (contrairement à un jeton à usage unique stocké en base) — une pré-validation
n'apporterait qu'un gain d'UX marginal (afficher une erreur avant que l'utilisateur ne saisisse son
mot de passe plutôt qu'après) au prix d'un endpoint anonyme supplémentaire à maintenir. La page
`recruiter-app` affiche l'erreur retournée par `POST /api/auth/accept-invitation` lui-même si le
jeton est invalide/expiré (voir Coordination frontend).

### 6. `Interfaces/IEmailNotificationService.cs` / `Services/EmailNotificationService.cs` — nouvelle méthode

```csharp
/// <summary>
/// Envoie l'email d'invitation à un compte interne nouvellement créé (template
/// "AccountInvitation", fr-FR). Même contrat de non-exception que
/// SendAccountActivationEmailAsync.
/// </summary>
Task<bool> SendAccountInvitationEmailAsync(string email, string invitationLink);
```

```csharp
public async Task<bool> SendAccountInvitationEmailAsync(string email, string invitationLink)
{
    var request = new SendTemplatedEmailRequestDto
    {
        TemplateName = "AccountInvitation",
        To = email,
        TemplateData = new Dictionary<string, string> { ["InvitationLink"] = invitationLink },
        Language = "fr-FR"
    };

    try
    {
        using var response = await _httpClient.PostAsJsonAsync("api/emails/send", request);
        if (response.IsSuccessStatusCode)
        {
            return true;
        }

        var body = await response.Content.ReadAsStringAsync();
        _logger.LogError(
            "CommunicationService returned {StatusCode} while sending AccountInvitation email to {Email}: {Body}",
            (int)response.StatusCode, email, body);
        return false;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to call CommunicationService to send AccountInvitation email to {Email}", email);
        return false;
    }
}
```

**Une seule variable de template, `InvitationLink`** — décision volontaire, pas une simplification
paresseuse : `email-sending-foundation.md` §4 documente explicitement que les valeurs de
`templateData` sont substituées dans le corps HTML **sans encodage HTML**, jugé acceptable
uniquement parce que la seule variable existante (`ActivationLink`) est une URL générée
serveur, jamais une saisie libre. Ajouter `FirstName`/`OrganizationName` (saisis librement par
l'admin dans `CreateUserDto`) à ce template violerait cette hypothèse de sécurité sans l'encodage
HTML correspondant (non ajouté à `TemplateService.cs` par cette spec) — reporté à une future
extension si un template avec du texte libre est réellement nécessaire (voir Points à confirmer).

### 7. `Services/AuthenticationService.ResendConfirmationEmailAsync` — branche interne vs candidat

Remplacer le corps existant par une branche sur `user.OrganizationId.HasValue` (Constat point 6) :

```csharp
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
            return AuthResult.Success(ResendConfirmationGenericMessage);
        }

        if (!ResendConfirmationCooldown.TryStart(user.Email!, TimeSpan.FromSeconds(60)))
        {
            return AuthResult.Success(ResendConfirmationGenericMessage);
        }

        bool sent;
        if (user.OrganizationId.HasValue)
        {
            // Compte interne : renvoyer une invitation (reset de mot de passe), pas une
            // confirmation d'email - voir Constat point 6 pour la justification détaillée.
            var invitationToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var invitationLink = BuildInvitationLink(user.Email!, invitationToken);
            sent = await _emailNotificationService.SendAccountInvitationEmailAsync(user.Email!, invitationLink);
        }
        else
        {
            // Candidat : comportement strictement inchangé.
            var confirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var activationLink = BuildActivationLink(user.Email!, confirmationToken);
            sent = await _emailNotificationService.SendAccountActivationEmailAsync(user.Email!, activationLink);
        }

        if (!sent)
        {
            _logger.LogError("Failed to resend account {Kind} email to {Email}",
                user.OrganizationId.HasValue ? "invitation" : "activation", user.Email);
        }

        return AuthResult.Success(ResendConfirmationGenericMessage);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error occurred while resending confirmation email for {Email}", dto.Email);
        return AuthResult.Success(ResendConfirmationGenericMessage);
    }
}
```

`BuildInvitationLink` : extraite dans `AuthenticationService` sur le même modèle que
`BuildActivationLink` (les deux méthodes coexistent, l'une pointant vers `candidate-app`, l'autre
vers `recruiter-app`) — dupliquée par rapport à `UserService.BuildInvitationLink` (§4) : les deux
classes n'ont pas de base commune aujourd'hui, une factorisation dans une classe utilitaire statique
partagée est possible mais non imposée par cette spec (deux fonctions à une ligne, risque de
divergence jugé faible).

**Le nom `resend-confirmation` reste inchangé** (pas de renommage en `resend-activation` ou
équivalent) : c'est un changement d'API cassant sans bénéfice proportionné (le endpoint existe déjà
en production potentielle pour les candidats) — le comportement interne est un branchement
supplémentaire, pas un renommage.

### 8. Sort de `RegisterAsync` (`POST /api/auth/register`) — unifié dans le flux d'invitation

Décision retenue (Constat point 5 : endpoint mort côté frontend, aucun appelant à préserver) :
**`RegisterAsync` est réécrit pour créer le compte sans mot de passe et envoyer une invitation**,
plutôt que d'être supprimé (le endpoint reste potentiellement utile comme point d'entrée
programmatique/API pour un futur script d'admin, et le retirer casserait sa policy d'autorisation
`CanCreateUsers` déjà en place sans bénéfice).

- `RegisterDto.Password`/`RegisterDto.ConfirmPassword` : retirés (même raisonnement que
  `CreateUserDto.Password`, §3). **Corollaire obligatoire** (Constat point 16) :
  `Validators/Auth/RegisterUserDtoValidator.cs` (le validator de `RegisterDto`, malgré son nom de
  fichier) doit perdre ses deux `RuleFor(x => x.Password)`/`RuleFor(x => x.ConfirmPassword)`
  correspondants, sous peine d'erreur de compilation.
- `RegisterAsync` : remplacer `_userManager.CreateAsync(user, registerDto.Password)` par
  `_userManager.CreateAsync(user)`, retirer l'assignation morte
  `user.EmailConfirmationToken = emailConfirmationToken` (déjà un vestige avant cette spec), et
  après création réussie, générer + envoyer l'invitation exactement comme `UserService.CreateAsync`
  (§4) — réutiliser `IEmailNotificationService.SendAccountInvitationEmailAsync` et une méthode
  `BuildInvitationLink` équivalente (déjà injectées dans `AuthenticationService` pour §7 ci-dessus,
  aucune nouvelle dépendance constructeur nécessaire pour ce point précis).
- Message de succès retourné mis à jour pour refléter la réalité (« Invitation envoyée. L'utilisateur
  doit consulter son email pour définir son mot de passe. ») plutôt que le message d'inscription
  candidat générique actuellement recopié à l'identique.

**Alternative écartée** : supprimer purement `RegisterAsync`/`RegisterDto`/la route
`POST /api/auth/register`. Écartée pour ne pas retirer un endpoint déjà exposé et documenté par
Swagger sans certitude qu'aucun script/outil externe ne s'appuie dessus (contrairement au frontend,
vérifié comme non-consommateur) — l'unifier dans le nouveau flux est strictement moins risqué et
comble la même dette technique.

### 9. `AuthenticationService.AdminResetPasswordAsync` — confirmer le compte à la réinitialisation

Correctif du cas limite documenté au Constat point 19. Après le succès de `AddPasswordAsync`
(juste avant `targetUser.LastPasswordChangeAt = DateTime.UtcNow;`, code déjà existant) :

```csharp
// Un admin qui définit lui-même un mot de passe hors-bande (ce endpoint) est une garantie au
// moins aussi forte qu'un aller-retour email : confirmer le compte du même coup évite qu'un admin
// croie avoir débloqué un utilisateur qui resterait pourtant bloqué à la connexion
// (RequireConfirmedEmail = true) - voir internal-user-account-invitation.md, Constat point 19.
if (!targetUser.EmailConfirmed)
{
    targetUser.EmailConfirmed = true;
}
```

Aucun changement de signature, de policy d'autorisation (`CanResetPasswords`, inchangée) ni de
DTO — uniquement l'ajout de cette ligne dans le corps de la méthode existante.

## Coordination CommunicationService (`XpertSphere.CommunicationService`)

Contrairement à `candidate-account-activation-email.md` (« aucun changement nécessaire »), cette
spec **nécessite un changement réel** côté `CommunicationService` : un nouveau template.

### `Models/MessageTemplate.cs` — nouvelle valeur d'enum

```csharp
public enum TemplateType
{
    // ... valeurs existantes inchangées ...
    AccountActivation,
    AccountInvitation, // nouveau
    Custom
}
```

### `Services/TemplateService.cs` — nouvelle entrée dans le dictionnaire statique

```csharp
[("AccountInvitation", "fr-FR")] = new MessageTemplate
{
    TemplateId = "AccountInvitation-fr-FR",
    Name = "AccountInvitation",
    Language = "fr-FR",
    Type = TemplateType.AccountInvitation,
    Subject = "Vous êtes invité(e) à rejoindre XpertSphere",
    Body = "<p>Bonjour,</p><p>Un compte XpertSphere vient d'être créé pour vous. Pour l'activer, définissez votre mot de passe en cliquant sur le lien ci-dessous :</p><p><a href=\"{{InvitationLink}}\">Définir mon mot de passe</a></p><p>Si vous n'êtes pas à l'origine de cette demande, ignorez cet email.</p>",
    Variables = new Dictionary<string, string>
    {
        ["InvitationLink"] = "Lien absolu vers la page de définition du mot de passe (recruiter-app)"
    },
    IsActive = true,
    Category = "Account"
}
```

Aucun autre changement : `POST /api/emails/send`, `ApiKeyMiddleware`, `EmailService`/`SmtpOptions`
inchangés — le contrat générique (nom de template + destinataire + données) couvre déjà ce nouveau
besoin, seul le contenu du template est nouveau.

## Coordination frontend — recruiter-app

### Nouvelle page d'acceptation d'invitation, avec durcissement anti-scanner

- `src/pages/auth/AcceptInvitationPage.vue` (nouveau, à côté de `LoginPage.vue`/
  `ForgotPasswordPage.vue` déjà sous `src/pages/auth/`) :
  - Lit `route.query.email`/`route.query.token` **au montage, sans effectuer aucun appel réseau à
    ce moment** — c'est le point central du durcissement anti-scanner demandé : un scanner de
    liens (type Antigena) qui pré-ouvre l'URL ne doit consommer aucun jeton. La page affiche
    directement un formulaire (nouveau mot de passe + confirmation), pré-rempli avec l'email en
    lecture seule.
  - `handleSubmit` (déclenché uniquement par le clic explicite sur le bouton "Définir mon mot de
    passe") appelle `authService.acceptInvitation({ email, token, newPassword, confirmPassword })`
    → `POST /api/auth/accept-invitation`.
  - Succès : message de confirmation + lien/bouton vers `/auth/login`.
  - Échec (jeton invalide/expiré, mot de passe non conforme à la politique) : message d'erreur
    explicite (`extractApiErrorMessages`, pattern déjà utilisé ailleurs dans ce package), pas de
    redirection automatique.
  - Réutiliser `usePasswordComplexityRules()` (`src/composables/passwordRules.ts`, déjà partagé
    par `UsersPage.vue`/`ProfilePage.vue`) pour la validation client du nouveau mot de passe — pas
    de quatrième copie de ces règles.
- `src/composables/passwordRules.ts` : mettre à jour le docblock, qui énumère aujourd'hui
  explicitement « 3 emplacements actifs (`UsersPage.vue` création et dialog "Reset Password",
  `ProfilePage.vue`) » (Constat point 18) — remplacer la mention « `UsersPage.vue` création » par
  `AcceptInvitationPage.vue`, le nombre d'emplacements actifs restant 3 mais l'ensemble ayant
  changé.
- `src/router/routes.ts` : nouvelle route, ex. `{ path: '/auth/accept-invitation', component:
  () => import('pages/auth/AcceptInvitationPage.vue') }`, cohérente avec le préfixe `/auth/*` déjà
  utilisé par `login`/`forgot-password`.
- **`src/router/guards/authGuard.ts` — changement obligatoire, sous peine de lien mort** : ajouter
  `/auth/accept-invitation` au tableau `publicPages` (Constat point 10) :
  ```ts
  const publicPages = ['/auth/login', '/auth/forgot-password', '/auth/accept-invitation'];
  ```
- `src/services/authService.ts` : nouvelle méthode
  ```ts
  async acceptInvitation(dto: AcceptInvitationDto): Promise<AuthResult | null> {
    return this.post<AuthResult>('/accept-invitation', dto, "Erreur lors de l'acceptation de l'invitation");
  }
  ```
- `src/models/auth.ts` : nouveau type
  ```ts
  export interface AcceptInvitationDto {
    email: string;
    token: string;
    newPassword: string;
    confirmPassword: string;
  }
  ```

### Formulaire de création d'utilisateur (`src/pages/admin/UsersPage.vue`) — retrait du mot de passe

- Retirer le champ mot de passe du formulaire de création (`userForm.password`, lignes ~274,
  ~524, ~697, ~716, ~759 — voir Constat point 9) et de l'interface TypeScript de payload
  correspondante (`src/models/user.ts:123`, `password: string;` → retiré ou rendu optionnel si le
  même type est réutilisé ailleurs pour un autre usage, à vérifier par le développeur).
  Remplacer, à l'endroit où s'affichait le champ, par un texte explicatif (ex. « Un email
  d'invitation sera envoyé à cette adresse pour permettre à l'utilisateur de définir son mot de
  passe. »).
- Le dialog « Reset Password » (`AdminResetPasswordDto`) **n'est pas concerné** — flux distinct
  pour un compte déjà activé, inchangé.

## Critères d'acceptation vérifiables

1. `dotnet build` réussit après l'ensemble des changements (`MonolithApi` et
   `CommunicationService`).
2. **Création via `POST /api/users`** (payload valide, sans champ `password`) : `201`/`200`,
   l'utilisateur est créé en base avec `PasswordHash = null` et `EmailConfirmed = false`, un email
   apparaît dans smtp4dev avec le sujet `"Vous êtes invité(e) à rejoindre XpertSphere"` et un lien
   `http://localhost:3001/#/auth/accept-invitation?email=...&token=...` valide et URL-encodé.
3. **Contournement impossible** : envoyer explicitement `emailConfirmed: true` (ou tout champ
   équivalent) dans le payload de `POST /api/users` n'a aucun effet — l'utilisateur créé a
   `EmailConfirmed = false` en base, quel que soit le contenu envoyé par l'appelant.
4. **Tentative de connexion avant acceptation** : `POST /api/auth/login` avec l'email créé au
   critère 2 (avant toute acceptation d'invitation) : `400`, message explicite (branche
   `IsNotAllowed`, comportement déjà existant réutilisé tel quel).
5. **Acceptation de l'invitation** : copier le lien reçu dans smtp4dev, appeler
   `POST /api/auth/accept-invitation` avec `email`/`token`/un nouveau mot de passe conforme à la
   politique (`ConfigurePasswordOptions`) : `200`. En base, `EmailConfirmed = true` et
   `PasswordHash` non nul.
6. **Connexion après acceptation** : reproduire le critère 4 avec les identifiants définis au
   critère 5 : `200`, connexion réussie avec jeton.
7. **Fenêtre de validité du jeton d'invitation** : reproduire les critères 2 et 5 en attendant au
   moins 10 minutes entre la création du compte et l'acceptation de l'invitation : toujours `200`
   (démontre le correctif `PasswordResetTokenProvider`, §Comportement cible §1).
8. **Jeton invalide/expiré** : appeler `POST /api/auth/accept-invitation` avec un jeton altéré (un
   ou plusieurs caractères modifiés) : `400`, message d'échec explicite, aucun mot de passe n'est
   modifié.
9. **Renvoi — compte interne non confirmé** : `POST /api/auth/resend-confirmation` avec l'email
   d'un compte interne créé mais non confirmé : `200`, message générique identique à celui du
   flux candidat, un **nouvel** email `AccountInvitation` apparaît dans smtp4dev (lien
   `recruiter-app`, pas `candidate-app`) ; l'accepter fonctionne (`POST /api/auth/accept-invitation`
   → `200`).
10. **Renvoi — non-régression candidat** : reproduire le critère 9 avec l'email d'un candidat non
    confirmé (`RegisterCandidateAsync`) : comportement strictement inchangé (email
    `AccountActivation`, lien `candidate-app`, `POST /api/auth/confirm-email`).
11. **`POST /api/auth/register` (endpoint admin)** : créer un utilisateur via ce endpoint (sans
    `password` dans le payload) : `200`, même comportement observable que le critère 2 (compte sans
    mot de passe, email d'invitation envoyé, acceptation fonctionnelle).
12. **Comptes seedés non affectés** : après un seed complet (`dotnet ef database update` +
    démarrage de l'API en Development), tous les comptes créés par
    `SeedPlatformSuperAdminAsync`/`SeedDemoDataAsync` ont toujours `EmailConfirmed = true` et se
    connectent avec le mot de passe seedé, sans email envoyé (non-régression).
13. **Échec d'envoi n'empêche pas la création** (même politique que
    `candidate-account-activation-email.md` §11) : rendre `CommunicationService` injoignable, puis
    créer un utilisateur via `POST /api/users` : toujours succès HTTP, compte créé en base sans mot
    de passe, erreur loguée côté serveur ; `POST /api/auth/resend-confirmation` avec son email,
    une fois `CommunicationService` de nouveau joignable, permet de récupérer l'invitation.
14. `recruiter-app` : le formulaire de création d'utilisateur n'affiche plus de champ mot de passe ;
    ouvrir le lien d'invitation reçu affiche un formulaire de définition de mot de passe sans
    qu'aucun appel réseau ne soit visible avant le clic explicite sur le bouton de soumission
    (vérifiable par l'onglet réseau du navigateur) ; soumettre le formulaire avec un mot de passe
    valide redirige vers un état de succès permettant d'accéder à `/auth/login`.
15. `recruiter-app` : ouvrir directement l'URL `/#/auth/accept-invitation?...` sans être connecté
    n'entraîne **pas** de redirection vers `/auth/login` par le garde de routes (vérifie l'ajout au
    tableau `publicPages`, Constat point 10).
16. `CommunicationService` : `POST /api/emails/send` avec `templateName: "AccountInvitation"` et
    `templateData: { "InvitationLink": "https://example.com/..." }` retourne `200`, email visible
    dans smtp4dev avec le sujet et le lien correctement substitués.
17. Aucune régression sur les critères d'acceptation déjà couverts par
    `candidate-account-activation-email.md` (le flux candidat au complet reste fonctionnel à
    l'identique).
18. Tests unitaires existants mis à jour et passants (`AuthenticationServiceTests.cs`,
    `AuthenticationServiceProfileCompletenessTests.cs`, `SimpleAuthenticationTests.cs`,
    `UserServiceProfileCurrencyTests.cs`, `UserServiceCvDownloadTests.cs` — nouvelles dépendances
    constructeur et/ou suppression de `RegisterDto.Password`/`ConfirmPassword`, voir Constat
    points 15-16).
19. **Reset de mot de passe admin sur un compte non confirmé** : créer un compte via
    `POST /api/users` (invitation non acceptée), puis appeler
    `POST /api/auth/admin-reset-password` avec l'email de ce compte et un nouveau mot de passe
    valide : `200`, et l'utilisateur ainsi mis à jour peut immédiatement se connecter avec ce mot
    de passe (`EmailConfirmed = true` en base après l'appel, voir §Comportement cible §9) — sans
    ce correctif, cette même séquence laisserait le compte bloqué malgré un mot de passe
    fonctionnel.

## Hors périmètre

- Entra ID / authentification B2B réelle : cette spec ne modifie rien au chemin
  `ShouldUseEntraId == true` — non exercé aujourd'hui (Development toujours en JWT local, voir
  Constat point 1). Si Entra ID est activé un jour en Staging/Production, ce flux d'invitation par
  mot de passe local ne s'appliquerait qu'aux comptes qui resteraient en JWT local (fallback), pas
  aux comptes provisionnés via Entra ID (déjà confirmés par `ClaimsEnrichmentMiddleware`).
- `ForgotPasswordAsync`/`ResetPasswordAsync` : comportement fonctionnel inchangé (toujours aucun
  email envoyé automatiquement) — seul le provider de jeton sous-jacent change (§Comportement
  cible §1), sans changement observable côté ces deux méthodes elles-mêmes.
- Endpoint de validation de jeton séparé avant soumission du formulaire (`GET
  /api/auth/validate-invitation-token` ou équivalent) : décision assumée de ne pas l'ajouter (voir
  §Comportement cible §5).
- Attribution automatique d'un rôle à la création/l'acceptation de l'invitation : comportement
  inchangé, l'attribution de rôle reste un endpoint séparé (Constat point 8).
- Encodage HTML des variables de template pour un futur template avec texte libre
  (`FirstName`/`OrganizationName`) : non ajouté, cohérent avec la limite déjà documentée par
  `email-sending-foundation.md` §4 (voir §Comportement cible §6).
- Renommage de `POST /api/auth/resend-confirmation` : conservé tel quel (voir §Comportement cible
  §7).
- Suppression de `RegisterAsync`/`RegisterDto`/la route `POST /api/auth/register` : conservés,
  unifiés dans le nouveau flux plutôt que retirés (voir §Comportement cible §8).
- Multi-langue du template `AccountInvitation` : seul `fr-FR`, cohérent avec le seul autre template
  existant.
- Un mécanisme dédié de « renvoi d'invitation » distinct de `resend-confirmation` (ex. un bouton
  admin sur `UsersPage.vue`, à côté de la ligne de l'utilisateur, appelant directement le renvoi
  sans que l'utilisateur invité n'ait à le demander lui-même) : non spécifié ici, voir Points à
  confirmer point (c).
- Tests automatisés au-delà du critère 18 : l'écriture exhaustive de nouveaux tests
  (`AcceptInvitationAsync`, branchement de `ResendConfirmationEmailAsync`) relève de l'agent
  `developer`.

## Fichiers à créer/modifier (récapitulatif)

Backend (`XpertSphere.MonolithApi`) :
- `Extensions/SecurityExtensions.cs` : `ConfigureTokenOptions` —
  `PasswordResetTokenProvider = DefaultProvider`.
- `Config/FrontendSettings.cs` : nouveau champ `RecruiterAppBaseUrl`.
- `appsettings.json`, `appsettings.Development.json`, `appsettings.Staging.json` (à vérifier avant
  modification) : clé `Frontend:RecruiterAppBaseUrl`.
- `DTOs/User/CreateUserDto.cs` : retrait de `Password` (`[Required]`) et d'`EmailConfirmed` (ou
  ignoré côté mapping, voir alternative documentée §3).
- `Validators/User/CreateUserDtoValidator.cs` : retrait de toute règle relative à `Password` si
  applicable (aucune trouvée à l'exploration au-delà de l'attribut `[Required]` du DTO lui-même).
- `Mappings/UserMappingProfile.cs` : ajustement si l'alternative « ignore » est retenue pour
  `EmailConfirmed`.
- `Services/UserService.cs` : `CreateAsync` — `_userManager.CreateAsync(user)` sans mot de passe,
  génération + envoi de l'invitation, nouvelle méthode privée `BuildInvitationLink`, nouvelles
  dépendances constructeur `IEmailNotificationService`, `IOptions<FrontendSettings>`.
- `Interfaces/IEmailNotificationService.cs` / `Services/EmailNotificationService.cs` : nouvelle
  méthode `SendAccountInvitationEmailAsync`.
- `DTOs/Auth/AcceptInvitationDto.cs` (nouveau).
- `Validators/Auth/AcceptInvitationDtoValidator.cs` (nouveau).
- `Interfaces/IAuthenticationService.cs` : nouvelle méthode `AcceptInvitationAsync`.
- `Services/AuthenticationService.cs` :
  - Nouvelle méthode `AcceptInvitationAsync` + méthode privée `BuildInvitationLink`.
  - `ResendConfirmationEmailAsync` : branchement `OrganizationId.HasValue` (voir §7).
  - `RegisterAsync` : réécriture (création sans mot de passe + envoi d'invitation, voir §8).
  - `AdminResetPasswordAsync` : confirme désormais le compte (`EmailConfirmed = true`) en cas de
    succès (voir §9).
  - Nouvelle dépendance constructeur : `IValidator<AcceptInvitationDto>`.
- `DTOs/Auth/RegisterDto.cs` : retrait de `Password`/`ConfirmPassword`.
- `Validators/Auth/RegisterUserDtoValidator.cs` : retrait des `RuleFor` correspondant à
  `Password`/`ConfirmPassword` (Constat point 16, corollaire obligatoire du point précédent).
- `Controllers/AuthController.cs` : nouvelle action `POST /api/auth/accept-invitation`
  (`[AllowAnonymous]`).
- `XpertSphere.MonolithApi.Tests/Services/AuthenticationServiceTests.cs`,
  `AuthenticationServiceProfileCompletenessTests.cs`, `SimpleAuthenticationTests.cs`,
  `UserServiceProfileCurrencyTests.cs`, `UserServiceCvDownloadTests.cs` : mise à jour des
  constructeurs (nouvelles dépendances) et/ou du test `RegisterDto_ShouldHaveRequiredProperties`
  (retrait de `Password`/`ConfirmPassword`, voir Constat point 15).

Backend (`XpertSphere.CommunicationService`) :
- `Models/MessageTemplate.cs` : nouvelle valeur d'enum `TemplateType.AccountInvitation`.
- `Services/TemplateService.cs` : nouvelle entrée `("AccountInvitation", "fr-FR")` dans le
  dictionnaire statique.

Frontend `recruiter-app` :
- `src/pages/auth/AcceptInvitationPage.vue` (nouveau).
- `src/router/routes.ts` : nouvelle route `/auth/accept-invitation`.
- `src/router/guards/authGuard.ts` : ajout de `/auth/accept-invitation` à `publicPages`.
- `src/services/authService.ts` : nouvelle méthode `acceptInvitation`.
- `src/models/auth.ts` : nouveau type `AcceptInvitationDto`.
- `src/pages/admin/UsersPage.vue` : retrait du champ mot de passe du formulaire de création.
- `src/models/user.ts` : ajustement du type de payload de création (retrait/optionnalité de
  `password`).
- `src/composables/passwordRules.ts` : mise à jour du docblock (liste des 3 emplacements actifs,
  voir Constat point 18).

Aucun changement dans `candidate-app`, `ReportingService`, `IntegrationService`, `ResumeAnalyzer`.

## Points à confirmer avec l'utilisateur

a. **`POST /api/users` avec `OrganizationId == null`** (compte plateforme
   `XpertSphere.Admin`/`XpertSphere.SuperAdmin`, ou saisie erronée par l'appelant) : le
   discriminant retenu (`OrganizationId.HasValue`) traite tout compte sans organisation comme un
   « candidat » dans `ResendConfirmationEmailAsync` (§Comportement cible §7), ce qui enverrait à
   tort un email `AccountActivation`/lien `candidate-app` à un admin plateforme légitime créé sans
   organisation. Faut-il un discriminant plus précis (ex. vérifier l'absence de rôle `Candidate` /
   la présence d'un rôle dans `Roles.InternalRoles` une fois attribué, sachant qu'aucun rôle n'est
   encore assigné à la création, voir Constat point 8) ou accepter cette limite pour l'instant (peu
   probable en pratique : `POST /api/users` n'est de toute façon jamais utilisé pour créer un
   candidat) ?
b. **Durée de vie du jeton d'invitation fixée à 1 jour** (`DataProtectionTokenProviderOptions.
   TokenLifespan`, partagée avec la confirmation d'email candidat et le reset de mot de passe) :
   acceptable, ou une durée plus longue (ex. 7 jours, plus conventionnelle pour une invitation
   envoyée par un tiers plutôt qu'auto-déclenchée) est-elle souhaitée ? Une durée différente
   nécessiterait un provider de jeton nommé dédié (`RegisterTokenProvider` custom), non
   implémenté par cette spec faute de décision.
c. **Renvoi d'invitation déclenché côté admin** (ex. bouton sur la fiche utilisateur de
   `UsersPage.vue`, appelant `resend-confirmation` au nom de l'invité, en plus du self-service déjà
   couvert) : souhaité dans le périmètre de cette spec, ou reporté à une itération ultérieure si le
   besoin se confirme à l'usage ?
