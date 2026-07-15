# Téléchargement/consultation sécurisée d'un CV déjà uploadé

## Contexte et diagnostic

Le container blob `resumes` est créé avec `PublicAccessType.None` (`Services/ResumeService.cs`, `UploadResumeAsync`, `containerClient.CreateIfNotExistsAsync(PublicAccessType.None)`) — choix délibéré, un CV étant une donnée personnelle sensible. `UploadResumeAsync` retourne néanmoins l'URL brute du blob (`blobClient.Uri.ToString()`), stockée telle quelle en base sur `User.CvPath` et republiée sans transformation dans plusieurs DTOs (`UserDto.CvPath`, `UserProfileDto.CvPath`, `UploadCvResponseDto.CvPath`). Cette URL n'a **jamais** été directement accessible, ni en local (Azurite) ni en Staging/Production (Azure Storage réel) : une requête anonyme dessus échoue avec `AuthorizationFailure`.

`IResumeService` expose déjà `DownloadResumeAsync(string resumePath)` et `GetResumeMetadataAsync(string resumePath)`, tous deux fonctionnels (utilisent `BlobUriBuilder`, corrigé pour Azurite comme pour Azure réel — voir `azurite-blob-storage-local.md` de ce même dossier) mais **inutilisés** : aucune recherche dans `Controllers/` ne trouve d'appel à ces deux méthodes. Le seul endpoint existant côté CV, `POST /api/users/{id}/cv` (`UsersController.UploadCv`), ne fait qu'uploader ; rien n'expose de lecture.

Conséquence côté frontend : trois emplacements distincts ouvrent directement l'URL brute stockée dans `cvPath`, et échouent tous silencieusement ou affichent une erreur XML brute du navigateur :

1. **`candidate-app`** — `src/pages/ProfilePage.vue`, fonction `downloadCV()` : `window.open(user.value.cvPath, '_blank')`.
2. **`recruiter-app`, fiche candidat** — trois composants distincts, découverts en explorant au-delà du périmètre initialement signalé :
   - `src/pages/candidates/CvViewPage.vue` (route réelle `candidates/:id/cv`, déjà branchée depuis `CandidateDetailPage.vue::viewCV()` et depuis `CvPreviewDialog.vue::openFullView()`) : `<iframe :src="candidate.cvPath">` et `downloadCV()` (lien `<a href="candidate.cvPath" download>`).
   - `src/components/candidates/CvPreviewDialog.vue` (aperçu au survol, affiché par `CandidateDetailPage.vue` après un délai de hover) : `<iframe :src="cvUrl">`, où `cvUrl` reçoit directement `candidate.cvPath` (`CandidateDetailPage.vue:563`, `:cv-url="candidate.cvPath!"`).
   - `src/pages/candidates/CandidateDetailPage.vue` : ne construit aucune URL lui-même, mais transmet `candidate.cvPath` brut aux deux composants ci-dessus.
3. **`recruiter-app`, vue candidatures** — `ApplicationDetailDialog.vue`, `ApplicationCard.vue`, `ApplicationTable.vue`, fonction `viewResume()` dans chacun : `window.open('/resume-viewer', '_blank')`. Cette route est un **placeholder mort** : `router/routes.ts` ne la définit pas (elle tomberait sur la route catch-all `ErrorNotFound.vue`), et les trois fonctions `viewResume()` ne reçoivent même pas l'`Application`/le candidat concerné en paramètre — fonctionnalité jamais branchée, contrairement au flux "fiche candidat" ci-dessus qui est réel mais cassé par le même problème racine.

Une variable `VITE_STORAGE_BASE_URL` existe dans `settings/index.ts` des deux apps (`settings.storage.baseUrl`) mais n'est utilisée nulle part dans le code : code mort, cohérent avec le fait qu'aucune URL de stockage n'est directement exploitable côté client.

## Décisions structurantes (confirmées par l'utilisateur)

