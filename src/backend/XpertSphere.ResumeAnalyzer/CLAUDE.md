# Instructions Claude — XpertSphere.ResumeAnalyzer

## Aperçu du service

XpertSphere.ResumeAnalyzer est un microservice Python (FastAPI), distinct des services .NET du monorepo XpertSphere (plateforme de recrutement pour le marché Sénégal / Afrique de l'Ouest). Son rôle : recevoir un CV (fichier PDF) envoyé par les autres services de la plateforme, en extraire le texte brut, puis appeler un LLM pour transformer ce texte en données structurées (identité, coordonnées, formations, expériences, compétences, langues).

Le service expose une API HTTP minimale :

- `POST /api/extract/` : reçoit un fichier CV et retourne les données extraites sous forme de JSON (`CVModel`).
- `GET /api/health`, `/api/health/detailed`, `/api/health/readiness`, `/api/health/liveness` : endpoints de supervision, pensés pour un déploiement sur Azure Container Apps.
- `GET /docs` et `/redoc` : documentation Swagger/ReDoc générée par FastAPI.

## Stack technique

- **Framework** : FastAPI (`fastapi`, `uvicorn`)
- **Extraction PDF** : `pdfplumber`
- **Client LLM** : SDK `openai` (classe `AzureOpenAI` actuellement utilisée, voir section « Fournisseur LLM » plus bas)
- **Configuration** : `pydantic-settings` (`app/core/config.py`), avec chargement optionnel des secrets depuis Azure Key Vault (`azure-keyvault-secrets`, `azure-identity`) en environnement `production`/`staging`
- **Tests** : `pytest`, `pytest-asyncio`, `pytest-mock`, `httpx`
- **Python** : géré via un environnement virtuel `.venv` (présent localement, non versionné)

## Structure du projet

```
app/
├── main.py                          # Point d'entrée FastAPI, CORS, gestion des exceptions
├── api/
│   ├── router.py                    # Agrège les routers (health, resume)
│   ├── dependencies.py              # Injection de dépendances (get_cv_service)
│   └── endpoints/
│       ├── health.py                # Endpoints de supervision
│       └── resume.py                # Endpoint POST /extract/
├── core/
│   ├── config.py                    # Settings (pydantic-settings) + intégration Azure Key Vault
│   └── exceptions.py                # Exceptions applicatives (ExtractionError, AnalysisError, ValidationError)
├── domain/
│   ├── interfaces/                  # Abstractions : DocumentExtractor, TextAnalyzer
│   └── models/resume.py             # CVModel, Experience, Training (dataclasses)
├── infrastructure/
│   ├── analyzers/
│   │   ├── base_analyzer.py         # BaseAnalyzer (implémente TextAnalyzer)
│   │   └── openai_analyzer.py       # OpenAIAnalyzer : implémentation concrète via Azure OpenAI
│   ├── extractors/
│   │   ├── base_extractor.py        # BaseExtractor (implémente DocumentExtractor)
│   │   └── pdf_extractor.py         # PDFExtractor : implémentation concrète via pdfplumber
│   └── schema.py                    # UserModel/Experience/Training : dataclasses proches de domain/models/resume.py
├── services/
│   └── cv_service.py                # CVService : orchestre extraction + analyse
└── utils/
    ├── openapi_utils.py              # get_llm() : construit le client AzureOpenAI depuis settings
    └── pdf_utils.py                   # extract_text_from_pdf() : utilitaire d'extraction autonome

tests/
├── conftest.py                       # Variables d'environnement de test (valeurs factices)
├── test_models.py
├── test_pdf_extractor.py
├── test_openai_analyzer.py
└── test_api.py
```

Point d'attention : `app/infrastructure/schema.py` définit un `UserModel` proche du `CVModel` de `app/domain/models/resume.py`, mais aucune référence à ce fichier n'a été trouvée ailleurs dans le code lors de cette exploration. À vérifier avant de s'appuyer dessus ou de le supprimer.

## Commandes

Activer l'environnement virtuel (déjà présent en `.venv`) :

```bash
source .venv/bin/activate
```

Installer les dépendances :

```bash
pip install -r requirements.txt
```

Lancer le serveur de développement (rechargement automatique, port 8000) :

```bash
uvicorn app.main:app --reload --port 8000
```

API disponible sur `http://localhost:8000` (`/docs` pour Swagger UI, `/redoc` pour ReDoc).

Lancer les tests :

```bash
pytest                              # tous les tests
pytest tests/test_models.py         # un fichier ciblé
pytest -v                           # sortie détaillée
```

La configuration pytest (`pyproject.toml`) fixe `testpaths = ["tests"]` et ajoute la racine du projet au `pythonpath`. Les tests mockent systématiquement les appels externes (API OpenAI, extraction PDF) pour éviter coûts et dépendances réseau ; `tests/conftest.py` définit des variables d'environnement Azure OpenAI factices avant l'exécution.

## Conventions de code observées

- Classes et fonctions documentées par des docstrings de style Args/Returns/Raises.
- Utilisation systématique de `logging` par module (`self.logger = logging.getLogger(self.__class__.__name__)` dans les classes de base).
- Exceptions applicatives dédiées (`ExtractionError`, `AnalysisError`, `ValidationError`) héritant de `BaseApplicationError`, capturées au niveau du service (`CVService`) et traduites en `HTTPException` avec codes 400/422/500, ou gérées globalement via un handler FastAPI (`main.py`) qui renvoie un 422.
- Modèles de domaine en `dataclass` (pas de modèles Pydantic pour `CVModel`/`Experience`/`Training`), avec validation légère dans `__post_init__` (strip des chaînes, normalisation de l'email en minuscules).
- Séparation stricte interface (`app/domain/interfaces/`) / implémentation (`app/infrastructure/`), avec une classe de base commune par famille (`BaseAnalyzer`, `BaseExtractor`) qui porte le logger et, pour les extracteurs, la logique générique de `can_extract`.

## Architecture : pattern interface/adaptateur

Le service applique une architecture hexagonale légère pour isoler le domaine métier des fournisseurs externes :

- `DocumentExtractor` (interface, `app/domain/interfaces/document_extractor.py`) définit le contrat d'extraction (`can_extract`, `extract_text`). `PDFExtractor` en est aujourd'hui la seule implémentation concrète (via `pdfplumber`), héritant de `BaseExtractor`.
- `TextAnalyzer` (interface, `app/domain/interfaces/text_analyzer.py`) définit le contrat d'analyse (`analyze`). `OpenAIAnalyzer` en est aujourd'hui la seule implémentation, héritant de `BaseAnalyzer`.
- `CVService` (`app/services/cv_service.py`) orchestre le flux : il reçoit une liste d'extracteurs et un analyseur en injection de dépendances (voir `app/api/dependencies.py`), sélectionne l'extracteur adapté au type de fichier, puis délègue l'analyse du texte extrait.

Cette séparation permet, en théorie, d'ajouter un nouvel extracteur (DOCX, image scannée, etc.) ou un nouvel analyseur (autre fournisseur LLM) sans modifier `CVService` ni les endpoints, à condition d'implémenter l'interface correspondante et de l'injecter dans `get_cv_service()`.

## Point d'attention : fournisseur LLM en cours de changement

Le fournisseur LLM actuellement câblé dans le code est **Azure OpenAI** :

- Configuration dans `app/core/config.py` (classe `Settings`) : `AZURE_OPENAI_ENDPOINT`, `AZURE_OPENAI_API_KEY`, `AZURE_OPENAI_API_VERSION`, déploiements distincts pour `gpt-35-turbo` (dev/staging) et `gpt-4o-mini` (production) via `current_deployment`/`current_model_version`.
- Client construit dans `app/utils/openapi_utils.py` (`get_llm()`) avec la classe `AzureOpenAI` du SDK `openai`.
- Utilisé directement dans `app/infrastructure/analyzers/openai_analyzer.py` (`OpenAIAnalyzer`), y compris pour construire le prompt d'extraction et parser la réponse JSON.
- `Settings` peut aussi charger ces valeurs depuis Azure Key Vault en environnement `production`/`staging` (`_load_from_keyvault`).

**Cette configuration n'est pas figée.** La souscription Azure utilisée pour Azure OpenAI a expiré, et un changement de fournisseur est en cours d'évaluation, potentiellement vers un service gratuit compatible avec le SDK `openai` (endpoint OpenAI-compatible). Ne pas présenter Azure OpenAI comme un choix définitif dans toute documentation ou décision d'architecture future : le point d'extension prévu pour ce changement est `TextAnalyzer`/`BaseAnalyzer`, en ajoutant une nouvelle implémentation à côté de `OpenAIAnalyzer` (ou en l'adaptant) plutôt qu'en modifiant `CVService` ou les endpoints.

Les variables d'environnement réelles sont définies dans `.env` (non versionné, non lu ici pour cette documentation). `.env.example` liste les noms de variables attendues (`AZURE_OPENAI_ENDPOINT`, `AZURE_OPENAI_API_KEY`, `AZURE_OPENAI_API_VERSION`, `AZURE_OPENAI_DEPLOYMENT_GPT_35_TURBO`, `AZURE_OPENAI_MODEL_VERSION_GPT_35_TURBO`, `AZURE_OPENAI_TEMPERATURE`) sans valeurs réelles.

## Spécifications

_À compléter : les spécifications fonctionnelles de ce service seront ajoutées ici au fur et à mesure._
