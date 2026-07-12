# Chapitre 5 : Analyse critique et perspectives

Les chapitres précédents ont décrit la conception et l'implémentation d'une plateforme de gestion du recrutement ancrée dans le contexte sénégalais. Ce chapitre en tire un bilan : apports techniques de XpertSphere, évaluation des choix architecturaux, limites de la réalisation actuelle. Les perspectives d'évolution sont examinées en dernier lieu, du marché sénégalais vers d'autres contextes africains.

## 5.1 Apports techniques de XpertSphere

### 5.1.1 Structuration du processus de recrutement

XpertSphere répond à un besoin de structuration observé dans des organisations qui recrutent régulièrement sans disposer d'outils adaptés. Quand les candidatures arrivent par email, que les CV s'accumulent dans des dossiers dispersés et que le suivi repose sur la mémoire collective, le processus devient opaque et difficile à piloter. La plateforme centralise ces informations et offre une représentation claire de l'état du recrutement.

Le pipeline s'organise autour de statuts correspondant aux étapes courantes d'un processus de sélection. Une candidature entre dans le système avec un statut initial, progresse selon les décisions prises, et conserve un historique exploitable. Ce mécanisme produit une traçabilité que les pratiques manuelles ne permettent pas : un recruteur peut retrouver rapidement où en est un dossier, comprendre comment la décision s'est construite, et justifier un arbitrage plusieurs semaines après les faits.

La coordination bénéficie aussi de cette structuration. Lorsque plusieurs personnes interviennent dans un recrutement, les échanges informels se multiplient et les informations se dispersent. XpertSphere concentre ces échanges dans un espace partagé où chacun retrouve le contexte sans multiplier les demandes de confirmation. Recruteurs, managers et évaluateurs consultent les mêmes informations, ce qui réduit les décalages et accélère les arbitrages.

Cet apport va au-delà de la simple numérisation, comme l'analyse du chapitre 1 le laissait entendre. La plateforme introduit un cadre qui rend le processus reproductible sans imposer un modèle rigide. Une organisation peut adapter ses pratiques en restant dans un environnement qui conserve la mémoire des décisions et garantit que les informations ne se perdent pas.

### 5.1.2 Mutualisation et isolation multi-tenant

La capacité à mutualiser une infrastructure tout en garantissant l'isolation des données est un apport qui répond à une contrainte économique réelle. Dans un contexte où les moyens sont souvent limités, une infrastructure partagée permet de diluer les coûts d'hébergement, de maintenance et d'évolution, tout en offrant à chaque organisation un environnement fonctionnellement équivalent à une installation dédiée.

L'implémentation repose sur un filtrage rigoureux au niveau applicatif. Chaque entité métier susceptible d'appartenir à une organisation possède une propriété `OrganizationId` qui conditionne l'accès. Les requêtes intègrent systématiquement ce filtre, de sorte qu'une organisation ne puisse jamais consulter les offres, les candidatures ou les utilisateurs d'une autre. Cette logique se double de contrôles d'autorisation au niveau des services métier, ce qui crée une défense en profondeur contre les erreurs de configuration ou les contournements involontaires.

Ce choix diffère des modèles où chaque client dispose d'une base de données dédiée. La mutualisation complète simplifie l'exploitation et facilite les montées de version. Mais elle impose une rigueur forte sur la conception des requêtes et sur la gestion des rôles. Un oubli de filtre peut créer une fuite d'informations entre organisations. Le code source reflète cette vigilance : les services métier rendent explicites les règles de filtrage, et les tests d'intégration vérifient que l'isolation fonctionne correctement.

Ce modèle offre une alternative viable pour des organisations qui ne peuvent pas supporter le coût d'une infrastructure dédiée, ce qui correspond bien aux contraintes identifiées au chapitre 1.

### 5.1.3 Automatisation du traitement des CV

Le traitement automatisé des curriculum vitae réduit la charge de travail manuel des recruteurs. Lorsqu'une offre génère plusieurs dizaines de candidatures, la simple consultation des CV peut occuper une part importante du temps disponible. L'analyse automatique extrait les informations structurées (expériences, formations, compétences) et les rend directement exploitables dans la plateforme.

Le service d'analyse fonctionne de manière autonome, développé en Python et exposé via une API FastAPI. Cette séparation facilite l'évolution des technologies d'extraction sans impacter le reste de la plateforme. Elle permet aussi de dimensionner cette charge de travail indépendamment du backend principal, ce qui devient utile lorsque le volume de candidatures augmente.

