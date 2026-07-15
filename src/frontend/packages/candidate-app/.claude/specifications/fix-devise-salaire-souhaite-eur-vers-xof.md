# Correctif — Devise du salaire souhaité affichée en EUR au lieu de XOF (Franc CFA)

## Constat et périmètre exact du bug

Le signalement initial parle du « salaire demandé par le candidat sur le formulaire de création de
candidature ». Après exploration complète du code, ce champ **n'existe pas** sur le formulaire de
création de candidature :

- `candidate-app/src/components/ApplicationDialog.vue` (formulaire "Candidater à cette offre") et
  `candidate-app/src/models/application.ts` (`CreateApplicationDto`) ne contiennent **aucun champ
  salaire**. Une candidature (`Application`) porte uniquement `coverLetter` et `additionalNotes`.
- Le champ concerné est en réalité **`desiredSalary`**, un attribut du **profil candidat**
  (entité `User` côté backend, `Models/User.cs:37` — `decimal? DesiredSalary`), saisi :
  1. à l'inscription, dans le formulaire multi-étapes (`MultiStepRegisterForm.vue`) ;
  2. modifiable ensuite depuis la page de profil (`EditProfileDialog.vue`), et affiché en lecture
     sur `ProfilePage.vue`.
- Le recruteur voit cette même valeur (lecture seule) dans la liste et le détail des candidats de
  `recruiter-app` (`CandidatesPage.vue`, `CandidateDetailPage.vue`).

