# Correction de 4 bugs sur la gestion des offres d'emploi (liste + fiche détail)

## Contexte et objectif

Un audit de code a identifié 4 bugs concentrés sur les pages de gestion des offres
d'emploi du back-office (`recruiter-app`), à corriger avant une démo prochaine. Ce
document couvre les 4 en une seule spécification car ils touchent les mêmes
fichiers/composants et partagent le même périmètre fonctionnel (offres d'emploi,
acteurs `Recruiter`/`Manager`/`Organization.Admin`).

Chaque bug a été vérifié dans le code réel (lignes actuelles données ci-dessous,
confirmées au moment de la rédaction — à revérifier par le `developer` si le fichier
a bougé depuis).

Le périmètre du bug 1 a été étendu, à la demande explicite de l'utilisateur
après une première exploration : "Voir les candidatures" ne doit pas
seulement naviguer vers `/applications?jobId=<id>`, mais réellement filtrer
la liste affichée sur cette page pour ne montrer que les candidatures de
l'offre d'origine (voir section dédiée sous le Bug 1).

## Acteurs concernés

Tous les rôles ayant accès à `/jobs` et `/jobs/:id` (recruteurs, managers,
admins d'organisation) — aucun changement de permissions/RBAC dans cette spec,
uniquement des corrections de comportement UI et de contrat de données.

## Bug 1 — "Dupliquer" / "Voir les candidatures" ne font rien depuis la liste des offres

### Constat vérifié

`src/pages/jobs/JobsPage.vue`, lignes 106-114 :

```ts
function duplicateJob(job: JobOffer) {
  console.log('Duplicate job:', job.id);
  // TODO: Implémenter la duplication
}

function viewApplications(job: JobOffer) {
  console.log('View applications for job:', job.id);
  // TODO: Naviguer vers les candidatures
}
```

Ces deux fonctions sont câblées aux événements `@duplicate` et
`@view-applications` émis par `JobList.vue` → `JobCard.vue`/`JobTable.vue` (vue
grille et vue tableau, même contrat d'événements dans les deux composants :
`$emit('duplicate', job)` / `$emit('viewApplications', job)`), mais ne font
rien d'observable pour l'utilisateur.

La même fonctionnalité existe et fonctionne côté fiche détail,
`src/pages/jobs/JobOfferDetailPage.vue` :

- `duplicateJob` (lignes 468-479) : clone l'offre courante (`jobOffer.value`),
  ajoute le suffixe `(Copie)` au titre, supprime `id`, appelle
  `jobStore.createJobOffer(...)`, notifie puis navigue vers `/jobs`.
- `viewApplications` (lignes 456-458) : `router.push('/applications?jobId=' + jobOffer.value?.id)`.

### Règle métier / comportement attendu

Réutiliser cette logique comme référence pour `JobsPage.vue`, avec deux
adaptations nécessaires (la liste n'est pas la fiche détail) :

1. **Dupliquer depuis la liste** (`duplicateJob(job: JobOffer)`) :
   - Construire l'objet de création à partir de `job` (le paramètre reçu par
     l'événement, pas d'un `ref` de page comme sur la fiche détail) :
     `{ ...job, title: `${job.title} (Copie)` }`, supprimer `id` (et toute
     propriété absente de `CreateJobOfferDto` sera de toute façon ignorée
     silencieusement par la désérialisation backend — cf.
     `configurable-salary-currency.md`, aucune action requise ici).
   - Appeler `jobOfferStore.createJobOffer(duplicated as CreateJobOfferDto)`.
   - **Ne pas** ajouter de notification de succès supplémentaire dans la page :
     `jobOfferStore.createJobOffer` affiche déjà en interne
     `notification.showSuccessNotification('Offre créée avec succès')` en cas
     de succès (voir `stores/jobOfferStore.ts`, ligne ~169) — dupliquer cette
     notification produirait un double toast (défaut déjà présent, mais non
     corrigé, dans l'implémentation de référence de la fiche détail ; ne pas le
     reproduire dans le nouveau code).
   - **Vérifier le résultat avant de recharger** : si `createJobOffer` retourne
     une valeur définie (succès), appeler `loadJobs()` pour rafraîchir la liste
     affichée (l'utilisateur reste sur `/jobs`, contrairement à la fiche détail
     qui fait `router.push('/jobs')` après duplication — ici on est déjà sur
     cette page, un simple rechargement de la liste suffit, pas de navigation).
     Si la création échoue (retour `undefined`/`null`), ne rien recharger de
     plus : le store a déjà affiché un toast d'erreur.

2. **Voir les candidatures depuis la liste** (`viewApplications(job: JobOffer)`) :
   - `router.push('/applications?jobId=' + job.id)`, identique au comportement
     de la fiche détail.

### Extension confirmée par l'utilisateur : filtrage réel de `ApplicationsPage.vue` par offre

**Constat vérifié** : `ApplicationsPage.vue` ne lit actuellement le paramètre
de requête `jobId` nulle part (aucune occurrence de `route.query` dans ce
fichier) et n'a pas de filtre `jobOfferId` câblé dans son formulaire de
recherche. Le plumbing existe pourtant de bout en bout et est déjà
fonctionnel côté données :

- Frontend : `ApplicationFilterDto.jobOfferId?: string` déjà présent
  (`src/models/application.ts`, ligne 158).
- Backend : `ApplicationService.cs`, lignes 627-629 —
  `if (filter.JobOfferId.HasValue) { query = query.Where(a => a.JobOfferId == filter.JobOfferId); }`
  — le filtrage par offre est déjà appliqué côté serveur dès que ce champ est
  transmis.

Conséquence actuelle : la navigation fonctionne (on arrive bien sur la page
des candidatures), mais elle n'est **pas filtrée** sur l'offre d'origine —
toutes les candidatures de l'organisation s'affichent. **Confirmé par
l'utilisateur : cette spec doit corriger ce point**, pour les deux points
d'entrée (liste `/jobs` et fiche détail `/jobs/:id`, qui partagent la même
URL cible `/applications?jobId=<id>`).

