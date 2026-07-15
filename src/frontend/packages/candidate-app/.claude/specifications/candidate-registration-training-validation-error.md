# Formations incomplètes bloquées trop tard et message d'erreur générique — inscription candidat

## Contexte et diagnostic (à partir du code réel)

Signalement initial (avec preuve réseau) : en laissant certains champs de formation (`Field`, `Level`) vides à l'étape "Formations" du formulaire d'inscription (`candidate-app`), l'utilisateur peut avancer jusqu'à la fin du formulaire (toutes les étapes passent) et cliquer sur "Créer mon compte" — l'inscription échoue seulement à ce moment, avec un bandeau générique "Erreur lors de l'inscription", alors que la vraie réponse HTTP (400, `ValidationProblemDetails`) contient déjà l'information précise :

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Trainings[1].Field": ["The Field field is required."],
    "Trainings[1].Level": ["The Level field is required."],
    "Trainings[2].Field": ["The Field field is required."],
    "Trainings[3].Field": ["The Field field is required."],
    "Trainings[3].Level": ["The Level field is required."]
  },
  "traceId": "00-1abeae971348820e0dfc2ba2bc6eb9b3-bd2af3c1e9b1573e-00"
}
```

### 1. Backend (`XpertSphere.MonolithApi`) — la validation bloque déjà correctement, aucun changement requis

- `Models/Training.cs` : `School`, `Period` (`string?`), `Field`, `Level` — pas de champ `Description`.
- `DTOs/TrainingDtos/CreateTrainingDto.cs` : `[Required]` sur `School`, `Period`, `Field`, `Level` (les quatre champs, sans `ErrorMessage` personnalisé — message par défaut ASP.NET Core, ex. `"The Field field is required."`).
- Aucun `CreateTrainingDtoValidator` FluentValidation n'existe (`Validators/` ne contient aucun sous-dossier `Training`, contrairement à `Validators/User/`, `Validators/JobOffer/`, etc.). Seules les `DataAnnotations` sont en jeu ici — confirmé, contrairement à l'hypothèse initiale d'une validation FluentValidation contextuelle.
- `RegisterCandidateDto.Trainings` est un `List<CreateTrainingDto>?`, posté via `[FromForm]` sur `AuthController.RegisterCandidate` (`[HttpPost("register/candidate")]`, `[AllowAnonymous]`). Le contrôleur porte `[ApiController]`, et **aucune** configuration de `Program.cs` ne modifie le comportement par défaut (`grep` sur `SuppressModelStateInvalidFilter`/`ApiBehaviorOptions`/`InvalidModelStateResponseFactory` : aucune occurrence dans le service). Le pipeline `FluentValidationExtensions.AddFluentValidationConfiguration()` enregistre seulement les validators pour l'injection de dépendances (`AddValidatorsFromAssembly`) — il n'y a pas d'intégration `FluentValidation.AspNetCore` dans le pipeline MVC qui interférerait avec ce constat.
- Conséquence : quand `ModelState` est invalide (un `[Required]` de `CreateTrainingDto` échoue), ASP.NET Core génère automatiquement la réponse `400 ValidationProblemDetails` **avant même l'exécution du corps de `RegisterCandidate`**, donc avant tout appel à `AuthenticationService.RegisterCandidateAsync` et à `_trainingService.CreateTrainingAsync`. **Rien n'est créé en base pour cette tentative** (`User`, `Training`, `Experience` : aucune ligne insérée) — comportement plus propre que le cas Experience.Description (spec sœur, voir section Coordination), où la création a lieu avant que l'absence de blocage ne soit even constatée.
- Cas limite « chaîne uniquement composée d'espaces » : déjà couvert. `System.ComponentModel.DataAnnotations.RequiredAttribute` trim la chaîne avant de tester sa longueur (comportement par défaut, `AllowEmptyStrings = false`) — une valeur `"   "` est donc déjà rejetée par le `[Required]` existant, sans changement nécessaire.
- **Conclusion backend : aucun changement de code n'est nécessaire.** Le blocage serveur fonctionne déjà, produit une réponse structurée et n'écrit rien en base en cas d'échec. Le problème n'est pas fonctionnel côté backend, il est côté frontend : (a) le blocage n'intervient que trop tard (à la soumission finale, pas à l'étape concernée) et (b) le message affiché à l'utilisateur est un texte générique fixe qui ignore le contenu réel de la réponse.
- Observation annexe (documentée, pas une action de ce correctif) : `Models/Training.cs` déclare `Period` nullable (`string?` avec valeur par défaut `""`) alors que `CreateTrainingDto.Period` est `[Required]` — incohérence de nullabilité entre le modèle EF et le DTO de création. Sans impact sur le comportement actuel (le DTO est la seule porte d'entrée validée), à surveiller si un futur besoin métier rend `Period` réellement optionnel côté modèle.

### 2. Frontend (`candidate-app`) — gating de l'étape "Formations" défaillant, symétrique au défaut déjà documenté pour "Expériences"

`MultiStepRegisterForm.vue`, step 4 ("Formations", lignes 209-295 du template, 582-591 et 604-628 du script) :

- Les 4 `q-input` (`school`, `level`, `field`, `period`) portent `label="... *"` et une règle Quasar (`:rules="[(val) => !!val || '... requis']"`) — **cosmétique uniquement** : ces règles Quasar n'affichent qu'un message d'aide au blur, elles ne bloquent ni la navigation ni la soumission (pas de `<q-form ref>` englobant avec `.validate()` appelé avant `nextStep`/`submitForm`).
- `canProceed`, cas `4` : `return true; // Training is optional` — toujours vrai, quel que soit le contenu des formations déjà ajoutées. Défaut identique à celui déjà documenté pour le cas `5` (Expériences) dans la spec sœur.
- `canSubmit` (`canProceed.value && currentStep.value === 6`) ne revérifie pas non plus les formations avant d'autoriser "Créer mon compte".
- `stepErrors` (`ref<Record<number, boolean>>({})`), lié à `:error="stepErrors[4]"` sur le `q-step`, n'est jamais renseigné dans le script — code mort, l'étape "Formations" ne s'affiche jamais en erreur dans le stepper, quel que soit son état.
- `canAddTraining` vérifie bien que la **dernière** formation ajoutée est complète avant d'autoriser l'ajout d'une nouvelle (`school && level && field && period`) — mais ne protège pas contre l'édition ultérieure d'un champ (vidage manuel après ajout) ni contre un peuplement direct du tableau qui ne passe pas par `addTraining()`.
- **Vecteur de contournement identifié, cohérent avec le signalement** (formation 0 complète, formations 1 à 3 partiellement incomplètes sur `Field`/`Level` uniquement — `School`/`Period` déjà renseignés) : `fillFromCVAnalysis()` (ligne 664) exécute `formData.trainings = [...data.trainings]` — assignation directe du tableau extrait de l'analyse de CV, **sans passer par `addTraining()` ni par aucune validation**. Une extraction CV imparfaite (champ `Field`/`Level` mal détecté ou absent du texte du CV) peuple donc directement des formations incomplètes dans `formData`, qui ne sont jamais revalidées ensuite par `canProceed`/`canSubmit`.
- `services/authService.ts`, `registerCandidate()` : `if (training.school) formData.append(...)` (même pattern pour `level`, `period`, `field`) — un champ vide n'est même pas envoyé au backend ; sans effet sur le blocage (le champ manquant déclenche `[Required]` côté backend de la même façon qu'un champ vide envoyé), mais masque silencieusement la valeur réelle envoyée.