L'intégration avec Azure OpenAI apporte une capacité de compréhension que les approches purement syntaxiques n'atteignent pas. Le modèle de langage identifie les sections d'un CV, normalise les dates, catégorise les expériences et extrait les compétences, même lorsque les formats varient fortement. Cette robustesse face à l'hétérogénéité des documents est un atout dans un contexte où les candidats n'utilisent pas tous les mêmes conventions de rédaction.

L'approche par prompt engineering facilite les ajustements. Les tests initiaux ont révélé des imprécisions dans l'extraction de certaines informations ; des modifications du prompt ont permis d'améliorer la qualité des résultats sans modifier le code applicatif. Cette flexibilité réduit les cycles de développement. Elle ouvre aussi la voie à l'adaptation à de nouveaux formats ou à des langues complémentaires.

L'utilisation de GPT-4o-mini offre un compromis entre qualité d'extraction et coût par requête. Les tests ont montré que ce modèle produit des résultats exploitables pour la grande majorité des CV rencontrés, sans nécessiter le recours à des modèles plus coûteux dont les capacités supplémentaires ne seraient pas mobilisées.

### 5.1.4 Gestion duale de l'authentification

La gestion duale de l'authentification sert deux objectifs distincts. En environnement local, le système s'appuie sur des jetons JWT générés après validation des identifiants, ce qui simplifie les tests et permet de reproduire le système sans dépendre d'une infrastructure externe.

En production, l'intégration avec Azure Entra ID offre une authentification unique et une gestion centralisée des identités. Les utilisateurs d'une organisation se connectent avec leurs identifiants d'entreprise, sans mot de passe supplémentaire à mémoriser. Les droits se gèrent dans l'annuaire d'entreprise et se répercutent immédiatement dans la plateforme.

La mise en œuvre repose sur un middleware d'enrichissement des revendications. Ce middleware détecte le mode d'authentification, extrait les informations utiles et ajoute des revendications complémentaires issues de la base de données locale. Contrôleurs et services métier s'appuient toujours sur les mêmes revendications, qu'elles proviennent d'un JWT local ou d'un jeton Entra ID enrichi. Ce modèle d'autorisation unifié évite de maintenir deux chemins parallèles et réduit les risques lors du passage d'un environnement à l'autre.

### 5.1.5 Intégration continue et automatisation du déploiement

L'automatisation du cycle de développement dépasse le simple confort d'usage. Les workflows GitHub Actions orchestrent la validation du code, l'exécution des tests, la construction des artefacts et leur déploiement vers l'environnement cible. Cette chaîne réduit les risques d'erreur humaine et accélère les cycles de livraison.

La configuration reflète la structure modulaire du système. Chaque composant dispose de son propre workflow, déclenché uniquement lorsque des modifications le concernent. Une modification du service d'analyse de CV déclenche le workflow Python sans toucher aux applications frontend. Cette granularité évite de reconstruire l'ensemble du système à chaque changement.

Chaque modification déclenche aussi l'exécution de la suite de tests correspondante, ce qui garantit qu'aucune régression n'est introduite. La validation automatique renforce la confiance dans les modifications et permet de détecter rapidement les problèmes d'intégration. À cela s'ajoute un bénéfice de documentation : le processus de construction devient lisible dans le code des workflows, ce qui facilite la reprise du projet par de nouveaux contributeurs.

### 5.1.6 Synthèse comparative

Le tableau suivant synthétise les apports de XpertSphere par rapport aux solutions du marché et aux pratiques locales observées au chapitre 1.

[À COMPLÉTER — Tableau comparatif : Solutions internationales / Pratiques locales / XpertSphere selon les critères : structuration du pipeline, traçabilité, mutualisation, automatisation CV, accessibilité économique, adaptation contexte local]

Ces apports se renforcent mutuellement. La structuration du pipeline améliore la traçabilité. L'automatisation du traitement des CV facilite la montée en volume. Le modèle multi-tenant rend l'ensemble économiquement accessible. L'intégration continue garantit que la plateforme évolue sans rupture. Pris ensemble, ces éléments répondent de façon cohérente aux limites identifiées dans l'analyse du contexte.

XpertSphere ne cherche pas à remplacer les grandes plateformes internationales lorsque celles-ci correspondent aux besoins et aux moyens d'une organisation. Il s'adresse aux structures en phase de transition qui cherchent à structurer leurs processus sans imposer une transformation radicale des pratiques. Cette position intermédiaire, entre les solutions d'entreprise complexes et les pratiques manuelles, correspond à un besoin observé sur le marché sénégalais et se retrouve dans de nombreux autres contextes africains où les organisations cherchent à moderniser leurs pratiques RH avec des ressources limitées.

