Plan détaillé — Ancrage sénégalais

Introduction générale
Présenter le contexte du marché du travail au Sénégal et l'état de la digitalisation des RH
Souligner le paradoxe : forte croissance démographique et active, mais processus de recrutement encore largement manuels ou informels
Poser la problématique : Dans quelle mesure une architecture logicielle modulaire et multi-tenant peut-elle répondre aux contraintes spécifiques du marché sénégalais tout en garantissant la traçabilité, l'automatisation et la conformité réglementaire des processus de recrutement ?
Annoncer les objectifs du mémoire et la démarche adoptée
Présenter la structure du document

Chapitre 1 : Analyse du contexte et de l'existant

1.1) Enjeux techniques des plateformes ATS
Définir ce qu'est un ATS et son rôle dans le cycle de recrutement
Décrire les enjeux globaux : volume de candidatures, multi-canal, traçabilité, expérience candidat
Identifier les défis techniques : scalabilité, interopérabilité, performance, sécurité des données
Spécificité sénégalaise : faible taux d'adoption des outils RH numériques, prépondérance des recrutements par réseau et bouche-à-oreille, usage dominant du CV papier ou email

1.2) Analyse des solutions du marché
Présenter et comparer les solutions existantes à l'international (Workday, Greenhouse, Lever, solutions open source)
Focus Afrique/Sénégal : état des outils utilisés localement (Jobberman, Emploi.sn, solutions maison), faible pénétration des ATS structurés
Grille de comparaison sur des critères techniques et fonctionnels
Pistes pour une solution adaptée : identification des critères d'une solution réaliste pour le contexte local

1.3) Limites observées et problématiques techniques
Limites des solutions internationales : coût élevé, interface en langue étrangère, inadaptation aux pratiques locales, dépendance vendor
Limites des pratiques locales : absence de traçabilité, perte d'information, inégalité de traitement des candidats, délais longs
Problématique technique résultante : formuler les exigences de conception qui découlent des limites observées

1.4) Cadre réglementaire
RGPD comme référence internationale et son influence croissante
Cadre sénégalais : loi n°2008-12 sur la protection des données personnelles, rôle de la Commission de Protection des Données Personnelles (CDP)
Droits des candidats, durée de conservation des données, consentement
Implications techniques pour la conception (anonymisation, suppression, logs d'accès)
Positionnement de XpertSphere vis-à-vis de ces contraintes



Chapitre 2 : Spécifications et positionnement de la solution proposée

2.1) Positionnement de XpertSphere face aux solutions existantes
Synthèse des écarts observés (allusion aux constats du Ch1, sans re-démonstration)
Orientations stratégiques : progressivité, mutualisation, ancrage local
Mécanisme du lien partageable : unifier la collecte des candidatures sans remplacer les canaux existants
Tableau de positionnement comparatif

2.2) Exigences fonctionnelles
Lister les fonctionnalités attendues : gestion des offres, suivi des candidatures, espace candidat, tableau de bord recruteur
Adaptations au contexte sénégalais : interface simple et accessible (utilisateurs peu habitués aux outils RH), support du français, processus guidés pour accompagner la prise en main
Prioriser avec la méthode MoSCoW (Must, Should, Could, Won't)
Présenter les acteurs du système et leurs interactions

2.3) Exigences non fonctionnelles
Performance, disponibilité, sécurité, maintenabilité, portabilité
Spécificité locale : tolérance aux connexions intermittentes, légèreté de l'interface, compatibilité avec des configurations matérielles modestes
Critères de qualité logicielle retenus (ISO 25010)

2.4) Contraintes techniques et hypothèses de conception
Stack technologique retenu (.NET, Vue.js, SQL Server, Python/FastAPI) — choix énoncés comme contraintes, justification architecturale en Ch3
Environnement local (Docker) pour le développement et la démonstration
Architecture cible sur Azure présentée comme perspective (sans déploiement effectif) — détail en Ch4
Hypothèses de volumétrie et d'usage (nombre d'organisations simulées, volume de données)
Hypothèse de transition : plateforme conçue pour des organisations en phase de numérisation, pas uniquement pour des structures déjà matures

Chapitre 3 : Conception et architecture logicielle

