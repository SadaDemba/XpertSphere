---
figure: Tableau A.4
titre: Étude comparative des solutions de déploiement Azure
---

```mermaid
---
title: Étude comparative des solutions de déploiement Azure (AAS / ACA / AKS)
config:
  theme: base
---
table
  columns: [Critères, "Azure App Service (AAS)", "Azure Container Apps (ACA)", "Azure Kubernetes Service (AKS)"]
  rows:
    - ["Déploiement simplifié", "Très simple (push GitHub, ZIP, FTP)", "Moyennement simple (conteneurs)", "Complexe (pipelines, YAML, Helm)"]
    - ["CI/CD GitHub Actions", "Intégration native", "Via Container Registry", "Configuration avancée requise"]
    - [Scalabilité, "Automatique (plan Premium)", "Auto-scale CPU / RAM", "Très fine mais manuelle"]
    - [Coût, Modéré, "Modéré (pay-per-use)", "Élevé (VMs + surcharge opérationnelle)"]
    - ["Simplicité de gestion", "Excellente (sans conteneur à gérer)", Moyenne, "Faible (cluster à maintenir)"]
    - ["Performance à charge élevée", Bonne, Excellente, Excellente]
    - [Monitoring, "Avancé (configuration requise)", Intégré, Intégré]
    - [Maintenance, "Très faible", "Faible (gérée par Microsoft)", "Élevée (mises à jour, sécurité)"]
    - ["Sécurité et conformité", "Bonne (certifiée, secrets intégrés)", Bonne, "Excellente (selon configuration)"]
    - ["Impact environnemental", "Variable", "Meilleur (scaling à zéro)", Variable]
```
