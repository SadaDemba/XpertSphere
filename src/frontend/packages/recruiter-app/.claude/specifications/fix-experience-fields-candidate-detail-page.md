# Correction de l'affichage des expériences candidat (`CandidateDetailPage.vue`)

## Contexte et périmètre

Cette spec est née d'une découverte faite pendant la rédaction d'une spec de seed backend (`src/backend/XpertSphere.MonolithApi/.claude/specifications/enrich-seed-candidate-profiles.md`) : une fois les candidats de démonstration enrichis d'`Experience`/`Training`, la section "Expériences professionnelles" de `CandidateDetailPage.vue` s'affiche de façon dégradée. Confirmé empiriquement par l'utilisateur sur un candidat créé manuellement : seuls le nom de l'entreprise et la description s'affichent, jamais l'intitulé du poste ni la période.

À la demande de l'utilisateur, une recherche plus large a été menée dans `recruiter-app` pour identifier d'autres écarts du même type (interface TypeScript "devinée" au lieu d'être alignée sur le contrat réel du backend). Cette recherche a permis de répondre à la question posée : **il n'existe aucun autre bug d'affichage actif de ce type dans `recruiter-app`** — seulement du code mort (composants `.vue` jamais montés par aucune page), documenté en §Constat annexe ci-dessous pour mémoire, mais **volontairement non traité par cette spec** (décision de l'utilisateur : ne rien supprimer pour l'instant, pour éviter tout risque). Cette spec se limite donc strictement à la correction du bug actif et visible : l'interface `Experience` et le template `CandidateDetailPage.vue`.

## Correction de `Experience` (`CandidateDetailPage.vue`)

### Constat détaillé (vérifié dans le code, pas une supposition)

Le backend (`XpertSphere.MonolithApi`) sérialise chaque expérience embarquée dans `UserDto.Experiences` (`GET /api/users/{id}`) avec les clés suivantes (modèle `Experience extends AuditableEntity`, `Models/Experience.cs` + `Models/Base/AuditableEntity.cs`) :

```json
{
  "id": "...",
  "createdAt": "...",
  "updatedAt": null,
  "userId": "...",
  "title": "Développeur back-end .NET",
  "description": "...",
  "location": "Dakar",
  "company": "Sonatel",
  "date": "10/2020 - 12/2022",
  "isCurrent": false
}
```

`recruiter-app/src/models/user.ts` (lignes 12-20) déclare pourtant :

```ts
export interface Experience {
  id?: string;
  position: string;
  company: string;
  startDate: string;
  endDate?: string;
  description?: string;
  technologies?: string[];
}
```

Aucune des clés `position`/`startDate`/`endDate`/`technologies` n'existe dans le JSON réel. Ni `userService.ts` ni `userStore.ts` (`recruiter-app`) ne transforment la réponse entre les deux formes (vérifié, aucune occurrence de ces noms dans ces deux fichiers). `candidate-app` (`src/models/experience.ts`) utilise, lui, la forme correcte (`title`/`date`/`company`/`description`/`location`/`isCurrent`) — c'est donc `recruiter-app` seul qui dévie, et c'est lui qu'on aligne, **pas le backend** (source de vérité, déjà cohérente avec `candidate-app`).

Conséquence observée dans `CandidateDetailPage.vue` (lignes 444-470) : `experience.company` et `experience.description` s'affichent (noms qui coïncident par hasard), `experience.position` est toujours `undefined` (titre de poste vide), `experience.startDate`/`endDate` sont toujours `undefined` (`formatDate(undefined)` renvoie `''`, donc la ligne de date affiche seulement `- En cours` en permanence, quel que soit `isCurrent`), et le bloc `technologies` ne s'affiche jamais (`v-if="experience.technologies && experience.technologies.length > 0"` toujours faux).

### Changement requis — `recruiter-app/src/models/user.ts`

Remplacer l'interface `Experience` (lignes 12-20) par la forme réellement renvoyée par le backend :

```ts
export interface Experience {
  id?: string;
  title: string;
  company: string;
  location?: string;
  date: string;
  isCurrent: boolean;
  description?: string;
}
```

`technologies` est retiré : aucun champ backend ne le porte (ni sur `Models/Experience.cs`, ni sur `DTOs/ExperienceDtos/ExperienceDto.cs` — vérifié, aucun des deux n'a de notion de "technologies"). Ce n'est pas un champ qui existait et qu'on aurait perdu : il n'a jamais existé côté backend, c'est une invention de l'interface frontend d'origine.

### Changement requis — `CandidateDetailPage.vue` (bloc "Expériences professionnelles", lignes 444-470)

- **Ligne 429**, en dehors du bloc principal ci-dessous mais à ne pas rater : `:key="experience.id || experience.position"` → `:key="experience.id"` (la clé de la boucle `v-for` référence elle aussi le champ fantôme `position`, plus la peine d'un repli une fois `id` toujours renseigné par le backend — `AuditableEntity.Id` est non-nullable).
- `experience.position` (ligne 444) → `experience.title`.
- La ligne de date (lignes 448-452) : remplacer les deux appels à `formatDate(experience.startDate)`/`formatDate(experience.endDate)` par un affichage **direct** de `experience.date`, sans passer par `formatDate()`. Point important à ne pas rater : `experience.date` est un champ texte libre (`[MaxLength(40)]` côté backend, ex. `"10/2020 - 12/2022"` ou `"09/2022 - Présent"`), pas une date ISO — `formatDate()` (qui appelle `date.formatDate(dateString, 'DD/MM/YYYY')` de Quasar) produirait un résultat vide ou incorrect sur cette chaîne. L'indicateur "En cours" doit utiliser `experience.isCurrent` (et non plus la présence/absence d'`endDate`, qui n'existe plus) :
  ```html
  <div class="text-caption text-grey-6 q-mb-sm">
    <q-icon name="event" size="xs" class="q-mr-xs" />
    {{ experience.date }}
    <q-chip
      v-if="experience.isCurrent"
      dense
      size="sm"
      color="secondary"
      text-color="white"
      class="q-ml-xs"
    >
      En cours
    </q-chip>
  </div>
  ```
  (Le développeur reste libre du détail visuel exact du badge "En cours", tant que son affichage est gouverné par `isCurrent` et que la période s'appuie sur `experience.date` affiché tel quel.)
- Le bloc `technologies` (lignes 456-470, chips) : **remplacé par l'affichage de `experience.location`** (champ qui existe déjà côté backend et modèle, actuellement jamais utilisé dans cette carte), pas simplement supprimé sans remplacement — décision de ce correctif, puisque l'information existe et a de la valeur pour une fiche candidat (ex. "Dakar", "Lyon"). Rendu proposé, cohérent avec le style existant de la carte (icône + texte, comme la ligne de date juste au-dessus) :
  ```html
  <div v-if="experience.location" class="text-caption text-grey-6">
    <q-icon name="location_on" size="xs" class="q-mr-xs" />
    {{ experience.location }}
  </div>
  ```
  Ne pas laisser de `v-if` mort référençant `technologies` : ce bloc est entièrement remplacé, pas commenté ou désactivé.

### Non régression — `Training` reste hors périmètre

`Training` (`training.field`/`training.school`/`training.period`, lignes 384-407 de `CandidateDetailPage.vue`) correspond déjà exactement au modèle backend (`Models/Training.cs` : `Field`, `School`, `Period`) — vérifié, aucune incohérence. **Aucun changement sur `Training` n'est demandé ni attendu par cette spec.** Le `validator` ne doit pas s'attendre à un diff sur cette partie du template ou sur l'interface `Training` de `models/user.ts`.

### Autres usages d'`Experience` à vérifier (rayon d'impact)

`Experience` (`models/user.ts`) est également référencé par `UserDto.experiences`/`CreateUserDto.experiences`/`UpdateUserDto.experiences` (mêmes lignes de fichier). Le développeur doit vérifier qu'aucun autre composant `recruiter-app` **réellement monté par une page** n'accède à `.position`/`.startDate`/`.endDate`/`.technologies` sur une valeur de ce type — recherche déjà effectuée pour cette spec (`grep -rn "position\|startDate\|endDate\|technologies"` sur `src/`), seul `CandidateDetailPage.vue` est concerné (les autres occurrences trouvées relèvent soit de CSS (`position: absolute`), soit du code mort documenté ci-dessous, non touché par cette spec).

## Constat annexe (documenté, non traité par cette spec) — composants morts au même type d'incohérence

En cherchant d'autres écarts du même type, deux grappes de composants `.vue`, **jamais importées par aucune page** (`pages/**`), ont été identifiées — vérifié par une recherche exhaustive des références dans tout `src/`, pas seulement `pages/`. **Décision de l'utilisateur : on ne supprime rien pour l'instant, pour éviter tout risque.** Cette section reste purement informative, pour une décision ultérieure éventuelle (spec séparée si l'utilisateur le souhaite un jour) — **aucune action n'est attendue du `developer` sur ce constat**.

**Grappe 1 — candidatures (`components/applications/`)**, 3 fichiers : `ApplicationList.vue` (racine, 0 référence externe), qui importe `ApplicationCard.vue` et `ApplicationTable.vue`. Utilisent l'interface `Application` de `models/application.ts` (`jobId`, `jobTitle`, `status`, `resumeUrl`, `notes`, `source`/`ApplicationSource`), sans aucun rapport avec `ApplicationDto` (utilisé, lui, par les vraies pages `ApplicationsPage.vue`/`ApplicationDetailPage.vue`).

**Grappe 2 — candidats (`components/candidates/`)**, 5 fichiers : `CandidateList.vue` (racine, 0 référence externe), qui importe `CandidateCard.vue`, `CandidateTable.vue`, et `AppPagination.vue` (composant partagé, lui bien vivant via `JobList.vue` — à ne surtout pas confondre avec les composants morts) ; ainsi que `CandidateDialog.vue` et `CandidateFilters.vue` (indépendants, eux aussi 0 référence externe). Ces cinq fichiers utilisent un champ `position` (poste actuel du candidat) sur des interfaces locales, sans équivalent backend.

**Trois autres composants orphelins repérés au passage**, sans lien avec un écart de nommage de champ (juste du code jamais branché) : `ApplicationDetailDialog.vue`, `ApplicationFilters.vue`, `InviteCandidateDialog.vue`.

Au total, 11 fichiers `.vue` sans aucune référence externe ont été identifiés dans `recruiter-app` pendant cette investigation, tous non touchés par cette spec.

## Hors périmètre

- **`Training`** : ne correspond à aucune incohérence, aucun changement demandé — voir §Non régression.
- **Les 11 composants morts listés en §Constat annexe** : explicitement non supprimés, non modifiés, par décision de l'utilisateur. Aucune action attendue dessus.
- **`models/application.ts`** : non modifié par cette spec (l'interface `Application`/`applicationSourceLabels`, utilisées uniquement par les composants morts de la Grappe 1, restent en l'état — leur éventuel retrait dépend d'une décision future sur le §Constat annexe).
- **Toute modification du backend** (`XpertSphere.MonolithApi`) : ce correctif n'aligne que le frontend sur un contrat backend déjà stable et déjà correctement suivi par `candidate-app`.
- **Tests automatisés** : `recruiter-app` n'a actuellement aucun test unitaire configuré (`"test": "echo \"No test specified\" && exit 0"`, `CLAUDE.md` du package) — aucun test à écrire ou à adapter pour ce correctif.

## Critères d'acceptation

1. `recruiter-app/src/models/user.ts` : l'interface `Experience` a exactement les champs `id?`, `title`, `company`, `location?`, `date`, `isCurrent`, `description?` — plus aucune trace de `position`/`startDate`/`endDate`/`technologies`.
2. `CandidateDetailPage.vue`, section "Expériences professionnelles" : pour un candidat ayant au moins une `Experience`, la carte affiche l'intitulé du poste réel (`experience.title`), la période réelle telle qu'enregistrée (`experience.date`, affichée telle quelle, sans passer par `formatDate()`), un badge "En cours" uniquement si `experience.isCurrent === true`, la description si présente, et le lieu (`experience.location`) si présent — plus aucun champ vide ni bloc `technologies`.
3. Aucune régression sur la section "Formations" (`Training`) de la même page.
4. `npm run build` (`recruiter-app`) et `npm run lint` réussissent sans erreur après ce changement.
5. Aucun des 11 fichiers documentés en §Constat annexe n'est supprimé ni modifié par ce correctif.

## Coordination avec `enrich-seed-candidate-profiles.md` (backend)

Cette spec répond au point signalé dans `src/backend/XpertSphere.MonolithApi/.claude/specifications/enrich-seed-candidate-profiles.md`, §Hors périmètre, sous-section "Découverte importante hors périmètre de cette spec". Une fois cette spec-ci implémentée, la section "Expériences professionnelles" de `CandidateDetailPage.vue` affichera complètement les expériences des 4 candidats de démonstration seedés par `enrich-seed-candidate-profiles.md` (plus de titre de poste vide ni de période absente).

## Fichiers à créer/modifier (récapitulatif)

- `src/models/user.ts` : interface `Experience` corrigée.
- `src/pages/candidates/CandidateDetailPage.vue` : bloc "Expériences professionnelles" corrigé (et ligne 429, `:key`).
- Aucun autre fichier touché : ni `models/application.ts`, ni aucun des 11 composants morts documentés en §Constat annexe, ni `src/router/routes.ts`.