Ce document corrige donc l'affichage/formatage de `desiredSalary` partout où il apparaît, et non le
formulaire `ApplicationDialog.vue` (qui n'a rien à corriger, faute de champ salaire).

**Hors périmètre, volontairement exclu** : le salaire _proposé_ par le recruteur sur une offre
d'emploi (`JobOffer.SalaryMin` / `SalaryMax` / `SalaryCurrency`). C'est un champ distinct de
`desiredSalary`, déjà doté d'une colonne `SalaryCurrency` explicite et configurable par offre
(valeur par défaut actuelle : `"EUR"`, dans `JobOffer.cs`, `CreateJobOfferDto.cs`, et en dur dans
plusieurs composants `recruiter-app` : `JobDialog.vue`, `JobOffersPage.vue`,
`JobOfferDetailPage.vue`, ainsi qu'en repli d'affichage côté candidat dans
`JobDetailsPage.vue:162` — `{{ currentJobOffer.salaryCurrency || '€' }}`). La demande porte
explicitement sur « le salaire que le candidat demande », pas sur le salaire proposé par l'offre :
ce second sujet constitue un correctif séparé, à spécifier indépendamment si l'éditeur du produit le
confirme. `[À CONFIRMER]` si ce second correctif doit être traité — ne pas le traiter dans le cadre
présent.

## Nature du correctif

Purement **cosmétique / affichage**. Aucune migration de données n'est nécessaire :

- `User.DesiredSalary` est un simple `decimal?` en base, sans colonne de devise associée
  (contrairement à `JobOffer.SalaryCurrency`). La valeur numérique ne change pas de sens : c'est un
  montant annuel, et l'étiquette "EUR"/"€" à l'affichage n'a jamais été qu'une convention
  d'interface, jamais stockée. Passer à XOF ne modifie donc aucune donnée existante, uniquement la
  façon de la présenter.
- Aucune migration EF Core, aucun changement de DTO ou de validateur backend n'est requis pour ce
  correctif.

## Acteurs concernés

- **Candidat** : saisit et visualise son salaire souhaité (inscription, profil).
- **Recruteur** : visualise (lecture seule) le salaire souhaité du candidat dans les vues de liste
  et de détail candidat.
- Aucun changement de permission : ce correctif ne touche aucune règle d'autorisation, seulement
  l'affichage/formatage.

## Localisation précise de la devise incorrecte (5 emplacements)

### `candidate-app`

1. **`src/components/register/MultiStepRegisterForm.vue`** (~ligne 157)
   Label du champ `formData.desiredSalary` : `"Salaire souhaité (€/an)"` → à remplacer par
   `"Salaire souhaité (XOF/an)"`.

2. **`src/components/EditProfileDialog.vue`** (ligne 73)
   Label du champ `formData.desiredSalary` : `"Salaire souhaité (€)"` → à remplacer par
   `"Salaire souhaité (XOF)"`.

3. **`src/pages/ProfilePage.vue`** (fonction `formatSalary`, lignes ~638-646)
   ```ts
   const formatSalary = (salary?: number) => {
     if (!salary) return 'Non renseigné';
     return (
       new Intl.NumberFormat('fr-FR', {
         style: 'currency',
         currency: 'EUR',
         maximumFractionDigits: 0,
       }).format(salary) + ' / an'
     );
   };
   ```
   `currency: 'EUR'` → `currency: 'XOF'`. Conserver la locale `fr-FR` (séparateurs de groupe
   uniquement, sans lien avec la devise) et `maximumFractionDigits: 0` (déjà correct : le XOF n'a
   pas de sous-unité usuelle, comme l'EUR l'affichait déjà sans décimales ici).
   **Point de vigilance formatage** : le rendu par défaut d'`Intl.NumberFormat` avec
   `currency: 'XOF'` dépend de la version ICU du moteur JS (résultats observés : `"XOF"`,
   `"F CFA"` ou `"CFA"` selon l'environnement) — voir critère d'acceptation ci-dessous pour lever
   l'ambiguïté.

### `recruiter-app`

4. **`src/pages/candidates/CandidatesPage.vue`** (colonne `desiredSalary`, ligne ~204)

   ```ts
   format: (val: number) => (val ? `${val.toLocaleString()} €` : 'Non spécifié'),
   ```

   Suffixe `€` en dur → à remplacer par `XOF` (ou format cohérent avec le point 6 ci-dessous).
   `toLocaleString()` seul n'affiche pas de décimales par défaut pour un entier ; si la valeur
   comporte des décimales residuelles (ex. `35000.5`), appliquer explicitement 0 décimale pour
   rester cohérent avec le point 3 (XOF sans sous-unité).

5. **`src/pages/candidates/CandidateDetailPage.vue`** (ligne ~217)
   ```html
   {{ candidate.desiredSalary.toLocaleString() }} €
   ```
   Même correctif : suffixe `€` en dur → `XOF`, avec 0 décimale.

### Backend (`XpertSphere.MonolithApi`)

Aucun changement de code requis (cf. « Nature du correctif »). Ajouter uniquement un commentaire
sur `Models/User.cs` au niveau de `DesiredSalary` précisant que la convention d'unité est
désormais le Franc CFA (XOF) et qu'aucune colonne de devise n'est stockée — pour éviter qu'un futur
développeur ne réintroduise une hypothèse EUR ailleurs (ex. export, reporting). Ce commentaire est
documentaire, il ne doit pas changer la signature du modèle ni du DTO.

## Décision de formatage à appliquer (pour lever l'ambiguïté ICU)

Afin que le résultat soit déterministe et vérifiable par le `validator`, quel que soit
l'environnement d'exécution :

- Utiliser `Intl.NumberFormat('fr-FR', { style: 'currency', currency: 'XOF', currencyDisplay:
'code', maximumFractionDigits: 0 })` pour `ProfilePage.vue`, ce qui garantit l'affichage du code
  ISO `"XOF"` plutôt qu'un symbole variable selon l'ICU (`currencyDisplay: 'code'` fixe le rendu à
  `"XOF"` de façon stable).
- Pour les deux emplacements `recruiter-app` qui utilisent `toLocaleString()` + suffixe en dur,
  conserver ce pattern simple mais remplacer le suffixe `'€'` par `'XOF'`, avec troncature/absence
  de décimales explicite.
- Les labels de champ de saisie (`MultiStepRegisterForm.vue`, `EditProfileDialog.vue`) utilisent le
  texte en dur `"(XOF/an)"` / `"(XOF)"` — pas de formatage `Intl` nécessaire, ce sont de simples
  libellés de `q-input`.

`[À CONFIRMER]` : le code ISO `XOF` est utilisé partout dans cette spécification, conformément à la
demande initiale. Si l'éditeur du produit préfère l'étiquette usuelle **"FCFA"** (plus lisible pour
un utilisateur final sénégalais, mais non normalisée ISO 4217) plutôt que `"XOF"` à l'affichage, la
substitution est mécanique (remplacer la chaîne `"XOF"` par `"FCFA"` dans les 5 emplacements
listés) — signaler ce choix au `developer` avant implémentation si l'utilisateur tranche pour
`FCFA`. En l'absence de nouvelle instruction, `XOF` est la valeur par défaut à implémenter.

## Ce qui n'est pas concerné par ce correctif

- `JobCard.vue` (candidate-app) : `formatSalary` n'y insère aucune devise en dur — la devise
  affichée provient du champ `job.salaryCurrency` (donnée pilotée par l'offre, hors périmètre, voir
  ci-dessus).
- `JobDetailsPage.vue` (candidate-app), ligne 162 : repli `'€'` uniquement si
  `currentJobOffer.salaryCurrency` est vide — relève du champ `JobOffer.SalaryCurrency` (hors
  périmètre).
- Tout composant/DTO lié à `JobOffer.SalaryMin/SalaryMax/SalaryCurrency` (`JobDialog.vue`,
  `JobOffersPage.vue`, `JobOfferDetailPage.vue`, `CreateJobOfferDto.cs`, `UpdateJobOfferDto.cs`,
  `JobOffer.cs`) : hors périmètre (salaire proposé par le recruteur, pas salaire demandé par le
  candidat).
- Aucune trace de `desiredSalary`/`DesiredSalary` n'a été trouvée dans
  `XpertSphere.ReportingService`, `XpertSphere.CommunicationService`, `XpertSphere.IntegrationService`
  ou `XpertSphere.ResumeAnalyzer` : aucun correctif à y apporter.
- Aucun test automatisé (`*.spec.ts`, `*.test.ts`, tests backend) ne référence actuellement
  `desiredSalary`/`DesiredSalary` avec une assertion sur `EUR`/`€` : aucun test existant à mettre à
  jour dans ce correctif. Si le `developer` ajoute des tests dans le cadre de ce correctif, ils
  doivent asserter `XOF`, pas `EUR`/`€`.

## Critères d'acceptation vérifiables

1. Dans `MultiStepRegisterForm.vue`, le label du champ `desiredSalary` affiche `"Salaire souhaité
(XOF/an)"` et ne contient plus `"€"` ni `"EUR"`.
2. Dans `EditProfileDialog.vue`, le label du champ `desiredSalary` affiche `"Salaire souhaité
(XOF)"` et ne contient plus `"€"` ni `"EUR"`.
3. Dans `ProfilePage.vue`, la fonction `formatSalary` utilise `currency: 'XOF'` (et non `'EUR'`) et
   produit, pour une valeur non nulle, une chaîne contenant `"XOF"` et ne contenant ni `"€"` ni
   `"EUR"` ; le résultat n'affiche aucune décimale (ex. pour `35000` → une chaîne du type
   `"35 000 XOF / an"`, la forme exacte des espaces dépendant de la locale `fr-FR` mais sans
   décimale).
4. Dans `CandidatesPage.vue`, la colonne `desiredSalary` affiche le suffixe `"XOF"` et non `"€"`.
5. Dans `CandidateDetailPage.vue`, l'affichage du salaire souhaité du candidat affiche le suffixe
   `"XOF"` et non `"€"`.
6. Une recherche texte (`grep`) de `"EUR"` et `"€"` dans les fichiers listés à la section
   « Localisation précise » (les 5 emplacements) ne retourne plus aucune occurrence liée à
   `desiredSalary`.
7. Aucune migration EF Core n'est ajoutée pour ce correctif ; le modèle `User.cs` n'a de changement
   que le commentaire documentaire mentionné plus haut (pas de nouvelle colonne, pas de changement
   de type).
8. Les champs `JobOffer.SalaryMin/SalaryMax/SalaryCurrency` et tous les composants qui les
   affichent restent inchangés (toujours `"EUR"` par défaut) — ce correctif ne doit pas les
   toucher, sauf confirmation explicite ultérieure d'élargir le périmètre.
9. La revue du `validator` confirme que la valeur numérique de `desiredSalary` (montant) n'a été
   modifiée dans aucune donnée existante — seul le libellé/formatage change.
