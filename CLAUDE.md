# Instructions Claude — XpertSphere (racine du monorepo)

## Contexte du projet

XpertSphere est une plateforme ATS (Applicant Tracking System) développée dans le cadre d'un projet de certification RNCP niveau 7. Le dossier `doc/` contient par ailleurs un mémoire de fin d'études distinct qui documente ce projet.

## Documentation (à charger à la demande, jamais automatiquement)

Ce fichier est le seul chargé systématiquement (une session Claude Code démarre toujours depuis cette racine). Tout ce qui suit ne doit être lu que si le sujet en cours le nécessite réellement — ne pas les ouvrir par précaution ni les résumer d'avance :

- `README.md` — présentation fonctionnelle complète du produit. À lire seulement pour une question sur les fonctionnalités métier ou la roadmap.
- `INFRASTRUCTURE.md` — installation, Docker, déploiement Azure. À lire seulement pour une tâche d'installation/infra/déploiement.
- `CONTRIBUTING.md` — conventions Git, commit, code, tests. À lire avant un commit ou une PR ; prime sur toute reformulation de ce fichier en cas de divergence.
- `doc/CLAUDE.md` et `doc/instructions.md` — s'appliquent uniquement à la rédaction du mémoire dans `doc/`, jamais au code. Ne les lire que si le travail concerne explicitement `doc/`.
- Le `CLAUDE.md` de chaque service (voir structure ci-dessous) — à lire seulement quand on travaille effectivement dans ce service, pas par anticipation.

## Structure du monorepo

```
XpertSphere/
├── src/backend/                       # Services backend
│   ├── XpertSphere.MonolithApi/       # Cœur métier (.NET 9)
│   ├── XpertSphere.CommunicationService/  # Notifications/emails (.NET 9)
│   ├── XpertSphere.ReportingService/  # Analytics et rapports (.NET 9)
│   ├── XpertSphere.IntegrationService/    # Plateformes externes (.NET 9)
│   ├── XpertSphere.ResumeAnalyzer/    # Analyse de CV (Python/FastAPI)
│   └── XpertSphere.sln
├── src/frontend/packages/
│   ├── recruiter-app/                 # Interface recruteurs
│   └── candidate-app/                 # Interface candidats
├── doc/                                # Mémoire de fin d'études (projet séparé)
├── docker/, docker-compose.yml         # Conteneurisation locale
└── .github/workflows/                  # CI/CD (un workflow par service, déclenché par chemin)
```

Chaque service backend et chaque application frontend possède son propre `CLAUDE.md` à sa racine, volontairement court (aperçu + commandes essentielles), avec ses propres références lazy-load vers `.claude/docs/` et `.claude/specifications/`.

## Convention `.claude/` par service

Le détail ne vit jamais dans `CLAUDE.md` lui-même, seulement dans les fichiers qu'il référence :

- `.claude/specifications/<slug-fonctionnalite>.md` : une spécification par fonctionnalité, rédigée par l'agent `spec-writer` avant tout développement.
- `.claude/docs/` : architecture, structure détaillée, conventions observées.

### Fonctionnalités transverses

Certaines fonctionnalités ne relèvent d'aucun service en particulier (ex. orchestration Docker Compose de plusieurs services, CI/CD globale). Leur spécification vit dans `.claude/specifications/<slug-fonctionnalite>.md` à la racine du monorepo, sur le même modèle que les specs par service, plutôt que d'être rattachée arbitrairement à l'un des services concernés. Voir `.claude/specifications/README.md` pour l'index. Actuellement : `dockerize-full-stack.md` (stack applicative complète via `docker compose up`).

## Agents disponibles à la racine

Trois agents sont définis dans `.claude/agents/` pour structurer le cycle spec → développement → validation, quel que soit le service concerné :

- **spec-writer** : aide à définir et rédiger les spécifications fonctionnelles/techniques d'une fonctionnalité avant tout développement. Le résultat est écrit dans `.claude/specifications/` du service concerné.
- **developer** : implémente une fonctionnalité en suivant la spécification déjà rédigée et les conventions du service concerné (`CONTRIBUTING.md` + `CLAUDE.md` local).
- **validator** : relit un développement terminé et vérifie sa conformité à la spécification et aux conventions du projet, sans se substituer aux tests automatisés.

Workflow attendu : spec-writer rédige la spec dans `.claude/specifications/` → developer implémente → validator vérifie la conformité au regard de la spec et signale les écarts.

## Notes de cohérence

- Ne pas dupliquer dans ce fichier racine ce qui est déjà détaillé dans `README.md`, `CONTRIBUTING.md`, `INFRASTRUCTURE.md` ou dans le `CLAUDE.md`/`.claude/docs/` de chaque service : y renvoyer plutôt.
- Les spécifications fonctionnelles sont ajoutées progressivement par l'utilisateur, service par service, dans `.claude/specifications/` — ne pas en inventer en attendant.