### 3. Étendue du problème côté client HTTP — confirmée systémique, pas propre à l'inscription

- `services/BaseClient.ts` : les cinq méthodes protégées (`get`, `post`, `postFormData`, `put`, `delete`) attrapent l'`AxiosError` et renvoient `error.response.data as T` **tel quel**, sans aucune normalisation. Les deux intercepteurs de réponse existants (`blobErrorResponseInterceptor`, `errorResponseInterceptor`) traitent respectivement le cas des téléchargements blob et le rafraîchissement de token 401 — **aucun traitement générique du format `ValidationProblemDetails` (`errors: Record<string, string[]>`) n'existe à un seul endroit centralisé, ni ailleurs**.
- Recherche exhaustive (`grep` sur tous les stores `src/stores/*.ts`) : **`response?.errors` n'est lu à aucun endroit dans `candidate-app`**, que ce soit sous forme de tableau (`ServiceResult`/`AuthResult.errors: string[]`) ou d'objet (`ValidationProblemDetails.errors`). Tous les stores (`authStore`, `experienceStore`, `userStore`, `applicationStore`, `jobOfferStore`) suivent le même pattern : `setError(response?.message || '<message générique fixe>')`. C'est un défaut systémique du client, pas propre au formulaire d'inscription — confirmé, l'utilisateur avait raison de soupçonner que "beaucoup" de réponses d'erreur sont mal exploitées.
- Autre flux exposé au même format de réponse (DTO avec `[Required]` sans validator dédié, comme `CreateTrainingDto`) : `ExperiencesController.ReplaceUserExperiences` (`CreateExperienceDto` porte `[Required]` sur `Title`, `Location`, `Company`, `Date`), appelé par `experienceService.replaceUserExperiences`/`experienceStore.replaceUserExperiences` depuis `ProfilePage.vue` (édition du profil candidat post-inscription) — même défaut reproductible avec la même cause racine. **Hors périmètre de cette spec** (voir section dédiée), documenté ici pour mémoire.
- `ProfilePage.vue`, `saveTrainings()` : stub purement local (commentaire dans le code : `// For now, we'll need to create a training service similar to experience service. Since it's not implemented yet, we'll just update locally`) — **n'appelle aucune API**. Aucun risque actuel pour l'édition de formations post-inscription, faute d'implémentation.

