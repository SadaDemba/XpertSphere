# Sélection du fournisseur LLM (Azure OpenAI / Groq)

## Contexte et objectif

La souscription Azure utilisée pour Azure OpenAI a expiré (le fournisseur actuel répond en 401). Objectif : ajouter **Groq** (`groq.com`) comme second fournisseur LLM, gratuit et compatible avec le SDK `openai` standard (client `OpenAI(base_url="https://api.groq.com/openai/v1", api_key=...)`), **sans supprimer** la configuration Azure OpenAI existante. Les deux configurations coexistent dans le code et dans les variables d'environnement ; un paramètre choisit lequel des deux est actif à l'exécution.

Périmètre : configuration, implémentation de l'analyzer Groq, point d'injection de dépendance, validation au démarrage. `CVService`, les endpoints (`app/api/endpoints/`) et le contrat `TextAnalyzer` ne sont pas modifiés.

## Variable de sélection

Ajouter dans `Settings` (`app/core/config.py`) :

```
LLM_PROVIDER: Literal["azure_openai", "groq"] = "azure_openai"
```

- Valeur par défaut `"azure_openai"` : si la variable n'est pas définie, le comportement actuel est strictement préservé (rétrocompatibilité).
- Une valeur hors de ces deux littéraux doit échouer à l'instanciation de `Settings` (validation native de pydantic sur `Literal`), pas silencieusement plus tard.

## Nouvelles variables d'environnement (Groq)

À ajouter dans `Settings`, sur le même modèle que les champs `AZURE_OPENAI_*` déjà présents :

```
GROQ_API_KEY: Optional[str] = None
GROQ_MODEL: Optional[str] = None
GROQ_BASE_URL: str = "https://api.groq.com/openai/v1"
GROQ_TEMPERATURE: float = 0.1
```

- `GROQ_MODEL` n'a volontairement **pas** de valeur par défaut dans le code : contrairement à Azure (où deux déploiements nommés existent déjà, `gpt-35-turbo` et `gpt-4o-mini`), Groq n'a pas de notion de « déploiement », seulement un nom de modèle passé directement en paramètre `model`. Comme le catalogue de modèles gratuits proposés par Groq peut évoluer, il n'est pas approprié de figer un nom de modèle par défaut dans le code : il doit être fourni explicitement quand `LLM_PROVIDER=groq`. Modèle retenu pour la configuration `.env` locale/déploiement actuel : `llama-3.3-70b-versatile` (bon compromis qualité/vitesse pour une tâche d'extraction structurée). Ce choix n'est pas figé dans le code, seulement documenté ici et dans `.env`/`.env.example` : il peut être changé sans modification de code si le catalogue Groq évolue.
- `GROQ_TEMPERATURE` est **dédiée** (et non partagée avec `AZURE_OPENAI_TEMPERATURE`), pour permettre un réglage indépendant par fournisseur ; la valeur par défaut `0.1` reprend celle d'Azure par cohérence, mais peut être ajustée séparément.
- Pas d'équivalent `GROQ_API_VERSION` ni de bascule de modèle par `ENVIRONMENT` (pas de `current_groq_model`) : cette dimension est spécifique à Azure (`current_deployment`/`current_model_version`) et n'a pas de raison d'être répliquée pour Groq à ce stade.
- Mettre à jour `.env.example` avec les noms de ces nouvelles variables (sans valeurs réelles), à la suite du bloc `AZURE_OPENAI_*` déjà présent.

## Contrat d'interface et mutualisation du code générique

`_create_prompt` (construction du prompt) et le parsing de la réponse JSON (`json.loads` + construction `Experience`/`Training`/`CVModel`) sont aujourd'hui écrits dans `OpenAIAnalyzer.analyze()` sans rien de spécifique à Azure : ce code doit être mutualisé plutôt que dupliqué dans un nouvel analyzer Groq.

Extension proposée de `BaseAnalyzer` (`app/infrastructure/analyzers/base_analyzer.py`), qui reste `ABC` et continue d'implémenter `TextAnalyzer` :

