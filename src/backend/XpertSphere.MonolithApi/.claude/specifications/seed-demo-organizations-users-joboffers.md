# Seed de données de démonstration : 3 organisations clientes, leurs profils utilisateurs, leurs offres d'emploi, 4 candidats et leurs candidatures

## Contexte et périmètre

Le seed actuel (`Extensions/DatabaseExtensions.cs`, méthode privée `SeedDatabaseAsync`, appelée depuis `UseDatabaseAsync()` à **chaque démarrage** de l'API, après `context.Database.MigrateAsync()`) ne pousse que des données structurelles minimales : l'organisation `XPERTSPHERE` elle-même (`SeedXpertSphereOrganizationAsync`), les 7 rôles RBAC (`SeedDefaultRolesAsync`), et un unique compte `PlatformSuperAdmin` (`SeedPlatformSuperAdminAsync`). Chaque étape vérifie l'existence avant insertion (par `Organization.Name`, `Role.Name`, `User.Email` respectivement), et une seule `SaveChangesAsync()` clôt l'ensemble.

Cette spécification étend ce même mécanisme pour pousser un jeu de données de démonstration réaliste, borné à l'environnement Development (voir §Garde-fou d'environnement) :
- 3 organisations clientes fictives, inspirées d'entreprises réelles (voir §Descriptions d'entreprise et avertissement).
- Un roster de profils utilisateurs par organisation (admin, managers, recruteurs, évaluateur technique).
- 10 offres d'emploi publiées par organisation (30 au total), cohérentes avec le secteur de chacune.
- 4 candidats fictifs (comptes `User` sans organisation), avec des candidatures réparties sur plusieurs des 30 offres, à travers plusieurs entreprises.

Ce n'est **pas** un script one-off séparé : c'est une extension du seeder existant, avec le même niveau d'idempotence que l'existant (voir §Idempotence), et un garde-fou explicite pour ne jamais s'exécuter hors Development.

## Garde-fou d'environnement (décision validée)

Le seed de démonstration ne doit **jamais** s'exécuter en Staging ni en Production : les comptes créés partagent tous le mot de passe `Azerty123*`, ce qui est acceptable pour un environnement de développement local mais constituerait une faille de sécurité réelle ailleurs.

Mécanisme retenu : un seul gate positif sur `IWebHostEnvironment.IsDevelopment()`, qui exclut à la fois Staging et Production sans avoir à énumérer les environnements à exclure (plus robuste qu'un futur environnement nommé différemment qui échapperait à une liste d'exclusion explicite).

`UseDatabaseAsync(this WebApplication app)` dispose déjà de `app.Environment` (le même objet que `builder.Environment` utilisé ailleurs dans `Program.cs` et dans `DatabaseExtensions.GetConnectionString`) — aucune nouvelle dépendance à injecter, seulement à le transmettre à travers la chaîne d'appel :

```csharp
// UseDatabaseAsync(...)
await SeedDatabaseAsync(context, userManager, configuration, app.Environment);

// SeedDatabaseAsync(...) — nouvelle signature
private static async Task SeedDatabaseAsync(XpertSphereDbContext context, UserManager<User> userManager,
    IConfiguration configuration, IWebHostEnvironment environment)
{
    await SeedXpertSphereOrganizationAsync(context, configuration);
    await SeedDefaultRolesAsync(context);
    await SeedPlatformSuperAdminAsync(context, userManager, configuration);

    if (environment.IsDevelopment())
    {
        await SeedDemoDataAsync(context, userManager);
    }

    await context.SaveChangesAsync();
}
```

`SeedDemoDataAsync` orchestre, dans cet ordre (voir §Ordre de dépendance), les nouvelles méthodes privées de ce correctif : `SeedDemoOrganizationsAsync`, `SeedDemoOrganizationUsersAsync`, `SeedDemoJobOffersAsync`, `SeedDemoCandidatesAsync`, `SeedDemoApplicationsAsync`.