## Root cause

Deux défauts distincts et cumulatifs, tous deux côté frontend :

1. **Gating défaillant à l'étape "Formations"** : `canProceed`/`canSubmit` ne vérifient jamais le contenu de `formData.trainings`, contrairement à l'apparence donnée par les astérisques et les règles Quasar cosmétiques par champ. Un tableau de formations peuplé de façon incomplète — manuellement (édition après ajout) ou automatiquement (`fillFromCVAnalysis`) — traverse toutes les étapes sans jamais être signalé, jusqu'à l'appel API final.
2. **Absence de normalisation des réponses d'erreur backend** : le client HTTP (`BaseClient.ts`) transmet la réponse d'erreur brute sans l'interpréter, et aucun store (dont `authStore`) n'exploite le champ `errors` de la réponse, qu'il s'agisse du format `ValidationProblemDetails` (objet clé→tableau, ce ticket) ou du format `ServiceResult`/`AuthResult` (tableau de chaînes, cas de la spec sœur Experience.Description). Le message affiché retombe systématiquement sur une chaîne générique fixe codée en dur.

## Objectif et périmètre

Ce correctif porte exclusivement sur `candidate-app` et sur le flux d'inscription candidat (`MultiStepRegisterForm.vue` → `POST /api/auth/register/candidate`) :

1. Rendre le gating de l'étape "Formations" (step 4) réellement bloquant, côté navigation (`canProceed`) et côté soumission finale (`canSubmit`), symétriquement au correctif déjà spécifié pour l'étape "Expériences" (step 5) dans la spec sœur — y compris pour les formations peuplées via l'analyse automatique de CV.
2. Introduire une fonction pure de normalisation des réponses d'erreur backend (`extractApiErrorMessages`), capable d'interpréter à la fois le format `ValidationProblemDetails` (`errors: Record<string,string[]>`, ce ticket) et le format `ServiceResult`/`AuthResult` (`errors: string[]`, déjà typé côté frontend), et la brancher dans `authStore.registerCandidate()` pour que le bandeau d'erreur de `RegisterPage.vue` affiche une information exploitable au lieu de la chaîne générique fixe.
3. **Aucun changement backend** : le blocage serveur (`[Required]` sur `CreateTrainingDto`) fonctionne déjà correctement et n'écrit rien en base en cas d'échec — voir diagnostic ci-dessus. Cette spec ne modifie aucun fichier de `XpertSphere.MonolithApi`.

