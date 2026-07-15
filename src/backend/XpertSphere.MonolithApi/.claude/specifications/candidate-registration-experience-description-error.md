# Message d'erreur explicite — description d'expérience vide à l'inscription candidat

## Contexte et diagnostic (à partir du code réel)

Signalement initial : lors de la création d'un compte candidat, si une formation ("Training") ou une expérience ("Experience") a une description vide, la validation bloquerait la soumission avec un message générique, sans indiquer quel champ/élément est concerné.

L'exploration du flux réel de bout en bout (`candidate-app` → `POST /api/auth/register/candidate` → `AuthenticationService.RegisterCandidateAsync`) donne un diagnostic différent de l'intuition initiale, détaillé ci-dessous. Cette spec se fonde sur ce diagnostic, pas sur l'hypothèse de départ.

### 1. Training n'a pas de champ Description — nulle part

- Modèle EF `Models/Training.cs` : `School`, `Period`, `Field`, `Level`. Pas de `Description`.
- `DTOs/TrainingDtos/CreateTrainingDto.cs` : mêmes champs, pas de `Description`.
- Interface frontend `candidate-app/src/models/auth.ts` (`Training`) : `school`, `level`, `period`, `field`. Pas de `description`.
- Étape "Formations" du formulaire d'inscription (`MultiStepRegisterForm.vue`, step 4) : aucun champ Description dans le template.

Une formation avec "description vide" ne peut donc pas se produire dans ce flux : le champ n'existe pas. **Confirmé par l'utilisateur** : il s'agit d'une confusion avec Experience dans le signalement initial, pas d'une intention métier d'ajouter un champ Description à Training. Aucune action requise sur Training ; cette spec couvre déjà le cas réel (Experience).

### 2. Experience.Description existe mais n'est pas requis, et ne bloque rien aujourd'hui

