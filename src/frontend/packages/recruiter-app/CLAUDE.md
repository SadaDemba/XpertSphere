# Instructions Claude — recruiter-app (XpertSphere)

## Aperçu de l'application

`@xpertsphere/recruiter-app` est l'interface web destinée aux équipes RH et aux
administrateurs de la plateforme de recrutement XpertSphere (contexte
Sénégal/Afrique de l'Ouest). C'est l'un des deux packages frontend du
monorepo, l'autre étant `candidate-app` (côté candidat). Ce package couvre le
côté « back-office » du recrutement : gestion des offres d'emploi, suivi des
candidatures, consultation des profils candidats, gestion des utilisateurs
internes, des organisations et des rôles/permissions (RBAC).

D'après les routes définies (`src/router/routes.ts`) et les pages présentes,
l'application propose au minimum : un tableau de bord, la gestion des offres
d'emploi, la liste et le détail des candidats (avec consultation de CV), la
liste et le détail des candidatures (avec historique de statut et
assignation), une section entretiens, une section rapports, un profil
utilisateur, et un espace d'administration (utilisateurs, organisations,
rôles) réservé aux profils habilités.

## Stack technique

- **Framework** : Vue.js 3 (Composition API) + Quasar Framework 2 (`@quasar/app-vite`)
- **Langage** : TypeScript (mode strict activé dans `quasar.config.ts`)
- **Gestion d'état** : Pinia (stores par domaine : `authStore`, `userStore`,
  `jobOfferStore`, `applicationStore`, `organizationStore`, `roleStore`,
  `userRoleStore`), écrits avec l'API de composition (`ref`/`computed`)
- **Routing** : Vue Router (mode `hash`, cf. `vueRouterMode` dans
  `quasar.config.ts`)
- **HTTP** : Axios, encapsulé dans une classe `BaseClient` commune
- **i18n** : vue-i18n (locales `en-US` et `fr-FR` dans `src/i18n/`, locale par
  défaut actuellement `en-US` dans `src/boot/i18n.ts`)
- **Authentification** : hybride JWT (mode développement local) / Microsoft
  Entra ID via MSAL (`@azure/msal-browser`, `src/services/MsalService.ts`),
  sélectionnée par la variable d'environnement `VITE_AUTH_MODE`
- **Accessibilité** : `eslint-plugin-vuejs-accessibility` (règles RGAA/WCAG
  2.1 AA en erreur) et `vue-axe` pour des alertes en temps réel en mode dev
- **Sécurité** : `dompurify` pour l'assainissement de contenu (`src/services/sanitizer.ts`)
- Le monorepo frontend est un workspace npm (`workspaces: ["packages/*"]`
  défini dans `src/frontend/package.json`), partagé avec `candidate-app` ;
  ESLint et une partie de la configuration (husky, lint-staged, prettier)
  sont mutualisés à la racine du workspace

## Structure du projet

```
src/
├── boot/            # Fichiers d'initialisation Quasar (i18n, axios, axe)
├── components/       # Composants Vue, organisés par domaine métier
│   ├── admin/        # Formulaires organisation/rôle, assignation de rôles
│   ├── applications/ # Cartes, tableaux, filtres, historique de candidatures
│   ├── candidates/   # Cartes, tableaux, filtres, aperçu de CV
│   ├── common/       # Composants transverses (ex. pagination)
│   └── jobs/         # Cartes, tableaux, filtres d'offres d'emploi
├── composables/      # Logique réutilisable (datatable, dialog, notification)
├── enums/            # Enums métier (statut candidature, type de contrat, mode de travail, etc.)
├── helpers/          # Utilitaires (dates, mapping de données)
├── i18n/             # Fichiers de traduction en-US / fr-FR
├── layouts/          # AuthLayout (pages d'authentification), MainLayout (appli connectée)
├── models/           # Interfaces/DTO TypeScript (auth, job, application, user, organization, role...)
├── pages/            # Vues routées, organisées par domaine (admin/, applications/, auth/, candidates/, jobs/)
├── router/           # Définition des routes + guards (auth, rôles)
│   └── guards/       # authGuard (session), roleGuard (RBAC par route)
├── services/         # Clients HTTP par ressource, dérivés de BaseClient
├── settings/         # Lecture centralisée des variables d'environnement (VITE_*)
└── stores/           # Stores Pinia par domaine
```

