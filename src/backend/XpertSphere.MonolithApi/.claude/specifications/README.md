# Spécifications — XpertSphere.MonolithApi

Un fichier Markdown par fonctionnalité spécifiée (nommage `<slug-fonctionnalite>.md`), rédigé par l'agent spec-writer avant tout développement.

Spécifications existantes :
- `azurite-blob-storage-local.md` — stockage Blob (CV/documents) en local via Azurite, coexistant avec la configuration Azure Storage réelle de Staging/Production.
- `secure-cv-download.md` — endpoint proxy authentifié pour consulter/télécharger un CV déjà uploadé (candidat propriétaire + rôles d'organisation), avec section de coordination frontend (`candidate-app`, `recruiter-app`).
