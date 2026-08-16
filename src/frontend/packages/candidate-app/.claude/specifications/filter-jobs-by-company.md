# Filtre par entreprise (dropdown) sur la liste des offres d'emploi

## Contexte

Fichier concerné : `src/pages/JobListingsPage.vue` (page liste des offres, route `/jobs` ou équivalente). Il n'existe pas de composant de filtres séparé : la recherche et les filtres avancés (Localisation, Mode de travail, Type de contrat, boutons Appliquer/Réinitialiser) sont codés en inline dans ce fichier, dans un panneau repliable (`showFilters`, `q-slide-transition`).

Aujourd'hui, le candidat peut filtrer par titre (`searchTerm`), localisation, mode de travail et type de contrat, mais pas par entreprise. Cette spec ajoute un quatrième filtre « Entreprise » sous forme de `q-select`, dans le même panneau.

## Décisions produit déjà tranchées (ne pas rouvrir)

1. **Emplacement** : dans le panneau de filtres avancés existant de `JobListingsPage.vue`, à côté de Localisation/Mode de travail/Type de contrat.
2. **Source des données** : aucune entreprise saisie librement ni nouveau modèle/service `Organization` côté frontend. La liste déroulante n'affiche que les entreprises **ayant au moins une offre publiée/active/non expirée**, dérivées **côté frontend** à partir des offres, par `organizationId` (libellé `organizationName`) — pas de nouvel endpoint backend.

## Constat technique déterminant (vérifié dans le code)

### `JobListingsPage.vue` utilise une pagination server-side, pas un filtrage client

