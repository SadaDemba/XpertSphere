# Spécifications — XpertSphere.CommunicationService

Un fichier Markdown par fonctionnalité spécifiée (nommage `<slug-fonctionnalite>.md`), rédigé par l'agent spec-writer avant tout développement.

- [`email-sending-foundation.md`](email-sending-foundation.md) — ticket A du système d'envoi de mail générique : envoi d'email templatisé synchrone (`POST /api/emails/send`), rendre le service réellement exécutable (`Program.cs`, Dockerfile, `docker-compose.yml`), catcher SMTP local smtp4dev en Development et provider Brevo en Staging, protection minimale par clé partagée `X-Api-Key`. Le branchement réel depuis `MonolithApi` (email d'activation de compte) est le ticket B, non couvert ici.
