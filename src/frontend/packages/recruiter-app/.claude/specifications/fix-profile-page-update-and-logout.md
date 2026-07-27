# Correctifs `ProfilePage.vue` — mise à jour de profil factice et déconnexion factice

## Contexte et périmètre

Un audit pré-démo a identifié deux régressions sur `src/pages/ProfilePage.vue` : le
formulaire d'édition de profil et le bouton de déconnexion affichent tous les deux un
toast de succès alors qu'aucun appel API réel n'est effectué. Cette spec couvre
uniquement ces deux correctifs (+ un correctif directement lié, découvert pendant
l'investigation, voir « Correctif connexe »), sur cette seule page. Aucune autre page
n'est dans le périmètre.

Fichiers concernés :

- `src/pages/ProfilePage.vue`
- `src/stores/authStore.ts` (lecture seule attendue — pas de modification)
- `src/stores/userStore.ts` (un correctif ponctuel, voir plus bas)

## État constaté (vérifié dans le code, pas seulement d'après l'audit)

1. **`updateProfile` (script `<script setup>`, fonction `updateProfile`)** : l'appel API
   est entièrement commenté (`// TODO: Implémenter l'appel API...` /
   `// await authStore.updateProfile(profileForm);`). `authStore` ne possède d'ailleurs
   aucune méthode `updateProfile` — confirmé en lisant `stores/authStore.ts` en entier.
   Un toast positif « Profil mis à jour avec succès » s'affiche malgré tout, et le
   formulaire quitte le mode édition : l'utilisateur croit avoir sauvegardé, rien n'a
   changé côté API.
2. **`logout`** : `await authStore.logoutUser();` est commenté ; cette méthode n'existe
   pas sur `authStore` (confirmé). Le vrai nom est `authStore.logout()` — déjà utilisé
   correctement dans `AppHeader.vue` (`handleLogout`, ligne ~200) et
   `AppNavigation.vue` (`handleLogout`, ligne ~226) : `await authStore.logout();
router.push('/auth/login');`. Sur `ProfilePage.vue`, le token/la session ne sont
   jamais invalidés ni côté client ni côté API, mais un toast de succès s'affiche et la
   redirection a lieu quand même.

## Méthode d'appel retenue pour la mise à jour de profil

Le service backend expose deux endpoints candidats pour la mise à jour de son propre
profil, tous deux protégés par la policy `CandidateOwnDataAccess` (autorisation sur ses
propres données, quel que soit le rôle) :

- `PUT /api/Users/{id}` (`UpdateUserDto`) — déjà consommé côté frontend par
  `userService.updateUser` / `userStore.updateUser`, utilisé aujourd'hui par
  `pages/admin/UsersPage.vue` pour l'édition admin d'un utilisateur.
- `PUT /api/Users/{id}/profile` (`UpdateUserProfileDto`) — pattern utilisé côté
  `candidate-app` (`EditProfileDialog.vue` → `userStore.updateUserProfile` →
  `userService.updateUserProfile`), non implémenté côté `recruiter-app` (ni service, ni
  store, ni méthode).

**Décision (prise pour limiter le changement avant la démo) : réutiliser
`userStore.updateUser(id, UpdateUserDto)` existant**, plutôt que de porter le pattern
`candidate-app` (ce qui impliquerait d'ajouter un nouveau service backend-client et une
nouvelle action de store côté `recruiter-app`, sans gain fonctionnel ici). Cette
réutilisation a été vérifiée comme sûre :

- `UserMappingProfile` applique `.ForAllMembers(opt => opt.Condition((src, dest,
srcMember) => srcMember != null))` sur `CreateMap<UpdateUserDto, User>` : les champs
  non envoyés (`null`/`undefined`) ne sont **pas** écrasés côté base (pas de risque de
  vider `organizationId`, `department`, `isActive`, `skills`, etc. en ne les envoyant
  pas).
- `Address` est un type owned (`OwnsOne`) mappé dans les colonnes `Address_*` de la
  table `Users` (`UserConfiguration.cs`) : il est chargé avec l'entité `User` sans
  `.Include()` explicite nécessaire, donc pas de risque d'insertion orpheline en
  renvoyant l'objet adresse du formulaire.

**Attention — la garde `srcMember != null` ne s'applique qu'au mapping racine
`UpdateUserDto → User`.** Le mapping imbriqué `AddressDto → Address` provient d'un
simple `CreateMap<Address, AddressDto>().ReverseMap()` **sans** condition de null sur
ses membres. Le formulaire `profileForm.address` n'a pas de champ `streetNumber`
(absent du template) : si le payload envoyé omet ce champ, il sera désérialisé à
`null` côté backend et **écrasera** `Address_StreetNumber` en base à chaque
sauvegarde, même si l'utilisateur avait une valeur existante pour ce champ.

**Exigence : le payload `address` doit reprendre explicitement la valeur actuelle de
`streetNumber` depuis `authStore.user`**, pour ne jamais la vider par omission :
`streetNumber: user.value?.address?.streetNumber`.

### Payload envoyé (`UpdateUserDto`)

Depuis `profileForm`, envoyer uniquement :

```ts
{
  firstName: profileForm.firstName,
  lastName: profileForm.lastName,
  phoneNumber: profileForm.phone,
  department: profileForm.department, // renommé depuis `title`, voir section dédiée
  address: {
    streetName: profileForm.address.street,
    addressLine2: profileForm.address.complement,
    city: profileForm.address.city,
    postalCode: profileForm.address.postalCode,
    region: profileForm.address.region,
    country: profileForm.address.country,
  },
}
```

- Ne **pas** inclure `email` (champ en lecture seule dans le formulaire, non
  modifiable — cohérent avec le hint déjà affiché « L'email ne peut pas être
  modifié »).
- Ne **pas** inclure `organizationId`, `employeeId`, `isActive`, `skills`,
  `trainings`, `experiences`, `desiredSalary`, `availability` : hors formulaire, doivent
  rester inchangés (garanti par le comportement AutoMapper décrit ci-dessus).
- `department` est un cas particulier : il **est** inclus dans le payload — c'est le
  champ actuellement appelé `title` (« Titre/Poste ») dans le formulaire, mappé sur
  `department` (voir section « Champ "Titre/Poste" → `department` » ci-dessous).

### Rafraîchissement de `authStore.user` après succès

`userStore.updateUser` ne met à jour que `userStore.currentUser` / `userStore.users` —
il ne touche jamais `authStore.user`. Or `ProfilePage.vue` affiche et recharge son
formulaire (`user` computed, `loadUserProfile()`) exclusivement depuis `authStore.user`.
Sans rafraîchissement explicite, un clic sur « Modifier » puis « Annuler » après une
sauvegarde réussie ré-afficherait les anciennes valeurs (régression silencieuse
équivalente au bug initial : l'utilisateur croit que ça n'a pas sauvegardé).

**Exigence : après un `updateUser` réussi (`response?.isSuccess`), appeler `await
authStore.loadCurrentUser()`** avant de ré-appeler `loadUserProfile()` et de sortir du
mode édition. `loadCurrentUser()` ne déclenche aucun toast en cas de succès (vérifié
dans `authStore.ts`), donc pas de double notification.

### Propriétaire unique du toast (pas de double notification)

`userStore.updateUser` déclenche déjà en interne, via `useNotification()` :

- succès : `notification.showSuccessNotification('Succès de la mise à jour')`
- échec : actuellement `notification.showSuccessNotification(response?.message ...)`
  — **bug**, voir « Correctif connexe » ci-dessous.

**Exigence : `ProfilePage.vue` ne doit plus appeler `$q.notify` lui-même** dans
`updateProfile` (ni pour le succès, ni pour l'échec) : le store est seul responsable du
toast, à l'image du pattern déjà en place côté `candidate-app`
(`EditProfileDialog.vue` ne notifie jamais lui-même, il délègue à
`userStore.updateUserProfile`).

Conséquence assumée (pas une régression) : le texte du toast de succès change de
« Profil mis à jour avec succès » (texte actuel, jamais réellement affiché après un
vrai succès puisque l'appel était commenté) vers le texte générique du store, « Succès
de la mise à jour ». C'est le prix de la réutilisation de `userStore.updateUser`
(partagé avec l'admin) plutôt que d'un message dédié à cette page ; le validator ne
doit pas le signaler comme un défaut.

### Comportement attendu de `updateProfile` (nouvelle implémentation)

```
async function updateProfile():
  loading = true
  response = await userStore.updateUser(user.id, <payload ci-dessus>)
  if response?.isSuccess:
    await authStore.loadCurrentUser()
    loadUserProfile()
    isEditing = false
  # pas de $q.notify ici, le store s'en charge (succès et échec)
  loading = false
```

Cas limites :

- `user.value?.id` absent (ne devrait pas arriver, la page est derrière un guard
  d'authentification) : ne pas appeler l'API, ne rien afficher de plus que ce que
  ferait `userStore.updateUser` avec un id invalide (le store gère déjà l'échec réseau
  applicatif). Pas de garde supplémentaire à ajouter en dehors de la vérification
  d'existence de `user.value` déjà utilisée ailleurs sur la page.
- Échec réseau/validation : le formulaire reste en mode édition (`isEditing` ne repasse
  pas à `false`), les valeurs saisies restent affichées pour permettre une nouvelle
  tentative — comportement déjà correct dans le `catch` actuel de la page, à conserver
  au niveau structure `try/catch/finally` (le `catch` ne devient nécessaire que si
  `userStore.updateUser` peut lever une exception non interceptée ; sinon la
  vérification de `response?.isSuccess` suffit).

## Correctif connexe : toast d'échec au mauvais type dans `userStore.ts`

Dans `stores/userStore.ts`, l'action `updateUser`, branche d'échec (`else`) :

```ts
} else {
  setError(response?.message || "Erreur lors de la mise à jour de l'utilisateur");
  notification.showSuccessNotification(   // <-- bug : devrait être showErrorNotification
    response?.message || "Erreur lors de la mise à jour de l'utilisateur",
  );
}
```

C'est exactement la même famille de défaut que celle visée par l'audit (un toast à
connotation positive s'affiche alors que l'opération a échoué). Ce correctif est requis
dans le périmètre de cette spec car il conditionne directement le comportement correct
de la correction de `updateProfile` (sinon un échec d'API continuerait d'afficher un
toast vert).

**Exigence : remplacer `notification.showSuccessNotification(...)` par
`notification.showErrorNotification(...)` dans cette branche.**

Ce comportement est partagé avec `pages/admin/UsersPage.vue` (édition admin d'un
utilisateur, `saveUser`) : le validator doit vérifier qu'un échec de mise à jour depuis
l'écran d'administration affiche désormais bien un toast d'erreur (négatif), pas un
toast positif avec un message d'erreur.

Aucune autre branche de `userStore.ts` (`createUser`, `fetchPaginatedUsers`,
`fetchUserById`, `fetchUserProfile`) ne fait partie du périmètre, même si certaines
présentent le même défaut (`showSuccessNotification` sur un message d'échec) — ne pas
les corriger ici, elles n'ont pas été demandées et ne sont pas exercées par
`ProfilePage.vue`.

## Correctif `logout`

Remplacer :

```ts
// await authStore.logoutUser();
```

par :

```ts
await authStore.logout();
```

`authStore.logout()` (vérifié dans `authStore.ts`) :

- tente `authService.logoutUser()` (l'appel API réel), et **n'échoue jamais** même si
  cet appel réseau échoue (`try/catch` interne, log `console.warn` et poursuite) ;
- réinitialise `user`, `token`, `refreshToken`, `error` dans le store ;
- vide les tokens JWT du `localStorage` (`authService.clearJwtTokens()`).

Conséquence : le `try/catch` déjà présent dans la fonction `logout` de `ProfilePage.vue`
devient du code mort inoffensif (la branche `catch` ne sera jamais atteinte) — il peut
être conservé tel quel (pas de régression à le garder) ou simplifié, au choix du
développeur, ce n'est pas un point bloquant.

Le toast « Déconnexion réussie » actuellement affiché par `ProfilePage.vue` (absent des
implémentations de `AppHeader.vue` / `AppNavigation.vue`) est **conservé** : cette
déconnexion est déclenchée depuis une boîte de dialogue de confirmation dédiée
(`showLogoutDialog`), contrairement aux actions rapides de la barre de navigation — un
retour visuel explicite reste pertinent ici. Ce n'est pas un défaut à corriger.

Comportement attendu de `logout` (nouvelle implémentation) :

```
async function logout():
  loadingLogout = true
  await authStore.logout()
  $q.notify({ type: 'positive', message: 'Déconnexion réussie' })
  router.push('/auth/login')
  loadingLogout = false
  showLogoutDialog = false
```

## Champ « Titre/Poste » → mappé sur `department` (décision actée)

Ce champ n'avait aucune correspondance dans `models/auth.ts` (`User`), ni dans
`UserDto`, ni dans `UpdateUserDto`, ni dans `UpdateUserProfileDto` côté backend. De
plus, `loadUserProfile()` ne le renseignait jamais depuis `user.value` (aucune ligne
`title: user.value.title` ou équivalent dans l'`Object.assign`) : ce champ ne se
chargeait jamais au chargement de la page et, même saisi, n'était persisté nulle
part — un défaut de la même famille que les deux bugs corrigés ici, mais limité à ce
seul champ.

**Décision confirmée par l'utilisateur : mapper ce champ sur `department`** (champ
interne existant sur `User`/`UserDto`/`UpdateUserDto`), et renommer son libellé
affiché de « Titre/Poste » vers **« Département »**.

Modifications requises dans `ProfilePage.vue` :

- Renommer la propriété du formulaire réactif `profileForm.title` en
  `profileForm.department` (le nom `title` était trompeur une fois mappé sur un champ
  `department`).
- Dans le template, renommer tous les libellés affichés « Titre/Poste » en
  « Département » (mode lecture et mode édition), y compris le `<p>` sous le nom
  affiché dans l'en-tête de la carte profil (`profileForm.title` → `.department`) et
  le `<q-input>` correspondant (`label="Titre/Poste"` → `label="Département"`).
- Dans `loadUserProfile()`, ajouter le chargement initial depuis l'utilisateur
  courant : `department: user.value.department || ''` dans l'`Object.assign` (champ
  absent aujourd'hui, à ajouter).
- Dans le payload envoyé à `userStore.updateUser` (voir section « Payload envoyé »
  ci-dessus), inclure `department: profileForm.department`.

Ce champ n'est **pas** un champ obligatoire (pas de `:rules` requis, cohérent avec le
comportement actuel du champ « Titre/Poste » qui n'était pas marqué `*`).

## Hors périmètre (constaté, non traité ici)

- `onMounted` contient `// await authStore.getCurrentUser();` commenté. Vérifié comme
  inerte et sans impact : `App.vue` appelle déjà `authStore.initialize()` au démarrage
  de l'application, qui charge `authStore.user` via `loadCurrentUser()` avant que le
  routeur ne monte les pages. Ne pas décommenter, ne pas modifier — mentionné ici pour
  mémoire uniquement.
- Les autres branches de `userStore.ts` présentant le même défaut de toast
  (`createUser`, etc.) — voir section précédente.
- Toute autre page de l'application.

## Acceptation

1. Éditer le profil (prénom, nom, téléphone, adresse) puis « Mettre à jour » : un appel
   réseau `PUT /api/Users/{id}` est effectué (vérifiable via l'onglet réseau /
   intercepteur de test), l'utilisateur en base est réellement modifié, un seul toast
   s'affiche (celui du store, pas de doublon), et les valeurs affichées restent celles
   qui viennent d'être sauvegardées même après un cycle Modifier → Annuler.
2. Simuler un échec de l'API de mise à jour (ex. mock 400/500) : un toast de type
   erreur (négatif) s'affiche — jamais un toast positif contenant un message d'erreur —
   et le formulaire reste en mode édition avec les valeurs saisies.
3. Le même comportement de toast d'échec (négatif, pas positif) est vérifié aussi sur
   `pages/admin/UsersPage.vue` (édition d'un utilisateur qui échoue), puisque le
   correctif est partagé au niveau de `userStore.updateUser`.
4. Cliquer sur « Se déconnecter » (confirmer la boîte de dialogue) : un appel réseau
   `POST /api/auth/logout` est effectué, les tokens JWT sont supprimés du
   `localStorage`, `authStore.user`/`authStore.token` repassent à `null`, un toast de
   succès s'affiche, redirection vers `/auth/login`. Un rechargement de page /
   nouvelle navigation vers une route protégée doit rediriger vers le login (session
   réellement invalidée, pas seulement visuellement).
5. `grep -n "TODO: Implémenter\|authStore.updateProfile\|authStore.logoutUser"
src/pages/ProfilePage.vue` ne retourne plus aucune ligne.
6. Le champ « Département » (ex-« Titre/Poste ») affiche la valeur `department` de
   l'utilisateur courant au chargement de la page, et une valeur saisie puis
   sauvegardée est bien persistée en base (`PUT /api/Users/{id}` avec `department` dans
   le payload) et réaffichée après un cycle Modifier → Annuler ou un rechargement de
   page — plus aucune saisie n'y est silencieusement perdue.
