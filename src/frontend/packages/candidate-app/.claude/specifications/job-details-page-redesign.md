# Refonte visuelle de la page détail d'offre (`JobDetailsPage.vue`)

## Contexte / constat

Fichier concerné : `src/pages/JobDetailsPage.vue` (route `jobs/:id`, nommée `JobDetails`, accessible publiquement sans authentification — `http://localhost:3000/#/jobs/<id>`).

Capture d'écran fournie par l'utilisateur : sur desktop/large viewport, la page affiche tout son contenu (bandeau titre en dégradé, puis cards "Description du poste", "Exigences", "Avantages", puis un bloc CTA "Intéressé par cette offre ? Connectez-vous pour candidater", puis "Informations") en une seule colonne étroite alignée à gauche, laissant une large zone vide inutilisée à droite.

### Diagnostic technique (vérifié dans le code)

Le template contient déjà, en apparence, une structure à deux colonnes :

```html
<div class="job-content q-pa-lg">
  <div class="row q-gutter-lg">
    <div class="col-12 col-md-8"><!-- contenu principal --></div>
    <div class="col-12 col-md-4"><!-- sidebar --></div>
  </div>
</div>
```

Le bug identifié est la classe `q-gutter-lg` (ligne ~52) utilisée à la place de `q-col-gutter-lg`. En Quasar :

- `.q-gutter-x-*` applique une `margin-left` négative sur la ligne (`row`) et une `margin-left` positive équivalente sur **chacun** de ses enfants directs (`> *`).
- `.q-col-gutter-x-*` applique la même `margin-left` négative sur la ligne, mais compense en `padding-left` sur les enfants, ce qui n'affecte pas leur largeur en pourcentage.

Avec `col-12 col-md-8` (66,667 %) et `col-12 col-md-4` (33,333 %) qui totalisent déjà 100 % de la largeur de la ligne, l'ajout d'une `margin-left` supplémentaire sur chaque colonne via `q-gutter-lg` fait déborder leur largeur cumulée au-delà de 100 %, ce qui provoque un retour à la ligne du flexbox (`row` a `flex-wrap: wrap` par défaut chez Quasar) : la colonne "sidebar" (`col-md-4`) est repoussée sur sa propre ligne, sous la colonne principale, au lieu de s'afficher à côté. C'est la cause la plus probable du rendu "une seule colonne étroite avec zone vide à droite" observé dans la capture, y compris à partir du breakpoint `md` de Quasar (≥ 1024px) où le layout est censé passer en deux colonnes.

**Correctif à vérifier/appliquer en premier lieu** : remplacer `class="row q-gutter-lg"` par `class="row q-col-gutter-lg"` sur le conteneur de la section "Content" (ligne ~52 du fichier actuel). Le développeur doit confirmer visuellement (à ≥ 1024px de large) que les deux colonnes s'affichent bien côte à côte après ce changement avant de poursuivre le reste de cette spec ; si le rendu ne se corrige pas entièrement avec ce seul changement, investiguer plus avant (mais ce diagnostic est la cause la plus probable identifiée par lecture du CSS Quasar réellement embarqué).

### Comportement mobile actuel (à conserver)

`col-12 col-md-8` / `col-12 col-md-4` : en dessous du breakpoint `md` de Quasar (< 1024px, donc `xs`/`sm`), les deux blocs sont déjà en `col-12` (pleine largeur, empilés verticalement). Ce comportement mobile/tablette est correct et ne doit **pas** être modifié par cette spec — seul le rendu à partir de `md` (≥ 1024px) est concerné par la refonte.

## Décision produit (déjà tranchée par l'utilisateur)

Adopter un layout à deux colonnes sur desktop (≥ breakpoint `md` Quasar), inspiré des pages d'offre d'emploi standard du marché (LinkedIn, Indeed) :