## 5.2 Évaluation qualitative de l'architecture

### 5.2.1 Séparation des responsabilités et maintenabilité

L'organisation du code source sépare clairement les responsabilités entre les couches applicatives. Le backend distingue les contrôleurs (qui gèrent le cycle requête-réponse HTTP) des services métier (qui encapsulent la logique applicative). Les entités du domaine représentent les concepts métier sans porter de logique technique. Cette séparation facilite la compréhension du système et réduit les couplages qui rendraient les évolutions coûteuses.

Un développeur qui rejoint le projet peut identifier rapidement où se trouve une fonctionnalité. La gestion des offres d'emploi se concentre dans `JobOfferService`. La logique d'authentification s'organise autour de `AuthenticationService`. Les règles de validation se définissent dans des classes dédiées via FluentValidation. Cette organisation réduit le risque de duplication et limite la surface à modifier lorsqu'une règle métier évolue.

La maintenabilité bénéficie aussi de l'injection de dépendances. Les services déclarent leurs dépendances via des interfaces, ce qui facilite le remplacement d'une implémentation par une autre. Le service de stockage de fichiers expose une interface `IBlobStorageService` qui abstrait les détails d'implémentation. Remplacer Azure Blob Storage par un autre fournisseur ne nécessite que la création d'une nouvelle implémentation de cette interface, sans modifier les services métier qui l'utilisent.

Cette approche a un coût initial. La multiplication des abstractions peut ralentir le développement de nouvelles fonctionnalités, surtout lorsque les besoins ne sont pas encore stabilisés. XpertSphere retient des abstractions là où elles apportent une valeur claire (en particulier sur les points d'intégration avec des services externes), et conserve une conception plus directe sur les mécanismes internes bien maîtrisés. L'enjeu est d'éviter une sur-ingénierie qui ajouterait de la complexité sans bénéfice démontré.

### 5.2.2 Modularité et extensibilité

La structure en composants autonomes facilite l'évolution sélective du système. Le service d'analyse de CV fonctionne indépendamment du reste de la plateforme : il expose une API documentée et peut être développé, testé et déployé sans impact sur le backend principal ou sur les interfaces frontend. Cela ouvre la voie à des évolutions technologiques (remplacement du modèle de langage, ajout de nouvelles langues, amélioration de l'extraction) sans déstabiliser l'ensemble.

Les applications frontend suivent une logique similaire. L'application candidat et l'application recruteur partagent des services communs pour la communication avec l'API, mais restent fonctionnellement distinctes. Un changement dans l'interface recruteur ne déclenche pas une reconstruction de l'application candidat. Cette séparation réduit les risques de régression croisée et raccourcit les cycles de développement.

L'extensibilité se manifeste aussi dans la capacité à ajouter de nouvelles fonctionnalités sans remettre en cause l'architecture existante. Un système de notifications, par exemple, pourrait s'intégrer en ajoutant un service dédié qui observe les événements du système et envoie des alertes selon des règles configurables. Ce service s'inscrirait dans l'architecture actuelle sans modifier les services métier existants.

Cette modularité comporte toutefois des limites dans un environnement local. Les avantages d'une architecture distribuée (mise à l'échelle indépendante, déploiement sélectif, résilience partielle) ne se matérialisent pleinement que dans un environnement cloud où les composants peuvent s'exécuter sur des infrastructures séparées. En local, tous les composants partagent les mêmes ressources, ce qui limite les bénéfices opérationnels.

### 5.2.3 Gestion de l'état et cohérence des données

La cohérence des données repose sur plusieurs mécanismes complémentaires. Les contraintes d'intégrité référentielle, définies au niveau de la base de données, garantissent que les relations entre entités restent valides : une candidature ne peut pas exister sans offre d'emploi associée, un utilisateur organisationnel ne peut pas être créé sans lien vers une organisation existante. Ces contraintes forment un premier niveau de protection contre les incohérences.

Le pattern Unit of Work [23], intégré nativement à Entity Framework Core, assure la cohérence transactionnelle des opérations. Lorsqu'un service crée une offre d'emploi et enregistre plusieurs entités associées, soit l'ensemble de l'opération réussit, soit elle échoue sans laisser de données partielles. Cette garantie simplifie la gestion des erreurs et évite les situations où le système se retrouverait dans un état incohérent après une panne.

Les validations métier complètent ces mécanismes. Avant de persister une entité, les services vérifient que les règles métier sont respectées. Une offre ne peut pas avoir une date de clôture antérieure à sa date de publication ; un candidat ne peut pas postuler deux fois à la même offre. Ces règles s'expriment de manière déclarative dans les validateurs FluentValidation [28], ce qui les rend explicites et testables indépendamment du reste du code.

Du côté frontend, la gestion de l'état s'appuie sur Pinia [29], qui centralise l'état global et garantit que les modifications se propagent de manière cohérente aux composants abonnés. Cette architecture réactive évite les décalages où différentes parties de l'interface afficheraient des informations contradictoires sur le même objet métier.

Un point de vigilance subsiste sur la cohérence entre le backend et le frontend. Lorsqu'une modification est effectuée par un utilisateur, l'interface met à jour son état local immédiatement pour offrir une réactivité perçue, puis synchronise avec le serveur. Si la requête serveur échoue, l'interface doit annuler la modification locale et restaurer l'état précédent. Cette logique de compensation fonctionne correctement pour les opérations simples, mais pourrait devenir difficile à maintenir dans des scénarios collaboratifs où plusieurs utilisateurs modifient simultanément les mêmes données.

### 5.2.4 Sécurité par conception

La sécurité s'intègre à plusieurs niveaux de l'architecture. L'authentification vérifie l'identité des utilisateurs avant tout accès au système. Les mots de passe sont hachés avec un algorithme fourni par ASP.NET Core Identity et ne sont jamais stockés en clair. Les jetons JWT portent une durée de vie limitée et sont renouvelés régulièrement, ce qui réduit la fenêtre d'exposition en cas de compromission.

L'autorisation granulaire filtre les actions selon les rôles [25]. Les attributs `[Authorize]` au niveau des contrôleurs spécifient les rôles requis pour accéder à une route ; les services métier vérifient ensuite que l'utilisateur dispose des droits nécessaires sur les ressources consultées ou modifiées. Cette double vérification limite les risques d'escalade de privilèges.

Le filtrage multi-tenant garantit que chaque organisation accède uniquement à ses propres données. Cette isolation repose sur un filtrage explicite des requêtes, appliqué dans les services métier. Les tests d'intégration vérifient que ce mécanisme fonctionne et qu'aucune fuite de données n'est possible entre organisations.

La traçabilité des actions sensibles s'appuie sur un système de journalisation structurée. Les opérations critiques (consultation de données personnelles, modification de statut, suppression d'information) génèrent des événements qui conservent le contexte de l'action. Cette traçabilité facilite les audits de sécurité.

