# Scoping des endpoints de détail de rôle + câblage du dialog "Utilisateurs avec ce rôle"

## Contexte

Fait suite à `role-user-count-organization-scope-fix.md` (déjà mergé dans `develop`, commit `ed7ddfb`), qui a scopé par organisation `RoleDto.UsersCount` sur `GET /api/Roles/paginated` (`RoleService.BuildRoleQuery`) et la liste retournée par `GET /api/UserRoles/role/{roleId}` (`UserRoleService.GetRoleUsersAsync`). Cette spec traite les deux trous restants explicitement mis de côté par le ticket précédent :

1. Deux autres méthodes de `RoleService` présentent le même défaut de scoping que le Bug 1 déjà corrigé, mais sur des chemins non couverts par ce correctif (`GetRoleByIdAsync`/`GetRoleByNameAsync`, et `UpdateRoleAsync`).
2. Le dialog "Utilisateurs avec ce rôle" de `RolesPage.vue` (`recruiter-app`), déjà présent dans le template mais jamais déclenché ("Points à confirmer" 1 et 2 de la spec précédente), est câblé ici — ce qui implique de lever la restriction d'autorisation qui bloquait `Organization.Admin` sur `GET /api/UserRoles/role/{roleId}`.

## Bug 3 — `GetRoleByIdAsync`/`GetRoleByNameAsync` non scopés (latent, même cause que Bug 1)

`Services/RoleService.cs` :

- `GetRoleByIdAsync` (lignes ~104-127) et `GetRoleByNameAsync` (lignes ~129-152) chargent chacune `_context.Roles.Include(r => r.UserRoles).Include(r => r.RolePermissions)` **sans filtre d'organisation**, exactement comme `BuildRoleQuery` avant son correctif. `RoleMappingProfile.cs:22` compte alors tous les `UserRole` actifs toutes organisations confondues dans `RoleDto.UsersCount`.
- **Latent aujourd'hui, pas visible en usage normal** : les endpoints `GET /api/Roles/{id}` et `GET /api/Roles/by-name/{name}` sont bien accessibles à un `Organization.Admin` (`[Authorize(Policy = "RequireInternalUser")]`), et `roleStore.ts` expose bien `fetchRoleById`/`fetchRoleByName` — mais recherche exhaustive confirmée : aucun fichier `.vue` de `recruiter-app` n'appelle ces deux actions du store (seules `fetchAllRoles`/`fetchPaginatedRoles` sont utilisées, dans `UserRoleAssignment.vue`, `UsersPage.vue` et `RolesPage.vue`, jamais pour afficher `usersCount` issu de ces deux méthodes précises). Corrigé ici **par cohérence structurelle**, même traitement que Bug 2 de la spec précédente — pas parce qu'un utilisateur voit aujourd'hui un nombre erroné via ce chemin.

## Bug 4 — `UpdateRoleAsync` : `UsersCount` retourné à `0` après une mise à jour (latent, cause différente)

`Services/RoleService.cs`, `UpdateRoleAsync` (lignes ~193-224) :

```csharp
var role = await _context.Roles.FindAsync(id);
// ...
_mapper.Map(updateRoleDto, role);
await _context.SaveChangesAsync();
var roleDto = _mapper.Map<RoleDto>(role);
```

`FindAsync` ne prend pas d'`Include`. Le projet n'active pas les lazy-loading proxies (vérifié : aucune occurrence de `UseLazyLoadingProxies`/`AddDbContext` avec proxy dans le service), donc `role.UserRoles` — bien que déclarée `virtual` dans `Models/Role.cs` — reste la collection vide initialisée par défaut (`= new List<UserRole>()`) et n'est jamais peuplée par `FindAsync`. Conséquence : `RoleMappingProfile` calcule `UsersCount = 0` dans le `RoleDto` retourné par `PUT /api/Roles/{id}`, **quel que soit le nombre réel d'utilisateurs affectés au rôle**.

- Cause différente de Bug 3 (absence totale d'`Include`, pas juste absence de filtre d'organisation) mais même symptôme final sur le contrat de sortie.
- **Latent aujourd'hui** : `PUT /api/Roles/{id}` est protégé par `[Authorize(Policy = "RequirePlatformSuperAdminRole")]` (seul `PlatformSuperAdmin` peut l'appeler), et côté frontend, `RolesPage.vue` (`saveRole`, ligne ~423-457) ignore la réponse de `roleStore.updateRole(...)` et enchaîne systématiquement sur un `fetchRoles()` complet (`GET /api/Roles/paginated`, déjà scopé/correct) avant tout rendu. Le `UsersCount = 0` erroné n'est donc jamais affiché à l'écran — mais la réponse JSON de l'endpoint est objectivement incohérente avec l'état réel, et le serait immédiatement si un futur appelant (script, autre client, futur écran) utilisait cette réponse directement sans refetch.