**Comportement attendu** dans `ApplicationsPage.vue` :

1. **Lecture du paramètre au montage** : importer `useRoute` (déjà utilisé
   ailleurs dans l'app, ex. `JobOfferDetailPage.vue`). Initialiser un état
   `jobOfferIdFilter = ref<string | null>(route.query.jobId as string ?? null)`
   au moment de la déclaration (avant `onMounted`, donc disponible dès le
   premier appel de `refreshData()` dans `onMounted`).
2. **Application du filtre** : dans `onTableRequest`, ajouter au `filter`
   construit : `jobOfferId: jobOfferIdFilter.value ?? undefined`. Le reste du
   filtre (`search`, `currentStatus`, `isActive`, pagination, tri) reste
   cumulable normalement avec ce filtre par offre — pas d'exclusivité entre
   eux.
3. **Indicateur visuel du filtre actif**, indispensable pour que
   l'utilisateur comprenne pourquoi la liste est restreinte (sans quoi un
   recruteur pourrait croire, à tort, qu'il n'y a que peu de candidatures au
   total) :
   - Un `q-banner` (ou `q-chip` avec bouton de fermeture, cohérent avec le
     style déjà utilisé pour la bannière d'erreur de `JobsPage.vue`,
     lignes 27-36) affiché au-dessus du tableau quand `jobOfferIdFilter` n'est
     pas `null` : `Candidatures filtrées pour l'offre « <titre> »`, avec une
     action pour retirer le filtre.
   - Le titre de l'offre n'est pas nécessairement présent dans les résultats
     filtrés (ex. offre sans aucune candidature) : ne pas dépendre de
     `applications.value[0]?.jobOfferTitle`. Récupérer le titre de façon
     indépendante et fiable via `useJobOfferStore().fetchJobOfferById(jobOfferIdFilter.value)`
     (déjà utilisé avec le même besoin dans `JobOfferDetailPage.vue`), stocké
     dans un état local dédié (ex. `filteredJobOfferTitle`), résolu une fois
     au montage si `jobOfferIdFilter.value` est défini. Si cet appel échoue
     (offre supprimée entre-temps, erreur réseau), replier sur un libellé
     générique (`Candidatures filtrées pour une offre spécifique`) plutôt que
     de bloquer l'affichage de la liste déjà filtrée côté serveur.
