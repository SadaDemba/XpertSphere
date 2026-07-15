# Instructions Claude — candidate-app (XpertSphere)

## Aperçu de l'application

`candidate-app` est l'application frontend publique destinée aux candidats de la plateforme de recrutement XpertSphere. Elle permet de consulter les offres d'emploi, de créer un compte et un profil candidat (avec analyse automatique de CV à l'inscription), de soumettre des candidatures et d'en suivre le statut.

Le package fait partie du monorepo `frontend` (workspaces npm), aux côtés de `recruiter-app` (interface réservée aux recruteurs/administrateurs, avec authentification Azure AD via `@azure/msal-browser` au niveau racine du monorepo). `candidate-app`, en revanche, n'utilise pas MSAL : l'authentification y repose uniquement sur un mécanisme JWT dédié.

Stack technique : Vue 3 (Composition API) + Quasar Framework 2 + TypeScript + Pinia + Vue Router 4 + Axios + vue-i18n.

## Documentation

- Structure détaillée, conventions de code et intégration avec le backend : voir `.claude/docs/architecture.md`
- Spécifications fonctionnelles (une par fonctionnalité) : voir `.claude/specifications/`. Actuellement : `candidate-registration-training-validation-error.md` (gating de l'étape "Formations" du formulaire d'inscription et normalisation des réponses d'erreur backend dans `authStore`).
- Le téléchargement sécurisé du CV (`ProfilePage.vue`) est piloté par une spécification backend qui impacte directement ce package : voir `src/backend/XpertSphere.MonolithApi/.claude/specifications/secure-cv-download.md`, section « Coordination frontend — candidate-app ».

## Commandes

Depuis ce dossier (`packages/candidate-app`) :

```bash
npm run dev              # Démarrage du serveur de dev (quasar dev), hot reload
npm run build             # Build de production (quasar build)
npm run preview           # Sert le build de dist/spa sur le port 3000
npm run lint              # ESLint (config partagée à la racine frontend/eslint.config.mjs), inclut les règles a11y
npm run lint:fix          # ESLint avec correction automatique
npm run format            # Prettier sur l'ensemble des fichiers du package
npm run test              # Aucun test défini actuellement (placeholder, exit 0)
```

Depuis la racine du monorepo frontend (`src/frontend`), des raccourcis équivalents existent : `npm run dev:candidate`, `npm run build:candidate`, `npm run preview:candidate`, ainsi que `npm run lint`, `npm run format` et `npm run test` appliqués à tous les workspaces.
