# Correction — condition de course sur `userStore.users` dans `ApplicationAssign.vue`

## Objectif et périmètre

Corriger un bug de concurrence dans le dialogue « Assigner des utilisateurs »
(`src/components/applications/ApplicationAssign.vue`), utilisé sur la page de
détail d'une candidature pour assigner un manager et/ou un évaluateur
technique. À l'ouverture du dialogue, les listes déroulantes « Manager » et
« Évaluateur technique » peuvent intermittemment se retrouver peuplées avec
les mauvais utilisateurs (rôles inversés), car les deux chargements
concurrents écrivent dans le même état partagé du store Pinia `userStore`.

Périmètre strict : correction du chargement des options de ces deux
`q-select` (`managerOptions`, `evaluatorOptions`). Aucune autre fonctionnalité
du dialogue (assignation, désassignation, affichage des utilisateurs déjà
assignés) n'est modifiée. Le comportement de `userStore.fetchPaginatedUsers`
et de tous ses autres appelants (`CandidatesPage.vue`, `UsersPage.vue`) n'est
pas modifié.

## Constat (bug confirmé par lecture du code)

Fichier : `src/frontend/packages/recruiter-app/src/components/applications/ApplicationAssign.vue`

- `loadManagers` (lignes ~174-198) et `loadEvaluators` (lignes ~200-224)
  appellent chacune `await userStore.fetchPaginatedUsers(filter)` avec un
  filtre de rôle différent (`Organization.Manager` vs
  `Organization.TechnicalEvaluator`), puis lisent ensuite
  `userStore.users` (état partagé du store) pour construire respectivement
  `managerOptions.value` et `evaluatorOptions.value`.
- Le watcher sur `props.modelValue` (lignes ~307-315) et le hook `onMounted`
  (lignes ~317-322) appellent `loadManagers()` et `loadEvaluators()` l'une
  après l'autre **sans `await`** entre les deux : les deux promesses
  s'exécutent concurremment.
- `userStore.fetchPaginatedUsers` (fichier `src/stores/userStore.ts`, lignes
  ~49-78) écrase entièrement `users.value` à chaque appel (`users.value =
response!.data`, ligne 56). Selon l'ordre de résolution réseau (non
  déterministe), la dernière réponse à s'appliquer à `users.value` peut être
  celle du manager ou celle de l'évaluateur : chaque fonction lit ensuite cet
  état déjà potentiellement écrasé par l'autre, produisant une liste erronée
  de façon intermittente et non reproductible à la demande.
- Effet de bord supplémentaire (même cause racine) : `fetchPaginatedUsers`
  déclenche une notification de succès (`notification.showSuccessNotification
('Utilisateurs chargés avec succès')`, ligne 61) à chaque appel. Avec deux
  appels concurrents à l'ouverture du dialogue, l'utilisateur voit
  potentiellement deux notifications de succès pour une simple ouverture de
  liste déroulante — signal supplémentaire que cette action de store n'est
  pas conçue pour ce cas d'usage (peuplement silencieux d'options de
  sélection).

## Recherche d'un pattern existant réutilisable (résultat : aucun trouvé)

Vérifié dans le monorepo (`recruiter-app` uniquement, seul service concerné
par ce store) :

- Aucun autre composant n'appelle deux fois `fetchPaginatedUsers` (ni aucune
  autre action `fetchPaginated*` de n'importe quel store) avec des filtres
  différents de façon concurrente. Les autres appelants
  (`CandidatesPage.vue`, `UsersPage.vue`, `JobOffersPage.vue`,
  `OrganizationsPage.vue`, `RolesPage.vue`, `JobsPage.vue`) ne font qu'un
  seul appel actif à la fois sur leur store respectif.
- Le seul autre usage de `Promise.all` avec des actions de store
  (`ApplicationDetailPage.vue`, ligne ~479 :
  `Promise.all([applicationStore.fetchApplicationById(...),
applicationStore.fetchStatusHistory(...)])`) n'est **pas** un cas
  comparable : les deux actions écrivent dans des états de store distincts
  (`currentApplication` et `statusHistory`), il n'y a donc pas de conflit
  d'écriture sur un état partagé.
- Aucun composant n'instancie un service directement (`new UserService()`
  ailleurs que dans `userStore.ts` lui-même) : la convention actuelle du
  projet est que les composants passent toujours par un store, jamais par un
  service directement.

Conclusion : il n'existe pas de pattern déjà établi ailleurs dans le
monorepo à réutiliser tel quel. La solution introduite ci-dessous doit
cependant respecter la convention observée (composant → store, jamais
composant → service).

## Décision de correctif retenue

**Ajouter une nouvelle méthode au store `userStore`, dédiée à la recherche
d'utilisateurs sans mutation de l'état partagé**, et l'utiliser dans
`ApplicationAssign.vue` à la place de `fetchPaginatedUsers`.

### Options évaluées