4. **Retrait du filtre** : une action explicite (icône de fermeture sur la
   bannière/le chip) qui remet `jobOfferIdFilter.value` à `null`, vide
   `filteredJobOfferTitle`, nettoie le paramètre `jobId` de l'URL courante
   (`router.replace({ query: {} })` ou équivalent, pour éviter qu'un rechargement
   de page ne réapplique le filtre) et relance `refreshData()` pour afficher
   à nouveau l'ensemble des candidatures de l'organisation.
5. **Aucun changement requis côté `JobOfferDetailPage.vue`/`JobsPage.vue`** :
   ces deux pages continuent de naviguer vers `/applications?jobId=<id>`
   sans modification supplémentaire (la logique de filtrage vit entièrement
   dans `ApplicationsPage.vue`, point d'arrivée commun aux deux).

## Bug 2 — Suppression d'offre sans confirmation sur la fiche détail (+ incohérence de pattern)

### Constat vérifié

`src/pages/jobs/JobOfferDetailPage.vue`, fonction `confirmDelete` (lignes
481-487) :

```ts
const confirmDelete = async () => {
  if (!jobOffer.value) return;

  await jobStore.deleteJobOffer(jobOffer.value.id);
  notification.showSuccessNotification('Offre supprimée avec succès');
  router.push('/jobs');
};
```

Malgré son nom, ne demande **aucune** confirmation avant de supprimer.
Problème supplémentaire constaté au passage : le résultat de
`jobStore.deleteJobOffer` n'est jamais vérifié — en cas d'échec (le store émet
déjà un toast d'erreur en interne, voir `stores/jobOfferStore.ts`), la fonction
affiche quand même un toast de succès contradictoire et navigue vers `/jobs`
comme si la suppression avait réussi.

`src/pages/jobs/JobsPage.vue`, fonction `deleteJob` (lignes 100-104), utilise
elle un `confirm()` natif du navigateur :

```ts
async function deleteJob(job: JobOffer) {
  if (confirm(`Êtes-vous sûr de vouloir supprimer "${job.title}" ?`)) {
    await jobOfferStore.deleteJobOffer(job.id);
  }
}
```

Ce `confirm()` natif est lui-même daté par rapport au reste de l'application :
un composable dédié `src/composables/dialog.ts` (`useDialog`), avec une méthode
`confirmDelete(itemName, itemType)` construite sur `$q.dialog(...)` (boîte de
dialogue Quasar cohérente avec le design system), est **déjà** le pattern
standard utilisé ailleurs dans ce même package pour ce cas précis :
`UsersPage.vue` (ligne ~798-805), `OrganizationsPage.vue` (ligne ~542-549), et
même `JobCard.vue` (ligne ~155, `useDialog().confirmAction` pour les
changements de statut Publier/Fermer/Remettre en brouillon d'une offre — donc
déjà utilisé dans ce même domaine fonctionnel). Idiome standard observé :

```ts
const confirmDelete = async (item: X) => {
  try {
    await dialog.confirmDelete(item.name, 'type');
    await deleteItem(item); // vérifie le résultat, ne notifie/navigue qu'en cas de succès
  } catch {
    // Annulé par l'utilisateur — ne rien faire
  }
};
```

### Règle métier / comportement attendu

Harmoniser les deux pages sur le pattern `useDialog().confirmDelete`
(remplace le `confirm()` natif de `JobsPage.vue` **et** comble l'absence totale
de confirmation de `JobOfferDetailPage.vue`) :

1. **`JobOfferDetailPage.vue`** : importer `useDialog` depuis
   `src/composables/dialog.ts`. Réécrire `confirmDelete` :
   - Appeler `await dialog.confirmDelete(jobOffer.value.title, 'offre')` dans
     un `try`.
   - En cas de confirmation (pas de rejet de la promesse), appeler
     `jobStore.deleteJobOffer(jobOffer.value.id)`, **vérifier son résultat**
     (le store retourne un booléen de succès — voir `deleteJobOffer` dans
     `stores/jobOfferStore.ts`) avant d'afficher le toast de succès et de
     naviguer vers `/jobs` : ne faire ni l'un ni l'autre si le retour indique
     un échec (le store a déjà notifié l'erreur).
   - En cas d'annulation/fermeture de la boîte de dialogue (promesse rejetée),
     `catch` silencieux, ne rien faire.
2. **`JobsPage.vue`** : remplacer le `confirm()` natif de `deleteJob` par le
   même idiome `useDialog().confirmDelete(job.title, 'offre')`, pour cohérence
   stricte entre les deux pages (même libellé de type d'élément, même
   comportement visuel).

## Bug 3 — Champs de date en mode édition (fiche détail)

### Constat vérifié

`src/pages/jobs/JobOfferDetailPage.vue`, lignes 282 et 290 :

```html
<q-input v-else v-model="editedJob.publishedAt" type="date" dense outlined />
...
<q-input v-else v-model="editedJob.expiresAt" type="date" dense outlined />
```

`editedJob` est peuplé via `Object.assign(editedJob, jobOffer.value)`
(`toggleEditMode`, ligne ~437-440), où `jobOffer.value` est le
`JobOfferDto` retourné par l'API. Backend (`DTOs/JobOffer/JobOfferDto.cs`,
lignes 19-20) :

```csharp
public DateTime? PublishedAt { get; set; }
public DateTime? ExpiresAt { get; set; }
```

Aucun convertisseur `DateTime` custom n'est enregistré (`Program.cs`, ligne
104-105 : seul un `JsonStringEnumConverter` est ajouté). Le sérialiseur JSON
par défaut de .NET produit donc, pour ces champs, une chaîne du type
`"2026-07-25T00:00:00"` (vérifié empiriquement : `DateTime` avec
`Kind=Unspecified` sérialisé par `System.Text.Json` ne produit ni suffixe `Z`
ni décalage horaire, mais conserve toujours le composant heure complet, même à
minuit).

Un `<input type="date">` HTML natif exige strictement le format `YYYY-MM-DD`
(10 caractères) pour que son setter `.value` accepte la valeur : toute chaîne
plus longue (avec `T...`) est silencieusement rejetée par le navigateur et
l'input **affiche vide**, même si `editedJob.publishedAt`/`expiresAt`
conserve la valeur d'origine en mémoire (le binding Vue n'est pas cassé, seul
l'affichage natif du champ l'est). **Confirmé** : le champ apparaît vide en
mode édition dès qu'une date existante est présente sur l'offre.

