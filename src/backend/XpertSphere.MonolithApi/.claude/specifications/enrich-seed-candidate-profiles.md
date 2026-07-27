# Enrichissement des profils des 4 candidats de démonstration (expériences, formations, champs scalaires, adresse, complétude)

## Contexte et périmètre

Cette spécification **étend** `seed-demo-organizations-users-joboffers.md` (déjà implémentée : `Extensions/DatabaseExtensions.DemoData.cs`, méthode `SeedDemoCandidatesAsync`). Cette dernière crée les 4 `User` candidats de démonstration (Aïssatou Ba, Ousmane Kane, Léa Dupont, Maxime Renard) mais ne renseigne que `FirstName`/`LastName`/`Email`/`UserName`/`EmailConfirmed`/`IsActive`/`CreatedAt`/`ConsentGivenAt` — tout le reste du profil (`PhoneNumber`, `LinkedInProfile`, `Skills`, `YearsOfExperience`, `DesiredSalary`/`DesiredSalaryCurrency`, `Availability`, `Address`, `Experiences`, `Trainings`) reste vide ou à sa valeur par défaut C#. `seed-demo-organizations-users-joboffers.md` liste d'ailleurs explicitement ce manque dans son §Hors périmètre ("Expériences/formations pour les 4 candidats : non demandées, non seedées").

Conséquence visible côté `recruiter-app` (`CandidateDetailPage.vue`) : pour les 4 candidats de démo, les sections "Formations"/"Expériences professionnelles" ne s'affichent pas du tout (rendues seulement `v-if` un tableau non vide), la carte "Informations professionnelles" affiche "Non spécifié"/masque ses champs, et la barre de "Complétude du profil" reste bloquée à 0 %.

Cette spec couvre :
1. Ajout de 2 `Experience` et 1 `Training` réalistes par candidat, narrativement cohérents avec l'historique déjà écrit par `seed-demo-organizations-users-joboffers.md` (candidatures de chaque candidat, secteurs des 3 organisations clientes).
2. Renseignement des champs scalaires `User` manquants : `PhoneNumber`, `LinkedInProfile`, `Skills`, `YearsOfExperience`, `DesiredSalary`/`DesiredSalaryCurrency`, `Availability`.
3. Renseignement d'une `Address` complète et cohérente par candidat (au lieu du défaut C# `Country = "France"`, reste `null`).
4. Correction de `ProfileCompletionPercentage`, resté à 0 % faute d'appel à `User.CalculateProfileCompletion()` sur le chemin de seed (qui crée les `User` via `UserManager.CreateAsync` directement, en dehors de `IUserService`).

Comme `seed-demo-organizations-users-joboffers.md`, ce n'est pas un script one-off : c'est une extension de `SeedDemoCandidatesAsync` (et, ponctuellement, de `SeedDemoDataAsync` si l'orchestration doit changer), avec le même niveau d'idempotence que l'existant.

**Rappel de la contrainte non négociable de l'utilisateur** : cette spec **n'aborde pas** `CvPath`/l'upload de CV, sous aucune forme. Voir §Hors périmètre.

## Décision de conception centrale : "renseigner seulement si vide", pas "toujours écraser"

Ce point est le plus structurant de la spec et diffère du reste de `DatabaseExtensions.DemoData.cs`, où les entités sans état modifiable (organisations, offres, candidatures) sont simplement re-vérifiées par une clé stable puis laissées intactes si elles existent déjà. Ici, on modifie un `User` déjà existant (créé par une version antérieure du seed, ou par ce correctif lui-même à un boot précédent) — un cas que le seed actuel ne traite jamais (il ne fait que créer, jamais mettre à jour, un `User`).

Deux comportements étaient possibles :
- (a) toujours réaffecter les valeurs de démonstration ci-dessous à chaque démarrage, sans condition ;
- (b) ne les affecter que si le champ correspondant est actuellement vide/`null`.

**Décision retenue : (b), "renseigner seulement si vide"**, pour deux raisons :
- **Enrichissement rétroactif des environnements déjà seedés** : toute base de développement déjà démarrée avec la version actuelle de `seed-demo-organizations-users-joboffers.md` (avant ce correctif) a déjà ses 4 candidats créés avec des champs vides. Avec (b), au prochain démarrage, ces champs vides sont détectés et remplis — le correctif s'applique donc aussi bien à une base neuve qu'à une base de développement existante. Avec (a) seul, le comportement serait identique pour ce cas précis (les champs sont vides de toute façon au premier passage du correctif), donc (a) fonctionnerait aussi initialement — mais voir le point suivant.
- **Non-destruction d'une modification faite depuis l'application** : un compte candidat de démo est un compte de connexion pleinement fonctionnel (`Azerty123*`), qu'un utilisateur de la plateforme (démonstration client, testeur) peut modifier depuis `candidate-app` (`EditProfileDialog.vue`) entre deux redémarrages de l'API — par exemple changer ses compétences ou son numéro de téléphone pour une démonstration particulière. Avec (a), un redémarrage de l'API écraserait silencieusement cette modification en la remplaçant par la valeur de démonstration figée dans le code. Avec (b), une fois le champ renseigné (par le seed ou par l'utilisateur), le seed ne le retouche plus jamais. Ce comportement est cohérent avec la philosophie "ne jamais écraser une donnée existante" qui gouverne déjà tout le reste du seeder (organisations/offres/candidatures ne sont jamais mises à jour une fois créées).

