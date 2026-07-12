# copilot-instructions.md — Guide de style (mémoire XpertSphere)

Ce document fixe les principes de rédaction pour conserver un style académique naturel et éviter les marqueurs courants des textes générés. Ce ne sont pas des interdictions absolues : l'objectif est d'éviter les automatismes.

---

## 1) Rythme et syntaxe

### 1.1 Variation des longueurs de phrases
Un texte naturel alterne irrégulièrement phrases longues et courtes. Ce n'est pas le rythme « long-court-long-court » qui compte — ce pattern régulier est lui-même détectable — c'est l'irrégularité. Deux longues de suite, puis trois courtes. Une phrase de cinq mots après un développement dense, sans transition explicite.

Les détecteurs IA mesurent en partie la **burstiness** : la variation de longueur entre phrases. Un texte trop homogène trahit une origine automatique, même si chaque phrase est correcte individuellement. L'objectif n'est pas de varier pour varier, mais de laisser le rythme suivre la pensée plutôt qu'un gabarit.

### 1.2 Imperfections contrôlées
Un texte humain porte des scories naturelles qu'une IA gomme systématiquement. Quelques-unes méritent d'être conservées ou introduites volontairement :

- Une parenthèse un peu longue qui interrompt le fil
- Une reformulation explicite : « Autrement dit, et c'est peut-être plus clair ainsi… »
- Un retour en arrière : « On y reviendra, mais notons déjà que… »
- Une nuance qui complique plutôt que de simplifier : « Cette idée est juste, en partie du moins. »
- Des phrases démarrant par « Et », « Mais », « Or » — rares dans les textes IA, courants chez l'humain.

Un texte trop lissé, trop homogène, peut paraître artificiel même s'il est grammaticalement correct.

### 1.3 Ponctuation et structures ordonnées
Le deux-points est acceptable en rédaction académique. Le problème vient de son usage systématique pour introduire des listes, qui donne un rythme mécanique. Mieux vaut intégrer l'énumération dans la phrase, ou répartir sur deux phrases courtes.

Les structures « d'une part / d'autre part », « premièrement / deuxièmement » sont naturelles — elles deviennent détectables quand elles s'enchaînent trop souvent. Alterner avec des transitions plus narratives : « Un autre point concerne… », « À cela s'ajoute… », « Dans le même mouvement… »

**Tirets cadratins : usage interdit.** Le tiret cadratin (—) est à proscrire dans l'ensemble du mémoire, y compris dans les titres et les légendes de figures. Selon le contexte :

- Dans un titre ou une légende de figure, remplacer par `:` (ex. `Figure 3.1 : Architecture globale…`, `Chapitre 3 : Conception…`).
- Dans un paragraphe, remplacer par des parenthèses pour toute insertion, liste ou aparté.
- Si l'accumulation de parenthèses dépasse deux ou trois par page, reformuler la phrase pour intégrer l'information directement.

Font exception les marqueurs internes de travail (`[À FAIRE — …]`, `[À SOURCER — …]`, `[À COMPLÉTER — …]`), où le tiret reste acceptable comme séparateur de note.

---

## 2) Organisation et argumentation

### 2.1 Structure du paragraphe
Un paragraphe porte un message central. Si deux idées fortes apparaissent, scinder puis relier par une transition. Cela évite les paragraphes denses qui accumulent sans développer.

### 2.2 Transitions naturelles
Relier les paragraphes évite l'effet « blocs thématiques » trop réguliers. Préférer des phrases-ponts : « Cette évolution a une conséquence directe… », « On comprend alors que… », « Cela devient encore plus vrai lorsque… »

### 2.3 Argumentation non-linéaire
Les IA construisent des arguments propres : prémisse → développement → conclusion. Un texte humain digresse. Il est sain d'évoquer une idée, de la quitter, puis d'y revenir plus tard avec un angle différent. Un exemple un peu déplacé peut éclairer davantage qu'un exemple parfaitement à sa place. Laisser une question ouverte plutôt que de tout conclure. Admettre une limite sans la résoudre immédiatement.

### 2.4 Gestion des répétitions inter-sections
Un fait contextuel se démontre une fois, à l'endroit le plus logique. Les sections suivantes y font allusion brièvement — « dans la continuité des canaux déjà évoqués… », « comme mentionné précédemment… » — sans le redémontrer.

