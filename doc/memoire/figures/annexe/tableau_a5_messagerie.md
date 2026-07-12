---
figure: Tableau A.5
titre: Étude comparative des solutions de messagerie asynchrone
---

```mermaid
---
title: Étude comparative des solutions de messagerie (Azure Service Bus / RabbitMQ / Kafka / Azure Queue Storage)
config:
  theme: base
---
table
  columns: [Critères, "Azure Service Bus", RabbitMQ, "Apache Kafka", "Azure Queue Storage"]
  rows:
    - [Débit, "Très bon", Bon, Excellent, Bon]
    - [Latence, "Très faible", "Très faible", Faible, Faible]
    - [Fiabilité, "Excellente (SLA 99,9 %)", Bonne, "Très bonne", Bonne]
    - ["Modèles de messagerie", "Complet (queue, topic, pub/sub)", Complet, "pub/sub", "Simple (queue)"]
    - ["Intégration Azure", Native, "Via conteneurs", "Via Event Hubs", Native]
    - [Complexité, Modérée, Élevée, "Très élevée", Faible]
    - [Coûts, Modérés, "Hébergement + maintenance", "Hébergement + maintenance", "Très faibles"]
    - [Scalabilité, Automatique, Manuelle, Complexe, Automatique]
    - [Durabilité, Excellente, Configurable, Excellente, Bonne]
    - ["Impact environnemental", "Optimisé (cloud mutualisé)", "Dépend de l'hébergement", "Consommation élevée", Faible]
```