**Découverte supplémentaire, en creusant la cause racine** : même en corrigeant
le format d'affichage, le champ "Date de publication" resterait un champ mort
en écriture. `PublishedAt` n'existe **pas** dans le contrat de mise à jour, ni
côté backend (`DTOs/JobOffer/UpdateJobOfferDto.cs` — seuls `Title`,
`Description`, `Requirements`, `Benefits`, `Location`, `WorkMode`,
`ContractType`, `SalaryMin`, `SalaryMax`, `ExpiresAt` sont présents, pas
`PublishedAt`), ni côté frontend (`src/models/job.ts`, interface
`UpdateJobOfferDto`, même liste de champs). `PublishedAt` est un champ
**géré par le domaine**, positionné uniquement par
`JobOffer.Publish()` (`Models/JobOffer.cs`, lignes 63-73 :
`Status = JobOfferStatus.Published; PublishedAt = DateTime.UtcNow;`), appelé
par le endpoint de publication dédié (déjà câblé côté frontend via
`jobOfferStore.publishJobOffer`). Toute valeur saisie dans ce champ en mode
édition serait donc silencieusement ignorée par le backend à l'enregistrement
— un bug fonctionnel plus profond que le simple problème d'affichage, présent
indépendamment du bug de format.

### Règle métier / comportement attendu

