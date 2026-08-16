# Pages CGU et Politique de confidentialité, liées depuis l'inscription candidat

## Contexte et périmètre

Le formulaire d'inscription candidat (`src/components/register/MultiStepRegisterForm.vue`, dernière
étape) affiche deux cases à cocher obligatoires :

```vue
<q-checkbox
  v-model="formData.acceptTerms"
  label="J'accepte les conditions d'utilisation"
  :rules="[(val: any) => val || 'Vous devez accepter les conditions']"
/>
<q-checkbox
  v-model="formData.acceptPrivacyPolicy"
  label="J'accepte la politique de confidentialité"
  :rules="[(val: any) => val || 'Vous devez accepter la politique']"
/>
```

Ces labels sont aujourd'hui du texte brut : aucun lien ne pointe vers un contenu réel. Le candidat
accepte donc sans jamais rien pouvoir lire. Objectif de cette spec :

1. Rédiger un contenu réel et honnête pour deux pages, "Conditions d'utilisation" et "Politique de
   confidentialité", spécifique à ce que XpertSphere fait réellement (pas un texte juridique
   générique, pas un déni "projet étudiant, vos données ne sont pas protégées").
2. Créer les pages et routes correspondantes (publiques, sans authentification), côté
   `candidate-app`.
3. Rendre les deux labels du formulaire d'inscription cliquables vers ces pages.
4. Corriger, côté `XpertSphere.MonolithApi`, la seule anomalie de code réellement confirmée
   pendant l'exploration (voir section "Correctif backend" — son périmètre est plus restreint que
   ce que le ticket d'origine supposait, voir plus bas).

`recruiter-app` n'est pas concerné : sa page d'inscription publique a déjà été supprimée
(`remove-public-recruiter-registration.md`, déjà mergée) et aucune case CGU/confidentialité
n'existe dans son code source actuel (seuls des artefacts de build obsolètes dans `dist/`
subsistent, hors périmètre).

## Découverte importante : le bug backend supposé n'existe pas en pratique

Le ticket d'origine partait du principe qu'un candidat peut aujourd'hui s'inscrire en laissant les
cases décochées, ou en appelant l'API `POST /api/auth/register/candidate` directement sans envoyer
`AcceptTerms`/`AcceptPrivacyPolicy`, à cause d'un `[Required]` inefficace sur un `bool` non
nullable (`RegisterCandidateDto.cs`). L'exploration confirme la première partie de ce diagnostic
mais **pas sa conséquence pratique** :

- **`[Required]` sur un `bool` non-nullable ne se déclenche effectivement jamais.**
  `RequiredAttribute.IsValid` renvoie `true` dès que la valeur n'est pas `null` ; une valeur `false`
  boxée n'est jamais `null`. Les messages français déjà écrits sur ces attributs (`"Vous devez
accepter les conditions d'utilisation"` / `"...la politique de confidentialité"`, ajoutés par
  `localize-identity-error-messages.md`, déjà mergée) sont donc du texte mort, jamais atteint.
- **Mais `AuthenticationService.RegisterCandidateAsync` contient déjà un contrôle manuel qui
  couvre exactement ce cas**, avant toute autre logique métier :

  ```csharp
  // Basic validation
  if (!registerDto.AcceptTerms || !registerDto.AcceptPrivacyPolicy)
  {
      return AuthResult.ValidationError(["Vous devez accepter les conditions d'utilisation et la politique de confidentialité"]);
  }
  ```

  Ce contrôle rejette aussi bien une valeur explicitement envoyée à `false` qu'un champ omis du
  corps multipart (qui se lie alors à sa valeur par défaut `false`). `AuthResult.ValidationError`
  est traduit par `ControllerExtensions.ToActionResult` en `400 Bad Request` avec le message
  français ci-dessus, **pour tout appelant, UI ou appel API direct**.

