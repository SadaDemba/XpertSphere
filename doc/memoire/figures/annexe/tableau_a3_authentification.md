---
figure: Tableau A.3
titre: Étude comparative des solutions d'authentification
---

```mermaid
---
title: Étude comparative des solutions d'authentification (Azure AD / Keycloak / Okta / Firebase Auth)
config:
  theme: base
---
table
  columns: [Critères, "Azure AD (Entra ID)", Keycloak, Okta, "Firebase Auth"]
  rows:
    - [Sécurité, "Très élevée (ISO 27001)", "Élevée (open source, auditable)", "Très élevée (SOC 2, ISO)", "Élevée (infrastructure Google)"]
    - [Coût, "Modéré (B2C gratuit jusqu'à 50 K MAU)", Gratuit, "Élevé (facturation par utilisateur)", "Gratuit jusqu'à 10 K MAU"]
    - ["Intégration Azure", "Native (écosystème Microsoft)", "Possible, configuration requise", "Externe, possible", Limitée]
    - [Multi-tenant, "Excellente (B2B natif, isolation par tenant)", "Possible mais complexe", "Très bonne", Limitée]
    - ["SSO et MFA", "Natif (SSO, MFA, Identity Protection)", "Disponible, à configurer", Natif, Basique]
    - ["Conformité RGPD", "Excellente (datacenters UE disponibles)", "Dépend de l'hébergement", "Bonne, mais centré USA", "Moyenne (hébergement Google)"]
    - ["Personnalisation UI", "Bonne (Identity Experience Framework)", "Complète mais complexe", Moyenne, Limitée]
```
