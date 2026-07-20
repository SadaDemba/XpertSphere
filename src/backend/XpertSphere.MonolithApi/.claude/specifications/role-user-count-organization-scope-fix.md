# Comptage/liste des utilisateurs par rôle non scopés par organisation

## Contexte et diagnostic (à partir du code réel)

Signalement initial : sur la page de gestion des rôles (`recruiter-app`, `pages/admin/RolesPage.vue`), le nombre d'utilisateurs affiché par rôle (colonne "Utilisateurs") compte les utilisateurs de **toutes les organisations**, alors qu'un `Organization.Admin` consultant cette page ne devrait voir que le nombre/la liste des utilisateurs de **sa propre organisation**.

`Role` (`Models/Role.cs`) est une entité globale à la plateforme, sans colonne `OrganizationId` : un même rôle (ex. `Organization.Recruiter`) est partagé par toutes les organisations clientes. C'est la source des deux défauts ci-dessous.

### Bug 1 — `UsersCount` sur la liste paginée des rôles (bug confirmé, visible en usage normal)

- `Mappings/RoleMappingProfile.cs:22` :
  ```csharp
  .ForMember(dest => dest.UsersCount, opt => opt.MapFrom(src => src.UserRoles.Count(ur => ur.IsActive)));
  ```
  Compte tous les `UserRole` actifs rattachés au rôle, sans filtre d'organisation.
- Alimenté par `Services/RoleService.cs`, `BuildRoleQuery` (lignes ~359-364), utilisée par `GetAllPaginatedRolesAsync` (ligne ~88, endpoint `GET /api/Roles/paginated`) :
  ```csharp
  var query = _context.Roles
      .Include(r => r.UserRoles)
      .ThenInclude(ur => ur.User)
      .AsQueryable();
  ```
  Le seul filtre existant sur la requête (lignes ~366-377) masque les *rôles plateforme* aux utilisateurs non-plateforme (visibilité des lignes de rôle elle-même — correcte, non concernée par ce correctif), mais ne filtre jamais la sous-collection `UserRoles` incluse par organisation.
- Chaîne de bout en bout confirmée : `recruiter-app/src/stores/roleStore.ts` (`fetchPaginatedRoles`, lignes 74-109) → `GET /api/Roles/paginated` (`[Authorize(Policy = "RequireInternalUser")]`, qui inclut `Organization.Admin`) → colonne `usersCount` affichée dans `pages/admin/RolesPage.vue` (colonne `usersCount`, lignes ~292-299).
- Reachabilité confirmée : la route `admin/roles` (`router/routes.ts`) est protégée par `adminSectionGuard` = `[...PlatformRoles, Organization.Admin]`. Un `Organization.Admin` accède donc bien à cette page en usage normal et y voit aujourd'hui un nombre erroné (toutes organisations confondues). **Ce bug est réellement visible pour l'utilisateur final.**

### Bug 2 — `GetRoleUsersAsync` (dialog détail rôle), même cause mais bug **latent** aujourd'hui (à documenter, pas à sur-vendre)

- `Services/UserRoleService.cs`, `GetRoleUsersAsync` (lignes ~54-75) :
  ```csharp
  var roleUsers = await _context.UserRoles
      .Include(ur => ur.User)
      .Include(ur => ur.Role)
      .Include(ur => ur.AssignedByUser)
      .Where(ur => ur.RoleId == roleId &&
                   ur.IsActive &&
                   ur.User.IsActive &&
                   (ur.ExpiresAt == null || ur.ExpiresAt > DateTime.UtcNow))
      // ... pas de filtre OrganizationId
  ```
  Même défaut structurel que Bug 1 : aucun filtre d'organisation sur les `UserRole` retournés.
