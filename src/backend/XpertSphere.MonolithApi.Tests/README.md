# XpertSphere.MonolithApi.Tests

Ce projet contient les tests unitaires pour l'API XpertSphere dans le cadre du PFE.

**📊 Total des Tests : 92 tests**

## Structure des Tests

### Services Tests (78 tests)

- **AuthenticationServiceTests** (9 tests) - Tests d'authentification (registration, login, confirmation email, reset
  password)
- **RoleServiceTests** (10 tests) - Tests CRUD des rôles (création, mise à jour, activation/désactivation)
- **UserRoleServiceTests** (10 tests) - Tests d'assignation des rôles aux utilisateurs
- **PermissionServiceTests** (10 tests) - Tests de gestion des permissions
- **RolePermissionServiceTests** (10 tests) - Tests d'assignation des permissions aux rôles
- **OrganizationServiceTests** (9 tests) - Tests CRUD des organisations
- **ApplicationStatusHistoryServiceTests** (10 tests) - Tests d'historique des statuts de candidatures
- **UserServiceTests** (10 tests) - Tests de gestion des utilisateurs

### Controllers Tests (14 tests)

- **OrganizationControllerTests** (9 tests) - Tests des endpoints d'organisations
- **RolesControllerTests** (14 tests) - Tests des endpoints de rôles

### Helpers

- **TestDbContextFactory** - Factory pour créer des contextes de base de données en mémoire
- **AutoMapperHelper** - Configuration AutoMapper pour les tests
- **MockHelper** - Helpers pour créer des mocks (UserManager, SignInManager, etc.)

## Technologies Utilisées

- **xUnit** - Framework de tests
- **FluentAssertions** - Assertions fluides pour des tests plus lisibles
- **Moq** - Framework de mocking
- **EntityFramework InMemory** - Base de données en mémoire pour les tests
- **AspNetCore.Mvc.Testing** - Tests d'intégration pour les controllers

## Exécution des Tests

```bash
# Exécuter tous les tests (92 tests)
dotnet test

# Exécuter avec couverture de code
dotnet test --collect:"XPlat Code Coverage"

# Exécuter des tests spécifiques par service
dotnet test --filter "AuthenticationServiceTests"
dotnet test --filter "RoleServiceTests"
dotnet test --filter "OrganizationServiceTests"

# Exécuter les tests de controllers
dotnet test --filter "ControllerTests"

# Exécuter avec sortie détaillée
dotnet test --logger "console;verbosity=detailed"
```

## Couverture des Tests

Les tests couvrent l'ensemble du système RBAC (Role-Based Access Control) de XpertSphere :

✅ **Authentification** (9 tests)

- Registration avec validation
- Login avec credentials valides/invalides
- Confirmation d'email
- Reset de mot de passe
- Gestion des erreurs d'authentification

✅ **Système de Rôles** (10 tests)

- CRUD complet des rôles
- Activation/Désactivation des rôles
- Validation de l'existence des rôles
- Vérification des contraintes de suppression

✅ **Gestion des Utilisateurs-Rôles** (10 tests)

- Assignation de rôles aux utilisateurs
- Suppression d'assignations
- Validation des utilisateurs et rôles existants
- Gestion des erreurs de duplication

✅ **Système de Permissions** (10 tests)

- CRUD complet des permissions
- Validation des noms de permissions
- Gestion des permissions système

✅ **Assignation Rôles-Permissions** (10 tests)

- Liaison permissions aux rôles
- Suppression d'assignations
- Validation des contraintes

✅ **Gestion des Organisations** (9 tests)

- CRUD des organisations
- Validation des règles métier (utilisateurs actifs)
- Gestion des conflits (noms dupliqués)

✅ **Historique des Statuts** (10 tests)

- Création d'historiques de candidatures jouant le role ici du flux d'infos de la candidature
- Mise à jour des statuts
- Validation des données

✅ **Tests Controllers** (14 tests)

- Endpoints API des organisations
- Endpoints API des rôles
- Validation des réponses HTTP
- Gestion des erreurs HTTP

## Configuration des Tests

Les tests utilisent :

- Base de données en mémoire (EntityFramework InMemory)
- Configuration de test (TestConfiguration.cs)
- Données de test seeded automatiquement
- Mocks pour les services externes

## Notes Importantes

1. **Isolation des Tests** - Chaque test utilise sa propre instance de base de données
2. **Cleanup** - Les ressources sont nettoyées automatiquement (IDisposable)
3. **Authentification** - Les tests d'intégration peuvent nécessiter une configuration auth spécifique
4. **Environment** - Les tests s'exécutent en mode "Testing" pour éviter les services externes

## Statistiques des Tests

| Catégorie       | Nombre de Tests | Description                         |
|-----------------|-----------------|-------------------------------------|
| **Services**    | 78              | Tests unitaires des services métier |
| **Controllers** | 14              | Tests des endpoints API             |
| **Total**       | **92**          | **Couverture complète RBAC**        |

## Exemple d'Exécution

```bash
cd src/backend/XpertSphere.MonolithApi.Tests
dotnet test --verbosity normal
```

**Résultat attendu : 92 tests réussis ✅**

## Objectif PFE

Ce projet respecte l'exigence d'un jeu de tests unitaires couvrant une fonctionnalité demandée

✅ **Fonctionnalité couverte** : Système RBAC complet (Role-Based Access Control)  
✅ **92 tests unitaires** couvrant toutes les couches (Services + Controllers)  
✅ **Patterns de test** : AAA (Arrange-Act-Assert), Mocking, InMemory DB  
✅ **Technologies** : xUnit, FluentAssertions, Moq, EF InMemory