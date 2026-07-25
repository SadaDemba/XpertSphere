# Devise configurable par organisation, snapshot figé par offre, devise candidat auto-déclarée

## Contexte et constat de départ

Il existe aujourd'hui **deux champs de salaire distincts**, avec deux problèmes différents, déjà
diagnostiqués dans deux documents séparés :

1. **`User.DesiredSalary`** (`Models/User.cs:39`, salaire souhaité du candidat — pertinent
   uniquement quand `OrganizationId == null`, cf. `User.IsCandidate`) : `decimal?` sans **aucune**
   colonne de devise. Un commentaire de code fige la convention en dur ("toujours XOF, affiché
   FCFA côté UI"). Le correctif `fix-devise-salaire-souhaite-eur-vers-xof.md`
   (`candidate-app/.claude/specifications/`) a traité ce champ de façon **purement cosmétique**
   (changement de libellé affiché, sans migration ni colonne de devise) et exclut explicitement
   `JobOffer.SalaryCurrency` de son périmètre, renvoyant explicitement à *"un correctif séparé, à
   spécifier indépendamment si l'éditeur du produit le confirme"* — **c'est cette spec-ci qui
   répond à ce renvoi**, et qui va plus loin que ce correctif cosmétique en introduisant une vraie
   colonne de devise pour le candidat.
2. **`JobOffer.SalaryMin`/`SalaryMax`/`SalaryCurrency`** (`Models/JobOffer.cs:25-29`) :
   `SalaryCurrency` est un `string?` **libre** (`[MaxLength(10)]`), défaut `"EUR"`, choisi
   aujourd'hui librement par le recruteur à chaque offre (`CreateJobOfferDto.SalaryCurrency`,
   `UpdateJobOfferDto.SalaryCurrency`), sans aucune liste de devises contrôlée nulle part dans le
   monorepo (vérifié : aucun enum/liste de devises n'existe, ni backend ni frontend, avant cette
   spec).

Une troisième spec déjà écrite mais pas encore implémentée,
`seed-demo-organizations-users-joboffers.md`, seed 3 organisations de démo avec des devises **par
offre** (Meilleurtaux/Expertime en EUR, Dynaminqs en XOF) et contient déjà une section de
coordination anticipant cette spec-ci (voir §Coordination avec le seed).

## Décisions déjà validées (documentées telles quelles, non rediscutées)

### 1. Pas de conversion monétaire, affichage en libellé seul

Chaque enregistrement (offre d'emploi, salaire souhaité candidat) garde son propre montant et sa
propre devise, affichés tels quels partout où ils apparaissent. Aucun taux de change, aucune
normalisation vers une devise unique. Un candidat postulant chez deux entreprises à devises
différentes voit chaque offre dans sa devise d'origine, sans conversion.

### 2. Le candidat choisit sa propre devise pour son salaire souhaité

Nouvelle colonne de devise sur `User`, pertinente uniquement pour les candidats
(`OrganizationId == null`), choisie par le candidat à l'inscription et modifiable depuis son
profil, à côté du montant `DesiredSalary`.

- Nommage retenu : **`User.DesiredSalaryCurrency`** (et non un simple `Currency` générique) —
  cohérent avec `DesiredSalary`, sans ambiguïté avec une éventuelle future notion de "devise
  préférée" plus large pour un utilisateur, qui n'existe pas aujourd'hui.
- **Migration de backfill** : tous les comptes candidats existants (`OrganizationId IS NULL`)
  reçoivent `Currency.XOF` (valeur déjà assumée partout aujourd'hui, cf. le commentaire actuel sur
  `User.cs:37-38`). Les comptes d'organisation (`OrganizationId IS NOT NULL`) reçoivent `NULL` —
  ce champ n'a aucun sens pour eux et ne doit jamais être backfillé avec une valeur arbitraire.

### 3. La devise d'une organisation s'impose aux offres créées à partir du moment où elle est configurée — pas rétroactivement

Comportement exact validé, à respecter précisément : *"si on configure une devise en euros, on
crée 10 offres d'emploi, les 10 seront en euros. Si demain on change la devise en FCFA, les 10
anciennes offres ne doivent pas bouger, seules les nouvelles offres créées après le changement
seront en FCFA."*

Conséquences de conception :

- `Organization` gagne un champ **`Currency` nullable** (`Currency?`) — une organisation peut ne
  pas encore l'avoir configuré (en particulier toute organisation existante avant cette migration,
  et toute nouvelle organisation créée via `POST /api/organizations` par un `PlatformSuperAdmin`,
  qui ne connaît pas ce champ — voir §Hors périmètre, `CreateOrganizationDto` n'est volontairement
  pas modifié).
- `JobOffer.SalaryCurrency` reste un champ **stocké par offre**, jamais recalculé depuis
  l'organisation : c'est un **instantané figé au moment de la création de l'offre**, recopié
  automatiquement depuis `Organization.Currency` à cet instant précis par le service applicatif
  (`JobOfferService.CreateJobOfferAsync`), pas par AutoMapper ni par une valeur par défaut du
  modèle.
- **`SalaryCurrency` n'est plus un choix libre du recruteur** : retiré de `CreateJobOfferDto` et de
  `UpdateJobOfferDto` (voir §Comportement cible ci-dessous pour le détail par composant frontend).
  Une fois une offre créée, sa devise ne change plus jamais, y compris via `PUT`.
- Modifier la devise d'une organisation n'a **aucun effet rétroactif** sur les offres déjà créées :
  leur `SalaryCurrency` stocké ne change jamais après coup — conséquence directe du fait que
  `SalaryCurrency` est copié une seule fois, à la création, jamais relu depuis `Organization` par
  la suite.

**Décision de conception retenue pour la création d'offre côté formulaire (résolution du "ou" de
la consigne initiale)** : l'option retenue est de **retirer le sélecteur de devise du formulaire
de création**, sans le remplacer par un affichage en lecture seule de la devise de l'organisation
au moment de la création. Justification : afficher la devise de l'organisation *avant* création
nécessiterait un chemin de lecture accessible à tout recruteur/manager créant une offre (`Manager`,
`Recruiter`, pas seulement `Organization.Admin`), alors que le seul point de lecture introduit par
cette spec (`GET /api/organizations/me/currency`, voir §Nouveaux endpoints) est volontairement
scopé `Organization.Admin` uniquement (cf. décision 5). Ajouter un second chemin de lecture plus
large uniquement pour cet affichage cosmétique élargirait le périmètre d'autorisation sans
bénéfice fonctionnel réel : la devise réellement appliquée reste de toute façon visible **après**
création, sur l'offre elle-même (`JobOfferDto.SalaryCurrency`, déjà retourné par la réponse de
création et par tout affichage ultérieur de l'offre). Aucun nouvel endpoint de lecture n'est donc
introduit pour le seul bénéfice du formulaire de création.