Composants racine notables : `AppHeader.vue`, `AppFooter.vue`,
`AppNavigation.vue`, `AppLogo.vue`, `NavItem.vue`.

## Commandes

Depuis ce package (`packages/recruiter-app`) :

```bash
npm install          # Installation des dépendances
npm run dev          # Développement avec hot reload (quasar dev)
npm run build        # Build de production (quasar build)
npm run preview      # Sert le build (dist/spa) sur le port 3001
npm run lint         # ESLint (config partagée ../../eslint.config.mjs)
npm run lint:fix     # ESLint avec correction automatique
npm run format       # Prettier sur l'ensemble des fichiers du package
npm run test         # Aucun test défini actuellement (placeholder, exit 0)
```

Depuis la racine du workspace frontend (`src/frontend`), équivalents ciblés
sur ce package : `npm run dev:recruiter`, `npm run build:recruiter`,
`npm run preview:recruiter`. Les commandes `npm run lint`, `npm run test` et
`npm run format` à la racine s'exécutent sur tous les packages du workspace
(`--workspaces --if-present`).

Aucun script de test unitaire n'est actuellement implémenté dans ce package
(`"test": "echo \"No test specified\" && exit 0"`).

## Conventions de code observées

- **Composants** : noms de fichiers en PascalCase (`ApplicationCard.vue`,
  `JobFilters.vue`), imposé par la règle ESLint
  `vue/component-definition-name-casing: PascalCase`. Utilisation en
  kebab-case dans les templates (`vue/component-name-in-template-casing`).
  Les noms de composants à un seul mot sont autorisés
  (`vue/multi-word-component-names: off`).
- **Organisation par domaine métier** : `components/`, `pages/` et les
  services suivent le même découpage (jobs, applications, candidates, admin),
  ce qui facilite la navigation croisée entre une page, ses composants et son
  service associé.
- **Services API** : chaque ressource backend a une classe dédiée qui étend
  `BaseClient` (ex. `JobOfferService extends BaseClient`), appelle `super()`
  avec le préfixe de route (ex. `/JobOffers`, `/Applications`, `/Users`), et
  exporte une instance singleton (`export const jobOfferService = new
JobOfferService(); export default jobOfferService;`). Les méthodes
  retournent des types génériques `ResponseResult<T>`, `PaginatedResult<T>`
  ou `VoidResponseResult` (définis dans `src/models/base.ts`), reflétant le
  format de réponse standard de l'API backend.
- **State management** : chaque store Pinia est défini avec la syntaxe
  « setup » (`defineStore('nom', () => { ... })`), état en `ref`, dérivés en
  `computed`, actions en fonctions fléchées classiques ; les stores
  consomment directement les services correspondants (ex. `authStore` utilise
  `authService`).
- **Modèles/DTO** : types et interfaces TypeScript dans `src/models/`, un
  fichier par domaine (`job.ts`, `application.ts`, `user.ts`, `auth.ts`,
  `organization.ts`, `role.ts`, `userRole.ts`), avec convention `CreateXDto`,
  `UpdateXDto`, `XFilterDto` pour les payloads.
- **Enums métier** centralisés dans `src/enums/` (`ApplicationStatus`,
  `JobOfferStatus`, `ContractType`, `WorkMode`, `OrganizationSize`,
  `ApplicationSource`, `SortDirection`).
- **Imports de types** : règle ESLint
  `@typescript-eslint/consistent-type-imports` en mode `type-imports`
  (préférer `import type { ... }`).
- **Accessibilité obligatoire** : toute modification de template `.vue` doit
  respecter les règles `vuejs-accessibility/*` (alt text, labels de
  formulaire, gestion clavier, aria, etc.), configurées en `error` dans
  `eslint.config.mjs`.
