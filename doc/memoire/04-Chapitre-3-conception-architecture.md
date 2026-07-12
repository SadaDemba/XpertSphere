# Chapitre 3 : Conception et architecture logicielle

La conception de XpertSphere repose sur un ensemble de décisions qui n'ont pas été prises isolément. Chacune répond à une contrainte identifiée au cours des chapitres précédents, qu'elle soit fonctionnelle, contextuelle ou réglementaire. Ce chapitre expose la logique architecturale du système, depuis les principes qui ont guidé les choix de structure jusqu'à la manière dont la plateforme gère la coexistence de plusieurs organisations dans un environnement partagé. Le fil directeur : montrer comment les choix s'articulent plutôt que d'énumérer chaque détail d'implémentation.

## 3.1 Principes architecturaux retenus

### 3.1.1 Modularité et découpage des responsabilités

L'architecture de XpertSphere suit un principe de séparation claire des responsabilités. Chaque composant du système est conçu pour exercer une fonction délimitée, sans empiéter sur celles des autres. Cette discipline, souvent désignée par le principe de responsabilité unique (*Single Responsibility Principle*) [24], conduit à une organisation du code où les contrôleurs, les services et les modèles jouent chacun un rôle précis.

Le choix d'une architecture hybride répond à plusieurs facteurs pragmatiques. Un noyau monolithique modulaire prend en charge la logique métier principale, les API transverses et la gestion des données partagées. Des services spécialisés gravitent autour de ce noyau pour des fonctions aux contraintes distinctes : l'analyse de CV, la communication, l'intégration avec des systèmes tiers et le reporting. Cette organisation réduit la complexité opérationnelle du cœur applicatif, tout en laissant chaque composant périphérique évoluer à son propre rythme, avec les technologies les mieux adaptées à son cas d'usage.

Dans la pratique, cela se traduit par un découpage en couches. Les contrôleurs reçoivent les requêtes HTTP, effectuent les validations initiales et délèguent le traitement aux services applicatifs. Ces services concentrent la logique métier et s'appuient sur le contexte de données pour accéder aux entités persistées. Les modèles de domaine ne contiennent que les attributs et les propriétés calculées directement liées à l'entité qu'ils représentent. Cette organisation limite les couplages et facilite la maintenance, car une modification dans les règles métier n'oblige pas à revoir les mécanismes d'accès aux données.

Ce découpage s'applique également à la séparation entre les interfaces et leurs implémentations. Chaque service expose une interface que les consommateurs utilisent. L'implémentation concrète reste cachée derrière cette interface, ce qui facilite les évolutions techniques et les tests, tout en rendant la structure du système lisible indépendamment de ses détails internes.

### 3.1.2 Injection de dépendances

L'injection de dépendances (*Dependency Injection*, DI) est un mécanisme structurant dans la conception de XpertSphere. Elle permet à chaque composant de recevoir ses collaborateurs au moment de sa création, sans en connaître l'implémentation exacte. Le framework ASP.NET Core, sur lequel repose la couche backend, intègre un conteneur d'injection de dépendances nativement. XpertSphere en tire parti pour enregistrer les services, les configurations et les dépendances d'infrastructure, puis les injecter là où ils sont nécessaires.

Cette approche apporte plusieurs bénéfices concrets. Elle évite d'abord les références statiques et les singletons fragiles. Chaque composant devient testable de manière isolée, parce que les dépendances peuvent être substituées par des versions de test sans toucher au code de production ; les relations entre composants se lisent directement dans les constructeurs plutôt que d'être disséminées à travers le code.

### 3.1.3 Pattern Repository et organisation des services

Le pattern Repository, tel qu'il est utilisé dans XpertSphere, consiste à faire transiter toutes les opérations d'accès aux données par des services dédiés. Chaque entité métier dispose d'un service correspondant, exposé via une interface. Ce service encapsule les requêtes, les validations et les transformations nécessaires, en faisant le lien entre la logique métier et le contexte de persistance. Les contrôleurs ne communiquent pas directement avec la base de données : ils passent par les services, qui gèrent eux-mêmes l'accès aux données via le contexte Entity Framework Core.

Cette organisation rend les règles d'accès plus faciles à centraliser et à vérifier. Elle évite surtout que des filtrages importants, comme l'isolation des données par organisation, soient oubliés dans certains contrôleurs. Le service devient un point de passage obligé, ce qui renforce la cohérence du traitement, en particulier pour les opérations sensibles [23].