## Acteurs et permissions

- **Candidat (anonyme)** : seul acteur de ce flux, via `POST /api/auth/register/candidate` (`[AllowAnonymous]`). Aucun changement de permission.

## Règles métier et cas limites

### Règle métier — gating étape "Formations"

Si `formData.trainings` contient au moins une entrée, **chaque** entrée doit avoir ses quatre champs (`school`, `level`, `field`, `period`) non vides (trim-aware, cohérent avec le comportement backend `RequiredAttribute`) pour que l'étape puisse être quittée / le formulaire soumis. Un tableau de formations vide ou absent (`null`/`[]`) reste autorisé (l'étape demeure optionnelle dans son ensemble, seul son contenu, une fois une formation ajoutée, doit être complet).

### Identification de la formation fautive

Chaque carte de formation affiche déjà "Formation {{ index + 1 }}" dans le template. Le message de blocage doit identifier la ou les formations fautives par leur position (index 1-based) et, si renseigné, leur établissement (`school`) — le champ le plus proche d'un identifiant lisible pour une formation, par analogie avec `title` pour une expérience. Le message doit aussi citer nommément le ou les champs manquants (parmi "École/Université", "Niveau", "Domaine", "Période" — labels identiques à ceux du template, sans l'astérisque) pour chaque formation fautive.

