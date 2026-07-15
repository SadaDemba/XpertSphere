# Expériences/formations absentes du profil candidat juste après connexion (sans reload)

## Contexte et diagnostic (à partir du code réel)

Signalement initial (confirmé, constaté par le propriétaire du projet) : un candidat se connecte sur `candidate-app`, navigue vers sa page de profil (`/profile`) sans recharger la page — les sections "Expériences professionnelles" et "Formations" apparaissent vides. Un simple F5/reload du navigateur les fait apparaître correctement.

L'exploration du code réel (branche `feature/secure-cv-download`, état actuel de `ProfilePage.vue` incluant les modifications de la fonctionnalité `secure-cv-download`) donne un diagnostic précis, qui **exclut** plusieurs pistes intuitives :

### 1. Ce n'est pas un problème de réactivité Vue/Pinia

`candidate-app/src/pages/ProfilePage.vue` :
```ts
const authStore = useAuthStore();
const user = computed(() => authStore.user);
```
et dans le template :
```html
<q-timeline-entry v-for="(exp, index) in user?.experiences || []" ... />
...
<div v-for="(training, index) in user?.trainings || []" ... />
```
`user` est un `computed` sur `authStore.user` (lui-même un `ref` non destructuré dans `authStore.ts`) : toute mutation de `authStore.user` se propage correctement au template. Il n'y a ni destructuration cassant la réactivité, ni variable non réactive, ni watcher manquant. Cette piste est écartée.

### 2. `ProfilePage.vue` ne recharge jamais les données utilisateur elles-mêmes

Le seul appel réseau fait au montage de la page :
```ts
onMounted(() => {
  applicationStore.fetchMyApplications();
});
```
Rien ne recharge `authStore.user` (donc ni `experiences`, ni `trainings`, ni les autres champs du profil). La page se contente d'afficher ce qui est déjà dans `authStore.user` au moment du montage.

### 3. La donnée `authStore.user` posée après connexion est incomplète — cause racine, côté backend

`candidate-app/src/stores/authStore.ts` :
```ts
const setAuth = (authResponse: AuthResult) => {
  if (authResponse.isSuccess && authResponse.data!.user && authResponse.data!.accessToken) {
    user.value = authResponse.data!.user;
    ...
  }
};

const login = async (loginDto: LoginDto): Promise<boolean> => {
  ...
  const response = await authService.login(loginDto);
  if (response?.isSuccess) {
    setAuth(response);
    return true;
  }
  ...
};
```
`login()` pose directement `authStore.user` à partir de la réponse de `POST /Auth/login`, sans jamais appeler `loadCurrentUser()` (qui, lui, tape `GET /Auth/me`).

Côté backend, `Services/AuthenticationService.cs`, `LoginAsync` (lignes ~348-350) :
```csharp
var user = await _userManager.Users
    .Include(u => u.Organization)
    .Include(u => u.UserRoles)
    .ThenInclude(ur => ur.Role)
    .FirstOrDefaultAsync(u => u.Email == loginDto.Email);
...
var authResponseDto = _mapper.Map<AuthResponseDto>(user);
```
Aucun `.Include(u => u.Experiences)`, `.Include(u => u.Trainings)`, ni `.Include(u => u.Address)`. EF Core ne charge donc jamais ces collections de navigation pour ce chemin : `AutoMapper` les mappe systématiquement comme vides dans `UserDto.Experiences`/`UserDto.Trainings` (`DTOs/User/UserDto.cs`), **quel que soit le contenu réel en base**.

À l'inverse, `GetCurrentUserAsync` (endpoint `GET /Auth/me`, lignes ~702-709) :
```csharp
var user = await _userManager.Users
    .Include(u => u.Organization)
    .Include(u => u.UserRoles)
    .ThenInclude(ur => ur.Role)
    .Include(u => u.Address)
    .Include(u => u.Experiences)
    .Include(u => u.Trainings)
    .FirstOrDefaultAsync(u => u.Id == userId);
```
charge tout, et renvoie un `UserDto` complet.

### 4. Pourquoi le reload (F5) corrige l'affichage

`App.vue` appelle `authStore.initialize()` au montage de l'application. `router/guards/auth.ts` fait de même à chaque navigation si le store n'est pas encore initialisé :
```ts
const initialize = async () => {
  if (isInitialized.value) return;
  if (authService.isAuthenticated()) {
    await loadCurrentUser();
  }
  isInitialized.value = true;
};
```
Au F5, un nouveau contexte JS démarre : `isInitialized` repart à `false`, `initialize()` s'exécute pour de vrai, détecte le token en storage, appelle `loadCurrentUser()` → `GET /Auth/me` → réponse complète (avec `Includes`) → `authStore.user` est remplacé par la version complète. C'est ce mécanisme, et uniquement lui, qui fait apparaître les données après reload.

