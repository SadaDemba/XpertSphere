# Architecture — candidate-app (XpertSphere)

## Stack technique

- **Framework** : Vue 3 (Composition API) + Quasar Framework 2 (`@quasar/app-vite`, basé sur Vite)
- **Langage** : TypeScript
- **State management** : Pinia (stores en Composition API, `defineStore` avec fonctions `setup`)
- **Routing** : Vue Router 4
- **HTTP** : Axios, avec une classe cliente de base (`BaseClient`) gérant l'authentification JWT
- **i18n** : vue-i18n (locales `fr-FR` et `en-US` disponibles dans `src/i18n`)
- **Accessibilité** : `eslint-plugin-vuejs-accessibility` + `vue-axe` (alertes en dev), conformité visée RGAA/WCAG 2.1 AA
- **Gestionnaire de paquets** : npm (workspaces), Node >= 20

## Structure du projet

```
candidate-app/
├── src/
│   ├── App.vue                 # Composant racine
│   ├── boot/                   # Fichiers d'initialisation Quasar (axios, i18n, axe)
│   ├── components/             # Composants réutilisables (JobCard, ApplicationCard, dialogs, formulaire d'inscription multi-étapes...)
│   ├── composables/            # Composables Vue (ex. notifications)
│   ├── css/                    # Styles globaux et variables Quasar
│   ├── enums/                  # Enums métier (ApplicationStatus, ContractType, WorkMode, JobOfferStatus, ApplicationSource)
│   ├── helpers/                # Fonctions utilitaires (ex. DateHelper)
│   ├── i18n/                   # Traductions fr-FR / en-US
│   ├── layouts/                # Layouts Quasar (MainLayout)
│   ├── models/                 # Types/DTO TypeScript (auth, job, application, base)
│   ├── pages/                  # Pages/routes (JobListings, JobDetails, Login, Register, Profile, MyApplications, ApplicationDetails, ErrorNotFound)
│   ├── router/                 # Configuration du router + guard d'authentification (router/guards/auth.ts)
│   ├── services/               # Couche d'accès à l'API backend (un service par ressource, hérite de BaseClient)
│   ├── settings/               # Configuration centralisée (URLs d'API, clés de stockage JWT, clé APIM)
│   └── stores/                 # Stores Pinia (auth, user, application, experience, jobOffer)
├── public/                      # Assets statiques (favicon)
├── quasar.config.ts             # Configuration Quasar/Vite (boot files, plugins Dialog/Notify, etc.)
├── .env / .env.example / .env.production / .env.staging   # Variables d'environnement par environnement
└── tsconfig.json                # Étend .quasar/tsconfig.json généré par Quasar
```

## Conventions de code observées

- **Composants** : nommage en PascalCase pour les fichiers `.vue` (`JobCard.vue`, `ApplicationDialog.vue`, `EditProfileDialog.vue`) ; usage en kebab-case dans les templates (imposé par la règle ESLint `vue/component-name-in-template-casing`).
- **Composition API** : les stores Pinia et composables utilisent la syntaxe `setup()` (fonctions, `ref`/`computed`) plutôt que l'Options API.
- **Services API** : chaque ressource métier a son propre service (`authService`, `jobOfferService`, `applicationService`, `userService`, `experienceService`), sous forme de classe héritant de `BaseClient` et exportée comme instance singleton (ex. `export const authService = new AuthService()`).
- **Typage strict des imports de types** : règle ESLint `@typescript-eslint/consistent-type-imports` (préférence pour `import type`).
- **Modèles** : les types/DTO sont centralisés dans `src/models` et ré-exportés via `src/models/index.ts`.
- **Enums métier** : centralisés dans `src/enums`, ré-exportés via `src/enums/index.ts`.
- **Formatage** : Prettier avec guillemets simples et largeur de ligne 100 caractères (`.prettierrc.json`).
- **Accessibilité** : composants soumis aux règles `vuejs-accessibility` (labels de formulaires, alt-text, gestion clavier, etc.), à respecter lors de l'ajout de nouveaux composants.
- **Routage protégé** : les routes nécessitant une authentification déclarent `meta: { requiresAuth: true }` (ex. `MyApplications`, `ApplicationDetails`, `Profile`) ; un guard global (`router/guards/auth.ts`) redirige vers `/login` si non authentifié, et redirige un utilisateur déjà authentifié qui visite `/login` ou `/register`.

Le linting utilise le fichier `eslint.config.mjs` partagé à la racine `src/frontend`, commun à `candidate-app` et `recruiter-app` : règles Vue + TypeScript + accessibilité (`vuejs-accessibility`) appliquées en mode strict (`error`) sur les fichiers `.vue`.

## Intégration avec le backend

La configuration des URLs d'API se trouve dans `src/settings/index.ts`, alimentée par les variables d'environnement (`VITE_WEB_API_BASE_URL`, `VITE_RESUME_ANALYZER_BASE_URL`, `VITE_APIM_SUBSCRIPTION_KEY`). En développement local (`.env.example`), l'API monolithique est attendue sur `http://localhost:5001` et le service d'analyse de CV sur `http://localhost:8000`. Il n'y a pas d'URL de stockage Azure Blob configurée côté frontend : le container `resumes` est privé, et la consultation/téléchargement d'un CV passe par l'endpoint proxy authentifié `GET /api/users/{id}/cv` (voir `src/backend/XpertSphere.MonolithApi/.claude/specifications/secure-cv-download.md`), jamais par une URL de blob ouverte directement.

Deux backends distincts sont consommés :

- **API monolithique XpertSphere** (`webApi.baseUrl`, préfixe `/api`) : accédée via `BaseClient`, qui gère automatiquement l'ajout du token JWT (`Authorization: Bearer ...`), le rafraîchissement automatique du token à l'approche de l'expiration (moins de 5 minutes), le retry sur erreur 401, et l'ajout conditionnel d'une clé d'abonnement APIM (`Ocp-Apim-Subscription-Key`) en environnements staging/production.
- **Service d'analyse de CV** (`resumeAnalyzer.baseUrl`) : appelé directement en Axios (hors `BaseClient`) depuis `authService.analyzeResume`, sur l'endpoint `POST /api/extract/`, pour extraire automatiquement les informations d'un CV téléversé lors de l'inscription.

Endpoints backend identifiés par ressource (préfixe `/api` sur `webApi.baseUrl`) :

- `Auth` : `POST /login`, `POST /register/candidate` (multipart, avec formations/expériences indexées et CV), `GET /me`, `POST /auth/refresh` (rafraîchissement de token, appelé directement par `BaseClient`)
- `JobOffers` : `GET /` (liste), `GET /paginated` (liste paginée avec filtres), `GET /{id}`, `GET /organization/{organizationId}`
- `Applications` : `POST /` (création), `GET /my` (candidatures du candidat courant), `GET /{id}`, `POST /{id}/withdraw`, `GET /check-applied/job-offer/{jobOfferId}`, `PUT /{id}` (mise à jour cover letter/notes)

D'autres services (`userService`, `experienceService`) existent dans `src/services` et suivent le même schéma (classe héritant de `BaseClient`, un constructeur fixant le préfixe de ressource).

L'authentification côté candidat est exclusivement JWT (stockage dans `localStorage`, clés `xpertsphere_candidate_token` / `xpertsphere_candidate_refresh_token`, distinctes de celles de `recruiter-app`).