- Si plusieurs formations ont des champs manquants, **toutes** doivent être signalées en une seule fois (ne pas s'arrêter à la première trouvée).
- Si une même formation a plusieurs champs manquants (ex. `Field` et `Level`, comme dans le signalement initial), tous les champs manquants de cette formation doivent apparaître dans le message qui la concerne.

### Cas limites

- Chaîne composée uniquement d'espaces (`"   "`) : traitée comme vide côté frontend (`!!val?.trim()`), cohérent avec le comportement backend (`RequiredAttribute` trim déjà les valeurs).
- Formations pré-remplies par l'analyse de CV (`fillFromCVAnalysis`, `shouldAnalyzeCV`) : peuvent arriver avec un ou plusieurs champs vides selon la qualité de l'extraction. La même règle de blocage s'applique, avec le même message explicite — le candidat doit compléter manuellement avant de pouvoir avancer/soumettre.
- Suppression d'une formation (`removeTraining`) : après suppression, les index des formations restantes changent — le message affiché doit toujours refléter l'index **courant** au moment de la validation (recalculé), pas un index mis en cache.
- Un candidat qui appellerait directement l'API (sans passer par `candidate-app`) reste bloqué par le backend existant (déjà fonctionnel, non modifié par ce ticket) : le frontend seul ne suffit pas comme garde-fou, cf. critères d'acceptation.
- Réponse backend qui ne correspond à aucun format connu (ni `ValidationProblemDetails.errors` en objet, ni `ServiceResult.errors` en tableau, ni `message`/`title`) : `extractApiErrorMessages` retourne un tableau vide, et `authStore` retombe sur le message générique fixe existant — pas de régression, pas d'exception levée.

## Contrat d'interface

### Backend — `XpertSphere.MonolithApi`

Aucun changement. Le contrat de réponse en cas d'échec de validation `[Required]` sur `CreateTrainingDto` reste le `ValidationProblemDetails` standard ASP.NET Core (RFC 9110), HTTP 400, tel qu'observé dans le signalement initial :

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Trainings[1].Field": ["The Field field is required."],
    "Trainings[1].Level": ["The Level field is required."]
  },
  "traceId": "..."
}
```

### Frontend — `candidate-app`

**Nouveau type** : `src/models/base.ts`, ajouter :

```ts
export interface ValidationProblemDetails {
  type?: string;
  title: string;
  status: number;
  errors: Record<string, string[]>;
  traceId?: string;
}
```

**Nouvelle fonction pure** : `src/utils/apiErrors.ts` (nouveau fichier) :

```ts
export function extractApiErrorMessages(payload: unknown): string[] {
  if (!payload || typeof payload !== 'object') return [];
  const obj = payload as Record<string, unknown>;

  // Format ServiceResult / AuthResult : errors est un tableau de chaînes non vide
  if (
    Array.isArray(obj.errors) &&
    obj.errors.length > 0 &&
    obj.errors.every((e) => typeof e === 'string')
  ) {
    return obj.errors as string[];
  }

  // Format ValidationProblemDetails : errors est un objet clé -> tableau de messages
  if (obj.errors && typeof obj.errors === 'object' && !Array.isArray(obj.errors)) {
    const flattened = Object.entries(obj.errors as Record<string, string[]>).flatMap(
      ([key, messages]) => (messages ?? []).map((msg) => `${key}: ${msg}`),
    );
    if (flattened.length > 0) return flattened;
  }

  // Repli : message (ServiceResult) ou title (ValidationProblemDetails)
  const fallback =
    (typeof obj.message === 'string' && obj.message) ||
    (typeof obj.title === 'string' && obj.title) ||
    '';
  return fallback ? [fallback] : [];
}
```

**Point d'attention impératif (pas seulement stylistique)** : les deux branches `errors` ci-dessus doivent explicitement vérifier que le tableau/objet aplati n'est **pas vide** avant de retourner, faute de quoi un `errors: []` (tableau vide) ou `errors: {}` (objet sans clé) ferait `return []` **avant** d'atteindre le repli `message`/`title` — régression concrète et vérifiée sur ce backend : `AuthResult.Conflict(message)` et `AuthResult.Failure(message)` (`Utils/Results/AuthResult.cs`) peuplent toujours `Errors` avec au moins l'élément `[message]` (jamais vide en pratique aujourd'hui), donc cette régression ne se manifeste pas sur les chemins actuels connus (email déjà utilisé, etc.) — mais la fonction doit rester correcte pour tout appelant futur ou tout format construit manuellement sans passer par les factory methods. C'est aussi ce qu'exige le critère d'acceptation 9 (cas `errors: []` + `message` non vide → doit retourner `[message]`, pas `[]`) : l'implémentation doit être cohérente avec ce critère, pas l'inverse.

- Fonction **pure et testable unitairement**, sans dépendance à Vue/Pinia/Axios — conçue pour pouvoir être reprise sans modification si une centralisation dans `BaseClient.ts` est décidée plus tard (voir section "Hors périmètre").
- Le préfixe `${key}: ` pour le format `ValidationProblemDetails` (ex. `"Trainings[1].Field: The Field field is required."`) reste en l'état technique du backend (clé de `ModelState`, en anglais, non traduite) — acceptable car ce message backend ne doit normalement **jamais être visible en usage normal** : le blocage frontend (étape "Formations", ci-dessous) intervient avant tout appel API. Il ne s'affiche que si le blocage frontend est contourné (appel API direct, ou futur champ sans mirroring frontend) — même philosophie que le message backend en anglais de la spec sœur Experience.Description.

**Modification** : `src/services/authService.ts`, `registerCandidate()` :

- Élargir le type de retour à `Promise<AuthResult | ValidationProblemDetails | null>` (au lieu de `Promise<AuthResult | null>`), pour refléter fidèlement que le corps de la réponse peut être l'un ou l'autre format en cas d'échec.
- Recommandé (non bloquant pour ce ticket) : envoyer les quatre champs de chaque formation même vides (`formData.append(...)` sans condition sur la valeur), symétriquement à la recommandation déjà faite pour `Experience.Description` dans la spec sœur — pour que le backend voie la valeur réelle envoyée si le blocage frontend est un jour contourné.

**Modification** : `src/stores/authStore.ts` :

- Type du paramètre local `response` dans `registerCandidate()` élargi en conséquence (`AuthResult | ValidationProblemDetails | null`).
- **Point d'attention pour le developer** : `ValidationProblemDetails` ne porte pas de propriété `isSuccess`. Une fois le type élargi, `response?.isSuccess` reste utilisable tel quel en JavaScript (`undefined` sur un objet qui n'a pas cette propriété, donc falsy — le `else` s'exécute correctement), mais TypeScript ne pourra plus garantir par narrowing que `response` est un `AuthResult` dans la branche `if (response?.isSuccess)` avant d'appeler `setAuth(response)` (qui attend un `AuthResult`). Un garde de type explicite (ex. `function isAuthResult(r: unknown): r is AuthResult { return !!r && typeof r === 'object' && 'isSuccess' in r; }`, ou une simple assertion `as AuthResult` dans la branche de succès puisque seul un `AuthResult` peut avoir `isSuccess: true`) est nécessaire pour que la compilation passe sans `any`. Détail d'implémentation laissé au developer, mais à anticiper — ne doit pas bloquer la revue si la solution retenue est différente mais équivalente en sûreté de typage.
- En cas d'échec (`!response?.isSuccess`) :
  ```ts
  const messages = extractApiErrorMessages(response);
  setError(messages.length > 0 ? messages.join(' ') : "Erreur lors de l'inscription");
  ```
  au lieu de `setError(response?.message || "Erreur lors de l'inscription")`.
- `login()` n'est **pas modifié** par ce ticket (même pattern de code, même bug potentiel, mais hors périmètre — voir "Hors périmètre" ; peut être traité en suivi si le temps le permet, à la discrétion du developer, sans que cela conditionne la validation de ce ticket).

**Modification** : `src/components/register/MultiStepRegisterForm.vue` :

- Ajouter une fonction (ex. `getInvalidTrainings()`) qui parcourt `formData.trainings` et retourne, pour chaque formation dont au moins un champ parmi `school`/`level`/`field`/`period` est vide/blanc, son index (1-based), son `school` (si renseigné), et la liste des labels de champs manquants (parmi "École/Université", "Niveau", "Domaine", "Période").
- `canProceed`, cas `4` : passer de `return true; // Training is optional` à une vérification qui reste `true` si `formData.trainings` est vide, mais devient `false` si `getInvalidTrainings()` retourne au moins une entrée.
- `canSubmit` : revalider également les formations (via la même fonction) avant d'autoriser "Créer mon compte" — pour couvrir le cas d'une formation invalidée après être passé par l'étape 4 (retour en arrière, ou peuplement tardif par `fillFromCVAnalysis` après un premier passage à l'étape 4).
- `nextStep`/`submitForm` : si le blocage est actif à l'étape 4, afficher via `notification.showErrorNotification(...)` (composable déjà importé/utilisé dans ce fichier) un message citant nommément chaque formation fautive et ses champs manquants, ex. pour une seule formation avec deux champs manquants : `"La formation 2 (\"Université Paris-Est\") est incomplète : Niveau, Domaine sont obligatoires."` ; pour plusieurs formations fautives, une notification par formation (ou un message unique concaténé — au choix du developer, tant que chaque formation fautive et ses champs manquants sont nommément cités).
- Renseigner `stepErrors[4]` (actuellement mort, comme `stepErrors[5]`) à `true` quand `getInvalidTrainings()` n'est pas vide, pour que le stepper (`:error="stepErrors[4]"`) signale visuellement l'étape en cause — mécanisme déjà présent dans le template, seulement jamais alimenté.