### 1. Endpoint proxy authentifié (Option B), pas de SAS token (Option A)

Un nouvel endpoint `GET /api/users/{id}/cv` appelle `IResumeService.DownloadResumeAsync`/`GetResumeMetadataAsync` côté serveur et streame le contenu dans la réponse HTTP, avec l'authentification déjà en place (JWT local / Entra ID). Le frontend appelle cet endpoint via son client HTTP authentifié existant, récupère un `Blob`, puis construit une `URL.createObjectURL(blob)` pour l'affichage/le téléchargement — au lieu d'ouvrir `cvPath` directement.

Justification, à partir du code réel :
- Une URL SAS ne peut pas être stockée dans `CvPath` (elle expire) : il faudrait la régénérer à chaque point d'exposition (`UserDto`, `UserProfileDto`, `UploadCvResponseDto`, mapping AutoMapper, plusieurs services), pour un gain de sécurité moindre — une URL SAS, une fois émise, reste utilisable par quiconque la détient jusqu'à son expiration, sans second contrôle d'autorisation par requête.
- Option B réutilise `DownloadResumeAsync`/`GetResumeMetadataAsync`, déjà écrits et fonctionnels, sans toucher `ResumeService.cs`.
- Option B impose un contrôle d'autorisation à **chaque** téléchargement, révocable immédiatement (contrairement à un lien SAS déjà émis).
- Aucune migration/changement de `CvPath` en base : les CV déjà uploadés (valeur brute déjà stockée) continuent de fonctionner sans modification de données.
- `recruiter-app` a déjà une méthode `downloadFile()` dans `services/BaseClient.ts` (utilisée nulle part aujourd'hui) qui anticipe exactement ce pattern (requête authentifiée, `responseType: 'blob'`, interception d'erreur JSON même en mode blob via `blobErrorResponseInterceptor`).

### 2. Autorisation : réutilisation de la policy existante `CandidateOwnDataAccess` — limitation connue acceptée

Le nouvel endpoint est protégé par `[Authorize(Policy = "CandidateOwnDataAccess")]` (`Extensions/SecurityExtensions.cs`), la même policy que `GET /api/users/{id}` et `GET /api/users/{id}/profile` aujourd'hui. Cette policy autorise :
- le `PlatformAdmin`/`PlatformSuperAdmin` (tout accès) ;
- **tout utilisateur ayant un rôle d'organisation** (`OrganizationAdmin`, `Manager`, `Recruiter`, `TechnicalEvaluator`) — **dans n'importe quelle organisation**, pas seulement celle liée à une candidature réelle du candidat ciblé ;
- le candidat lui-même, si `userId` (claim `NameIdentifier`) correspond au `{id}` de la route.

**Limitation connue et sciemment acceptée pour cette spec** : un recruteur de l'organisation A peut télécharger le CV d'un candidat qui n'a jamais postulé chez A, dès lors qu'il connaît (ou devine) son `userId`. Ce n'est pas un accès anonyme (authentification + rôle organisationnel réel requis, et l'action est traçable), mais ce n'est pas non plus strictement scopé "recruteur ayant accès à cette candidature précise". Décision utilisateur : ne pas corriger ce point dans cette spec, réutiliser la policy telle quelle. Piste de correction pour un futur correctif, non implémentée ici : une policy scopée à la candidature (vérifier que le recruteur a accès à une `Application` réelle du candidat dans son organisation), qui impliquerait probablement une route différente (`GET /api/applications/{id}/cv`, l'`Application` portant le contexte organisation/candidat nécessaire au contrôle), puisque `User` seul ne porte pas cette information.

### 3. Correctif connexe inclus dans le périmètre : sécurisation de l'upload

