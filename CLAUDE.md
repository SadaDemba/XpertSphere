# Instructions Claude — XpertSphere (racine du monorepo)

## Contexte du projet

XpertSphere est une plateforme ATS (Applicant Tracking System) développée dans le cadre d'un projet de certification RNCP niveau 7. Le dossier `doc/` contient par ailleurs un mémoire de fin d'études distinct qui documente ce projet (voir `doc/CLAUDE.md` et `doc/instructions.md`, qui s'appliquent uniquement à la rédaction du mémoire, pas au code).

Pour la présentation fonctionnelle complète, lire `README.md`. Pour l'installation et l'infrastructure, lire `INFRASTRUCTURE.md`. Pour les conventions Git, de commit et de code déjà en vigueur, lire `CONTRIBUTING.md` — ce fichier prime sur toute reformulation ci-dessous en cas de divergence.

## Structure du monorepo

```
XpertSphere/
├── src/backend/                       # Services backend
│   ├── XpertSphere.MonolithApi/       # Cœur métier (.NET 9) — voir son CLAUDE.md
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

Chaque service backend et chaque application frontend possède désormais son propre `CLAUDE.md` à sa racine, avec sa stack technique, ses commandes de build/test/run, ses conventions observées et une section `## Spécifications` à compléter au fil de l'eau. Toujours lire le `CLAUDE.md` du service concerné avant d'y travailler.

## Agents disponibles à la racine

Trois agents sont définis dans `.claude/agents/` pour structurer le cycle spec → développement → validation, quel que soit le service concerné :

- **spec-writer** : aide à définir et rédiger les spécifications fonctionnelles/techniques d'une fonctionnalité avant tout développement. Le résultat vient enrichir la section `## Spécifications` du `CLAUDE.md` du service concerné.
- **developer** : implémente une fonctionnalité en suivant les spécifications déjà rédigées et les conventions du service concerné (`CONTRIBUTING.md` + `CLAUDE.md` local).
- **validator** : relit un développement terminé et vérifie sa conformité aux spécifications et aux conventions du projet, sans se substituer aux tests automatisés.

Workflow attendu : spec-writer rédige la spec → developer implémente → validator vérifie la conformité au regard de la spec et signale les écarts.

## Notes de cohérence

- Ne pas dupliquer dans ce fichier racine ce qui est déjà détaillé dans `README.md`, `CONTRIBUTING.md`, `INFRASTRUCTURE.md` ou dans le `CLAUDE.md` de chaque service : y renvoyer plutôt.
- Les spécifications fonctionnelles sont ajoutées progressivement par l'utilisateur, service par service — ne pas en inventer en attendant.