Traiter différemment les deux champs, puisqu'ils n'ont pas le même statut vis-à-vis
du contrat de mise à jour :

1. **Date d'expiration (`expiresAt`)** : reste éditable (le champ existe bien
   dans `UpdateJobOfferDto`). Corriger la conversion de format :
   - À l'entrée en mode édition (`toggleEditMode`) ou à l'affichage du champ,
     convertir `jobOffer.value.expiresAt` (ISO datetime ou `undefined`) vers
     un format `YYYY-MM-DD` avant de l'assigner à `editedJob.expiresAt`, par
     exemple via `date.formatDate(value, 'YYYY-MM-DD')` de Quasar (déjà
     importé dans ce fichier : `import { date } from 'quasar';`, déjà utilisé
     pour `formatDate`). Gérer le cas `null`/`undefined` (champ vide, pas
     d'erreur de conversion).
   - À l'enregistrement (`saveChanges`), la valeur `YYYY-MM-DD` saisie par le
     natif `<input type="date">` est un format de date valide, directement
     interprétable par le model binding ASP.NET Core (`DateTime?`) — aucune
     reconversion nécessaire avant l'envoi à `jobStore.updateJobOffer`.
2. **Date de publication (`publishedAt`)** : puisqu'elle n'est **jamais**
   transmise par `UpdateJobOfferDto` (ni backend ni frontend) et n'est censée
   changer que via l'action métier "Publier", retirer l'`<q-input type="date">`
   éditable en mode édition et la remplacer par un affichage en lecture seule
   (même traitement que le bloc "Date de création" juste au-dessus dans le
   même composant, lignes 259-264 : `{{ formatDate(jobOffer.publishedAt) }}`),
   y compris en mode édition. **Ce point change un comportement visible** (un
   champ qui semblait éditable devient explicitement non éditable) — à
   valider par l'utilisateur au moment de la revue, mais fondé sur un constat
   de code sans ambiguïté (le champ n'a jamais été réellement modifiable, la
   correction ne fait que rendre ce fait visible plutôt que de le masquer
   derrière un bug d'affichage).

Aucun changement requis sur `JobDialog.vue` : son champ `expiresAt` (type
`date`) n'est utilisé qu'en création (`JobDialog` n'est aujourd'hui invoqué
que depuis `JobsPage.vue`, toujours avec `selectedJob = null`, donc toujours en
mode création — vérifié : aucun autre appelant du composant, `editJob` dans
`JobsPage.vue` navigue vers la fiche détail plutôt que d'ouvrir ce dialogue).
Le champ démarre donc toujours vide, sans donnée préexistante à convertir :
pas de bug observable dans ce composant pour ce point précis.

## Bug 4 — Suffixe "€" en dur sur le formulaire de création/édition d'offre

### Constat vérifié

`src/components/jobs/JobDialog.vue`, lignes 80 et 90 :

```html
<q-input v-model="formData.salaryMin" ... suffix="€" ... />
<q-input v-model="formData.salaryMax" ... suffix="€" ... />
```

La devise réelle d'une offre dépend de la devise configurée pour
l'organisation au moment de la création (`Organization.Currency`, `EUR` ou
`XOF`, cf. `configurable-salary-currency.md`, déjà mergée) : pour une
organisation en `XOF`, ce formulaire affiche à tort le symbole `€`.

Cette spec déjà mergée avait explicitement traité `JobDialog.vue` (section
« Formulaires de création/édition d'offre », ligne ~545) mais uniquement pour
retirer le champ de données `salaryCurrency` (déjà fait : `JobFormData`,
`CreateJobOfferDto`, `UpdateJobOfferDto` construits dans `saveJob()` n'ont
plus cette propriété, vérifié). Le suffixe cosmétique `"€"`, purement visuel
et non lié à une propriété de données, n'a pas été couvert par cette
correction précédente — c'est un résidu oublié, pas une régression.

Cette même spec a déjà établi et validé (décision 3, voir
`configurable-salary-currency.md` lignes 80-92) que le formulaire de
**création** ne doit **volontairement montrer aucun indicateur de devise**,
même en lecture seule : la devise appliquée n'est connue qu'après création
(stampée par le backend depuis `Organization.Currency`), et ajouter un chemin
de lecture supplémentaire pour ce seul affichage cosmétique élargirait le
périmètre d'autorisation sans bénéfice réel. Ce choix a déjà été appliqué
correctement dans `JobOffersPage.vue` (formulaire de création identique,
aucun suffixe, aucune devise affichée avant création — lignes 266-282) et
dans le mode édition de ce même fichier (devise affichée en lecture seule
uniquement quand une offre existante est sélectionnée, lignes 284-291) ainsi
que dans `JobOfferDetailPage.vue` (lignes 250-256).

`JobDialog.vue` n'est aujourd'hui utilisé qu'en création (voir Bug 3 ci-dessus
pour la vérification précise : toujours invoqué avec `selectedJob = null`
depuis `JobsPage.vue`), donc aucun affichage de devise en lecture seule n'est
nécessaire dans ce composant pour l'instant (contrairement à
`JobOffersPage.vue`/`JobOfferDetailPage.vue`, qui gèrent tous deux un vrai
mode édition).

### Règle métier / comportement attendu

- Retirer entièrement `suffix="€"` des deux `q-input` Salaire minimum/maximum
  dans `JobDialog.vue` (lignes 80 et 90), sans le remplacer par un autre
  suffixe ni par un affichage de devise quelconque (conforme à la décision 3
  déjà validée : aucun indicateur de devise sur le formulaire de création).
- Ne pas ajouter d'affichage de devise en lecture seule dans ce composant :
  son support d'édition (`isEditing`/`props.job`) existe dans le code mais
  n'est exercé par aucun appelant actuel. Si un futur appelant réutilise ce
  composant en mode édition, l'ajout d'un affichage de devise en lecture
  seule (sur le modèle de `JobOffersPage.vue`/`JobOfferDetailPage.vue`) sera
  à traiter à ce moment-là, hors périmètre de cette spec.

## Hors périmètre

- Conversion monétaire, ajout d'autres devises que `EUR`/`XOF` : hors périmètre
  (déjà tranché par `configurable-salary-currency.md`).
- Réactivation d'un vrai mode édition pour `JobDialog.vue` (actuellement mort
  en pratique) : non demandé, non traité.
