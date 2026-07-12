using XpertSphere.CommunicationService.Models;

namespace XpertSphere.CommunicationService.Services.Interfaces;

public interface IEmailService
{
    Task<bool> SendEmailAsync(EmailMessage message, CancellationToken cancellationToken = default);
    Task<bool> SendBulkEmailsAsync(List<EmailMessage> messages, CancellationToken cancellationToken = default);
    Task<bool> SendTemplatedEmailAsync(string templateName, string to, Dictionary<string, string> templateData, CancellationToken cancellationToken = default);
    Task<EmailMessage?> GetEmailStatusAsync(string messageId, CancellationToken cancellationToken = default);
    Task<bool> CancelScheduledEmailAsync(string messageId, CancellationToken cancellationToken = default);
    Task<bool> RetryFailedEmailAsync(string messageId, CancellationToken cancellationToken = default);
}