3.1) Principes architecturaux retenus
Justifier le choix d'une architecture modulaire (microservices ou modules découplés)
Présenter les design patterns appliqués (Repository, CQRS, DI, etc.)
Principe de progressivité : architecture pensée pour évoluer sans rupture, permettant une adoption progressive par des entreprises sénégalaises en cours de digitalisation
Expliquer les critères de découpe en services/modules

3.2) Architecture globale du système
Diagramme d'architecture global (couches, composants, flux de données)
Description des principaux composants et de leurs responsabilités
Présentation de l'architecture cible Azure (schéma sans implémentation)

3.3) Modélisation UML
Diagrammes de cas d'utilisation (par acteur)
Diagramme de classes du domaine métier
Diagrammes de séquence pour les flux principaux (création de compte candidat avec analyse de CV, dépôt de candidature)

3.4) Stratégie de gestion multi-entreprises
Présenter les patterns de multi-tenancy (base partagée, schéma par tenant, base par tenant)
Justifier le pattern retenu pour XpertSphere
Expliquer la gestion de l'isolation des données, des droits et des configurations par entreprise
Pertinence dans le contexte local : le multi-tenancy répond aussi à un besoin local de mutualisation des coûts — une seule plateforme partagée est plus viable économiquement qu'une solution dédiée par entreprise

Chapitre 4 : Réalisation technique

4.1) Organisation du système et implémentation des services applicatifs
Présenter la structure du projet (organisation des dossiers, couches applicatives)
Décrire l'implémentation des principaux services back-end (gestion des offres, candidatures, utilisateurs)
Illustrer avec des extraits de code significatifs

4.2) Implémentation du Front-end
Présenter les choix techniques front-end
Décrire les principales interfaces (tableau de bord recruteur, espace candidat, formulaire de candidature)
Considérations UX : interfaces pensées pour des utilisateurs non experts, ergonomie simple, guidage à l'écran
Illustrer avec des captures d'écran et extraits de code

4.3) Automatisation du traitement des CVs
Décrire le mécanisme de parsing/extraction d'informations depuis les CVs
Présenter les outils ou librairies utilisés
Hétérogénéité des CVs : les CVs ont souvent des structures variables et moins standardisées — mentionner comment le système gère cette hétérogénéité
Expliquer l'intégration dans le flux de candidature

4.4) Tests et validation
Présenter la stratégie de test retenue (unitaires, intégration, manuels)
Présenter les outils utilisés et quelques cas de test représentatifs
Donner un bilan qualitatif de la couverture obtenue



Chapitre 5 : Analyse critique et perspectives

5.1) Apports techniques de XpertSphere
Synthétiser les contributions techniques : modularité, automatisation, multi-tenancy
Apport contextuel : valeur ajoutée spécifique pour le marché sénégalais — structuration des processus, traçabilité, réduction des biais informels
Comparer avec les solutions du marché analysées au Chapitre 1

5.2) Évaluation qualitative de l'architecture
Analyser les forces de l'architecture retenue (maintenabilité, extensibilité, séparation des responsabilités)
Identifier les faiblesses ou compromis effectués

5.3 Limites du travail réalisé
Environnement local uniquement : impact sur les tests de charge et la haute disponibilité
Fonctionnalités non implémentées ou partiellement couvertes
Limite contextuelle : absence de validation terrain avec de vraies entreprises sénégalaises, résistance culturelle à la dématérialisation non mesurée empiriquement
Ce que le déploiement Azure apporterait concrètement

5.4 Perspectives d'évolution
Déploiement cloud et passage à l'échelle
Stratégie d'adoption locale : accompagnement au changement, formation des équipes RH, version mobile (usage fort du mobile)
Enrichissement de l'IA (matching CV/offre, scoring automatique, support multilingue français/wolof à terme)
Intégration avec des acteurs locaux (plateformes d'emploi sénégalaises, ANPEJ, agences de recrutement)
Ouverture vers une API publique pour les entreprises clientes



Conclusion générale
Rappeler la problématique et les objectifs initiaux
Faire le bilan de ce qui a été réalisé et validé
Réaffirmer la pertinence de la solution dans le contexte sénégalais et sa capacité à accompagner la transition numérique des RH
Ouvrir sur les perspectives d'évolution et la valeur du projet dans un contexte réel