- **Routes protégées** : les routes de l'espace connecté déclarent
  `meta: { requiresAuth: true }` au niveau du layout principal, et utilisent
  des guards dédiés (`beforeEnter: organizationRoleGuard` pour les pages
  métier accessibles aux rôles d'organisation, `adminSectionGuard` ou
  `platformAdminGuard` pour les pages `admin/*`). La logique RBAC est
  centralisée dans `src/router/guards/roleGuard.ts` et s'appuie sur les
  constantes de rôles définies dans `src/models/auth.ts`
  (`PlatformRoles`, `OrganizationRoles`, `ManagementRoles`, etc.).
- **Pas de tirets cadratins ni de style particulier imposé côté code** au-delà
  de la configuration ESLint/Prettier standard (`.prettierrc.json`,
  `eslint-config-prettier`).

## Intégration avec le backend

- L'application consomme une API REST (« Monolith Api » XpertSphere) dont
  l'URL de base est fournie par la variable d'environnement
  `VITE_WEB_API_BASE_URL` (ex. `https://localhost:7001` en développement),
  lue via `src/settings/index.ts`. Chaque requête est préfixée par `/api`
  (`BaseClient` construit `baseURL = ${settings.webApi.baseUrl}/api${url}`).
- Une deuxième URL de service est configurée pour un composant d'analyse de
  CV : `VITE_RESUME_ANALYZER_BASE_URL` (`settings.resumeAnalyzer.baseUrl`,
  ex. `http://localhost:8001` en local). Une URL de stockage Azure Blob est
  également prévue (`VITE_STORAGE_BASE_URL` / `settings.storage.baseUrl`).
- **Authentification hybride** gérée dans `BaseClient` et
  `settings.auth.mode` (`jwt` ou `entraid`) :
  - mode `jwt` : jeton et refresh token stockés en `localStorage`
    (`xpertsphere_token`, `xpertsphere_refresh_token`), rafraîchissement
    automatique si expiration proche (moins de 5 minutes), endpoints
    `/auth/login` et `/auth/refresh` appelés directement (hors intercepteur
    pour éviter la récursion) ;
  - mode `entraid` : jeton récupéré via MSAL (`msalInstance.getAccessToken`,
    `src/services/MsalService.ts`), configuration Entra ID (client IDs,
    tenant, instance, callback path) lue depuis les variables
    `VITE_ENTRAID_*`.
  - Un intercepteur Axios gère les réponses 401 avec tentative de
    rafraîchissement puis rejeu de la requête originale ; en cas d'échec,
    déclenchement d'un événement `auth:failure` et déconnexion.
- Une clé d'abonnement APIM (`Ocp-Apim-Subscription-Key`) est ajoutée aux
  requêtes uniquement pour les environnements `staging` et `production`
  (`settings.apim`).
- **Services et ressources backend identifiés** (préfixes de route dans
  `src/services/`) :
  - `AuthService` → `/auth` (login, current user, refresh, reset/forgot
    password, type d'utilisateur, health check)
  - `JobOfferService` → `/JobOffers` (CRUD, pagination, publication/fermeture,
    offres par organisation, offres de l'utilisateur courant, vérification de
    droits de gestion)
  - `ApplicationService` → `/Applications` (CRUD, pagination, changement de
    statut, retrait, candidatures par offre/candidat/organisation,
    assignation/désassignation d'utilisateur, vérification de candidature
    existante)
  - `ApplicationStatusHistoryService` → `/ApplicationStatusHistory`
    (historique de statut, commentaires sur candidature)
  - `UserService` → `/Users` (CRUD, activation/désactivation, complétion de
    profil, utilisateurs par organisation, utilisateurs inactifs ou
    récemment inscrits)
  - `OrganizationService` → `/Organizations` (CRUD, pagination)
  - `RoleService` → `/Roles` (CRUD, activation/désactivation, vérification
    d'existence et de suppressibilité)
  - `UserRoleService` → `/UserRoles` (attribution/retrait de rôle,
    prolongation, vérification de rôle actif)
- Le format de réponse attendu de l'API est homogène : `ResponseResult<T>`
  (`data`, `isSuccess`, `message`, `statusCode`, `errors`) ou
  `PaginatedResult<T>` (idem + objet `pagination`), défini dans
  `src/models/base.ts`.

## Spécifications

_À compléter : les spécifications fonctionnelles de cette application seront ajoutées ici au fur et à mesure._