## Bug 5 — Dialog "Utilisateurs avec ce rôle" jamais déclenché (`RolesPage.vue`)

`src/frontend/packages/recruiter-app/src/pages/admin/RolesPage.vue` :

- Le dialog (`v-if="selectedRole" v-model="showUserRolesDialog"`, lignes 174-225) et toute sa logique de mutation (`toggleUserRoleStatus`/`removeUserRole`, lignes 508-526, appelant `userRoleStore.updateUserRoleStatus`/`removeRoleFromUser`) sont déjà écrits et fonctionnels.
- Mais le menu d'actions de chaque ligne de rôle (lignes 67-108) ne propose que "Modifier", "Activer/Désactiver" et "Supprimer" : **aucune action n'assigne `selectedRole.value`, n'appelle `userRoleStore.fetchRoleUsers(...)`, ni ne positionne `showUserRolesDialog.value = true`.** Le dialog est aujourd'hui du code mort côté template.

### Bug 5bis — mauvais nom de champ dans la colonne "Utilisateur" du dialog

`userRoleColumns` (ligne 328) :
```ts
{ name: 'userName', field: 'userName', label: 'Utilisateur', ...dataTable.defaultConfig.value },
```
`UserRoleDto` (`models/userRole.ts:17`) n'a pas de champ `userName` — le champ réel est `userFullName`. Si le dialog est câblé sans corriger cette colonne, elle s'affichera vide pour chaque ligne. Trouvé pendant l'exploration de ce ticket, corrigé ici dans le même mouvement (pas une régression introduite par le câblage, un défaut préexistant révélé par lui).

## Root cause (Bug 3 et 4)

Même racine que la spec précédente : `Role` est une entité globale à la plateforme (pas de colonne `OrganizationId`), et le pattern de scoping conditionnel (`ICurrentUserService.OrganizationId`, déjà répliqué dans `BuildRoleQuery` et `GetRoleUsersAsync`) n'a simplement pas été appliqué aux deux méthodes restantes qui projettent aussi `RoleDto.UsersCount`.

## Objectif et périmètre

- Appliquer le même filtre conditionnel d'organisation (identique au pattern déjà établi dans `BuildRoleQuery`/`GetRoleUsersAsync`) à `GetRoleByIdAsync` et `GetRoleByNameAsync`.
- Corriger `UpdateRoleAsync` pour que le `RoleDto` retourné reflète le nombre réel d'utilisateurs affectés (recharger avec `Include` après la sauvegarde, en réutilisant le même pattern de scoping conditionnel — qui restera toujours "non scopé" en pratique puisque seul un appelant plateforme atteint cet endpoint, mais garde le code cohérent avec le reste du service et robuste si la policy change un jour).
- Introduire une nouvelle policy d'autorisation `RequirePlatformOrOrganizationAdminRole` (`PlatformSuperAdmin`/`PlatformAdmin`/`Organization.Admin`) et l'appliquer à `GET /api/UserRoles/role/{roleId}` (`UserRolesController.GetRoleUsers`), en remplacement de `RequirePlatformRole`.
- Câbler une nouvelle action "Voir les utilisateurs" dans le menu contextuel de chaque ligne de `RolesPage.vue`, qui ouvre le dialog existant en appelant `userRoleStore.fetchRoleUsers(role.id)`.
- Corriger `userRoleColumns` (`field: 'userName'` → `field: 'userFullName'`) dans le même fichier.
- **Hors périmètre** :
  - Ajouter une colonne `OrganizationId` sur `Role`, ou revoir la visibilité des lignes de rôle elles-mêmes — inchangé, comme dans la spec précédente.
  - `GetAllRolesAsync` (`GET /api/Roles`, non paginé) : même défaut structurel que Bug 3, mais confirmé toujours non affiché nulle part (`fetchAllRoles` alimente uniquement des sélecteurs de rôle dans `UserRoleAssignment.vue`/`UsersPage.vue`/`RolesPage.vue`, jamais rendu avec son `usersCount`). Laissé de côté pour la même raison que dans la spec précédente ; à traiter si un usage affichant ce champ apparaît un jour.
  - **Scoping par organisation de `RemoveRoleFromUserAsync`/`UpdateUserRoleStatusAsync`** (`UserRoleService.cs`) : ces deux méthodes localisent l'affectation uniquement par `userRoleId` (`_context.UserRoles.FindAsync`/`.FirstOrDefaultAsync(ur => ur.Id == userRoleId)`), sans jamais vérifier que l'organisation de l'utilisateur ciblé correspond à celle de l'appelant. `CanCreateUsers` (policy de `RemoveRoleFromUser`) et `RequireInternalUser` (policy de `UpdateUserRoleStatus`) admettent déjà `Organization.Admin` aujourd'hui, indépendamment de ce ticket — ce défaut est donc **préexistant** et non introduit par le câblage du dialog. Décision produit confirmée : le dialog reste pleinement mutable (lecture + toggle + suppression) pour quiconque peut l'ouvrir, sans le rendre lecture seule pour `Organization.Admin`, car (a) aucune capacité API nouvelle ne lui est accordée par ce ticket, et (b) la liste affichée dans le dialog est elle-même scopée par `GetRoleUsersAsync`, donc un `Organization.Admin` n'y voit et n'y agit jamais que sur des `userRoleId` de sa propre organisation en usage normal via cette UI. L'absence de vérification d'organisation sur ces deux méthodes reste documentée ici comme point préexistant hors périmètre, à traiter dans un ticket dédié si confirmé nécessaire — même traitement que la nuance de `UserService.BuildUserQuery` documentée dans la spec précédente.
  - Élargir `CanCreateUsers`/`RequireInternalUser` eux-mêmes : inchangés, seule la policy de `GetRoleUsers` change.
  - Aucun changement de contrat JSON (`RoleDto`, `UserRoleDto` gardent les mêmes champs/types).

