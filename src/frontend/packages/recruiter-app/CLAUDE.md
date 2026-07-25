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

Stack technique en bref : Vue 3 (Composition API) + Quasar Framework 2 +
Pinia + TypeScript (strict) + Vue Router + Axios, avec authentification
hybride JWT / Microsoft Entra ID (MSAL).

## Documentation

- Structure détaillée du projet, conventions de code et intégration avec le
  backend : voir `.claude/docs/architecture.md`.
- Spécifications fonctionnelles (une par fonctionnalité) : voir
  `.claude/specifications/`. Actuellement : `remove-fixed-footer-backoffice.md`
  (suppression complète du footer fixe du back-office, déplacement du numéro
  de version dans le menu compte utilisateur).
- La consultation/téléchargement sécurisée du CV (fiche candidat, candidatures)
  est pilotée par une spécification backend qui impacte directement ce
  package : voir
  `src/backend/XpertSphere.MonolithApi/.claude/specifications/secure-cv-download.md`,
  section « Coordination frontend — recruiter-app ».
- L'affichage du salaire souhaité du candidat (`CandidatesPage.vue`,
  `CandidateDetailPage.vue`) est piloté par une spécification côté
  `candidate-app` (le champ y est saisi) : voir
  `src/frontend/packages/candidate-app/.claude/specifications/fix-devise-salaire-souhaite-eur-vers-xof.md`.
- La nouvelle page « Paramètres de l'organisation » (devise appliquée aux
  offres d'emploi), le retrait du sélecteur de devise libre dans les
  formulaires de création/édition d'offre, et l'affichage de la devise réelle
  du candidat sont pilotés par une spécification backend qui impacte
  directement ce package : voir
  `src/backend/XpertSphere.MonolithApi/.claude/specifications/configurable-salary-currency.md`,
  section « Coordination frontend — recruiter-app ».

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
