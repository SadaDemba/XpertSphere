# Chapitre 3 — Conception et architecture logicielle

La conception de XpertSphere repose sur un ensemble de décisions qui n'ont pas été prises isolément. Chacune répond à une contrainte identifiée au cours des chapitres précédents, qu'elle soit fonctionnelle, contextuelle ou réglementaire. Ce chapitre expose la logique architecturale du système, depuis les principes qui ont guidé les choix de structure jusqu'à la manière dont la plateforme gère la coexistence de plusieurs organisations dans un environnement partagé. L'objectif n'est pas de décrire chaque détail d'implémentation, mais de rendre lisible la cohérence d'ensemble qui relie les choix entre eux.

## 3.1 Principes architecturaux retenus

### 3.1.1 Modularité et découpage des responsabilités

L'architecture de XpertSphere suit un principe de séparation claire des responsabilités. Chaque composant du système est conçu pour exercer une fonction délimitée, sans empiéter sur celles des autres. Cette discipline, souvent désignée par le principe de responsabilité unique (*Single Responsibility Principle*), conduit à une organisation du code où les contrôleurs, les services et les modèles jouent chacun un rôle précis.

Le choix d'une architecture monolithique modulaire plutôt qu'une architecture en microservices a été motivé par plusieurs facteurs pragmatiques. Un monolithe bien structuré permet de réduire la complexité opérationnelle initiale, de faciliter les refactorisations transverses et de garantir une cohérence transactionnelle plus simple à gérer. La modularité est toutefois préservée au niveau du code pour permettre une éventuelle extraction de services (comme l'analyse de CV) si les besoins de scalabilité l'exigent ultérieurement.

Dans la pratique, cela se traduit par un découpage en couches. Les contrôleurs reçoivent les requêtes HTTP, effectuent les validations initiales et délèguent le traitement aux services applicatifs. Ces services concentrent la logique métier et s'appuient sur le contexte de données pour accéder aux entités persistées. Les modèles de domaine ne contiennent que les attributs et les propriétés calculées directement liées à l'entité qu'ils représentent, assurant une base saine pour la manipulation des données. Cette organisation limite les couplages et facilite la maintenance, car une modification dans les règles métier n'oblige pas à revoir les mécanismes d'accès aux données.

Ce découpage s'applique également à la séparation entre les interfaces et leurs implémentations. Chaque service expose une interface que les consommateurs utilisent. L'implémentation concrète reste cachée derrière cette interface, ce qui facilite les évolutions techniques et les tests, tout en rendant la structure du système lisible indépendamment de ses détails internes.

### 3.1.2 Injection de dépendances

L'injection de dépendances (*Dependency Injection*, DI) constitue un mécanisme structurant dans la conception de XpertSphere. Elle permet à chaque composant de recevoir ses collaborateurs au moment de sa création, sans en connaître l'implémentation exacte. Le framework ASP.NET Core, sur lequel repose la couche backend, intègre un conteneur d'injection de dépendances nativement. XpertSphere en tire parti pour enregistrer les services, les configurations et les dépendances d'infrastructure, puis les injecter là où ils sont nécessaires.

Cette approche apporte plusieurs bénéfices concrets. Elle évite les références statiques et les singletons fragiles. Elle rend chaque composant testable de manière isolée, car les dépendances peuvent être substituées par des versions de test sans modifier le code de production. Elle clarifie aussi les relations entre les composants, puisque les dépendances deviennent explicites dans les constructeurs plutôt que disséminées dans le code.

### 3.1.3 Pattern Repository et organisation des services

Le pattern Repository, tel qu'il est utilisé dans XpertSphere, consiste à faire transiter toutes les opérations d'accès aux données par des services dédiés. Chaque entité métier dispose d'un service correspondant, exposé via une interface. Ce service encapsule les requêtes, les validations et les transformations nécessaires, en faisant le lien entre la logique métier et le contexte de persistance. Les contrôleurs ne communiquent pas directement avec la base de données : ils passent par les services, qui gèrent eux-mêmes l'accès aux données via le contexte Entity Framework Core.

Cette organisation rend les règles d'accès plus faciles à centraliser et à vérifier. Elle évite notamment que des filtrages importants, comme l'isolation des données par organisation, soient oubliés dans certains contrôleurs. Le service devient un point de passage obligé, ce qui renforce la cohérence du traitement, en particulier pour les opérations sensibles.

### 3.1.4 Progressivité comme principe de conception

Un principe transversal oriente la conception de XpertSphere : la progressivité. La plateforme ne cherche pas à imposer un usage complet et immédiat. Elle est pensée pour que chaque fonctionnalité apporte de la valeur dès ses premiers usages, tout en laissant la possibilité d'aller plus loin à mesure que l'organisation gagne en maturité.

Ce principe se manifeste concrètement dans plusieurs aspects. Le profil candidat peut être complété progressivement, sans que son caractère incomplet bloque le processus de candidature. Les organisations peuvent opérer avec une configuration minimale, puis enrichir leurs paramètres. Les rôles et permissions peuvent être ajustés sans intervention technique. Dans la structure même du code, la séparation en modules permet d'introduire de nouvelles capacités, comme un service de communication ou un service d'intégration, sans perturber le cœur de l'application.

Cette logique répond directement au contexte décrit en introduction : les organisations visées ne sont pas toutes à un niveau de maturité numérique identique. Une architecture rigide qui exigerait une configuration complète avant d'être utile aurait peu de chances d'être adoptée durablement dans un environnement où la transition vers le numérique reste en cours.

## 3.2 Architecture globale du système

### 3.2.1 Vue d'ensemble

L'architecture de XpertSphere adopte une approche moderne et scalable, structurée autour d'un backend centralisé et de deux applications frontend distinctes. Cette organisation reflète la séparation des parcours utilisateurs : l'espace candidat et l'espace recruteur possèdent des besoins et des interactions spécifiques qui justifient deux interfaces indépendantes, tout en partageant le même socle d'API et de données.

Le diagramme suivant illustre l'organisation globale du système et son intégration dans l'écosystème cloud Azure.

![Architecture globale de XpertSphere](figures/architecture_diagram.png)

*Figure 3.1 — Architecture globale et écosystème cloud de XpertSphere*

Le schéma met en évidence une structure en couches protégée par une passerelle API (*Azure API Management*). On y observe la séparation entre les utilisateurs internes (recruteurs) et externes (candidats), chacun accédant à son application dédiée. Le backend est composé d'une API monolithique principale complétée par des services spécialisés pour le reporting, l'analyse de CV et la communication. L'ensemble s'appuie sur une infrastructure de données robuste comprenant SQL Database pour la persistance structurée, Azure Blob Storage pour les documents, et Redis pour les performances de cache. La sécurité est assurée par une gestion d'identité hybride via Microsoft Entra ID (B2B pour les entreprises, B2C pour les candidats).

### 3.2.2 Choix technologiques

Le stack technique de XpertSphere a été sélectionné pour assurer un équilibre entre performance, productivité et maintenabilité. L'écosystème .NET constitue le cœur du backend pour sa robustesse, tandis que Vue.js assure une interface fluide et réactive. FastAPI est utilisé pour le service spécialisé d'analyse de CV, tirant parti de ses performances pour les tâches de traitement de données. L'ensemble est hébergé sur le cloud Azure, offrant une infrastructure fiable et managée.

![Stack technique de XpertSphere](figures/stack_technique.png)

*Figure 3.2 — Stack technique principal de XpertSphere*

### 3.2.3 Couche backend : l'API centrale

Le backend de XpertSphere est conçu comme une API monolithique modulaire, développée avec ASP.NET Core. Ce choix correspond à la phase actuelle du projet : un monolithe bien structuré offre une cohérence plus simple à maintenir qu'une architecture distribuée, notamment lorsque les frontières de découpe ne sont pas encore totalement stabilisées. Le code est organisé de manière à ne pas créer de dépendances circulaires entre les modules fonctionnels, ce qui ménage une voie d'évolution vers une décomposition plus fine si le besoin devait se confirmer à l'usage.

La couche backend distingue plusieurs responsabilités bien délimitées. Les contrôleurs constituent le point d'entrée des requêtes. Chacun gère un périmètre fonctionnel précis : gestion des offres, des candidatures, des utilisateurs, des organisations, des rôles et permissions, ou encore de l'historique des statuts. Les services applicatifs concentrent la logique de traitement. Ils coordonnent les accès aux données, appliquent les règles métier et produisent les réponses que les contrôleurs renvoient aux clients. Le contexte de données, fondé sur Entity Framework Core, assure la persistance.

En dehors du noyau monolithique, plusieurs services spécialisés existent dans la structure du projet. Un service de communication, un service d'intégration, un service d'analyse de CV et un service de rapports sont identifiés comme modules distincts. Ces modules restent indépendants du cœur applicatif et peuvent évoluer à leur propre rythme. Leur présence dans l'architecture traduit une anticipation des besoins futurs, sans imposer une complexité prématurée dans la phase courante.

### 3.2.3 Couche frontend : deux applications distinctes

Le frontend est organisé en deux applications Vue.js indépendantes, développées dans un dépôt monorepo. La première est orientée candidat : elle couvre la découverte des offres, le dépôt de candidature et le suivi des candidatures déposées. La seconde est orientée recruteur et organisation : elle permet de gérer les offres, de suivre les candidatures, de piloter les rôles et de consulter les tableaux de bord.

Cette séparation présente plusieurs avantages. Elle limite la surface d'exposition de chaque application aux fonctionnalités réellement utiles à son audience. Elle réduit le risque de confusion entre des écrans et des permissions destinés à des rôles différents. Elle permet également de faire évoluer chaque interface à son propre rythme, selon les retours des utilisateurs correspondants.

### 3.2.4 Infrastructure et services transverses

La couche d'infrastructure couvre la persistance des données, la gestion des fichiers et les mécanismes d'authentification. En développement local, la persistance repose sur une base de données SQL Server, configurée via Docker Compose pour simplifier la mise en place de l'environnement. Les fichiers, tels que les CV, sont stockés via un service de blob, dont l'interface est abstraite de manière à pouvoir s'appuyer sur Azure Blob Storage dans un environnement de production sans modifier le code applicatif.

L'authentification est configurable. Deux modes coexistent dans la codebase : un mode local basé sur ASP.NET Identity, utilisé en développement, et un mode s'appuyant sur Microsoft Entra ID (anciennement Azure Active Directory), activé pour les environnements non locaux. Cette dualité permet de ne pas imposer une dépendance cloud pendant les phases de développement, tout en maintenant une trajectoire cohérente vers une infrastructure managée.

Dans les environnements hors développement, des services complémentaires s'ajoutent : Azure Key Vault pour la gestion des secrets, Application Insights pour la télémétrie et la supervision. Ces services ne sont pas simulés en local, mais leur intégration est préparée dans le code de configuration, de sorte que l'activation reste peu invasive.

## 3.3 Modélisation UML

### 3.3.1 Cas d'utilisation

Le diagramme de cas d'utilisation permet d'identifier les acteurs du système et les fonctionnalités auxquelles chacun a accès. Dans XpertSphere, quatre acteurs principaux interagissent avec le système. Le **candidat** peut consulter les offres, créer un compte, se connecter, postuler, suivre ses candidatures et passer des entretiens. Le **recruteur** gère le cycle de vie des offres (publication, clôture), personnalise les réponses et suit les statistiques. Le **manager** intervient dans la prise de décision, visualise les candidatures et peut taguer d'autres collaborateurs. Enfin, le **collaborateur** (ou cooptant) participe via la cooptation de candidats et le suivi des statistiques associées. Un espace d'échange commun permet la communication entre ces acteurs.

Le diagramme ci-dessous représente ces interactions au sein de la plateforme.

![Diagramme de cas d'utilisation](figures/use_case.png)

*Figure 3.3 — Diagramme de cas d'utilisation de XpertSphere*

### 3.3.2 Modèle de classes du domaine

Le modèle de domaine de XpertSphere s'articule autour de quatre piliers : l'organisation, le processus de recrutement, la gestion des identités et le système de droits.

L'entité `Organization` est la racine de l'isolation. Elle porte les informations de l'entreprise cliente (nom, secteur, taille) et sert de pivot pour toutes les données métier. La table `JobOffer` lui est directement rattachée, portant les détails des postes (titre, description, mode de travail, type de contrat). Le lien entre un candidat et une offre est matérialisé par l'entité `Application`.

L'entité `User` est polyvalente. Grâce à l'héritage d'ASP.NET Identity, elle gère l'authentification tout en portant des attributs métier (`Skills`, `CvPath`, `YearsOfExperience`). La distinction entre un utilisateur "interne" (recruteur, manager) et un "candidat" se fait par la propriété `OrganizationId`. Si elle est renseignée, l'utilisateur appartient à une entreprise précise ; sinon, il appartient au vivier transverse.

La gestion des droits repose sur un modèle RBAC complet. Un `User` possède des `UserRoles`, qui lient l'utilisateur à un `Role`. Chaque rôle contient une collection de `RolePermission`, faisant le pont avec l'entité `Permission`. Ce découpage permet une granularité fine, par exemple pour distinguer un recruteur ayant le droit de publier une offre d'un manager ayant seulement le droit de consulter les candidatures.

[À FAIRE — Figure 3.4 — Diagramme de classes UML du domaine métier (E/A)]

### 3.3.3 Écosystème et interactions globales

Au-delà des flux purement techniques, XpertSphere s'inscrit dans un écosystème global impliquant divers acteurs. L'équipe projet (chef de projet, développeurs, designer UI/UX) collabore étroitement pour traduire les besoins exprimés par les commanditaires (Département RH, Direction Expertime, Équipe commerciale). Cette synergie est alimentée par les retours des utilisateurs finaux et guidée par les exigences de conformité des régulateurs et partenaires techniques.

Le schéma suivant illustre ces interactions au sein de l'écosystème global du projet.

![Écosystème Global](figures/diagramme_interactions.png)

*Figure 3.5 — Écosystème global et interactions entre les parties prenantes*

### 3.3.4 Flux de données et séquences opérationnelles

Les diagrammes de séquence illustrent la dynamique du système en montrant comment les composants collaborent pour réaliser une fonction métier.

Dans le flux "Dépôt de candidature", le processus débute par une requête du candidat via l'interface Vue.js. Le contrôleur API reçoit les données, valide le format via FluentValidation, puis délègue au `ApplicationService`. Ce service vérifie plusieurs règles critiques : l'existence de l'offre, son état de publication et l'absence de candidature déjà existante pour ce candidat. Une fois ces vérifications franchies, l'entité `Application` est persistée, et une entrée initiale est créée dans le `ApplicationStatusHistory`. Le succès est ensuite notifié au frontend, qui met à jour l'interface du candidat.

Le flux "Analyse de CV" met en jeu un service spécialisé. Lorsqu'un document est chargé, l'API backend communique avec le service FastAPI dédié au parsing. Ce dernier utilise des techniques de traitement de texte pour extraire les entités nommées (compétences, expériences, diplômes). Les données structurées retournées permettent d'enrichir automatiquement le profil du candidat, illustrant l'intégration de services spécialisés au sein de l'architecture globale.

[À SOURCER — Détails techniques de l'algorithme de parsing]

[À FAIRE — Figure 3.6 : diagramme de séquence UML détaillé pour le flux "dépôt de candidature"]

[À FAIRE — Figure 3.7 : diagramme de séquence UML pour le flux "analyse de CV"]

### 3.3.5 Diagramme de déploiement

Le déploiement de XpertSphere est conçu pour assurer une transition fluide entre le développement et la production.

En environnement local, l'utilisation de Docker Compose permet de simuler l'infrastructure complète. Un conteneur SQL Server assure la persistance, tandis que les services backend et frontend s'exécutent dans des environnements isolés. Cette approche garantit que chaque membre de l'équipe travaille sur une configuration identique à celle qui sera testée, limitant les effets de bord liés aux spécificités des postes de travail.

Pour l'environnement cible sur Azure, l'architecture s'appuie sur des services managés (PaaS) pour réduire la charge d'exploitation. L'API est hébergée sur Azure App Service, bénéficiant d'une mise à l'échelle automatique. Les bases de données sont confiées à Azure SQL Database, qui gère nativement les sauvegardes et la haute disponibilité. Les assets (images, CV) sont stockés sur Azure Blob Storage, accessible via un CDN pour optimiser les performances d'accès au Sénégal et au-delà.

[À FAIRE — Figure 3.8 : diagramme de déploiement UML (environnement local Docker Compose + cible Azure)]

## 3.4 Stratégie de gestion multi-entreprises

### 3.4.1 Les patterns de multi-tenancy

La gestion multi-entreprises, désignée techniquement par le terme *multi-tenancy*, consiste à faire coexister plusieurs clients, ou *tenants*, au sein d'une même instance applicative, tout en garantissant l'isolation de leurs données. Trois patterns principaux structurent le débat dans ce domaine.

Le premier pattern consiste à attribuer à chaque entreprise une base de données dédiée. Cette approche offre le niveau d'isolation le plus élevé. Chaque client dispose d'un espace de stockage séparé, sans risque de collision ou d'exposition involontaire. En contrepartie, elle est coûteuse à opérer : multiplier les bases de données implique de multiplier les ressources, les sauvegardes et les opérations de maintenance. Elle convient davantage aux contextes où les exigences de conformité sont très strictes et où les clients disposent des moyens pour financer cette isolation.

Le deuxième pattern repose sur l'utilisation de schémas distincts au sein d'une même base de données. Chaque entreprise obtient son propre schéma, avec ses propres tables. L'isolation des données reste forte, sans exiger le coût d'une base par client. Cette approche présente néanmoins des complexités opérationnelles non négligeables, notamment lors des migrations de schéma, qui doivent être appliquées à chaque tenant de manière coordonnée.

Le troisième pattern, souvent appelé base mutualisée avec discriminant de tenant, consiste à stocker les données de toutes les entreprises dans les mêmes tables, en les distinguant par un identifiant d'appartenance. Chaque enregistrement porte un champ qui l'associe à une organisation. L'isolation est alors assurée par la logique applicative plutôt que par la structure de la base. Ce pattern est le moins coûteux à déployer et à maintenir. Il suppose en revanche une rigueur forte dans le code : toute requête qui omet le filtre d'organisation expose potentiellement des données d'une entreprise à une autre.

### 3.4.2 Le pattern retenu pour XpertSphere

XpertSphere adopte le troisième pattern : une base de données mutualisée avec un identifiant d'organisation porté par chaque entité rattachée à une entreprise. Ce choix est cohérent avec les contraintes économiques et opérationnelles du contexte visé. La plateforme s'adresse à des organisations qui cherchent à mutualiser les coûts d'infrastructure tout en conservant une autonomie fonctionnelle. Imposer une base dédiée par client rendrait le modèle économique peu viable pour des structures de taille intermédiaire.

Un compromis a été nécessaire pour gérer l'isolation. Si la base mutualisée simplifie le déploiement, elle transfère la responsabilité de l'étanchéité des données vers la couche applicative. Pour mitiger ce risque, XpertSphere utilise des filtres de requête globaux (Query Filters) au niveau de l'ORM (Entity Framework Core). Ces filtres sont appliqués systématiquement à toutes les requêtes portant sur des entités "tenant-aware", garantissant qu'un utilisateur ne puisse jamais accéder accidentellement aux données d'une autre organisation, même en cas d'oubli d'une clause `Where` dans le code de service.

Dans le modèle de données, cela se traduit concrètement par la présence d'un champ `OrganizationId` sur toutes les entités qui appartiennent à une organisation. La table `JobOffer`, par exemple, référence systématiquement l'organisation qui l'a créée. La table `Application` hérite de cette appartenance indirectement, via l'offre à laquelle elle est rattachée. Les utilisateurs membres d'une organisation portent eux aussi cet identifiant, ce qui permet de distinguer clairement les comptes candidats des comptes rattachés à une structure.

### 3.4.3 Mécanismes d'isolation

L'isolation des données ne repose pas uniquement sur la présence d'un champ discriminant. Elle exige une discipline d'application systématique dans toutes les couches qui accèdent aux données.

Au niveau des services, chaque opération de lecture ou de modification portant sur des données d'organisation intègre un filtre explicite. Cette règle de conception évite que des requêtes omettent le périmètre de l'organisation par inadvertance.

Au niveau des contrôleurs, les droits d'accès sont vérifiés avant toute délégation au service. Le service d'utilisateur courant fournit le contexte d'identité, notamment l'organisation d'appartenance, ce qui permet aux couches applicatives d'appliquer les règles de périmètre de manière transparente.

Le système de rôles et de permissions complète ce dispositif. XpertSphere distingue des rôles de plateforme et des rôles d'organisation. Les rôles de plateforme couvrent des actions d'administration transversales, tandis que les rôles d'organisation s'appliquent dans le périmètre d'une entreprise précise. Cette séparation évite qu'un utilisateur puisse, par escalade de privilèges ou par confusion de périmètre, agir sur des données qui n'appartiennent pas à son organisation.

### 3.4.4 Le vivier candidat : une logique transverse

Une tension conceptuelle mérite d'être explicitée. Le vivier candidat repose sur des profils qui existent indépendamment de toute organisation. Un candidat peut postuler à des offres de plusieurs entreprises, et son profil doit rester cohérent à travers ces candidatures. Cette logique est transverse par nature : elle ne se rattache pas à une seule organisation.

La solution adoptée consiste à distinguer deux niveaux dans le modèle. Le profil candidat, représenté par l'entité `User` sans identifiant d'organisation, constitue la partie partageable et réutilisable. La candidature, représentée par l'entité `Application`, rattache ce profil à une offre précise et appartient de facto à l'organisation qui a publié cette offre. Les informations propres au processus de sélection d'une organisation restent donc isolées dans la table des candidatures et dans l'historique des statuts, tandis que les informations de profil restent accessibles au candidat lui-même sans appartenir à une organisation particulière.

Cette distinction permet de concilier deux exigences qui semblent opposées : d'un côté, la capacité à construire un vivier candidat exploitable dans le temps ; de l'autre, l'isolation stricte des données de recrutement entre organisations. Elle est aussi cohérente avec les exigences réglementaires, puisque le profil candidat appartient à son titulaire et peut être géré, mis à jour ou supprimé indépendamment de ses candidatures passées.

### 3.4.5 Pertinence dans le contexte local

La mutualisation que permet le pattern retenu est particulièrement pertinente dans le contexte sénégalais. Une organisation de taille intermédiaire n'a généralement pas la capacité — ni l'intérêt — de financer une infrastructure dédiée pour gérer ses recrutements. Une plateforme partagée, où les coûts d'exploitation sont dilués sur l'ensemble des organisations clientes, répond mieux à cette réalité économique. Elle rend le service accessible à des acteurs qui seraient autrement contraints d'utiliser des outils génériques inadaptés ou de maintenir des processus entièrement manuels.

Cette logique de mutualisation ne doit pas être confondue avec une absence d'isolation. Les organisations partagent une infrastructure, pas leurs données. Chaque entreprise travaille dans un périmètre qui lui est propre, sans avoir accès aux offres, aux candidatures ni aux configurations d'une autre. L'architecture garantit cette séparation par conception, pas seulement par convention.

---

Ce chapitre a présenté les choix de conception qui structurent XpertSphere : les patterns architecturaux organisant le code, la découpe en composants répartissant les responsabilités, et la stratégie multi-entreprises permettant le partage de la plateforme sans sacrifier l'isolation. Ces décisions répondent aux exigences formulées précédemment et anticipent les contraintes opérationnelles. La partie suivante décrit comment ces choix architecturaux se traduisent dans la réalisation technique de la plateforme.
