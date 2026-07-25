using XpertSphere.CommunicationService.Models;

namespace XpertSphere.CommunicationService.Services.Interfaces;

public interface IEmailService
{
    // Retourne true si l'envoi SMTP a réussi. En cas d'échec, une exception
    // (EmailSendException) est levée — cette méthode ne retourne jamais false :
    // c'est une simplification assumée du contrat bool d'origine, ambigu.
    Task<bool> SendEmailAsync(EmailMessage message, CancellationToken cancellationToken = default);

    // Retourne le MessageId de l'EmailMessage effectivement envoyé.
    // Lève TemplateNotFoundException / MissingTemplateVariableException (échec de rendu, mappé 400 côté controller)
    // ou EmailSendException (échec SMTP, mappé 502 côté controller).
    Task<string> SendTemplatedEmailAsync(
        string templateName,
        string to,
        Dictionary<string, string> templateData,
        string language = "fr-FR",
        CancellationToken cancellationToken = default);
}