- **Mais l'exploration montre que ce chemin n'est aujourd'hui atteignable par aucun `Organization.Admin`, pour deux raisons indépendantes, chacune suffisante à elle seule** :
  1. **Autorisation** : `Controllers/UserRolesController.cs`, `GetRoleUsers` (lignes ~36-42) est protégé par `[Authorize(Policy = "RequirePlatformRole")]`. Cette policy (`Extensions/SecurityExtensions.cs:475-476`) exige `Roles.PlatformRoles` (`PlatformAdmin`/`PlatformSuperAdmin` uniquement, cf. `Utils/Roles.cs:65-69`). Un `Organization.Admin` appelant cet endpoint reçoit un **403 Forbidden**, avant même d'atteindre la logique métier concernée par ce correctif.
  2. **Câblage frontend absent** : dans `recruiter-app/src/pages/admin/RolesPage.vue`, le dialog `showUserRolesDialog` (template lignes 174-225, alimenté par `userRoleStore.roleUsers`) existe bien dans le template, mais **rien ne l'ouvre** : le menu d'actions de chaque ligne de rôle (lignes 67-108) ne propose que "Modifier", "Activer/Désactiver" et "Supprimer" — aucune entrée "Voir les utilisateurs" n'appelle `userRoleStore.fetchRoleUsers(...)` ni ne positionne `selectedRole`/`showUserRolesDialog` à `true`. Ce dialog est aujourd'hui du code mort côté template.
- **Conséquence** : contrairement à Bug 1, ce second défaut ne produit aucun symptôme observable par un `Organization.Admin` en usage normal actuel (ni par appel direct de l'endpoint, bloqué en amont par l'autorisation). Il est corrigé ici **par cohérence structurelle avec Bug 1** (même cause racine, même correctif mécanique), comme filet de sécurité pour le jour où l'un de ces deux blocages serait levé (élargissement de policy et/ou câblage du bouton), pas parce qu'il cause un bug visible aujourd'hui — même logique que le traitement réservé à `RefreshTokenAsync` dans `login-response-missing-experiences-trainings.md` (défaut "présent mais inerte", corrigé pour cohérence de contrat).

### Pattern de filtrage par organisation déjà établi ailleurs — à répliquer, pas à réinventer

`ICurrentUserService` (`Interfaces/ICurrentUserService.cs:8`, implémentation `CurrentUserService.cs:19-26`) expose déjà `OrganizationId` (claim JWT `"OrganizationId"`). `RoleService` l'injecte déjà (`_currentUserService`, champ ligne ~23) et l'utilise déjà pour la visibilité des rôles plateforme (`GetAllRolesAsync` lignes ~53-63, et dans `BuildRoleQuery` lignes ~367-377), mais pas pour le comptage.

Le pattern exact à reproduire existe dans `Services/UserService.cs`, `BuildUserQuery` (lignes ~721-743) :
```csharp
var isOrgAdmin = _currentUserService.User.IsInRole(Roles.OrganizationAdmin.Name);
var isManager = _currentUserService.User.IsInRole(Roles.Manager.Name);
var isPlatformUser = _currentUserService.User.IsInRole(Roles.PlatformSuperAdmin.Name) ||
                     _currentUserService.User.IsInRole(Roles.PlatformAdmin.Name);

if ((isOrgAdmin || isManager) && !isPlatformUser && _currentUserService.OrganizationId.HasValue)
{
    query = query.Where(u => u.OrganizationId == _currentUserService.OrganizationId.Value);
}
```
**Comportement à reproduire à l'identique** : un appelant **plateforme** (`PlatformAdmin`/`PlatformSuperAdmin`) continue de voir le total/la liste **toutes organisations confondues** (cohérent avec son rôle de gestion transverse). Seul un appelant **non-plateforme, avec le rôle `Organization.Admin` ou `Organization.Manager`**, et disposant d'un `OrganizationId` dans son claim JWT, voit un nombre/une liste **scopée à sa seule organisation**. Un appelant non-plateforme sans ces deux rôles précis (ex. `Organization.Recruiter`, `Organization.TechnicalEvaluator`) n'est **pas** scopé par ce correctif — c'est exactement le comportement (potentiellement discutable, mais déjà existant et non modifié ici) de `BuildUserQuery`, répliqué à l'identique pour rester cohérent. Ne pas corriger cette nuance dans le cadre de ce ticket (elle préexiste dans `UserService` et n'est pas signalée ici).

## Root cause