Les fichiers téléchargés par les candidats transitent par Azure Blob Storage, avec chiffrement au repos et en transit. Les URL d'accès sont générées avec des signatures temporaires qui limitent la fenêtre d'exploitation en cas d'interception. Cette approche réduit la surface d'attaque et évite d'exposer les clés d'accès au stockage.

Ces protections laissent cependant certaines menaces non traitées dans la version actuelle. La plateforme ne met pas en œuvre de limitation de débit sur les tentatives d'authentification, ce qui la rend vulnérable aux attaques par force brute. Les mécanismes de détection d'intrusion ou d'analyse comportementale ne sont pas implémentés. Un filtrage réseau plus fin constituerait un prolongement naturel de ces mesures : la restriction des accès organisationnels par plage d'adresses IP, combinée à la mise en place d'un pare-feu applicatif web pour atténuer les attaques volumétriques, renforcerait la surface de protection sans nécessiter de modification architecturale majeure. Ces améliorations relèvent d'une évolution vers un environnement de production où l'exposition augmente.

### 5.2.5 Performance et optimisation des requêtes

Les performances observées en environnement local restent satisfaisantes pour les volumes de test utilisés. Les listes d'offres et de candidatures se chargent rapidement grâce à la pagination systématique et aux index définis sur les colonnes fréquemment filtrées. Les recherches s'appuient sur des requêtes LINQ qui se traduisent efficacement en SQL, évitant de charger en mémoire des ensembles de données inutiles.

Entity Framework Core facilite le développement en abstrayant les détails d'accès aux données, mais introduit parfois des inefficacités. Les requêtes générées automatiquement peuvent inclure des jointures inutiles ou charger des entités liées non exploitées. Une vigilance est nécessaire pour détecter ces cas. Les outils de profilage intégrés à Entity Framework facilitent cette analyse en révélant les requêtes coûteuses et les charges redondantes.

Les CV ne sont pas téléchargés lors de l'affichage d'une liste de candidatures : l'interface ne charge le fichier que lorsque l'utilisateur clique explicitement pour le consulter. Cette approche réduit la bande passante consommée et accélère l'affichage initial, ce qui importe dans des contextes où la connectivité est limitée.

