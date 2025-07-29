# Guide de contribution à XpertSphere

Ce document décrit les processus et conventions à suivre pour contribuer au projet XpertSphere. Veuillez lire attentivement ces directives avant de commencer à travailler sur le projet.

## 🏗️ Architecture du Projet

XpertSphere est organisé en monorepo avec une séparation claire entre backend et frontend :

```txt
XpertSphere/
├── src/backend/              # Services .NET + Python
│   ├── XpertSphere.MonolithApi/       # API principale (.NET 9)
│   ├── XpertSphere.CommunicationService/  # Notifications (.NET 9)
│   ├── XpertSphere.ResumeAnalyzer/    # Analyse CV (FastAPI)
│   └── ...
├── src/frontend/             # Applications Vue.js
│   ├── packages/recruiter-app/    # Interface recruteurs
│   ├── packages/candidate-app/    # Interface candidats
│   └── package.json              # Monorepo npm
├── .github/                  # CI/CD workflows
└── scripts/                  # Automatisation
```

## 📝 Processus de développement

### Workflow Git

Nous suivons un workflow Git basé sur le modèle GitFlow adapté :

1. **Création d'une branche** à partir de `develop` pour chaque fonctionnalité ou correction
2. **Développement et tests** dans cette branche avec commits atomiques
3. **Soumission d'une Pull Request** vers `develop`
4. **Revue de code** par au moins un autre développeur
5. **Tests automatiques** (CI/CD) qui doivent passer
6. **Fusion** dans `develop` une fois approuvée
7. Fusion périodique de `develop` vers `main` pour les releases

### Types de branches

- **Branche principale** : `main` (production stable)
- **Branche de développement** : `develop` (intégration continue)
- **Branches de fonctionnalités** : `feature/XS-123-description-courte`
- **Branches de correction** : `bugfix/XS-123-description-courte`
- **Branches de hotfix** : `hotfix/XS-123-description-courte`
- **Branches de release** : `release/v1.1.0`

### Conventions de nommage

```bash
# Fonctionnalités
feature/XS-123-add-cv-analysis
feature/XS-456-improve-candidate-ui

# Corrections de bugs
bugfix/XS-789-fix-authentication-issue
bugfix/XS-101-resolve-email-template

# Hotfixes (urgents en production)
hotfix/XS-999-critical-security-patch

# Releases
release/v1.1.0
release/v2.0.0-beta
```

## 💬 Conventions de commit

