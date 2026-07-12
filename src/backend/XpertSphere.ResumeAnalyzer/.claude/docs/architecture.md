# Architecture — XpertSphere.ResumeAnalyzer

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
│   │   ├── base_analyzer.py         # BaseAnalyzer (implémente TextAnalyzer) : méthode gabarit analyze()/_create_prompt()/_parse_response(), délègue l'appel réseau à _get_completion() (abstraite)
│   │   ├── openai_analyzer.py       # OpenAIAnalyzer : _get_completion() via Azure OpenAI
│   │   └── groq_analyzer.py         # GroqAnalyzer : _get_completion() via Groq (SDK openai standard)
│   ├── extractors/
│   │   ├── base_extractor.py        # BaseExtractor (implémente DocumentExtractor)
│   │   └── pdf_extractor.py         # PDFExtractor : implémentation concrète via pdfplumber
│   └── schema.py                    # UserModel/Experience/Training : dataclasses proches de domain/models/resume.py
├── services/
│   └── cv_service.py                # CVService : orchestre extraction + analyse
└── utils/
    ├── openapi_utils.py              # get_llm() : client AzureOpenAI ; get_groq_llm() : client OpenAI (base_url Groq)
    └── pdf_utils.py                   # extract_text_from_pdf() : utilitaire d'extraction autonome

tests/
├── conftest.py                       # Variables d'environnement de test (valeurs factices, Azure + Groq)
├── test_models.py
├── test_pdf_extractor.py
├── test_openai_analyzer.py
├── test_groq_analyzer.py
├── test_config.py
└── test_api.py
```

Point d'attention : `app/infrastructure/schema.py` définit un `UserModel` proche du `CVModel` de `app/domain/models/resume.py`, mais aucune référence à ce fichier n'a été trouvée ailleurs dans le code lors de la dernière exploration. À vérifier avant de s'appuyer dessus ou de le supprimer.

## Conventions de code observées

- Classes et fonctions documentées par des docstrings de style Args/Returns/Raises.
- Utilisation systématique de `logging` par module (`self.logger = logging.getLogger(self.__class__.__name__)` dans les classes de base).
- Exceptions applicatives dédiées (`ExtractionError`, `AnalysisError`, `ValidationError`) héritant de `BaseApplicationError`, capturées au niveau du service (`CVService`) et traduites en `HTTPException` avec codes 400/422/500, ou gérées globalement via un handler FastAPI (`main.py`) qui renvoie un 422.
- Modèles de domaine en `dataclass` (pas de modèles Pydantic pour `CVModel`/`Experience`/`Training`), avec validation légère dans `__post_init__` (strip des chaînes, normalisation de l'email en minuscules).
- Séparation stricte interface (`app/domain/interfaces/`) / implémentation (`app/infrastructure/`), avec une classe de base commune par famille (`BaseAnalyzer`, `BaseExtractor`) qui porte le logger et, pour les extracteurs, la logique générique de `can_extract`.

## Architecture : pattern interface/adaptateur

Le service applique une architecture hexagonale légère pour isoler le domaine métier des fournisseurs externes :

- `DocumentExtractor` (interface, `app/domain/interfaces/document_extractor.py`) définit le contrat d'extraction (`can_extract`, `extract_text`). `PDFExtractor` en est aujourd'hui la seule implémentation concrète (via `pdfplumber`), héritant de `BaseExtractor`.
- `TextAnalyzer` (interface, `app/domain/interfaces/text_analyzer.py`) définit le contrat d'analyse (`analyze`). Deux implémentations coexistent, héritant de `BaseAnalyzer` : `OpenAIAnalyzer` (Azure OpenAI) et `GroqAnalyzer` (Groq). `BaseAnalyzer` porte `analyze()` (méthode gabarit), `_create_prompt()` et `_parse_response()` ; chaque implémentation ne fournit que `_get_completion()`.
- `CVService` (`app/services/cv_service.py`) orchestre le flux : il reçoit une liste d'extracteurs et un analyseur en injection de dépendances (voir `app/api/dependencies.py`), sélectionne l'extracteur adapté au type de fichier, puis délègue l'analyse du texte extrait.

Cette séparation permet, en théorie, d'ajouter un nouvel extracteur (DOCX, image scannée, etc.) ou un nouvel analyseur (autre fournisseur LLM) sans modifier `CVService` ni les endpoints, à condition d'implémenter l'interface correspondante et de l'injecter dans `get_cv_service()`.

## Fournisseur LLM

Deux fournisseurs LLM coexistent (voir `.claude/specifications/llm-provider-groq-azure.md`), sélectionnés via `Settings.LLM_PROVIDER: Literal["azure_openai", "groq"]` (défaut `"azure_openai"`, rétrocompatible avec les déploiements existants) :

- **Azure OpenAI** (défaut) : `AZURE_OPENAI_ENDPOINT`, `AZURE_OPENAI_API_KEY`, `AZURE_OPENAI_API_VERSION`, déploiements distincts pour `gpt-35-turbo` (dev/staging) et `gpt-4o-mini` (production) via `current_deployment`/`current_model_version`. Client construit par `get_llm()` (`app/utils/openapi_utils.py`, classe `AzureOpenAI`). Implémentation : `OpenAIAnalyzer`.
- **Groq** : `GROQ_API_KEY`, `GROQ_MODEL` (pas de valeur par défaut dans le code — modèle retenu actuellement : `llama-3.3-70b-versatile`, documenté en `.env` uniquement), `GROQ_BASE_URL` (défaut `https://api.groq.com/openai/v1`), `GROQ_TEMPERATURE`. Client construit par `get_groq_llm()` (SDK `openai` standard, classe `OpenAI`). Implémentation : `GroqAnalyzer`.

Point d'injection : `app/api/dependencies.py` (`get_cv_service`) instancie `GroqAnalyzer()` ou `OpenAIAnalyzer()` selon `settings.LLM_PROVIDER` ; `CVService` et les endpoints ne connaissent que `TextAnalyzer`.

`Settings` valide au démarrage (fin de `__init__`, après `_load_from_keyvault()`) que la configuration du fournisseur sélectionné est complète, et lève une exception explicite sinon (échec au démarrage du processus, pas à la première requête). Les deux fournisseurs peuvent aussi charger leurs secrets depuis Azure Key Vault en environnement `production`/`staging` (`_load_from_keyvault`) ; `LLM_PROVIDER` reste une simple variable d'environnement, pas un secret.

Aucun repli automatique entre fournisseurs en cas d'échec à l'exécution : le basculement reste une action manuelle via `LLM_PROVIDER`.

Les variables d'environnement réelles sont définies dans `.env` (non versionné). `.env.example` liste les noms de variables attendues, y compris le bloc `GROQ_*`/`LLM_PROVIDER` à la suite du bloc `AZURE_OPENAI_*`, sans valeurs réelles.
