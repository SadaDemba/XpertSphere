# Correctifs UX/visuels indépendants avant démo (« polish »)

## Objectif et périmètre

Un audit de code a relevé six petits défauts UX/visuels indépendants dans
`recruiter-app`, à corriger avant une démo prochaine. Chaque point est
**indépendant, à faible risque, et vérifiable isolément** : le développeur
peut les traiter dans l'ordre qu'il souhaite, dans un seul développement
regroupé (pas de dépendance entre les points), mais **chaque critère
d'acceptation ci-dessous doit être vérifié individuellement** par le
`validator`.

Hors périmètre explicite : aucune nouvelle fonctionnalité, aucun changement
de design system, aucune modification du contrat backend. Un point (le
n°6) a été volontairement traité de façon minimale (voir décision ci-dessous)
plutôt que de corriger le comportement fonctionnel sous-jacent — ce choix est
documenté pour ne pas être confondu avec un oubli.

Acteurs concernés : tous les utilisateurs internes de `recruiter-app`
(rôles `Organization.*` et `XpertSphere.SuperAdmin`/`XpertSphere.Admin`) —
aucun de ces correctifs ne change les permissions existantes.

---

## Point 1 — Bannière d'erreur manquante sur la page Candidatures

### Constat

`src/pages/applications/ApplicationsPage.vue` ne rend aucun message si
`applicationStore.hasError` est vrai (le store expose bien `hasError`,
`errorMessage` et `clearError`, voir `src/stores/applicationStore.ts` lignes
63-67 et 720-724 — même contrat que `jobOfferStore`). En cas d'échec de
chargement, l'utilisateur voit un tableau vide sans aucune explication.

`src/pages/jobs/JobsPage.vue` (lignes 27-36) traite déjà ce cas avec un
`q-banner` :

```html
<q-banner v-if="jobOfferStore.hasError" class="bg-negative text-white q-mb-md">
  <template #avatar>
    <q-icon name="error" />
  </template>
  {{ jobOfferStore.errorMessage }}
  <template #action>
    <q-btn flat label="Réessayer" @click="loadJobs" />
    <q-btn flat icon="close" @click="jobOfferStore.clearError" />
  </template>
</q-banner>
```

### Règle métier / correctif attendu

Reproduire ce pattern à l'identique dans `ApplicationsPage.vue`, en
l'insérant dans le `q-card-section` (lignes 11-193), entre la fin de la
ligne de filtres/recherche (fin de la `div.row` qui se termine ligne 53) et
le `q-table` (qui démarre ligne 55) :

- Condition d'affichage : `applicationStore.hasError`.
- Message : `applicationStore.errorMessage`.
- Bouton « Réessayer » : appelle `refreshData()` (fonction déjà existante,
  lignes 349-351, équivalent local de `loadJobs()`).
- Bouton de fermeture (icône `close`) : appelle `applicationStore.clearError`.
- Même classe CSS que `JobsPage.vue` (`bg-negative text-white q-mb-md`), même
  icône (`error`).

### Critères d'acceptation

- Quand `applicationStore.fetchPaginatedApplications` échoue (ex. API
  indisponible), un bandeau rouge apparaît au-dessus du tableau avec le
  message d'erreur, un bouton « Réessayer » et un bouton de fermeture.
- Cliquer sur « Réessayer » relance le chargement (`refreshData`) sans
  recharger la page.
- Cliquer sur le bouton de fermeture masque le bandeau (`clearError` remet
  `hasError` à `false`) sans annuler les filtres en cours.
- Aucune régression sur le comportement existant en l'absence d'erreur (le
  bandeau ne doit jamais apparaître si `hasError` est `false`).

---

## Point 2 — Recherche incohérente entre les champs de filtre des offres

### Constat

`src/components/jobs/JobFilters.vue` :

- Champ « Titre » (`q-input` lignes 6-29) : un `watch` dédié (lignes
  173-184) déclenche `emitSearch()` à chaque frappe dès que la valeur atteint
  3 caractères — **sans debounce**, donc un appel réseau par caractère
  au-delà du 3ᵉ (via `handleSearch` → `loadJobs` dans `JobsPage.vue`).
- Champ « Localisation » (`q-input` lignes 32-46) : aucun `watch`, la
  recherche ne se déclenche qu'à l'appui sur `Entrée`
  (`@keydown.enter="emitSearch"`, ligne 40).

Les deux champs ont donc des comportements différents et le champ « Titre »
génère une charge réseau excessive.

### Règle métier / correctif attendu

Harmoniser les deux champs sur un comportement unique :