- Le correctif suggéré par le ticket (`bool` → `bool?` pour que `[Required]` se déclenche) est
  **plus faible que ce qui existe déjà** : `[Required]` sur un `bool?` ne se déclenche que si le
  champ est **absent** du corps de la requête (liaison à `null`), pas s'il est envoyé explicitement
  à `false` — ce qui laisserait passer exactement le cas qu'on cherche à couvrir si un appelant
  direct envoie `acceptTerms=false`. Ce correctif n'atteint donc pas l'objectif annoncé et n'est
  **pas retenu**.

**Conclusion vérifiée : il n'existe aujourd'hui aucune faille fonctionnelle.** Un candidat ne peut
pas créer de compte sans avoir explicitement accepté les deux conditions, que ce soit via
`MultiStepRegisterForm.vue` (qui bloque déjà côté client, voir plus bas) ou via un appel API direct
sans passer par l'UI. Le seul problème réel est un défaut de qualité de code : un attribut
`[Required]` mort et trompeur sur `RegisterCandidateDto.cs`, qui donne l'illusion d'une validation
qui n'a jamais lieu à cet endroit.

Confirmation côté frontend : `MultiStepRegisterForm.vue`, `canProceed` (case `6`, lignes ~649-657)
exige déjà `formData.acceptTerms && formData.acceptPrivacyPolicy` pour activer le bouton "Créer mon
compte" (`:disable="!canProceed"`). Le bug backend, même s'il avait existé, n'aurait donc jamais été
visible en usage normal via l'UI — uniquement via un appel API direct, ce qui est bien le cas
documenté ci-dessus comme déjà couvert.

Autre détail vérifié en cours d'exploration : les deux `:rules="[...]"` posées sur ces
`q-checkbox` (lignes 458 et 463) sont elles aussi inertes — `QCheckbox` (Quasar 2.16, version
utilisée par ce package) n'a pas de prop `rules` (celle-ci n'existe que sur les composants dérivés
de `QField`, ex. `QInput`/`QSelect`). Ce n'est pas un bug fonctionnel (le blocage réel vient de
`canProceed`/`:disable`, cf. ci-dessus) mais du texte mort qu'il est cohérent de retirer au moment
où l'on retouche ces deux lignes.

## Correctif backend (périmètre réduit)

Un seul changement, purement cosmétique/qualité de code, sans impact fonctionnel :

- **`src/backend/XpertSphere.MonolithApi/DTOs/Auth/RegisterCandidateDto.cs`** : supprimer les deux
  attributs `[Required(ErrorMessage = "...")]` sur `AcceptTerms` et `AcceptPrivacyPolicy` (dead
  code trompeur, cf. ci-dessus). Les deux propriétés restent des `bool` simples avec valeur par
  défaut `false`, sans changement de type. Aucun autre fichier n'a besoin d'être modifié : le
  contrôle manuel de `AuthenticationService.RegisterCandidateAsync` (lignes 191-195) reste la seule
  et suffisante source de vérité pour cette règle, inchangé.