1. **Séquencer les deux appels avec `await`** (`await loadManagers(); await
loadEvaluators();`). Élimine la course, mais :
   - N'importe quel futur appelant concurrent de `fetchPaginatedUsers`
     réintroduirait le même bug (le problème de fond — état partagé
     mutable utilisé comme canal de retour — reste présent).
   - Ne corrige pas l'effet de bord de la notification de succès
     systématique, ni le fait que la liste des managers reste visuellement
     vide plus longtemps (chargement séquentiel au lieu de parallèle).
   - **Rejetée** : traite le symptôme, pas la cause ; retenue seulement
     comme option de repli si la solution ci-dessous s'avérait bloquante en
     développement (à documenter alors comme écart justifié).

2. **Méthode de store dédiée retournant directement les résultats, sans
   passer par l'état partagé `users`.** `userService.getPaginatedUsers`
   (méthode déjà existante, `src/services/userService.ts`, ligne 17) retourne
   déjà `PaginatedResult<UserSearchResultDto> | null` sans aucune mutation
   d'état — le store l'enveloppe simplement pour l'assigner à `users.value`.
   Il suffit d'exposer une méthode de store qui appelle ce service et
   retourne le résultat, sans écrire dans `users`/`totalCount`/`currentPage`/
   `pageSize`/`totalPages`/`loading`, et sans déclencher de notification.
   **Retenue** — voir contrat détaillé ci-dessous. Cette option respecte la
   convention « composant → store » (pas d'appel direct à `new
UserService()` depuis le composant, ce qui casserait un précédent
   100% constant dans le code actuel) tout en éliminant complètement la
   cause racine (plus aucune lecture d'un état partagé mutable après l'appel
   asynchrone).

3. **Refs locales dédiées dans le composant plutôt que lecture de l'état du
   store.** Non viable isolément : `managerOptions` et `evaluatorOptions`
   sont **déjà** des refs locales au composant (lignes 171-172) ; le bug ne
   vient pas de leur absence mais de la source qu'elles lisent
   (`userStore.users`, partagé). Cette option ne devient viable que combinée
   à l'option 2 (une source de données qui retourne le résultat directement
   au lieu de le déposer dans un état partagé) — ce n'est donc pas une
   option alternative distincte, mais une caractéristique déjà présente du
   correctif retenu.

## Contrat de la nouvelle méthode de store

Fichier : `src/frontend/packages/recruiter-app/src/stores/userStore.ts`.

- Nom proposé : `searchUsers(filter: UserFilterDto): Promise<UserSearchResultDto[]>`
  (nom au choix du développeur si un nom plus cohérent avec le style du
  fichier est préférable — le contrat ci-dessous prime sur le nom).
- **Ne doit lire ni écrire** `users`, `totalCount`, `currentPage`,
  `pageSize`, `totalPages`, `loading`, `error` : l'appel est entièrement
  local à la méthode (variable locale pour la réponse), afin qu'aucun appel
  concurrent à `searchUsers` ou à `fetchPaginatedUsers` ne puisse interférer
  avec un autre appel en cours, quel que soit l'ordre de résolution réseau.
- **Ne doit déclencher aucune notification** (ni succès, ni erreur) : cette
  méthode sert à peupler silencieusement des options de sélection, pas à
  piloter un écran de liste principal. La gestion de l'erreur (affichage,
  log) reste à la charge de l'appelant.
- Implémentation : appelle `service.getPaginatedUsers(filter)` (le
  `UserService` déjà instancié dans le store), et :
  - si `response?.isSuccess` est vrai, retourne `response.data` (un tableau,
    potentiellement vide si aucun résultat) ;
  - sinon (réponse en échec ou `null`), retourne `[]` ;
  - toute exception levée par l'appel réseau est laissée remonter (pas de
    `try/catch` interne) — c'est à l'appelant (`ApplicationAssign.vue`, qui a
    déjà un `try/catch` dans `loadManagers`/`loadEvaluators`) de l'intercepter
    et de réinitialiser ses options à `[]`, comme il le fait déjà.
- `fetchPaginatedUsers` n'est **pas modifiée** : ses 3 appelants existants
  (`CandidatesPage.vue`, `UsersPage.vue`, et tout autre usage de l'état
  paginé principal) dépendent de sa mutation d'état (`users`, pagination) et
  de ses notifications ; ce comportement doit rester inchangé.

## Modification de `ApplicationAssign.vue`

- `loadManagers` et `loadEvaluators` (lignes ~174-224) remplacent l'appel
  `await userStore.fetchPaginatedUsers(filter)` suivi d'une lecture de
  `userStore.users` par un appel direct à `const results = await
userStore.searchUsers(filter)`, puis construisent `managerOptions.value` /
  `evaluatorOptions.value` à partir de `results` (au lieu de
  `userStore.users`).
- Le `try/catch/finally` existant autour de chaque fonction est conservé tel
  quel (gestion d'erreur locale déjà correcte : `console.error` +
  réinitialisation de la liste à `[]`).
- Le watcher (lignes ~307-315) et `onMounted` (lignes ~317-322) restent
  inchangés dans leur structure (deux appels non attendus l'un après
  l'autre, `loadManagers(); loadEvaluators();`) : puisque chaque fonction
  n'écrit plus que dans sa propre ref locale (`managerOptions` /
  `evaluatorOptions`) sans jamais lire ou écrire un état partagé entre les
  deux, l'absence de séquencement explicite n'est plus une condition de
  course — les deux traitements peuvent légitimement rester concurrents pour
  ne pas dégrader le temps de chargement perçu.

## Règles métier et cas limites

- Un utilisateur ne doit jamais voir, dans le sélecteur « Manager », un
  utilisateur qui n'a pas le rôle `Organization.Manager` pour son
  organisation (et symétriquement pour « Évaluateur technique » /
  `Organization.TechnicalEvaluator`), y compris en cas d'ouverture/fermeture
  rapide et répétée du dialogue (ex. l'utilisateur ouvre le dialogue,
  referme, rouvre avant la résolution des appels précédents — cas non
  couvert explicitement par la spec actuelle mais qui ne doit pas non plus
  provoquer de mélange de rôles, dans la mesure où chaque nouvel appel à
  `searchUsers` reste indépendant et n'écrit que dans la ref locale du
  composant qui l'a déclenché).
- Si l'API échoue pour l'un des deux chargements (ex. timeout réseau sur le
  filtre manager), l'autre chargement doit aboutir normalement sans être
  affecté (déjà garanti par l'absence d'état partagé entre les deux appels).
- En cas d'échec réseau sur `searchUsers`, le sélecteur concerné reste vide
  sans notification affichée à l'utilisateur : choix délibéré (peuplement
  silencieux d'options de sélection), l'échec est seulement loggé en console
  via le `catch` déjà présent dans `loadManagers`/`loadEvaluators`. Ceci est
  un changement de comportement assumé par rapport à aujourd'hui (où
  `fetchPaginatedUsers` affichait une notification sur échec, certes déjà
  bogée — voir section « Hors périmètre »). Si un retour visuel d'erreur est
  souhaité pour ce cas précis, il devra être ajouté explicitement dans le
  `catch` du composant (hors périmètre de cette correction sauf demande
  contraire de l'utilisateur).
- Aucun changement de comportement attendu côté utilisateur final en dehors
  de la disparition du bug et de la disparition des notifications de succès
  parasites à l'ouverture du dialogue (qui n'apportaient aucune valeur
  informative pour cette action).

## Hors périmètre (observé, non corrigé ici)

- `fetchPaginatedUsers` (lignes 62-66 de `userStore.ts`) appelle
  `notification.showSuccessNotification(...)` sur le **chemin d'échec**
  (`response?.isSuccess` faux), alors qu'il s'agit visiblement d'une erreur
  de copier-coller (devrait être `showErrorNotification`). Ce point est
  distinct du bug de concurrence traité ici, n'affecte pas le dialogue une
  fois la présente correction appliquée (qui n'appelle plus
  `fetchPaginatedUsers`), et n'est donc pas corrigé dans le cadre de cette
  spécification. À traiter séparément si confirmé pertinent par l'utilisateur.

## Critères d'acceptation vérifiables

1. `userStore.ts` expose une nouvelle méthode (ex. `searchUsers`) qui :
   - n'assigne jamais `users.value`, `totalCount.value`, `currentPage.value`,
     `pageSize.value`, `totalPages.value` ni `loading.value` ;
   - n'appelle jamais `notification.showSuccessNotification` ni
     `notification.showErrorNotification` ;
   - retourne un tableau (`UserSearchResultDto[]`), vide en cas de réponse en
     échec.
2. `fetchPaginatedUsers` est strictement inchangée (aucune ligne modifiée) —
   vérifiable par diff.
3. `ApplicationAssign.vue` : `loadManagers` et `loadEvaluators` n'accèdent
   plus à `userStore.users` à aucun moment ; elles utilisent le résultat
   direct de l'appel à la nouvelle méthode de store pour construire
   `managerOptions.value` / `evaluatorOptions.value`.
4. Test manuel de non-régression (à défaut de tests automatisés, absents de
   ce package) : ouvrir plusieurs fois de suite le dialogue « Assigner des
   utilisateurs » sur une candidature dont l'organisation a au moins un
   manager et un évaluateur technique actifs et distincts ; dans chaque
   ouverture, le sélecteur « Manager » ne doit proposer que des comptes
   ayant le rôle `Organization.Manager`, et le sélecteur « Évaluateur
   technique » ne doit proposer que des comptes ayant le rôle
   `Organization.TechnicalEvaluator`. Répéter en simulant un réseau lent
   (throttling navigateur) pour maximiser la probabilité de résolution
   inversée des deux promesses, avant et après correctif, afin de confirmer
   la disparition du mélange.
5. Aucune notification de succès ne doit plus apparaître à la simple
   ouverture du dialogue « Assigner des utilisateurs » (les notifications
   liées à l'assignation/désassignation effective d'un utilisateur, elles,
   restent inchangées — hors périmètre de cette correction, gérées par
   `applicationStore.assignUser` / `unassignUser`).
6. Aucun des autres appelants de `userStore.fetchPaginatedUsers`
   (`CandidatesPage.vue`, `UsersPage.vue`, et tout composant listant les
   utilisateurs paginés) ne doit présenter de régression de comportement.