L'analyse de CV par modèle de langage introduit une latence de quelques secondes. Cette durée reste acceptable dans le contexte d'un dépôt de candidature. L'approche asynchrone permet de confirmer immédiatement la réception de la candidature, l'analyse se déroulant en arrière-plan. Le candidat reçoit une confirmation rapide, même si l'extraction des informations structurées prend quelques secondes supplémentaires.

Les tests de charge formels, simulant une utilisation simultanée par de nombreux utilisateurs, n'ont pas été réalisés. Ces tests nécessitent une infrastructure représentative de l'environnement de production et des outils spécialisés qui dépassent le périmètre d'une preuve de concept. Les performances observées suggèrent que l'architecture retenue peut supporter une charge modérée, mais une validation rigoureuse reste nécessaire avant un déploiement en production.

### 5.2.6 Compromis effectués et dette technique

Plusieurs compromis ont été faits pour équilibrer la qualité technique et les contraintes d'un projet de mémoire. La couverture de tests, estimée à 70 % sur les services métier, reste en deçà des standards de l'industrie pour des applications critiques. Cet arbitrage est conscient : l'effort a été concentré sur la validation des mécanismes essentiels plutôt que sur une couverture exhaustive. Une couverture plus élevée serait nécessaire avant une mise en production.

La gestion des erreurs, structurée autour d'un middleware global, pourrait être affinée pour certains cas limites. Les erreurs de validation s'affichent correctement à l'utilisateur ; les exceptions techniques génèrent des messages génériques pour éviter d'exposer des détails sensibles. En revanche, certaines situations d'erreur réseau ou de service temporairement indisponible ne bénéficient pas d'une gestion spécifique avec mécanismes de retry ou de circuit breaker. Ces patterns deviendraient utiles dans un environnement distribué où les défaillances transitoires sont plus fréquentes.

La configuration du système repose actuellement sur des fichiers de configuration et des variables d'environnement. Cette approche fonctionne bien pour un nombre limité d'environnements, mais devient difficile à gérer lorsque plusieurs environnements coexistent avec des configurations différentes. Une gestion centralisée via Azure App Configuration ou un outil équivalent faciliterait les déploiements multiples et réduirait les risques d'erreur de configuration.

Le service d'analyse de CV utilise un cache simple pour éviter de retraiter un même fichier. Ce mécanisme fonctionne pour des cas basiques, mais ne gère pas l'invalidation lorsque les règles d'extraction évoluent. Une approche plus complète inclurait un versionnement des prompts et une invalidation automatique lorsque la version change.

Ces compromis forment une dette technique à adresser avant une mise en production. Ils ne remettent pas en cause la validité de l'architecture, mais ils identifient les zones où un effort supplémentaire sera nécessaire pour atteindre un niveau de fiabilité adapté à un usage réel.

## 5.3 Limites du travail réalisé

### 5.3.1 Validation limitée de la scalabilité en conditions réelles

XpertSphere est déployé sur Azure, mais la validation technique reste limitée par l'absence de charge représentative d'un usage réel. Le déploiement actuel fonctionne dans un environnement de démonstration qui ne reproduit pas les conditions d'une exploitation en production avec de multiples organisations clientes et des volumes significatifs de candidatures.

Les tests de performance n'ont pas été conduits à grande échelle. La plateforme a été testée avec quelques dizaines d'offres et quelques centaines de candidatures, ce qui reste modeste par rapport aux volumes qu'une organisation active pourrait générer, ou qu'un déploiement multi-tenant avec plusieurs dizaines d'entreprises clientes induirait. Les mécanismes de pagination et d'indexation fonctionnent correctement à cette échelle, mais leur comportement sous charge élevée n'a pas été vérifié. Une dégradation progressive des performances pourrait apparaître lorsque le volume de données et le nombre d'utilisateurs simultanés augmentent.

La haute disponibilité n'a pas été testée dans des conditions de défaillance réelles. Bien que l'infrastructure Azure offre des mécanismes de résilience intégrés, leur comportement effectif en cas de défaillance partielle ou de pic de charge n'a pas été validé. Les mécanismes de gestion des pannes transitoires, de retry automatique ou de circuit breaker n'ont pas été sollicités dans des scénarios de stress réels.

Les performances en conditions de connectivité dégradée n'ont pas non plus été mesurées de manière systématique. Les optimisations mises en place (chargement différé, pagination, cache côté client) visent à améliorer l'expérience sur des connexions lentes ou instables, mais leur efficacité n'a pas été validée avec des utilisateurs réels dans des contextes où la bande passante est limitée.