Concrètement, pour chaque champ scalaire nullable listé plus bas : `if (string.IsNullOrEmpty(user.PhoneNumber)) user.PhoneNumber = seed.PhoneNumber;` (même logique pour `LinkedInProfile`, `Skills`), `if (!user.YearsOfExperience.HasValue) ...`, `if (!user.Availability.HasValue) ...`. Pour `Address` : `if (user.Address.IsEmpty) { user.Address = new Address { ... }; }` (`Address.IsEmpty` est une propriété déjà existante sur `Models/Base/Address.cs`, qui teste `StreetName`/`City`/`PostalCode`).

**Cas particulier `DesiredSalary`/`DesiredSalaryCurrency` : un seul et même garde-fou, sur `DesiredSalary` uniquement — jamais un garde-fou séparé sur `DesiredSalaryCurrency`.** Raison : `configurable-salary-currency.md` (déjà implémentée) a backfillé par migration `DesiredSalaryCurrency = Currency.XOF` pour **tous** les candidats existants (`OrganizationId IS NULL`), y compris les 4 candidats de démonstration déjà créés par une version antérieure du seed — mais n'a **jamais** touché `DesiredSalary` (resté `null` pour eux, aucune migration ne le renseigne). Si `DesiredSalaryCurrency` avait son propre garde-fou "seulement si vide", il serait déjà non-`null` (`XOF`, posé par la migration) pour ces candidats préexistants et ne serait donc **jamais** réaffecté à `EUR` par ce correctif pour Léa Dupont/Maxime Renard — ce qui produirait, une fois `DesiredSalary` renseigné par le garde-fou séparé, une donnée absurde (`48000 XOF`/`42000 XOF`, soit environ 73€/64€) et contredirait le tableau §Champs scalaires. La règle correcte est donc :

```csharp
if (!user.DesiredSalary.HasValue)
{
    user.DesiredSalary = seed.DesiredSalary;
    user.DesiredSalaryCurrency = seed.DesiredSalaryCurrency; // toujours affecté dans le même bloc, jamais derrière son propre if
}
```

Ce garde-fou unique satisfait les trois cas : base neuve (`DesiredSalary` et `DesiredSalaryCurrency` tous deux `null` → les deux sont posés, `EUR`/`XOF` corrects par candidat) ; base existante déjà backfillée par la migration (`DesiredSalary` reste `null`, `DesiredSalaryCurrency` déjà `XOF` par la migration → le bloc s'exécute quand même car c'est `DesiredSalary` qui est testé, et écrase le `XOF` de la migration par la devise correcte du tableau) ; modification manuelle ultérieure par l'opérateur (un candidat dont `DesiredSalary` a été renseigné, par le seed ou via `EditProfileDialog.vue`, a déjà `HasValue == true` → le bloc entier est ignoré, montant **et** devise restent tels que laissés par l'opérateur, jamais écrasés).

Pour `Experience`/`Trainings` (voir §Idempotence Experience/Training ci-dessous) : le critère n'est pas "champ vide" mais "le candidat n'a **aucune** ligne existante dans cette table" — ajouter uniquement si `Experiences`/`Trainings` est actuellement vide pour ce candidat, jamais sinon (pas de complément partiel, pas de doublon).

## Idempotence — `Experience`/`Training` (aucune clé naturelle)

Contrairement aux entités déjà traitées par `seed-demo-organizations-users-joboffers.md` (qui ont toutes un critère de vérification stable explicite — voir son tableau §Idempotence), `Experience` et `Training` n'ont **aucune contrainte d'unicité naturelle** en base (pas de couple `(UserId, Title)` unique, `Title`/`Company`/`School` sont de simples chaînes libres). Critère retenu, à appliquer par candidat, **avant** toute insertion :

