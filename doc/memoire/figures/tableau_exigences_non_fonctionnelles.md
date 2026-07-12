---
figure: Tableau 2.2
titre: Exigences non fonctionnelles de XpertSphere
source_drawio: tableau_exigences_non_fonctionnelles.drawio
---

| Catégorie | Exigence | Critère mesurable ou vérifiable |
|-----------|----------|---------------------------------|
| Sécurité | Authentification robuste | Mots de passe hachés, protection contre les tentatives répétées, réinitialisation sécurisée |
| Sécurité | Autorisation granulaire | Chaque utilisateur limité à son périmètre, permissions attribuées par rôle |
| Sécurité | Isolation des données | Filtrage explicite par organisation sur toutes les requêtes, aucune fuite entre tenants vérifiable par test |
| Sécurité | Traçabilité des actions sensibles | Journal des consultations, modifications et suppressions de données personnelles conservé |
| Sécurité | Cycle de vie des données | Durées de conservation définissables, suppression ou anonymisation réalisable |
| Performance | Pagination systématique | Aucune liste chargée en totalité, pagination active sur toutes les vues de type liste |
| Performance | Chargement différé des documents | Les CV ne sont pas téléchargés à l'affichage d'une liste de candidatures |
| Performance | Légèreté de l'interface | Interface fonctionnelle sur connexion lente ou intermittente, sans requêtes multiples bloquantes |
| Utilisabilité | Parcours guidés | Tâches principales réalisables sans documentation préalable |
| Utilisabilité | Retours utilisateur explicites | Chaque message d'erreur indique la cause et la correction attendue |
| Utilisabilité | Accessibilité | Navigation au clavier, contraste visuel conforme, compatibilité avec les technologies d'assistance |
| Maintenabilité | Séparation des responsabilités | Une modification métier n'impose pas de modifier la couche d'accès aux données |
| Maintenabilité | Lisibilité du code | Architecture compréhensible par un nouveau contributeur sans reconstruction mentale complète |
| Évolutivité | Architecture ouverte | Tout composant peut être optimisé ou remplacé sans refonte de l'ensemble |
| Fiabilité (ISO 25010) | Cohérence transactionnelle | Les opérations critiques ne laissent pas de données partielles en cas d'échec |
| Compatibilité (ISO 25010) | Compatibilité navigateurs | Fonctionnel sur les navigateurs courants sans configuration particulière |
| Compatibilité (ISO 25010) | Ouverture aux intégrations | Architecture permettant un dialogue futur avec des systèmes RH ou de messagerie tiers |
| Portabilité (ISO 25010) | Reproductibilité locale | Environnement de développement reproductible sans effort excessif (Docker Compose) |
| Portabilité (ISO 25010) | Déploiement sans modification | Passage de l'environnement local à la production sans modification du code applicatif |
