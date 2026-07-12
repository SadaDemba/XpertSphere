# Annexes

## Annexe A : Études comparatives des choix technologiques

Les tableaux suivants documentent les études comparatives menées lors de la phase de conception. Chaque tableau met en regard les principales alternatives envisagées sur des critères techniques et opérationnels pertinents pour le contexte du projet.

### A.1 Étude comparative des frameworks front-end

Le choix de Vue.js comme framework front-end a été guidé par sa courbe d'apprentissage faible et sa flexibilité architecturale, deux critères déterminants pour un projet développé par une équipe réduite avec des délais contraints. Le tableau ci-dessous compare Vue.js aux deux principales alternatives du marché.

![Étude comparative des frameworks front-end](figures/annexe/tableau_a1_frontend.png)

*Tableau A.1 : Étude comparative des frameworks front-end (React / Angular / Vue.js)*

### A.2 Étude comparative des frameworks back-end

La sélection de .NET (C#) pour le noyau métier et de FastAPI (Python) pour les traitements IA repose sur une complémentarité technique : performances compilées pour la logique applicatrice, écosystème Python pour l'analyse de données. Le tableau suivant replace ces choix dans le panorama des solutions disponibles.

![Étude comparative des frameworks back-end](figures/annexe/tableau_a2_backend.png)

*Tableau A.2 : Étude comparative des frameworks back-end (Node.js / Spring Boot / .NET / FastAPI / Django)*

### A.3 Étude comparative des solutions d'authentification

Le recours à Microsoft Entra ID (anciennement Azure AD) répond à plusieurs contraintes simultanées : gestion multi-tenant native, séparation B2B / B2C, et intégration étroite avec l'infrastructure Azure. Les alternatives ci-dessous ont été évaluées sur ces mêmes critères.

![Étude comparative des solutions d'authentification](figures/annexe/tableau_a3_authentification.png)

*Tableau A.3 : Étude comparative des solutions d'authentification (Azure AD / Keycloak / Okta / Firebase Auth)*

### A.4 Étude comparative des solutions de déploiement Azure

Azure Container Apps a été retenu pour sa capacité à orchestrer des conteneurs indépendants avec un scaling automatique, sans la complexité opérationnelle d'un cluster Kubernetes. Le tableau ci-dessous compare les trois options natives Azure envisagées.

![Étude comparative des solutions de déploiement Azure](figures/annexe/tableau_a4_deploiement.png)

*Tableau A.4 : Étude comparative des solutions de déploiement Azure (AAS / ACA / AKS)*

### A.5 Étude comparative des solutions de messagerie asynchrone

La communication événementielle entre le noyau monolithique et les services spécialisés repose sur Azure Service Bus. Ce choix privilégie l'intégration native avec l'écosystème Azure et la fiabilité de livraison. Le tableau suivant positionne cette solution face aux alternatives couramment utilisées.

![Étude comparative des solutions de messagerie asynchrone](figures/annexe/tableau_a5_messagerie.png)

*Tableau A.5 : Étude comparative des solutions de messagerie asynchrone (Azure Service Bus / RabbitMQ / Apache Kafka / Azure Queue Storage)*
