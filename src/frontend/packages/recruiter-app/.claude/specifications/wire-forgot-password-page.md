# Câblage réel de `ForgotPasswordPage.vue` sur `POST /api/auth/forgot-password`

## Contexte et périmètre

`src/pages/auth/ForgotPasswordPage.vue` (fonctions `handleForgotPassword`/`resendEmail`,
~lignes 95-125) est aujourd'hui entièrement factice : un `setTimeout` simulé et un
`console.log` remplacent tout appel réseau, alors que l'écran affiche « Email envoyé ! »
et un compte à rebours de renvoi comme si un email avait réellement été délivré. Cette
page est accessible sans authentification depuis `LoginPage.vue` (« Mot de passe
oublié ? ») et est listée dans `publicPages` de `router/guards/authGuard.ts`.

Cette spec couvre uniquement le câblage de cette page sur l'endpoint backend réel
`POST /api/auth/forgot-password`, déjà public et fonctionnel côté génération de token.
Elle ne couvre pas `ResetPasswordPage` (saisie du nouveau mot de passe avec le token reçu
par email) : cette page n'existe pas aujourd'hui côté `recruiter-app` et n'est pas
demandée ici — voir « Hors périmètre ».

## Dépendance backend / limite connue — à lire en premier

**Ce câblage rend l'appel réseau réel, mais ne rend pas le parcours fonctionnel de bout
en bout.** Vérifié dans `Services/AuthenticationService.cs:666-696`
(`ForgotPasswordAsync`) : la méthode génère un token de réinitialisation et le persiste
(`user.SetPasswordResetToken` + `_userManager.UpdateAsync`), puis retourne — **elle
n'appelle jamais un service d'envoi d'email**. Ce point est un gap déjà documenté et
volontairement laissé hors périmètre dans
`src/backend/XpertSphere.MonolithApi/.claude/specifications/candidate-account-activation-email.md`
(Constat point 7 et section « Hors périmètre » : _« ForgotPasswordAsync/ResetPasswordAsync
: même lacune préexistante (token généré, jamais envoyé par email), non traitée ici —
nécessiterait un nouveau template côté CommunicationService, seul AccountActivation
existe aujourd'hui »_).

Conséquence concrète : après ce ticket, un recruteur qui clique « Envoyer le lien de
réinitialisation » déclenchera un vrai appel HTTP, une vraie génération de token en base,
mais **ne recevra jamais d'email** — quel que soit le compte utilisé. Le message affiché
(« Si vous avez un compte, vous allez recevoir un email ») reste donc, en pratique,
non tenu tant qu'un ticket backend séparé n'ajoute pas l'envoi réel (nouveau template
`PasswordReset` côté `CommunicationService`, appel `IEmailNotificationService` depuis
`ForgotPasswordAsync`, sur le modèle de ce qui a déjà été fait pour
`ResendConfirmationEmailAsync`/l'email d'activation).

**Recommandation explicite portée par cette spec : ouvrir un ticket backend séparé**
pour l'envoi réel de l'email de réinitialisation, plutôt que de considérer ce ticket
frontend comme suffisant pour rendre la fonctionnalité utilisable. Ce ticket-ci reste
utile en soi : il supprime la fausse promesse `setTimeout`/`console.log` purement
front-end, aligne l'écran sur ce que l'API renvoie réellement (chargement, erreurs
réseau, validation), et prépare le terrain pour que le futur ticket backend n'ait rien à
changer côté UI. Mais il ne doit pas être présenté comme « la réinitialisation de mot de
passe fonctionne » — seul « la page ne ment plus sur ce qu'elle fait » est vrai à l'issue
de ce ticket.

