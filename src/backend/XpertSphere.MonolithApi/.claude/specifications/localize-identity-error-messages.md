# Localisation des messages natifs ASP.NET Core Identity

## Contexte

`french-message-consistency.md` (déjà implémenté) a traduit en français les messages utilisateur du code applicatif (`Utils/Constants.cs`, `Validators/**`, `Services/*.cs`, `Controllers/*.cs`). Il ne couvre pas les messages **générés nativement par ASP.NET Core Identity** (`IdentityErrorDescriber`), qui restent systématiquement en anglais par défaut quel que soit le reste de l'UI, puisque Identity ne fournit pas de ressources satellites françaises pour ces textes.

Ce ticket corrige ce résidu, sans rouvrir le périmètre déjà traité par `french-message-consistency.md`.

## Diagnostic (vérifié dans le code)

### 1. Aucune personnalisation de `IdentityErrorDescriber` aujourd'hui

`Extensions/SecurityExtensions.cs` configure Identity ainsi :

```csharp
services.AddIdentity<User, IdentityRole<Guid>>(options => { ... })
    .AddEntityFrameworkStores<XpertSphereDbContext>()
    .AddDefaultTokenProviders();
```

Aucun `IdentityErrorDescriber` custom n'est enregistré. Toutes les méthodes de la classe (`PasswordTooShort`, `PasswordRequiresNonAlphanumeric`, `InvalidToken`, `DuplicateEmail`, etc.) renvoient donc leur texte anglais par défaut du framework.

### 2. Règles de mot de passe configurées (`ConfigurePasswordOptions`, `SecurityExtensions.cs:44-52`)

```csharp
options.Password.RequireDigit = true;
options.Password.RequireLowercase = true;
options.Password.RequireNonAlphanumeric = true;
options.Password.RequireUppercase = true;
options.Password.RequiredLength = environment.IsDevelopment() ? 6 : 8;
options.Password.RequiredUniqueChars = 1;
```

Et `ConfigureUserOptions` (`SecurityExtensions.cs:61-66`) : `RequireUniqueEmail = true`, `AllowedUserNameCharacters` restreint (le nom d'utilisateur est systématiquement l'email — voir `UserName = registerDto.Email` / `UserName = dto.Email` dans `AuthenticationService.cs`/`UserService.cs`).

### 3. Canal de transport confirmé : concaténation brute de `e.Description`

Le pattern `string.Join(", ", result.Errors.Select(e => e.Description))` puis interpolation dans une phrase française (`$"Échec de ... : {errors}"`) est confirmé à ces endroits, tous atteignables depuis un flux utilisateur réel (pas seulement du code mort) :

| Fichier | Ligne(s) | Méthode | Flux déclencheur |
|---|---|---|---|
| `Services/AuthenticationService.cs` | 157-162 | `RegisterAsync` | `POST /api/Auth/register` (policy `CanCreateUsers`, ex. admin plateforme) |
| `Services/AuthenticationService.cs` | 276-281 | `RegisterCandidateAsync` | `POST /api/Auth/register/candidate` — **inscription candidat**, `candidate-app` |
| `Services/AuthenticationService.cs` | 592-600 | `ConfirmEmailAsync` | `candidate-app/src/pages/ConfirmEmailPage.vue` |
| `Services/AuthenticationService.cs` | 715-729 | `ResetPasswordAsync` | Réinitialisation de mot de passe (lien email) |
| `Services/AuthenticationService.cs` | 783-795 | `AdminResetPasswordAsync` (`RemovePasswordAsync` + `AddPasswordAsync`) | `recruiter-app/src/pages/admin/UsersPage.vue`, dialog « Reset Password » |
| `Services/AuthenticationService.cs` | 1089-1099 | `LinkEntraIdAccountAsync` (`UpdateAsync`) | Liaison de compte Entra ID |
| `Services/UserService.cs` | 197-204 | `CreateAsync` | `POST /api/Users` — **création d'utilisateur interne**, `recruiter-app/src/pages/admin/UsersPage.vue` |

Hors périmètre (confirmé) : `Extensions/DatabaseExtensions.cs:284-289` et `Extensions/DatabaseExtensions.DemoData.cs:163,307` utilisent le même pattern mais via `Console.WriteLine` pendant le seed au démarrage (Development uniquement) — jamais renvoyé à un client HTTP. Ne pas traduire (pas un canal utilisateur), cohérent avec le traitement des logs dans `french-message-consistency.md`.

### 4. Reachabilité confirmée du bug pour l'utilisateur final (pas seulement en théorie)