1. **Debounce de 400ms** appliqué à la saisie sur les deux champs (Titre et
   Localisation) : après 400ms sans nouvelle frappe, une recherche est
   déclenchée automatiquement.
2. **Seuil de 3 caractères conservé** pour le déclenchement automatique par
   debounce, sur les deux champs (pas de recherche auto tant que la valeur a
   entre 1 et 2 caractères) — comportement actuel du champ Titre, étendu à
   Localisation.
3. **Effacement du champ** (valeur vidée, longueur 0) déclenche une
   recherche immédiate (reset des résultats), sans attendre le debounce —
   comportement actuel du champ Titre (lignes 179-182), étendu à
   Localisation.
4. **Appui sur `Entrée`** continue de déclencher une recherche immédiate sur
   les deux champs, en plus du debounce (comportement actuel conservé sur
   Localisation, étendu à Titre pour cohérence) — et doit annuler tout
   timer de debounce en attente pour éviter un double appel réseau
   rapproché.
5. Le clic sur le bouton `close` (icône `x` du champ Titre, ligne 19-27,
   propriété `clearable` du champ Localisation) reste inchangé dans son
   effet (vide le champ), et déclenche la même logique de recherche
   immédiate que le point 3 ci-dessus.

Aucune dépendance externe n'est disponible dans ce package pour un debounce
« prêt à l'emploo » applicable à un `q-input` texte libre (`lodash` n'est
pas une dépendance du package ; `input-debounce` de Quasar, utilisé ailleurs
— `UserRoleAssignment.vue` ligne 18 — s'applique uniquement au filtrage
d'options d'un `q-select`, pas à un `q-input`). Le développeur implémente un
debounce local (ex. `setTimeout`/`clearTimeout` dans un `watch`, ou un
composable dédié réutilisable) — le mécanisme précis est laissé à sa
discrétion, seul le comportement observable ci-dessus est contractuel.

### Critères d'acceptation

- Taper rapidement 5 caractères dans le champ Titre ne déclenche **aucun**
  appel réseau avant 400ms d'inactivité, puis un seul appel une fois le seuil
  de 3 caractères atteint et le debounce écoulé.
- Même comportement observé sur le champ Localisation.
- Vider complètement l'un des deux champs déclenche une recherche
  immédiate (sans attendre 400ms).
- Appuyer sur `Entrée` dans l'un des deux champs déclenche une recherche
  immédiate, y compris si un debounce est en attente (pas de double appel
  réseau rapproché).
- Le bouton « Effacer tous les filtres » (`clearAllFilters`, lignes 195-202)
  continue de fonctionner sans changement.

---

## Point 3 — Pages « à venir » accessibles depuis le menu et le logo

### Constat

`DashboardPage.vue`, `InterviewsPage.vue` et `ReportsPage.vue` sont de
simples pages d'attente : icône, titre, message _"Cette fonctionnalité sera
bientôt disponible"_, et une notification Quasar identique déclenchée au
montage (`onMounted`, ex. `InterviewsPage.vue` lignes 34-41). Elles sont
référencées :

- Dans `src/components/AppNavigation.vue`, tableau `navigationItems`
  (lignes 142-185) : entrées `dashboard` (route `/`, lignes 143-149),
  `interviews` (route `/interviews`, lignes 171-177) et `reports` (route
  `/reports`, lignes 178-184).
- Dans `src/router/routes.ts` : route racine `''` → `DashboardPage.vue`
  (lignes 39-44), route `interviews` → `InterviewsPage.vue` (lignes 87-92),
  route `reports` → `ReportsPage.vue` (lignes 93-98).

Un utilisateur qui clique sur ces entrées de menu en démo atterrit sur une
page vide avec un toast « bientôt disponible ».

**Point vérifié en complément** : le lien du logo/nom de marque dans l'en-tête
(`src/components/AppHeader.vue`, ligne 20, `<router-link to="/">`) pointe
vers la route racine `/`, c'est-à-dire `DashboardPage.vue`. Masquer
uniquement l'entrée « Tableau de bord » du menu latéral ne suffit donc pas :
un clic sur le logo — réflexe naturel — atterrirait quand même sur la page
vide. En revanche, `LoginPage.vue` (lignes 156-170) ne redirige jamais vers
`/` après connexion (il redirige vers `/jobs` ou `/admin/users` selon le
rôle) : ce résidu ne concerne donc que la navigation manuelle/le clic sur le
logo, pas le flux de connexion.

### Décision (confirmée avec l'utilisateur)

- Masquer les trois entrées `dashboard`, `interviews` et `reports` du menu
  latéral (`AppNavigation.vue`).
