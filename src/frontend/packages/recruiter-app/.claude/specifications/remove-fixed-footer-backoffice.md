# Suppression du footer fixe du back-office et déplacement de la version dans le menu compte

## Contexte et diagnostic (à partir du code réel)

`AppFooter.vue` (`src/components/AppFooter.vue`) est un footer de style « site vitrine » : logo + tagline, trois colonnes de liens (« Plateforme », « Support », « Légal »), séparateur, ligne copyright/version. Il est rendu via `<app-footer />` dans `MainLayout.vue` (ligne 13) à l'intérieur de `<q-layout view="lHh Lpr lFf">` (ligne 2) — la lettre `F` majuscule du groupe footer signifie, dans la syntaxe Quasar, un footer **fixé en permanence** au bas du viewport, sur tous les écrans de l'application.

Ce footer pose deux problèmes constatés dans le code :

1. **Occupation d'espace vertical permanente** sur un outil de back-office utilisé toute la journée. `MainLayout.vue` code en dur cette hauteur réservée dans le style scoped :

   ```css
   #main-content {
     min-height: calc(100vh - 64px - 160px);
   } /* desktop */
   @media (max-width: 599px) {
     #main-content {
       min-height: calc(100vh - 56px - 140px);
     } /* mobile */
   }
   ```

   Ces valeurs supposent la présence du footer (64px/56px = hauteur du header `q-toolbar`, cf. `AppHeader.vue` lignes 226/233 ; 160px/140px = hauteur estimée du footer).

2. **Contenu qui ne mène nulle part de fonctionnel** :
   - Colonne « Plateforme » : liens « Gestion des emplois »/« Viviers de candidats »/« Analyses » qui dupliquent la navigation déjà présente dans la sidebar (`AppNavigation.vue`).
   - Colonne « Support » : « Centre d'aide », « Nous contacter » → notification « Cette fonctionnalité sera bientôt disponible » (fonction `showComingSoon`, lignes 155-163) ; « État du système » → lien externe mort (`href="/status"`, page inexistante).
   - Colonne « Légal » : « Politique de confidentialité », « Conditions d'utilisation », « Accessibilité » → toutes déclenchent aussi `showComingSoon` (aucune de ces pages n'existe dans le router).
   - Seule la ligne finale « © {{ currentYear }} XpertSphere. Tous droits réservés. » / « Version {{ appVersion }} » porte une information réelle, avec `appVersion = '1.0.0'` **codé en dur** (ligne 144), alors que la variable d'environnement `VITE_APP_VERSION` est déjà exposée via `settings.app.version` (`src/settings/index.ts` ligne 39) et n'est actuellement consommée nulle part dans l'affichage de l'application (vérifié : aucune occurrence de `settings.app.version` dans le code ; `ProfilePage.vue` importe bien `settings` depuis `src/settings`, ligne 496, mais uniquement pour `settings.auth.mode`, ligne 511 — ce n'est pas un précédent d'affichage de version, seulement un précédent d'usage de l'import `settings`).

`AppFooter.vue` n'est référencé qu'à deux endroits dans le code de ce package (vérifié par recherche exhaustive) : l'import et l'usage dans `MainLayout.vue`. `.claude/docs/architecture.md` du package le mentionne aussi dans la liste des « Composants racine notables » (ligne 56).

## Décision retenue

**Suppression complète du footer, pas de version allégée.** Le seul élément conservé est le numéro de version de l'application, déplacé dans le menu compte utilisateur (`AppHeader.vue`, `q-btn-dropdown`) — pas de nouvelle page « À propos ». Tout le reste du contenu (logo/tagline, liens de navigation dupliqués, liens « bientôt disponible », pages légales inexistantes) est supprimé sans remplacement : aucune de ces pages/fonctionnalités n'existe réellement aujourd'hui, ce n'est pas une perte de fonctionnalité mais un nettoyage d'UI qui ne pointait déjà vers rien de fonctionnel pour la quasi-totalité de ces liens.

## Objectif et périmètre

- Retirer entièrement `AppFooter.vue` et son usage dans `MainLayout.vue` (composant, import, `<app-footer />`, et le groupe `q-layout view` correspondant).
- Recalculer (ou supprimer) le `min-height` de `#main-content` dans `MainLayout.vue` pour ne plus réserver l'espace du footer disparu.
- Afficher la version de l'application (`settings.app.version`) dans le menu compte utilisateur (`AppHeader.vue`), en tant qu'élément non cliquable.