### 5.3.2 Service d'intégration avec les plateformes d'emploi

L'intégration automatique avec les plateformes d'emploi externes (Jobberman, Emploi.sn, BrighterMonday) n'a pas été développée dans cette phase du projet. Une telle fonctionnalité permettrait de publier automatiquement les offres créées dans XpertSphere sur ces plateformes tierces, élargissant leur visibilité sans effort manuel supplémentaire.

Le développement d'un service d'intégration dédié nécessite d'abord l'accès aux API publiques de ces plateformes, ce qui dépend de partenariats commerciaux qui dépassent le cadre technique du projet. La conception actuelle ménage toutefois cette évolution : l'architecture modulaire permet d'ajouter un service d'intégration qui observerait les événements de publication d'offres et déclencherait la diffusion vers les canaux configurés, sans modifier le cœur applicatif.

### 5.3.3 Absence de validation terrain avec de vraies organisations

XpertSphere a été développé sans participation directe d'organisations dans la phase de conception et de validation. Les exigences fonctionnelles s'appuient sur une analyse documentaire du contexte sénégalais et sur une lecture des pratiques de recrutement en Afrique subsaharienne, mais elles n'ont pas été confrontées à des retours d'utilisateurs réels. Cette absence de validation terrain limite la confiance dans l'adéquation de la solution aux besoins réels.

Les hypothèses formulées au chapitre 2 (progressivité, adaptation aux pratiques locales, accessibilité) reposent sur un raisonnement logique et des observations indirectes, mais elles n'ont pas été vérifiées empiriquement. Une organisation réelle pourrait rencontrer des frictions que l'analyse n'a pas anticipées. Les interfaces, pensées pour être simples et accessibles, pourraient se révéler déroutantes pour des utilisateurs peu habitués aux outils numériques. Les workflows, conçus pour rester flexibles, pourraient ne pas correspondre aux pratiques réellement observées dans les structures visées.

La résistance culturelle à la dématérialisation n'a pas non plus été mesurée. L'analyse du contexte suggère que les pratiques manuelles et les recrutements par réseau occupent une place importante. Introduire un système formel pourrait rencontrer des réticences qui ne se manifesteraient qu'à l'usage, lorsque les utilisateurs perçoivent la plateforme comme une contrainte plutôt que comme une aide. Ces questions d'adoption dépassent le cadre technique et nécessiteraient un accompagnement qui n'a pas été modélisé dans le projet.

Une phase pilote avec quelques organisations volontaires aurait permis de valider les choix de conception, d'ajuster les interfaces et d'identifier les points de friction. Cette étape reste indispensable avant une diffusion plus large, mais elle n'a pas pu être conduite dans le cadre de ce mémoire.

### 5.3.4 Modèle économique et viabilité à grande échelle

L'intégration avec Azure OpenAI pour l'analyse de CV introduit une dépendance à un service externe payant. Chaque CV analysé génère un coût qui varie selon le volume de texte traité et le modèle utilisé. Le choix de GPT-4o-mini limite ces coûts, mais ils deviennent un poste de dépense non négligeable à grande échelle. Une organisation qui reçoit plusieurs centaines de candidatures par mois générerait des frais récurrents à intégrer dans le modèle d'abonnement.

Ce modèle économique n'a pas été formalisé dans le projet actuel. Il nécessiterait une étude de marché pour déterminer le prix acceptable pour les organisations cibles et vérifier que ce prix couvre les coûts d'exploitation (infrastructure Azure, analyse de CV, stockage) tout en restant accessible. L'équilibre entre accessibilité et viabilité économique conditionnera le succès commercial de la plateforme.

## 5.4 Perspectives d'évolution

### 5.4.1 Optimisation du déploiement cloud et passage à l'échelle

XpertSphere est déjà déployé sur Azure, mais la configuration actuelle reste dimensionnée pour un usage de démonstration. Le passage vers une exploitation à grande échelle nécessitera des ajustements progressifs de l'infrastructure existante.

La mise à l'échelle sera guidée par l'adoption réelle. La configuration actuelle repose sur une instance unique de chaque service, ce qui suffit pour les tests et la validation initiale. Lorsque le volume augmentera avec l'arrivée de nouvelles organisations clientes, les services critiques (l'API backend, le service d'analyse de CV) pourront être répliqués horizontalement pour distribuer la charge. Azure Container Apps permet cette réplication sans modification du code applicatif, en s'appuyant sur son système de load balancing intégré.

