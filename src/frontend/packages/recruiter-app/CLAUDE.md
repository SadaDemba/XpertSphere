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
  de version dans le menu compte utilisateur) et
  `fix-experience-fields-candidate-detail-page.md` (l'interface `Experience`/le
  template `CandidateDetailPage.vue` utilisaient des noms de champs sans
  rapport avec le DTO backend réel ; documente aussi, pour mémoire seulement,
  11 composants `.vue` orphelins au même type d'incohérence, jamais montés par
  aucune page, volontairement non supprimés).
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
- Les correctifs de `ProfilePage.vue` (mise à jour de profil et déconnexion qui
  n'appelaient jamais l'API réelle tout en affichant un toast de succès) sont
  pilotés par `.claude/specifications/fix-profile-page-update-and-logout.md`.
- 4 bugs sur la gestion des offres d'emploi (liste `/jobs` et fiche détail
  `/jobs/:id`) — actions "Dupliquer"/"Voir les candidatures" inertes depuis la
  liste, suppression sans confirmation sur la fiche détail, champs de date
  vides en édition, suffixe `€` en dur ignorant la devise de l'organisation —
  sont pilotés par `.claude/specifications/fix-job-offer-list-detail-actions.md`.
  Inclut également un filtrage réel de `ApplicationsPage.vue` par offre
  d'origine (`?jobId=`), aujourd'hui ignoré malgré un plumbing déjà présent
  côté modèle/backend.
- La correction de la condition de course sur `userStore.users` dans le
  dialogue d'assignation d'une candidature (`ApplicationAssign.vue`), qui
  pouvait mélanger les listes « Manager » et « Évaluateur technique », est
  pilotée par
  `.claude/specifications/fix-application-assign-race-condition.md`.
- Le câblage réel de `ForgotPasswordPage.vue` (mot de passe oublié) sur
  l'endpoint backend `POST /api/auth/forgot-password`, aujourd'hui purement
  factice (`setTimeout`/`console.log`), est piloté par
  `.claude/specifications/wire-forgot-password-page.md` — inclut une
  dépendance backend connue (aucun email n'est réellement envoyé aujourd'hui
  par `ForgotPasswordAsync`, ticket backend séparé recommandé).
- Le câblage d'une nouvelle action "Voir les utilisateurs" dans le menu de
  `RolesPage.vue` (le dialog associé existait déjà mais n'était jamais
  déclenché) et la correction d'un champ de colonne incorrect (`userName` →
  `userFullName` dans `userRoleColumns`) sont pilotés par une spécification
  backend qui impacte directement ce package : voir
  `src/backend/XpertSphere.MonolithApi/.claude/specifications/role-detail-scope-fix-users-dialog-wiring.md`.
- La suppression pure de la page publique « Créer un compte »
  (`RegisterPage.vue`, entièrement factice — aucun appel API réel —, route
  `/auth/register`, lien depuis `LoginPage.vue` et entrée correspondante
  dans `authGuard.ts`) est pilotée par
  `.claude/specifications/remove-public-recruiter-registration.md` —
  documente aussi en bonus, sans le traiter, l'endpoint backend orphelin
  `POST /auth/register` jamais appelé par aucun frontend du monorepo.
- L'alignement des règles de mot de passe sur les 3 formulaires de ce
  package (`UsersPage.vue` : création d'utilisateur et dialog de
  réinitialisation ; `ProfilePage.vue` : changement de mot de passe) sur la
  politique réelle du backend (majuscule, minuscule, chiffre, caractère
  spécial, longueur minimale 8) est pilotée par une spécification backend qui
  impacte directement ce package : voir
  `src/backend/XpertSphere.MonolithApi/.claude/specifications/localize-identity-error-messages.md`,
  section « Coordination frontend — alignement des règles de mot de passe ».
  Signale aussi, sans le traiter, un bug de câblage préexistant sur
  `ProfilePage.vue` (`changePassword` poste un payload `ChangePasswordDto`
  vers l'endpoint `/reset-password`, qui attend en réalité un
  `ResetPasswordDto`).
- Six correctifs UX/visuels indépendants et à faible risque, identifiés par
  un audit avant une démo, sont pilotés par
  `.claude/specifications/ux-polish-pre-demo.md` : bannière d'erreur
  manquante sur `ApplicationsPage.vue` (absente alors que `applicationStore`
  expose déjà `hasError`/`errorMessage`/`clearError`), recherche non
  harmonisée entre les champs de `JobFilters.vue` (Titre sans debounce,
  Localisation seulement sur `Entrée`), masquage des pages « à venir » du
  menu principal (Tableau de bord, Entretiens, Rapports) avec en complément
  une redirection de la route racine `/` vers `/jobs` (le logo de l'en-tête
  y pointe, indépendamment du menu), double pagination sur
  `CandidatesPage.vue` (pied de page intégré du `q-table` + `q-pagination`
  externe redondant), `console.log` résiduel dans
  `ApplicationDetailPage.vue`, et harmonisation à +1 an des deux valeurs de
  repli d'expiration de rôle dans `UsersPage.vue` (correctif volontairement
  minimal — documente sans le traiter le fait que le hint « rôle permanent »
  du même champ reste inexact, le backend traitant `ExpiresAt == null` comme
  réellement permanent alors que le frontend ne transmet jamais cette
  valeur).
- Le formulaire de création d'utilisateur (`UsersPage.vue`, `POST /api/Users`,
  `CreateUserDto` côté backend) bénéficie lui aussi d'une traduction des
  messages de validation (`DataAnnotations`), pilotée par la même
  spécification backend ci-dessus. Limite de rendu documentée là-bas, à
  connaître si ce fichier est retouché : `userStore.createUser`
  (`src/stores/userStore.ts`) ne lit que `response?.message` en cas
  d'échec et n'a pas d'équivalent à `extractApiErrorMessages`
  (`candidate-app/src/utils/apiErrors.ts`) — un échec de validation de champ
  affichera donc un message générique français, pas le détail du champ en
  cause, tant que cette normalisation n'est pas ajoutée à ce package (hors
  périmètre de la spécification citée).
- L'activation par invitation des comptes utilisateurs internes (retrait du
  champ mot de passe du formulaire de création `UsersPage.vue`, nouvelle page
  `/auth/accept-invitation` de définition de mot de passe avec durcissement
  anti-scanner, ajout de cette route à la liste blanche en dur de
  `authGuard.ts`) est pilotée par une spécification backend qui impacte
  directement ce package : voir
  `src/backend/XpertSphere.MonolithApi/.claude/specifications/internal-user-account-invitation.md`,
  section « Coordination frontend — recruiter-app ». Documente au passage que
  `authGuard.ts` n'utilise pas `route.meta.requiresAuth` mais une liste
  blanche `publicPages` en dur — toute nouvelle route publique doit y être
  ajoutée explicitement, y compris celle-ci.

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