- **`candidate-app`** (`MultiStepRegisterForm.vue:582-588`, `passwordRules`) : vérifie longueur ≥ 6, une majuscule, une minuscule, un chiffre — **mais jamais l'exigence `RequireNonAlphanumeric`**. Un candidat saisissant par exemple `Abcde1` (valide côté client) reçoit donc en retour, via le flux normal de l'UI (pas seulement via un appel API direct), le message anglais brut du framework une fois arrivé côté serveur.
- **`recruiter-app`** (`UsersPage.vue:278-281`, création d'utilisateur interne) : la règle client ne vérifie que `longueur ≥ 8`, aucune règle de complexité. Le même bug est donc trivialement atteignable pour un admin qui crée un utilisateur interne avec un mot de passe purement alphabétique.
- Le dialog « Reset Password » de `recruiter-app` (`UsersPage.vue:425-450`) ne vérifie que la correspondance des deux champs, pas la complexité : même exposition pour `AdminResetPasswordAsync`.

Ce constat, initialement documenté ici à titre d'explication de la reachabilité du bug, est désormais traité comme exigence de cette spec (voir « Coordination frontend — alignement des règles de mot de passe » plus bas).

### 5. `IdentityResult.Errors` couvre plus que les 2 exemples initiaux de l'audit

Toutes les méthodes de `IdentityErrorDescriber` ne sont pas forcément déclenchables aujourd'hui avec la configuration en place (pas de `RoleManager` utilisé — recherché, aucune occurrence de `AddToRoleAsync`/`RemoveFromRoleAsync`/`RoleManager.` dans le code : les rôles applicatifs transitent par la table `UserRole` propre au projet, pas par Identity), mais le correctif standard consiste à surcharger **toutes** les méthodes de la classe, pour rester correct si la configuration évolue plus tard (ajout de `RoleManager`, `AddLoginAsync`, etc.) — coût de développement négligeable, exhaustivité garantie.

| Règle de config actuelle | Méthode(s) `IdentityErrorDescriber` déclenchable(s) | Flux réellement atteignable aujourd'hui |
|---|---|---|
| `RequireDigit = true` | `PasswordRequiresDigit()` | Tous les flux du tableau §3 impliquant `CreateAsync`/`AddPasswordAsync` |
| `RequireLowercase = true` | `PasswordRequiresLower()` | idem |
| `RequireNonAlphanumeric = true` | `PasswordRequiresNonAlphanumeric()` | idem — **c'est le bug n°1 signalé initialement** |
| `RequireUppercase = true` | `PasswordRequiresUpper()` | idem |
| `RequiredLength = 6 (dev) / 8 (prod)` | `PasswordTooShort(int length)` | idem (le message doit garder le paramètre `{0}`, la valeur diffère par environnement) |
| `RequiredUniqueChars = 1` | `PasswordRequiresUniqueChars(int uniqueChars)` | Peu probable de se déclencher avec le seuil actuel (1), mais à surcharger par cohérence |
| `RequireUniqueEmail = true` | `DuplicateEmail(string email)` | `CreateAsync` en cas de course/contournement du check applicatif préalable (`FindByEmailAsync`) |
| `UserName = Email` (toujours) | `DuplicateUserName(string userName)`, `InvalidUserName(string userName)` | idem, ou email contenant un caractère hors `AllowedUserNameCharacters` |
| Format email | `InvalidEmail(string email)` | Défense en profondeur si un email passe la validation applicative mais pas celle d'Identity |
| Tokens (`ConfirmEmailAsync`, `ResetPasswordAsync`) | `InvalidToken()` | **Bug n°2 signalé initialement** — lien expiré/invalide |
| `AddPasswordAsync` (déjà un mot de passe) | `UserAlreadyHasPassword()` | `AdminResetPasswordAsync` si `RemovePasswordAsync` échoue silencieusement avant |
| `UpdateAsync` générique | `ConcurrencyFailure()` | Mise à jour concurrente d'un même utilisateur (rare mais possible) |
| Non utilisé aujourd'hui (pas de `RoleManager`) | `InvalidRoleName`, `DuplicateRoleName`, `UserAlreadyInRole`, `UserNotInRole`, `UserLockoutNotEnabled`, `LoginAlreadyAssociated`, `RecoveryCodeRedemptionFailed`, `PasswordMismatch`, `DefaultError` | Non atteignables avec la configuration actuelle — à surcharger quand même pour l'exhaustivité |

### 6. `UseRequestLocalization` : absent, mais ce n'est **pas** la cause ni le correctif du bug Identity

Vérifié : `Program.cs` ne configure ni `UseRequestLocalization` ni `DefaultRequestCulture`. **Point de clarification important, corrigeant l'hypothèse initiale du ticket** : configurer la culture de la requête sur `fr-FR` ne traduirait **pas** les messages par défaut d'`IdentityErrorDescriber`, car .NET ne fournit pas de ressources satellites françaises pour ces textes précis (ils sont câblés en dur en anglais dans le code source du framework, pas chargés depuis un fichier `.resx` localisable). La classe custom décrite ci-dessous renvoie du texte français en dur, indépendamment de toute culture. `UseRequestLocalization`/`fr-FR` reste néanmoins une amélioration générale correcte à part entière (formats de date/nombre, futurs messages culture-sensibles) — elle est incluse dans le périmètre de ce ticket pour cette seule raison, **pas** comme mécanisme de traduction d'Identity.

**Note (portée réduite depuis la décision d'inclure les `DataAnnotations`, voir section dédiée plus bas)** : contrairement à `IdentityErrorDescriber`, `System.ComponentModel.DataAnnotations` fournit des ressources satellites localisées pour certains messages par défaut (`[Required]`, `[EmailAddress]`, etc. sans `ErrorMessage` explicite). Ce point est désormais sans conséquence pratique pour les **DTOs d'authentification** traités par ce ticket : la section suivante impose un `ErrorMessage` français explicite sur **chaque** attribut de ces DTOs, ce qui élimine toute dépendance à une résolution de culture (déterministe, quelle que soit la configuration de `UseRequestLocalization`). Le chevauchement possible entre `fr-FR` et les messages par défaut de `DataAnnotations` reste néanmoins **[À CONFIRMER]** pour tout DTO **hors périmètre** de ce ticket (voir « Limite de périmètre » de la section suivante) — à vérifier empiriquement si un ticket futur les couvre, pas à supposer.

## Objectif et périmètre

**Objectif** : que tous les messages natifs `IdentityErrorDescriber` renvoyés au client (candidat, recruteur, admin) soient en français, sans dépendre de la culture serveur, en cohérence avec `french-message-consistency.md`. **Décisions utilisateur ayant élargi le périmètre initial** (voir sections dédiées plus bas) : ce ticket couvre aussi la traduction des `DataAnnotations` des DTOs d'authentification, et l'alignement des règles de mot de passe côté frontend (`candidate-app`/`recruiter-app`) sur la politique réelle du backend.

**Inclus** :
1. Une classe `FrenchIdentityErrorDescriber : IdentityErrorDescriber` (emplacement suggéré : `Utils/FrenchIdentityErrorDescriber.cs` ou `Extensions/FrenchIdentityErrorDescriber.cs`, au choix du developer selon la convention de rangement déjà observée dans le projet), surchargeant **toutes** les méthodes virtuelles de `IdentityErrorDescriber` (liste et traductions cibles ci-dessous).
2. Son enregistrement dans `SecurityExtensions.cs` via `.AddErrorDescriber<FrenchIdentityErrorDescriber>()` (méthode d'extension standard sur le builder Identity), à la suite de `AddIdentity<User, IdentityRole<Guid>>(...)`.
3. Ajout de `app.UseRequestLocalization(...)` dans `Program.cs` avec culture par défaut `fr-FR` (et `SupportedCultures`/`SupportedUICultures` limitées à `fr-FR` — aucune autre culture n'est utilisée aujourd'hui dans le produit) — amélioration générale de configuration, **non** un mécanisme de traduction des messages Identity (voir §6). Placement dans le pipeline : tôt, avant `app.UseAuthorization()`/`app.MapControllers()` (juste après `app.UseCookiePolicy()` est un emplacement raisonnable), pour s'appliquer à chaque requête avant que les middlewares suivants n'en aient besoin.
4. Mise à jour du test unitaire existant `ConfirmEmailAsync_WithInvalidToken_ShouldReturnFailure` (`XpertSphere.MonolithApi.Tests/Services/AuthenticationServiceTests.cs:333-374`) qui construit manuellement un `IdentityError { Description = "Invalid token" }` — ce test mocke `UserManager` et ne passe jamais par le descripteur réel ; son assertion doit être mise à jour vers le texte français retenu pour `InvalidToken()` (ex. `"Jeton invalide."`), pour rester représentative du comportement runtime réel une fois le descripteur en place. **Ce test ne peut pas servir de critère d'acceptation du correctif** (voir critères d'acceptation, point 2).
5. **Traduction des `DataAnnotations` des DTOs d'authentification** (`DTOs/Auth/*.cs`) — voir section dédiée « Traduction des `DataAnnotations`... » ci-dessous pour le détail exhaustif par fichier.
6. **Alignement des règles de mot de passe côté frontend** (`candidate-app`, `recruiter-app`) sur la politique réelle du backend (`ConfigurePasswordOptions`, `SecurityExtensions.cs:44-52`) — voir section dédiée « Coordination frontend... » ci-dessous.

**Hors périmètre** :
- Tout ce qui a déjà été traduit par `french-message-consistency.md` (`Constants.cs`, `Validators/**`, `Services/*.cs`/`Controllers/*.cs` hors canal Identity).
- Extension de `RoleManager`/rôles Identity natifs : non utilisés par ce produit, la surcharge des méthodes correspondantes de `IdentityErrorDescriber` est incluse par exhaustivité mais aucun flux ne les déclenche aujourd'hui.
- Toute construction d'un vrai système i18n multilingue (cf. `french-message-consistency.md`, même exclusion).
- Les `DataAnnotations` de **tout DTO en dehors de `DTOs/Auth/*.cs` et de `DTOs/User/CreateUserDto.cs`** (`Training`, `Organization`, `Role`, `UserRole`, `ApplicationStatusHistory`, `DTOs/User/UpdateUserDto.cs`, etc. — recherché, plusieurs en contiennent) : non auditées par ce ticket, portée volontairement bornée aux DTOs directement liés aux flux `IdentityErrorDescriber` déjà couverts par cette spec ; à traiter dans un ticket séparé si confirmé pertinent (voir « Limite de périmètre » de la section dédiée).
- Toute restructuration de l'architecture de validation (désactivation de la validation automatique du `ModelState`, ajout de validateurs FluentValidation manquants comme celui de `RegisterCandidateDto`, déduplication des règles redondantes entre `DataAnnotations` et FluentValidation) : ce ticket traduit les messages existants, il ne change pas quel mécanisme de validation s'exécute ni dans quel ordre.

## Traduction des `DataAnnotations` des DTOs d'authentification (inclus dans le périmètre — décision utilisateur)

**Découverte faite pendant l'exploration, hors de l'inventaire initial du ticket** (ni dans celui de `french-message-consistency.md`, qui n'a audité que `Constants.cs`/`Validators/**`/`Services/*.cs`/`Controllers/*.cs`, jamais `DTOs/**`) : les contrôleurs utilisent `[ApiController]` (ex. `Controllers/AuthController.cs:12`) sans aucune configuration de `ApiBehaviorOptions.SuppressModelStateInvalidFilter` ni `InvalidModelStateResponseFactory` custom (recherché, absent de tout le projet). La validation automatique du `ModelState` par ASP.NET Core (basée sur les attributs `System.ComponentModel.DataAnnotations` des DTOs) s'exécute donc **avant** que le contrôleur n'appelle le service et sa validation FluentValidation, et court-circuite la requête avec un `400` dès le premier attribut invalide — un mécanisme entièrement distinct d'Identity et de FluentValidation, non couvert par les deux specs existantes. **Décision utilisateur : ce mécanisme est inclus dans le périmètre de ce ticket**, borné aux DTOs d'authentification (`DTOs/Auth/*.cs`) plus `DTOs/User/CreateUserDto.cs` (décision utilisateur confirmée séparément, voir « Limite de périmètre » ci-dessous).

### Mécanisme de correction retenu

Ajouter un `ErrorMessage = "..."` français explicite sur **chaque** attribut de validation de chaque DTO listé ci-dessous — y compris ceux qui n'en ont aujourd'hui aucun (auquel cas c'est le texte anglais par défaut du framework qui s'applique). Ce choix (plutôt que désactiver la validation automatique du `ModelState` ou ajouter les validateurs FluentValidation manquants) est délibérément le moins invasif : il traduit le texte affiché sans changer quel mécanisme de validation s'exécute, ni son ordre d'exécution, ni les règles elles-mêmes — cohérent avec la philosophie déjà appliquée à `IdentityErrorDescriber` (traduire, ne pas restructurer) et à `french-message-consistency.md`.

Conséquence importante à documenter, pas à corriger ici : plusieurs champs (`Email`, `Password`/`NewPassword` avec sa longueur minimale, `ConfirmPassword`) sont aujourd'hui validés **deux fois** — une fois par `DataAnnotations` (court-circuite en premier via `ModelState`), une fois par le validateur FluentValidation correspondant s'il existe (`RegisterUserDtoValidator.cs` pour `RegisterDto`, aucun validateur pour `RegisterCandidateDto` — voir plus bas). Cette redondance architecturale n'est pas modifiée par ce ticket ; seule la langue des deux couches est désormais alignée sur le français, donc invisible pour l'utilisateur final.

**Exemple concret et significatif** : `RegisterCandidateDto` (DTO de l'inscription candidat réelle) n'a **aucun validateur FluentValidation dédié** (recherché : aucun fichier `Validators/**RegisterCandidate**`). Le seul contrôle de correspondance entre `Password` et `ConfirmPassword` pour ce flux est donc l'attribut `[Compare(..., ErrorMessage = "...")]` ci-dessous — c'est-à-dire que ce message est, aujourd'hui, la seule vérification existante de ce point précis pour l'inscription candidat.

Reachabilité par flux normal de l'UI : côté `candidate-app`, `MultiStepRegisterForm.vue:438-441` bloque déjà côté client une confirmation de mot de passe différente avant soumission (q-form) — ce message n'est donc atteignable aujourd'hui que via un appel API direct (Swagger, script, bug client), pas via le parcours normal de l'UI. Les champs `[Required]`/`[EmailAddress]` sans `ErrorMessage` (ex. email manquant ou mal formé) sont, eux, plus facilement atteignables si un champ requis est omis d'une requête API directe ou si un futur changement frontend retire une vérification client existante — traduits par précaution/exhaustivité, même si la reachabilité actuelle par l'UI normale est plus faible que pour le bug `RequireNonAlphanumeric`.

### Détail exhaustif par fichier — traductions à appliquer

| DTO | Propriété | Attribut(s) | Message actuel (si explicite) | `ErrorMessage` français à définir |
|---|---|---|---|---|
| `RegisterDto.cs` | `Email` | `[Required]` / `[EmailAddress]` | *(défaut framework)* | "L'email est obligatoire" / "Format d'email invalide" |
| | `Password` | `[Required]` / `[MinLength(6)]` | *(défaut)* | "Le mot de passe est obligatoire" / "Le mot de passe doit contenir au moins 6 caractères" |
| | `ConfirmPassword` | `[Required]` / `[Compare(nameof(Password))]` | *(défaut)* / "Passwords do not match" | "La confirmation du mot de passe est obligatoire" / "Les mots de passe ne correspondent pas" |
| | `FirstName` | `[Required]` / `[MaxLength(100)]` | *(défaut)* | "Le prénom est obligatoire" / "Le prénom ne peut pas dépasser 100 caractères" |
| | `LastName` | `[Required]` / `[MaxLength(100)]` | *(défaut)* | "Le nom est obligatoire" / "Le nom ne peut pas dépasser 100 caractères" |
| | `PhoneNumber` | `[Phone]` | *(défaut)* | "Format de numéro de téléphone invalide" |
| | `Trainings` | `[Required]` | *(défaut)* | "La liste des formations est obligatoire" (formulation libre, sens à préserver : présence du champ, pas non-vacuité) |
| | `Experiences` | `[Required]` | *(défaut)* | "La liste des expériences est obligatoire" (même remarque) |
| `RegisterCandidateDto.cs` | `Email` | `[Required]` / `[EmailAddress]` | *(défaut)* | idem `RegisterDto.Email` |
| | `Password` | `[Required]` / `[MinLength(6)]` | *(défaut)* | idem `RegisterDto.Password` |
| | `ConfirmPassword` | `[Required]` / `[Compare(nameof(Password))]` | *(défaut)* / "Passwords do not match" | idem `RegisterDto.ConfirmPassword` — **seule vérification existante pour ce flux, voir remarque ci-dessus** |
| | `FirstName` | `[Required]` / `[MaxLength(100)]` | *(défaut)* | idem `RegisterDto.FirstName` |
| | `LastName` | `[Required]` / `[MaxLength(100)]` | *(défaut)* | idem `RegisterDto.LastName` |
| | `PhoneNumber` | `[Phone]` | *(défaut)* | idem `RegisterDto.PhoneNumber` |
| | `AcceptTerms` | `[Required]` | *(défaut)* | "Vous devez accepter les conditions d'utilisation" |
| | `AcceptPrivacyPolicy` | `[Required]` | *(défaut)* | "Vous devez accepter la politique de confidentialité" |
| `ResetPasswordDto.cs` | `Email` | `[Required]` / `[EmailAddress]` | *(défaut)* | idem `RegisterDto.Email` |
| | `Token` | `[Required]` | *(défaut)* | "Le jeton est obligatoire" |
| | `NewPassword` | `[Required]` / `[MinLength(6)]` | *(défaut)* | "Le mot de passe est obligatoire" / "Le mot de passe doit contenir au moins 6 caractères" |
| | `ConfirmPassword` | `[Required]` / `[Compare(nameof(NewPassword))]` | *(défaut)* / "Passwords do not match" | "La confirmation du mot de passe est obligatoire" / "Les mots de passe ne correspondent pas" |
| `AdminResetPasswordDto.cs` | `Email` | `[Required(ErrorMessage="Email is required")]` / `[EmailAddress(ErrorMessage="Invalid email format")]` | "Email is required" / "Invalid email format" | "L'email est obligatoire" / "Format d'email invalide" |
| | `NewPassword` | `[Required(ErrorMessage="New password is required")]` / `[MinLength(6, ErrorMessage="...")]` | "New password is required" / "Password must be at least 6 characters long" | "Le nouveau mot de passe est obligatoire" / "Le mot de passe doit contenir au moins 6 caractères" |
| | `ConfirmPassword` | `[Required(ErrorMessage="Password confirmation is required")]` | "Password confirmation is required" | "La confirmation du mot de passe est obligatoire" |
| `ChangePasswordDto.cs` (DTO non câblé côté `MonolithApi` — voir note ci-dessous) | `CurrentPassword` | `[Required]` | *(défaut)* | "Le mot de passe actuel est obligatoire" |
| | `NewPassword` | `[Required]` / `[MinLength(6)]` | *(défaut)* | "Le nouveau mot de passe est obligatoire" / "Le mot de passe doit contenir au moins 6 caractères" |
| | `ConfirmPassword` | `[Required]` / `[Compare(nameof(NewPassword))]` | *(défaut)* / "Passwords do not match" | "La confirmation du mot de passe est obligatoire" / "Les mots de passe ne correspondent pas" |
| `ForgotPasswordDto.cs` | `Email` | `[Required]` / `[EmailAddress]` | *(défaut)* | idem `RegisterDto.Email` |
| `ConfirmEmailDto.cs` | `Email` | `[Required]` / `[EmailAddress]` | *(défaut)* | idem `RegisterDto.Email` |
| | `Token` | `[Required]` | *(défaut)* | "Le jeton est obligatoire" |
| `LoginDto.cs` | `Email` | `[Required]` / `[EmailAddress]` | *(défaut)* | idem `RegisterDto.Email` |
| | `Password` | `[Required]` / `[MinLength(6)]` | *(défaut)* | "Le mot de passe est obligatoire" / "Le mot de passe doit contenir au moins 6 caractères" |
| `LinkAccountDto.cs` | `EntraIdToken` | `[Required]` | *(défaut)* | "Le jeton Entra ID est obligatoire" |
| `EntraIdLoginUrlDto.cs` | `Email` | `[EmailAddress]` (non requis) | *(défaut)* | "Format d'email invalide" |
| `AccountLinkingDto.cs` | `Email` | `[Required]` / `[EmailAddress]` | *(défaut)* | idem `RegisterDto.Email` |
| | `EntraIdToken` | `[Required]` | *(défaut)* | "Le jeton Entra ID est obligatoire" |
| `RefreshTokenDto.cs` | `Email` | `[Required]` / `[EmailAddress]` | *(défaut)* | idem `RegisterDto.Email` |
| | `RefreshToken` | `[Required]` | *(défaut)* | "Le jeton de rafraîchissement est obligatoire" |
| `EntraIdCallbackDto.cs` | `Code` | `[Required]` | *(défaut)* | "Le code est obligatoire" |
| | `State` | `[Required]` | *(défaut)* | "Le paramètre state est obligatoire" |
| `ResendConfirmationDto.cs` | `Email` | `[Required]` / `[EmailAddress]` | *(défaut)* | idem `RegisterDto.Email` |
| `DTOs/User/CreateUserDto.cs` (**inclus par décision utilisateur**, voir note dédiée ci-dessous) | `FirstName` | `[Required]` / `[MaxLength(100)]` | *(défaut)* | "Le prénom est obligatoire" / "Le prénom ne peut pas dépasser 100 caractères" |
| | `LastName` | `[Required]` / `[MaxLength(100)]` | *(défaut)* | "Le nom est obligatoire" / "Le nom ne peut pas dépasser 100 caractères" |
| | `Email` | `[Required]` / `[EmailAddress]` / `[MaxLength(255)]` | *(défaut)* | "L'email est obligatoire" / "Format d'email invalide" / "L'email ne peut pas dépasser 255 caractères" |
| | `PhoneNumber` | `[MaxLength(20)]` | *(défaut)* | "Le numéro de téléphone ne peut pas dépasser 20 caractères" |
| | `EmployeeId` | `[MaxLength(50)]` | *(défaut)* | "Le matricule ne peut pas dépasser 50 caractères" |
| | `Department` | `[MaxLength(100)]` | *(défaut)* | "Le département ne peut pas dépasser 100 caractères" |
| | `LinkedInProfile` | `[MaxLength(255)]` | *(défaut)* | "Le profil LinkedIn ne peut pas dépasser 255 caractères" |
| | `CvPath` | `[MaxLength(500)]` | *(défaut)* | "Le chemin du CV ne peut pas dépasser 500 caractères" |
| | `PreferredLanguage` | `[MaxLength(20)]` | *(défaut)* | "La langue préférée ne peut pas dépasser 20 caractères" |
| | `TimeZone` | `[MaxLength(50)]` | *(défaut)* | "Le fuseau horaire ne peut pas dépasser 50 caractères" |
| | `ExternalId` | `[MaxLength(255)]` | *(défaut)* | "L'identifiant externe ne peut pas dépasser 255 caractères" |
| | `Password` | `[Required]` (pas de `[MinLength]` sur ce DTO, contrairement à `RegisterDto`/`LoginDto` — asymétrie préexistante, signalée mais non corrigée par ce ticket qui ne fait que traduire) | *(défaut)* | "Le mot de passe est obligatoire" |
| | `Address.StreetNumber` (classe `AddressDto` imbriquée, même fichier) | `[MaxLength(10)]` | *(défaut)* | "Le numéro de rue ne peut pas dépasser 10 caractères" |
| | `Address.StreetName` | `[MaxLength(200)]` | *(défaut)* | "Le nom de rue ne peut pas dépasser 200 caractères" |
| | `Address.City` | `[MaxLength(100)]` | *(défaut)* | "La ville ne peut pas dépasser 100 caractères" |
| | `Address.PostalCode` | `[MaxLength(20)]` | *(défaut)* | "Le code postal ne peut pas dépasser 20 caractères" |
| | `Address.Region` | `[MaxLength(100)]` | *(défaut)* | "La région ne peut pas dépasser 100 caractères" |
| | `Address.Country` | `[MaxLength(100)]` | *(défaut)* | "Le pays ne peut pas dépasser 100 caractères" |
| | `Address.AddressLine2` | `[MaxLength(100)]` | *(défaut)* | "Le complément d'adresse ne peut pas dépasser 100 caractères" |

**Note sur `ChangePasswordDto.cs`** : recherché exhaustivement, ce DTO n'est référencé nulle part côté `MonolithApi` (aucun contrôleur, aucun service ne l'utilise — code mort côté backend). **Il est néanmoins utilisé côté frontend** (`recruiter-app/src/pages/ProfilePage.vue`, formulaire « Changer le mot de passe », posté vers l'endpoint `/reset-password`) : voir la section « Coordination frontend » ci-dessous pour un constat plus large sur ce point (bug de câblage préexistant, distinct de la présente spec). Ce ticket traduit tout de même les `ErrorMessage` de `ChangePasswordDto.cs` par cohérence/exhaustivité de l'audit des DTOs `Auth`, indépendamment de ce bug de câblage.

### Limite de périmètre

Cette traduction est bornée à `DTOs/Auth/*.cs`, **plus `DTOs/User/CreateUserDto.cs` (décision utilisateur confirmée)** — inclus car ce DTO alimente `POST /api/Users`, le flux « création d'utilisateur interne » (`recruiter-app/UsersPage.vue`) déjà couvert par le tableau du §3 de cette spec pour son volet `IdentityErrorDescriber` ; il aurait été incohérent de traduire les erreurs Identity de ce flux sans traduire aussi ses `DataAnnotations`. D'autres DTOs du projet (`DTOs/TrainingDtos/*.cs`, `DTOs/Organization/UpdateOrganizationCurrencyDto.cs`, `DTOs/Role/*.cs`, `DTOs/UserRole/AssignRoleDto.cs`, `DTOs/ApplicationStatusHistory/UpdateApplicationStatusHistoryDto.cs`, `DTOs/User/UpdateUserDto.cs`, etc.) contiennent également des `DataAnnotations`, potentiellement avec le même résidu anglais — **non audités par ce ticket** (portée volontairement limitée aux DTOs d'authentification et à ce seul DTO de création d'utilisateur, tous deux directement liés aux flux `IdentityErrorDescriber` déjà couverts par cette spec). Si l'utilisateur souhaite une traduction exhaustive de tous les DTOs du projet, cela doit faire l'objet d'un ticket séparé, avec son propre audit exhaustif (ne pas supposer que cette liste est complète pour l'ensemble du projet).

## Acteurs et permissions

Aucun changement d'autorisation. Tous les acteurs déjà concernés par les flux du tableau §3 (candidat, recruteur/admin d'organisation, admin plateforme) reçoivent désormais ces messages en français.

## Règles métier et gabarit de traduction

Traductions cibles pour chaque méthode de `IdentityErrorDescriber` (le developer peut ajuster la formulation tant que le sens et le registre restent cohérents avec le reste du produit déjà en français ; les paramètres interpolés `{0}` doivent être préservés à l'identique) :

| Méthode | Texte anglais par défaut | Traduction cible proposée |
|---|---|---|
| `DefaultError()` | "An unknown failure has occurred." | "Une erreur inconnue est survenue." |
| `ConcurrencyFailure()` | "Optimistic concurrency failure, object has been modified." | "Échec de concurrence : cet élément a été modifié entre-temps." |
| `PasswordMismatch()` | "Incorrect password." | "Mot de passe incorrect." |
| `InvalidToken()` | "Invalid token." | "Jeton invalide." |
| `LoginAlreadyAssociated()` | "A user with this login already exists." | "Un utilisateur avec cette connexion existe déjà." |
| `InvalidUserName(string userName)` | "User name '{0}' is invalid, can only contain letters or digits." | "Le nom d'utilisateur « {0} » n'est pas valide : il ne peut contenir que des lettres ou des chiffres." |
| `InvalidEmail(string email)` | "Email '{0}' is invalid." | "L'email « {0} » n'est pas valide." |
| `DuplicateUserName(string userName)` | "User name '{0}' is already taken." | "Le nom d'utilisateur « {0} » est déjà utilisé." |
| `DuplicateEmail(string email)` | "Email '{0}' is already taken." | "L'email « {0} » est déjà utilisé." |
| `InvalidRoleName(string role)` | "Role name '{0}' is invalid." | "Le nom de rôle « {0} » n'est pas valide." |
| `DuplicateRoleName(string role)` | "Role name '{0}' is already taken." | "Le nom de rôle « {0} » est déjà utilisé." |
| `UserAlreadyHasPassword()` | "User already has a password set." | "Cet utilisateur possède déjà un mot de passe." |
| `UserLockoutNotEnabled()` | "Lockout is not enabled for this user." | "Le verrouillage n'est pas activé pour cet utilisateur." |
| `UserAlreadyInRole(string role)` | "User already in role '{0}'." | "L'utilisateur possède déjà le rôle « {0} »." |
| `UserNotInRole(string role)` | "User is not in role '{0}'." | "L'utilisateur ne possède pas le rôle « {0} »." |
| `PasswordTooShort(int length)` | "Passwords must be at least {0} characters." | "Le mot de passe doit contenir au moins {0} caractères." |
| `PasswordRequiresUniqueChars(int uniqueChars)` | "Passwords must use at least {0} different characters." | "Le mot de passe doit contenir au moins {0} caractère(s) distinct(s)." |
| `PasswordRequiresNonAlphanumeric()` | "Passwords must have at least one non alphanumeric character." | "Le mot de passe doit contenir au moins un caractère spécial (non alphanumérique)." |
| `PasswordRequiresDigit()` | "Passwords must have at least one digit ('0'-'9')." | "Le mot de passe doit contenir au moins un chiffre (0-9)." |
| `PasswordRequiresLower()` | "Passwords must have at least one lowercase ('a'-'z')." | "Le mot de passe doit contenir au moins une lettre minuscule (a-z)." |
| `PasswordRequiresUpper()` | "Passwords must have at least one uppercase ('A'-'Z')." | "Le mot de passe doit contenir au moins une lettre majuscule (A-Z)." |
| `RecoveryCodeRedemptionFailed()` | "Recovery code redemption failed." | "Échec de l'utilisation du code de récupération." |

Règles transverses :
- **Fidélité de sens**, pas de reformulation qui changerait le comportement métier (ex. distinction singulier/pluriel des placeholders reste au jugement du developer, sans changer l'information transmise).
- **Registre cohérent** avec le reste des messages déjà traduits par `french-message-consistency.md` (ton neutre, pas de tournure familière).
- **Ne jamais supprimer un placeholder `{0}`** (ex. `PasswordTooShort` doit continuer à refléter dynamiquement 6 ou 8 selon l'environnement, jamais une valeur codée en dur dans le message).

## Critères d'acceptation

1. `FrenchIdentityErrorDescriber` existe, hérite de `IdentityErrorDescriber`, et surcharge **toutes** les méthodes virtuelles de la classe de base (aucune n'est laissée à son implémentation par défaut) — le tableau ci-dessus en liste 22 sur la base du SDK .NET 9 utilisé au moment de la rédaction ; ce nombre est indicatif, le developer doit vérifier la liste exacte des méthodes virtuelles exposées par la version du SDK réellement utilisée (une méthode ajoutée/retirée entre versions ne doit pas être oubliée ni faussement comptée).
2. `SecurityExtensions.AddSecurity` enregistre ce descripteur (`.AddErrorDescriber<FrenchIdentityErrorDescriber>()` après `AddIdentity<User, IdentityRole<Guid>>(...)`), vérifiable par lecture du code — **pas seulement** par un test unitaire mocké (voir point 3, `dotnet test` seul ne suffit pas comme preuve).
3. **Vérification par un flux réel (pas mocké)**, à faire manuellement ou via un test d'intégration si l'infrastructure de test le permet : appeler `POST /api/Auth/register/candidate` avec un mot de passe respectant la longueur **dans les deux environnements** mais sans caractère non-alphanumérique (ex. `Abcdefg1`, 8 caractères — volontairement ≥ 8 pour isoler `PasswordRequiresNonAlphanumeric` sans déclencher aussi `PasswordTooShort` en Production où `RequiredLength = 8`) et constater que le message d'erreur retourné est entièrement en français (`"Le mot de passe doit contenir au moins un caractère spécial (non alphanumérique)."`), sans aucun fragment anglais résiduel. Un test unitaire qui mocke `UserManager`/construit un `IdentityError` à la main (comme l'existant `ConfirmEmailAsync_WithInvalidToken_ShouldReturnFailure`) ne prouve pas ce point, puisqu'il ne passe jamais par le descripteur réel.
4. Idem pour un lien de confirmation d'email invalide/expiré (`ConfirmEmailAsync`) : message entièrement français, plus de `"Invalid token"` en anglais concaténé.
5. Idem pour la réinitialisation de mot de passe admin (`AdminResetPasswordAsync`, `recruiter-app/UsersPage.vue`) avec un mot de passe ne respectant pas la complexité (atteignable car la règle client ne vérifie que la longueur).
6. `Program.cs` configure `UseRequestLocalization` avec `fr-FR` comme culture par défaut et culture supportée unique — vérifié par lecture du code, sans prétendre que ce point localise à lui seul les messages Identity (voir §6, à ne pas présenter comme la cause du correctif dans la documentation/commit).
7. Le test `ConfirmEmailAsync_WithInvalidToken_ShouldReturnFailure` (`AuthenticationServiceTests.cs:333-374`) est mis à jour pour asserter le texte français retenu (`"Jeton invalide."` ou équivalent), et `dotnet test` passe intégralement.
8. Aucune régression sur les messages déjà traduits par `french-message-consistency.md` (`Constants.cs`, `Validators/**`, `Services/*.cs`/`Controllers/*.cs` hors canal Identity).
9. **`DataAnnotations` des DTOs d'authentification et de `CreateUserDto`** : recherche exhaustive dans les 14 fichiers `DTOs/Auth/*.cs` et dans `DTOs/User/CreateUserDto.cs` (y compris la classe imbriquée `AddressDto`) listés dans la section dédiée : chaque attribut de validation a un `ErrorMessage` français explicite (aucun message par défaut du framework, aucun `ErrorMessage` anglais résiduel type `"Passwords do not match"`). Vérification par flux réel (pas seulement lecture du code), **au niveau du corps de la réponse HTTP** (vérifiable via Swagger ou un test d'intégration) : un appel direct à `POST /api/Auth/register/candidate` avec `Email` manquant renvoie un message entièrement français (`"L'email est obligatoire"` ou équivalent, potentiellement préfixé par le nom du champ selon la forme `ValidationProblemDetails`, ex. `"Email: L'email est obligatoire"` — voir note de rendu ci-dessous) ; un appel avec `Password`/`ConfirmPassword` différents renvoie `"Les mots de passe ne correspondent pas"` en français ; un appel direct à `POST /api/Users` avec `FirstName` manquant renvoie `"Le prénom est obligatoire"` en français. **Ce critère porte sur le corps de la réponse, pas sur son rendu à l'écran** — voir « Limitation de rendu (`recruiter-app`) » dans la section « Coordination frontend » pour la nuance côté affichage (rendu générique confirmé pour le flux `CreateUserDto` dans `recruiter-app`, non corrigé par ce ticket).
10. La limite de périmètre des `DataAnnotations` (bornée à `DTOs/Auth/*.cs` plus `DTOs/User/CreateUserDto.cs`, les autres DTOs du projet — y compris `DTOs/User/UpdateUserDto.cs` — restant non traités) est explicitement documentée dans la PR/le commit, pas silencieusement omise.
11. **Alignement frontend des règles de mot de passe** :
    - `candidate-app/src/components/register/MultiStepRegisterForm.vue` : `passwordRules` inclut désormais une règle de caractère spécial et une longueur minimale de 8 ; un mot de passe respectant majuscule/minuscule/chiffre/longueur mais sans caractère spécial (ex. `Abcdefgh1`) est rejeté côté client avec un message français avant tout envoi au serveur.
    - `recruiter-app` : les 3 emplacements actifs (`UsersPage.vue` création, `UsersPage.vue` dialog reset password, `ProfilePage.vue` changement de mot de passe) appliquent strictement le même jeu de règles (mêmes regex, mêmes libellés) — vérifiable par comparaison directe des 3 blocs de règles ou, si factorisé, par un point d'import unique. `RegisterPage.vue` (4ᵉ occurrence trouvée, page factice sans appel API réel, déjà couverte par une spec de suppression séparée) n'est délibérément pas modifié — vérifier qu'il n'a pas été touché à tort, sauf si sa suppression a entre-temps été implémentée.
    - Aucune régression sur les règles déjà correctes (correspondance des deux champs, longueur ≥ 8 déjà en place à ces 3 emplacements).
    - La limite connue de la regex de caractère spécial (accepte à tort une lettre accentuée comme "spéciale", alors qu'Identity la considère alphanumérique côté serveur) est un résidu accepté, pas une régression à corriger dans ce ticket.

## Coordination frontend — alignement des règles de mot de passe (inclus dans le périmètre — décision utilisateur)

**Constat déclencheur** (§4 ci-dessus) : les règles de mot de passe client-side ne reflètent pas la politique serveur réelle (`ConfigurePasswordOptions`, `SecurityExtensions.cs:44-52` : longueur ≥ 6 en Dev / ≥ 8 en Prod, au moins une majuscule, une minuscule, un chiffre, un caractère non-alphanumérique). C'est ce qui rend le bug n°1 (`RequireNonAlphanumeric`) atteignable depuis le parcours normal de l'UI plutôt que seulement via un appel API direct. **Décision utilisateur : corriger cette désynchronisation dans ce même ticket.**

### Longueur minimale côté client : 8, dans les deux apps, indépendamment de l'environnement backend

Le frontend est un bundle statique qui ne connaît pas, au moment de l'exécution, l'environnement du backend auquel il parle (`RequiredLength` vaut 6 en Development, 8 en Staging/Production), et n'expose aujourd'hui aucun endpoint pour interroger dynamiquement la politique de mot de passe. Plutôt que de complexifier avec une récupération dynamique (hors de proportion avec le problème), retenir **8 comme minimum client dans tous les cas** : un mot de passe de 8 caractères ou plus satisfait toujours l'exigence de longueur du backend, que l'environnement cible impose 6 ou 8 — la règle client ne bloquera donc jamais un mot de passe valide, dans aucun des deux environnements.

### `candidate-app` — `src/components/register/MultiStepRegisterForm.vue:582-588` (`passwordRules`)

Remplacer :
```ts
const passwordRules = [
  (val: string) => !!val || 'Mot de passe requis',
  (val: string) => val.length >= 6 || 'Au moins 6 caractères',
  (val: string) => /[A-Z]/.test(val) || 'Au moins une majuscule',
  (val: string) => /[a-z]/.test(val) || 'Au moins une minuscule',
  (val: string) => /[0-9]/.test(val) || 'Au moins un chiffre',
];
```
par (même style terse déjà en place, un seul ajout de règle + longueur portée à 8) :
```ts
const passwordRules = [
  (val: string) => !!val || 'Mot de passe requis',
  (val: string) => val.length >= 8 || 'Au moins 8 caractères',
  (val: string) => /[A-Z]/.test(val) || 'Au moins une majuscule',
  (val: string) => /[a-z]/.test(val) || 'Au moins une minuscule',
  (val: string) => /[0-9]/.test(val) || 'Au moins un chiffre',
  (val: string) => /[^a-zA-Z0-9]/.test(val) || 'Au moins un caractère spécial',
];
```
Un seul point d'usage dans ce package : pas d'obligation de factoriser dans un composable partagé, mais libre au developer de le faire.

### `recruiter-app` — 3 points d'usage actifs à aligner et à synchroniser entre eux, 1 point d'usage exclu (justifié)

Recherche exhaustive (recherche large sur le contenu, pas seulement sur le littéral `type="password"` qui aurait manqué au moins un composant dans ce même exercice — voir `candidate-app` où `MultiStepRegisterForm.vue` utilise un `:type` dynamique) : 5 fichiers `.vue` de ce package mentionnent « password ». `LoginPage.vue` (connexion, pas de création — hors périmètre, voir plus bas) et `ForgotPasswordPage.vue` (uniquement une adresse email, aucun champ de mot de passe réel) sont écartés à juste titre. Les 3 champs de création/modification de mot de passe suivants sont **actifs** et **atteignables réellement** — règles aujourd'hui incomplètes et incohérentes entre elles :

| Fichier | Lignes actuelles | Règles actuelles | Règles cibles |
|---|---|---|---|
| `src/pages/admin/UsersPage.vue` (création d'utilisateur) | 278-281 | requis, longueur ≥ 8 | + majuscule, minuscule, chiffre, caractère spécial |
| `src/pages/admin/UsersPage.vue` (dialog « Reset Password », champ `newPassword`) | 431-434 | requis, longueur ≥ 8 | + majuscule, minuscule, chiffre, caractère spécial |
| `src/pages/ProfilePage.vue` (« Changer le mot de passe », champ `newPassword`) | 320-324 | requis, longueur ≥ 8 | + majuscule, minuscule, chiffre, caractère spécial |

**4ᵉ occurrence trouvée et explicitement exclue, avec justification (ne pas la traiter silencieusement en la passant sous silence, ni l'inclure par excès de prudence)** : `src/pages/auth/RegisterPage.vue` (`registerForm.password`/`confirmPassword`, lignes ~92-139) a exactement le même défaut (requis + longueur ≥ 8, aucune règle de complexité). Cette page est cependant confirmée **entièrement factice, sans aucun appel API réel** (voir `recruiter-app/.claude/specifications/remove-public-recruiter-registration.md`, déjà existante), et fait l'objet d'une spec de **suppression complète** distincte. Aucun mot de passe saisi ici n'atteint jamais le backend aujourd'hui, donc aucune incohérence client/serveur réelle à corriger sur ce fichier — l'aligner serait un effort perdu sur du code dont la suppression est déjà planifiée ailleurs. Si la suppression de `RegisterPage.vue` n'a pas encore eu lieu au moment où ce ticket est implémenté, ne pas modifier ses règles de mot de passe : laisser la spec de suppression s'en charger.

**Exigence de synchronisation, même principe que la liste de compétences dans `french-message-consistency.md`** : les 3 emplacements actifs doivent utiliser exactement le même jeu de règles (mêmes regex, mêmes libellés). Pour éviter une divergence future entre 3 copies indépendantes, **factoriser ces règles dans un point unique du package** (ex. `recruiter-app/src/composables/passwordRules.ts` exportant un tableau/une fonction de règles Quasar, ou une constante partagée équivalente selon la convention déjà en place dans le package) et importer ce point unique dans les 3 emplacements, plutôt que de dupliquer le tableau trois fois.

Règle cible unique à appliquer aux 3 emplacements actifs (adapter le libellé de la règle "requis" existante si elle diffère légèrement d'un emplacement à l'autre, ex. "Le mot de passe est requis" vs "Le nouveau mot de passe est requis" — conserver le libellé "requis" déjà en place à chaque emplacement, n'ajouter que les 4 nouvelles règles de complexité) :
```ts
(val: string) => val.length >= 8 || 'Minimum 8 caractères',
(val: string) => /[A-Z]/.test(val) || 'Au moins une majuscule',
(val: string) => /[a-z]/.test(val) || 'Au moins une minuscule',
(val: string) => /[0-9]/.test(val) || 'Au moins un chiffre',
(val: string) => /[^a-zA-Z0-9]/.test(val) || 'Au moins un caractère spécial',
```

**Limite connue de la règle « caractère spécial » ci-dessus (candidate-app et recruiter-app), à documenter, pas nécessairement à corriger** : la regex `/[^a-zA-Z0-9]/` traite tout caractère hors ASCII alphanumérique comme « spécial », y compris une lettre accentuée (ex. `é`, `à`). Or Identity détermine `RequireNonAlphanumeric` via `char.IsLetterOrDigit` (Unicode), qui considère une lettre accentuée comme alphanumérique, donc **pas** comme satisfaisant cette exigence côté serveur. Un mot de passe du type `Abcdefgé1` (aucun symbole ASCII, seulement une lettre accentuée) passerait donc la règle client mais serait toujours rejeté par le backend — un résidu de désynchronisation résiduel et volontairement accepté par cette spec (cas limite peu probable en pratique pour des mots de passe saisis au clavier standard), qui se traduit désormais par un message français grâce au reste de ce ticket plutôt que par un blocage silencieux ou un message anglais. Ne pas tenter de reproduire `char.IsLetterOrDigit` côté client (complexité disproportionnée) sauf si l'utilisateur demande explicitement une parité stricte.

**Constat additionnel, signalé mais non corrigé par cette spec** : `ProfilePage.vue` (`changePassword`, ligne ~601) poste vers l'endpoint `/reset-password` (`ResetPasswordAsync`, DTO backend attendu `ResetPasswordDto` — `Email`/`Token`/`NewPassword`/`ConfirmPassword`) un objet typé `ChangePasswordDto` (`currentPassword`/`newPassword`/`confirmPassword`), sans `Email` ni `Token`. C'est une incohérence de contrat pré-existante entre ce formulaire et l'endpoint backend réellement appelé, indépendante de la langue des messages et hors sujet de cette spec de localisation — signalée ici uniquement pour ne pas laisser croire que l'alignement des règles de mot de passe rendrait ce formulaire pleinement fonctionnel de bout en bout. Un ticket séparé serait nécessaire pour corriger ce câblage si confirmé pertinent.

Aucune règle de complexité côté client n'est requise sur `LoginPage.vue` (champ mot de passe de connexion, pas de création) : hors périmètre, comportement inchangé.

### Limitation de rendu (`recruiter-app`) — le corps de la réponse est français, l'affichage écran ne l'est pas toujours

Les deux apps ne traitent pas les réponses d'erreur backend de la même façon. `candidate-app` normalise déjà les deux formes de réponse (`ServiceResult`/`AuthResult` : `errors` en tableau de chaînes ; `ValidationProblemDetails`, produit par la validation automatique du `ModelState` évoquée plus haut : `errors` en objet clé → tableau de messages) via `src/utils/apiErrors.ts` (`extractApiErrorMessages`, déjà en place suite à `candidate-registration-training-validation-error.md`) — le message français des `DataAnnotations` traduites par ce ticket s'affichera donc correctement à l'écran côté `candidate-app`, avec une nuance : le message est préfixé par le nom du champ (ex. `"Email: L'email est obligatoire"`), pas le message brut seul.

**`recruiter-app` n'a pas cet équivalent** (recherché : aucune occurrence de `ValidationProblemDetails`/`extractApiErrorMessages` dans ce package). Constat vérifié sur `userStore.createUser` (`src/stores/userStore.ts:137-160`, flux `POST /api/Users`, alimenté par `CreateUserDto` désormais inclus dans le périmètre de traduction ci-dessus) : en cas d'échec, seul `response?.message` est lu, avec un repli générique français (`"Erreur lors de la création de l'utilisateur"`) si absent — `response.errors` (tableau) n'est jamais lu, et une éventuelle forme `ValidationProblemDetails` (`errors` en objet, pas de `message` au niveau racine) tomberait systématiquement sur ce repli générique plutôt que d'afficher le message de champ spécifique. **Conséquence concrète pour ce ticket** : une fois les `DataAnnotations` de `CreateUserDto` traduites en français par ce ticket (voir table ci-dessus), l'utilisateur de `recruiter-app` verra tout de même aujourd'hui un message générique français (`"Erreur lors de la création de l'utilisateur"`) plutôt que le message de champ précis (`"Le prénom est obligatoire"`, etc.) sur ce flux précis — le corps de la réponse HTTP est bien en français (critère d'acceptation vérifiable via Swagger/test d'intégration), mais son rendu à l'écran dans `recruiter-app` reste générique. Ce comportement de rendu générique existait déjà avant ce ticket et n'est pas aggravé par lui ; ce ticket ne le corrige pas (voir ci-dessous).

**Cette spec ne porte pas de normalisation équivalente à `extractApiErrorMessages` dans `recruiter-app`** : effort de restructuration frontend distinct de la traduction, hors périmètre d'un ticket de localisation. Signalé ici pour que le validator ne conclue pas à tort qu'un message backend français implique automatiquement un affichage écran français et détaillé dans les deux apps de la même manière — en particulier pour `CreateUserDto`, où le gain de ce ticket est réel (plus d'anglais dans le corps de la réponse) mais partiel à l'écran (`recruiter-app` affiche un message générique, pas le détail du champ).

## Fichiers à créer/modifier (récapitulatif)

Backend (`XpertSphere.MonolithApi`) :
- Nouveau fichier : `Utils/FrenchIdentityErrorDescriber.cs` (ou `Extensions/`, au choix du developer) — classe `FrenchIdentityErrorDescriber`.
- `Extensions/SecurityExtensions.cs` : enregistrement `.AddErrorDescriber<FrenchIdentityErrorDescriber>()`.
- `Program.cs` : ajout de `app.UseRequestLocalization(...)` avec `fr-FR` par défaut.
- `DTOs/Auth/RegisterDto.cs`, `RegisterCandidateDto.cs`, `ResetPasswordDto.cs`, `AdminResetPasswordDto.cs`, `ChangePasswordDto.cs`, `ForgotPasswordDto.cs`, `ConfirmEmailDto.cs`, `LoginDto.cs`, `LinkAccountDto.cs`, `EntraIdLoginUrlDto.cs`, `AccountLinkingDto.cs`, `RefreshTokenDto.cs`, `EntraIdCallbackDto.cs`, `ResendConfirmationDto.cs`, `DTOs/User/CreateUserDto.cs` (classes `CreateUserDto` et `AddressDto` imbriquée) : ajout/traduction des `ErrorMessage` (voir tableau exhaustif ci-dessus).

Tests (`XpertSphere.MonolithApi.Tests`) :
- `Services/AuthenticationServiceTests.cs` (lignes 357-373) : mise à jour de l'assertion `ConfirmEmailAsync_WithInvalidToken_ShouldReturnFailure` vers le texte français.

Frontend `candidate-app` :
- `src/components/register/MultiStepRegisterForm.vue` (lignes 582-588) : ajout de la règle de caractère spécial, longueur minimale portée à 8.

Frontend `recruiter-app` :
- `src/pages/admin/UsersPage.vue` (création d'utilisateur, lignes 278-281 ; dialog reset password, lignes 431-434) : ajout des règles de complexité (majuscule, minuscule, chiffre, caractère spécial).
- `src/pages/ProfilePage.vue` (changement de mot de passe, lignes 320-324) : même ajout.
- Nouveau fichier recommandé pour éviter la duplication à 3 endroits : ex. `src/composables/passwordRules.ts` (nom au choix du developer selon la convention du package), importé dans les 3 emplacements ci-dessus.

Aucun changement dans `XpertSphere.CommunicationService`, `XpertSphere.ReportingService`, `XpertSphere.IntegrationService`, `XpertSphere.ResumeAnalyzer`, ni dans les migrations EF Core.