`Role` est une entité globale (pas de scoping par organisation par design). Les requêtes qui construisent `RoleDto.UsersCount` (`RoleService.BuildRoleQuery`) et la liste `UserRoleDto` d'un rôle (`UserRoleService.GetRoleUsersAsync`) chargent/filtrent les affectations `UserRole` sans jamais tenir compte de l'organisation de l'appelant, alors que le pattern de scoping conditionnel (`ICurrentUserService.OrganizationId`, déjà utilisé ailleurs dans le même service pour la visibilité des rôles plateforme, et dans `UserService.BuildUserQuery` pour les utilisateurs) existe déjà et est directement réutilisable.

## Objectif et périmètre

- Faire en sorte que `RoleDto.UsersCount`, retourné par `GET /api/Roles/paginated`, ne compte que les utilisateurs de l'organisation de l'appelant lorsque celui-ci est un `Organization.Admin`/`Organization.Manager` non-plateforme ; un appelant plateforme (`PlatformAdmin`/`PlatformSuperAdmin`) continue de voir le total toutes organisations confondues.
- Appliquer le même principe de scoping à `UserRoleService.GetRoleUsersAsync` (utilisée par `GET /api/UserRoles/role/{roleId}`), par cohérence structurelle avec le point précédent, même si ce chemin est aujourd'hui non atteignable par un `Organization.Admin` (cf. constat Bug 2 ci-dessus) — voir section "Points à confirmer" pour la question de fond que cela soulève.
- **Hors périmètre** :
  - Ajouter une colonne `OrganizationId` sur `Role` : l'entité reste globale à la plateforme par design ; seul le comptage/la liste des utilisateurs affectés doit être scopé, pas le rôle lui-même.
  - Revoir la visibilité des rôles eux-mêmes (quels rôles apparaissent dans la liste) : déjà correcte, logique existante inchangée (`GetAllRolesAsync` lignes ~53-63, `BuildRoleQuery` lignes ~367-377).
  - `GetAllRolesAsync` (`GET /api/Roles`, non paginé), `GetRoleByIdAsync`, `GetRoleByNameAsync` : ces trois méthodes calculent aussi `UsersCount` via le même profil AutoMapper et présentent donc le même défaut structurel, mais **leur champ `UsersCount` n'est affiché nulle part côté `recruiter-app`** (vérifié : `fetchAllRoles`/`fetchRoleById`/`fetchRoleByName` alimentent des sélecteurs de rôle et `currentRole`, jamais rendus avec leur `usersCount`). Ne pas les modifier dans ce correctif ; même défaut latent documenté ici pour un futur correctif de cohérence si un usage affichant ce champ apparaît.
  - Élargir la policy d'autorisation `[Authorize(Policy = "RequirePlatformRole")]` de `UserRolesController.GetRoleUsers`, ou câbler un déclencheur "Voir les utilisateurs" dans `RolesPage.vue` : ce sont des changements de périmètre fonctionnel (qui peut appeler l'endpoint, quelle action UI existe), distincts du bug de scoping traité ici. Voir "Points à confirmer".
  - Aucun changement de contrat JSON (`RoleDto`, `UserRoleDto` gardent les mêmes champs/types) : seul le contenu de `usersCount`/de la liste change selon l'appelant, pas la structure. Aucun changement frontend nécessaire au-delà de la vérification que l'affichage réagit correctement à un compteur/une liste maintenant scopés.

## Acteurs et permissions