- `DTOs/ExperienceDtos/CreateExperienceDto.cs` : `public string? Description { get; set; }` — nullable, aucun `[Required]`, aucune règle FluentValidation associée.
- Aucun validator `CreateExperienceDtoValidator` n'existe dans `Validators/` (contrairement à la plupart des autres DTOs de création : `Validators/User/CreateUserDtoValidator.cs`, `Validators/JobOffer/CreateJobOfferDtoValidator.cs`, etc. — pattern absent ici).
- `Services/ExperienceService.cs` : le constructeur accepte `IValidator<CreateExperienceDto>? createValidator = null` mais **`CreateExperienceAsync` ne l'appelle jamais** — le paramètre est mort. Seul `UpdateExperienceAsync` invoque son validator (`_updateValidator`).
- `Services/AuthenticationService.cs`, `RegisterCandidateAsync` (lignes ~243-261) : le `ServiceResult` retourné par `_trainingService.CreateTrainingAsync(...)` et `_experienceService.CreateExperienceAsync(...)` est **ignoré** (résultat de l'`await` jamais testé) — même si une validation existait et échouait, l'inscription continuerait comme si de rien n'était.
- Côté `candidate-app`, `MultiStepRegisterForm.vue` (step 5, "Expériences") : chaque `q-input` de description porte `label="Description *"` et `:rules="[(val) => !!val || 'Description requise']"` — cosmétique uniquement :
  - `canProceed` pour l'étape 5 renvoie **toujours `true`** (commentaire `// Experience is optional`), donc le bouton "Suivant" n'est jamais désactivé même avec une description vide.
  - `canSubmit` (bouton "Créer mon compte") ne revérifie pas non plus les expériences.
  - Il n'y a pas de `<q-form>` englobant ni d'appel à `.validate()` sur les steps : la règle Quasar n'affiche qu'un texte d'aide sous le champ au blur, sans jamais empêcher la navigation/soumission.
  - `stepErrors` (`ref<Record<number, boolean>>({})`) est déclaré et lié aux `:error` des `q-step`, mais **n'est jamais renseigné** dans le script — code mort, l'étape "Expériences" ne s'affiche jamais en erreur dans le stepper.
  - `authService.ts`, `registerCandidate()` (lignes 46-58) : lors de la construction du `FormData`, `if (experience.description) formData.append(...)` — si la description est vide, le champ n'est **même pas envoyé** au backend.
  - Backend : `CreateExperienceDto.Description` étant optionnel, l'absence du champ dans le `FormData` ne déclenche aucune erreur de model binding.

**Conséquence factuelle actuelle** : un candidat peut soumettre le formulaire avec une expérience dont la description est vide ; le compte est créé avec succès, sans aucune erreur, silencieusement. Le blocage décrit dans le signalement n'existe donc pas dans le code actuel pour Experience non plus — seule l'apparence (astérisque, message d'aide sous le champ) suggère un caractère obligatoire.

### 3. Le canal d'erreur générique existe bel et bien, et serait bien le point de rupture si la validation existait

- `RegisterPage.vue` affiche un unique bandeau générique : `<q-banner v-if="hasError">{{ error }}</q-banner>`, alimenté par `authStore.error`.
- `authStore.ts`, `registerCandidate()` : en cas d'échec, `setError(response?.message || "Erreur lors de l'inscription")` — **seul `response.message` est lu**. Le champ `response.errors` (tableau) est ignoré alors qu'il existe déjà dans le typage frontend :
  - `models/base.ts`, `ResponseResult<T>` : `{ data?, isSuccess, message, statusCode, errors: string[] }`.
  - Ce type correspond exactement à `ServiceResult<T>`/`AuthResult` côté backend (`Utils/Results/ServiceResult.cs`, `Utils/Results/AuthResult.cs` qui hérite de `ServiceResult<AuthResponseDto>`), sérialisé tel quel par `ControllerExtensions.ToActionResult<T>` en cas d'échec (`return controller.BadRequest(result)` avec `result` = l'objet complet, donc `errors` est bien présent dans le corps JSON).
- Donc : si le backend renseignait `Errors` (ex. via `AuthResult.ValidationError([...])`), le message précis existerait déjà dans la réponse HTTP, mais le frontend ne l'exploite pas et retombe sur la chaîne générique codée en dur `"Erreur lors de l'inscription"`. C'est ce mécanisme qui correspond au symptôme "l'utilisateur voit une indication générique sans savoir laquelle".

## Root cause

Chaîne complète actuelle :
1. Backend : aucune règle ne rend `Experience.Description` obligatoire (ni DataAnnotations, ni FluentValidation) → aucune erreur n'est jamais produite pour ce champ.
2. Frontend : la règle Quasar par champ existe visuellement (`*`, message au blur) mais ne bloque ni la navigation ni la soumission (`canProceed`/`canSubmit` ne la testent pas) → le caractère "obligatoire" affiché est trompeur.
3. Le canal d'affichage d'erreur global (`authStore` → bandeau `RegisterPage.vue`) ignore le tableau `errors` déjà typé et disponible, et retombe sur un message générique fixe.

Le signalement ("la validation bloque bien, mais le message n'est pas explicite") correspond à l'**intention métier confirmée par l'utilisateur** (la description d'une expérience doit être obligatoire et bloquer la soumission), mais cette intention n'est aujourd'hui implémentée nulle part de façon effective — ni le blocage, ni donc le message. Cette spec formalise l'intention confirmée (blocage réellement effectif) et le message explicite attendu, sans inventer de règle supplémentaire non demandée.

### 4. Autres chemins de création de compte candidat écartés après vérification

Deux autres endpoints backend acceptent aussi des listes `Trainings`/`Experiences` typées directement sur les modèles EF (`Training`/`Experience`, où `Experience.Description` est un `string` non-nullable — susceptible de déclencher l'inférence implicite `[Required]` d'ASP.NET Core sur model binding, contrairement au flux candidat qui passe par les DTOs `Create...Dto` avec `Description` nullable) : `POST /api/auth/register` (`RegisterDto`, réservé `[Authorize(Policy = "CanCreateUsers")]`) et `POST /api/users` (`CreateUserDto`, `UsersController.CreateUser`, même policy). Vérification faite côté `recruiter-app` : ni `pages/auth/RegisterPage.vue` (self-inscription d'un recruteur — champs `company`/`jobTitle`, sans notion de Training/Experience) ni `components/candidates/CandidateDialog.vue` (création/édition d'un candidat depuis l'interface recruteur — le seul champ `experience` y est un sélecteur "années d'expérience", pas une liste d'objets `Experience`) ne construisent ni n'envoient de `Trainings`/`Experiences` peuplés vers ces deux endpoints. Aucun chemin actif ne passe donc par cette variante (EF models + inférence implicite) aujourd'hui : le flux réellement empruntable pour créer un compte candidat avec formations/expériences est bien `POST /api/auth/register/candidate` (`RegisterCandidateDto`), objet de cette spec. Si un futur endpoint/écran venait à peupler `RegisterDto.Experiences` ou `CreateUserDto.Experiences`, la même exigence (description obligatoire, message explicite avec index/titre) devrait y être répliquée — hors périmètre de ce correctif tant qu'aucun appelant réel n'existe.

## Objectif et périmètre

- Rendre `Experience.Description` réellement obligatoire lors de la création d'un compte candidat (`POST /api/auth/register/candidate`), à la fois côté frontend (`candidate-app`) et côté backend (`XpertSphere.MonolithApi`), avec un message d'erreur explicite identifiant précisément l'expérience concernée.
- Corriger l'affichage du message d'erreur générique côté frontend pour qu'il exploite les erreurs détaillées déjà renvoyées par le backend (`errors: string[]`), au lieu de toujours afficher la chaîne fixe `"Erreur lors de l'inscription"`.
- **Hors périmètre** : Training — aucun champ Description n'existe (modèle EF, DTO, modèle frontend, formulaire), et il est confirmé qu'il ne doit pas en avoir : le signalement initial le mentionnant est une confusion avec Experience, pas une intention métier. Les autres champs obligatoires d'Experience (`title`, `company`, `date`) présentent le même défaut de gating (`canProceed`/`canSubmit` ne les vérifient pas) mais ne sont pas signalés dans le ticket initial : ne pas les corriger dans cette spec, uniquement documenter l'observation pour un futur correctif séparé.
- **Hors périmètre** : les endpoints `TrainingsController`/`ExperiencesController` (CRUD post-inscription, utilisés par `ProfilePage.vue`) ne sont pas concernés par cette spec, qui porte uniquement sur le flux de création de compte (`RegisterCandidateAsync`). `UpdateExperienceDtoValidator` (utilisé par `UpdateExperienceAsync`) n'est pas modifié.

## Acteurs et permissions

- **Candidat (anonyme)** : seul acteur de ce flux, via `POST /api/auth/register/candidate` (`[AllowAnonymous]`). Aucun changement de permission.

## Règles métier et cas limites

### Règle métier

Si `RegisterCandidateDto.Experiences` contient au moins une entrée, **chaque** entrée doit avoir une `Description` non vide (`NotEmpty`, y compris rejet des chaînes blanches) pour que l'inscription soit acceptée. Une liste d'expériences vide ou absente (`null`/`[]`) reste autorisée (l'étape "Expériences" demeure optionnelle dans son ensemble, seul son contenu, une fois une expérience ajoutée, doit être complet sur ce champ).

### Identification de l'expérience fautive

Le formulaire (step 5 de `MultiStepRegisterForm.vue`) permet d'ajouter plusieurs expériences (`formData.experiences: Experience[]`, bouton "Ajouter une expérience", chaque carte affichant "Expérience {{ index + 1 }}"). Le message d'erreur doit donc identifier l'expérience fautive par sa position dans la liste (index 1-based, cohérent avec l'affichage "Expérience N" déjà utilisé dans le template) et par son intitulé de poste (`title`) s'il est renseigné, pour éviter toute ambiguïté quand plusieurs expériences sont en cours de saisie.

- Si plusieurs expériences ont une description vide, **toutes** doivent être signalées en une seule fois (ne pas s'arrêter à la première trouvée), à la fois côté frontend (validation avant appel API) et côté backend (agrégation des erreurs avant retour).

### Cas limites

- Chaîne composée uniquement d'espaces (`"   "`) : traitée comme vide (utiliser `NotEmpty()` de FluentValidation côté backend, qui trim déjà l'entrée ; côté frontend, tester `!!val?.trim()`).
- CV analysé automatiquement (`shouldAnalyzeCV`, `fillFromCVAnalysis`) : les expériences extraites du CV peuvent arriver avec une description vide ou absente selon la qualité de l'extraction. La même règle de blocage s'applique : l'utilisateur doit compléter manuellement avant de pouvoir avancer/soumettre, avec le même message explicite.
- Suppression d'une expérience (`removeExperience`) : après suppression, les index des expériences restantes changent — le message d'erreur affiché doit toujours refléter l'index **courant** au moment de la validation (recalculé), pas un index mis en cache.
- Un candidat qui appellerait directement l'API (sans passer par `candidate-app`, ex. script, Postman) doit être bloqué par le backend avec la même règle : le frontend seul ne suffit pas, cf. "Contrat d'interface" plus bas.

## Contrat d'interface

### Backend — `XpertSphere.MonolithApi`

**Nouveau** : `Validators/Experience/CreateExperienceDtoValidator.cs`
```
RuleFor(x => x.Description).NotEmpty().WithMessage("Description is required");
```
(Convention de messages : en anglais, cohérent avec les validators existants — `RegisterUserDtoValidator`, etc., tous en anglais malgré une UI candidate-app en français. **Confirmé par l'utilisateur** : garder l'anglais pour ce message backend, cohérent avec la convention existante des autres validators. Ce message n'est qu'un filet de sécurité pour un appel API direct/bypass — en usage normal via le formulaire, le blocage frontend en français intervient avant tout appel API et ce message backend n'est jamais visible. Pas de couche de traduction à introduire pour ce cas résiduel.)

Ce validator ne connaît pas la position de l'expérience dans la liste (il valide un DTO isolé) : l'enrichissement du message avec l'index et le titre se fait dans l'appelant (`AuthenticationService`), pas dans le validator.

**Modification** : `Services/AuthenticationService.cs`, `RegisterCandidateAsync` :
- Avant la création de l'utilisateur (à côté du bloc "Basic validation" existant en tout début de méthode, avant `_userManager.CreateAsync`), valider `registerDto.Experiences` : pour chaque expérience à l'index `i` (0-based) dont `Description` est vide/blanche, ajouter à une liste d'erreurs un message du type :
  `$"Experience #{i + 1} (\"{experience.Title}\") is missing a description."` — si `Title` est vide, retomber sur `$"Experience #{i + 1} is missing a description."`.
- Si la liste d'erreurs n'est pas vide, retourner `AuthResult.ValidationError(errors)` **avant** toute création (utilisateur, formations, expériences) — ne pas laisser un rollback de transaction gérer ce cas, la validation doit être un pré-contrôle, cohérent avec le bloc "Basic validation" déjà présent en haut de la méthode pour `AcceptTerms`/`AcceptPrivacyPolicy`.
- Le `_createValidator` injecté dans `ExperienceService` (aujourd'hui mort) n'est pas utilisé pour cette règle contextuelle (elle a besoin de l'index de la liste, hors périmètre du DTO seul) ; le `CreateExperienceDtoValidator` ajouté ci-dessus reste utile pour une validation générique hors du flux d'inscription (ex. futur endpoint de création directe d'expérience), mais n'est pas requis dans le flux `RegisterCandidateAsync` si la validation contextuelle ci-dessus est faite dans `AuthenticationService`. **Décision** : implémenter la validation contextuelle (avec index/titre) directement dans `RegisterCandidateAsync`, le validator FluentValidation servant de garde-fou générique complémentaire si `ExperienceService.CreateExperienceAsync` est un jour appelé en dehors de ce flux (cette dernière partie — brancher effectivement `_createValidator` dans `CreateExperienceAsync` — reste recommandée par cohérence avec le pattern du reste du code, mais n'est pas strictement nécessaire pour fermer ce ticket : à la discrétion du developer si le temps le permet).

**Réponse HTTP en cas d'échec** : `AuthResult.ValidationError(errors)` produit `StatusCode = 422`. `AuthController.RegisterCandidate` retourne `this.ToActionResult(result)`, qui pour `422` appelle `controller.UnprocessableEntity(result)` — le corps JSON contient l'objet `AuthResult` complet sérialisé (camelCase par défaut ASP.NET Core) :
```json
{
  "isSuccess": false,
  "data": null,
  "message": "",
  "errors": [
    "Experience #2 (\"Backend Developer\") is missing a description."
  ],
  "statusCode": 422
}
```
Ce contrat correspond déjà exactement au type frontend existant `ResponseResult<T>` (`models/base.ts`) — aucun changement de type nécessaire côté frontend.

### Frontend — `candidate-app`

**`components/register/MultiStepRegisterForm.vue`** :
- Ajouter une fonction `getInvalidExperiences()` (ou équivalent) qui parcourt `formData.experiences` et retourne, pour chaque expérience dont `description` est vide/blanche, son index (1-based) et son `title`.
- `canProceed`, cas `5` : passer de `return true;` (commentaire "Experience is optional") à une vérification qui reste `true` si `formData.experiences` est vide, mais qui devient `false` si au moins une expérience a une description vide (l'étape reste optionnelle dans son ensemble, mais son contenu doit être complet une fois commencé).
- `canSubmit` : revalider également les expériences avant d'autoriser "Créer mon compte" (le candidat peut avoir été ramené en arrière/avant, ou avoir une expérience pré-remplie par l'analyse de CV jamais visitée manuellement).
- `nextStep`/`submitForm` : si le blocage est actif, afficher via `notification.showErrorNotification(...)` (composable déjà importé et utilisé dans ce même fichier pour les erreurs d'analyse de CV) un message listant les expériences fautives, ex. pour une seule : `"La description de l'expérience 2 (\"Backend Developer\") est obligatoire."` ; pour plusieurs, une liste (une notification par expérience fautive, ou un message unique concaténé — au choix du developer, tant que chaque expérience fautive est nommément citée).
- Renseigner enfin `stepErrors[5]` (actuellement mort) à `true` quand au moins une expérience est invalide, pour que le stepper (`:error="stepErrors[5]"`) signale visuellement l'étape en cause dans la timeline — mécanisme déjà présent dans le template, seulement jamais alimenté.

**`stores/authStore.ts`**, `registerCandidate()` :
- En cas d'échec (`!response?.isSuccess`), construire le message affiché à partir de `response?.errors` quand ce tableau est non vide (ex. `response.errors.join(' ')` ou premier élément si un seul), et ne retomber sur le message générique `"Erreur lors de l'inscription"` que si `errors` est vide/absent ET `message` est vide. Même traitement pour `login()` par cohérence (même pattern de code, même bug potentiel), **si cela n'allonge pas excessivement le correctif** — sinon, se limiter à `registerCandidate()` qui est le périmètre du ticket.

**`services/authService.ts`**, `registerCandidate()` :
- La garde `if (experience.description) formData.append(...)` (lignes ~56-57) devient sans effet une fois le blocage frontend en place (une description vide ne devrait jamais atteindre cette fonction) ; la laisser telle quelle ne casse rien mais masquerait un futur bypass silencieusement. Recommandé (non bloquant pour la validation du ticket) : envoyer le champ même vide (`formData.append(...)` sans condition sur `description`), pour que le backend voie la valeur réelle envoyée et puisse la rejeter explicitement si le blocage frontend a été contourné.

## Points clarifiés avec l'utilisateur (plus en suspens)

1. **Training sans champ Description** : confusion avec Experience dans le signalement initial, confirmée par l'utilisateur — pas d'intention d'ajouter un champ Description à Training. Aucune action sur Training ; cette spec couvre le cas réel (Experience) en intégralité.
2. **Langue du message backend** : reste en anglais, cohérent avec la convention existante des autres validators (`RegisterUserDtoValidator`, etc.), confirmé par l'utilisateur. Ce message n'est qu'un filet de sécurité pour un appel API direct/bypass, jamais visible en usage normal (le blocage frontend en français intervient avant tout appel API). Aucune couche de traduction à ajouter.

## Critères d'acceptation

1. Formulaire d'inscription (`candidate-app`), étape "Expériences" : ajouter une expérience, laisser sa description vide, cliquer sur "Suivant" → l'étape ne progresse pas, une notification explicite nommant l'expérience concernée par son numéro (et son intitulé de poste si renseigné) s'affiche (ex. "La description de l'expérience 1 (...) est obligatoire."), et le picto d'erreur du step "Expériences" (step 5) s'allume dans le stepper.
2. Ajouter deux expériences, laisser les deux descriptions vides, cliquer sur "Suivant" → les deux expériences sont signalées nommément (aucune n'est passée sous silence).
3. Compléter la description d'une seule des deux expériences fautives, réessayer → seule l'expérience restant incomplète est encore signalée.
4. Revenir en arrière après avoir rempli correctement l'étape 5, puis, sans repasser par cette étape, arriver à l'étape 6 et cliquer sur "Créer mon compte" avec une expérience dont la description a été vidée entre-temps (ex. en revenant sur l'étape 5 sans revalider) → la soumission finale est également bloquée avec le même type de message explicite (pas seulement le passage d'étape).
5. Appel direct de `POST /api/auth/register/candidate` (ex. via un client HTTP, en contournant `candidate-app`) avec un tableau `Experiences` contenant une entrée à `Description` vide (ou composée uniquement d'espaces) → réponse HTTP 422, corps JSON contenant `errors` avec un message citant explicitement l'expérience concernée (numéro et titre si disponible) — pas de création de compte candidat en base (`User`, `Training`, `Experience` : aucune ligne insérée pour cette tentative).
6. Même appel avec deux expériences à description vide → les deux sont listées dans `errors`, pas seulement la première.
7. Un formulaire sans aucune expérience ajoutée (`Experiences: []` ou absent) continue de s'inscrire avec succès (l'étape "Expériences" reste optionnelle dans son ensemble) — non-régression.
8. Un formulaire avec une ou plusieurs formations (`Trainings`) continue de s'inscrire avec succès sans qu'aucune erreur liée à une "description de formation" ne soit produite (Training n'a et ne doit avoir aucun champ Description, confirmé) — non-régression.
9. Simuler une réponse 422 du backend (ex. en isolant `authStore.registerCandidate` dans un test, ou en observant le comportement réel) contenant `errors: ["..."]` et `message: ""` → le bandeau d'erreur de `RegisterPage.vue` affiche le contenu de `errors`, pas la chaîne générique `"Erreur lors de l'inscription"`.