- **En plus**, remplacer la route racine `''` de `routes.ts` par une
  redirection vers `/jobs` (au lieu de rendre `DashboardPage.vue`), pour
  couvrir le clic sur le logo.
- Les routes `interviews` et `reports` restent enregistrées et pointent
  toujours vers leurs pages d'attente respectives (accès direct par URL
  toujours possible, volontairement — seul le point d'entrée depuis le menu
  disparaît, conformément à la demande initiale de l'audit).
- `DashboardPage.vue` devient orphelin (plus aucune route ne le référencie).
  Il n'est **pas supprimé** — cohérent avec la convention déjà appliquée
  dans ce service pour des composants orphelins équivalents (voir
  `fix-experience-fields-candidate-detail-page.md`) — mais son statut
  orphelin doit être documenté dans le commit/la PR pour ne pas être pris
  pour un oubli.

### Correctif attendu — détail

**`src/components/AppNavigation.vue`** : retirer du tableau
`navigationItems` (lignes 142-185) les trois objets `dashboard`
(lignes 143-149), `interviews` (lignes 171-177) et `reports` (lignes
178-184). Ne conserver que `jobs`, `candidates` et `applications`.

**`src/router/routes.ts`** : remplacer l'entrée de route racine (lignes
39-44) :

```ts
{
  path: '',
  component: () => import('pages/DashboardPage.vue'),
  meta: { title: 'Tableau de bord' },
  beforeEnter: organizationRoleGuard,
},
```

par :

```ts
{
  path: '',
  redirect: '/jobs',
},
```

(`beforeEnter`/`component` deviennent inutiles : la protection est déjà
assurée par la garde `organizationRoleGuard` déjà présente sur la route
`/jobs` cible ; un utilisateur non autorisé pour `/jobs` sera redirigé vers
`/unauthorized` par cette garde, pas par celle de la route racine.)

Les routes `interviews` (lignes 87-92) et `reports` (lignes 93-98) restent
inchangées.

**Note sur les rôles plateforme** : `/jobs` reste protégée par
`organizationRoleGuard` (`requireOrganizationRole: true`), qui exige un rôle
parmi `OrganizationRoles` (`Organization.Admin`, `Organization.Manager`,
`Organization.Recruiter`, `Organization.TechnicalEvaluator` —
`src/models/auth.ts` lignes 106-111). Un utilisateur **uniquement**
`XpertSphere.SuperAdmin`/`XpertSphere.Admin` (rôle plateforme, sans rôle
organisation) qui navigue vers `/` — ou clique sur le logo — sera donc
redirigé vers `/unauthorized` par cette garde en atteignant `/jobs`, pas
affiché sur `/jobs` lui-même. **Ce n'est pas une régression introduite par
ce correctif** : la route racine portait déjà `beforeEnter:
organizationRoleGuard` avant ce changement (elle rendait
`DashboardPage.vue` sous la même garde), donc un utilisateur plateforme-only
était déjà redirigé vers `/unauthorized` en arrivant sur `/` — seule la
route cible de la garde change (`/jobs` au lieu de `DashboardPage.vue`), pas
le résultat pour ce profil. Ce point n'est donc pas à corriger dans le
présent correctif ; il est documenté pour que le `validator` ne le
signale pas comme une régression en testant avec un compte
`XpertSphere.SuperAdmin`/`Admin` sans rôle organisation.

### Critères d'acceptation

- Le menu latéral n'affiche plus que « Offres d'emploi », « Candidats » et
  « Candidatures » dans le menu principal (plus de « Tableau de bord », «
  Entretiens », « Rapports »).
- Pour un utilisateur possédant un rôle `Organization.*`
  (`Organization.Admin`, `Organization.Manager`, `Organization.Recruiter`,
  `Organization.TechnicalEvaluator`) : naviguer vers `/` (URL directe, ou
  clic sur le logo dans l'en-tête) redirige immédiatement vers `/jobs`, sans
  afficher `DashboardPage.vue`.
- Pour un utilisateur **uniquement** `XpertSphere.SuperAdmin`/
  `XpertSphere.Admin` (sans rôle organisation) : naviguer vers `/` aboutit
  sur `/unauthorized` (via la garde de `/jobs`) — comportement identique à
  avant ce correctif (voir note ci-dessus), pas une régression à signaler.
- Naviguer directement vers `/interviews` ou `/reports` (URL tapée à la
  main) affiche toujours la page d'attente correspondante (comportement
  inchangé, accès direct volontairement conservé).
- Le flux de connexion (`LoginPage.vue`) n'est pas impacté : il continue de
  rediriger vers `/jobs` ou `/admin/users` selon le rôle, indépendamment de
  ce correctif.