`POST /api/users/{id}/cv` (`UsersController.UploadCv`) n'a **aucun** `[Authorize]` aujourd'hui — n'importe quelle requête, même non authentifiée, peut uploader un CV pour n'importe quel `userId`. Décision utilisateur : corriger ce point dans le périmètre de cette même spec, par cohérence avec la sécurisation du téléchargement. Ajouter `[Authorize(Policy = "CandidateOwnDataAccess")]` sur `UploadCv`, au même niveau que les autres actions du contrôleur (`GetUser`, `UpdateUserSkills`, `UpdateUserProfile`).

## Comportement cible — Backend (`MonolithApi`)

### Nouvelle classe de transport `CvDownloadResult`

Ajoutée dans `Interfaces/IUserService.cs`, à la suite de l'interface (même convention que `ResumeMetadata`, déclarée à la suite de `IResumeService` dans `Interfaces/IResumeService.cs`) :

```csharp
public class CvDownloadResult
{
    public Stream Content { get; set; } = default!;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
}
```

### Nouvelle méthode `IUserService.GetCvForDownloadAsync`

```csharp
Task<ServiceResult<CvDownloadResult>> GetCvForDownloadAsync(Guid userId);
```

Implémentation dans `Services/UserService.cs` (mêmes dépendances déjà injectées : `_context`, `_resumeService`, `_logger` — aucune nouvelle dépendance requise) :
1. Charger l'utilisateur par `userId` (`_context.Users.FindAsync` ou équivalent). Si absent → `ServiceResult<CvDownloadResult>.NotFound($"User with ID {userId} not found")`.
2. Si `user.CvPath` est `null`/vide → `NotFound("No CV uploaded for this user")` (le candidat n'a jamais uploadé de CV — cas légitime, pas une erreur serveur).
3. Appeler `_resumeService.GetResumeMetadataAsync(user.CvPath)`. Si `!IsSuccess` (ex. blob supprimé côté storage mais `CvPath` resté renseigné en base — désynchronisation) → propager en `NotFound("CV file not found in storage")`, pas en erreur 500 : c'est un cas de données incohérentes, pas une panne.
4. Appeler `_resumeService.DownloadResumeAsync(user.CvPath)`. Même traitement d'échec qu'à l'étape 3.
5. Retourner `ServiceResult<CvDownloadResult>.Success(new CvDownloadResult { Content = <stream de l'étape 4>, FileName = <metadata.FileName>, ContentType = <metadata.ContentType> })`.

Aucune modification de `ResumeService.cs`/`IResumeService.cs` : ces deux méthodes sont consommées telles qu'elles existent aujourd'hui.

### Nouvel endpoint `GET /api/users/{id}/cv`

Ajouté dans `Controllers/UsersController.cs`, région `#region File Operations` (à la suite de `UploadCv`) :

```csharp
[HttpGet("{id:guid}/cv")]
[Authorize(Policy = "CandidateOwnDataAccess")]
[ProducesResponseType(200)]
[ProducesResponseType(404)]
public async Task<IActionResult> DownloadCv(Guid id)
{
    var result = await _userService.GetCvForDownloadAsync(id);
    if (!result.IsSuccess)
    {
        return result.StatusCode switch
        {
            404 => NotFound(new { message = result.Message }),
            _ => StatusCode(result.StatusCode, new { message = result.Message })
        };
    }

    var cv = result.Data!;
    return File(cv.Content, cv.ContentType, cv.FileName);
}
```

Point d'attention explicite : cette action **ne passe pas** par `this.ToActionResult(result)` (`Extensions/ControllerExtensions.cs`). `ToActionResult`/`ToActionResult<T>` ne savent produire que des réponses JSON (`Ok(result)`, `NotFound(result)`, etc.) — aucune branche ne gère un flux binaire. C'est une dérogation délibérée et isolée à ce seul endpoint, pas un changement de convention générale des contrôleurs.

`File(stream, contentType, fileName)` (surcharge à 3 arguments) positionne `Content-Disposition: attachment; filename=...` dans la réponse HTTP brute — comportement conservateur par défaut. Ceci n'entrave pas un usage "aperçu inline" côté frontend : dans tous les cas prévus par cette spec, le client récupère la réponse via une requête authentifiée (`responseType: 'blob'`), pas par navigation directe du navigateur vers l'URL de l'endpoint (impossible sans porter le header `Authorization`) ; le header `Content-Disposition` de la réponse HTTP d'origine n'affecte pas le comportement d'une `URL.createObjectURL()` construite ensuite côté client à partir du `Blob` récupéré.

Le `Stream` retourné par `DownloadResumeAsync` (`blobClient.OpenReadAsync()`) doit être transmis tel quel à `File(...)` : ASP.NET Core prend en charge sa disposition après l'écriture de la réponse, aucune bufferisation manuelle en tableau d'octets n'est nécessaire.

### Sécurisation de l'upload

Dans `Controllers/UsersController.cs`, ajouter au-dessus de `UploadCv` :

```csharp
[HttpPost("{id:guid}/cv")]
[Authorize(Policy = "CandidateOwnDataAccess")]
[ProducesResponseType(typeof(UploadCvResponseDto), 200)]
[ProducesResponseType(400)]
[ProducesResponseType(404)]
public async Task<ActionResult<UploadCvResponseDto>> UploadCv(Guid id, [FromForm] UploadCvDto uploadCvDto)
```

Comportement inchangé sinon : la policy vérifie que le candidat n'uploade que son propre CV, ou qu'un utilisateur avec un rôle d'organisation/plateforme effectue l'upload (mêmes règles que la lecture, voir §2 des décisions).

## Coordination frontend — `candidate-app`

### `src/services/BaseClient.ts`

N'a **pas** de méthode de téléchargement binaire aujourd'hui (contrairement à `recruiter-app`). Ajouter :
- Une méthode `downloadFile(service, config?, errorMessage?)` retournant `Promise<Blob>`, sur le modèle de `recruiter-app/src/services/BaseClient.ts::downloadFile` (requête `apiClient.get(service, { responseType: 'blob', ...config })`, retourne `response.data`).
- Un intercepteur d'erreur équivalent à `blobErrorResponseInterceptor` de `recruiter-app` : si la réponse d'erreur a `responseType === 'blob'` et un `content-type` JSON, relire le blob en texte et le reparser en JSON avant de rejeter — sinon un 404/403 renvoyé en JSON par l'API est reçu côté client comme un `Blob` binaire inexploitable pour afficher le message d'erreur. Enregistrer cet intercepteur avant `errorResponseInterceptor` dans le constructeur, comme fait côté `recruiter-app`.

### `src/services/userService.ts`

Ajouter :
```ts
async downloadCv(userId: string): Promise<Blob> {
  return this.downloadFile(`/${userId}/cv`, {}, 'Erreur lors du téléchargement du CV');
}
```

### `src/pages/ProfilePage.vue`

Remplacer `downloadCV()` (actuellement `window.open(user.value.cvPath, '_blank')`) par : appel à `userService.downloadCv(user.value.id)`, récupération du `Blob`, création d'une `URL.createObjectURL(blob)`, déclenchement d'un téléchargement via un élément `<a>` temporaire avec attribut `download`, puis `URL.revokeObjectURL(url)` après déclenchement pour éviter une fuite mémoire.

Attention au nom de fichier proposé au téléchargement : l'endpoint `GET /api/users/{id}/cv` retourne des octets bruts, pas du JSON — `ResumeMetadata.FileName`/`UploadCvResponseDto.FileName` ne sont **jamais** disponibles côté client à ce moment (`UploadCvResponseDto` n'existe que juste après un upload, pas lors d'une consultation ultérieure, qui est le cas réel ici). Ne pas hardcoder une extension (ex. `CV_${firstName}_${lastName}.pdf`) : un CV `.doc`/`.docx` téléchargé serait alors mal nommé avec une extension `.pdf` trompeuse. La source fiable déjà disponible côté client est `user.value.cvPath` : son dernier segment se termine par la véritable extension du fichier uploadé (ex. `resume_20250101_120000.docx`), même si l'URL elle-même n'est plus ouvrable directement — n'en extraire que l'extension (ex. via `cvPath.split('.').pop()`) pour construire un nom de type `CV_${firstName}_${lastName}.${extension}`. Le bouton reste conditionné à `user?.cvPath` (inchangé : `cvPath` continue de servir de simple indicateur de présence d'un CV, sa valeur littérale — l'URL brute du blob — ne doit plus jamais être ouverte directement). En cas d'échec (404 CV introuvable, 401/403), afficher une notification d'erreur visible (le bug actuel étant justement un échec silencieux) — cohérent avec le pattern de gestion d'erreur déjà utilisé ailleurs dans ce fichier/l'application (`useQuasar`/`$q.notify` ou équivalent).

### `settings/index.ts` et `.env.example` — nettoyage

`VITE_STORAGE_BASE_URL` (bloc `storage.baseUrl`) n'est utilisé par aucun code après ce correctif (il ne l'était déjà par aucun code avant). Le supprimer de `settings/index.ts` et de l'entrée correspondante dans `.env.example`, pour éviter qu'un futur développeur ne s'appuie sur cette variable en pensant reconstruire une URL de CV côté client — ce que cette spec interdit précisément de faire.