- Double toast de succès sur `JobOfferDetailPage.vue#duplicateJob` (constat
  fait en explorant le bug 1, la fonction de référence affiche son propre
  toast de succès en plus de celui du store) et absence de vérification du
  résultat avant navigation dans cette même fonction : bug préexistant sur du
  code que l'audit qualifie de fonctionnel/référence, non touché par cette
  spec. Signalé ici pour mémoire, pourrait faire l'objet d'une correction
  séparée si souhaité.
- Tests automatisés : cette spec définit des critères vérifiables ;
  l'écriture effective de tests relève de l'agent `developer`.

## Critères d'acceptation vérifiables

1. Depuis `/jobs` (vue grille et vue tableau), cliquer sur "Dupliquer" dans le
   menu d'une offre crée une nouvelle offre en statut Brouillon, titre suffixé
   `(Copie)`, et la liste affichée se met à jour pour la montrer (sans
   navigation vers une autre page).
2. Depuis `/jobs`, cliquer sur "Candidatures" (menu ou bouton dédié)
   navigue vers `/applications?jobId=<id>`, et la page affiche **uniquement**
   les candidatures de cette offre (vérifiable en comparant le nombre total de
   lignes avec/sans le paramètre `jobId`, ou en confirmant que
   `props.row.jobOfferId === <id>` pour chaque ligne affichée) ; une bannière
   ou un chip indique clairement que le résultat est filtré par offre, avec
   une action pour retirer le filtre et revenir à la liste complète des
   candidatures de l'organisation.