La base de données peut également évoluer selon la charge. Azure SQL Database propose plusieurs niveaux de service qui ajustent les ressources allouées (CPU, mémoire, IOPS) selon les besoins, et un passage d'un niveau à l'autre s'effectue sans interruption de service. Cette flexibilité évite de sur-dimensionner l'infrastructure dès le démarrage tout en permettant d'absorber une croissance progressive.

Le modèle économique bénéficie de cette approche. Les coûts d'infrastructure augmentent proportionnellement à l'usage réel, ce qui aligne les dépenses sur la valeur générée. Cette logique correspond bien au modèle multi-tenant, où chaque nouvelle organisation cliente augmente la charge globale sans nécessiter une infrastructure dédiée.

### 5.4.2 Stratégie d'adoption et accompagnement au changement

La qualité technique ne suffit pas. Le succès de XpertSphere dépend aussi de sa capacité à être adopté par des structures habituées à des pratiques manuelles ou informelles, au Sénégal dans un premier temps, puis dans l'ensemble de l'Afrique subsaharienne. Introduire un système formel dans ce contexte nécessite un accompagnement qui dépasse le simple accès à la plateforme.

Une phase pilote avec quelques organisations volontaires au Sénégal constituerait un premier jalon. Ce marché sert de terrain de validation avant une expansion vers d'autres pays africains francophones, puis vers les marchés anglophones. Cette phase permettrait de valider les hypothèses de conception, d'identifier les frictions non anticipées et d'ajuster les interfaces selon les retours d'usage réel. Les organisations pilotes joueraient un rôle de co-conception, en apportant un regard critique qui enrichit la compréhension du contexte au-delà de l'analyse documentaire initiale.

La formation des équipes RH est un levier d'adoption déterminant. Un outil perçu comme complexe ou mal compris risque d'être sous-utilisé, même lorsqu'il apporte objectivement de la valeur. Des sessions de formation ciblées, adaptées aux rôles (recruteurs, managers, administrateurs), faciliteraient la prise en main. Ces formations ne se limiteraient pas à expliquer les fonctionnalités ; elles montreraient aussi comment la plateforme s'intègre dans les processus existants, réduisant ainsi la perception de rupture.

Un support réactif renforcerait cette dynamique. Les premières semaines d'utilisation génèrent souvent des questions et des besoins d'ajustement. Un canal de support accessible (email, chat, voire accompagnement sur site pour les organisations pilotes) faciliterait la résolution des blocages. La documentation complète ce dispositif : des guides illustrés, des tutoriels vidéo et des FAQ couvrant les scénarios courants, rédigés dans un langage accessible pour les utilisateurs peu familiers avec les outils numériques.

### 5.4.3 Enrichissement de l'intelligence artificielle

L'analyse automatique de CV est une première application de l'intelligence artificielle dans XpertSphere. Cette capacité peut être étendue pour apporter davantage de valeur aux recruteurs et améliorer l'efficacité du processus de sélection.

Un système de correspondance automatique entre CV et offres d'emploi réduirait le temps consacré au tri initial des candidatures. Le service d'analyse extrait déjà les compétences, les expériences et les formations. Ces informations pourraient être comparées aux exigences de l'offre pour calculer un score d'adéquation. Ce score ne remplacerait pas le jugement humain, mais il aiderait les recruteurs à prioriser la consultation des dossiers les plus adaptés lorsque le volume de candidatures est élevé.

Cet algorithme pourrait s'affiner progressivement en s'appuyant sur les décisions effectivement prises par les recruteurs. Si une candidature avec un score modéré est régulièrement retenue pour un entretien, le modèle ajusterait ses critères pour mieux refléter les préférences réelles de l'organisation. Cet apprentissage devrait être encadré pour éviter de reproduire des biais discriminatoires. C'est sans doute le point le plus délicat de cette évolution.

Le scoring automatique des candidatures prolonge cette logique. Au-delà du simple matching, un modèle pourrait évaluer la qualité globale d'une candidature en croisant plusieurs dimensions : cohérence du parcours, durée des expériences, adéquation des compétences, qualité rédactionnelle du CV. Ce scoring resterait indicatif, mais il faciliterait la gestion de volumes élevés en orientant l'attention vers les profils qui méritent une analyse approfondie.

Le support multilingue ouvre une troisième perspective. Le français domine actuellement, ce qui correspond au marché sénégalais. L'ajout du wolof élargirait l'accessibilité locale. Plus stratégiquement, le support de l'anglais devient nécessaire pour une expansion vers les marchés anglophones d'Afrique de l'Ouest et de l'Est (Nigeria, Ghana, Kenya, Afrique du Sud) qui représentent des volumes de recrutement significatifs. Les modèles de langage récents ont des capacités multilingues qui rendraient cette évolution techniquement réalisable ; l'effort porterait principalement sur la traduction des interfaces et l'adaptation des prompts d'analyse de CV.

