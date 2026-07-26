# Spécifications — candidate-app

Un fichier Markdown par fonctionnalité spécifiée (nommage `<slug-fonctionnalite>.md`), rédigé par l'agent spec-writer avant tout développement.

- `fix-devise-salaire-souhaite-eur-vers-xof.md` — correctif d'affichage : le salaire souhaité du candidat (`desiredSalary`) est formaté en EUR/€ alors qu'il doit l'être en XOF (Franc CFA). Couvre `candidate-app` et les vues en lecture seule de `recruiter-app`.
- `candidate-registration-training-validation-error.md` : formulaire d'inscription candidat (`MultiStepRegisterForm.vue`) — gating défaillant de l'étape "Formations" (identique au défaut déjà documenté pour "Expériences" dans la spec sœur côté `XpertSphere.MonolithApi`) et absence de normalisation des réponses d'erreur backend (`ValidationProblemDetails`/`ServiceResult`) dans `authStore`.
- `job-details-page-redesign.md` : refonte visuelle de `JobDetailsPage.vue` — correctif du bug de layout deux colonnes (`q-gutter-lg` → `q-col-gutter-lg`), sidebar sticky sur desktop, bloc "Autres offres de cette entreprise", bouton de partage et alignement du format de date de publication sur `JobCard.vue`.
