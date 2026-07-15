# Stack applicative complète via `docker compose up` (test end-to-end local)

## Contexte et objectif

Aujourd'hui, `docker-compose.yml` (racine du monorepo) ne démarre que l'infrastructure : `sqlserver`, `redis`, `adminer`, réseau `xpertsphere-network`, volumes `sqlserver_data`/`redis_data`. Aucun service applicatif n'y est déclaré : `XpertSphere.MonolithApi` se lance via `dotnet run` (profils `http`/`https` de `Properties/launchSettings.json`, ports `5001`/`7001`), `XpertSphere.ResumeAnalyzer` via `uvicorn app.main:app --reload --port 8000`, et les deux apps frontend via `npm run dev:recruiter`/`dev:candidate` (serveur de dev Quasar/Vite, port par défaut `9000`). C'est documenté explicitement dans `src/backend/XpertSphere.MonolithApi/.claude/docs/architecture.md` : *« aucun `docker-compose` n'est présent dans `src/backend/` »*.

Des Dockerfiles multi-stage existent déjà pour 7 services sous `docker/` (`docker/backend/monolith-api`, `docker/backend/resume-analyzer`, `docker/backend/communication-service`, `docker/backend/reporting-service`, `docker/backend/integration-service`, `docker/recruiter-app`, `docker/candidate-app`), mais ils ne sont utilisés aujourd'hui que par les workflows CI/CD (`.github/workflows/deploy-*-to-aca.yml`) pour construire et pousser des images vers `acrxpertspheredev.azurecr.io` (Azure Container Apps) — jamais via `docker compose` en local.

**Objectif de cette spécification** : faire en sorte qu'une seule commande `docker compose up` (depuis la racine du monorepo) démarre l'ensemble de la stack applicative fonctionnelle en local — infrastructure **et** les 4 services applicatifs listés ci-dessous — pour permettre un test end-to-end complet (inscription candidat avec upload de CV, analyse du CV, consultation côté recruteur, etc.) sans installation manuelle de chaque service. Ceci s'ajoute au fonctionnement actuel (`dotnet run` / `npm run dev` / `uvicorn`), qui doit rester possible et non dégradé (voir Rétrocompatibilité).

Cette fonctionnalité est **transverse** : elle ne relève d'aucun service en particulier (elle modifie `docker-compose.yml` et `.env.example` à la racine du monorepo, et s'appuie sans les modifier sur les Dockerfiles et le code déjà en place de 4 services). Elle vit donc dans `.claude/specifications/` à la racine du monorepo, conformément à la convention documentée dans le `CLAUDE.md` racine (section « Fonctionnalités transverses »).

## Périmètre

**Services inclus (4, exécutés comme conteneurs applicatifs)** :
- `XpertSphere.MonolithApi` (.NET 9, API métier — auth, users, organisations, offres, candidatures, stockage CV)
- `XpertSphere.ResumeAnalyzer` (Python/FastAPI, extraction/analyse de CV via LLM)
- `recruiter-app` (Vue.js/Quasar, interface recruteurs)
- `candidate-app` (Vue.js/Quasar, interface candidats)

**Explicitement exclus** : `XpertSphere.CommunicationService`, `XpertSphere.ReportingService`, `XpertSphere.IntegrationService`. Purs squelettes sans logique métier au moment de la rédaction ; leurs Dockerfiles existent sous `docker/backend/{communication-service,reporting-service,integration-service}/` mais ne sont pas ajoutés à `docker-compose.yml` par cette spec. Ils feront l'objet d'une spécification dédiée ultérieure quand une logique métier existera.

**Infrastructure déjà présente, non modifiée par cette spec** : `sqlserver`, `redis`, `adminer` (ports, identifiants, volumes, healthcheck existants inchangés). `redis` reste un conteneur d'infra non consommé par le code applicatif aujourd'hui (`Cache:Provider` vaut `"Memory"` dans `appsettings.Development.json` — vérifié dans le code, aucune classe ne construit de client Redis) : cette spec ne change pas ce comportement et n'active pas Redis.

## Prérequis : dépendance à la spécification Azurite

`XpertSphere.MonolithApi/.claude/specifications/azurite-blob-storage-local.md` ajoute un service `azurite` à `docker-compose.yml` (émulateur Azure Blob Storage) et un repli de configuration Development dans `BlobStorageExtensions.cs`. **Au moment de la rédaction de cette spec, ce travail existe uniquement sur la branche `feature/azurite-blob-storage-local` (non fusionnée dans `develop`)** : `docker-compose.yml` sur `develop` ne contient pas encore de service `azurite`, et `appsettings.Development.json` pointe encore vers un compte Azure réel non fonctionnel (`AccountName=xpertspheredev`, clé placeholder).

