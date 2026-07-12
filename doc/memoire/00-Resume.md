# Résumé

Le recrutement constitue un enjeu stratégique pour toute organisation, et les pratiques qui l'entourent sont en pleine mutation. Dans les pays d'Afrique subsaharienne, et au Sénégal en particulier, ce secteur se caractérise par un paradoxe persistant : une demande structurelle de recrutement en forte croissance, conjuguée à des pratiques encore largement manuelles et fragmentées. Les solutions internationales disponibles sur le marché supposent un niveau de maturité numérique et des moyens financiers que beaucoup d'organisations locales ne réunissent pas encore. Entre ces deux réalités, un espace existe pour une solution mieux adaptée.

Ce mémoire présente la conception et la réalisation de XpertSphere, une plateforme de suivi des candidatures (ATS) multi-entreprises conçue pour répondre aux contraintes spécifiques des marchés africains émergents. La problématique centrale porte sur les choix de conception et d'architecture permettant de développer un tel outil tout en respectant les exigences techniques et réglementaires d'un outil RH professionnel.

La démarche adoptée est à la fois analytique et applicative. Elle s'appuie sur une analyse du contexte et des solutions existantes, une formalisation des exigences fonctionnelles et non fonctionnelles, et une conception architecturale structurée autour de trois axes : la mutualisation de l'infrastructure via une architecture multi-tenant, la progressivité de l'adoption pour s'adapter à des organisations en transition numérique, et la conformité réglementaire au regard du cadre sénégalais et des standards internationaux de protection des données personnelles.

Sur le plan technique, XpertSphere repose sur un backend .NET avec SQL Server, deux interfaces Vue.js distinctes (candidats et utilisateurs internes), et un service Python dédié à l'analyse automatique des curricula vitae. Le déploiement est conteneurisé via Docker, avec une cible de production sur Azure.

Les résultats montrent qu'une architecture multi-tenant rigoureusement conçue permet de concilier mutualisation des coûts, isolation stricte des données entre organisations et adoption progressive, ouvrant ainsi des perspectives concrètes pour la modernisation des pratiques RH en Afrique subsaharienne.

---

**Mots-clés :** ATS, recrutement, multi-tenant, architecture logicielle, marchés émergents, Sénégal, conformité réglementaire, traçabilité, adoption progressive