> Le candidat a-t-il **au moins une** ligne `Experience` (respectivement `Training`) déjà associée à son `UserId`, tous critères confondus (seedée par un boot précédent de ce correctif, ou ajoutée manuellement par le candidat via `candidate-app`) ? Si oui, ne rien ajouter. Si non (candidat sans aucune expérience/formation), ajouter l'ensemble des entrées de démonstration listées plus bas pour ce candidat.

Ce test se fait par un `AnyAsync` sur `context.Experiences`/`context.Trainings` filtré par `UserId`, **pas** en relisant la collection de navigation `user.Experiences`/`user.Trainings` (voir §Point d'attention : chargement explicite requis pour `ProfileCompletionPercentage`, qui explique pourquoi ces collections doivent de toute façon être chargées explicitement).

Ce choix ("tout ou rien" par candidat, pas de complément ligne par ligne) est délibérément simple : il évite d'avoir à identifier individuellement chaque `Experience`/`Training` de démonstration par un critère ad hoc, au prix de ne pas pouvoir ajouter une 3ᵉ expérience de démonstration à un candidat qui en aurait déjà ajouté une manuellement — cas jugé suffisamment marginal pour un jeu de données de démonstration.

## Point d'attention : chargement explicite requis pour `ProfileCompletionPercentage`

`User.CalculateProfileCompletion()` (`Models/User.cs:176-197`) lit directement les collections de navigation en mémoire `Experiences`/`Trainings` de l'instance `User` (`if (Experiences is { Count: > 0 }) completedFields++;`, `if (Trainings.Count > 0) completedFields++;`) — elle ne requête jamais la base elle-même. Un `User` obtenu via `userManager.FindByEmailAsync(email)` (comme le fait déjà `SeedDemoCandidatesAsync`) n'a **pas** ses collections `Experiences`/`Trainings` chargées (pas d'`Include`) : elles apparaissent comme des listes vides en mémoire, que le candidat ait ou non déjà des lignes en base.

**Piège à éviter explicitement** : si le développeur ne charge pas ces collections avant d'appeler `CalculateProfileCompletion()`, le calcul de complétude serait correct au tout premier boot où les entrées sont ajoutées (elles sont alors trackées en mémoire dans la même passe), mais **redeviendrait incorrect à chaque boot suivant** — puisque, par idempotence (§Idempotence ci-dessus), aucune nouvelle ligne n'est ajoutée les fois suivantes, et les collections en mémoire resteraient vides, faisant chuter `ProfileCompletionPercentage` en dessous de sa valeur réelle (régression silencieuse à chaque redémarrage après le premier).

**Solution requise** : avant de décider d'ajouter ou non des `Experience`/`Training` (et dans tous les cas, avant tout appel à `CalculateProfileCompletion()`), charger explicitement les entrées existantes du candidat, par exemple :

```csharp
var existingExperiences = await context.Experiences
    .Where(e => e.UserId == user.Id)
    .ToListAsync();
var existingTrainings = await context.Trainings
    .Where(t => t.UserId == user.Id)
    .ToListAsync();

if (existingExperiences.Count == 0)
{
    foreach (var exp in seed.Experiences)
    {
        user.Experiences.Add(new Experience { UserId = user.Id, /* ... */ });
    }
}
else
{
    foreach (var exp in existingExperiences) user.Experiences.Add(exp);
}
// même logique pour user.Trainings / existingTrainings
```

Ajouter les entités **à la collection de navigation `user.Experiences`/`user.Trainings`** (comme ci-dessus), pas seulement à `context.Experiences`/`context.Trainings` : c'est ce qui garantit que `user.CalculateProfileCompletion()`, appelée juste après, voit un compte à jour, que les lignes viennent d'être ajoutées ou qu'elles existaient déjà. `Experience.UserId`/`Training.UserId` sont de toute façon aussi déduits automatiquement par la correction de relation d'EF Core lors du `SaveChanges` (l'assignation explicite ci-dessus reste néanmoins recommandée pour la lisibilité, cohérente avec `[Required] public Guid UserId` sur les deux modèles).

## Champs scalaires `User` à renseigner (les 4 candidats)

Table récapitulative — voir §Décision "renseigner seulement si vide" pour la sémantique exacte de l'affectation.

| Champ | Aïssatou Ba | Ousmane Kane | Léa Dupont | Maxime Renard |
|---|---|---|---|---|
| `PhoneNumber` | `+221771234501` | `+221771234502` | `+33612345601` | `+33612345602` |
| `LinkedInProfile` | `https://www.linkedin.com/in/aissatou-ba-demo` | `https://www.linkedin.com/in/ousmane-kane-demo` | `https://www.linkedin.com/in/lea-dupont-demo` | `https://www.linkedin.com/in/maxime-renard-demo` |
| `Skills` | `Analyse financière, Relation client, Excel, Power BI, Anglais professionnel` | `C#, .NET, Power Apps, Power Automate, SQL Server, JavaScript` | `Management d'équipe, Gestion de projet, Transformation digitale, Prince2, Excel` | `Assurance, Gestion de patrimoine, Power BI, Excel avancé, Relation client` |
| `YearsOfExperience` | `5` | `4` | `8` | `6` |
| `DesiredSalary` | `6500000` | `6800000` | `48000` | `42000` |
| `DesiredSalaryCurrency` | `Currency.XOF` | `Currency.XOF` | `Currency.EUR` | `Currency.EUR` |
| `Availability` (figée au premier renseignement, jamais recalculée aux boots suivants) | `DateTime.UtcNow.AddDays(30)` | `DateTime.UtcNow.AddDays(15)` | `DateTime.UtcNow.AddDays(45)` | `DateTime.UtcNow.AddDays(20)` |

Format de `Skills` : une seule chaîne, valeurs séparées par des virgules — convention déjà utilisée côté frontend (`recruiter-app/CandidateDetailPage.vue`, `skillsArray = candidate.skills.split(',')`), respectée ici.

### `DesiredSalaryCurrency` : XOF pour les profils basés à Dakar, EUR pour les profils basés en France (décision, pas une déduction automatique)

`configurable-salary-currency.md` (déjà implémentée, `Migrations/*AddConfigurableSalaryCurrency*`) a backfillé tous les candidats existants à `Currency.XOF` par défaut. Cette spec s'en écarte **intentionnellement pour 2 des 4 candidats** (Léa Dupont, Maxime Renard) : plutôt que XOF pour tous, chaque candidat reçoit la devise cohérente avec son adresse de démonstration (§Adresse ci-dessous) — XOF pour les deux candidats domiciliés à Dakar (Aïssatou Ba, Ousmane Kane), EUR pour les deux domiciliés en France (Léa Dupont, Maxime Renard). Rationale : un jeu de données de démonstration cohérent (adresse France + salaire souhaité en XOF serait une incohérence visible dans `CandidateDetailPage.vue`/`ProfilePage.vue`, qui affichent l'un à côté de l'autre) prime ici sur la reproduction stricte de la valeur de backfill par défaut, qui visait surtout les comptes candidats réels préexistants sans contexte narratif associé.

**Point d'implémentation important, pas seulement une nuance** : `DesiredSalaryCurrency` **n'a pas son propre garde-fou "seulement si vide"** — voir §Décision de conception centrale, "Cas particulier `DesiredSalary`/`DesiredSalaryCurrency`" ci-dessus, qui explique pourquoi un garde-fou séparé sur `DesiredSalaryCurrency` produirait une incohérence (le champ est déjà `XOF` pour tout candidat backfillé par la migration `configurable-salary-currency.md`, donc un garde-fou propre ne le laisserait jamais repasser à `EUR`). `DesiredSalaryCurrency` est toujours affecté **dans le même bloc conditionnel que `DesiredSalary`**, gouverné par le seul test `!user.DesiredSalary.HasValue`.

## Adresse (`Address`) — une par candidat, cohérente avec la consonance/le contexte narratif

Comme pour les organisations de démonstration (`seed-demo-organizations-users-joboffers.md`, §Descriptions d'entreprise), ces adresses sont illustratives, pas des adresses réelles vérifiées.

| Candidat | `StreetNumber` | `StreetName` | `City` | `PostalCode` | `Country` |
|---|---|---|---|---|---|
| Aïssatou Ba | `15` | `Rue de Fann` | `Dakar` | `10700` | `Sénégal` |
| Ousmane Kane | `7` | `Avenue Cheikh Anta Diop` | `Dakar` | `10700` | `Sénégal` |
| Léa Dupont | `23` | `Rue de la République` | `Lyon` | `69002` | `France` |
| Maxime Renard | `5` | `La Canebière` | `Marseille` | `13001` | `France` |

`Region`/`AddressLine2` : laissés `null` pour les 4 (pas d'information supplémentaire pertinente, cohérent avec le niveau de détail des adresses d'organisation déjà seedées, qui ne renseignent elles non plus que `City`/`Country`). Affectation complète de `Address` uniquement si `user.Address.IsEmpty` (§Décision "renseigner seulement si vide").

## Expériences (`Experience`) et formation (`Training`) — 2 expériences + 1 formation par candidat

Cohérence narrative : chaque candidat a un profil (secteur, compétences) qui justifie ses candidatures déjà définies dans `seed-demo-organizations-users-joboffers.md` (§Répartition des candidatures) — profils bancaires/finance pour Aïssatou Ba (candidate chez Meilleurtaux, Expertime, Dynaminqs), profil développeur pour Ousmane Kane (candidat chez Expertime/Dynaminqs sur des postes techniques), profil management/conseil pour Léa Dupont (candidate sur des postes de responsable/chef de projet), profil assurance/patrimoine avec appétence data pour Maxime Renard (candidat chez Meilleurtaux sur des postes assurance/patrimoine et chez Dynaminqs sur un poste Power BI).

Aucune des entreprises fictives des candidats précédents (`Banque Atlantique Sénégal`, `Sonatel`, `Société Générale`, `Sopra Steria`, `AXA France`, `Groupe Premium Courtage`, `Audencia Business School`, etc.) ne recoupe intentionnellement les 3 organisations clientes de démonstration (`Meilleurtaux`, `Expertime`, `Dynaminqs`) : un candidat n'a donc jamais déjà travaillé chez l'entreprise à laquelle il postule, ce qui aurait été une incohérence narrative.

### Aïssatou Ba

**Expérience 1**
- `Title` : `Chargée de clientèle particuliers`
- `Company` : `Banque Atlantique Sénégal`
- `Location` : `Dakar`
- `Date` : `09/2019 - 08/2022`
- `IsCurrent` : `false`
- `Description` : "Conseil et accompagnement d'une clientèle de particuliers dans leurs projets de crédit et d'épargne. Analyse des dossiers de financement, présentation des offres bancaires et suivi de la relation client sur un portefeuille de plus de 300 clients."

**Expérience 2**
- `Title` : `Analyste crédit junior`
- `Company` : `Société Générale Sénégal`
- `Location` : `Dakar`
- `Date` : `09/2022 - Présent`
- `IsCurrent` : `true`
- `Description` : "Étude et instruction des dossiers de crédit à la consommation et immobilier, évaluation du risque client et rédaction des recommandations de financement pour le comité de crédit."

**Formation**
- `School` : `Université Cheikh Anta Diop de Dakar (UCAD)`
- `Field` : `Finance et Contrôle de Gestion`
- `Level` : `Master / Bac+5`
- `Period` : `09/2015 - 06/2019`

### Ousmane Kane

**Expérience 1**
- `Title` : `Développeur back-end .NET`
- `Company` : `Sonatel`
- `Location` : `Dakar`
- `Date` : `10/2020 - 12/2022`
- `IsCurrent` : `false`
- `Description` : "Développement et maintenance d'API internes en C#/.NET pour les systèmes de facturation. Participation aux revues de code et à la mise en place de tests unitaires automatisés."

**Expérience 2**
- `Title` : `Développeur Power Platform freelance`
- `Company` : `Indépendant`
- `Location` : `Dakar`
- `Date` : `01/2023 - Présent`
- `IsCurrent` : `true`
- `Description` : "Conception d'applications métier avec Power Apps et automatisation de processus avec Power Automate pour des PME locales, de la prise de besoin jusqu'à la mise en production."

**Formation**
- `School` : `École Supérieure Polytechnique de Dakar (ESP)`
- `Field` : `Génie Logiciel`
- `Level` : `Ingénieur / Bac+5`
- `Period` : `09/2016 - 07/2020`

### Léa Dupont

**Expérience 1**
- `Title` : `Responsable d'agence bancaire`
- `Company` : `Société Générale`
- `Location` : `Nantes`
- `Date` : `03/2016 - 05/2021`
- `IsCurrent` : `false`
- `Description` : "Management d'une équipe de 6 conseillers, pilotage de l'activité commerciale de l'agence et développement du portefeuille clients particuliers et professionnels."

**Expérience 2**
- `Title` : `Cheffe de projet transformation digitale`
- `Company` : `Sopra Steria`
- `Location` : `Lyon`
- `Date` : `06/2021 - Présent`
- `IsCurrent` : `true`
- `Description` : "Pilotage de projets de transformation digitale pour des clients du secteur bancaire : cadrage, coordination des équipes techniques et fonctionnelles, suivi budgétaire et accompagnement du changement."

**Formation**
- `School` : `Audencia Business School`
- `Field` : `Management et Stratégie d'Entreprise`
- `Level` : `Master / Bac+5`
- `Period` : `09/2011 - 06/2016`

### Maxime Renard

**Expérience 1**
- `Title` : `Courtier en assurance`
- `Company` : `AXA France`
- `Location` : `Lyon`
- `Date` : `09/2017 - 08/2021`
- `IsCurrent` : `false`
- `Description` : "Développement d'un portefeuille de clients particuliers et professionnels, conseil en assurance habitation, auto et santé, et négociation des contrats auprès des compagnies partenaires."

**Expérience 2**
- `Title` : `Consultant en gestion de patrimoine`
- `Company` : `Groupe Premium Courtage`
- `Location` : `Marseille`
- `Date` : `09/2021 - Présent`
- `IsCurrent` : `true`
- `Description` : "Réalisation de bilans patrimoniaux complets et proposition de stratégies d'investissement adaptées aux objectifs des clients (assurance-vie, immobilier, défiscalisation)."

**Formation**
- `School` : `Université Paris-Dauphine`
- `Field` : `Gestion de Patrimoine et Finance`
- `Level` : `Master / Bac+5`
- `Period` : `09/2013 - 06/2017`

Toutes les valeurs ci-dessus respectent les contraintes de longueur des modèles (`Experience.Title`/`Location`/`Company` ≤ 100, `Experience.Date` ≤ 40 ; `Training.School` ≤ 100, `Training.Period` ≤ 40, `Training.Field` ≤ 150, `Training.Level` ≤ 60).

## `ProfileCompletionPercentage` attendu : 91 % pour les 4 candidats (pas 100 %)

`User.CalculateProfileCompletion()` compte 12 champs au total (`Models/User.cs:179`), dont `CvPath` (`if (!string.IsNullOrEmpty(CvPath)) completedFields++;`). Ce correctif ne renseigne **jamais** `CvPath` (voir §Hors périmètre) : au mieux, 11 des 12 champs sont remplis pour chacun des 4 candidats (`FirstName`, `LastName`, `Email` déjà présents + les 8 champs renseignés par ce correctif : `PhoneNumber`, `Address.StreetName`, `Skills`, `YearsOfExperience`, `LinkedInProfile`, `Experiences non vide`, `Trainings non vide`, `Availability`).

`ProfileCompletionPercentage = (11 * 100) / 12 = 91` (division entière C#, `1100 / 12 = 91.67` tronqué à `91`). **91 % est donc la valeur maximale atteignable et attendue par ce correctif, pas 100 %** — ne pas considérer un écart à 100 % comme un défaut lors de la validation ; seul un utilisateur ajoutant manuellement un CV (comme annoncé par l'utilisateur pour son candidat de démonstration) ferait passer ce candidat précis à 100 %.

## Ordre d'implémentation dans `SeedDemoCandidatesAsync`

Ce correctif modifie la méthode existante `SeedDemoCandidatesAsync` (`Extensions/DatabaseExtensions.DemoData.cs:273-339`), pas son orchestration globale (`SeedDemoDataAsync` reste inchangée dans son enchaînement d'étapes). Pour chaque candidat de la boucle existante (`foreach (var seed in DemoCandidates)`), **après** la résolution/création de `user` et **avant** l'affectation du rôle `Candidate` (ou après, l'ordre relatif entre les deux n'a pas d'importance) :

1. Charger `existingExperiences`/`existingTrainings` (voir §Point d'attention ci-dessus).
2. Affecter les champs scalaires "seulement si vide" (§Champs scalaires, §Adresse).
3. Ajouter les `Experience`/`Training` de démonstration à `user.Experiences`/`user.Trainings` uniquement si `existingExperiences`/`existingTrainings` est vide ; sinon, réintégrer les entrées existantes dans la collection de navigation (nécessaire à l'étape 4).
4. Appeler `user.CalculateProfileCompletion()`.
5. Ne pas appeler `userManager.UpdateAsync(user)` explicitement : laisser le `context.SaveChangesAsync()` déjà présent en fin de `SeedDemoDataAsync`/`SeedDatabaseAsync` flusher l'ensemble (`user` est déjà tracké par le même `DbContext` que `userManager`, qu'il vienne d'être créé via `CreateAsync` ou retrouvé via `FindByEmailAsync` puis réattaché par la requête EF Core de l'étape 1 — à vérifier empiriquement par le développeur ; si `FindByEmailAsync` ne renvoie pas une entité trackée par le même contexte que celui passé en paramètre, un appel explicite à `context.Update(user)` ou `userManager.UpdateAsync(user)` peut s'avérer nécessaire — point d'implémentation non garanti à 100 % par cette spec, cf. §Questions résiduelles).

Le développeur reste libre d'organiser ce code en méthodes privées supplémentaires (ex. `EnrichDemoCandidateProfileAsync`) si cela améliore la lisibilité de `SeedDemoCandidatesAsync`, tant que le comportement décrit ci-dessus est respecté.

## Hors périmètre

- **CV / upload de fichier (`CvPath`) : explicitement exclu, contrainte non négociable de l'utilisateur.** Aucune ligne de ce correctif ne doit lire, écrire, ni mentionner `CvPath` dans le code de seed. L'utilisateur ajoutera lui-même un CV pour le candidat qu'il présentera en démo, via l'upload existant (`secure-cv-download.md`/mécanisme d'upload déjà en place) — pas par ce seed.
- **`ApplicationStatusHistory`, variation du statut des candidatures** : hors périmètre, déjà exclu par `seed-demo-organizations-users-joboffers.md`, non rouvert ici.
- **Toute modification de `SeedDemoOrganizationsAsync`/`SeedDemoOrganizationUsersAsync`/`SeedDemoJobOffersAsync`/`SeedDemoApplicationsAsync`** : ce correctif ne touche que `SeedDemoCandidatesAsync`.
- **`EmployeeId`, `Department`, `HireDate`** : ces champs sont réservés aux utilisateurs d'organisation (`Organization.Admin`, `Manager`, etc.) et n'ont pas de sens pour un candidat (`OrganizationId == null`) ; non renseignés ici, cohérent avec le modèle (`CandidateDetailPage.vue` les masque déjà via `v-if` pour un candidat).
- **Migration EF Core** : aucune nécessaire, ce correctif ne modifie aucun schéma (contrairement à `configurable-salary-currency.md`), seulement les données de seed.
- **Tests automatisés** : cette spec définit des critères d'acceptation vérifiables ; l'écriture effective de tests (`XpertSphere.MonolithApi.Tests`) relève de l'agent `developer`.

### Découverte importante hors périmètre de cette spec — signalée à l'agent principal, pas traitée ici

**`recruiter-app/src/models/user.ts` (interface `Experience`, lignes 12-20) ne correspond pas au modèle backend `Experience`.** Le frontend `recruiter-app` attend `position`, `startDate`, `endDate`, `technologies` ; le backend expose (via `UserDto.Experiences: List<Experience>`, sérialisé tel quel — pas de `ExperienceDto` dédié pour cette liste embarquée) `title`, `company`, `date`, `isCurrent`, `description`, `location`. Vérifié : ni `userService.ts` ni `userStore.ts` (`recruiter-app`) ne transforment la réponse API entre les deux formes. Confirmé par ailleurs que `candidate-app` utilise, lui, les bons noms de champs (`title`/`date`/`company`), donc cette incohérence est propre à `recruiter-app`, préexistante à ce correctif.

**Conséquence concrète pour la démonstration** : une fois ce correctif implémenté, la section "Formations" de `CandidateDetailPage.vue` s'affichera correctement (les 3 champs utilisés, `training.field`/`training.school`/`training.period`, correspondent exactement au modèle `Training`). En revanche, la section "Expériences professionnelles" s'affichera de façon dégradée : `experience.company` et `experience.description` s'afficheront correctement, mais l'intitulé du poste (`experience.position`) restera vide et la période (`experience.startDate`/`experience.endDate`) ne s'affichera pas, faute de correspondance de nom de champ.

Ce correctif de seed **ne corrige pas** ce problème (il relève de `recruiter-app`, un package distinct avec son propre `.claude/specifications/`, et modifier une interface TypeScript + un template Vue dépasse le périmètre d'une spec de données de seed backend). **Traité désormais par une spec séparée** : voir `src/frontend/packages/recruiter-app/.claude/specifications/fix-experience-fields-candidate-detail-page.md`, qui corrige l'interface `Experience`/le template `CandidateDetailPage.vue`. Cette investigation a aussi mis au jour 11 composants `.vue` orphelins (jamais montés par aucune page) reposant sur le même type d'incohérence de nommage ; documentés pour mémoire dans cette même spec, mais volontairement non supprimés (décision de l'utilisateur, pour éviter tout risque).

## Critères d'acceptation

1. Sur une base neuve (premier démarrage) : les 4 candidats de démonstration ont chacun `PhoneNumber`, `LinkedInProfile`, `Skills`, `YearsOfExperience`, `DesiredSalary`, `DesiredSalaryCurrency`, `Availability` renseignés avec les valeurs du tableau §Champs scalaires, une `Address` complète correspondant au tableau §Adresse, 2 `Experience` et 1 `Training` correspondant à leur profil narratif (§Expériences et formation), et `ProfileCompletionPercentage = 91`.
2. Sur une base où les 4 candidats existent déjà avec un profil vide (créés par une version antérieure du seed, avant ce correctif) : un redémarrage de l'API applique rétroactivement l'enrichissement ci-dessus (mêmes valeurs, mêmes 91 %) — pas seulement sur une base neuve.
3. Un second démarrage consécutif (sans modification manuelle de la base entre-temps) ne crée **aucune** ligne `Experience`/`Training` supplémentaire (toujours exactement 2 `Experience` + 1 `Training` par candidat, jamais 4/2 ou plus), et `ProfileCompletionPercentage` reste `91` pour chacun (pas de régression vers une valeur plus basse au 2ᵉ boot).
4. Si un opérateur modifie manuellement, via `candidate-app` (`EditProfileDialog.vue`), un champ scalaire déjà renseigné par ce correctif (ex. `Skills`) pour l'un des 4 candidats, puis que l'API redémarre : la valeur modifiée manuellement n'est **pas** écrasée par la valeur de démonstration d'origine.
5. `CvPath` des 4 candidats reste `null` après exécution de ce correctif (aucune régression, aucun contournement de la contrainte utilisateur).
6. `GET /api/users/{id}` (ou l'équivalent déjà utilisé par `CandidateDetailPage.vue`/`userStore.fetchUserById`) pour chacun des 4 candidats retourne bien `experiences`/`trainings` non vides, `address` non vide, et les champs scalaires ci-dessus — vérifiable directement par un appel API authentifié en environnement Development.
7. `recruiter-app` : `CandidateDetailPage.vue` affiche, pour chacun des 4 candidats, la section "Formations" complète et correcte (`training.field`/`school`/`period` visibles), la carte "Informations personnelles"/"Informations professionnelles" avec téléphone/compétences/années d'expérience/salaire souhaité (devise correspondante)/disponibilité/LinkedIn tous visibles (plus de "Non spécifié"), la carte "Adresse" visible et complète, et la barre de complétude à 91 %. La section "Expériences professionnelles" s'affiche (le tableau `experiences` n'est plus vide, donc le bloc `v-if` s'active) mais avec le défaut déjà documenté en §Hors périmètre (intitulé de poste et dates non affichés, incohérence préexistante non corrigée par cette spec).

## Questions résiduelles / points `[À CONFIRMER]`

- **[À CONFIRMER] Mécanique exacte de persistance d'un `User` retrouvé via `FindByEmailAsync` puis modifié** (§Ordre d'implémentation, étape 5) : cette spec présume, par cohérence avec le reste du seeder, que `context`/`userManager` partagent le même `DbContext` de portée (comme documenté dans `seed-demo-organizations-users-joboffers.md`), rendant un `context.SaveChangesAsync()` final suffisant sans `UpdateAsync` explicite. Le développeur doit vérifier ce point empiriquement (ex. en relançant l'API deux fois de suite sur une base contenant déjà les candidats et en confirmant que les champs scalaires sont bien enrichis au second boot) et ajouter un `userManager.UpdateAsync(user)`/`context.Update(user)` explicite si l'observation contredit cette présomption. Ce point ne bloque pas le développement : une correction mineure suffirait si l'hypothèse s'avérait fausse.
- **Le signalement de l'incohérence `recruiter-app`/`Experience`** (§Hors périmètre) est désormais résolu : voir `fix-experience-fields-candidate-detail-page.md` côté `recruiter-app`, qui corrige ce point. Plus une question résiduelle.

## Fichiers à créer/modifier (récapitulatif)

- `Extensions/DatabaseExtensions.DemoData.cs` : modification de `SeedDemoCandidatesAsync` (enrichissement des champs scalaires, `Address`, `Experiences`, `Trainings`, appel à `CalculateProfileCompletion()`), et probablement extension de `DemoCandidateSeed` (nouveau record, ou records annexes `DemoCandidateExperienceSeed`/`DemoCandidateTrainingSeed`) pour porter les données listées dans cette spec, au choix du développeur pour la structure de données interne.
- Aucune migration EF Core, aucun changement de schéma.
- Aucun changement direct requis côté frontend pour ce correctif-ci (voir toutefois la découverte hors périmètre ci-dessus, qui pourrait motiver un correctif séparé côté `recruiter-app`).

## Coordination avec `seed-demo-organizations-users-joboffers.md`

Cette spec étend directement `seed-demo-organizations-users-joboffers.md`, qui listait explicitement "Expériences (`Experience`)/formations (`Training`) pour les 4 candidats : non demandées, non seedées" dans son §Hors périmètre — ce point est désormais couvert par la présente spec. `seed-demo-organizations-users-joboffers.md` a été mise à jour avec un renvoi vers ce fichier (voir sa section Hors périmètre).
