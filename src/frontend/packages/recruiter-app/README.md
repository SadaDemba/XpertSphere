# XpertSphere Recruiter (@xpertsphere/recruiter-app)

Interface de gestion pour les équipes RH du système ATS XpertSphere.

## Fonctionnalités

- Gestion des offres d'emploi
- Traitement et évaluation des candidatures
- Collaboration entre équipes RH
- Analytics et reporting
- Gestion des processus de recrutement

## Développement

### Prérequis

Ce package fait partie du monorepo XpertSphere. Consultez la [documentation principale](../../README.md) pour la configuration complète.

### Commandes depuis la racine

```bash
# Développement avec hot reload
npm run dev:recruiter

# Build pour production
npm run build:recruiter

# Preview du build
npm run preview:recruiter
```

### Commandes locales

```bash
# Installation des dépendances
npm install

# Développement local
npm run dev

# Linting avec validation accessibilité
npm run lint

# Formatage
npm run format

# Build
npm run build
```

## Accessibilité

Cette application respecte les standards **RGAA/WCAG 2.1 AA** :

- Validation automatique via ESLint lors du développement
- Alertes en temps réel avec `vue-axe` en mode dev
- Contrôles bloquants dans la CI/CD

### Vérification accessibilité

```bash
# Validation complète (depuis la racine)
npm run lint

# En développement, ouvrir la console navigateur
# pour voir les rapports vue-axe automatiques
```

## Architecture

- **Framework** : Vue.js 3 + Quasar Framework
- **Language** : TypeScript
- **État** : Pinia
- **Routing** : Vue Router
- **i18n** : Vue i18n
- **HTTP** : Axios

## Configuration

Voir [Configuring quasar.config.js](https://v2.quasar.dev/quasar-cli-vite/quasar-config-js) pour la configuration Quasar.