- Aucune erreur de navigation (boucle de redirection, 404) sur `/`.

---

## Point 4 — Double pagination sur la page Candidats

### Constat

`src/pages/candidates/CandidatesPage.vue` :

- Le `q-table` (lignes 50-122) est piloté en pagination serveur via
  `v-model:pagination="pagination"` et l'événement `@request="onTableRequest"`
  (lignes 282-291), avec `pagination.rowsNumber` synchronisé sur
  `userStore.totalCount` (`watch`, lignes 258-268) — il affiche donc déjà son
  propre pied de pagination fonctionnel.
- Juste en dessous, un bloc séparé (lignes 124-134) affiche **un second**
  contrôle de pagination (`q-pagination`) quand `userStore.totalPages > 1`,
  relié à la fonction `onPageChange` (lignes 302-305), qui modifie
  `pagination.value.page` et relance `loadCandidates()` indépendamment du
  cycle `@request` du tableau.

Les deux contrôles agissent sur le même état de pagination
(`pagination.value.page`) par deux chemins différents, ce qui est
redondant à l'écran et risque de déclencher un double appel réseau si
l'utilisateur interagit avec les deux contrôles à la suite.

### Choix du pattern de référence

L'audit suggérait de vérifier le pattern utilisé par `JobsPage.vue`. Après
vérification, `JobsPage.vue` **n'utilise pas** de `q-table` côté page : il
affiche des cartes/une table de présentation (`JobList.vue` → `JobTable.vue`,
sans pagination serveur intégrée) et un composant `AppPagination.vue`
externe est le seul contrôle de pagination. Ce pattern n'est pas
directement transposable à `CandidatesPage.vue`, qui repose sur un
`q-table` server-side complet (`@request`, `rowsNumber`).

Le pattern structurellement le plus proche est celui de
`ApplicationsPage.vue` (voir point 1) : un `q-table` server-side identique
(`v-model:pagination`, `@request="onTableRequest"`, `rowsNumber` synchronisé
sur `applicationStore.totalCount`), **sans** contrôle de pagination externe
additionnel — le pied de page intégré du tableau est le seul contrôle. C'est
ce pattern qui est retenu ici pour rester cohérent avec la page structurellement
la plus similaire.

### Correctif attendu

- Supprimer le bloc de pagination externe (lignes 124-134, commentaire
  `<!-- Pagination -->` inclus) dans `CandidatesPage.vue`.
- Supprimer la fonction `onPageChange` (lignes 302-305), devenue
  totalement inutilisée après la suppression ci-dessus (aucune autre
  référence dans le fichier) — à ne pas laisser en code mort (risque
  d'échec lint `no-unused-vars`/`eslint`).
- Conserver tel quel le `q-table` et son pied de pagination intégré
  (`v-model:pagination`, `@request`), déjà fonctionnel.

### Critères d'acceptation

- La page Candidats n'affiche plus qu'un seul contrôle de pagination (celui
  intégré au `q-table`).
- Changer de page via ce contrôle unique déclenche toujours
  `onTableRequest` → `loadCandidates()`, avec les bons `page`/`rowsPerPage`.
- `npm run lint` ne signale aucune fonction/variable inutilisée dans ce
  fichier après suppression de `onPageChange`.
- Aucune régression sur le tri (`sortBy`/`descending`) ni sur les filtres
  existants (recherche, statut actif, compétences).

---

## Point 5 — `console.log` résiduel dans `ApplicationDetailPage.vue`

### Constat

`src/pages/applications/ApplicationDetailPage.vue`, fonction `viewCandidate`
(lignes 463-468) :

```ts
const viewCandidate = () => {
  console.log('', application.value);
  if (application.value) {
    router.push(`/candidates/${application.value.candidateId}`);
  }
};
```

La ligne 464 est une trace de debug oubliée, sans valeur fonctionnelle.

### Correctif attendu

Supprimer uniquement la ligne `console.log('', application.value);` (ligne
464). Le reste de la fonction (navigation conditionnelle vers la fiche
candidat) reste inchangé.

### Critères d'acceptation

- `viewCandidate()` ne contient plus aucun appel `console.log`.
- Le comportement de navigation (clic sur « Voir le candidat » → navigation
  vers `/candidates/:candidateId` si `application.value` est défini) est
  strictement identique à avant.
- `grep -rn "console\.\(log\|debug\)" src/pages/applications/ApplicationDetailPage.vue`
  ne retourne plus aucune occurrence.

---

## Point 6 — Incohérence de durée par défaut sur l'expiration d'un rôle assigné

### Constat

`src/pages/admin/UsersPage.vue`, dialogue « Assigner un rôle » :

- `resetAssignRoleForm()` (lignes 917-925) pré-remplit le champ « Date
  d'expiration » (`q-input` ligne 391-397, hint _« Laisser vide pour un rôle
  permanent »_) avec **aujourd'hui + 1 an**, à l'ouverture du dialogue.