### Hors périmètre

- Créer une page « À propos » ou tout autre emplacement de remplacement pour le contenu du footer.
- Recréer ou réimplémenter les pages/liens qui existaient dans le footer et ne menaient déjà nulle part de fonctionnel : Centre d'aide, Nous contacter, État du système, Politique de confidentialité, Conditions d'utilisation, Accessibilité. Ces liens n'existaient déjà pas fonctionnellement ; leur suppression n'est pas une régression.
- Toucher au footer de `candidate-app` : vérifié, `candidate-app` ne comporte aucun composant `q-footer`/`AppFooter` — cette spec ne concerne que `recruiter-app`.
- Modifier le mécanisme de `VITE_APP_VERSION` lui-même : la variable est déjà définie dans `.env`/`.env.production`/`.env.staging`/`.env.example` et transmise via `docker/recruiter-app/Dockerfile` et `docker-compose.yml`. Aucune modification de ces fichiers n'est nécessaire ; on réutilise `settings.app.version` tel quel.
- Introduire des clés vue-i18n pour le libellé de version : les items existants du même menu (« Mon profil », « Préférences », « Tutoriels », « Déconnexion », le bloc info utilisateur) sont déjà en français codé en dur, sans clé i18n — le libellé de version doit suivre exactement le même pattern (chaîne française en dur dans le template), pas introduire une nouvelle convention isolée dans ce composant.

## Acteurs et permissions

Aucun changement de permission : le footer était visible par tous les profils authentifiés de `recruiter-app` (rendu inconditionnel dans `MainLayout.vue`, commun à toutes les routes protégées) ; le menu compte où la version est déplacée est lui aussi commun à tous les profils authentifiés (`AppHeader.vue` est monté pour tout utilisateur connecté, indépendamment du rôle). Pas de rôle plateforme/organisation à distinguer ici.

## Règles métier et changements ciblés

### 1. `src/components/AppFooter.vue`

Supprimer le fichier entièrement.

### 2. `src/layouts/MainLayout.vue`

- Retirer l'import `import AppFooter from 'components/AppFooter.vue';` (ligne 21) et l'usage `<app-footer />` (ligne 13).
- Adapter le prop `view` de `q-layout` : conserver un format syntaxiquement valide de 3 groupes de 3 lettres (11 caractères) — la casse de la lettre du groupe footer (`F`/`f`) n'a plus d'effet fonctionnel réel une fois `<app-footer />` retiré (aucun `q-footer` à fixer ou non), mais passer en minuscule par cohérence/lisibilité : `view="lHh Lpr lff"`.
- Recalculer le `min-height` de `#main-content` : la seule hauteur fixe restante à soustraire de `100vh` est celle du header (`q-toolbar`). Nouvelles valeurs proposées :
  ```css
  #main-content {
    min-height: calc(100vh - 64px);
  } /* desktop */
  @media (max-width: 599px) {
    #main-content {
      min-height: calc(100vh - 56px);
    } /* mobile */
  }
  ```
  Ces valeurs de 64px/56px reprennent celles déjà codées en dur pour le `q-toolbar` dans `AppHeader.vue` (lignes 226 et 233, `min-height: 64px`/`56px`) et sont probablement correctes, mais le développeur doit **vérifier visuellement** (desktop + mobile, media query `max-width: 599px`) la hauteur réelle rendue du header après suppression du footer, plutôt que de faire confiance à ces valeurs sans contrôle — en particulier vérifier qu'aucune bande vide ne subsiste en bas de page ET qu'aucune barre de défilement verticale parasite n'apparaît sur une page courte (le `min-height` interagissant avec le décalage de hauteur déjà appliqué par `q-page-container` sous le header).

### 3. `src/components/AppHeader.vue` — version dans le menu compte

Ajouter la version de l'application dans le `q-list` du `q-btn-dropdown` (lignes 55-106), comme élément non cliquable, texte discret (`text-caption`/`text-grey`), positionné à la suite du bloc info utilisateur (lignes 57-69) ou juste avant « Déconnexion » — au choix du développeur, cohérent visuellement avec le bloc info existant. Exemple :

```vue
<q-item>
  <q-item-section>
    <q-item-label caption class="text-grey">Version {{ settings.app.version }}</q-item-label>
  </q-item-section>
</q-item>
```