Sans stockage Blob fonctionnel, l'upload de CV (déclenché à l'inscription d'un candidat, `AuthController` → `AuthenticationService` → `UserService` → `ResumeService.UploadResumeAsync`) échoue systématiquement : un test end-to-end complet (incluant l'upload et l'analyse d'un CV) est impossible sans Azurite. **Cette spécification dépend donc de `azurite-blob-storage-local.md`** :

- Si cette dernière est déjà fusionnée dans `develop` au moment de l'implémentation : le service `azurite` existe déjà dans `docker-compose.yml`, réutiliser tel quel (voir ajustement de la connection string ci-dessous, §3.1).
- Si elle ne l'est pas encore : l'implémentation de cette spec doit d'abord ajouter le service `azurite` à `docker-compose.yml` exactement selon les termes de cette autre spécification (image, `command`, port `10000`, volume `azurite_data`), puis poursuivre avec le contenu ci-dessous. Ne pas dupliquer le raisonnement de cette spec ici — s'y référer.

Cette spec ne réécrit pas le diagnostic ni le correctif `BlobUriBuilder` de `ResumeService.cs` : ils sont repris tels que déjà spécifiés ailleurs, sans changement supplémentaire.

## Décisions structurantes

### 1. Navigateur → conteneurs : ports publiés sur `localhost`, jamais les noms de service Docker

Les deux apps frontend sont construites (`RUN npm run build:recruiter` / `build:candidate` dans leurs Dockerfiles respectifs) puis servies en statique via `npx serve dist/spa -l 3001` (recruiter) / `-l 3000` (candidate) — **pas** un serveur de dev Vite avec hot-reload, **pas** de nginx. C'est un build de production classique : les variables `VITE_*` (`VITE_WEB_API_BASE_URL`, `VITE_RESUME_ANALYZER_BASE_URL`, etc.) sont résolues par Vite **au moment du `npm run build`** (donc au moment du `docker build`, via les `ARG`/`ENV` déjà déclarés dans chaque Dockerfile) et **injectées en dur dans le bundle JS final**. Elles ne peuvent plus être changées à l'exécution du conteneur (`environment:` sur le service n'aurait aucun effet sur un bundle déjà construit).

Conséquence directe : le code JS exécuté **dans le navigateur** de l'utilisateur (hors du réseau Docker interne `xpertsphere-network`) doit appeler `MonolithApi`/`ResumeAnalyzer` via les **ports publiés sur l'hôte** (`http://localhost:<port>`), jamais via les noms de service Docker (`http://monolith-api:8080`), que le navigateur ne peut pas résoudre. Ces URLs sont donc fournies comme **arguments de build** (`build.args:` dans `docker-compose.yml`), pas comme variables d'environnement du conteneur au runtime.

Corollaire opérationnel : changer un port publié ou une URL d'API dans `.env` (racine) nécessite de **rebuilder** les images frontend (`docker compose up --build` ou `docker compose build recruiter-app candidate-app`), pas seulement un redémarrage des conteneurs. À documenter dans le README/aide de la commande (voir Critères d'acceptation).

À l'inverse, les 2 backends conteneurisés reçoivent leur configuration comme variables d'environnement de conteneur au runtime (`environment:`), lues soit directement par ASP.NET Core (fournisseur de configuration "variables d'environnement", priorité supérieure à `appsettings.{Environment}.json`), soit par `pydantic-settings` (FastAPI) — schéma déjà utilisé et documenté par `llm-provider-groq-azure.md` et `azurite-blob-storage-local.md`.

### 2. `.env` : un seul fichier racine pilote `docker-compose.yml` ; les `.env` par service restent inertes en conteneur