## Acteurs et permissions

- **`PlatformAdmin`/`PlatformSuperAdmin`** : comportement inchangé sur Bug 3/4 (voient toujours le total toutes organisations confondues, aucun scoping ne s'applique à eux). Peuvent désormais aussi ouvrir le dialog "Utilisateurs avec ce rôle" (déjà le cas avant ce ticket via la policy `RequirePlatformRole`, non régressé par le changement de policy).
- **`Organization.Admin`** : `usersCount` de `GetRoleByIdAsync`/`GetRoleByNameAsync` désormais scopé à sa seule organisation (effet non visible aujourd'hui, cf. "latent" ci-dessus, mais correct par cohérence). **Peut désormais ouvrir le dialog "Utilisateurs avec ce rôle"** depuis `RolesPage.vue` (nouvelle policy `RequirePlatformOrOrganizationAdminRole`), y voir la liste scopée à son organisation (déjà garanti par le correctif précédent sur `GetRoleUsersAsync`), et y activer/désactiver ou retirer un rôle utilisateur — capacités déjà permises par les policies existantes de ces deux mutations (`CanCreateUsers`, `RequireInternalUser`), simplement rendues accessibles via cette UI pour la première fois.
- **`Organization.Manager`** : même règle de scoping que `Organization.Admin` au niveau service pour Bug 3 (réplique du pattern déjà établi), mais n'accède ni à la page `admin/roles` (`adminSectionGuard` ne l'autorise pas) ni, avec la nouvelle policy `RequirePlatformOrOrganizationAdminRole`, à `GET /api/UserRoles/role/{roleId}` — reçoit un 403 en appel direct, cohérent avec le fait qu'il ne voit jamais cette page.
- **`Organization.Recruiter`/`Organization.TechnicalEvaluator`** : non concernés (pas d'accès à `admin/roles`, pas de scoping).

## Règles métier et cas limites

- Le scoping ajouté à `GetRoleByIdAsync`/`GetRoleByNameAsync` suit exactement les mêmes règles que celles déjà spécifiées pour `BuildRoleQuery`/`GetRoleUsersAsync` dans `role-user-count-organization-scope-fix.md` : appelant plateforme → non scopé ; `Organization.Admin`/`Organization.Manager` non-plateforme avec `OrganizationId` renseigné → scopé ; rôle sans utilisateur dans l'organisation de l'appelant → `UsersCount = 0`, pas d'erreur, la ligne de rôle reste renvoyée ; `OrganizationId` absent malgré le rôle → pas de filtre appliqué (comportement non scopé, pas d'exception), par cohérence stricte avec le pattern déjà en place.
- `UpdateRoleAsync` : le rechargement post-sauvegarde doit se faire **après** `SaveChangesAsync` (pour refléter l'état à jour du rôle, notamment `IsActive`/`DisplayName`/`Description` modifiés) et **avant** le mapping vers `RoleDto`. Ne pas réutiliser l'entité `role` déjà trackée par le `DbContext` de la méthode pour peupler `UserRoles` (le "relationship fixup" du tracker pourrait réattacher des `UserRole` déjà chargés par erreur sur le même contexte, cf. piège documenté dans `role-user-count-organization-scope-fix.md`) : effectuer une requête de lecture séparée en `AsNoTracking()`, par exemple en réutilisant un helper privé partagé avec `GetRoleByIdAsync` (voir "Contrat d'interface" ci-dessous), plutôt que de manipuler la même instance suivie.
- Le câblage du dialog dans `RolesPage.vue` ne doit pas dupliquer la logique déjà existante d'ouverture d'un `q-menu` par ligne : ajouter l'action "Voir les utilisateurs" comme un item de plus dans le même `q-list` que "Modifier"/"Activer-Désactiver"/"Supprimer" (lignes 67-108), pas un nouveau menu séparé.
- L'action "Voir les utilisateurs" doit être disponible pour toute ligne de rôle visible par l'appelant (pas de condition supplémentaire côté template : la policy backend est le seul garde-fou nécessaire — un rôle listé dans `admin/roles` est par construction un rôle que l'appelant a le droit de consulter).
- Ordre des opérations au clic sur "Voir les utilisateurs" : `selectedRole.value = role` → `showUserRolesDialog.value = true` → `await userRoleStore.fetchRoleUsers(role.id)` (ou l'inverse pour l'ordre `fetch` puis affichage — au choix du developer, mais le dialog doit afficher son état `loading` via `userRoleStore.isLoading` pendant le chargement, déjà câblé dans le template `q-table :loading="userRoleStore.isLoading"`, ligne 184). Si `fetchRoleUsers` échoue (ex. 403 imprévu), garder le comportement d'erreur déjà standard du store (notification d'erreur via `useNotification`), ne pas fermer le dialog automatiquement.
- Fermeture du dialog (bouton "Fermer", ligne 222) : ne réinitialise pas `selectedRole` aujourd'hui (comportement existant, non modifié) — le `v-if="selectedRole"` sur le `q-dialog` empêche simplement son rendu tant qu'aucun rôle n'a été sélectionné une première fois ; laisser ce comportement inchangé.

## Contrat d'interface — Backend (`XpertSphere.MonolithApi`)

### `Services/RoleService.cs` — helper partagé pour Bug 3 et Bug 4

Extraire la logique de scoping déjà écrite dans `BuildRoleQuery` (lignes ~361-381) dans une méthode privée réutilisable, par exemple :

```csharp
private IQueryable<Role> BuildScopedRoleDetailQuery()
{
    var isAuthenticated = _currentUserService.User?.Identity?.IsAuthenticated == true;
    var isPlatformUser = isAuthenticated &&
        (_currentUserService.User!.IsInRole(Roles.PlatformSuperAdmin.Name) ||
         _currentUserService.User.IsInRole(Roles.PlatformAdmin.Name));
    var isOrgAdmin = isAuthenticated && _currentUserService.User!.IsInRole(Roles.OrganizationAdmin.Name);
    var isManager = isAuthenticated && _currentUserService.User!.IsInRole(Roles.Manager.Name);

    var shouldScopeUserRolesToOrganization =
        (isOrgAdmin || isManager) && !isPlatformUser && _currentUserService.OrganizationId.HasValue;

    return shouldScopeUserRolesToOrganization
        ? _context.Roles
            .AsNoTracking()
            .Include(r => r.UserRoles.Where(ur => ur.User.OrganizationId == _currentUserService.OrganizationId!.Value))
            .ThenInclude(ur => ur.User)
            .Include(r => r.RolePermissions)
        : _context.Roles
            .AsNoTracking()
            .Include(r => r.UserRoles)
            .ThenInclude(ur => ur.User)
            .Include(r => r.RolePermissions);
}
```

- `GetRoleByIdAsync` : remplacer le corps de la requête par `await BuildScopedRoleDetailQuery().Where(r => r.Id == id).FirstOrDefaultAsync();` (reste du comportement, y compris `NotFound`, inchangé).
- `GetRoleByNameAsync` : `await BuildScopedRoleDetailQuery().Where(r => r.Name == name).FirstOrDefaultAsync();`, même principe.
- `BuildRoleQuery` (utilisée par `GetAllPaginatedRolesAsync`) peut optionnellement être refactorée pour réutiliser ce même helper pour la partie `Include` (avant d'enchaîner ses propres filtres `IsActive`/`UserId`/`SearchTerms`/tri) — refactor recommandé pour éviter la triplication de la logique de scoping, mais non obligatoire : ne pas modifier son comportement observable, qui est déjà correct et testé.
- `UpdateRoleAsync` : conserver le flux actuel (`_context.Roles.FindAsync(id)` pour la mise à jour trackée, `_mapper.Map(updateRoleDto, role)`, `SaveChangesAsync()`), puis **avant** de construire le `RoleDto` de retour, recharger avec le helper :
  ```csharp
  var reloadedRole = await BuildScopedRoleDetailQuery().Where(r => r.Id == id).FirstOrDefaultAsync();
  var roleDto = _mapper.Map<RoleDto>(reloadedRole ?? role);
  ```
  Le fallback `?? role` couvre le cas théorique où le rôle aurait disparu entre la sauvegarde et le rechargement (non attendu en usage normal, mais évite un `NullReferenceException` improbable plutôt que de renvoyer un `NotFound` après un `UpdateRoleAsync` qui a déjà réussi).
- Note DI : `BuildScopedRoleDetailQuery` n'ajoute aucune dépendance nouvelle (`_currentUserService` déjà injecté dans `RoleService`).

### `Extensions/SecurityExtensions.cs` — nouvelle policy

Ajouter, dans `AddAuthorizationPolicies`, à proximité des autres policies basées sur des rôles multiples (`RequireInternalUser`, `RequirePlatformRole`) :

```csharp
options.AddPolicy("RequirePlatformOrOrganizationAdminRole", policy =>
    policy.RequireRole(
        Roles.PlatformSuperAdmin.Name,
        Roles.PlatformAdmin.Name,
        Roles.OrganizationAdmin.Name));
```

`RequireRole(...)` avec plusieurs valeurs est une condition **OR** (satisfaite si l'utilisateur possède au moins un des rôles listés) — comportement standard ASP.NET Core Authorization, cohérent avec l'usage déjà fait de `RequireRole(Roles.InternalRoles)` (tableau) ailleurs dans le même fichier.

### `Controllers/UserRolesController.cs`

```csharp
[HttpGet("role/{roleId:guid}")]
[Authorize(Policy = "RequirePlatformOrOrganizationAdminRole")]
public async Task<ActionResult<IEnumerable<UserRoleDto>>> GetRoleUsers(Guid roleId)
```
(remplace `RequirePlatformRole`). Aucun autre endpoint de ce contrôleur n'est modifié par ce ticket.

## Contrat d'interface — Frontend (`recruiter-app`), coordination

### `src/pages/admin/RolesPage.vue`

1. **Nouvelle action de menu**, ajoutée dans le `q-list` du menu contextuel de chaque ligne (lignes 67-108), par exemple entre "Modifier" et le séparateur précédant "Supprimer" :
   ```html
   <q-item v-close-popup clickable @click="viewRoleUsers(props.row)">
     <q-item-section avatar>
       <q-icon name="group" color="primary" />
     </q-item-section>
     <q-item-section>Voir les utilisateurs</q-item-section>
   </q-item>
   ```
   (Nom d'icône indicatif — le developer choisit une icône Material Icons cohérente avec le reste de la page, ex. `group` ou `people`.)

2. **Nouvelle fonction `viewRoleUsers`**, dans le `<script setup>`, à ajouter à côté des autres handlers (`editRole`, `confirmDelete`, etc.) :
   ```ts
   const viewRoleUsers = async (role: RoleDto) => {
     selectedRole.value = role;
     showUserRolesDialog.value = true;
     await userRoleStore.fetchRoleUsers(role.id);
   };
   ```
   `selectedRole` et `showUserRolesDialog` existent déjà (lignes 249-251) ; `userRoleStore.fetchRoleUsers` existe déjà et est déjà correctement typé/branché sur `GET /api/UserRoles/role/{roleId}` (`userRoleService.getRoleUsers`, `services/userRoleService.ts:14-15`).

3. **Correction du champ de colonne** (ligne 328) :
   ```ts
   { name: 'userName', field: 'userFullName', label: 'Utilisateur', ...dataTable.defaultConfig.value },
   ```
   Le `name` de colonne (identifiant technique Quasar) peut rester `userName` ou être renommé `userFullName` par cohérence — seul le `field` doit impérativement correspondre à `userFullName` du DTO. Recommandé : renommer aussi `name` en `userFullName` pour éviter toute confusion future, mais ce n'est pas fonctionnellement requis (Quasar résout l'affichage via `field`, pas `name`).

Aucun autre fichier frontend n'est à modifier : `userRoleStore.fetchRoleUsers`, le dialog, ses colonnes (hors le champ corrigé), et les actions `toggleUserRoleStatus`/`removeUserRole` existent déjà et n'ont pas besoin de changement.

## Points à confirmer (aucun, décisions déjà tranchées)

Les deux décisions ouvertes identifiées pendant l'exploration ont été tranchées par l'utilisateur avant rédaction :
- Nouvelle policy `RequirePlatformOrOrganizationAdminRole` (confirmé).
- Dialog pleinement mutable pour `Organization.Admin`, absence de scoping d'organisation sur `RemoveRoleFromUserAsync`/`UpdateUserRoleStatusAsync` documentée comme point préexistant hors périmètre (confirmé).

## Critères d'acceptation

1. `GET /api/Roles/{id}` et `GET /api/Roles/by-name/{name}`, appelés par un `Organization.Admin` de "Meilleurtaux" pour un rôle ayant des utilisateurs actifs chez "Meilleurtaux" et chez "Expertime" → `usersCount` ne compte que les utilisateurs actifs de "Meilleurtaux".
2. Mêmes appels effectués par un `PlatformAdmin`/`PlatformSuperAdmin` → `usersCount` reste le total toutes organisations confondues (non-régression).
3. `PUT /api/Roles/{id}` (par un `PlatformSuperAdmin`) sur un rôle ayant N utilisateurs actifs assignés, avec une mise à jour ne touchant que `displayName`/`description`/`isActive` → la réponse `RoleDto` renvoie `usersCount = N` (pas `0`).
4. Test unitaire/service sur `RoleService.GetRoleByIdAsync`/`GetRoleByNameAsync` : avec un `Mock<ICurrentUserService>` configuré en `Organization.Admin` + `OrganizationId` donné, et des `UserRole` de deux organisations différentes en base de test, le `UsersCount` retourné ne compte que les `UserRole` de l'organisation du mock ; même test avec un mock `PlatformAdmin` → compte total toutes organisations.
5. Test unitaire/service sur `RoleService.UpdateRoleAsync` : rôle avec des `UserRole` actifs préexistants en base de test, mise à jour de `DisplayName` uniquement → le `RoleDto` retourné a un `UsersCount` égal au nombre réel d'utilisateurs actifs, pas `0`.
6. `GET /api/UserRoles/role/{roleId}` appelé par un `Organization.Admin` → `200 OK` avec la liste scopée à son organisation (au lieu du `403 Forbidden` actuel) ; appelé par un `Organization.Manager` ou un `Organization.Recruiter` → toujours `403 Forbidden` (non-régression, la nouvelle policy n'admet pas ces rôles).
7. Sur `recruiter-app`, un `Organization.Admin` connecté sur `admin/roles` : cliquer sur "Voir les utilisateurs" dans le menu d'un rôle ouvre le dialog, affiche la liste des utilisateurs de sa seule organisation ayant ce rôle (avec la colonne "Utilisateur" correctement renseignée, plus vide), et les boutons toggle/suppression y fonctionnent (appels `PUT .../status` et `DELETE ...` aboutissent, cohérent avec les policies déjà en place sur ces deux endpoints).
8. Même parcours pour un `PlatformAdmin`/`PlatformSuperAdmin` : le dialog affiche tous les utilisateurs ayant ce rôle, toutes organisations confondues (non-régression du comportement déjà existant pour ce profil).
9. Suite de tests existante (`dotnet test` depuis `XpertSphere.MonolithApi.Tests`) : aucun test préexistant sur `RoleService`/`UserRoleService`/`UserRolesController` ne casse suite à ce correctif.
10. Non-régression : la visibilité des lignes de rôle elles-mêmes, le filtre `filter.UserId` de `GET /api/Roles/paginated`, et le comportement de `GetAllPaginatedRolesAsync`/`GetRoleUsersAsync` déjà corrigés par la spec précédente restent strictement inchangés.
