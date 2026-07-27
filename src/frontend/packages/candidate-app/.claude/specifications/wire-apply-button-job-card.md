# Câblage du bouton "Candidater" sur `JobCard` (page liste d'offres)

## Contexte / constat

Sur `JobListingsPage.vue` (page d'accueil `/`, liste des offres), chaque offre est affichée via `JobCard.vue`. Ce composant émet déjà un événement `apply` correctement typé :

```html
<!-- JobCard.vue -->
<q-btn v-if="!hasApplied" ... @click.stop="$emit('apply', job.id)">
  <q-tooltip>Candidater</q-tooltip>
</q-btn>
```

Mais son seul point d'usage, `JobListingsPage.vue`, ne câble pas de listener `@apply` :

```html
<job-card :job="job" @view="viewJobDetails" @click="viewJobDetails(job.id)" />
```

Le clic sur l'icône "Candidater" (`send`) est donc actuellement sans aucun effet. Le seul chemin de candidature fonctionnel aujourd'hui passe par "voir les détails" → `JobDetailsPage.vue` → ouverture de `ApplicationDialog.vue`.

Par ailleurs, `JobCard` accepte une prop `hasApplied?: boolean` (défaut `false`) qui, si vraie, remplace le bouton "Candidater" par un chip "Candidaté" — mais `JobListingsPage.vue` ne la renseigne jamais : elle vaut donc toujours `false`, y compris pour une offre à laquelle le candidat a déjà postulé.

## Décision produit (déjà tranchée par l'utilisateur)

Le clic sur "Candidater" depuis la card doit ouvrir directement `ApplicationDialog.vue` (le même composant que celui utilisé depuis `JobDetailsPage.vue`), **sans navigation vers la page de détail**.

## Ce qui existe déjà et est réutilisé tel quel

- `ApplicationDialog.vue` : props `modelValue: boolean`, `job: JobOfferDto | null` ; émet `update:modelValue` et `submitted`. Aucune modification de ce composant n'est nécessaire.
- `JobOfferDto` (le type de `job.ts`) est identique en forme pour la liste (`jobOffers` du `jobOfferStore`, alimenté par `GET /JobOffers/paginated`) et pour le détail (`currentJobOffer`, alimenté par `GET /JobOffers/{id}`). **`JobListingsPage.vue` a donc déjà l'objet `JobOfferDto` complet en mémoire pour chaque `job` du `v-for`** : aucun refetch n'est nécessaire pour ouvrir le dialogue (contrairement à `JobDetailsPage.vue`, qui doit fetch avant d'afficher quoi que ce soit).
- Pattern de garde "candidat non authentifié" déjà établi dans `router/guards/auth.ts` : redirection vers `/login` avec `query: { redirect: to.fullPath }`, et `LoginPage.vue` (`handleLogin`) lit déjà `router.currentRoute.value.query.redirect` pour rediriger après connexion réussie. Ce mécanisme est réutilisé tel quel (pas de nouveau code de redirection à inventer).
- Blocage des candidatures en doublon déjà assuré côté backend : `ApplicationService.CreateApplicationAsync` (MonolithApi) renvoie `ServiceResult.Conflict("Vous avez déjà postulé à cette offre d'emploi")` (HTTP 409) si une candidature existe déjà pour le couple candidat/offre. Le message remonte correctement jusqu'au frontend : `BaseClient.post` retourne `error.response.data` tel quel sur une erreur Axios, donc `applicationStore.applyToJob` reçoit `isSuccess: false` et le message français exact, affiché via `showErrorNotification`. **Ce garde-fou serveur reste actif quoi qu'il arrive** ; ce qui suit est une amélioration UX pour éviter à un candidat déjà postulé de rouvrir inutilement le dialogue, pas une nécessité de sécurité.

## Comportement cible

### 1. Candidat authentifié, n'ayant pas encore postulé à cette offre

Clic sur "Candidater" (icône `send`) dans `JobCard` → `JobListingsPage.vue` reçoit l'événement `apply` avec le `jobId` → retrouve l'objet `job` complet correspondant dans `jobOffers` (le tableau du store, déjà en mémoire) → ouvre `ApplicationDialog` avec ce `job` (`v-model="showApplicationDialog"`, `:job="selectedJob"`).

À la soumission réussie (événement `submitted` de `ApplicationDialog`) : fermer le dialogue (déjà géré en interne par `ApplicationDialog.closeDialog()`) et marquer localement cette offre comme "déjà postulée" (voir section suivante) sans recharger toute la liste ni refetch `fetchMyApplications`.

### 2. Candidat authentifié, ayant déjà postulé à cette offre

Le bouton "Candidater" doit être remplacé par le chip "Candidaté" (comportement déjà codé dans `JobCard` via `v-if="!hasApplied"` / `v-else`), donc le clic ne peut plus se produire. Ceci nécessite que `JobListingsPage.vue` calcule et transmette effectivement la prop `hasApplied` à chaque `JobCard`, ce qu'elle ne fait pas aujourd'hui.

**Mécanisme retenu** (repris de l'avis exploré, à privilégier par rapport à N appels `check-applied` un par offre affichée) :

- Au montage de `JobListingsPage.vue`, uniquement si `authStore.isAuthenticated` est vrai, appeler `applicationStore.fetchMyApplications()` puis construire un `Set<string>` des `jobOfferId` déjà postulés (ex. `appliedJobOfferIds`), dérivé en `computed` de `applicationStore.applications` (donc en lecture seule, jamais assigné directement).
- Ne **pas** utiliser `applicationService.hasAppliedToJob` (endpoint `check-applied/job-offer/{id}`) en boucle sur la liste : un seul appel `GET /Applications/my` suffit et est déjà exploité par ailleurs (`MyApplicationsPage.vue`).
- Passer `:has-applied="appliedJobOfferIds.has(job.id)"` à chaque `<job-card>`.
- Ne pas appeler `fetchMyApplications()` si le candidat n'est pas authentifié (route publique, appel non pertinent et potentiellement rejeté par le backend).
- À la soumission réussie d'une candidature (événement `submitted`), **aucune mise à jour manuelle de `appliedJobOfferIds` n'est nécessaire** : `applicationStore.applyToJob` fait déjà `applications.value.unshift(newApplication.data!)` en cas de succès (comportement existant, inchangé), ce qui met à jour `applicationStore.applications` et donc, par réactivité, le `computed appliedJobOfferIds` et le chip "Candidaté" affiché — sans refetch complet ni état local dupliqué. `handleApplicationSubmitted` (déclenché par l'événement `submitted`) se limite donc à fermer le dialogue et réinitialiser `selectedJobForApplication` à `null`.

Remarque de cohérence transverse : `applicationStore.fetchMyApplications()` affiche aujourd'hui une notification de succès ("Candidatures récupérées avec succès") à chaque appel — ce comportement est traité par la spec sœur `prune-non-actionable-notifications.md` (le chargement silencieux de `hasApplied` en tâche de fond ne doit pas déclencher de toast visible). Les deux specs sont indépendantes à développer mais ce point est à garder en tête si elles sont livrées dans un ordre différent : tant que `prune-non-actionable-notifications` n'est pas implémentée, un toast "Candidatures récupérées avec succès" apparaîtra silencieusement au chargement de la page liste (comportement dégradé mais non bloquant).

### 3. Candidat non authentifié

`JobListingsPage.vue` est une route publique (pas de `meta: { requiresAuth: true }`), donc un visiteur non connecté peut cliquer sur "Candidater". `ApplicationDialog.vue` ne fait aucune vérification d'authentification et un envoi échouerait côté backend (401), avec un message générique peu clair ("Une erreur inattendue est survenue").

**Comportement retenu** : si `!authStore.isAuthenticated`, le clic sur "Candidater" ne doit pas ouvrir `ApplicationDialog` mais rediriger vers la page de connexion avec retour prévu vers la page courante, en réutilisant le mécanisme déjà en place dans `router/guards/auth.ts` :

```ts
router.push({ path: '/login', query: { redirect: router.currentRoute.value.fullPath } });
```

Ce choix est cohérent avec le pattern déjà utilisé pour les routes protégées (`MyApplications`, `ApplicationDetails`, `Profile`) et évite de dupliquer l'UI de choix "Se connecter / Créer un compte" déjà présente sur `JobDetailsPage.vue` dans une simple card de liste. Après connexion réussie, `LoginPage.handleLogin` redirigera automatiquement vers `redirect` (`/`), sans réouverture automatique du dialogue (acceptable : le candidat reclique sur "Candidater" une fois revenu sur la liste).

## Modifications attendues

### `src/pages/JobListingsPage.vue`

- Ajouter l'état `showApplicationDialog: ref(false)` et `selectedJobForApplication: ref<JobOfferDto | null>(null)`.
- Ajouter un computed `appliedJobOfferIds` (Set) dérivé de `applicationStore.applications`, alimenté par `fetchMyApplications()` au montage si authentifié (import `useAuthStore`, `useApplicationStore`, `storeToRefs`).
- Ajouter une méthode `handleApply(jobId: string)` :
  - si non authentifié → redirection `/login?redirect=...` (voir ci-dessus) ;
  - sinon → retrouver `job` dans `jobOffers.value` par `id`, l'assigner à `selectedJobForApplication`, ouvrir `showApplicationDialog = true`.
- Câbler `@apply="handleApply"` sur `<job-card>`.
- Câbler `:has-applied="appliedJobOfferIds.has(job.id)"` sur `<job-card>`.
- Instancier `<application-dialog v-model="showApplicationDialog" :job="selectedJobForApplication" @submitted="handleApplicationSubmitted" />` dans le template (à côté du `q-page`, comme dans `JobDetailsPage.vue`).
- `handleApplicationSubmitted` : réinitialise `selectedJobForApplication` à `null` (la fermeture du dialogue est déjà gérée par `ApplicationDialog` lui-même via son propre `closeDialog` ; le chip "Candidaté" apparaît automatiquement par réactivité, voir section précédente — aucune mutation manuelle de `appliedJobOfferIds` à écrire ici, ce n'est pas une ref mais un `computed`).

### `src/components/JobCard.vue`

Aucune modification requise : les props (`hasApplied`), emit (`apply`) et le template existant sont déjà corrects et suffisants pour ce comportement.

## Hors périmètre

- Toute modification de `ApplicationDialog.vue` ou de `applicationStore.ts` (le flux de soumission est déjà fonctionnel et inchangé).
- Le contenu et l'UX de `JobDetailsPage.vue` (traité par la spec `job-details-page-redesign.md`).
- Le principe de notifications de succès/erreur (traité par la spec `prune-non-actionable-notifications.md`).

## Critères d'acceptation

1. Sur `/` (liste d'offres), avec un candidat authentifié n'ayant pas postulé à l'offre affichée : cliquer sur l'icône "Candidater" d'une `JobCard` ouvre `ApplicationDialog` pré-rempli avec les informations de cette offre (titre, organisation, localisation, mode de travail), sans navigation vers `/jobs/:id`.
2. La soumission du formulaire dans ce dialogue crée effectivement la candidature (comportement `applicationStore.applyToJob` inchangé) et ferme le dialogue.
3. Après soumission réussie, la `JobCard` correspondante affiche immédiatement le chip "Candidaté" à la place du bouton "Candidater", sans rechargement de page ni navigation.
4. Sur `/`, avec un candidat authentifié ayant déjà postulé à une offre (candidature existante en base au chargement de la page), la `JobCard` de cette offre affiche directement le chip "Candidaté" (pas le bouton "Candidater") dès le chargement initial de la liste.
5. Sur `/`, avec un visiteur non authentifié, cliquer sur "Candidater" d'une `JobCard` redirige vers `/login?redirect=/` (ou l'URL courante avec ses filtres/pagination le cas échéant) sans jamais ouvrir `ApplicationDialog`. `ApplicationDialog` n'est à aucun moment monté/ouvert pour un utilisateur non authentifié depuis cette page.
6. Après connexion réussie depuis cette redirection, l'utilisateur est ramené sur la page liste d'offres (comportement déjà géré par `LoginPage.handleLogin`, non modifié).
7. Le bouton "Voir les détails" (icône `visibility`) et le clic sur le corps de la card conservent leur comportement actuel (navigation vers `/jobs/:id`), non affectés par ce correctif.
8. `JobDetailsPage.vue` et son propre flux de candidature (dialogue ouvert depuis la page détail) restent inchangés et continuent de fonctionner à l'identique.
9. Aucune régression sur les filtres/recherche/pagination de `JobListingsPage.vue` (le `handleApply` ne doit pas interférer avec `currentFilter`/`currentPage`).

## Points à confirmer

Aucun. Toutes les décisions nécessaires s'appuient sur des mécanismes déjà existants et vérifiés dans le code (redirection `/login?redirect=` déjà utilisée par `router/guards/auth.ts` et déjà consommée par `LoginPage.handleLogin`, `JobOfferDto` déjà en mémoire côté liste, garde-fou serveur anti-doublon déjà en place côté `ApplicationService.CreateApplicationAsync`). Le comportement "candidat non authentifié → redirection vers `/login`" décrit en section 3 est la décision retenue, pas une option ouverte.
