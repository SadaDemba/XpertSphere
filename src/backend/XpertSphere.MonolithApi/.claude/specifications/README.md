# Spécifications — XpertSphere.MonolithApi

Un fichier Markdown par fonctionnalité spécifiée (nommage `<slug-fonctionnalite>.md`), rédigé par l'agent spec-writer avant tout développement.

Spécifications existantes :
- `azurite-blob-storage-local.md` — stockage Blob (CV/documents) en local via Azurite, coexistant avec la configuration Azure Storage réelle de Staging/Production.
- `secure-cv-download.md` — endpoint proxy authentifié pour consulter/télécharger un CV déjà uploadé (candidat propriétaire + rôles d'organisation), avec section de coordination frontend (`candidate-app`, `recruiter-app`).
- `candidate-registration-experience-description-error.md` — rend obligatoire la description d'une expérience à l'inscription candidat (`POST /api/auth/register/candidate`) avec message d'erreur explicite (numéro/titre de l'expérience), et corrige l'affichage du message générique côté frontend ; coordination avec `candidate-app`.
- `login-response-missing-experiences-trainings.md` — `LoginAsync`/`RefreshTokenAsync` ne chargent pas `Experiences`/`Trainings`/`Address` contrairement à `GetCurrentUserAsync`, profil candidat visuellement incomplet jusqu'à un reload ; coordination avec `candidate-app`.
- `seed-demo-organizations-users-joboffers.md` — extension du seeder existant (`DatabaseExtensions.cs`), borné à l'environnement Development, pour pousser 3 organisations clientes de démonstration avec leur roster d'utilisateurs, 30 offres d'emploi, 4 candidats et leurs candidatures.
- `role-user-count-organization-scope-fix.md` — corrige `RoleDto.UsersCount` (`GET /api/Roles/paginated`, comptage non scopé par organisation, bug visible pour `Organization.Admin`) et applique le même correctif de cohérence à `UserRoleService.GetRoleUsersAsync` (bug latent, endpoint aujourd'hui restreint aux profils plateforme).
- `configurable-salary-currency.md` — nouvel enum `Currency` (EUR/XOF) contrôlé, devise configurable par organisation (`Organization.Currency`, snapshot figé sur `JobOffer.SalaryCurrency` à la création, non rétroactif) et devise auto-déclarée par le candidat (`User.DesiredSalaryCurrency`), avec nouvel endpoint self-service `GET/PUT /api/organizations/me/currency` réservé à `Organization.Admin` ; coordination avec `seed-demo-organizations-users-joboffers.md`, `candidate-app` et `recruiter-app`.