Dans le flux SPA normal (login → redirection interne sans reload), `initialize()` n'est jamais rappelé (`isInitialized` est déjà `true` depuis le montage initial de l'app), et `login()` ne rappelle jamais `/me` : `authStore.user` reste bloqué sur la version incomplète issue de `POST /Auth/login` jusqu'au prochain reload.

### 5. Vérification du flux de refresh-token (pattern potentiellement similaire)

`Services/AuthenticationService.cs`, `RefreshTokenAsync` (lignes ~452-454) présente le même défaut :
```csharp
var user = await _userManager.Users
    .Include(u => u.UserRoles)
    .ThenInclude(ur => ur.Role)
    .FirstOrDefaultAsync(u => u.Email == refreshTokenDto.Email);
```
Pas d'`Include` sur `Address`/`Experiences`/`Trainings` non plus. Côté frontend, `candidate-app/src/services/BaseClient.ts` (`performJwtRefresh`) n'exploite aujourd'hui que `accessToken`, `refreshToken` et `expiresIn` de la réponse de `POST /api/auth/refresh` — le champ `user` de cette réponse n'est jamais appliqué à `authStore`. Le défaut est donc **présent mais inerte** aujourd'hui côté `candidate-app` : il ne produit aucun symptôme observable tant que le frontend n'exploite pas ce champ. Corrigé par cohérence de contrat (cf. périmètre ci-dessous), pas parce qu'il cause un bug visible actuellement.

## Root cause

`POST /Auth/login` (et `POST /api/auth/refresh`) renvoient un `UserDto` structurellement identique à celui de `GET /Auth/me`, mais dont le contenu réel diverge : la requête EF Core sous-jacente à `LoginAsync`/`RefreshTokenAsync` ne charge pas les collections `Address`/`Experiences`/`Trainings`, alors que celle de `GetCurrentUserAsync` les charge. Le frontend, qui pose `authStore.user` directement depuis la réponse de login sans jamais la recompléter via `/me` avant le prochain reload, expose cette incohérence de contrat à l'utilisateur sous forme de profil visuellement incomplet jusqu'au reload.

## Décisions structurantes (confirmées par le coordinateur du cycle spec → dev → validation)

1. **Correctif à la source, côté backend uniquement** : aligner `LoginAsync` sur `GetCurrentUserAsync` en ajoutant les mêmes `.Include()`. Pas de correctif frontend redondant (pas d'appel réseau supplémentaire type `loadCurrentUser()` déclenché après login ou au montage de `ProfilePage.vue`) : une fois la réponse de `/login` complète, `ProfilePage.vue` et `authStore.ts` fonctionnent tels qu'ils sont écrits aujourd'hui, sans aucune modification de code frontend.
2. **Périmètre élargi par cohérence de contrat** (coût mécanique faible, alignement de toutes les méthodes qui retournent un `AuthResponseDto`/`UserDto`) :
   - `RefreshTokenAsync` : ajouter le même `.Include(u => u.Address).Include(u => u.Experiences).Include(u => u.Trainings)`, même si `candidate-app` n'exploite pas actuellement le champ `user` de cette réponse (bug latent, corrigé pour éviter qu'il ne resurgisse si un frontend consomme un jour ce champ).
   - `RegisterCandidateAsync` : ne pas présumer du comportement. `Experiences`/`Trainings` y sont créées via des appels séparés à `_experienceService.CreateExperienceAsync(...)`/`_trainingService.CreateTrainingAsync(...)` sur le même `DbContext` que l'utilisateur en cours de création — le mécanisme de "relationship fixup" d'EF Core peut, ou non, peupler automatiquement les collections en mémoire de l'entité `user` déjà trackée, sans qu'un `.Include()` explicite soit nécessaire (ambigu sans exécution réelle du code). **Le developer doit vérifier empiriquement par un test** (ex. test d'intégration/service existant type `AuthenticationServiceTests`, en inspectant `authResponseDto.User.Experiences`/`.Trainings` juste après l'appel à `RegisterCandidateAsync` avec au moins une expérience et une formation dans la requête) si ces collections sont bien peuplées au moment du mapping vers `AuthResponseDto`. Si le test montre qu'elles sont vides, appliquer le même correctif défensif qu'aux points 1 et 2 (recharger l'utilisateur avec les `Include` nécessaires avant le mapping final, ou tout mécanisme équivalent garantissant que `authResponseDto.User.Experiences`/`.Trainings` reflètent ce qui vient d'être inséré). Documenter le résultat de cette vérification dans la description de la PR/commit correspondant.
3. **Emplacement de la spec** : cette spec vit dans `MonolithApi/.claude/specifications/` (cause racine et correctif principal backend), avec une section "Coordination frontend — candidate-app" ci-dessous, et une référence courte ajoutée à `candidate-app/CLAUDE.md` (suit le pattern déjà établi par `secure-cv-download.md` et `candidate-registration-experience-description-error.md`).