Second point vérifié, sans impact sur la spec ci-dessous mais à connaître : la réponse
de `ForgotPasswordAsync` **n'est pas enumeration-safe aujourd'hui**, contrairement à
`ResendConfirmationEmailAsync` (qui l'est, voir `candidate-account-activation-email.md`
§7). Le message diffère selon que le compte existe (« Email de réinitialisation du mot
de passe envoyé ») ou non (« Si un compte existe avec cet email, un lien de
réinitialisation a été envoyé »), et `Data.User` est peuplé avec le profil utilisateur
complet (`AuthMappingProfile.CreateMap<User, AuthResponseDto>`, y compris
`RefreshToken`) uniquement quand le compte existe. Le statut HTTP reste `200` dans les
deux cas. Cette spec neutralise ce problème **côté UI** en imposant de ne jamais
afficher/logger le message ou les données renvoyés par le backend (voir « Comportement
cible », règle d'affichage générique) — mais ne corrige pas la fuite au niveau de la
réponse HTTP brute elle-même (visible par ex. dans l'onglet réseau du navigateur), qui
reste un défaut backend, hors périmètre de ce ticket. **[À CONFIRMER auprès de
l'utilisateur, hors périmètre de cette spec]** : ouvrir séparément un ticket backend pour
aligner `ForgotPasswordAsync` sur le pattern déjà en place dans
`ResendConfirmationEmailAsync` (message générique unique + aucune donnée utilisateur
dans la réponse, quel que soit le cas réel).

## Acteurs et permissions

- **Recruteur/utilisateur interne non authentifié** (candidat exclus : cette page
  n'existe que côté `recruiter-app`) : seul acteur de ce flux, avant toute connexion.
  Aucune permission particulière : l'endpoint est `[AllowAnonymous]`
  (`Controllers/AuthController.cs:108-110`), confirmé sans autre attribut
  d'autorisation.

## Contrat d'interface

### Endpoint (inchangé, déjà existant)

`POST /api/auth/forgot-password`, `[AllowAnonymous]`.

Requête (`ForgotPasswordDto`, `DTOs/Auth/ForgotPasswordDto.cs`) :

```json
{ "email": "string (requis, format email)" }
```

Réponse : `ServiceResult<AuthResponseDto>` sérialisé (`isSuccess`, `message`,
`statusCode`, `data`, `errors`). Trois cas possibles côté backend, **dont le contenu
exact ne doit jamais être affiché tel quel côté UI** (voir règle d'affichage générique
ci-dessous) :

- `200`, `isSuccess: true` — compte existant ou non (indistinct par design voulu, même
  si `data`/`message` diffèrent aujourd'hui, voir section précédente).
- `422`, `isSuccess: false`, `errors: string[]` — échec de validation FluentValidation
  (`ForgotPasswordDtoValidator` : email vide ou format invalide). Ce cas ne devrait pas
  survenir en pratique grâce à la validation client déjà présente sur `q-input`, sauf
  contournement (devtools, extension navigateur, etc.).
- `400`, `isSuccess: false`, `message: "Une erreur est survenue lors du traitement de
votre demande"` — exception inattendue côté serveur (catch-all).

### Frontend — méthode de service déjà existante, à réutiliser telle quelle

`src/services/authService.ts:110-112` expose déjà, non utilisée par aucune page
aujourd'hui :

```ts
public async requestPasswordReset(email: string): Promise<ResponseResult<boolean> | null> {
  return this.post('/forgot-password', { email });
}
```

Cette méthode est correctement câblée sur le bon endpoint avec le bon payload — **la
réutiliser sans la dupliquer**. Un seul ajustement de typage est requis : le type de
retour annoncé (`ResponseResult<boolean>`) ne correspond pas à ce que l'API renvoie
réellement (`AuthResponseDto`, jamais un `boolean`) ; corriger la signature en
`Promise<ResponseResult<AuthResponseDto> | null>` pour la cohérence TypeScript (aucun
champ de `data` n'est cependant destiné à être lu par l'appelant, voir règle
d'affichage générique — ce correctif est une question de typage strict, pas de
comportement).

Aucune nouvelle méthode de service, aucun nouveau DTO frontend à créer.

## Comportement cible — `ForgotPasswordPage.vue`

### Règle d'affichage générique (enumeration-safety côté UI, priorité sur tout le reste)

Quel que soit le contenu réel de `message`/`data` renvoyé par le backend en cas de
succès (`isSuccess: true`), la page affiche **toujours le même texte générique**, déjà
présent dans le template actuel et à conserver tel quel :

> « Nous avons envoyé un lien de réinitialisation à `{{ email }}`. Vérifiez votre boîte
> de réception et suivez les instructions. »

Le champ `message`/`data` de la réponse backend n'est ni affiché, ni loggé en `console`,
ni utilisé pour une quelconque branche conditionnelle. Seul `isSuccess` (et
implicitement le statut HTTP) pilote la navigation entre états de la page.

### État initial (formulaire) → `handleForgotPassword`

1. Le `q-form` existant déclenche déjà la validation client (`required`, format email)
   avant `@submit` — comportement Quasar existant, ne pas dupliquer de validation
   manuelle supplémentaire.
2. `isLoading = true`.
3. Appeler `authService.requestPasswordReset(email.value)`.
4. Résultat :
   - `result === null` (échec réseau bas niveau, ex. `BaseClient.post` a levé une
     exception sans corps de réponse — timeout, service injoignable, CORS) **ou**
     `result?.isSuccess === false` avec `result?.statusCode !== 422` (échec serveur
     générique, cas `400` catch-all décrit ci-dessus) : rester sur l'état formulaire
     (`emailSent` reste `false`), afficher une notification d'erreur **générique**, texte
     fixe côté frontend (ne pas afficher `result?.message`, pour ne pas dépendre d'un
     texte backend qui pourrait changer sans revue) via
     `useNotification().showErrorNotification(...)`, par exemple : « Une erreur est
     survenue. Veuillez réessayer. ». Le champ email reste rempli pour permettre une
     nouvelle tentative.
   - `result?.isSuccess === false` avec `result?.statusCode === 422` : cas de validation
     format (ne devrait survenir qu'en cas de contournement de la validation client) —
     afficher `result.errors` (ces messages ne concernent que le format de l'email, pas
     l'existence d'un compte : pas de risque d'enumeration à les afficher tels quels),
     rester sur l'état formulaire.
   - `result?.isSuccess === true` : `emailSent.value = true`, démarrer le cooldown de
     renvoi (`startResendCooldown()`, logique déjà présente, inchangée).
5. `isLoading = false` dans tous les cas (`finally`).

### État succès (`emailSent === true`) → `resendEmail`

Comportement identique à `handleForgotPassword` (même appel, même règle d'affichage
générique, même gestion d'erreur), avec deux différences :

- Le cooldown de 60 secondes déjà implémenté (`resendCooldown`, `startResendCooldown`)
  continue d'empêcher les clics rapprochés côté client — **à conserver tel quel**. Ce
  cooldown reste une protection **UX uniquement, pas une protection de sécurité** :
  vérifié dans `Controllers/AuthController.cs`, l'action `ForgotPassword` ne porte
  aucun attribut `[EnableRateLimiting(...)]` (contrairement à
  `POST /auth/resend-confirmation`, qui en a un) — aucune limite serveur par IP
  n'existe aujourd'hui sur cet endpoint. Ce constat est mentionné pour mémoire ; corriger
  ce manque de rate limiting serveur est hors périmètre de cette spec (ticket backend
  potentiel, non traité ici).
- En cas de nouvelle erreur au renvoi, rester dans l'état succès (ne pas repasser
  `emailSent` à `false`), afficher la même notification d'erreur générique ; le
  compte à rebours n'est pas relancé si l'appel échoue (il ne doit démarrer/redémarrer
  que sur un `isSuccess === true`).

Introduire un indicateur de chargement dédié au renvoi (ex. `isResending`, séparé de
`isLoading` qui ne concerne que la soumission initiale) pour désactiver visuellement le
lien « renvoyer » pendant l'appel, en plus du désactivage déjà existant piloté par
`resendCooldown > 0`.

### Ce qui ne change pas

- Structure du template (état formulaire vs état succès), style, lien « Retour à la
  connexion », logique de compte à rebours (`startResendCooldown`, nettoyage dans
  `onUnmounted`).
- Aucune redirection automatique après succès (l'utilisateur reste sur la page,
  cohérent avec le pattern déjà en place côté `candidate-app` pour un flux similaire).

## Cas limites

- Double-clic rapide sur « Envoyer » avant la fin du premier appel : le bouton porte
  déjà `:loading="isLoading"`, ce qui désactive nativement les clics suivants côté
  Quasar (`q-btn` avec `loading` désactive le bouton) — aucun garde supplémentaire
  requis.
- Email saisi avec des espaces ou une casse différente : aucun trim/normalisation
  n'est appliqué aujourd'hui côté page ni côté `ForgotPasswordDtoValidator` (validation
  `EmailAddress` uniquement) — ne pas en ajouter, hors périmètre, non demandé.
- Navigation hors de la page pendant un appel en cours (`isLoading === true`) : aucun
  garde de navigation à ajouter (comportement identique aux autres pages d'auth du
  package, pas de pattern de confirmation de sortie existant à répliquer ici).

## Critères d'acceptation vérifiables

Vérifiables sans dépendre d'une réception réelle d'email (cf. limite backend documentée
ci-dessus) :

1. Soumettre le formulaire avec un email valide déclenche un appel réseau réel
   `POST /api/auth/forgot-password` avec le payload `{ "email": "<valeur saisie>" }`
   (vérifiable via l'onglet réseau ou un intercepteur de test) — plus aucun `setTimeout`
   ni `console.log` dans `handleForgotPassword`/`resendEmail`
   (`grep -n "setTimeout\|console.log" src/pages/auth/ForgotPasswordPage.vue` ne
   retourne plus aucune ligne dans ces deux fonctions).
2. Que le compte existe ou non côté backend (tester avec un email connu en base et un
   email inconnu), la réponse `200`/`isSuccess: true` produit exactement le même texte
   affiché côté UI (« Nous avons envoyé un lien de réinitialisation à ... ») — aucune
   différence de comportement observable dans l'UI entre les deux cas.
3. Simuler une réponse `422` (email invalide ayant contourné la validation client, ou
   test direct de la fonction avec un mock) : le formulaire reste affiché, les messages
   d'erreur de validation sont montrés, `emailSent` reste `false`.
4. Simuler une réponse `400`/échec réseau (mock, ou arrêt du backend) : une notification
   d'erreur générique s'affiche (texte fixe frontend, jamais le message backend brut),
   le formulaire reste affiché et réutilisable, `isLoading` repasse à `false`.
5. Après un succès, cliquer sur « renvoyer » déclenche un nouvel appel réseau réel au
   même endpoint (vérifiable), respecte toujours le cooldown de 60 secondes déjà
   implémenté, et applique la même règle d'affichage générique/gestion d'erreur qu'au
   point 2/4.
6. `authService.requestPasswordReset` a un type de retour corrigé
   (`Promise<ResponseResult<AuthResponseDto> | null>`), `npm run build` (ou `vue-tsc`)
   ne remonte aucune erreur de type sur ce fichier.
7. La section « Dépendance backend / limite connue » de cette spec est reflétée dans le
   commentaire de code ou la description de la PR associée (ex. courte mention que
   l'envoi réel d'email reste un ticket backend séparé) — vérifiable par relecture du
   diff/de la PR, pas par un test automatisé.

## Hors périmètre

- Envoi réel de l'email de réinitialisation côté backend (nouveau template
  `PasswordReset`, appel `IEmailNotificationService` depuis `ForgotPasswordAsync`) —
  ticket backend séparé recommandé, non traité ici.
- Correction de l'enumeration-safety de la réponse HTTP brute de `ForgotPasswordAsync`
  (message et `data.User` différents selon l'existence du compte) — défaut backend
  préexistant, neutralisé uniquement côté UI par cette spec (règle d'affichage
  générique), pas corrigé à la source. **[À CONFIRMER]** si un ticket backend dédié doit
  être ouvert.
- Ajout d'un rate limiting serveur sur `POST /auth/forgot-password` (contrairement à
  `POST /auth/resend-confirmation` qui en a un) — constaté, non traité ici.
- `ResetPasswordPage.vue` (saisie du nouveau mot de passe avec le token reçu par email) :
  n'existe pas aujourd'hui côté `recruiter-app`, non demandée, non créée par cette spec.
- `authService.resetPassword()` (appelle à tort `/admin-reset-password` avec un DTO sans
  token) et `ProfilePage.vue`/`changePassword()` (utilise `authStore.token`, un jeton
  d'accès JWT, comme s'il s'agissait d'un token de réinitialisation par email) : bugs
  préexistants, sans rapport avec `ForgotPasswordPage.vue`, non corrigés ici — voir
  `fix-profile-page-update-and-logout.md` pour le périmètre déjà couvert sur
  `ProfilePage.vue` (n'inclut pas `changePassword`).
- Normalisation de la saisie email (trim, minuscule) : non demandée.

## Fichiers à modifier (récapitulatif)

- `src/pages/auth/ForgotPasswordPage.vue` : `handleForgotPassword`/`resendEmail` appellent
  réellement `authService.requestPasswordReset`, ajout d'un état d'erreur générique et
  d'un indicateur `isResending`, suppression du `setTimeout`/`console.log`.
- `src/services/authService.ts` : correction du type de retour de
  `requestPasswordReset` (`ResponseResult<AuthResponseDto>` au lieu de
  `ResponseResult<boolean>`), aucun changement de logique.

Aucun changement dans `src/models/auth.ts` (le payload `{ email }` ne nécessite pas de
nouveau type nommé), ni dans `authStore.ts` (ce flux ne modifie pas l'état
d'authentification), ni côté backend, ni côté `candidate-app`.
