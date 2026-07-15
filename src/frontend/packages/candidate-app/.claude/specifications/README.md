# Spécifications — candidate-app

Un fichier Markdown par fonctionnalité spécifiée (nommage `<slug-fonctionnalite>.md`), rédigé par l'agent spec-writer avant tout développement.

- `candidate-registration-training-validation-error.md` : formulaire d'inscription candidat (`MultiStepRegisterForm.vue`) — gating défaillant de l'étape "Formations" (identique au défaut déjà documenté pour "Expériences" dans la spec sœur côté `XpertSphere.MonolithApi`) et absence de normalisation des réponses d'erreur backend (`ValidationProblemDetails`/`ServiceResult`) dans `authStore`.
