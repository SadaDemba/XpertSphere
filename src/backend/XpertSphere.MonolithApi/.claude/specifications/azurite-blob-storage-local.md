# Stockage de fichiers (CV/documents) en local via Azurite

## Contexte et objectif

Aujourd'hui, `AddBlobStorage()` (`Extensions/BlobStorageExtensions.cs`) est appelé sans condition d'environnement dans `Program.cs`, et instancie systématiquement un `BlobServiceClient` réel à partir de `ConnectionStrings:BlobStorage`. En Development, cette clé est définie dans `appsettings.Development.json` avec une valeur qui pointe vers un vrai compte Azure (`AccountName=xpertspheredev`) mais avec une clé placeholder (`AccountKey=your-storage-key`) : ce n'est pas juste "taper sur Azure en dev", c'est une valeur non fonctionnelle qui ne peut aboutir à aucun appel réussi aujourd'hui. Tout développement/test local touchant à l'upload/téléchargement/suppression de CV échoue donc actuellement, sauf à substituer manuellement une vraie connection string Azure.

Objectif : permettre de faire tourner ce stockage entièrement en local via **Azurite** (émulateur Azure Storage officiel, image Docker `mcr.microsoft.com/azure-storage/azurite`), **sans supprimer** la configuration Azure Storage réelle utilisée en Staging/Production. Les deux coexistent ; la bascule se fait uniquement par configuration (connection string), jamais par code — même principe que la spec `llm-provider-groq-azure.md` de ResumeAnalyzer.