- `BaseAnalyzer` implémente concrètement `analyze(text, options) -> CVModel` (méthode gabarit) : construit le prompt via `_create_prompt(text)` (déplacé tel quel depuis `OpenAIAnalyzer`, contenu inchangé), délègue l'appel réseau à une nouvelle méthode abstraite `_get_completion(prompt: str) -> str`, puis parse le résultat vers `CVModel` via une méthode `_parse_response(content: str) -> CVModel` (déplacée telle quelle depuis `OpenAIAnalyzer.analyze`), le tout encadré par la capture d'exception existante qui lève `AnalysisError`.
- `_get_completion(prompt: str) -> str` devient la seule méthode que chaque fournisseur doit implémenter : elle appelle `self.client.chat.completions.create(...)` avec le modèle et la température propres au fournisseur, et retourne `response.choices[0].message.content`.
- `OpenAIAnalyzer` (existant) perd son `analyze()` et son `_create_prompt()` (remontés dans `BaseAnalyzer`) ; il ne conserve que `__init__`/`_initialize_client` (client `AzureOpenAI` via `get_llm()`) et implémente `_get_completion` avec `model=settings.current_deployment`, `temperature=settings.AZURE_OPENAI_TEMPERATURE`.
- Nouvelle classe `GroqAnalyzer(BaseAnalyzer)` dans `app/infrastructure/analyzers/groq_analyzer.py` : même structure, avec un client `OpenAI` (et non `AzureOpenAI`) via une nouvelle fonction `get_groq_llm()` dans `app/utils/openapi_utils.py` (`OpenAI(base_url=settings.GROQ_BASE_URL, api_key=settings.GROQ_API_KEY)`), et `_get_completion` utilisant `model=settings.GROQ_MODEL`, `temperature=settings.GROQ_TEMPERATURE`.
- Exporter `GroqAnalyzer` dans `app/infrastructure/analyzers/__init__.py`, aux côtés de `BaseAnalyzer` et `OpenAIAnalyzer`.

Avec cette structure, `TextAnalyzer` (l'interface abstraite) n'est pas modifié, et `CVService`/les endpoints n'ont aucune connaissance du fournisseur actif : ils continuent de manipuler un `TextAnalyzer` générique.

## Point d'injection de dépendance

Le branchement du choix de fournisseur se fait dans `app/api/dependencies.py` (`get_cv_service`), seul endroit du code qui construit l'analyzer concret aujourd'hui :

- Remplacer l'instanciation fixe `analyzer = OpenAIAnalyzer()` par une sélection basée sur `settings.LLM_PROVIDER` (`GroqAnalyzer()` si `"groq"`, `OpenAIAnalyzer()` sinon/par défaut).
- Aucune autre modification dans ce fichier : `CVService`, `extractors`, la signature de `get_cv_service()` restent inchangés.

## Chargement depuis Azure Key Vault

`Settings._load_from_keyvault()` (actif seulement si `ENVIRONMENT` vaut `production`/`staging` **et** `KEY_VAULT_URL` est défini) ne charge aujourd'hui que les secrets Azure OpenAI. Si Groq est amené à tourner en production (l'abonnement Azure ayant justement expiré), ses secrets doivent pouvoir être chargés de la même manière, sans quoi la validation au démarrage (voir plus bas) échouerait alors même que le secret existe dans le coffre.

Ajouter, sur le même modèle (bloc `try/except` par secret, log d'avertissement et repli sur la valeur déjà chargée en cas d'échec, sans interrompre le chargement des autres secrets) :

- `groq-api-key` → `GROQ_API_KEY`
- `groq-model` → `GROQ_MODEL`
- `groq-temperature` → `GROQ_TEMPERATURE` (conversion `float`, comme pour `azure-openai-temperature`)

`LLM_PROVIDER` reste une simple variable d'environnement/déploiement (non un secret) et n'a pas besoin d'être chargée depuis Key Vault.

## Validation au démarrage (fail-fast)

Comportement attendu si `LLM_PROVIDER` pointe vers un fournisseur dont la configuration est incomplète : **échec explicite au démarrage du processus**, pas une erreur qui n'apparaît qu'au premier appel à `POST /api/extract/`.