## Objectif et périmètre

- Garantir que `POST /Auth/login` renvoie un `UserDto` (`AuthResponseDto.User`) contenant les mêmes données que `GET /Auth/me` pour un même utilisateur au même instant : `Address`, `Experiences`, `Trainings` complets et à jour, pas seulement `Organization`/`UserRoles`.
- Étendre le même correctif à `POST /api/auth/refresh` (`RefreshTokenAsync`), par cohérence de contrat.
- Vérifier (empiriquement, via test) et corriger si besoin le même défaut potentiel dans `RegisterCandidateAsync`.
- **Hors périmètre** : toute modification de `candidate-app` (aucune prévue — la section "Coordination frontend" ci-dessous documente pourquoi, elle ne prescrit aucun changement de code). Toute modification de `recruiter-app` (non concerné, ce flux d'authentification JWT candidat lui est propre). Le mécanisme d'authentification Entra ID (B2B) n'est pas concerné : il ne passe pas par ce chemin de code (`ShouldUseEntraId` redirige avant d'atteindre la requête `_userManager.Users...FirstOrDefaultAsync` visée).

## Acteurs et permissions

- **Candidat authentifié via JWT local** : seul acteur concerné. Aucun changement de permission, de policy d'autorisation, ni de rôle. Le correctif ne change que la complétude des données renvoyées par des endpoints déjà accessibles à l'utilisateur pour ses propres données.

## Règles métier et cas limites