`jobOfferStore.fetchJobOffers(filter?)` appelle `jobOfferService.getAllPaginatedJobOffers(filter)` → `GET /JobOffers/paginated`, avec un filtre (`JobOfferFilterDto`) envoyé au backend (`BuildJobOfferQuery` applique `Title`, `Location`, `WorkMode`, `ContractType`, `Status`, `OrganizationId`, `IsActive`, `IsExpired`, etc. comme des `.Where` explicites — vérifié dans `Services/JobOfferService.cs`, méthode `BuildJobOfferQuery`). `jobOffers` (le state affiché par la page) ne contient donc **que la page courante** (10 éléments par défaut, `paginationInfo.pageSize`). **Dériver la liste des entreprises de `jobOffers` serait incorrect** : elle ne refléterait que les entreprises présentes sur la page actuellement affichée, et se réduirait en plus à mesure que l'utilisateur filtre/pagine — l'exact anti-pattern à éviter (voir critère d'acceptation dédié plus bas).

### Le champ `organizationId` existe déjà de bout en bout — aucun changement backend requis

- Backend : `DTOs/JobOffer/JobOfferFilterDto.cs` expose déjà `public Guid? OrganizationId { get; set; }`, appliqué dans `BuildJobOfferQuery` (`if (filter.OrganizationId.HasValue) query = query.Where(jo => jo.OrganizationId == filter.OrganizationId);`).
- Frontend : `src/models/job.ts`, `JobOfferFilterDto` expose déjà `organizationId?: string`.
- `jobOfferService.getAllPaginatedJobOffers` sérialise génériquement chaque clé du filtre dans `URLSearchParams` (`Object.entries(filter).forEach(...)`) — aucune modification de service nécessaire. Le binding de modèle ASP.NET Core est insensible à la casse sur les query strings (`organizationId` → `OrganizationId`), déjà éprouvé par les autres filtres (`location`, `workMode`, `contractType`).

**Conclusion : le filtrage par entreprise (envoi du paramètre au backend) ne nécessite aucune plomberie nouvelle.** Seule la **source des options de la dropdown** demande un mécanisme dédié (voir ci-dessous).

### Source des options de la dropdown : pourquoi `GET /JobOffers` (sans filtre) est écarté

Le service exposait déjà `jobOfferService.getAllJobOffers()` → `GET /JobOffers` (`JobOffersController.GetAllJobOffers`), public (pas de `[Authorize]`), sans aucun paramètre de filtre. Vérification du code réel (`JobOfferService.cs`, méthode `ApplyUserBasedFilteringAsync`, ligne ~568) :

```csharp
if (!_currentUserService.UserId.HasValue)
{
    return query; // le commentaire au-dessus dit "return empty query" mais le code renvoie la requête NON filtrée
}
```

Pour un visiteur non authentifié (cas normal de consultation publique de `JobListingsPage.vue`), cette méthode est un **no-op** : elle ne restreint ni par statut, ni par organisation, contrairement à ce que son commentaire laisse penser. `GetAllJobOffers()` n'appliquant lui-même aucun filtre explicite de statut (contrairement à `GetAllPaginatedJobOffersAsync`, qui applique `BuildJobOfferQuery(filter)` **avant** `ApplyUserBasedFilteringAsync`), l'utiliser pour peupler la dropdown ferait apparaître des entreprises dont **aucune offre n'est publiée** (brouillons, offres closes/expirées uniquement) — contraire à la décision produit n°2. **Cet endpoint est donc écarté pour cet usage.**

### Mécanisme retenu

Réutiliser l'endpoint paginé existant (`GET /JobOffers/paginated`), avec un filtre explicite dédié à la dropdown, indépendant du filtre/de la pagination affichés à l'écran :

```ts
{
  pageNumber: 1,
  pageSize: 100,       // plafond serveur (JobOfferFilterDtoValidator : LessThanOrEqualTo("100"))
  status: 'Published',
  isActive: true,
  isExpired: false,
  // pas d'organizationId
}
```

- `pageSize: 100` est le **plafond serveur maximal autorisé** (`JobOfferFilterDtoValidator.PageSize.LessThanOrEqualTo("100")`) : impossible de demander plus en un seul appel sans backend dédié, ce qui est explicitement hors périmètre (décision produit n°2).
- **Limitation connue à documenter** (pas à corriger dans cette spec) : si plus de 100 offres publiées/actives/non expirées existent simultanément, les entreprises dont toutes les offres tombent au-delà des 100 premières (triées par `CreatedAt` décroissant, tri par défaut) n'apparaîtront pas dans la dropdown. Sans impact avec le volume de données actuel (~30 offres de démonstration, cf. `seed-demo-organizations-users-joboffers.md` côté backend) ; à réévaluer (nouvel endpoint dédié côté backend, hors périmètre ici) si le catalogue grossit significativement.

## Spécification technique

### `jobOfferStore.ts` — nouvel état et nouvelle action, isolés de l'état de pagination affiché

Ajouter, **sans toucher** à `jobOffers`, `paginationInfo`, ni `currentFilter` (ces trois états pilotent la liste affichée et sa pagination — les réutiliser pour la dropdown écraserait la page affichée, cf. `fetchJobOffers` qui réassigne les trois) :

- Un nouvel état, par exemple `companyFilterSourceOffers: ref<JobOfferDto[]>([])`.
- Un nouveau computed, par exemple `companyFilterOptions`, dérivant les entreprises distinctes de `companyFilterSourceOffers` :
  - filtrer défensivement avec le même critère que `publishedJobOffers` déjà défini dans ce store (`job.status === JobOfferStatus.Published && job.isActive && !job.isExpired`, en important l'enum `JobOfferStatus` — pas la chaîne littérale `'Published'`) ;
  - dédoublonner par `organizationId` ;
  - libellé = `organizationName` ;
  - trier alphabétiquement (`localeCompare` avec locale française) ;
  - forme de sortie attendue par `q-select` (`emit-value`/`map-options`, cohérent avec `workModeOptions`/`contractTypeOptions` déjà présents dans `JobListingsPage.vue`) : `{ label: string; value: string }[]`.
- Une nouvelle action, par exemple `fetchCompanyFilterOptions(): Promise<void>` :
  - appelle `jobOfferService.getAllPaginatedJobOffers({ pageNumber: 1, pageSize: 100, status: 'Published', isActive: true, isExpired: false })` ;
  - **silencieuse en cas de succès comme d'échec** (aucune notification, ni succès ni erreur) — alignée sur le précédent déjà établi dans ce même store pour `fetchJobOffersByOrganization` (chargement en arrière-plan, cf. commentaire existant « volontairement silencieux, sans notification de succès/erreur (cf. prune-non-actionable-notifications.md) »), et **non** sur le précédent de `fetchJobOffers` (qui affiche un toast d'erreur car il pilote directement le résultat visible à l'écran). Le validator ne doit pas considérer l'absence de toast d'erreur ici comme un écart par rapport à `prune-non-actionable-notifications.md` : les deux précédents coexistent déjà dans ce store selon la nature du chargement (résultat visible vs enrichissement d'arrière-plan) ;
  - **ne doit appeler ni `setLoading`, ni `clearError`, ni `setError`** — ces trois helpers pilotent `isLoading`/`error`, lus par `JobListingsPage.vue` (`v-if="isLoading"` gate toute la section résultats, `hasError` affiche le `q-banner` rouge). `fetchJobOffersByOrganization` (le précédent cité juste au-dessus) ne les appelle déjà pas non plus, pour la même raison. Si `fetchCompanyFilterOptions` les appelait (en copiant le patron de `fetchJobOffers`), deux régressions concrètes apparaîtraient : (a) sous le `Promise.all([loadJobOffers(), fetchCompanyFilterOptions()])` prescrit plus bas, le premier des deux appels à se résoudre repasserait `isLoading` à `false` dans son `finally` pendant que l'autre est encore en cours, affichant un « Aucune offre trouvée » transitoire incorrect ; (b) un échec du chargement des options ferait basculer toute la page sur le bandeau d'erreur rouge, remplaçant la liste d'offres, alors que le comportement attendu est une dropdown vide/désactivée sans rien changer d'autre à l'écran ;
  - en cas d'échec (réponse non `isSuccess`, ou exception), fixer uniquement `companyFilterSourceOffers.value = []` (dégrade vers une dropdown vide/désactivée, sans erreur bloquante, sans toucher `isLoading`/`error`).
- Exposer `companyFilterSourceOffers` (ou seulement `companyFilterOptions`, au choix du développeur si `companyFilterSourceOffers` n'a pas d'utilité hors du store), `companyFilterOptions` et `fetchCompanyFilterOptions` dans le `return` du store.

### `JobListingsPage.vue`

- Ajouter `filters.value.companyId: string | null` (nommage libre, ex. `companyId` ou `organizationId` — cohérent avec le champ backend si choisi `organizationId`).
- Ajouter un `q-select` « Entreprise » dans le panneau de filtres, propriétés identiques aux selects existants (`filled`, `clearable`, `emit-value`, `map-options`), `options` liées à `jobOfferStore.companyFilterOptions` (via `storeToRefs`).
- `handleFilter` : ajouter `organizationId: filters.value.companyId || undefined` dans `filterData`, **toujours présent** dans l'objet (même à `undefined` si aucune sélection) — indispensable pour que la désélection écrase effectivement un `organizationId` précédemment appliqué dans `currentFilter` du store (le spread `{...currentFilter.value, ...filter}` n'écrase une clé que si elle est présente dans l'objet, même avec une valeur `undefined` ; omettre complètement la clé laisserait l'ancien filtre actif).
- `resetFilters` : remettre `filters.value.companyId = null` en plus des trois champs déjà réinitialisés.
- `onMounted` : déclencher `jobOfferStore.fetchCompanyFilterOptions()` en parallèle de `loadJobOffers()` (ex. `Promise.all([...])` ou deux appels indépendants), **une seule fois** au montage de la page — ne pas la relancer sur `handleFilter`, `handlePageChange`, `resetFilters` ni `handleSearch` (la liste d'entreprises doit rester stable pendant que l'utilisateur filtre/pagine la liste affichée).
- q-select désactivé (`disable`) quand `companyFilterOptions.length === 0` — pas de message d'erreur, pas de notification, juste un select non interactif.