3. Depuis `/jobs`, cliquer sur "Supprimer" une offre ouvre une boîte de
   dialogue de confirmation Quasar (pas de `confirm()` natif) ; annuler ne
   supprime rien ; confirmer supprime l'offre et la retire de la liste.
4. Depuis `/jobs/:id`, cliquer sur "Supprimer l'offre" ouvre la même boîte de
   dialogue de confirmation (actuellement absente) ; annuler ne supprime
   rien et reste sur la page ; confirmer supprime l'offre, affiche un toast de
   succès **uniquement** si la suppression a réussi, puis navigue vers
   `/jobs` ; en cas d'échec de la suppression, aucun toast de succès ni
   navigation, l'utilisateur reste sur la fiche.
5. Depuis `/jobs/:id`, cliquer sur "Dupliquer l'offre" (bouton existant,
   non régressé) continue de fonctionner à l'identique.
6. Depuis `/jobs/:id` en mode édition, sur une offre ayant déjà une date
   d'expiration définie, le champ "Date d'expiration" affiche cette date au
   format attendu par le navigateur (pas vide), reste modifiable, et la
   modification est bien persistée après "Enregistrer" (vérifiable par un
   rechargement de la fiche).
7. Depuis `/jobs/:id` en mode édition, le champ "Date de publication" n'est
   plus un input modifiable : il affiche la date de publication existante en
   lecture seule (ou "Non définie" si absente), identique en mode lecture et
   en mode édition.
8. Le formulaire de création d'offre (`JobDialog.vue`, ouvert depuis "Nouvelle
   offre" sur `/jobs`) n'affiche aucun symbole ou libellé de devise à côté des
   champs Salaire minimum/maximum, quelle que soit la devise configurée pour
   l'organisation courante.
9. Après création d'une offre via ce formulaire, la devise réellement
   appliquée (visible sur la fiche détail ou dans la liste) correspond à la
   devise configurée de l'organisation (ou `XOF` par repli si non configurée),
   conformément à `configurable-salary-currency.md` — non-régression, cette
   spec ne modifie pas ce comportement backend déjà en place.
10. Depuis `/jobs/:id`, cliquer sur "Voir les candidatures" produit le même
    résultat filtré que depuis `/jobs` (critère 2) : même page cible
    `/applications?jobId=<id>`, filtrage identique puisqu'il est implémenté
    une seule fois dans `ApplicationsPage.vue`, point d'arrivée commun aux
    deux pages.
11. Retirer le filtre (action sur la bannière/le chip de `ApplicationsPage.vue`)
    réaffiche l'ensemble des candidatures de l'organisation (plus seulement
    celles de l'offre d'origine) et retire `jobId` de l'URL affichée par le
    navigateur.
12. Accéder directement à `/applications?jobId=<id>` (navigation manuelle,
    sans passer par les boutons "Voir les candidatures") produit le même
    filtrage dès le chargement initial de la page (pas seulement après une
    navigation interne depuis `/jobs`/`/jobs/:id`).