- Ajouter une validation exécutée à la fin de `Settings.__init__`, **après** l'appel à `_load_from_keyvault()` (pour que les valeurs chargées depuis le coffre comptent) :
  - si `LLM_PROVIDER == "azure_openai"` (défaut) : `AZURE_OPENAI_ENDPOINT` et `AZURE_OPENAI_API_KEY` doivent être renseignés (c'est déjà vérifié aujourd'hui, mais dans `OpenAIAnalyzer._initialize_client`, donc à la première requête plutôt qu'au démarrage) ;
  - si `LLM_PROVIDER == "groq"` : `GROQ_API_KEY` et `GROQ_MODEL` doivent être renseignés.
- En cas de configuration incomplète pour le fournisseur sélectionné, lever une exception explicite dès l'instanciation du module `app/core/config.py` (import-time, via `settings = Settings()`), et non une exception silencieusement absorbée ou reportée à l'exécution.
- Cette validation porte uniquement sur la **présence** de la configuration du fournisseur sélectionné (pas sur sa validité, ex. une clé Azure expirée reste « présente » et continuera de produire une 401 à l'exécution comme aujourd'hui : cela reste un comportement inchangé, hors périmètre de cette fonctionnalité). Le fournisseur non sélectionné n'a pas besoin d'être configuré.

Note de rétrocompatibilité sur ce point : ce changement fait remonter le moment de l'échec (démarrage du process au lieu de la première requête) pour Azure OpenAI également, ce qui est le comportement explicitement demandé et non une régression, mais mérite d'être noté comme un changement de comportement observable.

## Rétrocompatibilité

- Sans `LLM_PROVIDER` défini (déploiements existants), le comportement est identique à aujourd'hui : Azure OpenAI, mêmes noms de variables, mêmes valeurs par défaut, mêmes propriétés `current_deployment`/`current_model_version`.
- Aucune variable Azure existante n'est renommée, supprimée, ni sa valeur par défaut modifiée.
- Le seul changement de comportement observable pour les déploiements Azure existants est le moment de l'échec en cas de configuration incomplète (voir ci-dessus).

## Tests et critères d'acceptation

- Basculer `LLM_PROVIDER=groq` (avec `GROQ_API_KEY`/`GROQ_MODEL` définis) suffit à changer de fournisseur actif, sans modification de code.
- `tests/conftest.py` continue de définir les variables Azure factices actuelles (aucune modification requise pour que la suite existante passe) ; y ajouter des valeurs factices `GROQ_API_KEY`/`GROQ_MODEL` (et `GROQ_TEMPERATURE` si besoin), pour permettre l'instanciation directe de `GroqAnalyzer()` dans ses propres tests, sans changer `LLM_PROVIDER` par défaut (qui reste `azure_openai` pour tous les tests existants).
- Les tests existants (`tests/test_openai_analyzer.py`, `tests/test_api.py`, etc.) passent sans modification.
- Nouveau fichier `tests/test_groq_analyzer.py`, miroir de `tests/test_openai_analyzer.py` : mêmes trois cas (succès, erreur API, JSON invalide), en mockant la classe `OpenAI` dans le module `groq_analyzer` (au lieu de `AzureOpenAI` dans `openai_analyzer`).
- Instancier `Settings` avec `LLM_PROVIDER="groq"` et sans `GROQ_API_KEY`/`GROQ_MODEL` lève une exception explicite (test à ajouter, ex. dans un futur `tests/test_config.py`).
- Instancier `Settings` avec une valeur de `LLM_PROVIDER` hors `{azure_openai, groq}` lève une exception explicite.
- `app/services/cv_service.py` et `app/api/endpoints/resume.py` ne sont pas modifiés.

## Hors périmètre

Décidé : aucun repli automatique (fallback) d'un fournisseur vers l'autre en cas d'échec en cours d'exécution (ex. bascule automatique vers Groq si Azure répond en 401). Si le fournisseur actif échoue, l'erreur remonte telle quelle (comportement actuel inchangé) ; le basculement de fournisseur reste une action manuelle via `LLM_PROVIDER`. Ce choix évite d'ajouter une logique de détection d'échec qui risquerait de masquer un problème de configuration.
