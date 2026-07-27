namespace XpertSphere.MonolithApi.Interfaces;

/// <summary>
/// Client HTTP vers XpertSphere.CommunicationService (POST /api/emails/send), voir
/// candidate-account-activation-email.md. Ne lève jamais d'exception : un échec d'envoi (SMTP,
/// réseau, service injoignable) est signalé par un retour <c>false</c>, jamais par une exception
/// qui remonterait jusqu'à un appelant transactionnel (ex. RegisterCandidateAsync, dont l'envoi
/// d'email a lieu après un commit déjà effectué — voir §11 de la spec, aucun rollback ne doit
/// pouvoir être déclenché par un échec d'envoi).
/// </summary>
public interface IEmailNotificationService
{
    /// <summary>
    /// Envoie l'email d'activation de compte (template "AccountActivation", fr-FR). Retourne
    /// <c>true</c> si l'appel à CommunicationService a réussi (200), <c>false</c> dans tous les
    /// autres cas (erreur HTTP, timeout, service injoignable) — jamais d'exception.
    /// </summary>
    Task<bool> SendAccountActivationEmailAsync(string email, string activationLink);
}
