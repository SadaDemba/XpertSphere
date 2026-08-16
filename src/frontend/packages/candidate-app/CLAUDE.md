# Instructions Claude — candidate-app (XpertSphere)

## Aperçu de l'application

`candidate-app` est l'application frontend publique destinée aux candidats de la plateforme de recrutement XpertSphere. Elle permet de consulter les offres d'emploi, de créer un compte et un profil candidat (avec analyse automatique de CV à l'inscription), de soumettre des candidatures et d'en suivre le statut.

Le package fait partie du monorepo `frontend` (workspaces npm), aux côtés de `recruiter-app` (interface réservée aux recruteurs/administrateurs, avec authentification Azure AD via `@azure/msal-browser` au niveau racine du monorepo). `candidate-app`, en revanche, n'utilise pas MSAL : l'authentification y repose uniquement sur un mécanisme JWT dédié.

Stack technique : Vue 3 (Composition API) + Quasar Framework 2 + TypeScript + Pinia + Vue Router 4 + Axios + vue-i18n.

## Documentation

- Structure détaillée, conventions de code et intégration avec le backend : voir `.claude/docs/architecture.md`
- Spécifications fonctionnelles (une par fonctionnalité) : voir `.claude/specifications/`, dont l'index est tenu à jour dans `.claude/specifications/README.md`. Actuellement : `fix-devise-salaire-souhaite-eur-vers-xof.md` (devise du salaire souhaité, EUR → XOF), `candidate-registration-training-validation-error.md` (gating de l'étape "Formations" du formulaire d'inscription et normalisation des réponses d'erreur backend dans `authStore`) et `job-details-page-redesign.md` (refonte visuelle de `JobDetailsPage.vue`).
- Le téléchargement sécurisé du CV (`ProfilePage.vue`) est piloté par une spécification backend qui impacte directement ce package : voir `src/backend/XpertSphere.MonolithApi/.claude/specifications/secure-cv-download.md`, section « Coordination frontend — candidate-app ».
- Le blocage et le message d'erreur explicite sur la description d'expérience vide à l'inscription (`MultiStepRegisterForm.vue`, `RegisterPage.vue`) sont pilotés par une spécification backend qui impacte directement ce package : voir `src/backend/XpertSphere.MonolithApi/.claude/specifications/candidate-registration-experience-description-error.md`, section « Frontend — candidate-app ».
- Le correctif du bug « expériences/formations absentes de `ProfilePage.vue` juste après connexion, sans reload » est entièrement backend (`LoginAsync`/`RefreshTokenAsync` ne chargeaient pas `Experiences`/`Trainings`/`Address`) : voir `src/backend/XpertSphere.MonolithApi/.claude/specifications/login-response-missing-experiences-trainings.md`, section « Coordination frontend — candidate-app ». Aucune modification de code attendue dans ce package.
- Le sélecteur de devise du salaire souhaité (inscription, profil) et la devise réellement affichée sur `ProfilePage.vue`/`JobDetailsPage.vue` sont pilotés par une spécification backend qui impacte directement ce package : voir `src/backend/XpertSphere.MonolithApi/.claude/specifications/configurable-salary-currency.md`, section « Coordination frontend — candidate-app ».
- L'alignement des règles de mot de passe de `MultiStepRegisterForm.vue` (ajout d'une règle « caractère spécial », longueur minimale portée à 8) sur la politique réelle du backend est piloté par une spécification backend qui impacte directement ce package : voir `src/backend/XpertSphere.MonolithApi/.claude/specifications/localize-identity-error-messages.md`, section « Coordination frontend — alignement des règles de mot de passe ».
- Le contenu réel des pages « Conditions d'utilisation »/« Politique de confidentialité » (`/terms`, `/privacy`), les liens cliquables correspondants sur `MultiStepRegisterForm.vue`, et un correctif ponctuel côté backend sans impact fonctionnel (`RegisterCandidateDto.cs`) : voir `.claude/specifications/terms-and-privacy-policy-pages.md`.
- Le filtre « Entreprise » (dropdown) de `JobListingsPage.vue`, sa source de données dérivée côté frontend (sans nouvel endpoint ni modèle `Organization`) et le correctif de layout associé : voir `.claude/specifications/filter-jobs-by-company.md`.

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
