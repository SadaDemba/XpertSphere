# Cohérence des messages utilisateur en français

## Contexte

Le site XpertSphere mélange aujourd'hui les langues : le backend (`XpertSphere.MonolithApi`) est rédigé à 100% en anglais dans ses messages destinés à l'utilisateur final, alors que l'UI frontend (`candidate-app`, `recruiter-app`) est écrite quasi intégralement en français, avec quelques résidus anglais isolés. Ce ticket vise à uniformiser l'ensemble en français. C'est le premier d'une paire de tickets envisagés : un second ticket, séparé et non traité ici, introduira plus tard un vrai système multilingue FR/EN/ES. Cette spec ne doit rien anticiper de ce second ticket (pas de couche i18n à construire, pas d'extension des fichiers de traduction existants au-delà d'un seul correctif de configuration détaillé plus bas).

## Diagnostic (vérifié dans le code)

### 1. `Utils/Constants.cs`

78 constantes `public const string`, dont 76 sont des messages utilisateur (succès/erreur d'API), 100% en anglais. Les 2 restantes ne sont pas des messages et ne doivent pas être traduites : `XPERTSPHERE` (identifiant technique) et `DEFAULT_PASSWORD` (valeur littérale de mot de passe par défaut).

Exemples représentatifs (gabarit de ton attendu, fautes d'anglais à corriger au passage sans changer le sens) :
- `OPERATION_SUCCEEDED = "Operation completed successfully"` → `"Opération réalisée avec succès"`
- `RESOURCE_NOT_FOUND = "Resource not found"` → `"Ressource introuvable"`
- `ACCESS_DENIED = "Unauthorized acces"` (faute : "acces" sans accent ni double c manquant) → `"Accès non autorisé"`
- `ACCESS_FORBIDDEN = "Forbidden acces"` (même faute) → `"Accès interdit"`
- `CONNECTION_DENIED = "Email or Password Incorrect"` → `"Email ou mot de passe incorrect"`
- `USER_EXIST = "User already exist"` (faute de grammaire anglaise : "already exist" au lieu de "already exists") → `"Cet utilisateur existe déjà"`
- `DELETE_SUCCEEDED = "Successfully deleted"` → `"Suppression réussie"`
- `Error = "An error has occured !"` (faute : "occured" au lieu de "occurred", point d'exclamation à retirer par cohérence de registre avec le reste des messages déjà réécrits) → `"Une erreur est survenue"`

### 2. `Validators/**/*.cs` (FluentValidation)

268 occurrences de `.WithMessage(...)` réparties sur 32 fichiers, toutes en anglais (aucun caractère accentué français détecté). Exemples :
- `Validators/Organization/CreateOrganizationDtoValidator.cs:11` : `"Organization name is required."` → `"Le nom de l'organisation est obligatoire."`
- `Validators/JobOffer/CreateJobOfferDtoValidator.cs:18` : `"Job title is required"` → `"L'intitulé du poste est obligatoire"`
- `Validators/JobOffer/CreateJobOfferDtoValidator.cs:65` : `"Minimum salary cannot be greater than maximum salary"` → `"Le salaire minimum ne peut pas être supérieur au salaire maximum"`

### 3. Extension de périmètre par rapport à l'inventaire initial : `Services/*.cs` et `Controllers/*.cs`

**Ce point n'était pas dans l'inventaire de départ du ticket ; il a été découvert pendant l'exploration et change matériellement le périmètre.** Ces messages empruntent exactement le même canal de transport que ceux de `Constants.cs`/des validators (`ServiceResult`/`AuthResult` → sérialisation JSON → `BaseClient.ts` des deux frontends, qui relaie `error.response.data`/`response.data` sans transformation → affichage direct en toast/bandeau côté UI). Les laisser en anglais viderait l'objectif même du ticket ("uniformiser en français **tous** les messages utilisateur") de facto pour une grosse portion des messages d'erreur réellement vus par les utilisateurs.

Décompte vérifié :
- **~82 messages** codés en dur dans `Services/*.cs`, passés en argument à `ServiceResult.NotFound(...)`, `ServiceResult<T>.NotFound(...)`, `.Conflict(...)`, `.ValidationError([...])`, `AuthResult.Conflict(...)`, `AuthResult.ValidationError([...])`, etc. — jamais via `Constants.*`. Fichiers concernés : `ApplicationStatusHistoryService.cs`, `ApplicationService.cs`, `AuthenticationService.cs`, `OrganizationService.cs`, `JobOfferService.cs`, `RoleService.cs`, `ExperienceService.cs`, `PermissionService.cs`, `RolePermissionService.cs`, `ResumeService.cs`, `UserRoleService.cs`, `TrainingService.cs`, `UserService.cs`.
  - Exemples : `$"Role with ID {id} not found"` → `$"Rôle avec l'ID {id} introuvable"` ; `"User with this email already exists"` → `"Un utilisateur avec cet email existe déjà"` ; `ServiceResult<ApplicationDto>.Conflict("You have already applied to this job offer")` → `"Vous avez déjà postulé à cette offre d'emploi"`.
- **~20 messages** dans `Controllers/*.cs`, essentiellement `return BadRequest("User ID not found in claims")` / `"Organization ID not found in claims"` / `"User ID or Organization ID not found in claims"` (`ApplicationStatusHistoryController.cs`, `ApplicationsController.cs`, `JobOffersController.cs`), ainsi que `AuthCallbackController.cs:191` (`support_message = "Please try again or contact support if the problem persists"`, dans le corps JSON `HandleAuthenticationError`, avec `error_description` par défaut `"An unknown error occurred"` ligne 187).

**Décision proposée par cette spec** : inclure ces deux sources dans le périmètre du ticket (voir "Objectif et périmètre" ci-dessous), avec un **point de confirmation explicite en fin de spec** pour que l'humain puisse arbitrer/reporter cette extension à un ticket séparé s'il préfère limiter l'effort initial. Ne pas attendre cette confirmation pour figer le reste de la spec.

**Discriminateur retenu pour distinguer "message utilisateur" de "texte technique/log" dans `Services/*.cs`/`Controllers/*.cs`** (pour éviter qu'une recherche exhaustive ne remonte indéfiniment des faux positifs) :
- **En périmètre** : chaînes passées en argument aux factory methods `ServiceResult(<T>).NotFound/.Conflict/.ValidationError/.Error/.Success` et `AuthResult.NotFound/.Conflict/.ValidationError/.Error`, ainsi que les littéraux passés à `BadRequest(...)`/`Unauthorized(...)`/`Forbid(...)` dans les contrôleurs et les champs de type message dans un objet JSON retourné au client (ex. `support_message`, `error_description` de `AuthCallbackController.HandleAuthenticationError`).
- **Hors périmètre** : appels `_logger.Log*(...)` (`LogWarning`, `LogError`, etc. — jamais renvoyés au client), messages d'exceptions techniques (`throw new InvalidOperationException(...)`, ex. `Extensions/KeyVaultExtensions.cs:96`), et plus généralement tout texte qui n'atteint jamais le corps de la réponse HTTP consommée par le frontend.

**Vérifié : aucun gestionnaire d'exception global n'existe** (`Program.cs` ne configure ni `UseExceptionHandler` ni `IExceptionHandler` ; les seuls `catch (Exception ex)` trouvés dans les contrôleurs, `AuthCallbackController.cs:93/164`, ne font que logger `ex.Message`, jamais le renvoyer au client). Les messages d'exception brute ne sont donc pas un canal utilisateur conçu comme tel aujourd'hui : c'est une lacune architecturale distincte (absence de gestion d'exception centralisée), hors périmètre de ce ticket, à ne pas corriger ici.

### 4. Contradiction avec une spec précédente — décision inversée dans ce ticket

`candidate-registration-experience-description-error.md` (déjà en place dans ce même dossier) a délibérément tranché, avec confirmation utilisateur explicite à l'époque, de garder `Validators/Experience/CreateExperienceDtoValidator.cs` (`RuleFor(x => x.Description).NotEmpty().WithMessage("Description is required")`) **en anglais**, "cohérent avec la convention existante des autres validators". Cette spec **inverse cette décision** : ce message doit désormais être traduit en français comme les 267 autres, par cohérence avec l'objectif de ce nouveau ticket. Il faut mettre à jour en conséquence les 3 assertions qui testent ce texte exact : `XpertSphere.MonolithApi.Tests/Validators/Experience/CreateExperienceDtoValidatorTests.cs` lignes 29, 50, 71 (`e.ErrorMessage == "Description is required"`).

### 5. Risque de régression sur les tests unitaires — confirmé et à traiter, pas à contourner

Recherche des assertions exactes sur du texte anglais dans `XpertSphere.MonolithApi.Tests/` : au moins 24 assertions de ce type confirmées, réparties sur `Validators/Experience/CreateExperienceDtoValidatorTests.cs` (3, voir §4), `Services/RolePermissionServiceTests.cs`, `Services/OrganizationServiceTests.cs`, `Services/PermissionServiceTests.cs`, `Services/UserRoleServiceTests.cs`, `Services/AuthenticationServiceTests.cs`, `Services/RoleServiceTests.cs` — toutes de la forme `result.Errors.Should().Contain("...")` ou `.ContainSingle(e => e.ErrorMessage == "...")`. Une recherche plus large (`.Should().Contain(`/`.Be(`/`.ContainSingle(` avec un littéral de chaîne, toutes classes de test confondues) remonte 84 occurrences au total ; certaines ne concernent pas des messages utilisateur (assertions sur des valeurs de propriété métier, ex. un nom renvoyé identique à l'entrée), à trier au cas par cas par le developer.

**Ce ticket casse ces tests par construction** (le texte change de langue, l'assertion littérale ne correspond plus). Traitement attendu : **mettre à jour les assertions pour qu'elles vérifient le nouveau texte français**, pas les contourner (ni les supprimer, ni les rendre insensibles à la casse/langue sans raison). `dotnet test` doit passer intégralement une fois les traductions et les assertions alignées.

### 6. Signal d'architecture (à noter, pas à corriger ici)

Le fait que plusieurs tests fassent des assertions sur le contenu textuel exact des messages d'erreur (plutôt que sur un code d'erreur structuré stable, ex. un enum ou une chaîne technique du type `ROLE_NOT_FOUND`) est un signal que ces messages seraient plus robustes en tant que codes stables, le texte affiché étant dérivé côté client ou par une couche de traduction. Ce refactor plus large (introduire des codes d'erreur au lieu de texte libre) est **hors périmètre** de ce ticket — à noter comme limite connue, pas à traiter maintenant. Ce constat renforce d'ailleurs la nécessité du futur ticket i18n multilingue mentionné en introduction.

### 7. Résidus anglais côté frontend

- **Page 404**, identique dans les deux apps : `recruiter-app/src/pages/ErrorNotFound.vue:6` (`"Oops. Nothing here..."`) et `:14` (`label="Go Home"`) ; `candidate-app/src/pages/ErrorNotFound.vue:6` et `:14` (mêmes textes). Proposition : `"Oups. Il n'y a rien ici..."` et `label="Retour à l'accueil"` (à ajuster librement par le developer tant que le sens et le registre restent cohérents avec le reste de l'UI).
- **Listes de compétences** (`recruiter-app` uniquement — vérifié absent de `candidate-app`) : `src/components/candidates/CandidateDialog.vue` (variable `skillsOptions`, ligne ~306) et `src/components/candidates/CandidateFilters.vue` (même liste, ligne ~175) contiennent, mêlés à des noms propres/outils à **ne pas traduire** (`Figma`, `AWS`, `Azure`, `Docker`, `Kubernetes`, `Git`, `Sketch`, `Adobe Creative Suite`, `SEO/SEM`, langages/frameworks comme `JavaScript`, `React`, etc.), des termes génériques anglais à traduire :
  - `'User Research'` → `'Recherche utilisateur'`
  - `'Project Management'` → `'Gestion de projet'`
  - `'UI/UX Design'` → `'Design UI/UX'` (ou équivalent, au choix du developer)
  - `'Prototyping'` (même liste) est un terme générique du même ordre — à traduire également par cohérence (`'Prototypage'`), sauf si le developer juge que ce terme reste couramment utilisé tel quel dans le jargon métier français ; documenter le choix fait si diffèrent de cette suggestion.
  - `'Analytics'` est plus ambigu (terme parfois utilisé tel quel en français dans un contexte marketing/outil) : laisser au jugement du developer, avec la même exigence de cohérence entre les deux fichiers listée ci-dessous.
  - **Ces deux fichiers doivent utiliser exactement les mêmes libellés traduits pour les mêmes termes** (la liste apparaît deux fois, une fois pour l'édition d'un candidat, une fois pour le filtrage) : une divergence entre les deux casserait silencieusement le filtrage par compétence (une valeur choisie dans `CandidateDialog.vue` ne correspondrait plus à celle attendue par `CandidateFilters.vue`).
  - Point d'attention (pas un blocage) : ces libellés ne sont retrouvés dans aucun script de seed ni ailleurs dans le backend — un candidat déjà existant en base (hors seed, créé manuellement via l'UI) qui aurait la valeur anglaise `"Project Management"` stockée comme compétence ne sera pas migré par ce ticket (aucune migration de données n'est demandée) ; il continuera de s'afficher tel quel (un `q-select` avec `use-chips` affiche une valeur du modèle même si elle ne correspond à aucune option de la liste), sans casser l'UI, seulement de façon incohérente avec la nouvelle liste d'options.
- **`recruiter-app/src/composables/dialog.ts:46`** (`new Error('Dialog cancelled')`) et le `onDismiss` équivalent (`'Dialog dismissed'`) : **vérifié hors périmètre**. Ce sont des motifs de rejet de `Promise` internes (résolution de la boîte de dialogue Quasar), jamais lus ni affichés : tous les appelants trouvés (`UsersPage.vue`, `OrganizationsPage.vue`, `RolesPage.vue`, `UserRoleAssignment.vue`) interceptent avec `catch { /* commentaire "User cancelled" */ }` sans jamais lire `error.message`. Ne pas traduire, ne pas toucher.

### 8. Bug de configuration i18n — correctif isolé inclus dans ce ticket

Un scaffold `vue-i18n` existe dans les deux apps (`src/i18n/index.ts`, `src/i18n/en-US/index.ts`, `src/i18n/fr-FR/index.ts`, `src/boot/i18n.ts`) mais n'est branché nulle part dans le code applicatif réel (aucun appel à `useI18n()`/`$t()`/`t()` trouvé en dehors de ce scaffold, dans aucun des deux packages). `boot/i18n.ts` de `recruiter-app` et de `candidate-app` règlent tous deux `locale: 'en-US'` (ligne 26), alors que toute l'UI réelle est écrite en dur en français. **Correctif attendu, borné strictement à ceci** : changer `locale: 'en-US'` en `locale: 'fr-FR'` dans les deux fichiers. Ne pas construire de véritable système i18n, ne pas étendre `en-US/index.ts`/`fr-FR/index.ts` au-delà de ce qui existe déjà — ce travail relève du futur ticket multilingue séparé.

### 9. Confirmé : `BaseClient.ts` relaie le message backend tel quel dans les deux apps

`recruiter-app/src/services/BaseClient.ts` (`get`/`post`/etc.) retourne `error.response.data as T`/`response.data` sans transformation. Vérifié également pour `candidate-app/src/services/BaseClient.ts` : même pattern (`return response.data;` / `return error.response.data as T;`). **Conséquence : traduire `Constants.cs`, les validators, et les messages `Services`/`Controllers` (voir §3) suffit à corriger l'affichage des toasts/messages d'erreur/succès dans les deux apps, sans modification frontend nécessaire pour cette partie** (au-delà des résidus listés au §7 et du correctif §8, qui sont des correctifs frontend distincts et isolés).

### 10. Autres services du monorepo — vérifiés hors périmètre

`XpertSphere.CommunicationService`, `XpertSphere.ReportingService`, `XpertSphere.IntegrationService` : aucun `Constants.cs` ni fichier `Validator` trouvé dans ces trois projets — pas de messages utilisateur en anglais à traduire aujourd'hui (projets encore peu développés sur ce plan). `XpertSphere.ResumeAnalyzer` (Python/FastAPI) contient bien des messages `HTTPException(detail=...)` en anglais (ex. `cv_service.py`, `resume.py` : `"Unsupported file format: ..."`, `"Failed to analyze CV: ..."`), potentiellement consommés par `candidate-app` lors de l'analyse automatique de CV à l'inscription — **non traité par cette spec** : stack et service différents (Python, pas .NET), spécification et `CLAUDE.md` propres à ce service, à traiter dans un ticket séparé si confirmé pertinent. Signalé ici uniquement pour ne pas laisser croire que "tous les messages du site" auraient été couverts après ce ticket.

## Objectif et périmètre

**Objectif** : traduire en français l'ensemble des messages utilisateur actuellement en anglais dans `XpertSphere.MonolithApi`, et corriger les résidus anglais identifiés dans `candidate-app`/`recruiter-app`, sans introduire de système multilingue.

**Inclus** :
1. Les 76 messages utilisateur de `Utils/Constants.cs` (hors `XPERTSPHERE`, `DEFAULT_PASSWORD`).
2. Les 268 `.WithMessage(...)` de `Validators/**/*.cs`, y compris `Validators/Experience/CreateExperienceDtoValidator.cs` (décision inversée, voir §4) et ses 3 tests associés.
3. **Sous réserve de confirmation** (voir "Questions" en fin de spec) : les ~82 messages de `Services/*.cs` et les ~20 de `Controllers/*.cs` identifiés au §3, selon le discriminateur défini (facteurs `ServiceResult`/`AuthResult`, littéraux `BadRequest(...)`, champs de message JSON explicitement retournés au client).
4. Page 404 des deux apps (`ErrorNotFound.vue`).
5. Les 3-4 termes génériques des listes de compétences (`CandidateDialog.vue`, `CandidateFilters.vue` de `recruiter-app`), en excluant explicitement les noms propres/outils.
6. Locale par défaut `fr-FR` dans `boot/i18n.ts` des deux apps.
7. Mise à jour des assertions de tests backend impactées (`XpertSphere.MonolithApi.Tests`), pour que `dotnet test` passe intégralement après traduction.

**Hors périmètre** :
- Tout système i18n multilingue réel (FR/EN/ES) : ticket futur séparé, rien à anticiper ici.
- Extension des fichiers `src/i18n/en-US/index.ts`/`fr-FR/index.ts` au-delà de rien (ils ne sont pas modifiés du tout ; seul `boot/i18n.ts` change).
- Traduction des noms des constantes elles-mêmes (`OPERATION_SUCCEEDED`, `USER_NOT_FOUND`, etc.) : ce sont des identifiants de code, seules leurs valeurs de chaîne changent.
- Traduction des noms propres/outils dans les listes de compétences (`Figma`, `AWS`, `Azure`, `Docker`, `Kubernetes`, `Git`, `Sketch`, `Adobe Creative Suite`, langages/frameworks, etc.).
- `recruiter-app/src/composables/dialog.ts` (`'Dialog cancelled'`/`'Dialog dismissed'`) : jamais affiché à l'utilisateur, vérifié (§7).
- Refactor vers des codes d'erreur stables au lieu de texte libre traduit (voir §6) : limite documentée, non traitée.
- `XpertSphere.CommunicationService`, `XpertSphere.ReportingService`, `XpertSphere.IntegrationService` : vérifiés sans messages utilisateur à traduire aujourd'hui.
- `XpertSphere.ResumeAnalyzer` (Python/FastAPI) : messages anglais confirmés existants (§10), mais stack différente et hors spécification de ce service — à traiter séparément si confirmé pertinent.
- Logs serveur (`_logger.Log*`) et messages d'exceptions techniques non renvoyés au client (`throw new ...Exception(...)`) : jamais un canal utilisateur, pas de gestionnaire d'exception global exposant `ex.Message` au client (vérifié, §3).
- Toute migration de données pour les candidats existants dont une compétence serait déjà stockée en anglais (§7) : aucune migration n'est demandée par ce ticket.

## Acteurs et permissions

Aucun changement d'autorisation ou de permission. Tous les acteurs (candidat, recruteur, admin d'organisation, admin plateforme) sont concernés en tant que destinataires des messages traduits, selon les endpoints/écrans qu'ils utilisent déjà — ce ticket ne change qui peut faire quoi, seulement la langue de ce qui leur est affiché en retour.

## Règles métier et gabarit de traduction

- **Fidélité de sens** : chaque traduction doit conserver exactement le sens fonctionnel du message anglais d'origine (ex. distinction 400/404/409/422 déjà encodée dans le code appelant ne doit pas changer).
- **Correction des fautes d'anglais rencontrées**, sans changer le sens ni ajouter d'information (ex. "acces" → "accès", "occured" → correction implicite en traduisant, "already exist" → grammaire correcte en français directement).
- **Registre professionnel cohérent** avec le reste de l'UI déjà en français (ton neutre, vouvoiement implicite du "vous" français standard déjà utilisé ailleurs dans les deux apps, pas de tournure trop familière ni trop administrative).
- **Ne pas traduire les noms des constantes/identifiants de code** : seules les valeurs de chaîne (`= "..."`) changent, jamais le nom à gauche du `=` (`Constants.OPERATION_SUCCEEDED` reste `OPERATION_SUCCEEDED`).
- **Interpolation de chaînes préservée** : les `$"...{variable}..."` gardent leurs variables interpolées au même endroit logique dans la phrase traduite (ex. `$"Role with ID {id} not found"` → `$"Rôle avec l'ID {id} introuvable"`, pas de perte de `{id}`).
- **Synchronisation obligatoire** entre `CandidateDialog.vue` et `CandidateFilters.vue` pour les libellés de compétences traduits (voir §7) : mêmes chaînes exactes dans les deux fichiers.

## Critères d'acceptation

1. Recherche exhaustive dans `Utils/Constants.cs` : aucune valeur de constante (hors `XPERTSPHERE`, `DEFAULT_PASSWORD`) ne contient de texte anglais résiduel.
2. Recherche exhaustive de `.WithMessage(` dans `Validators/**/*.cs` : les 268 messages sont en français, y compris `CreateExperienceDtoValidator.cs` (`"Description is required"` traduit).
3. Si le périmètre §3 est confirmé inclus : recherche des littéraux passés aux factory methods `ServiceResult(<T>).NotFound/.Conflict/.ValidationError/.Error` et `AuthResult.NotFound/.Conflict/.ValidationError/.Error` dans `Services/*.cs`, et aux appels `BadRequest(...)`/champs de message JSON explicites dans `Controllers/*.cs` : plus aucun texte anglais résiduel selon le discriminateur défini au §3 (les logs et exceptions techniques ne sont pas concernés par ce critère).
4. `dotnet test` (depuis `XpertSphere.MonolithApi.Tests`) passe intégralement : toutes les assertions de texte identifiées au §5 (au moins les 24 confirmées, plus toute autre trouvée lors d'une revue exhaustive des 84 candidates) sont mises à jour pour vérifier le nouveau texte français, pas contournées.
5. `candidate-app/src/pages/ErrorNotFound.vue` et `recruiter-app/src/pages/ErrorNotFound.vue` : plus aucun texte anglais (titre, libellé du bouton).
6. `recruiter-app/src/components/candidates/CandidateDialog.vue` et `CandidateFilters.vue` : `'User Research'`, `'Project Management'`, `'UI/UX Design'` traduits identiquement dans les deux fichiers ; les noms propres/outils (`Figma`, `AWS`, etc.) restent inchangés.
7. `candidate-app/src/boot/i18n.ts` et `recruiter-app/src/boot/i18n.ts` : `locale: 'fr-FR'` (au lieu de `'en-US'`), aucune autre modification de ces fichiers ni des fichiers `src/i18n/*/index.ts`.
8. Aucun changement de comportement fonctionnel (codes HTTP, structure des DTOs/`ServiceResult`/`AuthResult`, règles de validation elles-mêmes) : uniquement le texte des messages change.
9. Revue manuelle (ou test d'intégration si déjà existant) d'au moins un flux de bout en bout par catégorie de message : un message de succès (ex. connexion), un message d'erreur de validation (ex. champ obligatoire manquant), un message d'erreur métier (ex. ressource introuvable) — affichés en français dans l'UI (`recruiter-app` et `candidate-app`) sans repasser par le texte anglais d'origine.

## Fichiers à créer/modifier (récapitulatif)

Backend (`XpertSphere.MonolithApi`) :
- `Utils/Constants.cs` : traduction des 76 valeurs de messages.
- `Validators/**/*.cs` (32 fichiers listés au §2) : traduction des 268 `.WithMessage(...)`.
- `Services/*.cs` (13 fichiers listés au §3) : traduction des ~82 messages, sous réserve de confirmation.
- `Controllers/*.cs` (`ApplicationStatusHistoryController.cs`, `ApplicationsController.cs`, `JobOffersController.cs`, `AuthCallbackController.cs`) : traduction des ~20 messages, sous réserve de confirmation.

Tests (`XpertSphere.MonolithApi.Tests`) :
- `Validators/Experience/CreateExperienceDtoValidatorTests.cs` (lignes 29, 50, 71).
- `Services/RolePermissionServiceTests.cs`, `Services/OrganizationServiceTests.cs`, `Services/PermissionServiceTests.cs`, `Services/UserRoleServiceTests.cs`, `Services/AuthenticationServiceTests.cs`, `Services/RoleServiceTests.cs`, et toute autre classe de test dont une assertion de texte exact serait impactée par la traduction (revue exhaustive requise, voir §5).

Frontend `recruiter-app` :
- `src/pages/ErrorNotFound.vue`
- `src/components/candidates/CandidateDialog.vue`, `src/components/candidates/CandidateFilters.vue` (libellés de compétences génériques)
- `src/boot/i18n.ts` (locale par défaut)

Frontend `candidate-app` :
- `src/pages/ErrorNotFound.vue`
- `src/boot/i18n.ts` (locale par défaut)

Aucun changement dans `docker-compose.yml`, les migrations EF Core, `XpertSphere.CommunicationService`, `XpertSphere.ReportingService`, `XpertSphere.IntegrationService`, `XpertSphere.ResumeAnalyzer`.

## Questions à trancher (regroupées, non bloquantes pour le reste de la spec)

1. **[À CONFIRMER]** Le périmètre de ce ticket inclut-il la traduction des ~82 messages de `Services/*.cs` et des ~20 de `Controllers/*.cs` (§3, découverte hors inventaire initial), ou doit-elle être reportée à un ticket séparé pour limiter l'effort de cette première itération ? Cette spec recommande de les inclure (cohérence avec l'objectif du ticket, canal de transport identique aux messages déjà prévus), mais laisse le choix final à l'utilisateur.
2. **[À CONFIRMER]** `AuthCallbackController.cs:191` (`support_message`, `error_description` par défaut) : ces champs sont-ils affichés tels quels dans une page d'erreur d'authentification côté frontend, ou seulement loggés/transmis à un composant qui ne les affiche pas directement ? Si affichés, ils suivent le même traitement que les autres messages `Controllers/*.cs` du §3 ; sinon, à documenter comme hors périmètre au même titre que les logs.
3. **`'Prototyping'`/`'Analytics'`** dans les listes de compétences (§7) : cette spec propose une traduction par défaut (`'Prototypage'`) et laisse `'Analytics'` à la discrétion du developer — si l'utilisateur a une préférence tranchée, la préciser ; sinon le developer choisit et documente son choix dans la PR.
