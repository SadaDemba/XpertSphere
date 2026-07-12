# Instructions Claude — XpertSphere.ResumeAnalyzer

## Aperçu du service

XpertSphere.ResumeAnalyzer est un microservice Python (FastAPI), distinct des services .NET du monorepo XpertSphere. Il reçoit un CV (PDF) envoyé par les autres services, en extrait le texte, puis appelle un LLM pour produire des données structurées (identité, formations, expériences, compétences, langues).

API : `POST /api/extract/` (extraction), `GET /api/health*` (supervision), `/docs`/`/redoc` (Swagger/ReDoc).

Stack : FastAPI, `pdfplumber` (extraction PDF), SDK `openai` (client LLM), `pydantic-settings` + Azure Key Vault optionnel (`app/core/config.py`), tests `pytest`.

## Documentation

- Architecture, structure du projet, conventions de code, fournisseur LLM : `.claude/docs/architecture.md` — à lire avant toute modification non triviale.
- Spécifications fonctionnelles : `.claude/specifications/` (un fichier par fonctionnalité). Actuellement : `llm-provider-groq-azure.md` (coexistence Azure OpenAI / Groq).

## Commandes

```bash
source .venv/bin/activate           # activer le venv (déjà présent en local)
pip install -r requirements.txt     # installer les dépendances
uvicorn app.main:app --reload --port 8000   # lancer le serveur de dev (http://localhost:8000)
pytest                               # lancer les tests (mock des appels externes)
```
