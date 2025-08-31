# XpertSphere Candidate (@xpertsphere/candidate-app)

Interface publique pour les candidats du système ATS XpertSphere.

## Fonctionnalités

- Consultation des offres d'emploi
- Création et gestion de profil candidat
- Soumission de candidatures
- Suivi des candidatures en temps réel

## Développement

### Prérequis

Ce package fait partie du monorepo XpertSphere. Consultez la [documentation principale](../../README.md) pour la configuration complète.

### Commandes depuis la racine

```bash
# Développement avec hot reload
npm run dev:candidate

# Build pour production
npm run build:candidate

# Preview du build
npm run preview:candidate
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
