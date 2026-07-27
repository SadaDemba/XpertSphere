# Suppression de la page publique « Créer un compte » (auto-inscription recruteur)

## Contexte et diagnostic (à partir du code réel)

`RegisterPage.vue` (`src/pages/auth/RegisterPage.vue`) est une page publique
« Créer un compte » accessible via la route `/auth/register`
(`src/router/routes.ts`, lignes 20-23, `meta: { requiresGuest: true }`), elle
listée comme page publique dans le guard (`src/router/guards/authGuard.ts`,
ligne 15, `publicPages = ['/auth/login', '/auth/register',
'/auth/forgot-password']`), et accessible depuis `LoginPage.vue` via le lien
« Pas encore de compte ? Créer un compte » (~lignes 111-119).

Cette page est **entièrement factice** : sa fonction `handleRegister`
(lignes 238-257) exécute un `await new Promise(resolve =>
setTimeout(resolve, 2000))`, un `console.log` des champs saisis
(`firstName`, `lastName`, `email`, `company`, `jobTitle` — le mot de passe
n'est même pas loggé), puis une redirection simulée vers
`/auth/login?registered=true`. **Aucun appel API n'est effectué**, aucun
compte n'est réellement créé.

Le formulaire collecte des champs révélateurs de son intention d'origine :
`company` (nom de l'entreprise) et `jobTitle` (fonction dans l'entreprise),
en plus des champs d'identité/mot de passe classiques — ce qui suggère une
intention de self-service **entreprise** (créer une organisation et son
premier compte admin en un seul flux), et non une simple inscription
utilisateur au sein d'une organisation déjà existante.

Or, l'exploration exhaustive du backend montre qu'**aucun endpoint public ne
permet ce cas d'usage, ni aucun autre cas d'auto-inscription recruteur** :

- `POST /organizations` (`Controllers/OrganizationsController.cs`, ligne 53)
  — seul moyen de créer une organisation — exige la policy
  `RequirePlatformSuperAdminRole`. Aucune création d'organisation en
  self-service n'existe dans le produit.
- `POST /Users` (`Controllers/UsersController.cs`, ligne 74) — endpoint
  réellement utilisé par le flux fonctionnel existant `admin/UsersPage.vue`
  (`userStore.createUser` → `userService.createUser` →
  `POST /Users`) — exige la policy `CanCreateUsers` (utilisateur déjà
  authentifié et habilité) et un `organizationId` d'une organisation
  **déjà existante** (menu déroulant, pas de création à la volée).
- `POST /auth/register` (`Controllers/AuthController.cs`, lignes 26-31,
  `RegisterDto`) exige lui aussi la policy `CanCreateUsers` — ce n'est donc
  pas un endpoint d'auto-inscription malgré son nom. Vérification
  supplémentaire : **aucun frontend, ni `recruiter-app` ni `candidate-app`,
  n'appelle cet endpoint** (recherche exhaustive de `auth/register'` et
  `RegisterAsync` dans `src/frontend`) — il est orphelin, cf. section « Dette
  technique documentée en bonus » ci-dessous.
- Seul `POST /auth/register/candidate` (ligne 37-38, `[AllowAnonymous]`) est
  public — mais il est consommé exclusivement par le vrai flux `/register`
  de `candidate-app` (`JobDetailsPage.vue`, `LoginPage.vue`,
  `router/guards/auth.ts`), pour l'auto-inscription **candidat**, pas
  recruteur/entreprise. Ce flux est fonctionnel, hors périmètre de cette
  spec, et n'y est pas modifié.

Le contraste confirme le diagnostic : le produit prévoit bien une
auto-inscription self-service, mais uniquement côté **candidat**. Aucune
trace dans `README.md` (racine du monorepo) d'une intention produit
d'onboarding self-service pour une entreprise/un recruteur (essai gratuit,
inscription société, etc.).

`RegisterPage.vue` n'est référencée nulle part ailleurs dans le code du
package (recherche exhaustive : seuls `routes.ts`, `LoginPage.vue` et
`authGuard.ts` la mentionnent), et aucun test ne la couvre.

## Décision retenue

**Suppression pure et complète**, sans remplacement ni implémentation
alternative : ni la page, ni la route, ni le lien d'accès depuis le login,
ni l'entrée du guard ne sont conservés. Il n'existe aujourd'hui aucun besoin
produit confirmé, ni aucun support backend, pour une auto-inscription
recruteur/entreprise — proposer une implémentation à la place serait
inventer une fonctionnalité et un backend associé non demandés.

Le seul chemin légitime de création de compte utilisateur côté
`recruiter-app` reste celui déjà fonctionnel et inchangé :
`admin/UsersPage.vue`, réservé aux utilisateurs authentifiés disposant de la
policy `CanCreateUsers`.

## Objectif et périmètre

- Supprimer le fichier `src/pages/auth/RegisterPage.vue`.
- Retirer la route enfant `register` de `src/router/routes.ts` (bloc
  `/auth`, lignes 20-23).
- Retirer le lien « Pas encore de compte ? Créer un compte » de
  `src/pages/auth/LoginPage.vue` (le `q-card-section` correspondant,
  ~lignes 108-114, y compris son `<p>` d'accompagnement, `q-separator`
  associé à retirer/adapter selon la mise en page résultante).
- Retirer `'/auth/register'` du tableau `publicPages` dans
  `src/router/guards/authGuard.ts` (ligne 15).

### Hors périmètre

- Toucher au flux d'auto-inscription candidat (`candidate-app`,
  `POST /auth/register/candidate`) : fonctionnel, non concerné.
- Modifier `admin/UsersPage.vue` ou le flux `POST /Users` : fonctionnels,
  non concernés.
- Supprimer ou modifier l'endpoint backend orphelin `POST /auth/register`
  (`AuthController.cs`) : documenté ci-dessous comme dette technique
  séparée, mais son traitement (suppression, ou rattachement à un futur
  besoin) relève d'une décision et d'une spec backend distinctes.
- Implémenter tout mécanisme de création d'organisation en self-service :
  décision explicitement écartée faute de besoin produit confirmé.

## Acteurs et permissions

Aucun changement de permission : la page supprimée était accessible à tout
visiteur anonyme (`requiresGuest`) et ne créait aucun compte réel. Sa
suppression ne retire aucune capacité fonctionnelle existante à aucun rôle
(candidat, recruteur, admin d'organisation, superadmin plateforme).

## Règles métier et changements ciblés

### 1. `src/pages/auth/RegisterPage.vue`

Supprimer le fichier entièrement.

### 2. `src/router/routes.ts`

Retirer le bloc de route :

```ts
{
  path: 'register',
  component: () => import('pages/auth/RegisterPage.vue'),
  meta: { requiresGuest: true },
},
```

du tableau `children` de la route `/auth` (lignes 20-23). Conserver les
routes `login` et `forgot-password` inchangées.

### 3. `src/pages/auth/LoginPage.vue`

Retirer le `q-card-section` contenant le lien vers `/auth/register`
(~lignes 108-114 : le `<q-separator class="q-my-md" />` qui précède et le
bloc `<p>Pas encore de compte ?</p>` + `<router-link
to="/auth/register">Créer un compte</router-link>`). Le développeur doit
vérifier visuellement le rendu final de la carte de login après retrait
(pas d'espacement résiduel incohérent, pas de séparateur orphelin en fin de
carte).

### 4. `src/router/guards/authGuard.ts`

Retirer `'/auth/register'` du tableau `publicPages` (ligne 15) :

```ts
const publicPages = ['/auth/login', '/auth/forgot-password'];
```

### 5. Dette technique documentée en bonus (hors implémentation de cette spec)

`POST /auth/register` (`src/backend/XpertSphere.MonolithApi/Controllers/AuthController.cs`,
lignes 24-31, `RegisterDto` avec champs `Trainings`/`Experiences` requis —
peu cohérents avec un enregistrement de recruteur/admin) n'est appelé par
aucun frontend du monorepo (`recruiter-app` ni `candidate-app`). Il s'agit
d'un endpoint orphelin, probablement un résidu d'une itération antérieure du
produit où ce flux était peut-être prévu autrement. Cette spec ne le
supprime pas ni ne le modifie : elle documente le constat pour qu'une
décision ultérieure (suppression, ou réutilisation encadrée par une policy
`CanCreateUsers` déjà en place) soit prise consciemment côté backend, dans
une spec dédiée à `src/backend/XpertSphere.MonolithApi/.claude/specifications/`.

## Cas limites

- Aucune page de l'application ne doit conserver de lien résiduel vers
  `/auth/register` après suppression (vérifié aujourd'hui : seuls les trois
  fichiers listés ci-dessus la référencent).
- Un utilisateur ayant l'URL `/auth/register` en favori ou dans l'historique
  du navigateur doit obtenir le comportement de routage par défaut de
  l'application pour une route inexistante (redirection ou page 404 déjà
  gérée par le router existant) — pas de comportement spécifique à ajouter
  pour cette URL en particulier.

## Critères d'acceptation

1. Le fichier `src/pages/auth/RegisterPage.vue` n'existe plus dans le
   repository.
2. `src/router/routes.ts` ne contient plus de route `register` sous
   `/auth` ; naviguer vers `/auth/register` ne monte plus `RegisterPage.vue`
   (résolu par la route de fallback existante de l'application).
3. `src/pages/auth/LoginPage.vue` n'affiche plus aucun lien ni texte
   invitant à « Créer un compte », et le rendu de la carte de login ne
   présente pas d'espacement/séparateur résiduel incohérent en bas de carte
   (vérification visuelle desktop + mobile).
4. `src/router/guards/authGuard.ts` : `publicPages` ne contient plus
   `'/auth/register'`.
5. Aucune occurrence de `RegisterPage`, ni de la chaîne `/auth/register`,
   ne subsiste dans `src/` du package `recruiter-app` (vérifiable par
   recherche texte).
6. Le flux fonctionnel `admin/UsersPage.vue` (création d'utilisateur par un
   admin habilité) reste inchangé et fonctionnel.
7. `npm run lint` reste au vert après suppression (pas d'import mort, pas de
   route orpheline détectée).
8. `.claude/specifications/README.md` et `CLAUDE.md` du package référencent
   cette nouvelle spec.

## Points à confirmer

Aucun point bloquant restant : la direction (suppression pure) a été
explicitement validée par l'utilisateur avant rédaction de cette spec.