- Importer `settings` depuis `src/settings` (`import { settings } from 'src/settings';`), suivant le pattern déjà utilisé dans `ProfilePage.vue`.
- Pas de `clickable`, pas de handler `@click`.
- Libellé en français codé en dur (« Version X.Y.Z »), pas de clé i18n (cf. « Hors périmètre »).

### 4. Documentation à mettre à jour dans la même tâche

- `.claude/docs/architecture.md` (ligne 56) : retirer `AppFooter.vue` de la liste des « Composants racine notables ».
- `.claude/specifications/README.md` : remplacer la mention « Ce dossier est vide pour l'instant » par une référence à cette spec.
- `CLAUDE.md` du package : dans la section « Documentation », remplacer « voir `.claude/specifications/` (vide pour l'instant) » par une phrase référençant cette spec (sans en dupliquer le contenu), en cohérence avec le format utilisé pour les autres références de cette section.

## Cas limites

- Aucune page de l'application ne doit conserver de référence résiduelle à `AppFooter` après suppression (import mort, composant enregistré globalement, etc.) — vérifié aujourd'hui : seuls `MainLayout.vue` (import + usage) et `architecture.md` (mention documentaire) référencent ce composant, aucun autre fichier de test ou de configuration ne le mentionne.
- `settings.app.version` peut être une chaîne vide ou `undefined` si `VITE_APP_VERSION` n'est pas défini à la compilation (cas déjà possible avant ce changement, comportement inchangé) : afficher « Version » suivi d'une chaîne vide n'est pas un cas à corriger dans le cadre de cette spec — la variable d'environnement est déjà renseignée dans tous les environnements connus (`.env`, `.env.production`, `.env.staging`, `.env.example`, build args Docker).
- Le menu compte (`q-btn-dropdown`) doit rester scrollable/lisible avec l'ajout de cette ligne supplémentaire ; aucun changement de `min-width: 200px` (ligne 55) n'est requis a priori, mais à vérifier visuellement si le libellé de version est plus long que prévu.

## Critères d'acceptation

1. Le fichier `src/components/AppFooter.vue` n'existe plus dans le repository.
2. Aucune occurrence de `AppFooter` ni de `<q-footer` ne subsiste dans `src/` du package `recruiter-app` (vérifiable par recherche texte).
3. `MainLayout.vue` : le prop `view` de `q-layout` reste un format valide (3 groupes de 3 lettres), sans `<app-footer />` dans le template.
4. Sur n'importe quelle page de l'application (desktop et mobile, `max-width: 599px`), le contenu principal (`#main-content`) occupe toute la hauteur disponible sous le header, sans bande vide/espace réservé en bas anciennement occupé par le footer, et sans barre de défilement verticale parasite introduite par le nouveau `min-height`.
5. Le menu compte utilisateur (`AppHeader.vue`, icône `account_circle`) affiche la version de l'application sous la forme « Version {{ settings.app.version }} » (valeur issue de `VITE_APP_VERSION`), en texte non cliquable, sans action au clic.
6. `.claude/docs/architecture.md`, `.claude/specifications/README.md` et `CLAUDE.md` du package sont mis à jour en cohérence avec les changements ci-dessus (plus de mention de `AppFooter.vue` en composant racine, plus de mention « vide pour l'instant » pour le dossier de specs).
7. Aucune régression d'accessibilité : le footer supprimé portait `role="contentinfo"` et des landmarks `nav` — aucune règle RGAA/WCAG (`eslint-plugin-vuejs-accessibility`, actif en erreur sur ce projet) n'impose la présence d'un landmark `contentinfo`, et la navigation `nav` du footer dupliquait déjà celle de la sidebar ; sa suppression n'introduit pas de nouvelle erreur de lint accessibilité (`npm run lint` doit rester au vert).
8. Vérification visuelle manuelle sur au moins une page de contenu court (ex. tableau de bord) et une page de contenu long (ex. liste paginée) : pas de régression de mise en page dans les deux cas, desktop et mobile.

## Points à confirmer

Aucun point bloquant identifié : toutes les décisions nécessaires ont été validées en amont (suppression complète sans remplacement, réutilisation de `settings.app.version` sans nouveau mécanisme, emplacement du libellé de version au choix du développeur dans le menu compte). Aucun `[À CONFIRMER]` n'est nécessaire pour cette spec.