## Coordination frontend — `recruiter-app`

### `src/services/userService.ts`

Ajouter :
```ts
async downloadCv(userId: string): Promise<Blob> {
  return this.downloadFile(`/${userId}/cv`, {}, 'Erreur lors du téléchargement du CV');
}
```
(`downloadFile` existe déjà dans `BaseClient.ts`, aucune modification requise de ce fichier côté `recruiter-app`.)

### `src/pages/candidates/CvViewPage.vue`

- `<iframe :src="candidate.cvPath">` : remplacer par une `ref` locale contenant une URL d'objet obtenue via `userService.downloadCv(candidate.value.id)` + `URL.createObjectURL(blob)`, récupérée à `onMounted` (en plus de l'appel existant à `userStore.fetchUserById`, qui reste nécessaire pour les métadonnées candidat affichées — nom, email, breadcrumb). Gérer l'état "CV non disponible" existant (`v-else` du template) également pour le cas où le téléchargement échoue en 404 (candidat sans CV, ou CV désynchronisé), pas seulement pour `!candidate.cvPath`.
- `downloadCV()` (actuellement un lien `<a href="candidate.cvPath" download>`, avec `link.download = \`CV_${candidate.value.fullName}.pdf\`` — extension `.pdf` hardcodée, même défaut que côté `candidate-app` à corriger) : pointer vers la même URL d'objet plutôt que vers `candidate.cvPath`, et dériver l'extension réelle depuis `candidate.value.cvPath` plutôt que de la fixer en dur (voir remarque équivalente dans la section `candidate-app` ci-dessus).
- Révoquer l'URL d'objet (`URL.revokeObjectURL`) dans un hook `onUnmounted`, pour éviter une fuite mémoire à chaque navigation vers cette page.