**Décision validée par l'utilisateur** — comportement si `Organization.Currency` n'est pas encore
configuré au moment de la création d'une offre : **repli silencieux vers `Currency.XOF`** (pas de
blocage, et pas `EUR`). `JobOfferService.CreateJobOfferAsync` stampe `Currency.XOF` sur l'offre
créée lorsque `organization.Currency` est `null`, sans erreur de validation ni interruption du flux
de création. Une organisation qui n'a pas encore configuré sa devise peut donc immédiatement créer
des offres (comportement non bloquant, cohérent avec le fonctionnement actuel où la création
n'a jamais été conditionnée par un réglage préalable), avec `XOF` comme valeur de repli plutôt que
`EUR` — choix cohérent avec le contexte du produit (marché Sénégal/Afrique de l'Ouest) et avec la
convention déjà utilisée par ailleurs pour `User.DesiredSalaryCurrency` (décision 2, backfill des
candidats existants vers `XOF`). Une organisation dont la devise naturelle est bien `EUR`
(ex. Meilleurtaux/Expertime dans le seed de démo) doit donc configurer sa devise via
`PUT /api/organizations/me/currency` avant sa première offre si elle veut éviter que ses premières
offres soient stampées `XOF` par défaut — aucune offre existante n'est corrigée rétroactivement si
la devise est configurée après coup (cf. décision 3, immutabilité du snapshot).

### 4. Devise = liste contrôlée, pas texte libre

Nouvel enum **`Currency`** (`Enums/Currency.cs`), sur le modèle des enums existants
(`OrganizationSize`, `WorkMode`) :

```csharp
namespace XpertSphere.MonolithApi.Enums;

public enum Currency
{
    EUR,
    XOF
}
```

Liste volontairement minimale : `EUR` et `XOF` couvrent les deux devises déjà utilisées par le
seed de démonstration (`Meilleurtaux`/`Expertime` en EUR, `Dynaminqs` en XOF). Pas d'ajout
spéculatif d'autres devises (ex. USD, GBP) : rien dans le code existant ni dans les décisions
validées ne le justifie ; l'enum reste trivialement extensible plus tard si un besoin réel
apparaît.

Réutilisé pour les trois emplacements : `JobOffer.SalaryCurrency`, `Organization.Currency`,
`User.DesiredSalaryCurrency`.

**Convention de stockage EF Core** (suit exactement le pattern existant de
`Data/Configurations/JobOfferConfiguration.cs`/`OrganizationConfiguration.cs`, méthodes
`ToStringValue()`/`ToXxx()` de `Extensions/EnumExtensions.cs`) :

```csharp
// EnumExtensions.cs — nouvelles méthodes
public static string ToStringValue(this Currency currency) => currency switch
{
    Currency.EUR => "EUR",
    Currency.XOF => "XOF",
    _ => throw new ArgumentOutOfRangeException(nameof(currency), currency, null)
};

public static Currency ToCurrency(this string value) => value?.ToUpper() switch
{
    "EUR" => Currency.EUR,
    "XOF" => Currency.XOF,
    _ => throw new ArgumentException($"Invalid Currency value: {value}", nameof(value))
};
```

`ToCurrency(string)` **lève une exception pour toute valeur non reconnue**, exactement comme
`ToOrganizationSize`/`ToWorkMode`/`ToContractType` aujourd'hui — cohérence avec la convention
établie du projet, pas de comportement permissif ad hoc pour ce seul enum. Le risque réel (valeurs
historiques en base ne correspondant à aucun membre de l'enum) est traité **en amont, dans la
migration EF Core elle-même** (voir §Migrations, étape de normalisation SQL), pas en assouplissant
le convertisseur — de sorte qu'au moment où `HasConversion` avec ce convertisseur strict est
activé, la colonne ne contient plus que des valeurs garanties valides. Le convertisseur ne
change **pas** le type de colonne SQL sous-jacent (reste `nvarchar`, comme pour les autres enums
stockés en string) : c'est une conversion applicative EF Core, pas un changement de type de
colonne.

## Modèle de données

### `JobOffer.SalaryCurrency` : de `string?` (libre) à `Currency` (contrôlé, non nullable)

```csharp
// Avant
[MaxLength(10)] public string? SalaryCurrency { get; set; } = "EUR";

// Après
[Required] public Currency SalaryCurrency { get; set; }
```

Passage en **non-nullable** : après cette spec, `SalaryCurrency` est **toujours** renseigné à la
création (stampé depuis `Organization.Currency`, ou création bloquée si absente — voir décision 3
ci-dessus) ; il n'existe donc plus de cas légitime où il serait `null`. `[MaxLength(10)]` est
retiré (n'a plus de sens pour un enum converti en string par EF Core, la colonne reste bornée par
les seules valeurs de l'enum).

### `Organization.Currency` : nouveau champ nullable

```csharp
public Currency? Currency { get; set; }
```

### `User.DesiredSalaryCurrency` : nouveau champ nullable

```csharp
// Pertinent uniquement si IsCandidate (OrganizationId == null) ; toujours null pour un
// utilisateur d'organisation. Convention alignée sur DesiredSalary (montant annuel).
public Currency? DesiredSalaryCurrency { get; set; }
```

### Configuration EF Core (`Data/Configurations/`)

`JobOfferConfiguration.cs` — ajouter, à côté des conversions `WorkMode`/`ContractType`/`Status`
déjà présentes :

```csharp
builder.Property(jo => jo.SalaryCurrency)
    .HasConversion(
        v => v.ToStringValue(),
        v => v.ToCurrency())
    .HasMaxLength(10)
    .IsRequired();
```

`OrganizationConfiguration.cs` — ajouter, à côté de la conversion `Size` déjà présente :

```csharp
builder.Property(o => o.Currency)
    .HasConversion(
        v => v.HasValue ? v.Value.ToStringValue() : null,
        v => !string.IsNullOrEmpty(v) ? v.ToCurrency() : (Currency?)null)
    .HasMaxLength(10);
```

`UserConfiguration.cs` — nouvelle conversion, même pattern nullable que ci-dessus, sur
`u.DesiredSalaryCurrency`.

## Migrations EF Core (ordre à respecter strictement)

Une seule migration `dotnet ef migrations add AddConfigurableSalaryCurrency` peut porter
l'ensemble du changement de schéma, mais son contenu (`Up()`) doit respecter cet ordre logique
pour ne jamais faire passer une valeur non convertible par le nouveau convertisseur d'enum :

1. **Ajouter les nouvelles colonnes** `Organizations.Currency` (nvarchar, nullable) et
   `Users.DesiredSalaryCurrency` (nvarchar, nullable) — colonnes neuves, aucune donnée existante à
   transformer, `NULL` par défaut pour toutes les lignes.
2. **Normaliser les valeurs existantes de `JobOffers.SalaryCurrency`** *avant* de changer la
   contrainte de colonne, via `migrationBuilder.Sql(...)` :
   ```sql
   UPDATE JobOffers SET SalaryCurrency = 'EUR' WHERE SalaryCurrency IS NULL;
   UPDATE JobOffers SET SalaryCurrency = 'EUR' WHERE SalaryCurrency NOT IN ('EUR', 'XOF');
   ```
   Sur l'état actuel du code (seed pas encore implémenté, aucune donnée en Staging/Production
   vérifiée porteuse d'une valeur hors `"EUR"`), ce cas ne devrait concrètement affecter aucune
   ligne — cette étape est un filet de sécurité, pas une correction de données connues comme
   erronées. Repli vers `EUR` choisi par cohérence avec la valeur par défaut actuelle du modèle
   (`= "EUR"`).
3. **Rendre `JobOffers.SalaryCurrency` `NOT NULL`** (`ALTER COLUMN ... NOT NULL`), maintenant que
   l'étape 2 garantit l'absence de `NULL`.
4. **Backfill de `Users.DesiredSalaryCurrency`** :
   ```sql
   UPDATE Users SET DesiredSalaryCurrency = 'XOF' WHERE OrganizationId IS NULL;
   ```
   (Les lignes `OrganizationId IS NOT NULL` restent `NULL`, valeur déjà posée par défaut à l'étape
   1 — aucune instruction supplémentaire nécessaire pour elles.)

Aucun backfill n'est nécessaire pour `Organizations.Currency` : reste `NULL` pour toutes les
organisations existantes (comportement voulu — voir décision 3, une organisation existante n'a
"pas encore" configuré sa devise tant qu'un `Organization.Admin` ne l'a pas fait explicitement).

## Comportement applicatif — Backend

### `JobOfferService.CreateJobOfferAsync`

Après la résolution de l'utilisateur (`_context.Users.FirstOrDefaultAsync(...)`,
`Services/JobOfferService.cs:138-143`), avant `_mapper.Map<JobOffer>(...)` :

```csharp
var organization = await _context.Organizations.FirstOrDefaultAsync(o => o.Id == organizationId);
jobOffer.SalaryCurrency = organization?.Currency ?? Currency.XOF;
```

`SalaryCurrency` est retiré de `CreateJobOfferDto`/`UpdateJobOfferDto` (voir plus bas) : aucune
valeur envoyée par le client ne peut donc plus jamais écraser cette assignation côté service, y
compris via `UpdateJobOfferAsync` (`_mapper.Map(updateJobOfferDto, jobOffer)` ne touchera plus ce
champ puisqu'il n'existera plus sur le DTO source).

### DTOs backend à modifier

| DTO | Changement |
|---|---|
| `CreateJobOfferDto` | Retirer `SalaryCurrency` (n'est plus un choix du client). |
| `UpdateJobOfferDto` | Retirer `SalaryCurrency` (immuable après création). |
| `JobOfferDto` | `SalaryCurrency` passe de `string?` à `Currency` (non-nullable, reflète le modèle). |
| `OrganizationDto` | **Inchangé** — voir §Hors périmètre : `Organization.Currency` n'est pas exposé via les DTOs CRUD génériques, uniquement via le nouvel endpoint dédié `me/currency`. |
| `CreateOrganizationDto`, `UpdateOrganizationDto` | **Inchangés**, même raison. |
| `UserDto`, `UserProfileDto`, `UserSearchResultDto` | Ajouter `DesiredSalaryCurrency` (`Currency?`), à côté de `DesiredSalary` existant dans chacun des trois. |
| `RegisterCandidateDto` | Ajouter `DesiredSalaryCurrency` (`Currency?`), optionnel — voir note ci-dessous sur le repli si omis. |
| `UpdateUserProfileDto` | Ajouter `DesiredSalaryCurrency` (`Currency?`), optionnel, modifiable indépendamment du montant. |

Nouveaux DTOs dédiés (voir §Nouveaux endpoints) :

```csharp
// DTOs/Organization/OrganizationCurrencyDto.cs
public class OrganizationCurrencyDto
{
    public Currency? Currency { get; set; }
}

// DTOs/Organization/UpdateOrganizationCurrencyDto.cs
public class UpdateOrganizationCurrencyDto
{
    [Required] public Currency Currency { get; set; }
}
```

**Note sur le repli si `DesiredSalaryCurrency` est omis à l'inscription** : `RegisterCandidateDto`
et le formulaire d'inscription proposeront toujours une valeur pré-sélectionnée (voir
§Coordination frontend), mais pour tolérer un appel direct à l'API (Swagger, Postman, ancien
client non mis à jour) sans casser la création de compte, `AuthenticationService` doit appliquer
un repli explicite : si `registerDto.DesiredSalaryCurrency` est `null` **et** que
`registerDto.DesiredSalary` a une valeur, poser `Currency.XOF` par défaut (cohérent avec le
backfill de la décision 2). Si `DesiredSalary` est lui-même absent, `DesiredSalaryCurrency` reste
`null` (aucun salaire souhaité renseigné, la devise n'a pas de sens).

### Validators backend à modifier

- `Validators/JobOffer/CreateJobOfferDtoValidator.cs` : retirer la règle
  `RuleFor(x => x.SalaryCurrency)...` (lignes 39-41, propriété n'existe plus sur le DTO).
- `Validators/JobOffer/UpdateJobOfferDtoValidator.cs` : même retrait (lignes 43-45).
- Aucune règle `.IsInEnum()` supplémentaire n'est nécessaire pour `Currency` sur les DTOs qui le
  portent encore (`UpdateOrganizationCurrencyDto`, `RegisterCandidateDto`,
  `UpdateUserProfileDto`) : le convertisseur JSON global (`JsonStringEnumConverter`,
  `Program.cs:64`) rejette déjà toute chaîne non reconnue au moment de la désérialisation
  (réponse `400` automatique par le model binding), comme c'est déjà le cas aujourd'hui pour
  `WorkMode`/`ContractType` sans validateur `.IsInEnum()` dédié côté désérialisation (le
  `.IsInEnum()` existant sur `WorkMode`/`ContractType` dans `CreateJobOfferDtoValidator` couvre en
  réalité surtout le cas d'une valeur entière hors bornes, transmise via un client qui sérialise
  les enums en nombre plutôt qu'en chaîne — par cohérence, ajouter tout de même
  `RuleFor(x => x.Currency).IsInEnum()` sur `UpdateOrganizationCurrencyDto` uniquement, ce DTO
  n'ayant pas d'autre garde-fou de validation).

### AutoMapper (`Mappings/`)

- `JobOfferMappingProfile.cs` : **changement requis**, malgré l'intuition inverse. Les deux
  `CreateMap` existants (`CreateJobOfferDto -> JobOffer`, `UpdateJobOfferDto -> JobOffer`)
  `.Ignore()`ent déjà explicitement chaque membre destination absent du DTO source (`Id`,
  `Status`, `PublishedAt`, `OrganizationId`, `Organization`, etc.) — signe que la validation de
  configuration AutoMapper (`AssertConfigurationIsValid`) est probablement activée quelque part
  dans le projet (à vérifier par le développeur, ex. dans les tests ou au démarrage). Une fois
  `SalaryCurrency` retiré des deux DTOs, `JobOffer.SalaryCurrency` devient un membre destination
  non mappé de la même catégorie que `Id`/`Status` : sans `Ignore()` explicite, la validation de
  configuration échouerait (au démarrage ou dans les tests selon où elle est déclenchée). Ajouter
  donc, sur les deux `CreateMap` :
  ```csharp
  .ForMember(dest => dest.SalaryCurrency, opt => opt.Ignore())
  ```
  (le service positionne cette valeur explicitement après le mapping, voir
  `JobOfferService.CreateJobOfferAsync` ci-dessus — `UpdateJobOfferAsync` ne doit de toute façon
  jamais y toucher, cohérent avec l'immutabilité de ce champ après création).
- `UserMappingProfile.cs` : `DesiredSalaryCurrency` se mappe par convention de nommage (même nom
  des deux côtés) sur `User -> UserDto`, `User -> UserProfileDto`, `User -> UserSearchResultDto`,
  `RegisterCandidateDto -> User` (implicite dans `AuthenticationService`, voir
  `Services/AuthenticationService.cs:241` où `DesiredSalary = registerDto.DesiredSalary` est déjà
  assigné manuellement, pas via un `CreateMap` — ajouter
  `DesiredSalaryCurrency = registerDto.DesiredSalaryCurrency ?? (registerDto.DesiredSalary.HasValue ? Currency.XOF : null)`
  au même endroit, voir note de repli ci-dessus), et `UpdateUserProfileDto -> User` (mapping manuel
  existant dans `UserService.UpdateProfileAsync`, `Services/UserService.cs:921-922` — ajouter le
  même bloc conditionnel `if (dto.DesiredSalaryCurrency.HasValue) user.DesiredSalaryCurrency = dto.DesiredSalaryCurrency;`).
- `OrganizationMappingProfile.cs` : aucun changement — `Organization.Currency` n'est délibérément
  mappé par aucun `CreateMap` existant (voir §Hors périmètre).

## Nouveaux endpoints — paramètres de devise de l'organisation

### Décision d'autorisation

Réutiliser tel quel `PUT /api/organizations/{id}` (policy `OrganizationAccess`) n'est **pas**
adapté : cette policy autorise **tout** utilisateur dont l'`OrganizationId` correspond à
l'organisation ciblée, quel que soit son rôle (`Recruiter`, `Manager`, `TechnicalEvaluator`
compris) — trop large pour un réglage réservé à `Organization.Admin`.

Décision retenue : **nouvel endpoint self-scoped**, qui résout l'organisation depuis le claim de
l'appelant (`this.GetCurrentUserOrganizationId()`, `Extensions/ControllerExtensions.cs:145`)
plutôt que depuis un `{id}` d'URL — élimine tout risque d'IDOR par construction (aucun paramètre
d'organisation n'est jamais accepté depuis la requête). Protégé par la policy existante
`RequireOrganizationAdminRole` (`Extensions/SecurityExtensions.cs:466-467`, qui exige déjà
exactement le rôle `Organization.Admin`, ni plus large ni plus étroit — aucune nouvelle policy à
créer). Ce choix reste cohérent avec le pattern déjà en place dans le projet ("policy large +
validation fine au niveau service", cf. commentaires `CanCreateUsers`/`OrganizationIsolation`) :
ici la policy elle-même est déjà suffisamment précise (un seul rôle autorisé), donc aucune
validation supplémentaire au niveau service n'est requise au-delà de la résolution de
l'organisation depuis le claim.

### `GET /api/organizations/me/currency`

Nouveau, dans `Controllers/OrganizationsController.cs`, à la suite des actions existantes :

```csharp
[HttpGet("me/currency")]
[Authorize(Policy = "RequireOrganizationAdminRole")]
public async Task<ActionResult<OrganizationCurrencyDto>> GetMyOrganizationCurrency()
{
    var organizationId = this.GetCurrentUserOrganizationId();
    if (organizationId is null)
    {
        return Forbid();
    }

    var result = await organizationService.GetCurrencyAsync(organizationId.Value);
    return this.ToActionResult(result);
}
```

### `PUT /api/organizations/me/currency`

```csharp
[HttpPut("me/currency")]
[Authorize(Policy = "RequireOrganizationAdminRole")]
public async Task<ActionResult<OrganizationCurrencyDto>> UpdateMyOrganizationCurrency(
    UpdateOrganizationCurrencyDto dto)
{
    var organizationId = this.GetCurrentUserOrganizationId();
    if (organizationId is null)
    {
        return Forbid();
    }

    var result = await organizationService.UpdateCurrencyAsync(organizationId.Value, dto);
    return this.ToActionResult(result);
}
```

### `IOrganizationService` / `OrganizationService`

Deux nouvelles méthodes, sur le même modèle que `GetByIdAsync`/`UpdateAsync` existants :

```csharp
Task<ServiceResult<OrganizationCurrencyDto>> GetCurrencyAsync(Guid organizationId);
Task<ServiceResult<OrganizationCurrencyDto>> UpdateCurrencyAsync(Guid organizationId, UpdateOrganizationCurrencyDto dto);
```

`UpdateCurrencyAsync` ne modifie **que** `Organization.Currency` (pas les autres champs de
l'organisation) — ne réutilise pas `UpdateAsync(id, UpdateOrganizationDto)` existant, qui ne porte
d'ailleurs pas ce champ (voir §Hors périmètre).

Ces routes vivent sous `[Route("api/[controller]")]` déjà défini par
`OrganizationsController` — attention à l'ordre de résolution de route ASP.NET Core :
`me/currency` doit être déclaré de façon à ne pas entrer en conflit avec la route paramétrée
`{id:guid}` existante (`GetOrganization(Guid id)`). Le segment littéral `me` ne matche jamais la
contrainte `:guid`, donc aucun conflit de routage réel n'est attendu, mais le développeur doit
vérifier ce point en écrivant le code (test d'intégration recommandé : `GET /me/currency` ne doit
pas être intercepté par la route `{id:guid}`).

## Coordination frontend — `candidate-app`

- **`src/enums/Currency.ts`** (nouveau) : **volontairement un enum à valeurs `string`**, pas
  numérique comme `WorkMode`/`ContractType` — le backend sérialise `Currency` en JSON via
  `JsonStringEnumConverter` (`"EUR"`/`"XOF"`, jamais un indice numérique), et ce champ est
  purement déclaratif/d'affichage côté candidat (contrairement à `WorkMode`/`ContractType`, il
  n'existe aucune couche `convertXxx`/`convertJobOffer` côté `User` pour re-mapper un indice
  numérique reçu vers l'enum). Un enum numérique casserait silencieusement l'affichage (la valeur
  reçue `"XOF"` ne correspondrait à aucune clé numérique de l'enum). Définition retenue :
  ```ts
  export enum Currency {
    EUR = 'EUR',
    XOF = 'XOF',
  }

  export const currencyLabels: Record<Currency, string> = {
    [Currency.EUR]: 'Euro (EUR)',
    [Currency.XOF]: 'Franc CFA (XOF)',
  };

  export const currencyOptions = Object.entries(currencyLabels).map(([value, label]) => ({
    label,
    value: value as Currency,
  }));
  ```
  Une valeur `Currency` reçue du backend (`user.desiredSalaryCurrency`, `job.salaryCurrency`) est
  donc directement une chaîne affichable (`"EUR"`/`"XOF"`), sans conversion ni indexation
  supplémentaire.
- **`src/models/auth.ts`** : ajouter `desiredSalaryCurrency?: Currency` sur `User` et
  `RegisterCandidateDto` (ligne 71 et 121 respectivement, à côté de `desiredSalary`).
- **`src/components/register/MultiStepRegisterForm.vue`** (ligne ~155, étape "Informations
  professionnelles") : ajouter un `q-select` `formData.desiredSalaryCurrency` à côté du
  `q-input` `formData.desiredSalary`, options `Currency.EUR`/`Currency.XOF`, valeur
  pré-sélectionnée `Currency.XOF` par défaut (cohérent avec la convention actuelle et le
  backfill). Le libellé du champ `desiredSalary` (actuellement `"Salaire souhaité (FCFA/an)"`,
  déjà en dur suite au correctif cosmétique précédent) devient générique, ex.
  `"Salaire souhaité annuel"`, la devise étant désormais portée par le sélecteur adjacent et non
  plus par le libellé.
- **`src/components/EditProfileDialog.vue`** (ligne ~72) : même ajout de sélecteur à côté du champ
  montant existant (actuellement `"Salaire souhaité (FCFA)"`), même changement de libellé
  générique, pré-rempli avec `user.desiredSalaryCurrency` existant (pas de valeur par défaut
  imposée ici, contrairement à l'inscription : un profil existant a déjà une valeur backfillée).
- **`src/pages/ProfilePage.vue`** (fonction `formatSalary`, ligne ~114 et alentours) : remplacer la
  devise fixe (`currency: 'XOF'` en dur suite au correctif précédent) par
  `user.value?.desiredSalaryCurrency` réellement stocké, utilisé **directement** comme paramètre
  `currency` d'`Intl.NumberFormat` (`Currency` étant un enum à valeurs `string`, la valeur reçue
  est déjà `'EUR'`/`'XOF'`, aucune conversion/indexation nécessaire) : `currency:
  user.value?.desiredSalaryCurrency ?? 'XOF'` (repli uniquement pour le cas très transitoire d'un
  profil chargé avant que le champ ne soit renseigné), avec `currencyDisplay: 'code'` (conserver ce
  réglage déjà décidé dans le correctif précédent, pour un rendu déterministe indépendant de
  l'ICU).
- **`src/components/JobCard.vue`** (lignes 40-51) : aucun changement de mécanisme — `job.salaryCurrency`
  reste affiché tel quel ; garantie supplémentaire apportée par cette spec : la valeur vient
  désormais d'un enum contrôlé côté backend (sérialisé en chaîne `"EUR"`/`"XOF"` par
  `JsonStringEnumConverter`, identique en forme à l'actuel `string?` libre), donc aucun changement
  de template n'est requis ici. Remarque cosmétique hors périmètre, non traitée par cette spec :
  l'icône `euro` (ligne 41) reste fixe quelle que soit la devise réelle de l'offre — préexistant,
  non lié à cette spec.
- **`src/pages/JobDetailsPage.vue`** (ligne 162) : `{{ currentJobOffer.salaryCurrency || '€' }}`
  → retirer le repli `|| '€'` (`currentJobOffer.salaryCurrency` est désormais **toujours** défini
  côté backend, `JobOffer.SalaryCurrency` n'étant plus nullable) : `{{ currentJobOffer.salaryCurrency }} brut/an`.

## Coordination frontend — `recruiter-app`

### Nouvelle page "Paramètres de l'organisation" (décision 5)

- **`src/pages/admin/OrganizationSettingsPage.vue`** (nouveau) : formulaire minimal, un seul champ
  `q-select` pour la devise (`Currency.EUR`/`Currency.XOF`), pas de sélecteur d'organisation ni
  d'`id` dans l'URL — la page appelle `GET /api/organizations/me/currency` au montage et
  `PUT /api/organizations/me/currency` à la sauvegarde. Afficher un état "non configurée" explicite
  si `currency` est `null` (organisation n'ayant jamais configuré sa devise), avec un message
  cohérent avec la conséquence métier (tant qu'elle n'est pas configurée, toute nouvelle offre est
  créée avec `XOF` par défaut — voir décision 3).
- **`src/router/routes.ts`** : nouvelle route, ex. `admin/organization-settings`, gardée par
  **`organizationAdminGuard`** (`router/guards/roleGuard.ts`, déjà défini —
  `createRoleGuard({ requireOrganizationAdmin: true })`, non utilisé ailleurs dans les routes
  actuelles) — **pas** `adminSectionGuard` ni `platformAdminGuard`. Point d'attention : bien que la
  décision 5 dise "gardée comme `UsersPage`/`RolesPage`", ces deux pages utilisent
  `adminSectionGuard` (`[...PlatformRoles, OrganizationAdmin]`), ce qui **ne correspond pas** à
  l'autorisation backend de cette page précise : l'endpoint `me/currency` est protégé par
  `RequireOrganizationAdminRole`, qui n'autorise **que** le rôle `Organization.Admin` — un
  `PlatformAdmin`/`PlatformSuperAdmin` recevrait un `403` de l'API alors même qu'`adminSectionGuard`
  l'aurait laissé passer côté route. Utiliser `organizationAdminGuard` (garde déjà existante,
  jamais utilisée jusqu'ici) fait correspondre exactement la garde de route à l'autorisation
  backend, et respecte l'intention réelle de la décision 5 ("accessible à `Organization.Admin`,
  pas besoin d'être plateforme" pointait vers "ne pas utiliser `platformAdminGuard`", pas vers "
  inclure aussi les rôles plateforme").
- **`src/services/organizationService.ts`** : nouvelles méthodes
  `getMyOrganizationCurrency(): Promise<ResponseResult<OrganizationCurrencyDto> | null>` (`GET`
  `/me/currency`) et
  `updateMyOrganizationCurrency(dto: UpdateOrganizationCurrencyDto): Promise<ResponseResult<OrganizationCurrencyDto> | null>`
  (`PUT` `/me/currency`), sur le modèle des méthodes existantes de ce service.
- **`src/models/organization.ts`** : ajouter `OrganizationCurrencyDto`
  (`{ currency: Currency | null }`) et `UpdateOrganizationCurrencyDto` (`{ currency: Currency }`).
  Ne **pas** ajouter `currency` à `Organization`/`OrganizationDto`/`CreateOrganizationDto`/
  `UpdateOrganizationDto` existants — voir §Hors périmètre, ces types restent alignés avec le
  backend qui ne les modifie pas non plus.
- **`src/enums/Currency.ts`** (nouveau, même contenu que côté `candidate-app`, dupliqué par
  package comme c'est déjà le cas pour `WorkMode`/`ContractType`/`OrganizationSize`, chaque
  package frontend ayant sa propre copie de ces enums).

### Formulaires de création/édition d'offre

| Composant | Changement |
|---|---|
| `src/components/jobs/JobDialog.vue` (utilisé par `src/pages/jobs/JobsPage.vue`) | Aucun champ `salaryCurrency` n'est aujourd'hui exposé dans le template (vérifié : le script fixe `salaryCurrency: 'EUR'` en dur dans `JobFormData`, lignes 215/251/279/300/345/366, sans `q-input`/`q-select` correspondant). Retirer entièrement `salaryCurrency` de l'interface `JobFormData` et des objets `CreateJobOfferDto`/`UpdateJobOfferDto` construits dans `saveJob()` (lignes 335-347, 356-368) : le backend stampe désormais cette valeur, l'envoyer serait un champ mort silencieusement ignoré par la désérialisation (le DTO n'aura plus cette propriété). |
| `src/pages/admin/JobOffersPage.vue` | Le `q-input v-model="formData.salaryCurrency"` (lignes 284-291, libellé "Devise") est **retiré du formulaire de création** (`formData` typé `CreateJobOfferDto`, qui n'aura plus cette propriété). Pour l'édition (`editJobOffer`, ligne 500-516, qui réutilise le même `formData`), afficher à la place la devise de l'offre sélectionnée en lecture seule (ex. texte simple `{{ selectedJobOffer?.salaryCurrency }}` à côté des champs Min/Max, hors de `formData`/`UpdateJobOfferDto`), puisque `formData` ne porte plus `salaryCurrency` du tout. |
| `src/pages/jobs/JobOfferDetailPage.vue` | Le `q-input v-model="editedJob.salaryCurrency"` en mode édition (lignes 250-256) devient un texte en lecture seule (`{{ jobOffer.salaryCurrency }}`), à côté des champs Min/Max qui restent éditables ; `editedJob` (type `UpdateJobOfferDto`) n'a plus cette propriété. |

### Affichage salaire candidat

| Composant | Changement |
|---|---|
| `src/pages/candidates/CandidatesPage.vue` (colonne `desiredSalary`, lignes 197-205) | Le `format` d'une colonne Quasar `q-table` reçoit `(val, row)` — utiliser le second paramètre pour lire `row.desiredSalaryCurrency` plutôt que le suffixe `"FCFA"` fixe actuel (ligne 204) : `format: (val, row) => (val ? \`${Math.round(val).toLocaleString()} ${row.desiredSalaryCurrency ?? ''}\` : 'Non spécifié')` — `desiredSalaryCurrency` étant un enum à valeurs `string` (`'EUR'`/`'XOF'`), aucune conversion n'est nécessaire, la valeur reçue est déjà la chaîne à afficher. |
| `src/pages/candidates/CandidateDetailPage.vue` (lignes 211-217) | Remplacer le suffixe `"FCFA"` fixe par `candidate.desiredSalaryCurrency` réel, affiché tel quel (même raison : enum `string`, pas de conversion). |
| `src/models/user.ts` | Ajouter `desiredSalaryCurrency?: Currency` sur `UserSearchResultDto` (ligne 36), `UserDto` (ligne 71), `UpdateUserDto` (ligne 142) — ce dernier bien que non utilisé pour un candidat par le formulaire recruteur actuel (`CreateUserDto`/`UpdateUserDto` servent aux utilisateurs d'organisation), gardé aligné avec le DTO backend correspondant par cohérence de contrat. |
| `src/models/auth.ts` | Ajouter `desiredSalaryCurrency?: Currency` sur `User` (ligne 62), si ce fichier est également utilisé côté recruteur pour typer un candidat (à vérifier par le développeur selon l'usage réel — sinon ignorer ce fichier côté `recruiter-app`, il duplique potentiellement `models/user.ts`). |

## Coordination avec `seed-demo-organizations-users-joboffers.md`

Cette spec (`seed-demo-organizations-users-joboffers.md`) contient déjà une section de
coordination anticipant l'introduction du champ `Organization.Currency` (écrite après cette
spec-ci mais avant son implémentation). Une fois ce champ introduit par la présente spec, le seed
devra être ajusté pour définir explicitement `Organization.Currency` sur les 3 organisations de
démonstration avec les mêmes valeurs déjà cohérentes avec les offres seedées :
`Currency.EUR` pour Meilleurtaux et Expertime, `Currency.XOF` pour Dynaminqs. Ce fichier de seed
n'est **pas modifié par cette spec-ci** (l'utilisateur s'en charge séparément) — cette section sert
uniquement de rappel de dépendance croisée pour le `developer` qui implémenterait les deux specs
successivement.

## Hors périmètre

- **Conversion monétaire / taux de change** : explicitement exclue (décision 1). Aucune table de
  taux, aucune normalisation vers une devise pivot.
- **Page globale `admin/organizations` (`OrganizationsPage.vue`) et sa policy
  `platformAdminGuard`** : reste réservée aux admins plateforme pour la gestion globale des
  organisations (toutes organisations, tous champs). Non touchée par cette spec.
- **`CreateOrganizationDto`/`UpdateOrganizationDto`/`OrganizationDto` (endpoints CRUD génériques
  `POST`/`PUT /api/organizations/{id}`)** : ne portent pas `Currency`, volontairement. La
  configuration de devise passe exclusivement par le nouvel endpoint dédié `me/currency`, réservé
  à `Organization.Admin` — pas par la gestion CRUD générale réservée aux rôles plateforme. Un
  `PlatformSuperAdmin` créant une nouvelle organisation ne peut donc pas positionner sa devise à la
  création : elle reste `null` jusqu'à ce qu'un `Organization.Admin` de cette organisation la
  configure via la nouvelle page self-service. Si ce comportement doit changer (ex. un
  `PlatformSuperAdmin` doit pouvoir aussi configurer la devise depuis la page globale), ce serait
  une extension ultérieure hors du périmètre validé ici.
- **Affichage d'un indicateur de devise dans le formulaire de création d'offre avant
  soumission** : explicitement écarté (voir décision 3, justification détaillée) — la devise
  appliquée n'est visible qu'après création de l'offre.
- **Ajout d'autres devises que EUR/XOF** : non traité, pas de besoin identifié au-delà du jeu de
  données de démonstration existant.
- **Modification du fichier `seed-demo-organizations-users-joboffers.md`** : laissée à l'utilisateur
  (voir §Coordination).
- **Tests automatisés** : cette spec définit des critères d'acceptation vérifiables ; l'écriture
  effective de tests (`XpertSphere.MonolithApi.Tests`, tests frontend le cas échéant) relève de
  l'agent `developer`.
- **Icône `euro` fixe dans `JobCard.vue` (candidate-app)** : préexistante, non liée à la devise
  réelle de l'offre, non corrigée ici (remarque cosmétique mineure, hors périmètre des décisions
  validées).

## Critères d'acceptation vérifiables

1. `Enums/Currency.cs` existe avec exactement les membres `EUR`, `XOF` ; `EnumExtensions.cs`
   expose `ToStringValue(this Currency)` et `ToCurrency(this string)` suivant le même pattern que
   `OrganizationSize`/`WorkMode`.
2. `JobOffer.SalaryCurrency` est de type `Currency` (non-nullable) ; `Organization.Currency` et
   `User.DesiredSalaryCurrency` sont de type `Currency?`.
3. La migration EF Core normalise toute valeur historique de `JobOffers.SalaryCurrency` non
   présente dans `{EUR, XOF}` (ou `NULL`) vers `EUR` **avant** d'appliquer la contrainte
   `NOT NULL` et le convertisseur d'enum — vérifiable en relisant le script SQL généré
   (`Migrations/<timestamp>_AddConfigurableSalaryCurrency.cs`) et en confirmant l'ordre des
   opérations.
4. Après migration, tous les comptes candidats existants (`OrganizationId IS NULL`) ont
   `DesiredSalaryCurrency = XOF` ; tous les comptes d'organisation ont `DesiredSalaryCurrency = NULL`.
5. `POST /api/jobOffers` (création) : si l'organisation appelante n'a pas de `Currency` configurée,
   l'offre est tout de même créée, avec `SalaryCurrency = Currency.XOF` (repli, décision 3 —
   validée par l'utilisateur, aucun blocage).
6. `POST /api/jobOffers` avec une organisation dont `Currency = XOF` : l'offre créée a
   `SalaryCurrency = XOF`, quelle que soit la valeur éventuellement envoyée dans le corps de la
   requête (le champ n'existe plus sur `CreateJobOfferDto`, toute valeur envoyée par un client
   n'ayant pas mis à jour son contrat est silencieusement ignorée par la désérialisation, pas
   rejetée en erreur).
7. Changer `Organization.Currency` (via `PUT /api/organizations/me/currency`) après la création de
   plusieurs offres ne modifie **aucune** offre déjà créée (`SalaryCurrency` de chacune reste
   inchangé) ; seule une offre créée **après** le changement porte la nouvelle devise.
8. `GET /api/organizations/me/currency` : `200` pour un utilisateur `Organization.Admin`
   authentifié, retourne la devise de sa propre organisation (jamais un paramètre d'URL, aucune
   possibilité de cibler une autre organisation) ; `403` pour un `Recruiter`/`Manager`/
   `TechnicalEvaluator` de la même organisation ; `403` pour un `PlatformAdmin`/
   `PlatformSuperAdmin` n'ayant pas le rôle `Organization.Admin` (comportement de la policy
   `RequireOrganizationAdminRole`, réutilisée telle quelle) ; `401` sans authentification.
9. `PUT /api/organizations/me/currency` avec `{ "currency": "XOF" }` : `200`, la devise de
   l'organisation de l'appelant est mise à jour ; vérifiable en rappelant immédiatement
   `GET /api/organizations/me/currency`, qui retourne la nouvelle valeur.
10. `PUT /api/organizations/{id}` (endpoint générique existant, `UpdateOrganizationDto`) : envoyer
    un champ `currency` dans le corps n'a aucun effet (le DTO ne le porte pas) — non-régression du
    comportement existant sur les autres champs (`Name`, `Address`, etc., toujours modifiables via
    cet endpoint).
11. `recruiter-app` : la nouvelle page de paramètres de devise est accessible à un compte
    `Organization.Admin` et refusée (redirection `/unauthorized`) pour un compte `Recruiter` seul
    **et** pour un compte `PlatformAdmin`/`PlatformSuperAdmin` n'ayant pas le rôle
    `Organization.Admin`, cohérent avec `organizationAdminGuard` et avec le `403` retourné par
    `GET/PUT me/currency` (critère 8) — la garde de route et l'autorisation backend doivent
    concorder exactement, aucun rôle ne doit franchir l'une sans franchir l'autre.
    `admin/organizations` (page globale) reste inaccessible à `Organization.Admin` et continue de
    fonctionner à l'identique pour `PlatformAdmin`/`PlatformSuperAdmin`.
12. `recruiter-app` : le formulaire de création d'offre (`JobDialog.vue`, dialogue de
    `JobOffersPage.vue`) ne présente plus aucun champ de sélection de devise ; l'offre créée
    affiche ensuite, en lecture, la devise réellement stampée par le backend.
13. `recruiter-app` : en mode édition d'une offre existante (`JobOfferDetailPage.vue`,
    `JobOffersPage.vue`), la devise est affichée en lecture seule, jamais modifiable ; les champs
    Min/Max restent modifiables normalement.
14. `recruiter-app` : `CandidatesPage.vue` et `CandidateDetailPage.vue` affichent la devise réelle
    du candidat (`EUR` ou `XOF` selon la valeur stockée), pas un suffixe `FCFA` fixe.
15. `candidate-app` : `MultiStepRegisterForm.vue` et `EditProfileDialog.vue` proposent un
    sélecteur de devise à côté du champ `desiredSalary` ; un compte créé sans interaction avec ce
    sélecteur (valeur par défaut du formulaire) reçoit tout de même `XOF` côté backend (repli
    documenté).
16. `candidate-app` : `ProfilePage.vue` affiche la devise réellement stockée du candidat (pas une
    valeur `XOF` fixe indépendante du champ backend) ; `JobDetailsPage.vue` n'affiche plus jamais
    de repli `'€'` (le champ `salaryCurrency` d'une offre est toujours défini).
17. Aucune régression sur les champs non concernés par cette spec (`Location`, `WorkMode`,
    `ContractType`, `Title`, etc. d'une offre ; `Name`, `Address`, etc. d'une organisation) :
    toujours modifiables comme avant, aucun changement de comportement en dehors de la devise.

## Questions résiduelles / points marqués `[À CONFIRMER]`

Aucune question bloquante restante : le comportement de repli si `Organization.Currency` est
absente à la création d'une offre (décision 3) est tranché — repli silencieux vers `Currency.XOF`,
aucun blocage. Conséquence acceptée par l'utilisateur : une organisation dont la devise naturelle
est `EUR` doit configurer sa devise via la nouvelle page self-service avant sa première offre pour
éviter un stampage `XOF` par défaut ; aucune offre déjà créée n'est corrigée rétroactivement si la
devise est configurée après coup. Toutes les autres décisions de conception découlent directement
des choix déjà validés par l'utilisateur.

## Fichiers à créer/modifier (récapitulatif)

Backend (`XpertSphere.MonolithApi`) :
- `Enums/Currency.cs` (nouveau).
- `Extensions/EnumExtensions.cs` : `ToStringValue(this Currency)`, `ToCurrency(this string)`.
- `Models/JobOffer.cs` (`SalaryCurrency` → `Currency` non-nullable), `Models/Organization.cs`
  (+`Currency?`), `Models/User.cs` (+`DesiredSalaryCurrency`, mise à jour du commentaire existant
  ligne 37-38 qui n'est plus exact une fois la colonne introduite).
- `Data/Configurations/JobOfferConfiguration.cs`, `OrganizationConfiguration.cs`,
  `UserConfiguration.cs` : nouvelles conversions d'enum.
- Nouvelle migration EF Core (`Migrations/<timestamp>_AddConfigurableSalaryCurrency.cs`), avec le
  script SQL de normalisation/backfill décrit ci-dessus.
- `DTOs/JobOffer/CreateJobOfferDto.cs`, `UpdateJobOfferDto.cs` (retrait de `SalaryCurrency`),
  `JobOfferDto.cs` (type `Currency` non-nullable).
- `DTOs/Organization/OrganizationCurrencyDto.cs`, `UpdateOrganizationCurrencyDto.cs` (nouveaux).
- `DTOs/User/UserDto.cs`, `UserProfileDto.cs`, `UserSearchResultDto.cs`, `UpdateUserProfileDto.cs`,
  `DTOs/Auth/RegisterCandidateDto.cs` (+`DesiredSalaryCurrency`).
- `Validators/JobOffer/CreateJobOfferDtoValidator.cs`, `UpdateJobOfferDtoValidator.cs` (retrait
  règle `SalaryCurrency`).
- Nouveau validator `Validators/Organization/UpdateOrganizationCurrencyDtoValidator.cs`
  (`RuleFor(x => x.Currency).IsInEnum()`).
- `Interfaces/IOrganizationService.cs`, `Services/OrganizationService.cs` :
  `GetCurrencyAsync`/`UpdateCurrencyAsync`.
- `Services/JobOfferService.cs` (`CreateJobOfferAsync` : résolution + stamping de la devise, ou
  blocage).
- `Services/AuthenticationService.cs` (ligne ~241 : assignation `DesiredSalaryCurrency` avec repli).
- `Services/UserService.cs` (ligne ~921-922 : assignation `DesiredSalaryCurrency` sur update
  profil).
- `Controllers/OrganizationsController.cs` : `GetMyOrganizationCurrency`,
  `UpdateMyOrganizationCurrency`.
- `Mappings/UserMappingProfile.cs` : vérification du mapping par convention (pas de changement de
  code attendu si le nommage est identique des deux côtés).

Frontend `candidate-app` :
- `src/enums/Currency.ts` (nouveau).
- `src/models/auth.ts` (+`desiredSalaryCurrency` sur `User`, `RegisterCandidateDto`).
- `src/components/register/MultiStepRegisterForm.vue`, `src/components/EditProfileDialog.vue`
  (nouveau sélecteur de devise).
- `src/pages/ProfilePage.vue` (`formatSalary` utilise la devise réelle).
- `src/components/JobCard.vue` (aucun changement de template, juste garantie de type).
- `src/pages/JobDetailsPage.vue` (retrait du repli `'€'`).

Frontend `recruiter-app` :
- `src/enums/Currency.ts` (nouveau).
- `src/pages/admin/OrganizationSettingsPage.vue` (nouveau).
- `src/router/routes.ts` (nouvelle route `admin/organization-settings`, `organizationAdminGuard`).
- `src/services/organizationService.ts` (+`getMyOrganizationCurrency`,
  `updateMyOrganizationCurrency`).
- `src/models/organization.ts` (+`OrganizationCurrencyDto`, `UpdateOrganizationCurrencyDto`).
- `src/models/user.ts`, `src/models/auth.ts` (+`desiredSalaryCurrency`).
- `src/models/job.ts` (`salaryCurrency` reste `string` — vérifier que le type reflète toujours une
  chaîne, cohérent avec la sérialisation JSON de l'enum backend, aucun changement de type
  nécessaire côté TypeScript pour ce champ précis).
- `src/components/jobs/JobDialog.vue`, `src/pages/admin/JobOffersPage.vue`,
  `src/pages/jobs/JobOfferDetailPage.vue` (retrait du sélecteur de devise à la création, lecture
  seule à l'édition).
- `src/pages/candidates/CandidatesPage.vue`, `CandidateDetailPage.vue` (devise réelle au lieu de
  `FCFA` fixe).

Aucun changement dans `docker-compose.yml`, ni dans les autres services du monorepo
(`CommunicationService`, `ReportingService`, `IntegrationService`, `ResumeAnalyzer`).