Périmètre technique : `docker-compose.yml` (racine du monorepo), `appsettings.Development.json`, `.env.example`, `Extensions/BlobStorageExtensions.cs`, `Program.cs` (signature d'appel uniquement), et **`Services/ResumeService.cs`** — ce dernier est en périmètre à cause d'un bug de parsing d'URL détaillé ci-dessous, pas pour un changement de comportement fonctionnel.

**Précédence de configuration à connaître avant toute modification** : `Program.cs` appelle `Env.Load()` (DotNetEnv) tout au début, avant `WebApplication.CreateBuilder(args)`. Les valeurs d'un fichier `.env` local deviennent donc des variables d'environnement du processus, et le fournisseur de configuration "variables d'environnement" d'ASP.NET Core a une priorité **supérieure** à `appsettings.Development.json` dans la configuration fusionnée. `.env.example` (suivi par git, gabarit sans secret réel) contient déjà une ligne `ConnectionStrings__BlobStorage=DefaultEndpointsProtocol=https;AccountName=your-storage-account;AccountKey=your-key;EndpointSuffix=core.windows.net`. Conséquence directe : si le `.env` local d'un développeur (fichier non versionné, `.env` à la racine du service) définit déjà `ConnectionStrings__BlobStorage`, modifier `appsettings.Development.json` (§2) n'aura **aucun effet observable** — `configuration.GetConnectionString("BlobStorage")` renverra la valeur du `.env`, pas celle de `appsettings.Development.json`, et l'éventuel repli Development de l'étape 3 (§3) ne se déclenchera pas non plus puisque la résolution aura déjà réussi à l'étape 1. Tout développeur ayant déjà configuré un `.env` local avec une vraie valeur Azure doit la retirer (ou la remplacer par la connection string Azurite, §2) pour que la bascule prenne effet.

## Diagnostic : le stockage ne peut pas "juste" tourner en local sans corriger `ResumeService.cs`

`UploadResumeAsync` construit lui-même le nom du blob (`{userId}/resume_{timestamp}{ext}`) et retourne `blobClient.Uri.ToString()`. Les trois autres méthodes (`DeleteResumeAsync`, `DownloadResumeAsync`, `GetResumeMetadataAsync`) re-dérivent ce nom de blob à partir de l'URL stockée via :

```csharp
var uri = new Uri(resumePath);
var blobName = uri.Segments.Skip(2).Aggregate((a, b) => a + b); // Skip /container/
```

Ce `Skip(2)` part de l'hypothèse d'une URL Azure **virtual-hosted-style**, où le compte est dans le host et le premier segment de chemin est le container :
`https://xpertspheredev.blob.core.windows.net/resumes/{userId}/resume_x.pdf` → segments `["/", "resumes/", "{userId}/", "resume_x.pdf"]` → `Skip(2)` donne bien `{userId}/resume_x.pdf`.

Azurite (et le storage emulator historique) retourne des URLs **path-style**, où le compte est aussi un segment de chemin :
`http://127.0.0.1:10000/devstoreaccount1/resumes/{userId}/resume_x.pdf` → segments `["/", "devstoreaccount1/", "resumes/", "{userId}/", "resume_x.pdf"]` → `Skip(2)` donne `resumes/{userId}/resume_x.pdf` : le nom du container se retrouve inclus dans le nom de blob recherché **à l'intérieur** du container `resumes`, qui ne correspond à aucun blob existant.

Conséquence : avec Azurite, l'upload réussirait (il ne dépend pas de ce parsing) mais le téléchargement, la suppression et la lecture de métadonnées échoueraient systématiquement (404 silencieux côté `ServiceResult.NotFound`). Ce n'est donc pas un simple changement de configuration : `ResumeService.cs` doit être corrigé pour rester correct quel que soit le style d'URL.

**Correctif attendu** : remplacer le parsing manuel par `Azure.Storage.Blobs.BlobUriBuilder`, qui gère nativement les deux styles d'URL (il détecte si le host est une adresse IP/nom d'emulateur et ajuste l'extraction du nom de compte/container/blob en conséquence) :

```csharp
var blobName = new BlobUriBuilder(uri).BlobName;
```

à appliquer dans les 3 méthodes concernées (`DeleteResumeAsync`, `DownloadResumeAsync`, `GetResumeMetadataAsync`), en remplaçant les deux lignes actuelles (`uri.Segments.Skip(2)...`) par cet appel. Aucun autre comportement de `ResumeService` ne change (validation de fichier, container `resumes`, extensions autorisées, taille max, sanitization du nom de fichier : inchangés).

## Comportement cible

### 1. `docker-compose.yml` (racine du monorepo) — nouveau service `azurite`

Ajouter un service au même niveau que `sqlserver`/`redis`/`adminer`, rejoignant le réseau `xpertsphere-network` existant :

```yaml
  azurite:
    image: mcr.microsoft.com/azure-storage/azurite:latest
    container_name: xpertsphere-azurite
    command: "azurite-blob --blobHost 0.0.0.0 --blobPort 10000 --location /data"
    ports:
      - "10000:10000"
    volumes:
      - azurite_data:/data
    networks:
      - xpertsphere-network
    restart: unless-stopped
```

- `azurite-blob` (et non `azurite`) : ne démarre que le service Blob, seul utilisé par le MonolithApi (aucun consommateur de Queue/Table Storage identifié dans le code) — pas de ports 10001/10002 publiés.
- `--blobHost 0.0.0.0` est nécessaire : sans ce flag, Azurite n'écoute que sur l'interface loopback interne au conteneur et le port mappé côté host (`dotnet run` tournant hors conteneur, comme SQL Server/Redis aujourd'hui) ne reçoit rien.
- Volume nommé `azurite_data` à ajouter à la section `volumes:` du fichier (aux côtés de `sqlserver_data`/`redis_data`), pour persister les blobs entre redémarrages du conteneur.
- Pas de `depends_on` nécessaire : le MonolithApi n'est pas conteneurisé dans ce `docker-compose.yml` (il tourne via `dotnet run` sur le host, comme documenté dans `.claude/docs/architecture.md`).
- Pas de healthcheck ajouté (hors périmètre, voir plus bas).

### 2. Connection string Azurite — valeur exacte

Valeur canonique à utiliser (connection string bien connue et documentée par Microsoft pour Azurite/Storage Emulator, avec le compte de développement par défaut `devstoreaccount1`) :

```
DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;
```

- Cette forme explicite est préférée au raccourci `UseDevelopmentStorage=true` (également reconnu par le SDK mais résolu de façon interne/implicite) : elle est directement lisible dans `appsettings.Development.json`, ne dépend d'aucune résolution spéciale côté SDK, et pointe explicitement vers `127.0.0.1:10000`, cohérent avec le port publié par le service `azurite` ci-dessus.
- `appsettings.Development.json` : remplacer la valeur actuelle de `ConnectionStrings:BlobStorage` (`AccountName=xpertspheredev;...`, non fonctionnelle) par cette connection string Azurite.
- `.env.example` : remplacer la valeur placeholder de `ConnectionStrings__BlobStorage` (`AccountName=your-storage-account;...`) par la même connection string Azurite, pour que le gabarit versionné reflète la valeur locale par défaut plutôt qu'un exemple Azure — cohérent avec le fait qu'un `.env` local a la priorité sur `appsettings.Development.json` (voir encadré de précédence ci-dessus). Un développeur disposant déjà d'un `.env` local avec une vraie valeur Azure doit la mettre à jour manuellement ; cette spec ne peut pas modifier un fichier non versionné.
- `appsettings.json` (base), `appsettings.Staging.json`, `appsettings.Production.json` : **aucun changement**. `appsettings.json` ne définit pas de clé `BlobStorage` aujourd'hui ; Staging/Production ne sont pas concernés par cette spec (voir Rétrocompatibilité).

### 3. `Extensions/BlobStorageExtensions.cs` — comportement au démarrage

Ordre de résolution de la connection string, dans cet ordre (le premier trouvé gagne) :
1. `configuration.GetConnectionString("BlobStorage")` (inclut Key Vault en Staging/Production, et `appsettings.Development.json` en Development) — comportement actuel, inchangé.
2. Variable d'environnement `ConnectionStrings__BlobStorage` — comportement actuel, inchangé.
3. **[CONFIRMÉ]** Nouveau repli, uniquement si `IWebHostEnvironment.IsDevelopment()` est vrai : si les deux étapes précédentes n'ont rien trouvé, utiliser par défaut la connection string Azurite canonique (section 2 ci-dessus) plutôt que de lever une exception.
4. Si toujours vide (Staging/Production, ou Development avec le repli désactivé) : lever `InvalidOperationException` comme aujourd'hui.

Décision utilisateur : implémenter l'étape 3. Elle rapproche `AddBlobStorage()` du traitement déjà réservé à Key Vault/CORS (comportement explicitement différencié en Development), et rend le service utilisable "out of the box" après un simple `docker compose up` même si `appsettings.Development.json` a été localement modifié/supprimé.

La signature de la méthode change et l'appel dans `Program.cs` doit être mis à jour en conséquence :

```csharp
public static IServiceCollection AddBlobStorage(
    this IServiceCollection services,
    IConfiguration configuration,
    IWebHostEnvironment environment)
```
```csharp
// Program.cs
builder.Services.AddBlobStorage(builder.Configuration, builder.Environment);
```

Dans tous les cas (étape 3 retenue ou non) : **aucun changement** à la façon dont `BlobServiceClient` est construit (`new BlobServiceClient(connectionString)` reste valide pour une connection string Azurite comme pour une connection string Azure réelle — le comportement de bascule est entièrement porté par la valeur de configuration, pas par une branche de code différente selon l'environnement). Le constructeur de `BlobServiceClient` ne se connecte pas immédiatement : une chaîne syntaxiquement valide mais pointant vers un Azurite non démarré ne fait pas échouer le démarrage de l'application ; l'erreur ne surviendra qu'au premier appel réel (`CreateIfNotExistsAsync`, upload, etc.) dans `ResumeService`. Ce comportement est inchangé et hors périmètre d'un fail-fast réseau (voir Hors périmètre).

### 4. `Services/ResumeService.cs`

Voir section Diagnostic ci-dessus : remplacer le parsing manuel par blob (`uri.Segments.Skip(2).Aggregate(...)`) par `new BlobUriBuilder(uri).BlobName` dans `DeleteResumeAsync`, `DownloadResumeAsync`, `GetResumeMetadataAsync`. Le container reste résolu comme aujourd'hui via `_blobServiceClient.GetBlobContainerClient(_containerName)` (nom de container fixe `"resumes"`, pas dérivé de l'URL) : seul le nom de blob doit être recalculé via `BlobUriBuilder`.

Aucun changement sur `UploadResumeAsync` (la création du container via `CreateIfNotExistsAsync(PublicAccessType.None)` fonctionne à l'identique avec Azurite ; aucune étape d'initialisation de container n'est nécessaire côté `docker-compose.yml` ou au démarrage de l'application).

## Rétrocompatibilité avec Staging/Production

- `KeyVaultExtensions.GetSecretMappings` charge déjà `ConnectionStrings:BlobStorage` depuis Azure Key Vault en Staging/Production (`Extensions/KeyVaultExtensions.cs`, liste `mappings`) : **aucune modification requise** de ce fichier ni du coffre pour cette spec.
- `Program.cs` continue d'appeler `AddBlobStorage()` sans condition d'environnement (contrairement à Key Vault/Application Insights) : le stockage Blob reste une fonctionnalité "core", pas une fonctionnalité d'observabilité désactivable en Development. Seule la signature de l'appel change si l'étape 3 (§3) est retenue.
- Aucune variable/clé de configuration existante n'est renommée ou supprimée. Seule la valeur de `ConnectionStrings:BlobStorage` dans `appsettings.Development.json` change.
- Le correctif `BlobUriBuilder` dans `ResumeService.cs` (§4) est rétrocompatible avec les URLs Azure réelles existantes (virtual-hosted-style) : `BlobUriBuilder` gère les deux styles, aucun changement de comportement pour les CV déjà uploadés en Staging/Production.

## Fichiers à modifier (récapitulatif)

- `docker-compose.yml` (racine) : nouveau service `azurite`, nouveau volume `azurite_data`.
- `src/backend/XpertSphere.MonolithApi/appsettings.Development.json` : nouvelle valeur de `ConnectionStrings:BlobStorage`.
- `src/backend/XpertSphere.MonolithApi/.env.example` : nouvelle valeur de `ConnectionStrings__BlobStorage` (gabarit versionné uniquement ; un `.env` local existant doit être mis à jour manuellement par chaque développeur, voir encadré de précédence).
- `src/backend/XpertSphere.MonolithApi/Extensions/BlobStorageExtensions.cs` : ajout du repli Development de l'étape 3 (§3).
- `src/backend/XpertSphere.MonolithApi/Program.cs` : mise à jour de l'appel à `AddBlobStorage(...)` (nouvelle signature avec `IWebHostEnvironment`).
- `src/backend/XpertSphere.MonolithApi/Services/ResumeService.cs` : remplacement du parsing d'URL par `BlobUriBuilder` dans les 3 méthodes citées (§4). **Ce changement est requis indépendamment de la décision sur l'étape 3.**
- Aucun changement dans `appsettings.json`, `appsettings.Staging.json`, `appsettings.Production.json`, `KeyVaultExtensions.cs`.

## Critères d'acceptation / tests

1. `docker compose up azurite` (ou `docker compose up` complet) démarre un conteneur `xpertsphere-azurite` exposant le port `10000` sur le host, sans erreur.
2. Avec `ConnectionStrings:BlobStorage` positionnée sur la connection string Azurite canonique (§2) dans `appsettings.Development.json`, **et en l'absence de surcharge par un `.env` local ou une variable d'environnement `ConnectionStrings__BlobStorage`** (voir encadré de précédence en Contexte), `dotnet run` (environnement Development) démarre sans lever `InvalidOperationException` liée au Blob Storage. Si un `.env` local surcharge cette clé avec une vraie valeur Azure, ce critère ne peut être vérifié qu'après avoir aligné/retiré cette entrée du `.env`.
3. Cycle complet **upload → download → get-metadata → delete** d'un CV via `IResumeService` (test d'intégration ou manuel via un endpoint existant consommant `ResumeService`) réussit de bout en bout contre Azurite :
   - l'upload retourne une URL de la forme `http://127.0.0.1:10000/devstoreaccount1/resumes/{userId}/resume_....pdf` ;
   - le téléchargement à partir de cette URL retourne le contenu du fichier (pas de `NotFound`) ;
   - la récupération de métadonnées retourne le nom de fichier original et la date d'upload (pas de `NotFound`) ;
   - la suppression retourne un succès, et une tentative de téléchargement suivante retourne `NotFound`.
4. Le container `resumes` est créé automatiquement dans Azurite lors du premier upload, sans étape manuelle (`az storage container create` ou équivalent) — comportement déjà porté par `CreateIfNotExistsAsync` dans `ResumeService`, à vérifier non régressé.
5. Si la variable de configuration `ConnectionStrings:BlobStorage` est totalement absente en Staging/Production : le démarrage échoue avec le même message d'erreur qu'aujourd'hui (`InvalidOperationException: BlobStorage connection string is not configured...`) — non-régression du comportement fail-fast actuel, le repli de l'étape 3 étant réservé à Development.
6. Supprimer `ConnectionStrings:BlobStorage` de `appsettings.Development.json` puis démarrer en Development doit malgré tout permettre un upload/download réussi contre Azurite (valeur de repli de l'étape 3 appliquée), sans modification de code au moment du test.
7. Aucune régression sur `ResumeService` en environnement Staging/Production : une URL Azure réelle existante (virtual-hosted-style, ex. `https://xpertspheredev.blob.core.windows.net/resumes/{userId}/f.pdf`) doit continuer à être correctement parsée par `BlobUriBuilder` (test unitaire recommandé, ex. dans `XpertSphere.MonolithApi.Tests`, vérifiant que `new BlobUriBuilder(new Uri(url)).BlobName` retourne bien `{userId}/f.pdf` pour ce style d'URL et `{userId}/f.pdf` également pour l'équivalent Azurite path-style).

## Hors périmètre

- Toute modification du frontend, y compris la variable `VITE_STORAGE_BASE_URL` (`src/frontend/packages/recruiter-app` et `candidate-app`) : ce point relève du périmètre frontend, avec son propre cycle spec-writer/developer si un ajustement s'avère nécessaire. Les URLs retournées par `ResumeService` restent des URLs absolues et complètes ; en local elles contiendront `127.0.0.1:10000`, ce qui est attendu et non corrigé ici.
- Healthcheck Docker Compose sur le service `azurite` (`docker-compose.yml` n'en définit pas non plus aujourd'hui pour `redis`, seul `sqlserver` en a un) : pas ajouté par cohérence avec l'état actuel, sauf demande explicite ultérieure.
- Fail-fast réseau si Azurite n'est pas démarré au moment du premier appel Blob Storage (le `BlobServiceClient` ne se connecte qu'à l'usage, comme en production avec un vrai compte Azure injoignable) : comportement actuel inchangé, pas de vérification de connectivité ajoutée au démarrage de l'application.
- Support des services Queue/Table Storage d'Azurite : non utilisés par le code actuel, non ajoutés.
- Migration ou script d'import des CV déjà stockés sur le vrai compte Azure `xpertspheredev` vers Azurite : hors périmètre, ce sont deux stockages indépendants et non synchronisés.
- Toute modification des secrets/valeurs réelles dans Azure Key Vault (Staging/Production) : aucune action requise, déjà en place (voir Rétrocompatibilité).