### `src/components/candidates/CvPreviewDialog.vue` + `src/pages/candidates/CandidateDetailPage.vue`

- `CandidateDetailPage.vue` transmet aujourd'hui `candidate.cvPath` brut à `CvPreviewDialog.vue` via la prop `cv-url`. Cette prop doit recevoir une URL d'objet obtenue de la même façon que ci-dessus, pas `candidate.cvPath` directement.
- Le déclenchement du survol (`startHoverTimer`, délai de 1s avant `showCvPreview.value = true`) est un point d'ancrage raisonnable pour lancer l'appel réseau (`userService.downloadCv`) en parallèle du délai, plutôt que d'attendre l'ouverture du dialogue pour commencer le téléchargement — le détail d'implémentation exact (mise en cache de l'URL d'objet pour éviter un re-téléchargement à chaque survol, état de chargement pendant le fetch) est laissé au développeur, la seule exigence de cette spec est qu'un aperçu fonctionnel s'affiche sans URL brute inaccessible.
- Révoquer l'URL d'objet quand elle n'est plus utilisée (fermeture du dialogue ou démontage du composant), pour la même raison qu'au-dessus.

### `src/components/applications/ApplicationDetailDialog.vue`, `ApplicationCard.vue`, `ApplicationTable.vue`

Remplacer `viewResume()` (actuellement `window.open('/resume-viewer', '_blank')`, route inexistante) par une navigation vers la page CV déjà fonctionnelle une fois corrigée ci-dessus : `router.push(`/candidates/${candidateId}/cv`)`, en réutilisant `CvViewPage.vue` plutôt qu'en dupliquant une logique de visualisation. Aucune des trois fonctions actuelles ne reçoit le candidat/l'`Application` concerné en paramètre ; corriger l'appel pour transmettre effectivement l'identifiant :
- `ApplicationDetailDialog.vue` : `props.application?.candidateId` est disponible (prop `application?: Application | null`) — garder l'appel désactivé/no-op si `props.application` est `null`.
- `ApplicationCard.vue` : la prop `application: Application` est déjà dans le scope du template (`application.candidateId`).
- `ApplicationTable.vue` : le bouton "Voir le CV" est dans le template `#body-cell-actions="props"`, où `props.row` est déjà utilisé pour les autres actions (`$emit('view', props.row)`) — utiliser `props.row.candidateId`, ce qui implique de passer `props.row` à `viewResume()` (actuellement appelée sans argument : `@click="viewResume()"`).