- **`PlatformAdmin`/`PlatformSuperAdmin`** : comportement inchangé, voient le total/la liste toutes organisations confondues (rôle de gestion transverse de la plateforme).
- **`Organization.Admin`** : voit désormais un `usersCount` scopé à sa seule organisation sur `GET /api/Roles/paginated` (bug corrigé, effet visible immédiatement). Reste bloqué en 403 sur `GET /api/UserRoles/role/{roleId}` (comportement d'autorisation inchangé par ce ticket, cf. "Points à confirmer").
- **`Organization.Manager`** : même règle de scoping que `Organization.Admin` dans le code (réplique du pattern `BuildUserQuery`), mais n'accède pas à la page `admin/roles` aujourd'hui (`adminSectionGuard` ne l'autorise pas) — effet du correctif non observable pour ce rôle en usage normal actuel, uniquement au niveau service/API.
- **`Organization.Recruiter`/`Organization.TechnicalEvaluator`** : non concernés par le scoping ajouté (même limite préexistante que `BuildUserQuery`, non corrigée ici) ; de toute façon non autorisés à accéder à la page `admin/roles`.

## Règles métier et cas limites

- Le scoping s'applique uniquement à la sous-collection `UserRoles`/à la liste de `UserRoleDto` retournée pour un rôle donné ; il ne change jamais la liste des **rôles** eux-mêmes (un `Organization.Admin` voit toujours toutes les lignes de rôle non-plateforme, y compris celles où son organisation n'a aucun utilisateur assigné — dans ce cas `usersCount` doit valoir `0`, pas faire disparaître la ligne).
- Un rôle sans aucun utilisateur assigné dans l'organisation de l'appelant (mais avec des utilisateurs actifs dans d'autres organisations) doit renvoyer `UsersCount = 0` pour cet appelant — pas une erreur, pas le total global.
- `IsActive` sur `UserRole` (et les conditions déjà existantes `User.IsActive`, `ExpiresAt` pour `GetRoleUsersAsync`) restent appliquées **en plus** du filtre d'organisation, pas à sa place : un utilisateur inactif ou avec un rôle expiré de la même organisation reste exclu du compte, exactement comme avant ce correctif.
- Utilisateur authentifié mais sans claim `OrganizationId` exploitable (`_currentUserService.OrganizationId` est `null`) alors qu'il a le rôle `Organization.Admin`/`Organization.Manager` : cas anormal (un compte organisationnel doit toujours porter ce claim) — ne pas planter ; par cohérence stricte avec `BuildUserQuery` (qui exige `.HasValue` dans sa condition), ne pas appliquer de filtre dans ce cas et laisser le comportement non scopé (identique à la situation actuelle), sans lever d'exception.
- Appel non authentifié : théorique uniquement (les deux endpoints sont derrière `[Authorize]`), pas de changement de comportement à spécifier ici — garder la même garde `_currentUserService.User?.Identity?.IsAuthenticated == true` déjà en place avant toute autre vérification de rôle.

## Contrat d'interface — Backend (`XpertSphere.MonolithApi`)

### `Services/RoleService.cs`, `BuildRoleQuery` (Bug 1)

Restructurer pour filtrer la sous-collection `UserRoles` **incluse** selon le contexte de l'appelant, avant toute projection AutoMapper — aucune modification de `Mappings/RoleMappingProfile.cs` n'est nécessaire, EF Core "filtered Include" (supporté nativement depuis EF Core 5, disponible en EF Core 9) permet de restreindre le contenu de la collection de navigation matérialisée sans toucher au mapping :

```csharp
private IQueryable<Role> BuildRoleQuery(RoleFilterDto filter)
{
    var isAuthenticated = _currentUserService.User?.Identity?.IsAuthenticated == true;
    var isPlatformUser = isAuthenticated &&
        (_currentUserService.User!.IsInRole(Roles.PlatformSuperAdmin.Name) ||
         _currentUserService.User.IsInRole(Roles.PlatformAdmin.Name));
    var isOrgAdmin = isAuthenticated && _currentUserService.User!.IsInRole(Roles.OrganizationAdmin.Name);
    var isManager = isAuthenticated && _currentUserService.User!.IsInRole(Roles.Manager.Name);

    var shouldScopeUserRolesToOrganization =
        (isOrgAdmin || isManager) && !isPlatformUser && _currentUserService.OrganizationId.HasValue;

    var query = shouldScopeUserRolesToOrganization
        ? _context.Roles
            .AsNoTracking()
            .Include(r => r.UserRoles.Where(ur => ur.User.OrganizationId == _currentUserService.OrganizationId!.Value))
            .ThenInclude(ur => ur.User)
            .AsQueryable()
        : _context.Roles
            .AsNoTracking()
            .Include(r => r.UserRoles)
            .ThenInclude(ur => ur.User)
            .AsQueryable();

    // Bloc existant inchangé : visibilité des rôles plateforme (pas des UserRoles)
    if (isAuthenticated && !isPlatformUser)
    {
        query = query.Where(r => !Roles.PlatformRoles.Contains(r.Name));
    }

    // ... reste de la méthode (filtres IsActive/UserId/SearchTerms/tri) inchangé
}
```

Effet mécanique : `RoleMappingProfile.ForMember(dest => dest.UsersCount, ...Count(ur => ur.IsActive))` opère déjà en mémoire sur `Role.UserRoles` — en restreignant en amont le contenu de cette collection de navigation via l'`Include` filtré, le compte reflète automatiquement le scope voulu, sans toucher au profil AutoMapper.

Point d'attention pour le developer : le filtre `filter.UserId.HasValue` (ligne ~385-388 actuelle, `query.Where(r => r.UserRoles.Any(ur => ur.User.Id == filter.UserId))`) est traduit par EF Core en sous-requête SQL indépendante de l'`Include` filtré — il continue de fonctionner sans changement et n'est pas affecté par ce correctif.

**Piège à éviter (important, à ne pas découvrir en test) : `AsNoTracking()` est obligatoire ici, pas optionnel.** Sur une requête *avec tracking* (comportement par défaut d'EF Core), le mécanisme de "relationship fixup" du `ChangeTracker` réattache aux collections de navigation matérialisées **toutes** les entités déjà suivies par le même `DbContext`, y compris celles qui ne correspondent pas au filtre du `Include` — l'`Include` filtré serait alors silencieusement pollué par des `UserRole` d'autres organisations dès lors qu'ils ont été chargés/insérés plus tôt sur la même instance de contexte (exactement le scénario d'un test unitaire qui fait `_context.UserRoles.Add(...)` pour deux organisations puis interroge le même `_context`). `BuildRoleQuery` ne fait que projeter vers un DTO en lecture seule : `AsNoTracking()` est donc correct en soi (pas seulement un contournement de test) et rend le comportement de l'`Include` filtré déterministe indépendamment de l'état du tracker. Sans ce `AsNoTracking()`, un test unitaire construit sur le pattern habituel du repo (seed multi-organisations sur le même `_context`, cf. tests existants de `RoleServiceTests`/`UserRoleServiceTests`) ferait apparaître un `UsersCount` non scopé alors même que la logique métier serait correcte en production (un `DbContext` par requête HTTP n'a jamais de `UserRole` d'une autre organisation déjà tracké) — ne pas se laisser dérouter par ce faux négatif, et ne pas chercher une solution plus alambiquée que ce simple ajout.

Note : `UserRoleService.GetRoleUsersAsync` (Bug 2 ci-dessous) n'est **pas** concerné par ce piège : son filtre d'organisation est un prédicat de premier niveau sur le `DbSet` (`query.Where(ur => ur.User.OrganizationId == ...)`), pas un `Include` filtré peuplant une collection de navigation — aucune pollution par fixup possible, `AsNoTracking()` n'y est pas requis pour la correction du scoping (il reste une bonne pratique générale sur une lecture, mais n'est pas indispensable pour éviter un faux négatif de test ici).

### `Services/UserRoleService.cs`, `GetRoleUsersAsync` (Bug 2, cohérence)

Injecter `ICurrentUserService` dans le constructeur de `UserRoleService` (nouveau paramètre, comme dans `RoleService`) et ajouter le même filtre conditionnel directement dans la clause `.Where(...)` :

```csharp
public async Task<ServiceResult<IEnumerable<UserRoleDto>>> GetRoleUsersAsync(Guid roleId)
{
    try
    {
        var isAuthenticated = _currentUserService.User?.Identity?.IsAuthenticated == true;
        var isPlatformUser = isAuthenticated &&
            (_currentUserService.User!.IsInRole(Roles.PlatformSuperAdmin.Name) ||
             _currentUserService.User.IsInRole(Roles.PlatformAdmin.Name));
        var isOrgAdmin = isAuthenticated && _currentUserService.User!.IsInRole(Roles.OrganizationAdmin.Name);
        var isManager = isAuthenticated && _currentUserService.User!.IsInRole(Roles.Manager.Name);

        var query = _context.UserRoles
            .Include(ur => ur.User)
            .Include(ur => ur.Role)
            .Include(ur => ur.AssignedByUser)
            .Where(ur => ur.RoleId == roleId &&
                         ur.IsActive &&
                         ur.User.IsActive &&
                         (ur.ExpiresAt == null || ur.ExpiresAt > DateTime.UtcNow));

        if ((isOrgAdmin || isManager) && !isPlatformUser && _currentUserService.OrganizationId.HasValue)
        {
            query = query.Where(ur => ur.User.OrganizationId == _currentUserService.OrganizationId.Value);
        }

        var roleUsers = await query
            .OrderBy(ur => ur.User.FirstName)
            .ToListAsync();

        var roleUserDtos = _mapper.Map<IEnumerable<UserRoleDto>>(roleUsers);
        return ServiceResult<IEnumerable<UserRoleDto>>.Success(roleUserDtos);
    }
    catch (Exception ex) { /* inchangé */ }
}
```

Migration mécanique requise par ce changement de constructeur :
- `Extensions/DependencyInjections/ApplicationServicesExtensions.cs` : vérifié, `services.AddScoped<IUserRoleService, UserRoleService>()` (ligne 24) et `services.AddScoped<ICurrentUserService, CurrentUserService>()` (ligne 29) sont déjà enregistrés sans paramètres explicites — le conteneur DI résout automatiquement le nouveau paramètre de constructeur, **aucune modification de câblage DI nécessaire**.
- `XpertSphere.MonolithApi.Tests/Services/UserRoleServiceTests.cs` : le helper privé `CreateUserRoleService(AutoMapper.IMapper mapper)` (lignes ~284-292) doit être étendu avec un `Mock<ICurrentUserService>` (même pattern que `RoleServiceTests.CreateRoleService`, qui l'a déjà) ; le test existant `GetRoleUsersAsync_WithExistingRole_ShouldReturnSuccess` (lignes ~75-121) ne configure aujourd'hui aucune attente sur ce mock — un `Mock<ICurrentUserService>` non configuré (comportement par défaut : `User` renvoie `null`) doit continuer à satisfaire ce test sans le faire échouer (branche `isAuthenticated = false` → pas de filtre appliqué → comportement identique à avant, non-régression).

## Points à confirmer (regroupés, ne bloquent pas la rédaction de cette spec)

1. **Policy d'autorisation de `GET /api/UserRoles/role/{roleId}`** (`UserRolesController.GetRoleUsers`, `[Authorize(Policy = "RequirePlatformRole")]`) : cette policy exclut aujourd'hui tout `Organization.Admin`, qui reçoit un 403 avant même d'atteindre le scoping corrigé ici. Si l'intention produit est que le dialog "Utilisateurs avec le rôle" (actuellement non câblé côté UI, cf. constat Bug 2) devienne un jour utilisable par un `Organization.Admin`, cette policy devra être élargie (ex. `RequireInternalUser`, ou une policy dédiée alignée sur `adminSectionGuard`) — **hors périmètre de ce ticket**, à traiter dans une spec séparée si confirmé nécessaire.
2. **Câblage du dialog "Utilisateurs avec le rôle"** dans `RolesPage.vue` : aucune action du menu (`···`) n'ouvre aujourd'hui ce dialog ni n'appelle `userRoleStore.fetchRoleUsers`. Sans ce câblage (et sans le point 1 ci-dessus), le correctif de `GetRoleUsersAsync` reste un filet de sécurité non exercé par l'UI actuelle. À confirmer si ce câblage doit être ajouté dans une spec frontend séparée, ou reste volontairement différé.
3. Si les points 1 et 2 sont confirmés comme nécessaires, ils justifieraient une spec dédiée (périmètre fonctionnel : qui peut voir le détail d'un rôle, quelle action UI l'expose), distincte de ce correctif de scoping.

## Note pour les tests unitaires (mock `ICurrentUserService`)

`IsInRole(...)` (utilisé par le code du correctif via `_currentUserService.User!.IsInRole(...)`) est une méthode de `ClaimsPrincipal` (`_currentUserService.User`), pas un membre de `ICurrentUserService` lui-même : ne pas essayer de la mocker directement sur `Mock<ICurrentUserService>`. Pour simuler un `Organization.Admin` d'une organisation donnée dans un test, construire un vrai `ClaimsPrincipal` portant les claims de rôle attendus (`ClaimTypes.Role` = `Roles.OrganizationAdmin.Name`, cohérent avec la façon dont l'authentification pose ce claim en production) et le retourner via `_mockCurrentUserService.Setup(x => x.User).Returns(claimsPrincipal)`, en complément de `_mockCurrentUserService.Setup(x => x.OrganizationId).Returns(organizationId)`.

## Critères d'acceptation

1. `GET /api/Roles/paginated`, appelé par un `Organization.Admin` de l'organisation "Meilleurtaux", pour un rôle (ex. `Organization.Recruiter`) ayant des utilisateurs actifs à la fois chez "Meilleurtaux" et chez "Expertime" → `usersCount` ne compte que les utilisateurs actifs de "Meilleurtaux" pour ce rôle.
2. Même appel effectué par un `PlatformAdmin`/`PlatformSuperAdmin` → `usersCount` reste le total toutes organisations confondues (Meilleurtaux + Expertime + toute autre organisation) — non-régression, comportement inchangé pour ce profil.
3. `Organization.Admin` de "Meilleurtaux" consultant `pages/admin/RolesPage.vue` (`recruiter-app`) : la colonne "Utilisateurs" reflète désormais le nombre d'utilisateurs de sa seule organisation par rôle.
4. Un rôle sans aucun utilisateur chez "Meilleurtaux" (mais avec des utilisateurs chez "Expertime") consulté par un `Organization.Admin` de "Meilleurtaux" → `usersCount = 0` pour ce rôle (la ligne de rôle reste visible, ce n'est pas une erreur).
5. Test unitaire/service sur `RoleService.GetAllPaginatedRolesAsync` (ou `BuildRoleQuery` si testable directement) : avec un `Mock<ICurrentUserService>` configuré en `Organization.Admin` + `OrganizationId` donné, et des `UserRole` de deux organisations différentes en base de test, le `RoleDto.UsersCount` retourné ne compte que les `UserRole` de l'organisation du mock.
6. Même test avec un `Mock<ICurrentUserService>` configuré en `PlatformAdmin` (sans `OrganizationId` pertinent, ou peu importe sa valeur) : `UsersCount` compte tous les `UserRole` actifs, toutes organisations confondues.
7. Test unitaire/service sur `UserRoleService.GetRoleUsersAsync` : avec un `Mock<ICurrentUserService>` configuré en `Organization.Admin` + `OrganizationId` donné, et des `UserRole` actifs pour le même `roleId` répartis sur deux organisations, seuls les `UserRoleDto` de l'organisation du mock sont retournés.
8. Même test avec le mock configuré en `PlatformAdmin` (ou non configuré/non authentifié, reproduisant le test existant `GetRoleUsersAsync_WithExistingRole_ShouldReturnSuccess`) : tous les `UserRoleDto` actifs pour ce rôle sont retournés, toutes organisations confondues — non-régression du test existant.
9. Suite de tests existante (`dotnet test` depuis `XpertSphere.MonolithApi.Tests`) : aucun test préexistant sur `RoleService`/`UserRoleService` ne casse suite à ce correctif (en particulier après l'ajout du paramètre `ICurrentUserService` au constructeur de `UserRoleService` et la mise à jour du helper `CreateUserRoleService`).
10. Non-régression : le filtre `filter.UserId` de `RoleFilterDto` sur `GET /api/Roles/paginated` continue de fonctionner comme avant ce correctif (recherche des rôles possédés par un utilisateur donné, indépendamment du scoping de `UsersCount`).
11. Non-régression : la visibilité des **lignes de rôle** elles-mêmes (quels rôles apparaissent dans la liste, filtre "rôles plateforme masqués aux non-plateforme") reste strictement inchangée par ce correctif — seul le contenu de `usersCount`/de la liste d'utilisateurs change, pas la liste des rôles visibles.