Nous utilisons [Conventional Commits](https://www.conventionalcommits.org/) pour standardiser nos messages de commit et permettre la génération automatique du changelog.

### Format

```markdown
<type>(<scope>): <description>

[corps optionnel]

[footer(s) optionnel(s)]
```

### Types de commit

| Type | Description | Exemple |
|------|-------------|---------|
| `feat` | Nouvelle fonctionnalité | `feat(auth): add Azure AD B2C integration` |
| `fix` | Correction de bug | `fix(cv-parser): handle corrupted PDF files` |
| `docs` | Documentation uniquement | `docs(readme): update installation instructions` |
| `style` | Formatage, styling | `style(frontend): apply consistent button styling` |
| `refactor` | Refactorisation sans changement de fonctionnalité | `refactor(jobs): simplify filtering logic` |
| `perf` | Amélioration des performances | `perf(api): optimize database queries` |
| `test` | Ajout ou correction de tests | `test(auth): add integration tests for login` |
| `build` | Modifications du système de build | `build(docker): update base images` |
| `ci` | Modifications CI/CD | `ci(github): add security scanning workflow` |
| `chore` | Autres changements (deps, config) | `chore(deps): update .NET to version 9.0.1` |

### Scopes recommandés

**Backend** :

- `auth` - Authentification et autorisation
- `api` - APIs REST
- `jobs` - Gestion des offres d'emploi
- `applications` - Gestion des candidatures
- `cv-parser` - Analyse de CV
- `notifications` - Communications
- `reporting` - Analytics et rapports
- `integration` - Intégrations externes

**Frontend** :

- `recruiter` - Application recruteur
- `candidate` - Application candidat
- `components` - Composants partagés
- `ui` - Interface utilisateur
- `forms` - Formulaires
- `navigation` - Navigation et routing

**Général** :

- `config` - Configuration
- `security` - Sécurité
- `performance` - Performance
- `accessibility` - Accessibilité

### Exemples de commits

```bash
# Nouvelles fonctionnalités
feat(cv-parser): add support for DOCX files
feat(recruiter): implement advanced candidate filtering
feat(auth): add multi-factor authentication

# Corrections
fix(api): resolve CORS issues for candidate app
fix(notifications): prevent duplicate email sending
fix(ui): improve mobile responsiveness on job listings

# Documentation
docs(backend): update API documentation with new endpoints
docs(infrastructure): add Azure deployment guide

# Refactoring
refactor(database): optimize candidate search queries
refactor(frontend): extract reusable form components

# Performances
perf(api): implement Redis caching for job listings
perf(frontend): lazy load candidate profiles

# Tests
test(integration): add E2E tests for recruitment workflow
test(unit): increase coverage for CV parsing service

# Build et CI
build(docker): optimize container size and build time
ci(github): add automated security scans on PRs
```

## 🧪 Standards de tests

Tous les nouveaux développements doivent être accompagnés de tests appropriés avec une couverture minimale de 80%.

### Backend .NET

```bash
# Tests unitaires avec xUnit
dotnet test --collect:"XPlat Code Coverage"

# Tests d'intégration
dotnet test --filter Category=Integration

# Standards de tests
- Utiliser xUnit + Moq + FluentAssertions
- Tests d'intégration avec base de données en mémoire
- Tests d'APIs avec WebApplicationFactory
- Mocks pour les services externes
```

### Backend Python

```bash
# Tests unitaires avec pytest
pytest --cov=app tests/

# Standards de tests
- Utiliser pytest + pytest-mock + httpx pour FastAPI
- Tests avec fixtures pour les données de test
- Mocks pour les appels OpenAI et services externes
- Tests de performance pour l'analyse de CV
```

### Frontend Vue.js

```bash
# Tests unitaires et composants
npm run test

# Tests E2E
npm run test:e2e

# Standards de tests
- Utiliser Vitest + Vue Test Utils + Testing Library
- Tests de composants avec données mockées
- Tests E2E avec Playwright
- Tests d'accessibilité avec axe-core
```

### Couverture de tests requise

| Composant | Couverture minimale | Outils |
|-----------|-------------------|---------|
| **Backend .NET** | 85% | xUnit, Coverlet |
| **Backend Python** | 80% | pytest-cov |
| **Frontend Vue.js** | 75% | Vitest coverage |
| **APIs E2E** | 70% | Postman/Newman |

## 📏 Conventions de codage

### .NET (C#)

```csharp
// Suivre les conventions Microsoft
// https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/

// ✅ Bon
public class JobApplicationService
{
    private readonly IJobRepository _jobRepository;
    
    public async Task<JobApplication> CreateApplicationAsync(
        CreateJobApplicationDto dto, 
        CancellationToken cancellationToken = default)
    {
        // Utiliser les fonctionnalités C# modernes
        var application = new JobApplication
        {
            JobId = dto.JobId,
            CandidateId = dto.CandidateId,
            Status = ApplicationStatus.New,
            AppliedAt = DateTimeOffset.UtcNow
        };
        
        return await _jobRepository.CreateAsync(application, cancellationToken);
    }
}

// ❌ Éviter
public class jobapplicationservice // PascalCase manquant
{
    public JobApplication CreateApplication(CreateJobApplicationDto dto)
    {
        // Pas d'async/await, pas de cancellation token
        var application = new JobApplication();
        application.JobId = dto.JobId; // Préférer l'initialisation d'objet
        // ...
        return _jobRepository.Create(application);
    }
}
```

**Standards .NET** :

- Utiliser les **records** pour les DTOs immutables
- **Pattern matching** et **switch expressions** quand approprié
- **Nullable reference types** activés
- **Minimal APIs** pour les endpoints simples
- **FluentValidation** pour la validation des entrées
- **MediatR** pour CQRS quand approprié

### Python (FastAPI)

```python
# Suivre PEP 8 et utiliser des annotations de type
# https://www.python.org/dev/peps/pep-0008/

# ✅ Bon
from typing import Optional
from pydantic import BaseModel, Field
from fastapi import HTTPException, status

class CVAnalysisRequest(BaseModel):
    """Request model for CV analysis."""
    file_content: bytes = Field(..., description="CV file content in bytes")
    file_type: str = Field(..., regex="^(pdf|docx|txt)$", description="File type")

async def analyze_cv(request: CVAnalysisRequest) -> CVAnalysisResponse:
    """
    Analyze CV content and extract structured information.
    
    Args:
        request: CV analysis request containing file data
        
    Returns:
        CVAnalysisResponse: Structured CV information
        
    Raises:
        HTTPException: If CV analysis fails
    """
    try:
        analysis_result = await cv_service.analyze_cv_content(
            content=request.file_content,
            file_type=request.file_type
        )
        return CVAnalysisResponse(**analysis_result)
    except CVAnalysisError as e:
        raise HTTPException(
            status_code=status.HTTP_422_UNPROCESSABLE_ENTITY,
            detail=f"CV analysis failed: {str(e)}"
        )

# ❌ Éviter
def analyze_cv(file_content, file_type):  # Pas d'annotations de type
    result = cv_service.analyze_cv_content(file_content, file_type)  # Pas async
    return result  # Pas de gestion d'erreur
```

**Standards Python** :

- **Type hints** obligatoires pour toutes les fonctions publiques
- **Pydantic models** pour la validation des données
- **Async/await** pour les opérations I/O
- **Docstrings** Google style pour toutes les fonctions publiques
- **Black** pour le formatage automatique
- **isort** pour l'organisation des imports

### Vue.js (TypeScript)

```vue
<!-- ✅ Bon -->
<template>
  <div class="job-card">
    <h3 class="job-card__title">{{ job.title }}</h3>
    <p class="job-card__company">{{ job.company.name }}</p>
    <div class="job-card__actions">
      <q-btn 
        :loading="isApplying"
        :disable="hasApplied"
        @click="handleApply"
        color="primary"
        label="Postuler"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useJobApplicationStore } from '@/stores/jobApplication'
import type { Job } from '@/types/job'

interface Props {
  job: Job
}

interface Emits {
  (e: 'applied', jobId: string): void
}

const props = defineProps<Props>()
const emit = defineEmits<Emits>()

const jobApplicationStore = useJobApplicationStore()

const isApplying = ref(false)
const hasApplied = computed(() => 
  jobApplicationStore.hasAppliedToJob(props.job.id)
)

const handleApply = async (): Promise<void> => {
  if (hasApplied.value) return
  
  isApplying.value = true
  try {
    await jobApplicationStore.applyToJob(props.job.id)
    emit('applied', props.job.id)
  } catch (error) {
    console.error('Failed to apply to job:', error)
    // Handle error appropriately
  } finally {
    isApplying.value = false
  }
}
</script>

<style scoped lang="scss">
.job-card {
  &__title {
    @apply text-xl font-semibold text-gray-900;
  }
  
  &__company {
    @apply text-sm text-gray-600 mt-1;
  }
  
  &__actions {
    @apply mt-4 flex justify-end;
  }
}
</style>
```

**Standards Vue.js** :

- **Composition API** obligatoire pour tous les nouveaux composants
- **TypeScript** strict avec interfaces définies
- **Props/Emits** typés avec TypeScript
- **Composables** pour la logique réutilisable
- **Pinia** pour la gestion d'état
- **Quasar** components suivant les guidelines
- **BEM methodology** pour les classes CSS
- **Préfixe `Xs`** pour les composants partagés

## 🚀 Pull Requests

### Processus de soumission

1. **Créer une branche** depuis `develop`
2. **Développer** la fonctionnalité avec tests
3. **Tester localement** toutes les vérifications
4. **Pousser** la branche et créer la PR
5. **Remplir le template** de PR complètement
6. **Demander des reviewers** appropriés
7. **Répondre aux commentaires** et appliquer les corrections
8. **Attendre l'approbation** et le merge

### Template de Pull Request

```markdown
## Description
<!-- Description claire de ce qui a été changé et pourquoi -->

## Type de changement
- [ ] Bug fix (changement non-breaking qui corrige un problème)
- [ ] Nouvelle fonctionnalité (changement non-breaking qui ajoute une fonctionnalité)
- [ ] Breaking change (correction ou fonctionnalité qui casserait les fonctionnalités existantes)
- [ ] Documentation uniquement

## Tests
- [ ] Tests unitaires ajoutés/mis à jour
- [ ] Tests d'intégration ajoutés/mis à jour
- [ ] Tests E2E ajoutés/mis à jour (si applicable)
- [ ] Tests manuels effectués

## Checklist
- [ ] Code suit les standards de style du projet
- [ ] Auto-révision effectuée
- [ ] Commentaires ajoutés dans les zones complexes
- [ ] Documentation mise à jour
- [ ] Pas de nouvelles warnings
- [ ] Tests ajoutés couvrent les changements
- [ ] Tests locaux passent
- [ ] Changements dependants mergés et publiés

## Screenshots (si applicable)
<!-- Ajouter des captures d'écran pour les changements UI -->

## Notes supplémentaires
<!-- Toute information additionnelle pour les reviewers -->
```

### Critères d'approbation

Une PR ne peut être mergée que si :

- ✅ Au moins **1 approbation** de développeur
- ✅ Tous les **tests automatiques** passent
- ✅ **Aucun conflit** de merge
- ✅ **Branch protection rules** respectées
- ✅ **Standards de code** respectés (linting, formatting)
- ✅ **Documentation** mise à jour si nécessaire

## 📚 Documentation

### Standards de documentation

- **Fonctionnalités complexes** doivent être documentées
- **APIs** documentées avec Swagger/OpenAPI
- **README** mis à jour pour les changements de configuration
- **CHANGELOG** généré automatiquement via conventional commits
- **Code comments** pour la logique complexe uniquement

### Structure de documentation

```txt
docs/
├── api/                      # Documentation des APIs
│   ├── openapi.yaml         # Spécification OpenAPI
│   └── postman/             # Collections Postman
├── architecture/            # Documentation architecture
│   ├── backend.md          # Architecture backend
│   ├── frontend.md         # Architecture frontend
│   └── security.md         # Spécifications sécurité
├── deployment/             # Guides de déploiement
│   ├── azure.md           # Déploiement Azure
│   └── docker.md          # Configuration Docker
└── user-guides/           # Guides utilisateur
    ├── recruiter.md       # Guide recruteur
    └── candidate.md       # Guide candidat
```

## 🔍 Révision de code

### Checklist pour les reviewers

**Fonctionnel** :

- [ ] La fonctionnalité fonctionne comme décrite
- [ ] Les cas d'erreur sont gérés appropriément
- [ ] Les performances sont acceptables
- [ ] La sécurité est prise en compte

**Technique** :

- [ ] Code suit les conventions du projet
- [ ] Architecture et patterns appropriés
- [ ] Tests suffisants et pertinents
- [ ] Documentation adéquate
- [ ] Pas de code dupliqué

**Qualité** :

- [ ] Code lisible et maintenable
- [ ] Nommage clair et cohérent
- [ ] Complexité raisonnable
- [ ] Dépendances justifiées

### Processus de review

1. **Review automatique** (CI/CD, linting, tests)
2. **Review fonctionnelle** (logique métier, UX)
3. **Review technique** (architecture, performance, sécurité)
4. **Review de tests** (couverture, pertinence)
5. **Approbation finale** et merge

## 🔒 Sécurité

### Signalement de vulnérabilités

Si vous découvrez une vulnérabilité de sécurité :

1. **NE PAS** créer d'issue publique
2. **Envoyer un email** à [sadadembat9@gmail.com](mailto:sadadembat9@gmail.com)
3. **Inclure** une description détaillée de la vulnérabilité
4. **Attendre** une réponse avant de divulguer publiquement

### Standards de sécurité

- **Authentification** obligatoire pour toutes les APIs sensibles
- **Validation** des entrées côté client ET serveur
- **Chiffrement** des données sensibles
- **Principe du moindre privilège** pour les accès
- **Audit** des actions critiques
- **Scan** automatique des dépendances vulnérables

## 🛠️ Outils de développement

### Obligatoires

- **Git** (2.40+) avec configuration appropriée
- **IDE/Editor** avec support LSP (VS Code recommandé)
- **Docker** et Docker Compose pour l'environnement local
- **Node.js** (20+) pour le frontend
- **.NET SDK** (9.0+) pour le backend
- **Python** (3.10+) pour le service d'IA

### Recommandés

```bash
# Extensions VS Code recommandées
- C# Dev Kit
- Vue - Official
- Python
- Docker
- GitLens
- ESLint
- Prettier
- Thunder Client (pour tester les APIs)
```

### Configuration Git recommandée

```bash
# Configuration globale
git config --global user.name "Votre Nom"
git config --global user.email "votre.email@example.com"
git config --global init.defaultBranch main
git config --global pull.rebase true
git config --global core.autocrlf input  # Linux/Mac
git config --global core.autocrlf true   # Windows

# Hooks Git (optionnel mais recommandé)
git config core.hooksPath .githooks
```

## 🎯 Bonnes pratiques

### Performance

- **Lazy loading** pour les ressources non critiques
- **Caching** approprié (Redis, browser cache)
- **Optimisation des requêtes** base de données
- **Compression** des assets frontend
- **CDN** pour les ressources statiques

### Accessibilité

- **WCAG 2.1 AA** compliance minimum
- **Tests automatiques** avec axe-core
- **Navigation clavier** supportée
- **Screen readers** compatibles
- **Contraste** suffisant (4.5:1 minimum)

### Monitoring

- **Logs structurés** avec corrélation IDs
- **Métriques** business et techniques
- **Alertes** proactives sur les erreurs
- **Health checks** pour tous les services
- **Performance monitoring** en continu

---

## 🤝 Questions et Support

- 💬 **Discussions générales** : [GitHub Discussions](../../discussions)
- 🐛 **Bugs et demandes** : [GitHub Issues](../../issues)
- 📧 **Contact direct** : [sadadembat9@gmail.com](mailto:sadadembat9@gmail.com)
- 📚 **Documentation** : Consultez les README dans chaque dossier

**Merci de contribuer à XpertSphere !** 🚀