- **Colonne principale (gauche, large, `col-md-8` conservé)** : description du poste, exigences, avantages (contenu et ordre inchangés).
- **Colonne secondaire (droite, étroite, `col-md-4` conservé, sticky au scroll)** : regroupe le bloc CTA de candidature et le bloc "Informations" (aujourd'hui deux `q-card` empilées dans la même colonne — déjà le cas dans le code actuel, à conserver dans cet ordre : CTA puis Informations).

### Vérification "redondance chips bandeau / bloc Informations"

Vérifié dans le code : **aucune redondance de champ n'existe actuellement** entre les chips du bandeau (`location`, `workMode`, `contractType`) et le bloc "Informations" de la sidebar (`salaryMin`/`salaryMax`/`salaryCurrency`, `publishedAt`/`createdAt`, `expiresAt`, `status`). Les deux blocs affichent des informations complémentaires, pas dupliquées. Aucune suppression de champ n'est donc nécessaire à ce titre ; cette vérification est documentée ici pour lever l'hypothèse de redondance envisagée en amont, mais ne débouche sur aucune action.

### Sticky sidebar

La colonne secondaire doit rester visible au scroll (`position: sticky`) à partir du breakpoint desktop uniquement. Points d'attention à respecter :

- Le décalage (`top`) doit tenir compte du header fixe de l'application (`AppHeader`, hauteur 64px desktop / 56px mobile selon `MainLayout.vue`), pour que le contenu sticky ne soit pas masqué sous le header au scroll.
- Le comportement sticky n'a d'effet visible que si le contenu de la colonne secondaire est plus court que la hauteur de la fenêtre ; si le CTA + Informations (+ éventuels ajouts, voir ci-dessous) dépassent la hauteur visible, la colonne défilera normalement jusqu'à son bas puis restera collée — comportement standard, à ne pas sur-ingénierer (pas de scroll interne dans la sidebar).
- Ne pas appliquer le sticky en dessous du breakpoint desktop (mobile/tablette restent en flux normal, colonne unique).

## Pistes d'enrichissement explorées (données déjà disponibles, sans changement backend)

Le DTO `JobOfferDto` (`src/models/job.ts`) expose : `id`, `title`, `description`, `requirements`, `benefits`, `location`, `workMode`, `contractType`, `salaryMin/Max/Currency`, `status`, `publishedAt`, `expiresAt`, `createdAt`, `updatedAt`, `organizationId`, `organizationName`, `createdByUserId/Name`, `isActive`, `isExpired`, `applicationsCount`.

### Proposition 1 — "Autres offres de cette entreprise" (retenue)

L'endpoint `GET /JobOffers/organization/{organizationId}` (public, sans `[Authorize]`, vérifié côté `JobOffersController.cs`) est déjà exposé côté frontend via `jobOfferService.getJobOffersByOrganization(organizationId)`, mais n'est utilisé nulle part dans `candidate-app` actuellement. Il retourne **toutes** les offres de l'organisation, sans filtre de statut (vérifié dans `JobOfferService.GetJobOffersByOrganizationAsync`, MonolithApi : pas de `.Where` sur `status`/`isActive`/`isExpired`).

Proposition : ajouter, en bas de la colonne principale (sous "Avantages"), un bloc "Autres offres de cette entreprise" listant jusqu'à 3-4 autres offres de la même organisation, avec un filtrage **côté frontend** (le backend ne filtre pas) :

- exclure l'offre actuellement affichée (`id !== currentJobOffer.id`) ;
- ne garder que les offres publiées/actives/non expirées, avec exactement le même critère que le `computed publishedJobOffers` déjà défini dans `jobOfferStore.ts` (`job.status === JobOfferStatus.Published && job.isActive && !job.isExpired`, en important l'enum `JobOfferStatus` — ne pas comparer à la chaîne littérale `'Published'`) ;
- limiter à N éléments (proposition : 3), triés par date de publication décroissante (l'API renvoie déjà par `CreatedAt` décroissant, suffisant) ;
- ne pas afficher le bloc si la liste filtrée est vide.

Ne pas nommer ce bloc "offres similaires" (le service ne fait aucun matching par compétences/poste, seulement par organisation) — le nom "Autres offres de cette entreprise" reflète exactement ce que fait l'endpoint.

Chaque offre de ce bloc peut être un lien simple (titre + lieu + type de contrat) vers `/jobs/:id`, sans réutiliser `JobCard.vue` en entier (trop large pour un bloc secondaire) — un composant/markup plus compact suffit, au choix du développeur.

### Proposition 2 — Bouton de partage (retenue, pure UI)

Ajouter un bouton "Partager" dans la colonne secondaire (icône `share`), sans dépendance backend :

- utiliser l'API native `navigator.share` si disponible (mobile principalement), avec fallback copie du lien de l'offre dans le presse-papiers (`navigator.clipboard.writeText`) sinon, accompagné d'une notification de confirmation (action explicite de l'utilisateur, conforme au principe retenu dans `prune-non-actionable-notifications.md`).
- Aucune donnée backend nouvelle requise (l'URL de la page courante suffit).

### Proposition 3 — Date de publication en relatif (retenue, pure UI)

`JobDetailsPage.vue` définit sa propre fonction locale `formatDate` (format absolu complet, "DD MMMM YYYY"), différente de la fonction déjà partagée `formatDate` de `src/helpers/DateHelper.ts` (format relatif : "aujourd'hui" / "hier" / "il y a X jours" / "il y a X semaines" / date courte au-delà) — cette dernière est déjà utilisée par `JobCard.vue`. Remplacer l'usage local de `JobDetailsPage.vue` par le helper partagé pour la ligne "Date de publication" du bloc "Informations", par cohérence avec l'affichage déjà vu par le candidat sur la liste d'offres. Attention : `JobDetailsPage.vue` définit déjà une fonction locale nommée `formatDate` (même nom que l'export de `DateHelper.ts`) — importer le helper partagé sans renommer/supprimer la fonction locale provoquerait une collision de nom (échec de compilation TypeScript). Renommer ou supprimer la fonction locale : la conserver uniquement si elle reste utilisée pour la date d'expiration (format absolu complet), auquel cas la renommer (ex. `formatFullDate`, qui existe déjà dans `DateHelper.ts` avec un format quasi identique — vérifier si elle peut être réutilisée directement plutôt que dupliquée).

### Proposition écartée — Nombre de candidatures reçues (`applicationsCount`)

Vérifié : `applicationsCount` est bien peuplé sur l'endpoint public `GET /JobOffers/{id}` (mapping `ApplicationsCount = src.Applications.Count` dans `JobOfferMappingProfile.cs`, avec `.Include(jo => jo.Applications)` dans `GetJobOfferByIdAsync`, endpoint sans restriction `[Authorize]`). La donnée existe donc et est déjà transmise au frontend.

**Question produit non tranchée** : faut-il afficher ce chiffre au candidat (ex. "12 candidatures déjà reçues") ? C'est un choix éditorial, pas une contrainte technique :

- Pour : effet de preuve sociale, pratique déjà vue sur certains jobboards (LinkedIn affiche parfois "plus de 100 candidats").
- Contre : peut décourager un candidat face à une forte concurrence perçue ; cette donnée est habituellement réservée aux vues recruteur dans ce type de plateforme (à vérifier côté `recruiter-app`, hors périmètre de cette spec) ; l'exposer publiquement aux candidats est un choix produit qui dépasse la simple question de faisabilité technique.

**[À CONFIRMER]** : afficher ou non `applicationsCount` sur cette page, et si oui sous quelle forme (nombre exact, tranche "10+", etc.). Recommandation par défaut en l'absence de réponse : **ne pas l'afficher** (aligné sur les pratiques les plus prudentes des ATS destinés aux candidats), le bloc "Autres offres de cette entreprise" et le bouton de partage suffisant à occuper la colonne secondaire sans cette donnée.

### Pistes explicitement hors périmètre (nécessitent un changement backend ou n'existent pas ailleurs dans le code)

- **Informations sur l'organisation/employeur (logo, description, site web, taille)** : aucun modèle ni service `Organization` n'existe côté `candidate-app` (seuls `organizationId`/`organizationName` sont disponibles via `JobOfferDto`). Nécessiterait un nouvel endpoint public "profil organisation" côté MonolithApi — hors périmètre, à spécifier séparément côté backend si souhaité.
- **Bouton "Sauvegarder l'offre" (bookmark/favoris)** : vérifié, cette fonctionnalité n'existe nulle part ailleurs dans `candidate-app` (aucune trace de favoris/bookmark dans le code, ni de endpoint dédié côté service). Conformément à la consigne de ne proposer cet ajout que s'il existe déjà ailleurs, il n'est **pas proposé** dans le périmètre de cette spec. Une version purement `localStorage` (sans backend) serait techniquement possible mais introduirait une fonctionnalité entièrement nouvelle plutôt qu'un enrichissement de la page existante — à traiter, si souhaité, dans une spec dédiée avec son propre arbitrage produit (persistance multi-appareil ou non, etc.).

## Modifications attendues (récapitulatif fichiers)

`src/pages/JobDetailsPage.vue` :

1. Remplacer `class="row q-gutter-lg"` par `class="row q-col-gutter-lg"` sur le conteneur de section content (correctif du bug de layout).
2. Ajouter `position: sticky` (avec `top` tenant compte du header fixe) sur la colonne secondaire (`col-12 col-md-4`), actif uniquement à partir du breakpoint desktop.
3. Ajouter un bloc "Autres offres de cette entreprise" en bas de la colonne principale (nouvel appel à `jobOfferService.getJobOffersByOrganization` ou nouvelle action de store, filtrage côté frontend décrit ci-dessus).
4. Ajouter un bouton de partage dans la colonne secondaire (`navigator.share` avec fallback presse-papiers).
5. Remplacer le `formatDate` local (date de publication) par le helper partagé `src/helpers/DateHelper.ts` pour cohérence avec `JobCard.vue` ; conserver un format absolu pour la date d'expiration.
6. `[À CONFIRMER]` : ajout ou non de `applicationsCount` — ne pas implémenter tant que non tranché (voir recommandation par défaut ci-dessus).

Selon l'implémentation retenue pour le point 3 (nouvel appel API), possibilité d'ajouter une action dédiée dans `jobOfferStore.ts` (ex. `fetchOrganizationJobOffers`) plutôt que d'appeler `jobOfferService` directement depuis la page — au choix du développeur, à condition de respecter le principe de notifications de `prune-non-actionable-notifications.md` si cette nouvelle action est ajoutée au store (pas de notification de succès pour ce chargement en arrière-plan).

## Hors périmètre

- Toute modification de `JobCard.vue` ou `JobListingsPage.vue` (traité par `wire-apply-button-job-card.md`).
- Toute modification du principe de notifications (traité par `prune-non-actionable-notifications.md`).
- Nouvel endpoint backend "profil organisation" ou "bookmark/favoris" (voir sections ci-dessus).

## Remarque transversale (constatée, non traitée ici)

`JobListingsPage.vue` utilise le même anti-pattern Quasar (`class="row q-gutter-md"` sur la grille de cards, avec des colonnes `col-12 col-md-6 col-lg-4`), ce qui probablement réduit le nombre de cards affichées par ligne sur grand écran (2 au lieu de 3 attendues en `lg`) pour la même raison de calcul de largeur que celle diagnostiquée ci-dessus. Ce point est **explicitement hors périmètre** de cette spec (qui ne couvre que `JobDetailsPage.vue`) mais mérite sa propre spec/correctif si confirmé visuellement.

## Critères d'acceptation

1. À une largeur d'écran ≥ 1024px (breakpoint `md` Quasar), la page détail d'offre affiche la colonne principale (description/exigences/avantages) et la colonne secondaire (CTA + Informations) côte à côte sur une même ligne, sans zone vide significative à droite.
2. En dessous de 1024px, le rendu reste inchangé : une seule colonne, blocs empilés dans l'ordre actuel.
3. En scrollant sur desktop, la colonne secondaire (CTA + Informations, + ajouts éventuels) reste visible à l'écran (sticky) sans jamais passer sous le header fixe de l'application, tant que son contenu total est plus court que la hauteur visible de la fenêtre.
4. Un bloc "Autres offres de cette entreprise" apparaît sous "Avantages" uniquement si l'organisation a au moins une autre offre publiée/active/non expirée que celle affichée ; il n'affiche jamais l'offre courante ni une offre non publiée/inactive/expirée.
5. Un bouton de partage est présent dans la colonne secondaire ; son clic déclenche le partage natif si disponible, sinon copie le lien de la page dans le presse-papiers avec confirmation visuelle.
6. La date de publication affichée dans "Informations" utilise le même format relatif que celui déjà vu sur la liste d'offres (`JobCard.vue`), pour une même offre.
7. Aucune régression sur le flux de candidature existant (CTA "Postuler", états non-authentifié / déjà postulé / candidature envoyée) ni sur le contenu des sections "Description", "Exigences", "Avantages".

## Points à confirmer

- Affichage ou non de `applicationsCount` ("X candidatures reçues") — voir section dédiée ci-dessus ; recommandation par défaut : ne pas l'afficher tant que non tranché.