Staging est donc exclu par défaut par construction (`IsDevelopment()` ne vaut vrai qu'en Development). Si l'utilisateur souhaite en relecture un comportement différent pour Staging (ex. un jeu de démo également désiré en Staging pour des démonstrations client), cela nécessiterait un gate distinct — non retenu par défaut ici.

## Où vivent les données (décision : code, pas configuration)

Le seed existant pour l'organisation `XPERTSPHERE` lit ses valeurs depuis `appsettings.json` (`Seeding:Organization`), adapté à une seule organisation. Ce pattern ne passe pas à l'échelle pour 3 organisations × leurs profils × 30 offres d'emploi (bien plus de texte libre — `Description`/`Requirements`/`Benefits` de chaque `JobOffer` sont `[Required]`, donc non triviaux). Décision : les données de démonstration sont définies **directement en C#**, sous forme de collections/records internes à `DatabaseExtensions.cs` (ou un fichier partiel/annexe du même dossier `Extensions/` si la taille du fichier le justifie, au choix du développeur), pas dans `appsettings.json`. Aucune nouvelle section `Seeding:*` n'est requise pour ce correctif.

## Ordre de dépendance (a respecter strictement)

1. **Organisations** — 3 `Organization` créées en premier (`SeedDemoOrganizationsAsync`), **suivi d'un `await context.SaveChangesAsync()` explicite avant de retourner de cette méthode** — voir §Point d'attention : flush explicite requis ci-dessous pour la justification précise.
2. **Utilisateurs d'organisation** — pour chaque organisation, les profils du roster (`SeedDemoOrganizationUsersAsync`), avec assignation de rôle (`UserRole`), en interrogeant `context.Roles` par `Name` pour résoudre le `RoleId` de chaque `UserRole`. Grâce au flush de l'étape 1, ces requêtes trouvent bien les 7 rôles (déjà persistés) dès le premier démarrage.
3. **Offres d'emploi** — pour chaque organisation, ses 10 offres (`SeedDemoJobOffersAsync`), chaque offre référençant `OrganizationId` (organisation déjà persistée à l'étape 1) et `CreatedByUserId` (un utilisateur du roster de la **même** organisation, déjà persisté à l'étape 2 par `UserManager.CreateAsync` — jamais un utilisateur d'une autre organisation).
4. **Candidats** — 4 `User` sans `OrganizationId` (`SeedDemoCandidatesAsync`), rôle `Candidate` assigné. Ne dépend d'aucune des étapes précédentes autre que l'existence du rôle `Candidate` (déjà persisté, voir point d'attention ci-dessous).
5. **Candidatures** — pour chaque candidat, 2 à 3 `Application` référençant une offre déjà seedée à l'étape 3 (`JobOfferId`) et le candidat lui-même (`CandidateId`) (`SeedDemoApplicationsAsync`). Dépend strictement des étapes 3 et 4.

Le `SaveChangesAsync()` global en fin de `SeedDatabaseAsync` couvre les entités ajoutées directement au contexte (offres, candidatures, rôles utilisateurs) ; les `User` eux-mêmes sont déjà persistés au moment de leur `CreateAsync` respectif, comme c'est déjà le cas pour `PlatformSuperAdmin`.

### Point d'attention : flush explicite requis (premier démarrage sur base vide)

`SeedDefaultRolesAsync` (existant, inchangé) ajoute les 7 `Role` au contexte mais ne les sauvegarde pas lui-même (le seul `SaveChangesAsync()` de `SeedDatabaseAsync` intervient tout à la fin). Sur une base fraîchement migrée (premier démarrage), au moment où `SeedDemoDataAsync` s'exécute, ces 7 rôles ainsi que l'organisation `XPERTSPHERE` sont donc encore uniquement **trackés en mémoire**, pas en base. Une requête LINQ (`context.Roles.FirstOrDefaultAsync(r => r.Name == ...)`) interroge la base sous-jacente et ne voit **pas** les entités ajoutées mais non flushées : elle renverrait `null` pour un rôle pourtant "ajouté" quelques lignes plus haut dans le même passage de code.

C'est précisément ce qui affecte déjà silencieusement `SeedPlatformSuperAdminAsync` aujourd'hui : sur un tout premier démarrage sur base vide, son `context.Organizations.FirstOrDefaultAsync(o => o.Name == Constants.XPERTSPHERE)` peut renvoyer `null` (l'organisation XpertSphere venant d'être ajoutée mais pas encore flushée), et la méthode retourne alors prématurément sans créer le `PlatformSuperAdmin` — ce n'est un comportement correct qu'à partir du **second** démarrage. Ce correctif ne doit **pas** reproduire cette même faille latente pour les données de démonstration : le critère d'acceptation §1 exige que les 6 compteurs soient corrects dès le **premier** démarrage.

D'où l'exigence : `SeedDemoOrganizationsAsync` doit appeler `await context.SaveChangesAsync()` juste après avoir ajouté les 3 organisations de démonstration (ce qui flushe par la même occasion les 7 rôles et l'organisation `XPERTSPHERE`, ajoutés juste avant dans `SeedDatabaseAsync`). Toutes les requêtes de résolution de rôle (`SeedDemoOrganizationUsersAsync`, `SeedDemoCandidatesAsync`) qui suivent dans l'orchestration trouvent alors des données réellement persistées, sur le premier démarrage comme sur les suivants.

## Idempotence (a respecter strictement, un critère par type d'entité)

Même philosophie que l'existant (vérifier avant d'insérer), avec un critère stable **explicite** par entité — nécessaire notamment pour `JobOffer`, qui n'a aujourd'hui aucune contrainte d'unicité naturelle en base :

| Entité | Critère de vérification avant insertion |
|---|---|
| `Organization` (démo) | `Code` (ex. `MEILLEURTAUX`) — même logique que l'organisation `XPERTSPHERE` existante, qui vérifie par `Name` ; ici `Code` est plus stable car c'est un identifiant métier court dédié à cet usage. |
| `User` (roster d'organisation, candidats) | `Email`, via `userManager.FindByEmailAsync(email)` — identique au pattern `SeedPlatformSuperAdminAsync`. |
| `UserRole` | Couple `(UserId, RoleId)` — ne (re)créer l'association que si elle n'existe pas déjà pour cet utilisateur précis. |
| `JobOffer` | Couple `(Title, OrganizationId)` — une offre est considérée comme déjà seedée si une offre de même titre existe déjà pour la même organisation. Ce couple est unique par construction dans le jeu de données proposé ci-dessous (aucun doublon de titre au sein d'une même organisation). |
| `Application` | Couple `(JobOfferId, CandidateId)` — s'appuie sur la contrainte d'unicité déjà existante en base (`IX_Applications_JobOfferId_CandidateId`, `Data/Configurations/ApplicationConfiguration.cs`) : un candidat ne peut de toute façon postuler qu'une fois à une offre donnée. Vérifier l'existence par ce couple avant insertion évite une exception de contrainte au deuxième démarrage. |

A chaque démarrage suivant le premier, l'intégralité du jeu de données de démonstration doit donc être détectée comme déjà présente et ne rien insérer de plus (compteurs stables — voir critères d'acceptation).

## Comptes créés directement sur le contexte, pas via la couche service

Comme pour `SeedXpertSphereOrganizationAsync`/`SeedPlatformSuperAdminAsync` existants, ce correctif construit les entités (`Organization`, `JobOffer`, `Application`) directement via `context.<DbSet>.Add(...)`, sans passer par `IJobOfferService`/`IUserService`/FluentValidation/AutoMapper. Ce n'est pas un oubli : au démarrage de l'application, il n'existe ni contexte HTTP, ni utilisateur authentifié, ni `IValidator<TDto>` pertinent pour une opération de seed — le développeur ne doit pas chercher à faire transiter ces créations par les services métier existants. Les données proposées ci-dessous respectent néanmoins toutes les règles de validation que `JobOffer.Validate()` aurait vérifiées (`Location` renseigné sauf `FullRemote`, `SalaryMin <= SalaryMax`, pas de `ExpiresAt` dans le passé), par cohérence, même si `Validate()` n'est pas explicitement invoquée par le seed.

Seuls les `User` continuent de passer par `UserManager<User>.CreateAsync(user, password)` (gestion du hash de mot de passe par Identity, comme l'existant) — jamais d'affectation directe d'un champ mot de passe.

Par cohérence avec ce même choix, `EmployeeId` (`[MaxLength(50)] public string? EmployeeId`) n'est **volontairement pas renseigné** pour les 15 utilisateurs d'organisation de démonstration, alors que `Constants.EMPLOYEE_ID_REQUIRED_FOR_ORGANIZATIONAL_USERS` suggère cette exigence côté validation applicative (`CreateUserDtoValidator`) : cette validation n'est jamais invoquée par le seed (aucun `IValidator<TDto>` n'est utilisé, voir plus haut), exactement comme le compte `PlatformSuperAdmin` existant, qui n'a lui non plus pas d'`EmployeeId`. Ce n'est pas un oubli à corriger par l'agent `validator`.

## Mot de passe partagé

Tous les comptes créés par ce correctif (utilisateurs d'organisation et candidats) utilisent le mot de passe `Azerty123*`. Vérification déjà faite : ce mot de passe (10 caractères, chiffre + minuscule + majuscule + caractère non-alphanumérique) satisfait la politique Identity (`Extensions/SecurityExtensions.cs`) aussi bien en Development (longueur min 6) qu'en Production (longueur min 8) — mentionné ici pour mémoire, sans risque puisque ce seed ne s'exécute jamais hors Development (voir §Garde-fou).

Tous les comptes créés doivent être immédiatement exploitables pour une connexion (mêmes propriétés que le seed `PlatformSuperAdmin` existant) : `EmailConfirmed = true`, `IsActive = true`, `UserName = Email`, `ConsentGivenAt = DateTime.UtcNow`, `CreatedAt = DateTime.UtcNow`.

## Descriptions d'entreprise et avertissement

### Meilleurtaux (Code `MEILLEURTAUX`)

Courtier français en services financiers, plateforme de comparaison de crédits (prêts immobiliers, crédits à la consommation) et d'assurances (habitation, auto, santé, emprunteur). Plus de 350 agences en France, plus de 3 millions de clients par an, partenariats avec plus de 125 banques pour négocier des taux compétitifs. Conseil personnalisé via digital, téléphone et agences physiques.

- `Industry` : "Courtage en crédits et assurances"
- `Size` : `Large`
- `Website` : `https://www.meilleurtaux.com`
- `ContactEmail` : `contact@meilleurtaux-demo.fr` (fictif, ne pas confondre avec une adresse réelle de l'entreprise)
- `ContactPhone` : `+33140506070` (fictif)
- `Address` : `City` = "Paris", `Country` = "France" (adresse illustrative, pas le siège social réel vérifié)

### Expertime (Code `EXPERTIME`)

Cabinet de conseil en transformation digitale basé à Paris, avec implantations à Nantes, Lyon, Aix-en-Provence et Hong Kong. Trois piliers : Conseil - Intégration - Adoption. Services : data & analytics, applications métier sur mesure, solutions low-code, espaces de travail modernes, e-commerce. Engagement transformation durable (méthodologie GreenOps, certification EcoVadis Platinum).

- `Industry` : "Conseil en transformation digitale"
- `Size` : `Medium`
- `Website` : `https://expertime.com`
- `ContactEmail` : `contact@expertime-demo.fr` (fictif)
- `ContactPhone` : `+33140506080` (fictif)
- `Address` : `City` = "Paris", `Country` = "France" (illustrative)

### Dynaminqs (Code `DYNAMINQS`) — **hypothèse à confirmer par l'utilisateur en relecture**

Le site officiel (`https://www.dynaminqs.com`) ne renvoie aucun contenu exploitable (page vide/JS, non indexable). La description suivante provient d'une recherche web complémentaire, **non vérifiée par une source officielle** : groupe spécialisé dans le conseil/intégration Microsoft Dynamics 365 et Power Platform, avec une présence à Dakar (Sénégal), positionné à l'intersection technologie/processus métier pour accompagner la transformation digitale de ses clients.

- `Industry` : "Conseil et intégration Microsoft Dynamics 365 / Power Platform"
- `Size` : `Small` **[À CONFIRMER]** — taille réelle non vérifiée, proposée par défaut (structure de conseil spécialisé, présence géographique limitée à Dakar contrairement aux deux autres entreprises).
- `Website` : `https://www.dynaminqs.com`
- `ContactEmail` : `contact@dynaminqs-demo.fr` (fictif)
- `ContactPhone` : `+221338000000` (fictif, format sénégalais)
- `Address` : `City` = "Dakar", `Country` = "Sénégal" (illustrative)

## Roster de profils utilisateurs (5 par organisation, 15 au total)

Domaine d'email fictif dédié par organisation (`<organisation>-demo.fr`), à ne pas confondre avec un domaine de messagerie réel de l'entreprise. Mot de passe : `Azerty123*` pour tous.

### Meilleurtaux

| Nom | Email | Rôle |
|---|---|---|
| Sophie Lambert | sophie.lambert@meilleurtaux-demo.fr | `Organization.Admin` |
| Julien Moreau | julien.moreau@meilleurtaux-demo.fr | `Organization.Manager` |
| Camille Girard | camille.girard@meilleurtaux-demo.fr | `Organization.Recruiter` |
| Nicolas Petit | nicolas.petit@meilleurtaux-demo.fr | `Organization.Recruiter` |
| Aurélie Dubois | aurelie.dubois@meilleurtaux-demo.fr | `Organization.TechnicalEvaluator` |

### Expertime

| Nom | Email | Rôle |
|---|---|---|
| Thomas Bernard | thomas.bernard@expertime-demo.fr | `Organization.Admin` |
| Claire Rousseau | claire.rousseau@expertime-demo.fr | `Organization.Manager` |
| Mehdi Benali | mehdi.benali@expertime-demo.fr | `Organization.Recruiter` |
| Laura Fontaine | laura.fontaine@expertime-demo.fr | `Organization.Recruiter` |
| Antoine Leroy | antoine.leroy@expertime-demo.fr | `Organization.TechnicalEvaluator` |

### Dynaminqs

| Nom | Email | Rôle |
|---|---|---|
| Fatou Ndiaye | fatou.ndiaye@dynaminqs-demo.fr | `Organization.Admin` |
| Moussa Diop | moussa.diop@dynaminqs-demo.fr | `Organization.Manager` |
| Awa Sarr | awa.sarr@dynaminqs-demo.fr | `Organization.Recruiter` |
| Ibrahima Fall | ibrahima.fall@dynaminqs-demo.fr | `Organization.Recruiter` |
| Khadija Sy | khadija.sy@dynaminqs-demo.fr | `Organization.TechnicalEvaluator` |

`CreatedByUserId` des offres d'emploi (voir plus bas) est réparti en round-robin entre les deux `Organization.Recruiter` de chaque organisation (offres 1, 3, 5, 7, 9 créées par le premier recruteur listé ; offres 2, 4, 6, 8, 10 par le second).

## Offres d'emploi (10 par organisation, 30 au total)

### Règles communes à toutes les offres

- `Status = JobOfferStatus.Published`, `PublishedAt = DateTime.UtcNow` au moment du seed (valeur relative, jamais une date littérale figée, pour ne jamais devenir incohérente selon la date réelle du premier démarrage).
- `ExpiresAt = null` (pas d'expiration) pour toutes les offres de démonstration : une date figée ou relative avec une échéance courte finirait par rendre `JobOffer.IsExpired` vrai avec le temps, sans que le seed (idempotent, ne s'exécutant qu'une fois) ne revienne corriger la donnée. `null` est la valeur la plus robuste pour un jeu de démonstration destiné à rester visible indéfiniment en Development.
- `Location` obligatoire sauf pour les offres en `WorkMode.FullRemote` (règle `RequiresLocation` de `JobOffer`), respectée dans le tableau ci-dessous (colonne "Lieu" vide seulement pour les lignes `FullRemote`).
- `SalaryMin <= SalaryMax` toujours respecté.
- `SalaryCurrency` : `EUR` pour Meilleurtaux et Expertime, `XOF` pour Dynaminqs — champ libre par offre (`JobOffer.SalaryCurrency`), sans conflit avec le correctif `fix-devise-salaire-souhaite-eur-vers-xof.md` (`candidate-app`), qui exclut explicitement `JobOffer.SalaryCurrency` de son périmètre (il ne modifie que `User.DesiredSalary`).
- Niveau de détail attendu pour `Description`/`Requirements`/`Benefits` (tous `[Required]`, non nullable, type `string`) : 2 à 4 phrases par champ, texte réaliste et spécifique au poste (pas un simple recopiage du titre), cohérent avec le secteur de l'entreprise. Un exemple entièrement rédigé est fourni ci-dessous (Meilleurtaux, offre 1) comme gabarit de ton et de longueur ; les 29 autres offres suivent la même structure (`Description` = contexte + mission ; `Requirements` = profil/expérience/diplôme attendus ; `Benefits` = avantages concrets), sans qu'il soit nécessaire de reproduire un texte intégral pour chacune dans cette spécification — au développeur de rédiger un texte du même niveau de qualité pour chaque ligne du tableau, à partir du triplet (titre, secteur, séniorité) qu'elle porte.

### Exemple entièrement rédigé (gabarit à reproduire pour les 29 autres offres)

**Meilleurtaux — Conseiller(ère) en crédit immobilier**
- `Description` : "Au sein de notre agence de Nantes, vous accompagnez une clientèle de particuliers dans leurs projets d'acquisition immobilière. Vous analysez leur situation financière, comparez les offres de nos partenaires bancaires et négociez les meilleures conditions de taux et d'assurance emprunteur."
- `Requirements` : "Formation Bac+2/3 en banque, finance ou commerce. Une première expérience en courtage ou en agence bancaire est appréciée mais non exigée pour les profils juniors motivés. Aisance relationnelle et goût du conseil client indispensables."
- `Benefits` : "Rémunération fixe + variable sur objectifs, mutuelle d'entreprise, tickets restaurant, parcours de formation interne aux produits de crédit et d'assurance."
- `WorkMode = OnSite`, `Location = "Nantes"`, `ContractType = FullTime`, `SalaryMin = 28000`, `SalaryMax = 35000`, `SalaryCurrency = "EUR"`.

### Tableau récapitulatif des 30 offres

**Meilleurtaux** (`SalaryCurrency = EUR` pour toutes les lignes de cette section)

| # | Titre | Contrat | Mode | Lieu | Salaire (EUR) |
|---|---|---|---|---|---|
| 1 | Conseiller(ère) en crédit immobilier | FullTime | OnSite | Nantes | 28 000 – 35 000 |
| 2 | Courtier(ère) en assurance | FullTime | Hybrid | Lyon | 30 000 – 40 000 |
| 3 | Analyste financier(ère) | FullTime | OnSite | Paris | 35 000 – 45 000 |
| 4 | Chargé(e) de clientèle crédit consommation | FullTime | OnSite | Bordeaux | 26 000 – 32 000 |
| 5 | Responsable d'agence | FullTime | OnSite | Marseille | 40 000 – 55 000 |
| 6 | Conseiller(ère) en gestion de patrimoine | FullTime | Hybrid | Paris | 35 000 – 50 000 |
| 7 | Chargé(e) de conformité | FullTime | OnSite | Paris | 38 000 – 48 000 |
| 8 | Data analyst risques crédit | FullTime | FullRemote | *(aucun)* | 36 000 – 46 000 |
| 9 | Alternant(e) conseiller crédit | Internship | OnSite | Toulouse | 18 000 – 20 000 |
| 10 | Responsable marketing digital | FullTime | Hybrid | Paris | 40 000 – 52 000 |

**Expertime** (`SalaryCurrency = EUR` pour toutes les lignes de cette section)

| # | Titre | Contrat | Mode | Lieu | Salaire (EUR) |
|---|---|---|---|---|---|
| 1 | Consultant(e) Data & Analytics | FullTime | Hybrid | Paris | 40 000 – 55 000 |
| 2 | Développeur(se) Low-Code (Power Apps/OutSystems) | FullTime | Hybrid | Nantes | 38 000 – 48 000 |
| 3 | Chef(fe) de projet transformation digitale | FullTime | OnSite | Lyon | 45 000 – 60 000 |
| 4 | Développeur(se) Full-Stack e-commerce | FullTime | FullRemote | *(aucun)* | 40 000 – 52 000 |
| 5 | Consultant(e) conduite du changement | FullTime | Hybrid | Aix-en-Provence | 38 000 – 48 000 |
| 6 | Architecte solutions cloud | FullTime | Hybrid | Paris | 55 000 – 70 000 |
| 7 | UX/UI Designer espaces de travail modernes | FullTime | Hybrid | Nantes | 35 000 – 45 000 |
| 8 | Consultant(e) GreenOps / RSE numérique | FullTime | Hybrid | Paris | 42 000 – 52 000 |
| 9 | Business Analyst applications métier | FullTime | OnSite | Lyon | 36 000 – 46 000 |
| 10 | Stagiaire consultant(e) data (6 mois) | Internship | OnSite | Paris | 12 000 – 15 000 |

**Dynaminqs** (`SalaryCurrency = XOF` pour toutes les lignes de cette section, montants annuels bruts)

| # | Titre | Contrat | Mode | Lieu | Salaire (XOF) |
|---|---|---|---|---|---|
| 1 | Consultant(e) fonctionnel(le) Dynamics 365 F&O | FullTime | OnSite | Dakar | 6 000 000 – 9 000 000 |
| 2 | Développeur(se) Power Platform (Power Apps/Power Automate) | FullTime | Hybrid | Dakar | 5 500 000 – 8 000 000 |
| 3 | Architecte solution Microsoft Dynamics 365 | FullTime | OnSite | Dakar | 9 000 000 – 13 000 000 |
| 4 | Chef(fe) de projet intégration Dynamics 365 | FullTime | OnSite | Dakar | 7 000 000 – 10 000 000 |
| 5 | Consultant(e) Power BI / Data Analyst | FullTime | Hybrid | Dakar | 5 000 000 – 7 500 000 |
| 6 | Développeur(se) .NET / Dynamics 365 CE | FullTime | FullRemote | *(aucun)* | 6 000 000 – 9 000 000 |
| 7 | Business Analyst transformation digitale | FullTime | OnSite | Dakar | 4 500 000 – 7 000 000 |
| 8 | Administrateur(trice) systèmes Dynamics 365 | FullTime | OnSite | Dakar | 4 000 000 – 6 000 000 |
| 9 | Consultant(e) conduite du changement | FullTime | Hybrid | Dakar | 4 500 000 – 6 500 000 |
| 10 | Stagiaire développeur(se) Power Platform | Internship | OnSite | Dakar | 1 500 000 – 2 000 000 |

## Candidats de démonstration (4, tous rattachés aux 30 offres, pas à une seule organisation)

Décision validée par l'utilisateur : **exactement 4 candidats au total** (pas 4 par entreprise), avec un mix de consonances sénégalaises et françaises, chacun candidatant sur 2 à 3 offres réparties entre plusieurs des 3 entreprises (pas 1 candidat = 1 entreprise), pour un jeu de données varié.

Domaine d'email fictif dédié : `candidat-demo.fr` (distinct des domaines `<organisation>-demo.fr` du roster d'organisation, puisqu'un candidat n'est rattaché à aucune organisation — `OrganizationId = null`, cf. `User.IsCandidate`). Mot de passe : `Azerty123*` pour tous. Rôle `Candidate` assigné via `UserRole` à chacun. Mêmes propriétés d'activation que le reste du seed (`EmailConfirmed = true`, `IsActive = true`, `UserName = Email`, `ConsentGivenAt = DateTime.UtcNow`).

| Candidat | Email | Consonance |
|---|---|---|
| Aïssatou Ba | aissatou.ba@candidat-demo.fr | Sénégalaise |
| Ousmane Kane | ousmane.kane@candidat-demo.fr | Sénégalaise |
| Léa Dupont | lea.dupont@candidat-demo.fr | Française |
| Maxime Renard | maxime.renard@candidat-demo.fr | Française |

### Répartition des candidatures (11 `Application` au total)

| Candidat | Offres visées | Entreprises couvertes |
|---|---|---|
| Aïssatou Ba | Meilleurtaux #1 (Conseiller crédit immobilier), Expertime #1 (Consultant Data & Analytics), Dynaminqs #1 (Consultant fonctionnel D365) | 3 |
| Ousmane Kane | Expertime #2 (Développeur Low-Code), Dynaminqs #2 (Développeur Power Platform), Dynaminqs #6 (Développeur .NET/D365 CE) | 2 |
| Léa Dupont | Meilleurtaux #5 (Responsable d'agence), Expertime #3 (Chef de projet transformation digitale) | 2 |
| Maxime Renard | Meilleurtaux #2 (Courtier en assurance), Meilleurtaux #6 (Conseiller gestion de patrimoine), Dynaminqs #5 (Consultant Power BI) | 2 |

Pour chaque `Application` créée :
- `CurrentStatus = ApplicationStatus.Applied` (valeur par défaut de l'entité, pas de variation de statut proposée par cette spec — simplicité du jeu de données de démo).
- `AppliedAt = DateTime.UtcNow` au moment du seed (valeur relative, cohérent avec la contrainte `CK_Application_AppliedAt` en base : `AppliedAt <= GETUTCDATE()`).
- `CoverLetter`/`AdditionalNotes`/`Rating`/`AssignedTechnicalEvaluatorId`/`AssignedManagerId` : laissés `null` (aucun de ces champs n'est requis par le modèle ; pas de valeur de démonstration proposée ici, pour ne pas complexifier inutilement le jeu de données).

## Récapitulatif chiffré (pour vérification rapide)

- 3 `Organization` (`MEILLEURTAUX`, `EXPERTIME`, `DYNAMINQS`).
- 15 `User` d'organisation (5 par organisation : 1 `Organization.Admin`, 1 `Organization.Manager`, 2 `Organization.Recruiter`, 1 `Organization.TechnicalEvaluator`) + 15 `UserRole` associés.
- 30 `JobOffer` (10 par organisation), toutes `Published`.
- 4 `User` candidats (sans organisation) + 4 `UserRole` (`Candidate`).
- 11 `Application`.

## Hors périmètre

- Variation du `CurrentStatus` des candidatures au-delà de `Applied` (ex. simuler un pipeline de recrutement avec des candidatures à différents stades) : non demandé, non traité ici.
- Upload de CV réel (`CvPath`) pour les 4 candidats : non demandé ; les candidats de démonstration n'ont pas de CV associé par cette spec.
- Expériences (`Experience`)/formations (`Training`) pour les 4 candidats : non demandées, non seedées.
- `ApplicationStatusHistory` : aucun historique de statut créé (cohérent avec `CurrentStatus = Applied` par défaut, qui ne nécessite pas de transition).
- Toute activation de ce jeu de données en Staging : explicitement exclue par le garde-fou (§Garde-fou d'environnement) ; un retour de l'utilisateur en relecture pourrait changer ce choix, non anticipé ici.
- Tests automatisés : cette spec définit des critères d'acceptation vérifiables ; l'écriture effective de tests relève de l'agent `developer`.

## Critères d'acceptation

1. Au premier démarrage en Development (base vide de ces données) : exactement 3 `Organization` de démonstration créées (`Code` = `MEILLEURTAUX`/`EXPERTIME`/`DYNAMINQS`), 15 `User` d'organisation + 15 `UserRole` actifs avec le rôle attendu par profil, 30 `JobOffer` toutes `Status = Published` avec `PublishedAt` renseigné, 4 `User` candidats + 4 `UserRole` (`Candidate`), 11 `Application`.
2. Chaque `JobOffer` a `OrganizationId` correspondant à l'organisation attendue (cohérente avec le tableau §Offres d'emploi) et `CreatedByUserId` référençant un utilisateur du roster de la **même** organisation (jamais un utilisateur d'une autre organisation ou un candidat).
3. Chaque `JobOffer` respecte `RequiresLocation` (Location renseignée sauf `FullRemote`) et `SalaryMin <= SalaryMax`.
4. Chaque `Application` a un `JobOfferId` et un `CandidateId` correspondant à la répartition du tableau §Répartition des candidatures, sans doublon de couple `(JobOfferId, CandidateId)`.
5. Tous les comptes créés (organisation + candidats) peuvent se connecter via `POST /api/auth/login` avec leur email et le mot de passe `Azerty123*` (ou l'équivalent endpoint déjà existant), preuve que `EmailConfirmed`/`IsActive`/hash de mot de passe sont correctement positionnés.
6. Un second démarrage de l'application (sans modification manuelle de la base) laisse les 6 compteurs ci-dessus strictement inchangés (aucune duplication d'organisation, d'utilisateur, de rôle assigné, d'offre, de candidat, de candidature).
7. En environnement Staging ou Production (`ASPNETCORE_ENVIRONMENT` positionné en conséquence), aucune donnée de démonstration n'est créée au démarrage — seules les 3 étapes de seed déjà existantes (organisation XpertSphere, rôles, PlatformSuperAdmin) s'exécutent, comme avant ce correctif.
8. Le seed existant (organisation `XPERTSPHERE`, rôles, `PlatformSuperAdmin`) continue de fonctionner à l'identique en Development comme en Staging/Production : aucune régression sur son comportement ou son idempotence propre.

## Questions résiduelles

Aucune question bloquante identifiée au-delà des points déjà marqués `[À CONFIRMER]` dans le corps de la spec (taille `Small` de Dynaminqs, description Dynaminqs elle-même basée sur une recherche web non officielle). Ces deux points sont à trancher par l'utilisateur en relecture, sans bloquer le développement (des valeurs par défaut raisonnables sont déjà en place).

## Fichiers à créer/modifier (récapitulatif)

- `Extensions/DatabaseExtensions.cs` : nouvelle signature de `SeedDatabaseAsync` (paramètre `IWebHostEnvironment environment`), nouvel appel conditionnel `SeedDemoDataAsync`, et nouvelles méthodes privées `SeedDemoDataAsync`, `SeedDemoOrganizationsAsync`, `SeedDemoOrganizationUsersAsync`, `SeedDemoJobOffersAsync`, `SeedDemoCandidatesAsync`, `SeedDemoApplicationsAsync` (ou un découpage équivalent au choix du développeur, tant que l'ordre de dépendance et l'idempotence par entité décrits ci-dessus sont respectés). Éventuellement un fichier annexe (ex. `Extensions/DatabaseExtensions.DemoData.cs`, `partial class`) si le volume de données inline rend `DatabaseExtensions.cs` difficile à maintenir en un seul fichier — au choix du développeur, sans impact sur le contrat de cette spec.
- Aucune migration EF Core requise (aucun changement de schéma).
- Aucun changement côté frontend (`candidate-app`, `recruiter-app`) ni dans les autres services du monorepo.