### Layout — éviter le bug déjà diagnostiqué (`q-gutter-*` vs `q-col-gutter-*`)

Le panneau de filtres avancés utilise actuellement `class="row q-gutter-md"` avec 4 colonnes enfants (`col-12 col-md-3` × 3 selects + 1 colonne boutons `col-12 col-md-3`) totalisant déjà 100 % de la largeur de la ligne. `q-gutter-md` ajoute une `margin-left` positive sur **chaque** enfant en plus de leur largeur en `%`, ce qui fait déborder le total et provoque un retour à la ligne (même mécanisme exact que le bug diagnostiqué et corrigé dans `job-details-page-redesign.md`, `q-gutter-lg` vs `q-col-gutter-lg`). Ajouter un 5ème enfant (`col-md-3` pour « Entreprise ») sans corriger ce point ferait immanquablement passer le total à 125 % et casser l'alignement en une seule ligne à partir du breakpoint `md`.

**Restructuration attendue** (attention : `class="row q-gutter-md"` apparaît deux fois dans le fichier actuel — une fois ligne ~10 pour la ligne recherche + bouton « Filtres » (`col` + `col-auto`, total < 100 %, où `q-gutter-md` est inoffensif et ne doit **pas** être touché), une fois ligne ~42 pour le panneau de filtres avancés (les 4 colonnes à `col-md-3`, où le bug s'applique). Seule la seconde occurrence — celle du panneau de filtres, celle qui contient les `q-select` — est concernée par ce qui suit) :

1. Remplacer `class="row q-gutter-md"` par `class="row q-col-gutter-md"` uniquement sur la ligne des filtres du panneau (`.filters-panel`, contenant les `q-select` Localisation/Mode de travail/Type de contrat), pas sur la ligne recherche + bouton « Filtres ».
2. Garder les 4 selects (Localisation, Mode de travail, Type de contrat, **Entreprise**) en `col-12 col-md-3` sur cette même ligne (4 × 25 % = 100 %).
3. Sortir les boutons « Appliquer »/« Réinitialiser » de cette ligne, dans une ligne dédiée en dessous (ex. `<div class="row q-mt-md"><div class="col-12 flex justify-end">...</div></div>`), alignement au choix du développeur (cohérent avec le reste de la page).

## Comportement fonctionnel attendu

- Le filtre Entreprise se comporte comme les trois filtres existants : appliqué uniquement au clic sur « Appliquer » (pas au changement de sélection), combiné avec la recherche par titre et les autres filtres actifs.
- Aucune sélection = toutes les entreprises (comportement identique à aujourd'hui, aucun paramètre `organizationId` envoyé).
- « Réinitialiser » réinitialise la sélection Entreprise en même temps que les autres filtres et recharge la liste non filtrée. Le `resetFilters()` du store reconstruit déjà `currentFilter` sans `organizationId` — aucun changement requis côté store sur ce point, seule la partie page (`filters.value.companyId = null`) doit être ajoutée.
- Une seule entreprise disponible : le select reste actif, sans traitement spécial (affiche cette unique option).
- Aucune entreprise disponible (aucune offre publiée/active/non expirée) : select désactivé, aucune notification.
- Aucun toast de succès sur le chargement des offres ni sur le chargement des options de la dropdown (conforme à `prune-non-actionable-notifications.md` et au point ci-dessus sur le précédent `fetchJobOffersByOrganization`).

## Hors périmètre

- Aucun nouveau modèle/service `Organization` côté frontend.
- Aucun nouvel endpoint, ni modification de `JobOfferFilterDto`/`JobOfferFilterDtoValidator`/`JobOffersController`/`JobOfferService` côté backend.
- Pas de résolution de la limitation des 100 offres (nouvel endpoint dédié « organisations distinctes ayant des offres publiées ») : documentée comme limitation connue, pas traitée ici.

## Critères d'acceptation

1. Un `q-select` « Entreprise » est présent dans le panneau de filtres avancés de `JobListingsPage.vue`, aux côtés de Localisation/Mode de travail/Type de contrat.
2. Les options de ce select correspondent aux entreprises distinctes (par `organizationId`, libellé `organizationName`) ayant au moins une offre avec `status === JobOfferStatus.Published && isActive && !isExpired`, obtenues via un appel dédié à `GET /JobOffers/paginated` (`pageSize: 100`, `status: 'Published'`, `isActive: true`, `isExpired: false`, sans `organizationId`) — **indépendant** de l'état `jobOffers`/`paginationInfo` qui pilote la liste affichée.
3. Les options sont triées alphabétiquement (ordre français), sans doublon.
4. Sélectionner une entreprise puis cliquer sur « Appliquer » ne modifie **pas** le contenu de la dropdown : toutes les entreprises restent listées (seule la liste des offres affichées se restreint). Vérifie l'absence de l'anti-pattern « dropdown reconstruite depuis le résultat filtré ».
5. Sélectionner une entreprise puis « Appliquer » restreint la liste des offres affichées à cette organisation (`organizationId` transmis à `GET /JobOffers/paginated`), en combinaison correcte avec la recherche par titre et les filtres Localisation/Mode de travail/Type de contrat déjà sélectionnés.
6. Sélectionner une entreprise, cliquer « Appliquer », puis désélectionner (vider le select) et re-cliquer « Appliquer » restaure la liste complète (non filtrée par organisation) — vérifie que la désélection écrase bien un `organizationId` précédemment actif dans le filtre du store.
7. « Réinitialiser » vide la sélection Entreprise (visuellement, `filters.value.companyId === null`) en plus des autres filtres, et recharge la liste non filtrée.
8. Le filtre Entreprise ne s'applique qu'au clic sur « Appliquer », jamais au simple changement de sélection.
9. Avec zéro entreprise disponible, le select est rendu désactivé (`disable`), sans erreur ni notification.
10. Avec une seule entreprise disponible, le select fonctionne normalement (pas de cas particulier de code).
11. Aucune notification de succès ou d'erreur n'est déclenchée par le chargement des options de la dropdown (`fetchCompanyFilterOptions`), qu'il réussisse ou échoue.
    11bis. Un échec (ou une lenteur) du chargement des options de la dropdown n'affecte ni le spinner de chargement (`isLoading`) ni le bandeau d'erreur (`hasError`/`error`) de la liste d'offres affichée : `fetchCompanyFilterOptions` n'appelle ni `setLoading`, ni `clearError`, ni `setError`. Vérifiable en simulant un échec de cet appel : la liste d'offres continue de s'afficher normalement, seule la dropdown Entreprise est vide/désactivée.
12. Aucune modification de fichier backend (`JobOffersController.cs`, `JobOfferService.cs`, `JobOfferFilterDto.cs`, `JobOfferFilterDtoValidator.cs`) n'est apportée par cette fonctionnalité.
13. À une largeur ≥ 1024px (breakpoint `md` Quasar), les quatre selects (Localisation, Mode de travail, Type de contrat, Entreprise) s'affichent côte à côte sur une seule ligne sans retour à la ligne intempestif, suivis des boutons Appliquer/Réinitialiser sur leur propre ligne — la ligne des filtres utilise `q-col-gutter-md` (pas `q-gutter-md`).
14. En dessous de ce breakpoint, le comportement empilé (une colonne) déjà existant pour les autres filtres est conservé pour le nouveau select.