Un assistant conversationnel intégré à l'interface candidat prolongerait ces capacités vers une dimension relationnelle. Les candidats posent souvent les mêmes questions : déroulement du processus, critères de sélection, délais de réponse. Un modèle de langage entraîné sur ces informations pourrait en traiter une large part automatiquement, réduisant la charge des équipes RH sans dégrader l'expérience. Cette disponibilité permanente prendrait un relief particulier dans un contexte où les délais de réponse peuvent décourager des candidats qui ont peu d'autres canaux pour obtenir une information fiable.

### 5.4.4 Intégration avec l'écosystème de l'emploi

XpertSphere gagnerait en valeur en dialoguant avec les acteurs déjà établis sur les marchés de l'emploi africains. Les plateformes de diffusion d'offres (Jobberman présent dans plusieurs pays d'Afrique de l'Ouest, Emploi.sn au Sénégal, BrighterMonday en Afrique de l'Est) touchent un large public de candidats. Une intégration permettrait de publier automatiquement les offres créées dans XpertSphere sur ces plateformes, élargissant leur visibilité sans effort manuel supplémentaire.

Cette intégration suppose des API publiques documentées et accessibles. Si ces API existent, le développement d'un connecteur reste techniquement simple. XpertSphere publierait l'offre via l'API de la plateforme partenaire, et les candidats recevraient un lien redirigeant vers l'application candidat de XpertSphere. Cette approche conserve la centralisation du suivi tout en bénéficiant de la portée des plateformes établies.

Une collaboration avec les agences nationales pour l'emploi, comme l'ANPEJ au Sénégal, ou avec des cabinets de recrutement régionaux pourrait également enrichir l'écosystème. Ces acteurs disposent de bases de profils et d'expertises sectorielles qui complèteraient les capacités de XpertSphere. Une intégration pourrait prendre la forme d'un accès partagé au vivier candidat, encadré par des règles strictes de confidentialité et de consentement, ou d'une mise en relation facilitée entre organisations clientes et cabinets spécialisés.

La signature électronique des documents prolonge cette logique vers l'aval du recrutement. Une fois un candidat retenu, la transmission et la signature des contrats, accords de confidentialité ou documents d'intégration peuvent rester dans la plateforme. L'intégration d'un service de signature électronique supprimerait les allers-retours par email et les impressions inutiles, tout en apportant une valeur légale et une traçabilité des consentements. Cette fonctionnalité couvrirait la totalité du parcours, du dépôt de candidature jusqu'à la finalisation administrative, ce qui renforcerait la position de XpertSphere comme outil central du cycle de recrutement.

### 5.4.5 Application mobile candidat et responsive design

Dans de nombreux contextes africains, le smartphone est le principal moyen d'accès à internet. Une application mobile dédiée aux candidats améliorerait l'accessibilité de XpertSphere et faciliterait le dépôt de candidatures depuis un petit écran.

Une application native pour iOS et Android offrirait une expérience optimisée pour les interactions tactiles. L'interface simplifierait la saisie des informations et le téléchargement du CV. Les notifications push alerteraient les candidats lorsqu'une nouvelle offre correspondant à leur profil est publiée, ou lorsque le statut de leur candidature évolue.

L'approche progressive web app (PWA) est une alternative qui mérite d'être examinée. Une PWA fonctionne dans le navigateur mobile, ce qui évite le passage par les stores d'applications et réduit les frictions d'installation. Elle peut aussi fonctionner partiellement hors ligne, en conservant localement certaines données et en synchronisant lorsque la connexion se rétablit. Cette capacité est utile dans des contextes où la connectivité est intermittente.

Pour les recruteurs et les équipes RH, l'approche est différente. L'interface web sera rendue responsive pour garantir une utilisabilité correcte sur mobile via navigateur. Cela évite de développer et maintenir une application native distincte pour un cas d'usage secondaire. Les recruteurs effectuent généralement leurs tâches depuis un ordinateur ; les actions urgentes (consulter rapidement une candidature, valider un changement de statut) restent possibles via le navigateur mobile, sans nécessiter une application dédiée.

Cette stratégie concentre l'effort de développement mobile sur l'expérience candidat, là où la valeur est la plus claire, tout en maintenant l'accessibilité de l'interface recruteur en mobilité.