Aucun changement n'est requis dans `router/routes.ts` : la route `candidates/:id/cv` existe déjà et reste le point d'entrée cible.

### `settings/index.ts` et `.env.example` — nettoyage

Même suppression que côté `candidate-app` : `VITE_STORAGE_BASE_URL`/`settings.storage.baseUrl`, non utilisé avant ce correctif et non nécessaire après (le composant `.claude/docs/architecture.md` de ce package mentionne cette variable comme "également prévue" — cette mention devient obsolète et doit être retirée en cohérence, à la ligne correspondante de `architecture.md`).

## Rétrocompatibilité

- Aucune donnée en base n'est modifiée : `User.CvPath` continue de contenir l'URL brute du blob exactement comme aujourd'hui, pour les CV déjà uploadés comme pour les nouveaux. Le nouvel endpoint la lit sans la transformer ni la republier.
- `UserDto.CvPath`, `UserProfileDto.CvPath`, `UploadCvResponseDto.CvPath` restent inchangés dans leur définition (aucun champ ajouté/retiré) ; leur usage côté frontend change de nature (indicateur de présence uniquement, plus jamais ouvert directement).
- `ResumeService.cs`/`IResumeService.cs` : aucune modification. Le correctif Azurite déjà en place (`BlobUriBuilder`) reste inchangé et continue de fonctionner à l'identique pour les deux méthodes désormais consommées.
- `POST /api/users/{id}/cv` : l'ajout de `[Authorize]` change le comportement observable pour un appelant non authentifié (401 au lieu d'un upload silencieusement accepté) — changement de comportement intentionnel (voir décision §3), pas une régression au sens de cette spec.
- Aucun changement de schéma EF Core, aucune migration requise.

## Critères d'acceptation / tests