## Coordination avec la spec sœur (`candidate-registration-experience-description-error.md`)

Cette spec sœur existe dans le dépôt (branche `feature/secure-cv-download`, commit `f9cdbdb`, fichier `src/backend/XpertSphere.MonolithApi/.claude/specifications/candidate-registration-experience-description-error.md`) mais n'est pas encore fusionnée dans `develop` au moment de la rédaction de cette spec-ci. Elle porte sur un mécanisme distinct (validation FluentValidation contextuelle de `Experience.Description` dans `AuthenticationService.RegisterCandidateAsync`, produisant un `AuthResult.errors: string[]` avec statut 422) mais son volet frontend prévoit exactement la même correction que celle introduite ici dans `authStore.registerCandidate()` : lire `response?.errors` au lieu de se limiter à `response?.message`.

**Conséquence pour la coordination** : une fois cette spec-ci implémentée, `extractApiErrorMessages()` gère déjà nativement le format `errors: string[]` (`ServiceResult`/`AuthResult`, branche `Array.isArray(obj.errors)`) en plus du format `ValidationProblemDetails` (`errors: Record<string, string[]>`) traité ici. **Le volet frontend de la spec sœur devient donc redondant** : quand elle sera implémentée, elle n'a plus besoin de modifier `authStore.ts` — son message backend (`AuthResult.ValidationError([...])`) s'affichera automatiquement via le mécanisme générique introduit ici. La spec sœur n'a plus qu'à :