### 3.1.4 Progressivité comme principe de conception

Un principe transversal oriente la conception de XpertSphere : la progressivité. La plateforme ne cherche pas à imposer un usage complet et immédiat. Elle est pensée pour que chaque fonctionnalité apporte de la valeur dès ses premiers usages, tout en laissant la possibilité d'aller plus loin à mesure que l'organisation gagne en maturité.

Ce principe se manifeste à plusieurs niveaux. Le profil candidat peut être complété progressivement, sans que son caractère incomplet bloque le processus de candidature. Les organisations peuvent opérer avec une configuration minimale, puis enrichir leurs paramètres au fil du temps. Les rôles et permissions peuvent être ajustés sans intervention technique. Dans la structure même du code, la séparation en modules permet d'introduire de nouvelles capacités (un service de communication, un service d'intégration) sans perturber le cœur de l'application.

Cette logique répond directement au contexte décrit en introduction : les organisations visées ne sont pas toutes à un niveau de maturité numérique identique. Une architecture rigide qui exigerait une configuration complète avant d'être utile aurait peu de chances d'être adoptée durablement dans un environnement où la transition vers le numérique reste en cours.

## 3.2 Architecture globale du système

### 3.2.1 Vue d'ensemble

L'architecture de XpertSphere s'organise autour d'un backend centralisé et de deux applications frontend distinctes. Cette séparation reflète celle des parcours utilisateurs : l'espace candidat et l'espace recruteur ont des besoins suffisamment distincts pour justifier deux interfaces indépendantes, tout en partageant le même socle d'API et de données.

Le diagramme suivant illustre l'organisation globale du système et son intégration dans l'écosystème cloud Azure.

![Architecture globale de XpertSphere](figures/architecture_diagram.png)

*Figure 3.1 : Architecture globale et écosystème cloud de XpertSphere*

Le schéma fait apparaître une structure en couches protégée par une passerelle API (*Azure API Management*). On y distingue les utilisateurs internes (recruteurs) des utilisateurs externes (candidats), chacun accédant à son application dédiée. Le backend repose sur un noyau monolithique central, complété par des services spécialisés indépendants pour le reporting, l'analyse de CV, la communication et l'intégration externe. L'ensemble forme une architecture hybride dont les composants communiquent de façon asynchrone via Azure Service Bus. La couche de données comprend SQL Database pour la persistance structurée, Azure Blob Storage pour les documents et Redis pour le cache. La gestion des identités distingue deux populations : Microsoft Entra ID B2B pour les utilisateurs professionnels, B2C pour les candidats.

### 3.2.2 Choix technologiques

Le stack technique repose sur trois technologies dont la combinaison répond aux contraintes du projet : performance, productivité de développement et facilité de maintenance à long terme.