1. `GET /api/users/{id}/cv` avec un token JWT valide du candidat propriétaire (`id` = son propre `userId`), CV existant : retourne `200`, `Content-Type` correspondant au type de fichier réellement uploadé (`application/pdf`, `application/msword`, ou `.docx` équivalent), corps = contenu binaire exact du fichier uploadé.
2. `GET /api/users/{id}/cv` par un autre candidat (`userId` différent, rôle `Candidate` uniquement, sans rôle d'organisation) : `403 Forbidden` (échec de la policy `CandidateOwnDataAccess`).
3. `GET /api/users/{id}/cv` par un utilisateur avec un rôle d'organisation (`Recruiter`, `Manager`, `OrganizationAdmin`, `TechnicalEvaluator`), quelle que soit son organisation : `200` (comportement de la policy réutilisée, cf. limitation documentée §2 — testé et accepté tel quel, pas un bug de cette spec).
4. `GET /api/users/{id}/cv` sans authentification (pas de header `Authorization`) : `401 Unauthorized`.
5. `GET /api/users/{id}/cv` pour un `id` d'utilisateur existant mais sans CV uploadé (`CvPath` `null`/vide) : `404 Not Found`.
6. `GET /api/users/{id}/cv` pour un `id` d'utilisateur inexistant : `404 Not Found`.
7. `POST /api/users/{id}/cv` sans authentification : `401 Unauthorized` (non-régression testable : ce comportement n'existait pas avant ce correctif — vérifier explicitement qu'il est désormais en place).
8. Cycle complet **upload → GET CV** via les deux endpoints : uploader un CV via `POST /api/users/{id}/cv` (candidat authentifié), puis le récupérer via `GET /api/users/{id}/cv` (même candidat) : le contenu binaire retourné est identique octet pour octet au fichier uploadé.
9. `candidate-app` : sur `ProfilePage.vue`, avec un CV déjà uploadé, cliquer sur "Télécharger mon CV" déclenche un téléchargement de fichier exploitable par le navigateur (pas d'onglet vide, pas d'erreur XML) — vérifiable manuellement en local (Azurite) et par revue de code pour la logique d'erreur.
10. `recruiter-app` : sur `CandidateDetailPage.vue`, le survol du bouton "Consulter le CV" affiche un aperçu du CV dans `CvPreviewDialog.vue` sans URL brute inaccessible ; le clic navigue vers `/candidates/{id}/cv` (`CvViewPage.vue`) qui affiche le CV dans l'`iframe` et permet son téléchargement via le bouton "Télécharger".
11. `recruiter-app` : depuis `ApplicationsPage.vue` (table, cartes, ou dialogue de détail d'une candidature), le bouton "Voir le CV" navigue vers `/candidates/{candidateId}/cv` avec le bon `candidateId` (celui de l'`Application` affichée), et non plus vers `/resume-viewer`.
12. Aucune occurrence résiduelle de `window.open(...cvPath...)`, `<iframe :src="...cvPath...">`, `<a href="...cvPath...">` ou de la route `/resume-viewer` dans `candidate-app`/`recruiter-app` après le correctif (vérifiable par recherche textuelle sur `cvPath` et `resume-viewer` dans les deux packages : les seules occurrences restantes de `cvPath` doivent être des tests de présence, `v-if="...cvPath"` / `if (...cvPath)`, jamais une valeur assignée à `src`/`href`/`window.open`).
13. `VITE_STORAGE_BASE_URL` n'apparaît plus dans `settings/index.ts` ni `.env.example` d'aucun des deux packages frontend, ni dans `recruiter-app/.claude/docs/architecture.md`.
14. Pour un CV uploadé au format `.docx` : le nom de fichier proposé au téléchargement (`candidate-app::ProfilePage.vue` et `recruiter-app::CvViewPage.vue`) se termine par `.docx`, pas par `.pdf` — vérifie que l'extension est bien dérivée de `cvPath` et non hardcodée.

## Hors périmètre

- **Scoping de l'autorisation à la candidature réelle** (policy plus stricte que `CandidateOwnDataAccess`, ou nouvel endpoint `GET /api/applications/{id}/cv`) : limitation connue, sciemment non corrigée dans cette spec (voir décision §2). Piste pour un futur correctif, non implémentée ici.
- **Prévisualisation inline des formats `.doc`/`.docx`** dans les `iframe` (`CvViewPage.vue`, `CvPreviewDialog.vue`) : limitation déjà présente avant ce correctif (les navigateurs ne rendent nativement que le PDF en `iframe`), non traitée ici — l'objectif de cette spec est de rendre le contenu du CV *accessible*, pas d'ajouter un rendu multi-format.
- **Génération de SAS token (Option A)** : explicitement écartée, voir décision §1.
- **Migration ou script de resynchronisation** des `CvPath` incohérents (référence en base vers un blob supprimé côté storage) : le cas est géré au niveau applicatif par un `404` (§ Comportement cible, étape 3), aucune tâche de nettoyage de données n'est ajoutée.
- **`ResumeService.cs`/`IResumeService.cs`** : aucune modification, ces fichiers sont uniquement consommés tels qu'ils existent.
- **`ApplicationsController.cs`** : aucune modification. Le point d'entrée retenu pour la consultation du CV depuis une candidature reste `GET /api/users/{id}/cv`, atteint indirectement via la page candidat (`/candidates/{candidateId}/cv`), pas un nouvel endpoint sous `/api/applications`.
- **Autorisation de `GET /api/applications/{id}`** (actuellement protégée par le seul `[Authorize]` de classe, sans policy plus fine — tout utilisateur authentifié peut consulter n'importe quelle candidature par ID) : faille distincte, découverte en cours d'exploration, non corrigée ici — hors périmètre de cette spec, à traiter dans un correctif séparé si nécessaire.
- **Tests automatisés** : cette spec définit des critères d'acceptation vérifiables manuellement ou par tests d'intégration ; l'écriture effective de tests (`XpertSphere.MonolithApi.Tests`) relève de l'agent `developer`, pas de cette spécification.

## Fichiers à créer/modifier (récapitulatif)

Backend (`XpertSphere.MonolithApi`) :
- `Interfaces/IUserService.cs` : nouvelle méthode `GetCvForDownloadAsync`, nouvelle classe `CvDownloadResult`.
- `Services/UserService.cs` : implémentation de `GetCvForDownloadAsync`.
- `Controllers/UsersController.cs` : nouvel endpoint `GET {id:guid}/cv` (`DownloadCv`), ajout de `[Authorize(Policy = "CandidateOwnDataAccess")]` sur `UploadCv`.

Frontend `candidate-app` :
- `src/services/BaseClient.ts` : nouvelle méthode `downloadFile`, nouvel intercepteur d'erreur blob→JSON.
- `src/services/userService.ts` : nouvelle méthode `downloadCv`.
- `src/pages/ProfilePage.vue` : `downloadCV()` réécrite (blob + object URL + téléchargement + gestion d'erreur visible).
- `src/settings/index.ts`, `.env.example` : suppression de `VITE_STORAGE_BASE_URL`/`storage.baseUrl`.

Frontend `recruiter-app` :
- `src/services/userService.ts` : nouvelle méthode `downloadCv`.
- `src/pages/candidates/CvViewPage.vue` : chargement du CV via l'endpoint sécurisé (iframe + téléchargement), révocation de l'URL d'objet.
- `src/components/candidates/CvPreviewDialog.vue`, `src/pages/candidates/CandidateDetailPage.vue` : la prop `cv-url` reçoit une URL d'objet issue de l'endpoint sécurisé, plus `candidate.cvPath` brut.
- `src/components/applications/ApplicationDetailDialog.vue`, `ApplicationCard.vue`, `ApplicationTable.vue` : `viewResume()` corrigée (navigation vers `/candidates/{candidateId}/cv`, avec le `candidateId` effectivement transmis).
- `src/settings/index.ts`, `.env.example`, `.claude/docs/architecture.md` : suppression/retrait de la mention `VITE_STORAGE_BASE_URL`/`storage.baseUrl`.

Aucun changement dans `docker-compose.yml`, les migrations EF Core, ou les autres services du monorepo (`CommunicationService`, `ReportingService`, `IntegrationService`, `ResumeAnalyzer`).