1. Implémenter son volet backend (validator + validation contextuelle dans `RegisterCandidateAsync`) tel que décrit.
2. Vérifier, une fois son backend implémenté, que le message produit s'affiche correctement dans le bandeau de `RegisterPage.vue` via `extractApiErrorMessages` — sans dupliquer de logique d'affichage côté frontend.

Si la spec sœur est implémentée **avant** celle-ci (ordre inversé), son volet frontend (modification d'`authStore.ts` pour lire `response?.errors`) reste valide en soi mais devra être remplacé/fusionné avec `extractApiErrorMessages()` au moment de l'implémentation de cette spec-ci, pour éviter une double logique de lecture des erreurs dans le même fichier.

## Hors périmètre

- **Backend `XpertSphere.MonolithApi`** : aucun changement. Le blocage `[Required]` sur `CreateTrainingDto` fonctionne déjà et n'a pas besoin d'évoluer pour ce ticket.
- **`authStore.login()`** : même pattern de code, même bug potentiel (`response?.message` uniquement), mais non traité ici — le signalement porte sur l'inscription. Peut être aligné sur `registerCandidate()` en suivi (changement d'une ligne, faible risque), à la discrétion du developer, sans conditionner la validation de ce ticket.
- **`experienceStore.replaceUserExperiences()` / `ProfilePage.vue`** (édition des expériences post-inscription, `ExperiencesController.ReplaceUserExperiences`) : expose le même défaut (`CreateExperienceDto` avec `[Required]` sur plusieurs champs, réponse `ValidationProblemDetails` non exploitée par `experienceStore`), documenté dans le diagnostic ci-dessus, mais non corrigé par ce ticket — flux distinct du formulaire d'inscription.
- **`ProfilePage.saveTrainings()`** : stub purement local, n'appelle aucune API — aucune action nécessaire tant qu'il reste non implémenté.
- **Centralisation dans `BaseClient.ts`** : la solution la plus robuste à terme serait d'intercepter et de normaliser **toutes** les réponses d'erreur (quel que soit le format backend) à un seul endroit du client HTTP, pour que chaque store de `candidate-app` (et par extension `recruiter-app`, qui a son propre client) bénéficie du même traitement sans dupliquer `extractApiErrorMessages()` dans chaque store. Cette évolution changerait le contrat de retour de `get`/`post`/`postFormData`/`put`/`delete` pour tous les appelants existants (impact transverse, hors périmètre d'un correctif ciblé sur l'inscription) — recommandée comme suite possible, non traitée ici. `extractApiErrorMessages()` est conçue comme fonction pure et indépendante de `BaseClient` précisément pour pouvoir être reprise sans modification le jour où cette centralisation sera décidée.
- **`recruiter-app`** : non concerné, ni exploré dans le cadre de ce ticket.
- **Autres champs obligatoires de Training** (`School`, `Period`) : couverts par la même fonction `getInvalidTrainings()`/le même mécanisme de blocage que `Field`/`Level` — pas un hors-périmètre à proprement parler, mais il est précisé que le correctif traite les quatre champs de façon uniforme, pas seulement les deux champs cités dans le signalement initial (qui ne l'étaient que parce que `School`/`Period` étaient déjà renseignés dans le cas observé).
- **Incohérence de nullabilité `Training.Period` (modèle EF `string?` vs DTO `[Required]`)** : documentée dans le diagnostic, non corrigée — sans impact sur le comportement actuel.

## Critères d'acceptation

1. Formulaire d'inscription (`candidate-app`), étape "Formations" : ajouter une formation, renseigner `École/Université` et `Période`, laisser `Niveau` et `Domaine` vides, cliquer sur "Suivant" → l'étape ne progresse pas, une notification explicite nommant la formation concernée (numéro + établissement si renseigné) et listant les champs manquants ("Niveau", "Domaine") s'affiche, et le picto d'erreur du step "Formations" (step 4) s'allume dans le stepper.
2. Ajouter trois formations, chacune avec au moins un champ manquant parmi `Field`/`Level` (reproduisant le signalement initial), cliquer sur "Suivant" → les trois formations sont signalées nommément avec leurs champs manquants respectifs (aucune n'est passée sous silence).
3. Compléter les champs manquants d'une seule des formations fautives, réessayer → seules les formations restant incomplètes sont encore signalées.
4. Simuler un CV analysé automatiquement dont l'extraction produit une formation avec `field` ou `level` vide (`fillFromCVAnalysis`) → en tentant de passer l'étape 4 ou de soumettre à l'étape 6 sans avoir corrigé manuellement, la soumission/navigation est bloquée avec le même type de message explicite.
5. Revenir en arrière après avoir rempli correctement l'étape 4, puis, sans repasser par cette étape, arriver à l'étape 6 et cliquer sur "Créer mon compte" avec une formation dont un champ a été vidé entre-temps → la soumission finale est également bloquée avec le même type de message explicite (pas seulement le passage d'étape).
6. Un formulaire sans aucune formation ajoutée (`trainings: []` ou absent) continue de s'inscrire avec succès (l'étape "Formations" reste optionnelle dans son ensemble) — non-régression.
7. Un formulaire avec une ou plusieurs expériences continue de s'inscrire avec succès sans qu'aucune régression ne soit introduite sur le gating de l'étape "Expériences" (step 5, non modifié par ce ticket) — non-régression.
8. Appel direct de `POST /api/auth/register/candidate` (en contournant `candidate-app`, ex. via un client HTTP) avec un tableau `Trainings` contenant une entrée à `Field`/`Level` vides (ou composés uniquement d'espaces) → réponse HTTP 400 `ValidationProblemDetails` contenant `errors["Trainings[i].Field"]`/`errors["Trainings[i].Level"]` — comportement déjà existant, non-régression, aucune ligne créée en base (`User`, `Training`, `Experience`) pour cette tentative.
9. `extractApiErrorMessages()` testée (unitairement ou par observation directe) avec les trois formats possibles :
   - `{ isSuccess: false, errors: ["Experience #2 is missing a description."], message: "" }` → retourne `["Experience #2 is missing a description."]`.
   - `{ title: "One or more validation errors occurred.", status: 400, errors: { "Trainings[1].Field": ["The Field field is required."], "Trainings[1].Level": ["The Level field is required."] } }` → retourne un tableau de deux éléments contenant les deux messages (préfixés de leur clé).
   - `{ isSuccess: false, message: "Erreur inconnue", errors: [] }` → retourne `["Erreur inconnue"]`.
   - `null`, `undefined`, ou objet sans `errors`/`message`/`title` exploitable → retourne `[]`.
10. En simulant la réponse 400 `ValidationProblemDetails` ci-dessus reçue par `authStore.registerCandidate()` (ex. en isolant l'appel dans un test, ou en observant le comportement réel via contournement du gating frontend) → le bandeau d'erreur de `RegisterPage.vue` affiche un message dérivé de `errors` (contenant les mots "Field"/"Level" ou équivalent), et non la chaîne générique fixe `"Erreur lors de l'inscription"`.
11. En simulant une réponse `AuthResult` 422 avec `errors: ["..."]` et `message: ""` (format de la spec sœur Experience.Description) reçue par `authStore.registerCandidate()` → le même bandeau affiche le contenu de `errors`, démontrant que le mécanisme introduit ici couvre aussi ce format sans modification supplémentaire.
