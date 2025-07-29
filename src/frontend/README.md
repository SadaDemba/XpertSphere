# XpertSphere Frontend

Documentation technique pour les applications frontend de XpertSphere ATS.

## Architecture

Le frontend XpertSphere utilise une architecture monorepo avec npm workspaces, regroupant deux applications Vue.js distinctes :

- **Candidate App** : Interface destinée aux candidats pour consulter les offres et postuler
- **Recruiter App** : Interface de gestion pour les recruteurs et équipes RH

### Stack technique

- **Framework** : Vue.js 3.4+ avec Composition API
- **Build Tool** : Quasar Framework 2.16+ avec Vite
- **Langage** : TypeScript 5.5+
- **Gestion d'état** : Pinia 3.0+
- **Routing** : Vue Router 4.0+
- **Internationalisation** : Vue i18n 11.0+
- **HTTP Client** : Axios 1.2+

### Structure du monorepo

``` txt
src/frontend/
├── packages/
│   ├── candidate-app/          # Application candidat
│   └── recruiter-app/          # Application recruteur
├── .husky/                     # Git hooks
├── eslint.config.mjs          # Configuration ESLint partagée
├── .prettierrc                # Configuration Prettier partagée
└── package.json               # Scripts et dépendances partagées
```

## Prérequis

### Environnement

- **Node.js** : Version 20.0.0 ou supérieure
- **npm** : Version 10.0.0 ou supérieure
- **Git** : Version 2.0+ (pour les hooks Husky)

### Variables d'environnement

Créez un fichier `.env` à la racine du projet avec :

```bash
VITE_API_BASE_URL=http://localhost:5000/api
```

## Installation

### 1. Installation des dépendances

```bash
# Depuis la racine du projet
cd src/frontend

# Installation de toutes les dépendances du monorepo
npm ci
```

### 2. Configuration des outils de développement

#### Setup Husky (git hooks)

```bash
# Configuration manuelle de Husky
cd ../..  # Retour à la racine du projet
git config core.hooksPath src/frontend/.husky

# Vérification
cd src/frontend
cat .husky/pre-commit  # Doit contenir : npx lint-staged
```

#### Test du setup

```bash
# Test du formatage automatique
echo 'const test={a:1,b:2}' > packages/candidate-app/src/test.js
git add .
git commit -m "test husky"

# Le fichier devrait être automatiquement formaté en :
# const test = { a: 1, b: 2 };

# Nettoyage
git reset --soft HEAD~1
rm packages/candidate-app/src/test.js
```

## Scripts de développement

### Scripts principaux

```bash
# Development
npm run dev:candidate        # Lance l'app candidat en mode dev
npm run dev:recruiter        # Lance l'app recruteur en mode dev

# Build
npm run build:candidate      # Build l'app candidat
npm run build:recruiter      # Build l'app recruteur
npm run build:all           # Build les deux applications

# Preview
npm run preview:candidate    # Aperçu de l'app candidat buildée
npm run preview:recruiter    # Aperçu de l'app recruteur buildée
```

### Scripts de qualité

```bash
# Linting
npm run lint                # Lint sur tous les workspaces
npm run lint:fix            # Lint avec correction automatique

# Formatage
npm run format              # Formate tous les fichiers
npm run format:check        # Vérifie le formatage (CI)
npm run lint-and-format     # Lint + format en une commande

# Tests
npm run test               # Lance les tests sur tous les workspaces
npm run test:coverage      # Tests avec couverture de code
```

### Scripts utilitaires

```bash
# Nettoyage
npm run clean              # Supprime node_modules et builds

# Post-installation (automatique)
npm run postinstall:candidate   # Prépare l'environnement Quasar
npm run postinstall:recruiter   # Prépare l'environnement Quasar
```

## Configuration des outils

### ESLint

Configuration centralisée dans `eslint.config.mjs` avec :

- Support TypeScript complet
- Règles Vue.js spécifiques
- Intégration Prettier
- Variables d'environnement conditionnelles
- Règles personnalisées XpertSphere

### Prettier

Configuration dans `.prettierrc` :

- **Semi-colonnes** : Activées
- **Guillemets simples** : Préférés
- **Largeur de ligne** : 100 caractères
- **Indentation** : 2 espaces
- **Virgules finales** : ES5 compatible

### Lint-staged

Configuration automatique au commit :

```json
{
  "packages/**/*.{js,ts,vue,mjs}": [
    "eslint --fix",
    "prettier --write"
  ],
  "packages/**/*.{json,md,yml,yaml,html,css,scss}": [
    "prettier --write"
  ]
}
```

## Applications

### Candidate App

**Port** : 3000 (dev/preview)
**Description** : Interface publique pour les candidats

**Fonctionnalités principales** :

- Consultation des offres d'emploi
- Création de profil candidat
- Soumission de candidatures
- Suivi des candidatures

### Recruiter App  

**Port** : 3001 (dev/preview)
**Description** : Interface de gestion pour les équipes RH

**Fonctionnalités principales** :

- Gestion des offres d'emploi
- Traitement des candidatures
- Collaboration équipe RH
- Reporting et analytics

## Workflow de développement

### 1. Création d'une nouvelle fonctionnalité

```bash
# Créer une branche
git checkout -b feature/<nouvelle-fonctionnalite>

# Développer avec le serveur de développement
npm run dev:candidate  # ou dev:recruiter

# Tester le build
npm run build:candidate  # ou build:recruiter
```

### 2. Avant commit

Les hooks Husky automatisent :

- Linting avec correction automatique
- Formatage Prettier
- Vérifications TypeScript

### 3. Tests et validation

```bash
# Linting complet
npm run lint

# Vérification formatage
npm run format:check

# Build des deux apps
npm run build:all

# Tests
npm run test
```

## Troubleshooting

### Problèmes courants

#### Erreur Husky lors du commit

```bash
# Vérifier la configuration
git config core.hooksPath
# Doit afficher : src/frontend/.husky

# Reconfigurer si nécessaire
cd ../..
git config core.hooksPath src/frontend/.husky
```

### Erreurs de formatage CI

```bash
# Corriger localement
npm run format
git add .
git commit -m "fix: formatting"
```

### Problèmes de dépendances

```bash
# Nettoyage complet
npm run clean
npm ci
```

### Commandes de diagnostic

```bash
# Vérifier les versions
node --version
npm --version

# Vérifier la configuration
npm run lint --silent
npm run format:check --silent

# Vérifier les hooks
ls -la .husky/
cat .husky/pre-commit
```

## CI/CD

### Workflows GitHub Actions

**Pull Requests** :

- Installation des dépendances
- Vérification du formatage
- Linting complet
- Tests unitaires
- Build check

**Branches main/develop** :

- Validation complète
- Build Docker des applications
- Push vers Azure Container Registry
- Déploiement automatique

### Optimisations CI

- Utilisation du cache npm
- Build conditionnel selon les fichiers modifiés
- Parallélisation des vérifications
- Cleanup automatique des images Docker

## Contributions

### Standards de code

- **Composants Vue** : PascalCase pour les noms, kebab-case dans les templates
- **Variables TypeScript** : camelCase
- **Constantes** : SCREAMING_SNAKE_CASE
- **Fichiers** : kebab-case

### Commit messages

Utiliser le format conventionnel :

``` txt
feat(candidate): add job search functionality
fix(recruiter): resolve candidate filtering issue
docs(frontend): update setup instructions
```

### Avant de soumettre une PR

1. Vérifier que tous les tests passent
2. S'assurer que le formatage est correct
3. Documenter les nouvelles fonctionnalités
4. Tester les deux applications en local