- Un utilisateur sans aucune expérience/formation renseignée doit continuer à recevoir `Experiences: []`/`Trainings: []` (liste vide, pas `null`, cohérent avec le comportement actuel de `GetCurrentUserAsync`/`UserDto`) — non-régression, pas une erreur.
- Un utilisateur organisationnel (recruteur/admin, `OrganizationId` non nul) n'a pas d'`Experiences`/`Trainings` significatives dans son usage métier habituel, mais l'`Include` supplémentaire ne doit pas provoquer d'erreur ni de comportement différent pour ce type d'utilisateur — c'est un `Include` EF Core standard sur une collection potentiellement vide, sans branchement conditionnel selon le type d'utilisateur.
- Le correctif ne doit pas changer la structure du contrat JSON (`UserDto`) : mêmes noms de champs, mêmes types, uniquement le contenu qui devient correct pour `Experiences`/`Trainings`/`Address`.
- Cas `RefreshTokenAsync` : le correctif s'applique même si l'utilisateur n'a pas modifié son profil entre le login initial et le refresh — l'objectif est la cohérence de contrat, pas une invalidation de cache (il n'y a pas de cache applicatif ici, juste une requête EF Core incomplète).

## Contrat d'interface — Backend (`MonolithApi`)

### `Services/AuthenticationService.cs`, `LoginAsync`

Remplacer :
```csharp
var user = await _userManager.Users
    .Include(u => u.Organization)
    .Include(u => u.UserRoles)
    .ThenInclude(ur => ur.Role)
    .FirstOrDefaultAsync(u => u.Email == loginDto.Email);
```
par :
```csharp
var user = await _userManager.Users
    .Include(u => u.Organization)
    .Include(u => u.UserRoles)
    .ThenInclude(ur => ur.Role)
    .Include(u => u.Address)
    .Include(u => u.Experiences)
    .Include(u => u.Trainings)
    .FirstOrDefaultAsync(u => u.Email == loginDto.Email);
```
(mêmes `Include` que `GetCurrentUserAsync`, à la ligne 702-709 du même fichier — s'aligner explicitement sur ce bloc existant plutôt que d'en réinventer un autre).

### `Services/AuthenticationService.cs`, `RefreshTokenAsync`

Même modification sur la requête ligne ~452-454 :
```csharp
var user = await _userManager.Users
    .Include(u => u.UserRoles)
    .ThenInclude(ur => ur.Role)
    .Include(u => u.Address)
    .Include(u => u.Experiences)
    .Include(u => u.Trainings)
    .FirstOrDefaultAsync(u => u.Email == refreshTokenDto.Email);
```
Note : `Organization` n'était pas inclus dans la version actuelle de `RefreshTokenAsync` (contrairement à `LoginAsync`) — ne pas l'ajouter dans le cadre de cette spec (hors périmètre, pas signalé, ne pas élargir davantage sans confirmation) ; se limiter à `Address`/`Experiences`/`Trainings` qui sont l'objet exact du bug traité ici.

### `Services/AuthenticationService.cs`, `RegisterCandidateAsync`

Aucune modification de code prescrite a priori. Le developer doit :
1. Écrire ou étendre un test (ex. dans `AuthenticationServiceTests.cs`) qui appelle `RegisterCandidateAsync` avec un `RegisterCandidateDto` contenant au moins une entrée dans `Experiences` et une dans `Trainings`.
2. Inspecter `result.Data?.User?.Experiences` et `result.Data?.User?.Trainings` dans la réponse retournée (pas une relecture en base après coup : c'est bien l'objet renvoyé par l'appel HTTP/service qui doit être vérifié, puisque c'est lui que `candidate-app` consommerait si `registerCandidate()` posait `authStore.user` avec ces données, comme `login()` le fait).
3. Si ces collections sont vides alors que la requête en contenait : appliquer un correctif équivalent (ex. recharger l'entité `user` avec les `Include` nécessaires juste avant `_mapper.Map<AuthResponseDto>(user)`, à la fin de la méthode, après le `SaveChangesAsync`/`CommitAsync`).
4. Si ces collections sont déjà correctement peuplées (fixup EF Core effectif) : ne rien changer, et le documenter (ex. commentaire bref dans le test, ou note dans la description du commit) pour que ce ne soit pas réinvestigué plus tard sans raison.

## Coordination frontend — `candidate-app`

Aucune modification de code attendue dans `candidate-app` pour fermer ce ticket. Une fois `POST /Auth/login` complet :
- `authStore.ts`, `setAuth()` pose déjà `user.value = authResponse.data.user` avec la donnée complète, sans changement de code.
- `ProfilePage.vue` lit déjà `user?.experiences || []` et `user?.trainings || []` de façon réactive (`computed` sur `authStore.user`), sans changement de code.

Le comportement observé (expériences/formations visibles immédiatement après connexion, sans reload) découle mécaniquement du correctif backend, sans aucune intervention frontend. Ce point est documenté ici explicitement pour éviter qu'un futur correctif frontend redondant (ex. rappel de `loadCurrentUser()` après login) ne soit ajouté sans nécessité.

Référence ajoutée dans `candidate-app/CLAUDE.md` (section Documentation), pointant vers cette spec, à titre informatif uniquement — aucune action de code associée côté `candidate-app`.

## À vérifier par le developer (résumé)

- `RegisterCandidateAsync` : comportement du fixup EF Core sur `user.Experiences`/`user.Trainings` — cf. section dédiée ci-dessus, à trancher par un test avant de décider s'il faut un correctif.

## Critères d'acceptation

1. Un candidat ayant au moins une expérience et une formation enregistrées se connecte via `POST /Auth/login` (test d'intégration ou requête HTTP manuelle) → la réponse contient `data.user.experiences` et `data.user.trainings` non vides, identiques (mêmes entrées) à ce que renvoie `GET /Auth/me` pour le même utilisateur juste après.
2. Test manuel bout en bout (`candidate-app`) : se connecter avec un compte candidat ayant des expériences/formations existantes, être redirigé vers `/` (comportement actuel de `LoginPage.vue`), naviguer vers `/profile` sans recharger la page (navigation interne du routeur, pas de F5) → les sections "Expériences professionnelles" et "Formations" affichent les données immédiatement, sans action supplémentaire de l'utilisateur.
3. Même test, en effectuant cette fois un F5 sur `/profile` après la navigation initiale → aucune régression, les données restent affichées (comportement déjà correct aujourd'hui, à ne pas casser).
4. Un candidat sans aucune expérience ni formation se connecte → `data.user.experiences` et `data.user.trainings` valent `[]` (pas `null`, pas d'erreur), et `ProfilePage.vue` affiche les messages vides existants ("Aucune compétence renseignée" / timeline vide) sans erreur JS console — non-régression.
5. `POST /api/auth/refresh` avec un refresh token valide pour un utilisateur ayant des expériences/formations → la réponse contient `data.user.experiences`/`data.user.trainings` complets (même test que le point 1, appliqué à l'endpoint refresh).
6. Test (unitaire ou d'intégration) sur `RegisterCandidateAsync` : appel avec au moins une expérience et une formation dans la requête → `result.Data.User.Experiences`/`.Trainings` dans la réponse retournée par l'appel sont non vides et correspondent aux données soumises (que ce soit grâce au fixup EF Core existant ou à un correctif ajouté suite à la vérification décrite plus haut).
7. Aucune régression sur les endpoints/flux non concernés : un utilisateur organisationnel (recruteur) se connectant via ce même chemin JWT local (si applicable) ne provoque pas d'erreur liée aux nouveaux `Include` sur `Experiences`/`Trainings` (collections vides attendues et acceptées pour ce profil).
8. Suite de tests existante (`dotnet test` depuis `XpertSphere.MonolithApi.Tests`) : aucun test préexistant sur `LoginAsync`/`RefreshTokenAsync`/`RegisterCandidateAsync` ne casse suite à l'ajout des `Include` (à faire tourner avant de considérer le correctif terminé).
