# Spécifications transverses — XpertSphere (racine du monorepo)

Un fichier Markdown par fonctionnalité transverse spécifiée (nommage `<slug-fonctionnalite>.md`), rédigé par l'agent spec-writer avant tout développement. Une fonctionnalité transverse est une fonctionnalité qui ne relève d'aucun service en particulier (ex. orchestration Docker Compose de plusieurs services, CI/CD globale) — voir `CLAUDE.md` racine, section « Fonctionnalités transverses ». Les spécifications propres à un seul service vivent dans le `.claude/specifications/` de ce service, pas ici.

Spécifications existantes :
- `dockerize-full-stack.md` — `docker compose up` unique démarrant la stack applicative complète (MonolithApi, ResumeAnalyzer, recruiter-app, candidate-app + infrastructure) pour un test end-to-end local.