Chaque service backend a déjà son propre `.env`/`.env.example` (`src/backend/XpertSphere.MonolithApi/.env*`, `src/backend/XpertSphere.ResumeAnalyzer/.env*`), chargé respectivement par `DotNetEnv.Env.Load()` (`Program.cs`, tout début) et `python-dotenv.load_dotenv()` (`app/main.py`)/`pydantic-settings` (`env_file=".env"` dans `Settings.model_config`). **Ces fichiers `.env` par service sont explicitement exclus des contextes de build Docker** (`**/.env` dans `docker/backend/monolith-api/.dockerignore` et `docker/backend/resume-analyzer/.dockerignore`) : ils ne sont jamais copiés dans l'image, et aucun des deux appels (`Env.Load()`, `load_dotenv()`) ne trouve de fichier en conteneur — ils deviennent des no-op silencieux. Toute la configuration d'un conteneur backend doit donc arriver comme **vraies variables d'environnement du process conteneurisé**, injectées par `docker-compose.yml` (bloc `environment:`), jamais via un `env_file:` qui pointerait vers le `.env` d'un service (ce `.env` contient des valeurs pensées pour `dotnet run`/`uvicorn` sur l'hôte — ex. `Server=localhost,1433` — qui ne fonctionnent pas depuis un conteneur, voir §3).

Un fichier `.env.example` existe déjà à la **racine** du monorepo (`/.env.example`), avec des variables (`DB_SERVER`, `SQLSERVER_PORT`, `REDIS_PORT`, `ADMINER_PORT`, `API_PORT`, `AZURE_CLIENT_ID`, `JWT_SECRET`, ...) qui ne sont **actuellement référencées par aucun `${...}` dans `docker-compose.yml`** — un fichier gabarit resté sans effet depuis sa création. **Décision : réutiliser et étendre ce fichier racine plutôt que d'en créer un second.** Il devient la source unique de vérité pour paramétrer `docker-compose.yml` (ports publiés, arguments de build frontend, secrets injectés dans les conteneurs backend), via le mécanisme natif de Docker Compose (`.env` au même niveau que `docker-compose.yml`, substitution `${VAR}` dans le fichier compose lui-même). Ceci est indépendant et ne remplace pas les `.env` par service, qui continuent de servir exclusivement les usages non conteneurisés (`dotnet run`, `npm run dev`, `uvicorn`).

Les variables déjà présentes dans `.env.example` racine mais non reprises par cette spec (`DB_SERVER`, `DB_NAME`, `DB_USER`, `DB_PASSWORD`, `REDIS_HOST`, `REDIS_PASSWORD`, `SQLSERVER_PORT`, `REDIS_PORT`, `ADMINER_PORT`, `AZURE_CLIENT_ID`, `AZURE_CLIENT_SECRET`, `AZURE_TENANT_ID`, `JWT_SECRET`, `JWT_ISSUER`, `JWT_AUDIENCE`, `ENABLE_ENTRAID_IN_DEV`) restent **inchangées et toujours sans effet sur `docker-compose.yml`** (elles concernent l'infra `sqlserver`/`redis`/`adminer`, dont les valeurs restent en dur dans le fichier compose, ou une authentification Entra ID hors périmètre ici) : ni suppression ni câblage, pour ne pas élargir le périmètre. Seule `API_PORT` (déjà existante, `5000`) est réutilisée telle quelle pour `monolith-api` (voir §4).

### 3. Configuration runtime des conteneurs backend

#### 3.1 `monolith-api`

Le conteneur doit recevoir, en `environment:` :

| Variable conteneur | Valeur | Justification |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Development` | Le Dockerfile fixe `ENV ASPNETCORE_ENVIRONMENT=Production` dans l'image finale. Sans cette surcharge, `DatabaseExtensions.GetConnectionString` prendrait la branche `IsProduction()` (attend `ConnectionStrings:DefaultConnection:Production`, absente) et `KeyVaultExtensions.AddKeyVaultConfiguration` exigerait `KEY_VAULT_URL` → échec de démarrage immédiat. `Development` active aussi Swagger, CORS (voir plus bas), `EnableSensitiveDataLogging`. |
| `ConnectionStrings__DefaultConnection` | `Server=sqlserver,1433;Database=XpertSphereDb;User Id=sa;Password=XpertSphere123!;TrustServerCertificate=true;MultipleActiveResultSets=true` | La valeur de `appsettings.Development.json` (`Server=localhost,1433;...`) est inutilisable depuis un conteneur (`localhost` y désigne le conteneur lui-même, pas `sqlserver`). Le nom de service Docker `sqlserver` remplace `localhost` ; le mot de passe reprend celui déjà en dur dans `docker-compose.yml` pour le service `sqlserver` (`SA_PASSWORD: "XpertSphere123!"`), non modifié par cette spec. La variable d'environnement a une priorité supérieure à `appsettings.Development.json` dans la configuration ASP.NET Core fusionnée (même mécanisme de précédence que documenté dans `azurite-blob-storage-local.md` pour `ConnectionStrings__BlobStorage`) : aucune modification de code ni de `appsettings.Development.json` n'est nécessaire. |
| `ConnectionStrings__BlobStorage` | `DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://azurite:10000/devstoreaccount1;` | Reprend la connection string canonique Azurite définie par `azurite-blob-storage-local.md`, avec une différence assumée : le host est `azurite` (nom de service Docker), pas `127.0.0.1`. Cette autre spec suppose `MonolithApi` lancé via `dotnet run` sur l'hôte (`127.0.0.1:10000` = port publié d'Azurite atteint depuis l'hôte) ; ici `MonolithApi` tourne **dans** `xpertsphere-network`, où `127.0.0.1` désignerait le conteneur `monolith-api` lui-même (pas Azurite) — la résolution DNS interne par nom de service est nécessaire. Le correctif `BlobUriBuilder` (déjà prévu par cette autre spec, indépendant du host) reste valide avec ce host. |
| `Admin__Email` | `${ADMIN_EMAIL}` (racine `.env`) | Requis par `DatabaseExtensions.SeedPlatformSuperAdminAsync`, appelé sans condition d'environnement à chaque démarrage (`UseDatabaseAsync`) : son absence lève `InvalidOperationException` et empêche tout démarrage, conteneurisé ou non. |
| `Admin__Password` | `${ADMIN_PASSWORD}` (racine `.env`) | Idem. Doit respecter la politique de mot de passe Identity active en Development (`RequireDigit`, `RequireLowercase`, `RequireUppercase`, `RequireNonAlphanumeric`, longueur ≥ 6) — voir `.env.example` (§4) pour un exemple valide. |
| `Jwt__Key` | `${JWT_KEY}` (racine `.env`) | `SecurityExtensions.GetJwtKey` lit `Jwt:Key` puis, si absent (le cas en Development, `appsettings.Development.json` ne définit pas cette clé), retombe explicitement sur `Environment.GetEnvironmentVariable("JWT__KEY")` (nom exact, double underscore) — pas via le mécanisme générique de configuration. Utiliser ce nom exact. |
| `CORS__ALLOWED_ORIGINS` | `http://localhost:${CANDIDATE_APP_PORT},http://localhost:${RECRUITER_APP_PORT}` | `Program.cs` n'active CORS qu'en Development, à partir de cette variable (split sur `,`), avec `AllowCredentials()` : les origines doivent correspondre exactement aux ports publiés des conteneurs frontend (§4), sinon les appels XHR du navigateur vers l'API échouent en CORS malgré un backend fonctionnel. |

Pas de variable `USE_ENTRA_ID` définie (donc absente/`false`) : authentification JWT locale uniquement en local Docker, comme en `dotnet run` par défaut aujourd'hui — Entra ID hors périmètre de cette spec.

**Migrations et seed EF Core** : `UseDatabaseAsync()` (`Program.cs`) appelle déjà `context.Database.MigrateAsync()` puis le seed (organisation XpertSphere, rôles, `PlatformSuperAdmin`) **automatiquement et sans condition d'environnement**, à chaque démarrage de l'application — comportement déjà en place, inchangé par cette spec. Décision : ne rien ajouter (pas de script d'attente, pas de commande `dotnet ef database update` séparée) ; la résilience déjà présente sur le `DbContext` (`EnableRetryOnFailure(3, 10s, null)`) couvre une base encore en cours de démarrage. En complément, `depends_on: sqlserver: condition: service_healthy` (§4) réduit encore la fenêtre de course.

#### 3.2 `resume-analyzer`

| Variable conteneur | Valeur | Justification |
|---|---|---|
| `ENVIRONMENT` | `development` | Le Dockerfile fixe `ENV ENVIRONMENT=production`. `Settings.current_deployment`/`current_model_version` (`app/core/config.py`) sélectionnent un déploiement Azure OpenAI différent selon cette valeur (`_PROD` si `production`) : surcharger en `development` évite d'appeler par erreur un déploiement de production depuis un test local. `KEY_VAULT_URL` n'est volontairement pas défini : la condition `if self.ENVIRONMENT in ["production", "staging"] and self.KEY_VAULT_URL` reste fausse, Key Vault n'est jamais sollicité, cohérent avec un test 100% local. |
| `LLM_PROVIDER`, `GROQ_API_KEY`, `GROQ_MODEL`, `GROQ_BASE_URL`, `GROQ_TEMPERATURE`, `AZURE_OPENAI_ENDPOINT`, `AZURE_OPENAI_API_KEY`, `AZURE_OPENAI_API_VERSION`, `AZURE_OPENAI_TEMPERATURE` | Valeurs depuis la racine `.env`, transmises telles quelles | Voir `llm-provider-groq-azure.md` : bascule Groq/Azure OpenAI entièrement pilotée par `LLM_PROVIDER`. Aucun émulateur local pour un LLM : ces appels restent externes, le conteneur reçoit juste les bonnes variables. Le développeur doit fournir ses propres identifiants (Groq ou Azure OpenAI) dans son `.env` local ; aucune valeur par défaut fonctionnelle n'est possible ici (secret personnel). |

CORS : `app/main.py` configure `CORSMiddleware(allow_origins=["*"], ...)` sans condition d'environnement — aucune variable CORS à fournir, aucun ajustement nécessaire pour ce service.

**Vérifié : `MonolithApi` n'appelle jamais `ResumeAnalyzer` côté serveur.** Aucune référence à `ResumeAnalyzer`/port `8000`/`HttpClient` correspondant dans le code C# (`Services/`, `Extensions/`). L'appel d'analyse de CV part directement du navigateur : `candidate-app/src/services/authService.ts` poste le fichier vers `` `${settings.resumeAnalyzer.baseUrl}/api/extract/...` `` (donc `VITE_RESUME_ANALYZER_BASE_URL`) via `axios`, en requête XHR directe depuis le poste du candidat. C'est cohérent avec la décision §1 (navigateur → port publié `localhost`) : `resume-analyzer` n'a donc besoin d'aucune configuration réseau supplémentaire côté `monolith-api`, et `candidate-app` reste le seul consommateur de `VITE_RESUME_ANALYZER_BASE_URL` (`recruiter-app` définit la même variable dans ses `.env` par service mais aucun code ne l'utilise — appel absent du Dockerfile `recruiter-app` par cohérence, voir §4).

### 4. `docker-compose.yml` — services à ajouter

```yaml
  monolith-api:
    build:
      context: .
      dockerfile: docker/backend/monolith-api/Dockerfile
    container_name: xpertsphere-monolith-api
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ConnectionStrings__DefaultConnection: "Server=sqlserver,1433;Database=XpertSphereDb;User Id=sa;Password=XpertSphere123!;TrustServerCertificate=true;MultipleActiveResultSets=true"
      ConnectionStrings__BlobStorage: "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://azurite:10000/devstoreaccount1;"
      Admin__Email: ${ADMIN_EMAIL}
      Admin__Password: ${ADMIN_PASSWORD}
      Jwt__Key: ${JWT_KEY}
      CORS__ALLOWED_ORIGINS: "http://localhost:${CANDIDATE_APP_PORT:-3000},http://localhost:${RECRUITER_APP_PORT:-3001}"
    ports:
      - "${API_PORT:-5000}:8080"
    depends_on:
      sqlserver:
        condition: service_healthy
      azurite:
        condition: service_started
    networks:
      - xpertsphere-network
    restart: unless-stopped

  resume-analyzer:
    build:
      context: .
      dockerfile: docker/backend/resume-analyzer/Dockerfile
    container_name: xpertsphere-resume-analyzer
    environment:
      ENVIRONMENT: development
      LLM_PROVIDER: ${LLM_PROVIDER:-groq}
      GROQ_API_KEY: ${GROQ_API_KEY}
      GROQ_MODEL: ${GROQ_MODEL}
      GROQ_BASE_URL: ${GROQ_BASE_URL:-https://api.groq.com/openai/v1}
      GROQ_TEMPERATURE: ${GROQ_TEMPERATURE:-0.1}
      AZURE_OPENAI_ENDPOINT: ${AZURE_OPENAI_ENDPOINT}
      AZURE_OPENAI_API_KEY: ${AZURE_OPENAI_API_KEY}
      AZURE_OPENAI_API_VERSION: ${AZURE_OPENAI_API_VERSION:-2024-12-01-preview}
      AZURE_OPENAI_TEMPERATURE: ${AZURE_OPENAI_TEMPERATURE:-0.1}
    ports:
      - "${RESUME_ANALYZER_PORT:-8001}:8000"
    networks:
      - xpertsphere-network
    restart: unless-stopped

  recruiter-app:
    build:
      context: .
      dockerfile: docker/recruiter-app/Dockerfile
      args:
        VITE_APP_ENV: development
        VITE_AUTH_MODE: jwt
        VITE_WEB_API_BASE_URL: http://localhost:${API_PORT:-5000}
        VITE_APP_VERSION: docker-local
    container_name: xpertsphere-recruiter-app
    ports:
      - "${RECRUITER_APP_PORT:-3001}:3001"
    depends_on:
      monolith-api:
        condition: service_healthy
    networks:
      - xpertsphere-network
    restart: unless-stopped

  candidate-app:
    build:
      context: .
      dockerfile: docker/candidate-app/Dockerfile
      args:
        VITE_APP_ENV: development
        VITE_WEB_API_BASE_URL: http://localhost:${API_PORT:-5000}
        VITE_RESUME_ANALYZER_BASE_URL: http://localhost:${RESUME_ANALYZER_PORT:-8001}
        VITE_APP_VERSION: docker-local
    container_name: xpertsphere-candidate-app
    ports:
      - "${CANDIDATE_APP_PORT:-3000}:3000"
    depends_on:
      monolith-api:
        condition: service_healthy
      resume-analyzer:
        condition: service_healthy
    networks:
      - xpertsphere-network
    restart: unless-stopped
```

Points à noter sur ce contenu :

- `context: .` (racine du monorepo) pour les 4 services, cohérent avec les `COPY src/backend/...`/`COPY src/frontend/...` déjà écrits dans les Dockerfiles (chemins relatifs à la racine, pas au dossier `docker/`). `docker-compose.yml` étant lui-même à la racine, `context: .` désigne la racine du repo — aucune image ne fonctionnerait avec un autre `context`.
- `monolith-api` et `resume-analyzer` ont déjà un `HEALTHCHECK` défini dans leur Dockerfile (`curl -f http://localhost:8080/health` / `curl -f http://localhost:8000/api/health`) : réutilisé directement par `condition: service_healthy`, sans redéfinition dans `docker-compose.yml`.
- Aucun healthcheck pour `recruiter-app`/`candidate-app` (pas défini dans leurs Dockerfiles, pas ajouté ici — cohérent avec l'absence de healthcheck déjà observée pour `redis` dans ce fichier). Aucun autre service n'en dépend.
- `azurite` (§ Prérequis) : pas de healthcheck prévu par `azurite-blob-storage-local.md` ; `condition: service_started` (pas `service_healthy`, qui exigerait un healthcheck absent) suffit ici, l'accès au Blob Storage n'étant sollicité qu'au premier upload (`CreateIfNotExistsAsync` dans `ResumeService`), pas au démarrage de `monolith-api`.
- Ports par défaut choisis pour ne **pas** entrer en conflit avec l'usage non conteneurisé existant, permettant en théorie de faire tourner les deux en parallèle si besoin (`dotnet run` sur `5001`/`7001` + Docker sur `5000` ; `uvicorn` sur `8000` + Docker sur `8001` ; `quasar dev` sur `9000`/`9200` + Docker sur `3000`/`3001`) : `API_PORT` réutilise la valeur déjà présente (`5000`) dans le `.env.example` racine existant (jusqu'ici sans effet) ; `RESUME_ANALYZER_PORT` (`8001`), `RECRUITER_APP_PORT` (`3001`) et `CANDIDATE_APP_PORT` (`3000`) sont nouveaux. `3000`/`3001` correspondent aux ports déjà `EXPOSE`-és dans les Dockerfiles frontend respectifs (`docker/candidate-app/Dockerfile` : `EXPOSE 3000`, script `preview` → `serve dist/spa -l 3000` ; `docker/recruiter-app/Dockerfile` : `EXPOSE 3001`, `serve dist/spa -l 3001`) : aucun autre choix ne serait cohérent avec ce que le conteneur écoute réellement en interne.
- `ports:` mappe toujours `<port hôte>:<port interne réel>`, pas `<port hôte>:<port hôte>` — attention à ne pas confondre lors de l'implémentation (ex. `monolith-api` écoute sur `8080` en interne quel que soit `API_PORT`, cf. `ENV ASPNETCORE_URLS=http://+:8080` fixé dans le Dockerfile, non modifié).
- `VITE_APIM_SUBSCRIPTION_KEY` (déclaré comme `ARG`/`ENV` dans `docker/recruiter-app/Dockerfile` et `docker/candidate-app/Dockerfile`) et `VITE_AUTH_MODE` (déclaré uniquement côté `recruiter-app`) sont **volontairement omis** des `build.args` ci-dessus pour `VITE_APIM_SUBSCRIPTION_KEY` : cette clé sert à authentifier les appels passant par Azure API Management (chemin Staging/Production), non pertinent en mode `jwt` 100% local. `VITE_AUTH_MODE` reste fourni (`jwt`) car `recruiter-app` en a besoin quel que soit l'environnement. Ne pas interpréter cette omission comme un oubli.

## Fichiers à créer / modifier

- `docker-compose.yml` (racine) : ajout des 4 services ci-dessus (§4), et du service `azurite` + volume `azurite_data` s'ils ne sont pas déjà présents (§ Prérequis). Aucune modification de `sqlserver`/`redis`/`adminer`.
- `.env.example` (racine) : ajout d'un nouveau bloc de variables (voir ci-dessous), sans toucher aux variables existantes déjà présentes (même si actuellement sans effet, voir §2).

```dotenv
# --- Stack applicative complète (docker compose up) ---

# Ports publiés sur l'hôte pour les services applicatifs conteneurisés
# (API_PORT réutilise la variable déjà définie plus haut)
RESUME_ANALYZER_PORT=8001
RECRUITER_APP_PORT=3001
CANDIDATE_APP_PORT=3000

# Compte PlatformSuperAdmin seedé automatiquement au démarrage de monolith-api
# (voir DatabaseExtensions.SeedPlatformSuperAdminAsync ; l'absence de ces valeurs empêche le démarrage)
ADMIN_EMAIL=admin@xpertsphere.local
ADMIN_PASSWORD=DevAdmin1!

# Clé de signature JWT utilisée par monolith-api en mode conteneurisé
JWT_KEY=dev-only-jwt-signing-key-change-me-min-256-bits

# Fournisseur LLM pour resume-analyzer (voir llm-provider-groq-azure.md) — fournir vos propres identifiants
LLM_PROVIDER=groq
GROQ_API_KEY=your-groq-api-key
GROQ_MODEL=your-groq-model
GROQ_BASE_URL=https://api.groq.com/openai/v1
GROQ_TEMPERATURE=0.1
AZURE_OPENAI_ENDPOINT=
AZURE_OPENAI_API_KEY=
AZURE_OPENAI_API_VERSION=2024-12-01-preview
AZURE_OPENAI_TEMPERATURE=0.1
```

- `.claude/specifications/README.md` (racine, nouveau) : court index listant cette spec, sur le même modèle que celui déjà présent dans chaque service (ex. `src/backend/XpertSphere.MonolithApi/.claude/specifications/README.md`).
- `CLAUDE.md` (racine) : ajout d'une ligne listant cette spécification dans la section « Fonctionnalités transverses » (référence, pas de duplication de contenu).

**Aucune modification de code applicatif** (`Program.cs`, `Extensions/*.cs`, code Python, code Vue) et **aucune modification des Dockerfiles/`.dockerignore` existants** — tout le comportement ciblé passe par la configuration `docker-compose.yml`/`.env`, exploitant des mécanismes déjà en place (précédence de configuration ASP.NET Core, `pydantic-settings`, résolution `VITE_*` au build).

## Rétrocompatibilité

- `dotnet run` (profils `http`/`https`), `uvicorn app.main:app --reload --port 8000`, `npm run dev:recruiter`/`dev:candidate` continuent de fonctionner exactement comme aujourd'hui : aucun fichier `.env` par service, aucun `appsettings.*.json`, aucun code applicatif n'est modifié par cette spec.
- `docker-compose.yml` reste utilisable pour ne démarrer que l'infrastructure existante (`docker compose up sqlserver redis adminer`), comme avant l'ajout des 4 nouveaux services.
- Les workflows CI/CD (`deploy-*-to-aca.yml`) qui construisent les mêmes Dockerfiles pour Azure Container Apps ne sont pas modifiés et ne sont pas affectés (ils ne passent pas par `docker-compose.yml`).

## Hors périmètre

- `XpertSphere.CommunicationService`, `XpertSphere.ReportingService`, `XpertSphere.IntegrationService` : non ajoutés à `docker-compose.yml` (voir Périmètre).
- Activation de `redis` côté code applicatif (`Cache:Provider` reste `"Memory"`) : aucun changement, cette spec ne câble pas de client Redis qui n'existe pas dans le code aujourd'hui.
- Modification des Dockerfiles existants, y compris le comportement du multi-stage build de `docker/backend/monolith-api/Dockerfile` qui exécute `dotnet test` pendant `docker build` (stage `build`, avant `publish`) : ce comportement est hérité de l'usage CI/CD existant de ce Dockerfile et n'est pas modifié ici. Conséquence assumée : le premier `docker compose up` (ou tout `--build`) sur `monolith-api` exécute l'intégralité de la suite de tests `XpertSphere.MonolithApi.Tests`, ce qui allonge le temps de build et **fait échouer le build si un test échoue** — comportement à connaître, pas un défaut introduit par cette spec.
- Accès direct depuis le navigateur aux URLs de blob brutes (`CvPath` en base, champ `resumeUrl` exposé par les DTOs `Application`) : ces URLs contiendront `azurite:10000`, non résolvable par un navigateur classique. Non traité ici : à la vérification du code frontend, aucune fonctionnalité actuelle n'ouvre réellement cette URL (`ApplicationDetailDialog.vue`, fonction `viewResume()`, ouvre en réalité une route statique `/resume-viewer`, pas `resumeUrl` — fonctionnalité non implémentée aujourd'hui, indépendamment de Docker). Si cette fonctionnalité est implémentée plus tard, un ajustement (proxy de téléchargement via l'API plutôt qu'URL directe, ou variable dédiée type `VITE_STORAGE_BASE_URL`) devra faire l'objet de sa propre spécification.
- Authentification Entra ID en local Docker (`USE_ENTRA_ID`) : hors périmètre, JWT local uniquement.
- Toute modification du pipeline CI/CD (`.github/workflows/deploy-*-to-aca.yml`) ou du déploiement Azure Container Apps réel : ce fichier ne concerne que l'usage local via `docker compose`.
- Câblage des variables déjà présentes dans le `.env.example` racine mais actuellement sans effet sur `docker-compose.yml` (`DB_*`, `REDIS_*` hors `RESUME_ANALYZER_PORT` nouvellement ajouté, `AZURE_CLIENT_*`, `JWT_SECRET`/`JWT_ISSUER`/`JWT_AUDIENCE`, `ENABLE_ENTRAID_IN_DEV`) : laissées inchangées, voir §2.
- **[À CONFIRMER]** Le `.env.example` du service `XpertSphere.MonolithApi` lui-même (`src/backend/XpertSphere.MonolithApi/.env.example`, distinct du `.env.example` racine modifié par cette spec) définit `ADMIN_EMAIL`, `ADMIN_PASSWORD`, `ADMIN_FIRST_NAME`, `ADMIN_LAST_NAME` et `JWT_KEY` (simple underscore). Ces noms ne correspondent pas à ce que le code lit réellement : `DatabaseExtensions.SeedPlatformSuperAdminAsync` attend `Admin:Email`/`Admin:Password` (donc, via la convention de mapping des variables d'environnement .NET, `Admin__Email`/`Admin__Password`, double underscore) et `SecurityExtensions.GetJwtKey` lit littéralement `JWT__KEY` ; `ADMIN_FIRST_NAME`/`ADMIN_LAST_NAME` ne sont lus par aucun code (le prénom/nom du seed viennent de `Seeding:PlatformSuperAdmin` dans `appsettings.json`, valeurs fixes `"Super"`/`"Admin"`). Un `dotnet run` local suivant ce gabarit tel quel échouerait donc dès le seed (`Admin:Email` introuvable), **indépendamment de Docker**. Cette spec n'en dépend pas techniquement (voir §2 : les `.env` par service sont inertes en conteneur, la configuration de `monolith-api` conteneurisé passe uniquement par `docker-compose.yml`/`.env` racine, avec les bons noms `Admin__Email`/`Admin__Password`/`JWT__KEY`) mais le signale car découvert pendant l'exploration : à confirmer avec l'utilisateur si une correction de ce gabarit (`src/backend/XpertSphere.MonolithApi/.env.example`) doit être faite dans le cadre de cette spec, dans une spec dédiée, ou laissée en l'état.

## Critères d'acceptation

1. Depuis un clone propre du repo sur `develop`, après avoir copié `.env.example` (racine) vers `.env` et renseigné au minimum `ADMIN_EMAIL`/`ADMIN_PASSWORD`/`JWT_KEY` et un jeu de clés LLM valide (Groq ou Azure OpenAI), `docker compose up --build` démarre sans erreur les 8 services (`sqlserver`, `redis`, `adminer`, `azurite`, `monolith-api`, `resume-analyzer`, `recruiter-app`, `candidate-app`).
2. `docker compose ps` montre `monolith-api` et `resume-analyzer` en état `healthy` (pas seulement `running`) après un délai raisonnable (`start_period` des healthchecks déjà définis dans leurs Dockerfiles).
3. `curl http://localhost:5000/health` (ou le port choisi via `API_PORT`) répond `200` **sans redirection vers HTTPS** (pas de code `307`/`308`) — le conteneur ne sert que du HTTP (`ASPNETCORE_URLS=http://+:8080`), `app.UseHttpsRedirection()` ne doit pas casser les appels du navigateur faute de port HTTPS configuré.
4. `curl http://localhost:8001/api/health` (ou le port choisi via `RESUME_ANALYZER_PORT`) répond `200`.
5. `http://localhost:3001` (recruiter-app) et `http://localhost:3000` (candidate-app) sont accessibles dans un navigateur.
6. Depuis `candidate-app` dans le navigateur : un scénario d'inscription candidat avec upload d'un CV (PDF) réussit de bout en bout — l'appel réseau visible dans les DevTools cible `http://localhost:5000/...` (jamais `http://monolith-api:...`), reçoit un `200`/`201` sans erreur CORS, et le compte est créé (vérifiable via `adminer` sur la base `XpertSphereDb`, table `Users`).
7. Le compte `PlatformSuperAdmin` seedé (email/mot de passe = `ADMIN_EMAIL`/`ADMIN_PASSWORD` du `.env`) permet une connexion réussie via `recruiter-app`.
8. Un CV uploadé via `candidate-app` peut être soumis à l'analyse via `resume-analyzer` (appel visible ciblant `http://localhost:8001/...`) et retourne des données structurées (identité, expériences, formations, compétences).
9. Arrêter puis relancer la stack (`docker compose down` puis `docker compose up`, sans `-v`) conserve les données (base SQL Server, blobs Azurite) grâce aux volumes nommés déjà en place/ajoutés — pas de re-seed en doublon (le seed vérifie déjà l'existence avant insertion, comportement inchangé).
10. `docker compose up sqlserver redis adminer` (sans les 4 nouveaux services) démarre toujours correctement seul, comme avant cette spec — non-régression de l'usage infra-seulement.
11. `dotnet run` (`XpertSphere.MonolithApi`), `uvicorn app.main:app --reload --port 8000` (`XpertSphere.ResumeAnalyzer`) et `npm run dev:recruiter`/`dev:candidate` continuent de fonctionner sans changement après l'implémentation de cette spec — non-régression du flux de développement non conteneurisé.
