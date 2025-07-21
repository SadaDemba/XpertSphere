# Guide de contribution à XpertSphere

Ce document décrit les processus et conventions à suivre pour contribuer au projet XpertSphere. Veuillez lire attentivement ces directives avant de commencer à travailler sur le projet.

## 📝 Processus de développement

Nous suivons un workflow Git basé sur le modèle GitFlow :

1. **Création d'une branche** à partir de `develop` pour chaque fonctionnalité ou correction
2. **Développement et tests** dans cette branche
3. **Soumission d'une Pull Request** vers `develop`
4. **Revue de code** par au moins un autre développeur
5. **Fusion** dans `develop` une fois approuvée
6. Fusion périodique de `develop` vers `main` pour les releases

## 🌿 Conventions de branches

- **Branche principale** : `main` (production)
- **Branche de développement** : `develop`
- **Branches de fonctionnalités** : `feature/XS-123-description-courte`
- **Branches de correction** : `bugfix/XS-123-description-courte`
- **Branches de hotfix** : `hotfix/XS-123-description-courte`

## 💬 Conventions de commit

Nous utilisons [Conventional Commits](https://www.conventionalcommits.org/) pour standardiser nos messages de commit :

```
<type>(<scope>): <description>

[corps optionnel]

[footer(s) optionnel(s)]
```

### Types de commit
- `feat` : Nouvelle fonctionnalité
- `fix` : Correction de bug
- `docs` : Documentation uniquement
- `style` : Formatage, point-virgule manquant, etc.
- `refactor` : Refactorisation du code sans changement de fonctionnalité
- `perf` : Amélioration des performances
- `test` : Ajout ou correction de tests
- `build` : Modifications du système de build ou des dépendances
- `ci` : Modifications des fichiers de configuration CI
- `chore` : Autres changements qui ne modifient pas les fichiers src ou test

### Exemples
```
feat(auth): ajouter l'authentification avec Entra ID
fix(cv-parser): corriger l'extraction des dates d'expérience
docs(readme): mettre à jour les instructions d'installation
refactor(jobs): simplifier la logique de filtrage des offres
```

## 🧪 Tests

Tous les nouveaux développements doivent être accompagnés de tests appropriés :

- **Backend .NET** : Tests unitaires avec xUnit
- **Backend Python** : Tests unitaires avec pytest
- **Frontend** : Tests unitaires avec Vitest et tests de composants avec Testing Library

## 📏 Conventions de codage

### .NET
- Suivre les [conventions de nommage Microsoft](https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/naming-guidelines)
- Utiliser les fonctionnalités C# modernes (records, pattern matching, etc.)
- Privilégier les types immutables quand c'est possible
- Respecter les principes SOLID

### Python
- Suivre [PEP 8](https://www.python.org/dev/peps/pep-0008/)
- Utiliser des typages statiques avec les annotations de type
- Docstrings pour toutes les fonctions publiques

### Vue.js
- Utiliser la Composition API et les composables
- Suivre le Style Guide [Vue.js officiel](https://v3.vuejs.org/style-guide/)
- Préfixer les composants partagés avec `Xs`
- Utiliser TypeScript pour tous les composants et stores

## 🚀 Pull Requests

Pour soumettre une Pull Request :

1. Assurez-vous que votre code respecte les conventions de codage
2. Vérifiez que tous les tests passent
3. Mettez à jour la documentation si nécessaire
4. Remplissez le template de PR correctement
5. Demandez une revue à au moins un développeur
6. Répondez aux commentaires et apportez les modifications nécessaires

## 📚 Documentation

- Les fonctionnalités complexes doivent être documentées
- Mettez à jour le README.md si vous modifiez la configuration ou les processus d'installation
- Documentez les APIs avec Swagger pour le backend et JSDoc pour le frontend

## 💻 Environnement de développement

Pour mettre en place votre environnement de développement, suivez les instructions du fichier README.md à la racine du projet.

## 🔒 Sécurité

Si vous découvrez une vulnérabilité de sécurité, merci de bien vouloir envoyer un e-mail à [Sada Thiam](mailto:sadadembat9@gmail.com) au lieu de l'exposer publiquement.