- `assignRole()` (lignes 857-880), à la soumission : si le champ a été
  vidé volontairement par l'utilisateur, applique un repli à **aujourd'hui +
  5 ans** (lignes 865-870) plutôt que de laisser le champ vide.

**Constat complémentaire (documenté pour mémoire, non traité ici par
décision explicite)** : le backend (`AssignRoleDto.cs` —
`public DateTime? ExpiresAt { get; set; }` —, `AssignRoleDtoValidator.cs`,
`UserRoleService.cs` lignes 76/233/275/295 — `ur.ExpiresAt == null ||
ur.ExpiresAt > DateTime.UtcNow`) traite explicitement `ExpiresAt == null`
comme un rôle **réellement permanent**, sans expiration. Le hint du champ
(_« Laisser vide pour un rôle permanent »_) promet donc un comportement que
le frontend ne peut jamais produire : `assignRole()` ne transmet jamais
`undefined`, il substitue toujours une date (+5 ans aujourd'hui, ou la même
valeur que le préremplissage après ce correctif). Un utilisateur qui vide
le champ en pensant obtenir un rôle permanent obtient en réalité un rôle qui
expirera silencieusement.

### Décision (confirmée avec l'utilisateur)

**Correctif minimal retenu** : aligner les deux valeurs de repli sur la
même durée, **+1 an**, sans changer le mécanisme lui-même (le champ vidé
continue de produire une date d'expiration calculée côté client, jamais
`undefined`/permanent réel). L'option consistant à honorer réellement le
hint (envoyer `undefined` quand le champ est vide, retirer tout repli
calculé) est explicitement écartée pour ce correctif.

**Cette décision signifie que le hint du champ (« Laisser vide pour un rôle
permanent ») reste inexact après ce correctif** — c'est un choix délibéré de
périmètre (démo à venir, changement de comportement fonctionnel jugé hors
scope d'un correctif « polish » à faible risque), pas un oubli. Toute
correction future de ce hint ou du comportement réel de rôle permanent
nécessite une spécification dédiée, hors périmètre du présent document.

### Correctif attendu

`src/pages/admin/UsersPage.vue`, fonction `assignRole()` (lignes 865-870) :

```ts
if (assignRoleForm.expiresAt) {
  assignData.expiresAt = assignRoleForm.expiresAt;
} else {
  const nextYear = new Date();
  nextYear.setFullYear(nextYear.getFullYear() + 5); // ← à changer en +1
  assignData.expiresAt = nextYear.toISOString().split('T')[0]!;
}
```

Remplacer `+ 5` par `+ 1`, pour que ce repli produise exactement la même date
que `resetAssignRoleForm()` (lignes 917-925, déjà à `+ 1`, inchangé).

### Critères d'acceptation

- Ouvrir le dialogue « Assigner un rôle » : le champ « Date d'expiration »
  est pré-rempli avec la date du jour + 1 an (comportement déjà existant,
  non modifié).
- Vider volontairement ce champ puis soumettre le formulaire : la requête
  envoyée au backend (`AssignRoleDto.expiresAt`) contient une date égale à
  la date du jour + 1 an — **la même valeur** que celle affichée au
  préremplissage, à la date près (pas de divergence entre +1 an affiché et
  +5 ans réellement soumis).
- Aucun autre comportement du dialogue (sélection du rôle, validation du
  champ requis, fermeture) n'est modifié.
- Le hint _« Laisser vide pour un rôle permanent »_ reste affiché tel quel
  (non modifié dans ce correctif, malgré son inexactitude documentée
  ci-dessus).

---

## Récapitulatif des fichiers modifiés

| Point | Fichier(s)                                                 |
| ----- | ---------------------------------------------------------- |
| 1     | `src/pages/applications/ApplicationsPage.vue`              |
| 2     | `src/components/jobs/JobFilters.vue`                       |
| 3     | `src/components/AppNavigation.vue`, `src/router/routes.ts` |
| 4     | `src/pages/candidates/CandidatesPage.vue`                  |
| 5     | `src/pages/applications/ApplicationDetailPage.vue`         |
| 6     | `src/pages/admin/UsersPage.vue`                            |

Aucun de ces fichiers ne se recoupe entre points : les six correctifs peuvent
être développés et relus indépendamment, y compris en parallèle si
nécessaire (pas de conflit de fichier).