- Aucun test ne casse — vérifié explicitement, pas seulement supposé : toutes les occurrences de
  `AcceptTerms`/`AcceptPrivacyPolicy` dans `XpertSphere.MonolithApi.Tests/` (`AuthenticationServiceTests.cs`,
  `AuthenticationServiceProfileCompletenessTests.cs`,
  `Integration/IdentityErrorLocalizationIntegrationTests.cs`) leur assignent explicitement `true`
  (ou `"true"` en formulaire multipart pour les tests d'intégration) ; aucun test n'exerce le
  chemin "champ absent/`false`" via `ModelState`/`[Required]`, donc aucun n'observait déjà le
  comportement (de toute façon inatteignable) que cette suppression retire.
- Aucun changement de comportement observable pour l'utilisateur final ni pour un appelant API :
  le message renvoyé en cas de refus (`"Vous devez accepter les conditions d'utilisation et la
politique de confidentialité"`, 400) reste identique.
- **Contradiction avec une spec précédente, à documenter explicitement (même principe que
  `french-message-consistency.md` §4, "décision inversée dans ce ticket")** : le tableau exhaustif
  de `localize-identity-error-messages.md` (lignes 137-138) liste `AcceptTerms`/
  `AcceptPrivacyPolicy` comme portant un `[Required]` avec un `ErrorMessage` français précis. Cette
  spec invalide ces deux lignes : les attributs qu'elles décrivent sont supprimés ici. Le
  `developer` ne doit pas restaurer ces attributs par souci de cohérence avec cette table — c'est
  cette spec qui prime sur ce point précis, la table de `localize-identity-error-messages.md`
  devenant obsolète sur ces deux lignes seulement (le reste de cette table, portant sur d'autres
  DTOs/champs, n'est pas concerné).

### `[À CONFIRMER]` — option non retenue par défaut, à la discrétion de l'utilisateur

Une alternative plus "propre" architecturalement consisterait à créer un
`Validators/Auth/RegisterCandidateDtoValidator.cs` (FluentValidation, `RuleFor(x =>
x.AcceptTerms).Equal(true)...`), sur le modèle exact de `RegisterUserDtoValidator.cs` (qui fait
déjà ça pour `RegisterDto`, le DTO frère utilisé par l'inscription non-candidat). Ce point avait
déjà été identifié et **explicitement classé hors périmètre** par
`localize-identity-error-messages.md` (ligne 103 : "ajout de validateurs FluentValidation
manquants comme celui de `RegisterCandidateDto`... ce ticket ne change pas quel mécanisme de
validation s'exécute ni dans quel ordre"). Cette spec ne rouvre pas ce chantier par défaut, pour
les mêmes raisons :

- Ça change le mécanisme de validation (constructeur d'`AuthenticationService` à modifier,
  injection d'un nouveau `IValidator<RegisterCandidateDto>`, suppression du contrôle manuel
  redondant) pour un DTO qui, par ailleurs, continue de valider le reste de ses champs uniquement
  par `DataAnnotations` — un validateur ne couvrant que 2 champs sur ~30 serait incohérent avec le
  reste du fichier.
- Ça impose de retoucher les 9 tests existants `RegisterCandidateAsync_*` dans
  `AuthenticationServiceTests.cs` (ajout d'un mock `IValidator<RegisterCandidateDto>` avec une
  configuration par défaut "valide" pour ne pas les casser, plus mise à jour du constructeur
  `CreateAuthenticationService()` du fichier de test) et le builder associé — un coût de test non
  négligeable pour un gain fonctionnel nul (le comportement observé reste "refus si non accepté",
  seul le grain du message change : un message combiné aujourd'hui vs. deux messages par champ
  avec un validateur dédié).

Si l'utilisateur souhaite tout de même ce refactor (par cohérence avec `RegisterUserDtoValidator`,
ou pour préparer un futur besoin de messages par champ), il est décrit ci-dessus pour ne pas avoir
à le re-découvrir, mais **n'est pas demandé par défaut à l'agent `developer`** : à traiter comme un
ticket séparé si confirmé.

## Frontend — `candidate-app`

### Nouvelles pages

Deux nouveaux fichiers dans `src/pages/`, cohérents avec la convention PascalCase + suffixe `Page`
déjà en usage (`ConfirmEmailPage.vue`, `JobDetailsPage.vue`) :

- `src/pages/TermsOfServicePage.vue`
- `src/pages/PrivacyPolicyPage.vue`

Structure attendue (cohérente avec le reste du package : pas d'utilisation de `vue-i18n`/`$t()`
dans les pages existantes — texte français en dur directement dans le template, comme
`ConfirmEmailPage.vue`/`MultiStepRegisterForm.vue`) :

```vue
<template>
  <q-page class="legal-page q-py-xl">
    <div class="legal-content q-px-md">
      <h1 class="text-h4 q-mb-xs">Conditions d'utilisation</h1>
      <p class="text-caption text-grey-7 q-mb-lg">Dernière mise à jour : 3 août 2026</p>

      <section class="q-mb-lg">
        <h2 class="text-h6">1. Objet</h2>
        <p>...</p>
      </section>
      <!-- ... -->
    </div>
  </q-page>
</template>

<style scoped>
.legal-content {
  max-width: 800px;
  margin: 0 auto;
}
</style>
```

Pas de `q-card` englobante (contrairement à `ConfirmEmailPage.vue`) : un contenu de lecture longue
n'a pas besoin d'un cadre de carte, un conteneur centré à largeur de lecture confortable
(`max-width: 800px`) suffit, cohérent avec les usages courants de pages de contenu légal. Pas de
dépendance à un store ou à un service API : ce sont des pages statiques.

### Nouvelles routes (`src/router/routes.ts`)

Ajouter, dans les enfants de la route `/` (même niveau que `login`/`register`/`confirm-email`),
**sans** `meta: { requiresAuth: true }` (routes publiques, cohérent avec `authGuard.ts` qui ne
bloque que les routes portant explicitement cette meta) :

```ts
{
  path: 'terms',
  component: () => import('pages/TermsOfServicePage.vue'),
  name: 'Terms',
},
{
  path: 'privacy',
  component: () => import('pages/PrivacyPolicyPage.vue'),
  name: 'PrivacyPolicy',
},
```

**Découverte notable, à vérifier explicitement par le `validator`** : `src/components/AppFooter.vue`
contient déjà, depuis avant ce ticket, des liens morts vers exactement ces chemins :

```vue
<router-link to="/privacy" class="footer-link text-white">Confidentialité</router-link>
<router-link to="/terms" class="footer-link text-white">CGU</router-link>
```

Ces deux liens deviennent fonctionnels dès l'ajout des routes ci-dessus, **sans qu'aucune
modification d'`AppFooter.vue` ne soit nécessaire**. Le chemin (`/terms`, `/privacy`) a été choisi
pour correspondre exactement à ces liens existants plutôt qu'un nom arbitraire.

Le footer contient un troisième lien mort, `/help` ("Aide"), qui reste **explicitement hors
périmètre** de cette spec : ne pas créer de page/route pour `/help` ici, ce serait un scope creep
non demandé.

### `MultiStepRegisterForm.vue` — rendre les deux labels cliquables

Remplacer le prop `label` de chaque `q-checkbox` par le slot par défaut, afin d'y insérer un lien.
Retirer aussi les deux `:rules="[...]"` inertes (cf. section précédente) puisqu'on retouche ces
lignes :

```vue
<q-checkbox v-model="formData.acceptTerms">
  <span>
    J'accepte les
    <router-link to="/terms" target="_blank" rel="noopener noreferrer" @click.stop>
      conditions d'utilisation
    </router-link>
    (nouvel onglet)
  </span>
</q-checkbox>
<q-checkbox v-model="formData.acceptPrivacyPolicy">
  <span>
    J'accepte la
    <router-link to="/privacy" target="_blank" rel="noopener noreferrer" @click.stop>
      politique de confidentialité
    </router-link>
    (nouvel onglet)
  </span>
</q-checkbox>
```

Points à respecter :

- **`target="_blank"`, ouverture en nouvel onglet — décision retenue, pas une alternative en
  `q-dialog`.** Vérifié : `MultiStepRegisterForm.vue` ne persiste sa progression que dans un objet
  `reactive` local (`formData`), sans aucun `localStorage`/`sessionStorage` de brouillon. Naviguer
  dans le même onglet ferait perdre tout ce que le candidat a déjà saisi dans les 6 étapes du
  formulaire (identité, informations pro, formations, expériences, etc.). Un nouvel onglet est donc
  le choix qui préserve l'expérience, pas une préférence arbitraire.
- **`@click.stop`** sur chaque `router-link` : sans ça, cliquer sur le texte du lien (qui est à
  l'intérieur de la zone cliquable du label de la case à cocher) basculerait aussi la case, en plus
  d'ouvrir le lien — comportement à éviter (le candidat clique pour lire, pas pour cocher/décocher
  par accident).
- **`rel="noopener noreferrer"`** : bonne pratique standard pour tout lien `target="_blank"`
  (évite qu'un onglet ouvert puisse accéder à `window.opener`).
- La mention visible "(nouvel onglet)" est une amélioration d'accessibilité recommandée (annoncer
  qu'un lien change de contexte de navigation, cohérent avec l'objectif RGAA/WCAG 2.1 AA déjà
  affiché dans `.claude/docs/architecture.md`) ; le package n'a pas de classe utilitaire
  "sr-only"/visuellement masquée existante, donc ce texte reste visible plutôt que d'en introduire
  une pour ce seul besoin. Formulation libre pour le `developer`, l'important est qu'un lecteur
  d'écran annonce l'ouverture dans un nouvel onglet.
- Aucun changement à `canProceed`/`nextButtonDisabled` : le blocage tant que les deux cases ne sont
  pas cochées reste géré exactement comme avant (section "Découverte importante" ci-dessus).

## Contenu réel des deux pages

Contenu à reproduire tel quel (adapter uniquement la mise en page/markup Vue, pas le texte, sauf
correction orthographique). Rédigé pour refléter fidèlement ce que XpertSphere fait aujourd'hui
(vérifié dans le code : `Models/User.cs`, `Models/Experience.cs`, `Models/Training.cs`,
`azurite-blob-storage-local.md`, `llm-provider-groq-azure.md`, `email-sending-foundation.md`,
`secure-cv-download.md`, `candidate-account-activation-email.md`), sans donnée inventée. En
particulier, la phrase du §3 des CGU ("confirmer votre adresse email ... sans quoi vous ne pourrez
pas vous connecter") est vérifiée dans le code, pas supposée : `SecurityExtensions.cs` fixe bien
`options.SignIn.RequireConfirmedEmail = true`, et un email d'activation réel est envoyé à
l'inscription (`AuthenticationService.RegisterCandidateAsync`, appel à
`_emailNotificationService.SendAccountActivationEmailAsync`).

**Choix documentés (à ne pas remettre en question sans repasser par une clarification) :**

- Adresse de contact : `contact@xpertsphere.com`. Cette adresse existe déjà dans le code
  (`XpertSphere.MonolithApi/appsettings.json`, `Seeding:Organization:ContactEmail`, adresse de
  contact de l'organisation "XpertSphere" elle-même dans les données de démonstration) : on la
  réutilise par cohérence plutôt que d'en inventer une nouvelle, mais il s'agit bien d'un choix
  déterministe, pas d'une adresse de support réellement surveillée à ce jour.
- Pas d'adresse postale, pas de nom de DPO (délégué à la protection des données), pas de numéro de
  SIRET : ces informations n'existent pas de façon vérifiable dans le projet (l'adresse "123 Tech
  Street, Tech City" présente dans les données de seed est une donnée de démonstration fictive,
  pas une adresse légale réelle) — volontairement non mentionnées plutôt qu'inventées.
- Pas de mention RGPD précise et invérifiable (base légale article par article, durée de
  conservation en nombre de jours exact) : le texte reste sincère sur les catégories de données et
  les finalités réelles, sans détail juridique que le projet ne peut pas honorer.
- Pas de disclaimer "projet étudiant, vos données ne sont pas protégées" : le texte est écrit comme
  celui d'un produit réel, dans un registre sobre.

### Page "Conditions d'utilisation" (`/terms`)

```markdown
# Conditions d'utilisation

Dernière mise à jour : 3 août 2026

## 1. Objet

Les présentes conditions d'utilisation régissent l'accès et l'utilisation de la plateforme
XpertSphere par les candidats à la recherche d'un emploi (« vous », « le candidat »). En créant un
compte, vous acceptez ces conditions ainsi que notre politique de confidentialité.

## 2. Présentation du service

XpertSphere est une plateforme de recrutement (ATS) qui permet de consulter des offres d'emploi
publiées par des organisations recruteuses, de créer et gérer un profil candidat, de soumettre des
candidatures et d'en suivre le statut.

## 3. Création de compte

Pour utiliser XpertSphere, vous devez créer un compte avec une adresse email valide. Vous vous
engagez à :

- fournir des informations exactes et à jour sur votre identité, votre parcours et vos
  expériences ;
- confirmer votre adresse email suite à la réception de l'email d'activation, sans quoi vous ne
  pourrez pas vous connecter ;
- conserver la confidentialité de votre mot de passe et nous signaler toute utilisation non
  autorisée de votre compte ;
- ne créer qu'un seul compte par personne.

## 4. Votre CV et votre profil

Vous pouvez déposer un CV au format PDF lors de votre inscription ou depuis votre profil. Le
contenu de ce CV est analysé automatiquement pour préremplir votre profil (formations,
expériences, compétences) ; vous restez responsable de la relecture et de l'exactitude des
informations finalement enregistrées sur votre profil, l'analyse automatisée pouvant comporter des
erreurs d'extraction.

Vous demeurez seul responsable du contenu que vous déposez sur la plateforme (CV, lettre de
motivation, informations de profil) et garantissez qu'il ne porte pas atteinte aux droits d'un
tiers.

## 5. Candidatures

En postulant à une offre, vous autorisez l'organisation ayant publié cette offre à consulter votre
profil candidat, votre CV et les éléments de votre candidature (motivation, notes) afin
d'évaluer votre candidature. Vous pouvez consulter le statut de vos candidatures depuis votre
espace « Mes candidatures » et, selon les fonctionnalités disponibles, retirer une candidature en
cours.

## 6. Usage interdit

Vous vous engagez à ne pas :

- fournir de fausses informations sur votre identité ou votre parcours ;
- utiliser la plateforme à des fins frauduleuses ou nuisibles à un tiers ;
- tenter de contourner les mesures de sécurité de la plateforme ou d'accéder à des données qui ne
  vous sont pas destinées ;
- utiliser la plateforme à des fins commerciales autres que votre recherche d'emploi.

## 7. Disponibilité du service

Nous nous efforçons de maintenir la plateforme accessible et fonctionnelle, sans garantie de
disponibilité continue. La plateforme peut être temporairement interrompue pour maintenance ou
évolution.

## 8. Résiliation

Vous pouvez cesser d'utiliser la plateforme à tout moment et demander la suppression de votre
compte (voir notre politique de confidentialité). Nous nous réservons le droit de suspendre ou de
supprimer un compte en cas de manquement grave aux présentes conditions.

## 9. Modification des présentes conditions

Nous pouvons modifier ces conditions d'utilisation ; la date de dernière mise à jour figure en
haut de cette page. Votre utilisation continue de la plateforme après une modification vaut
acceptation des nouvelles conditions.

## 10. Contact

Pour toute question relative à ces conditions d'utilisation : contact@xpertsphere.com.
```

### Page "Politique de confidentialité" (`/privacy`)

```markdown
# Politique de confidentialité

Dernière mise à jour : 3 août 2026

## 1. Qui sommes-nous

XpertSphere est une plateforme de recrutement (ATS) qui met en relation des candidats et des
recruteurs. Cette politique décrit quelles données nous collectons lorsque vous utilisez
XpertSphere en tant que candidat, dans quel but, avec qui nous pouvons les partager, et comment
exercer vos droits sur ces données.

Pour toute question relative à vos données personnelles : contact@xpertsphere.com.

## 2. Données que nous collectons

Lorsque vous créez un compte candidat et utilisez XpertSphere, nous collectons :

- **Données d'identité et de contact** : nom, prénom, adresse email, numéro de téléphone, adresse
  postale.
- **Votre CV** : le fichier que vous déposez lors de votre inscription ou depuis votre profil.
- **Informations extraites de votre CV** : lorsque vous déposez un CV, son contenu est analysé
  automatiquement (voir section 4) afin d'en extraire vos formations, expériences professionnelles
  et compétences, et de préremplir votre profil. Vous pouvez relire et corriger ces informations
  avant de valider votre profil.
- **Informations de profil renseignées manuellement** : formations, expériences professionnelles,
  compétences, années d'expérience, prétentions salariales, disponibilité, lien vers un profil
  LinkedIn.
- **Données de candidature** : les offres auxquelles vous postulez, vos lettres de motivation ou
  notes associées, le statut de vos candidatures et son historique.
- **Préférences de communication** : votre choix de recevoir des notifications par email et/ou par
  SMS, votre langue préférée.
- **Données techniques liées à votre compte** : date de création du compte, date de dernière
  connexion, jeton de connexion.

Nous ne collectons aucune donnée via des cookies tiers, des outils publicitaires ou de mesure
d'audience : XpertSphere n'intègre aucun outil de ce type. Votre session est maintenue par un jeton
de connexion stocké localement dans votre navigateur, jamais par un cookie de suivi.

## 3. Pourquoi nous utilisons ces données

Nous utilisons vos données pour :

- créer et gérer votre compte candidat, et vous authentifier lors de vos connexions ;
- vous permettre de constituer et de tenir à jour votre profil candidat ;
- analyser automatiquement le contenu de votre CV afin de préremplir votre profil et vous faire
  gagner du temps à l'inscription ;
- vous permettre de postuler aux offres d'emploi publiées sur la plateforme, et transmettre votre
  candidature (profil, CV, motivation) aux recruteurs de l'organisation qui a publié
  l'offre ;
- vous envoyer l'email nécessaire à l'activation de votre compte à l'inscription (confirmation de
  votre adresse email, sans laquelle vous ne pouvez pas vous connecter) ;
- assurer la sécurité de votre compte (par exemple : détection de tentatives de connexion
  invalides).

Nous n'utilisons pas vos données à des fins de publicité, ni de revente à des tiers.

## 4. Avec qui vos données sont-elles partagées

- **Les recruteurs et administrateurs de l'organisation** qui a publié une offre à laquelle vous
  postulez ont accès à votre profil candidat, votre CV et votre candidature, dans la limite
  nécessaire au traitement de celle-ci.
- **Un fournisseur d'intelligence artificielle tiers** (Azure OpenAI ou Groq, selon la
  configuration de la plateforme) reçoit le texte extrait de votre CV, dans le seul but de
  l'analyser automatiquement et d'en restituer une structure exploitable (identité, formations,
  expériences, compétences).
- **Un prestataire d'envoi d'email** (Brevo) est utilisé pour l'acheminement des emails
  transactionnels décrits en section 3.
- **Un hébergeur de stockage de fichiers** (Microsoft Azure) conserve votre CV de façon sécurisée ;
  ce fichier n'est jamais rendu accessible par une adresse publique, sa consultation ou son
  téléchargement nécessite d'être authentifié comme vous-même ou comme un recruteur autorisé de
  l'organisation concernée.

Nous ne vendons, ne louons et ne partageons vos données avec aucun autre tiers.

## 5. Combien de temps conservons-nous vos données

Nous conservons vos données tant que votre compte candidat reste actif. Si vous souhaitez faire
supprimer votre compte et les données associées, vous pouvez nous en faire la demande (voir
section 6) ; cette demande est traitée manuellement par l'équipe administrative de la plateforme.

## 6. Vos droits

Vous pouvez nous demander, à tout moment et par email à contact@xpertsphere.com :

- l'accès aux données personnelles que nous détenons sur vous ;
- la rectification de données inexactes ou incomplètes ;
- la suppression de votre compte et des données associées.

Ces demandes sont traitées manuellement par l'équipe administrative de la plateforme ; il n'existe
pas aujourd'hui de suppression de compte en libre-service depuis votre profil.

## 7. Sécurité

Votre mot de passe n'est jamais stocké en clair : il est protégé par les mécanismes standards
d'authentification de la plateforme. Votre CV est stocké dans un espace privé et n'est jamais
accessible par une adresse publique ; son téléchargement passe systématiquement par un point
d'accès qui vérifie que vous êtes bien le propriétaire du CV, ou un recruteur autorisé de
l'organisation concernée.

## 8. Modifications de cette politique

Nous pouvons faire évoluer cette politique de confidentialité, notamment si de nouvelles
fonctionnalités impliquant un nouveau traitement de données sont ajoutées à la plateforme. La date
de dernière mise à jour figure en haut de cette page.

## 9. Contact

Pour toute question relative à cette politique ou à vos données personnelles :
contact@xpertsphere.com.
```

## Acteurs et permissions

- **Candidat non authentifié** : peut consulter `/terms` et `/privacy` librement (routes
  publiques), y compris en cours d'inscription (nouvel onglet).
- **Candidat authentifié** : accès identique, aussi via les liens du footer (`AppFooter.vue`,
  visible sur toutes les pages du layout principal).
- Aucune notion de rôle/permission différenciée : ce sont des pages de contenu statique, sans
  donnée métier ni appel API.

## Critères d'acceptation

1. `GET /terms` et `GET /privacy` (côté `candidate-app`, via le router) affichent respectivement le
   contenu "Conditions d'utilisation" et "Politique de confidentialité" ci-dessus, accessibles sans
   authentification.
2. Les liens `/privacy` et `/terms` déjà présents dans `AppFooter.vue` fonctionnent (ne renvoient
   plus vers `ErrorNotFound`), sans qu'`AppFooter.vue` n'ait été modifié.
3. Sur la dernière étape de `MultiStepRegisterForm.vue`, le texte « conditions d'utilisation » et le
   texte « politique de confidentialité » sont des liens cliquables ouvrant respectivement `/terms`
   et `/privacy` dans un nouvel onglet, sans perte de la progression du formulaire en cours dans
   l'onglet d'origine.
4. Cliquer sur l'un de ces liens n'a pas d'effet sur l'état cochée/décochée de la case à cocher
   correspondante.
5. Le comportement de blocage existant (bouton "Créer mon compte" désactivé tant que les deux
   cases ne sont pas cochées) n'est pas modifié.
6. `RegisterCandidateDto.cs` ne porte plus d'attribut `[Required]` sur `AcceptTerms`/
   `AcceptPrivacyPolicy` ; une tentative d'inscription (via l'UI ou un appel direct à
   `POST /api/auth/register/candidate`) avec l'un des deux champs à `false` ou absent continue de
   renvoyer `400` avec le message `"Vous devez accepter les conditions d'utilisation et la
politique de confidentialité"`, exactement comme avant ce correctif.
7. `dotnet test` (côté `XpertSphere.MonolithApi.Tests`) passe sans modification requise sur les
   tests existants de `RegisterCandidateAsync`.
8. Aucune régression sur `/help` (reste une route inexistante, hors périmètre).

## Fichiers à créer/modifier — récapitulatif

**`candidate-app`** :

- Créer `src/pages/TermsOfServicePage.vue`
- Créer `src/pages/PrivacyPolicyPage.vue`
- Modifier `src/router/routes.ts` (deux nouvelles routes publiques `terms`/`privacy`)
- Modifier `src/components/register/MultiStepRegisterForm.vue` (les deux `q-checkbox` de la
  dernière étape : slot par défaut + lien, suppression des `:rules` inertes)
- Mettre à jour `.claude/specifications/README.md` de ce package (cette spec)
- Mettre à jour `CLAUDE.md` de ce package (référence à cette spec dans la liste de la section
  Documentation)

**`XpertSphere.MonolithApi`** :

- Modifier `DTOs/Auth/RegisterCandidateDto.cs` (suppression des deux `[Required]` morts sur
  `AcceptTerms`/`AcceptPrivacyPolicy`)
- Ajouter une ligne de renvoi dans `.claude/specifications/README.md` de ce service, pointant vers
  cette spec (qui vit côté `candidate-app` mais modifie un fichier de ce service — sens inhabituel,
  à signaler explicitement pour que le `validator` la retrouve facilement depuis le côté backend).

Aucun changement n'est nécessaire côté `recruiter-app`, `XpertSphere.ResumeAnalyzer` ou
`XpertSphere.CommunicationService` : ces services sont uniquement mentionnés à titre informatif
dans le contenu des pages (comme destinataires réels de données), sans aucune modification de leur
code.