.NET (C#) structure le noyau backend. Ses performances compilées, son écosystème de sécurité natif (gestion des identités, JWT, validation de modèles) et son intégration avec les services Azure en font la base la plus adaptée à une application à forte logique métier. Vue.js prend en charge le frontend. Sa nature non prescriptive permet de construire deux interfaces distinctes sans imposer une architecture rigide ; chaque composant reste indépendant et réutilisable selon les besoins de chaque audience.

FastAPI complète ce duo pour les traitements de données. Construit en Python, il expose le service d'analyse de CV, qui délègue l'extraction des informations à Azure OpenAI plutôt que de reposer sur des bibliothèques de traitement du langage naturel entraînées en interne. Sa génération automatique de documentation simplifie l'interopérabilité avec le backend .NET. Cette répartition bimodale (logique métier en .NET, traitement IA en Python) est une des articulations centrales de l'architecture hybride. Une étude comparative des frameworks front-end et back-end envisagés est présentée en annexe (voir Tableaux A.1 et A.2).

![Stack technique de XpertSphere](figures/stack_technique.png)

*Figure 3.2 : Stack technique principal de XpertSphere*

### 3.2.3 Couche backend : noyau central et services spécialisés

Le backend de XpertSphere s'organise autour d'un noyau monolithique développé avec ASP.NET Core, auquel s'adjoignent des services spécialisés indépendants. Le noyau prend en charge la logique métier principale : gestion des offres, des candidatures, des utilisateurs, des organisations et des droits d'accès. Il est conçu de manière à éviter les dépendances circulaires entre modules fonctionnels, ce qui ménage une voie d'évolution si les besoins le justifient.

La couche backend distingue plusieurs responsabilités bien délimitées. Les contrôleurs forment le point d'entrée des requêtes HTTP ; chacun gère un périmètre fonctionnel précis (offres, candidatures, utilisateurs, organisations, rôles et permissions, historique des statuts). Les services applicatifs concentrent la logique de traitement.Ils coordonnent les accès aux données, appliquent les règles métier et produisent les réponses que les contrôleurs renvoient aux clients. Le contexte de données, fondé sur Entity Framework Core, assure la persistance.

En dehors du noyau, quatre services spécialisés complètent l'architecture : analyse de CV (en FastAPI), communication (notifications et emails), intégration avec des plateformes externes de diffusion d'offres, et reporting à destination des recruteurs. Chacun évolue indépendamment du cœur applicatif, avec ses propres cycles de déploiement.

La communication entre le noyau et ces services suit un modèle événementiel. Le monolithe publie des événements (confirmation de candidature, mise à jour de statut, email de relance) qu'Azure Service Bus achemine de façon asynchrone vers les services concernés. Ce découplage renforce la résilience de la plateforme. En effet, une indisponibilité temporaire du service de notification n'affecte pas le traitement des candidatures. Azure SignalR complète ce dispositif pour les notifications en temps réel ; les mises à jour parviennent aux utilisateurs connectés sans rechargement de page. Le choix d'Azure Service Bus face aux solutions de messagerie alternatives est documenté en annexe (voir Tableau A.5).

### 3.2.4 Couche frontend : deux applications distinctes

Le frontend est organisé en deux applications Vue.js indépendantes, développées dans un dépôt monorepo. La première est orientée candidat. Elle couvre la découverte des offres, le dépôt de candidature et le suivi des candidatures déposées. La seconde est orientée recruteur et organisation. Elle permet quant à elle de gérer les offres, de suivre les candidatures, de piloter les rôles et de consulter les tableaux de bord.

Cette séparation limite la surface d'exposition de chaque application aux fonctionnalités réellement utiles à son audience et réduit le risque de confusion entre écrans et permissions destinés à des rôles différents. Chaque interface peut aussi évoluer à son propre rythme, selon les retours de l'audience concernée.

### 3.2.5 Infrastructure et services transverses

La couche d'infrastructure couvre la persistance des données, la gestion des fichiers et les mécanismes d'authentification. En développement local, la persistance repose sur une base de données SQL Server, configurée via Docker Compose pour simplifier la mise en place de l'environnement. Les fichiers, tels que les CV, sont stockés via un service de blob, dont l'interface est abstraite de manière à pouvoir s'appuyer sur Azure Blob Storage dans un environnement de production sans modifier le code applicatif.

L'authentification est configurable. Deux modes coexistent dans la codebase : un mode local basé sur ASP.NET Identity, utilisé en développement, et un mode s'appuyant sur Microsoft Entra ID, activé pour les environnements non locaux. Cette dualité permet de ne pas imposer une dépendance cloud pendant les phases de développement, tout en maintenant une trajectoire cohérente vers une infrastructure managée.

La séparation B2B/B2C dans Microsoft Entra ID répond à une distinction fonctionnelle réelle. Les utilisateurs professionnels (recruteurs, managers) s'authentifient via le flux B2B, qui permet une délégation d'administration par organisation. Les candidats passent par le flux B2C, mieux adapté à une population externe et hétérogène, avec des parcours d'inscription et de récupération de compte plus souples. Cette séparation évite de faire coexister dans le même annuaire deux populations aux exigences de gestion très différentes. Le positionnement d'Azure AD face aux alternatives disponibles est détaillé en annexe (voir Tableau A.3).

En production, les composants applicatifs sont déployés sur Azure Container Apps, un service managé qui orchestre les conteneurs sans la complexité opérationnelle d'un cluster Kubernetes. Chaque service de l'architecture hybride bénéficie d'un déploiement indépendant et d'une mise à l'échelle automatique selon la charge. La gestion des secrets repose sur Azure Key Vault, tandis qu'Application Insights assure la télémétrie et la supervision. Ces services ne sont pas simulés en local, mais leur intégration est préparée dans le code de configuration de sorte que l'activation reste peu invasive. Une comparaison des options de déploiement Azure envisagées est disponible en annexe (voir Tableau A.4).

Les deux environnements, local et Azure, restent volontairement proches dans leur organisation. En local, Docker Compose orchestre l'ensemble des conteneurs applicatifs et d'infrastructure (backend, frontend, base de données, messagerie) dans des environnements isolés, ce qui permet à chaque membre de l'équipe de travailler sur une configuration reproductible, proche de celle qui sera testée en intégration. Sur Azure, cette même logique de conteneurs se retrouve dans Container Apps, mais les services d'infrastructure environnants changent de nature : SQL Server local devient Azure SQL Database, avec sauvegardes et haute disponibilité gérées nativement ; le stockage de fichiers s'appuie sur Azure Blob Storage, accessible via un CDN pour limiter la latence d'accès aux documents comme les CV. La correspondance entre les deux environnements est directe composant par composant, ce qui limite les surprises au moment du passage en production.

## 3.3 Modélisation UML

### 3.3.1 Modèle de classes du domaine

Le modèle de domaine de XpertSphere s'articule autour de quatre piliers : l'organisation, le processus de recrutement, la gestion des identités et le système de droits. Ces piliers se répartissent sur deux diagrammes distincts, l'un consacré au domaine métier du recrutement, l'autre au modèle de contrôle d'accès. Les deux logiques restent suffisamment indépendantes l'une de l'autre pour justifier cette scission en deux figures.

L'entité `Organization` est la racine de l'isolation. Elle porte les informations de l'entreprise cliente (nom, secteur, taille) et sert de pivot pour toutes les données métier. La table `JobOffer` lui est directement rattachée et porte les détails des postes (titre, description, mode de travail, type de contrat). Le lien entre un candidat et une offre est matérialisé par l'entité `Application`.

L'entité `User` est polyvalente. Grâce à l'héritage d'ASP.NET Identity, elle gère l'authentification et porte des attributs métier (`Skills`, `CvPath`, `YearsOfExperience`). La distinction entre un utilisateur interne (recruteur, manager) et un candidat se fait par la propriété `OrganizationId`. Si elle est renseignée, l'utilisateur appartient à une entreprise précise ; sinon, il appartient au vivier transverse.

![Diagramme de classes — Domaine recrutement](figures/figure_3_3a_domaine_recrutement.png)

*Figure 3.3a : Diagramme de classes UML du domaine recrutement*

La gestion des droits, elle, repose sur un modèle RBAC complet, représenté séparément. Un `User` possède des `UserRoles`, qui lient l'utilisateur à un `Role`. Chaque rôle contient une collection de `RolePermission`, qui fait le pont avec l'entité `Permission`. Ce découpage permet une granularité fine : il distingue par exemple un recruteur qui peut publier une offre d'un manager qui peut seulement consulter les candidatures.

![Diagramme de classes — Modèle de contrôle d'accès RBAC](figures/figure_3_3b_modele_rbac.png)

*Figure 3.3b : Diagramme de classes UML du modèle RBAC*

### 3.3.2 Flux de données et séquences opérationnelles

Les diagrammes de séquence décrivent comment les composants collaborent pour réaliser une fonction métier.

Le flux « Création de compte candidat » montre que l'analyse de CV n'existe pas comme une étape isolée : elle s'intègre directement dans le parcours d'inscription. Dès que le candidat téléverse son CV depuis l'interface Vue.js, le service ResumeAnalyzer (FastAPI) en extrait le texte brut, puis transmet ce contenu à Azure OpenAI accompagné d'un prompt d'extraction [20], [21]. Le modèle retourne des données structurées (compétences, expériences, formations) qui pré-remplissent le formulaire d'inscription ; en cas d'échec de l'extraction, un formulaire vide s'affiche et le parcours se poursuit normalement. Le candidat vérifie ces informations, les corrige au besoin, puis soumet le formulaire complet. Le contrôleur `AuthController` valide alors les données via FluentValidation et s'assure de l'unicité de l'adresse e-mail avant de persister le compte : cette dernière étape regroupe, dans une même transaction, l'enregistrement de l'utilisateur, le téléversement du CV vers Azure Blob Storage et l'insertion des expériences et formations extraites. La création de compte se conclut par une connexion automatique.

![Diagramme de séquence : création de compte candidat](figures/figure_3_4_sequence_creation_compte.png)

*Figure 3.4 : Diagramme de séquence : Création de compte candidat*

Le flux « Dépôt de candidature » intervient à un moment distinct du parcours, une fois le compte déjà constitué. Le candidat soumet sa candidature via l'interface Vue.js. Le contrôleur API reçoit les données, valide le format via FluentValidation, puis délègue au `ApplicationService`. Ce service applique plusieurs règles de validation : l'existence de l'offre, son état de publication, l'absence de candidature déjà enregistrée pour ce candidat. Une fois ces vérifications franchies, l'entité `Application` est persistée avec une entrée initiale dans le `ApplicationStatusHistory`. Le frontend est notifié du résultat et met à jour l'interface.

![Diagramme de séquence : dépôt de candidature](figures/figure_3_5_sequence_candidature.png)

*Figure 3.5 : Diagramme de séquence : Dépôt de candidature*

## 3.4 Stratégie de gestion multi-entreprises

### 3.4.1 Les patterns de multi-tenancy

La gestion multi-entreprises, désignée techniquement par le terme *multi-tenancy*, consiste à faire coexister plusieurs clients (ou *tenants*) au sein d'une même instance applicative en maintenant l'isolation de leurs données. Trois patterns principaux permettent d'aborder ce problème ; chacun représente un compromis différent entre niveau d'isolation et coût opérationnel [23], [26].

Le premier pattern consiste à attribuer à chaque entreprise une base de données dédiée. Cette approche offre le niveau d'isolation le plus élevé. Chaque client dispose d'un espace de stockage séparé, sans risque de collision ou d'exposition involontaire. En contrepartie, elle est coûteuse à opérer : multiplier les bases de données implique de multiplier les ressources, les sauvegardes et les opérations de maintenance. Elle convient davantage aux contextes où les exigences de conformité sont très strictes et où les clients disposent des moyens pour financer cette isolation.

Le deuxième pattern repose sur l'utilisation de schémas distincts au sein d'une même base de données. Chaque entreprise obtient son propre schéma, avec ses propres tables. L'isolation des données reste forte, sans exiger le coût d'une base par client. Cette approche présente néanmoins des complexités opérationnelles non négligeables : les migrations de schéma doivent être appliquées à chaque tenant de manière coordonnée.

Le troisième pattern, souvent appelé base mutualisée avec discriminant de tenant, consiste à stocker les données de toutes les entreprises dans les mêmes tables, en les distinguant par un identifiant d'appartenance. Chaque enregistrement porte un champ qui l'associe à une organisation. L'isolation est alors assurée par la logique applicative plutôt que par la structure de la base. Ce pattern est le moins coûteux à déployer et à maintenir. Il suppose en revanche une rigueur forte dans le code : toute requête qui omet le filtre d'organisation expose potentiellement des données d'une entreprise à une autre.

### 3.4.2 Le pattern retenu pour XpertSphere

XpertSphere adopte le troisième pattern : une base de données mutualisée avec un identifiant d'organisation porté par chaque entité rattachée à une entreprise. Ce choix est cohérent avec les contraintes économiques et opérationnelles du contexte visé. La plateforme s'adresse à des organisations qui cherchent à mutualiser les coûts d'infrastructure tout en conservant une autonomie fonctionnelle. Imposer une base dédiée par client rendrait le modèle économique peu viable pour des structures de taille intermédiaire.

Ce choix implique un compromis sur l'isolation. La base mutualisée simplifie le déploiement, mais transfère la responsabilité de l'étanchéité des données vers la couche applicative. Pour réduire ce risque, XpertSphere utilise des filtres de requête globaux (Query Filters) au niveau de l'ORM (Entity Framework Core). Ces filtres s'appliquent systématiquement à toutes les requêtes portant sur des entités appartenant à une organisation, de sorte qu'un utilisateur ne puisse jamais accéder accidentellement aux données d'une autre organisation, même en cas d'oubli d'une clause `Where` dans le code de service.

Dans le modèle de données, cela se traduit concrètement par la présence d'un champ `OrganizationId` sur toutes les entités qui appartiennent à une organisation. La table `JobOffer`, par exemple, référence systématiquement l'organisation qui l'a créée. La table `Application` hérite de cette appartenance indirectement, via l'offre à laquelle elle est rattachée. Les utilisateurs membres d'une organisation portent eux aussi cet identifiant, ce qui permet de distinguer clairement les comptes candidats des comptes rattachés à une structure.

### 3.4.3 Mécanismes d'isolation

L'isolation des données ne repose pas uniquement sur la présence d'un champ discriminant. Elle exige une discipline d'application systématique dans toutes les couches qui accèdent aux données.

Au niveau des services, chaque opération de lecture ou de modification portant sur des données d'organisation intègre un filtre explicite, ce qui évite qu'une requête omette le périmètre de l'organisation par inadvertance. Au niveau des contrôleurs, les droits d'accès sont vérifiés avant toute délégation au service. Le service d'utilisateur courant fournit le contexte d'identité (l'organisation d'appartenance, entre autres), ce qui permet aux couches applicatives d'appliquer les règles de périmètre de manière transparente.

Le système de rôles et de permissions complète ce dispositif. XpertSphere distingue des rôles de plateforme et des rôles d'organisation. Les rôles de plateforme couvrent des actions d'administration transversales, tandis que les rôles d'organisation s'appliquent dans le périmètre d'une entreprise précise. Cette séparation évite qu'un utilisateur puisse, par escalade de privilèges ou par confusion de périmètre, agir sur des données qui n'appartiennent pas à son organisation.

### 3.4.4 Le vivier candidat : une logique transverse

Une tension conceptuelle mérite d'être explicitée. Le vivier candidat repose sur des profils qui existent indépendamment de toute organisation : un candidat peut postuler à des offres de plusieurs entreprises, et son profil doit rester cohérent à travers ces candidatures successives.

La solution adoptée consiste à distinguer deux niveaux dans le modèle. Le profil candidat, représenté par l'entité `User` sans identifiant d'organisation, est la partie partageable et réutilisable. La candidature, représentée par l'entité `Application`, rattache ce profil à une offre précise et appartient de facto à l'organisation qui a publié cette offre. Les informations propres au processus de sélection d'une organisation restent donc isolées dans la table des candidatures et dans l'historique des statuts, tandis que les informations de profil restent accessibles au candidat lui-même sans appartenir à une organisation particulière.

Cette distinction permet de concilier deux exigences qui semblent opposées : d'un côté, la capacité à construire un vivier candidat exploitable dans le temps ; de l'autre, l'isolation stricte des données de recrutement entre organisations. Elle est aussi cohérente avec les exigences réglementaires, puisque le profil candidat appartient à son titulaire et peut être géré, mis à jour ou supprimé indépendamment de ses candidatures passées.

### 3.4.5 Pertinence dans le contexte local

La mutualisation que permet le pattern retenu correspond bien aux contraintes du contexte sénégalais. Une organisation de taille intermédiaire n'a généralement pas la capacité (ni l'intérêt) de financer une infrastructure dédiée pour gérer ses recrutements. Une plateforme partagée, où les coûts d'exploitation sont dilués sur l'ensemble des organisations clientes, répond mieux à cette réalité économique. Elle rend le service accessible à des acteurs qui seraient autrement contraints d'utiliser des outils génériques inadaptés ou de maintenir des processus entièrement manuels.

Cette logique de mutualisation ne doit pas être confondue avec une absence d'isolation. Les organisations partagent une infrastructure, pas leurs données : chaque entreprise travaille dans un périmètre qui lui est propre, sans avoir accès aux offres, aux candidatures ni aux configurations d'une autre. L'architecture garantit cette séparation de manière structurelle. Elle ne repose pas sur la vigilance ponctuelle des développeurs, mais sur des filtres appliqués systématiquement par l'ORM.

---

Les choix de conception présentés dans ce chapitre reposent sur deux logiques complémentaires. La première est structurelle : les patterns architecturaux organisent le code, distribuent les responsabilités entre les couches et séparent les composants selon leurs cycles de vie propres. La seconde est fonctionnelle : la stratégie multi-entreprises rend la plateforme partageable sans sacrifier l'isolation des données. Ces décisions répondent aux exigences formulées dans les chapitres précédents et anticipent les contraintes d'un déploiement progressif. Le chapitre suivant décrit comment elles se concrétisent dans la réalisation technique.