Quand une idée forte traverse plusieurs sections, chaque occurrence doit apporter un angle différent : contextuel, positionnel ou technique. Répéter la même formulation sans variation donne l'impression que le texte tourne en rond, même si l'idée est juste. Bon réflexe : se demander ce que l'occurrence apporte de neuf par rapport à la précédente.

---

## 3) Lexique et ton

### 3.1 Mots et tournures à surveiller
Certains mots sont surreprésentés dans les sorties IA en français. Les limiter, surtout quand ils s'enchaînent :

- Connecteurs redondants : « notamment », « en effet », « par ailleurs », « ainsi », « en outre »
- Formules figées : « il convient de », « il est important de noter que », « force est de constater »
- Entrées génériques : « dans un monde où… », « à l'ère du numérique »
- Adjectifs vagues répétés : « crucial », « essentiel », « pertinent », « significatif »

Beaucoup de ces transitions sont superflues. Les supprimer simplifie sans perte de sens.

### 3.2 Verbes concrets et formulations méta
Éviter les enchaînements de « il s'agit de », « cela consiste à », « ceci permet de ». Alterner avec des verbes porteurs de sens : « on observe », « cela se traduit par », « cela conduit à », « cela réduit », « cela renforce ».

Les formulations méta (« l'objectif de cette section est de… ») sont acceptables quand elles clarifient réellement la structure. Les éviter comme réflexe automatique — si chaque sous-partie commence ainsi, le texte devient prévisible.

### 3.3 Ton académique
Éviter un vocabulaire promotionnel (« révolutionner », « solution ultime »). Préférer un ton mesuré : « vise à », « cherche à », « contribue à », « facilite », « limite ».

### 3.4 Subjectivité mesurée
Même un mémoire académique laisse passer une voix. Un texte entièrement neutre peut sonner creux. À doser :

- Une formulation d'hésitation : « sans doute », « peut-être », « il n'est pas exclu que »
- Un jugement nuancé sur une source : « cette analyse est intéressante mais reste partielle »
- Avec parcimonie : « il me semble que », « à mon sens » — à réserver aux moments où la position de l'auteur est réellement pertinente.

---

## 4) Références, listes et présentation

### 4.1 Listes à puces
Dans le corps des chapitres, privilégier un style narratif. Les listes à puces sont possibles mais doivent rester rares, motivées par un besoin de lisibilité. Elles conviennent davantage aux annexes, tableaux ou encadrés méthodologiques.

### 4.2 Tableaux et comparaisons
Quand une comparaison devient très structurée, un tableau en annexe est préférable. Dans le texte, on garde une comparaison narrative et on renvoie à l'annexe si besoin : « Une grille de comparaison est proposée en annexe. »

### 4.3 Contexte local
Pour le contexte Sénégal/Afrique, privilégier des réalités stables et observables : email, réseaux sociaux, plateformes locales d'annonces, outils génériques. Les noms propres peuvent être cités quand une source fiable est disponible ou quand ils sont directement observés dans le terrain d'étude.

---

## 5) Rigueur factuelle (obligatoire)

Le mémoire doit rester factuel. Aucun agent ne doit inventer des informations, des chiffres, des exemples d'entreprises, des usages locaux, ou des éléments juridiques.

Règles à respecter :
- Ne jamais créer de statistiques, pourcentages, classements, volumes ou tendances chiffrées sans source explicite.
- Ne pas attribuer à un outil une adoption locale, une « popularité », ou des fonctionnalités précises si cela n'a pas été observé dans le projet ou documenté par une source.
- Les exemples d'outils peuvent être cités, mais sans affirmation non vérifiée sur leur présence ou leurs parts de marché.
- En cas d'incertitude, rester générique ou formuler comme une hypothèse clairement indiquée.

Bon réflexe : ajouter un marqueur interne `[À SOURCER]` quand une référence devra être vérifiée lors de la phase bibliographie.

---

## Note de cohérence d'ensemble

Quand un nouveau texte est ajouté, relire rapidement les deux sections précédentes et ajuster le rythme, le vocabulaire, les transitions, la densité des phrases et l'usage des structures ordonnées. Un lecteur doit avoir l'impression qu'un seul auteur a rédigé tout le mémoire.

---
Fin du guide.
