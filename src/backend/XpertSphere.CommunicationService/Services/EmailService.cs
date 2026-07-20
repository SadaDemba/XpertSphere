using Microsoft.Extensions.Options;
using MimeKit;
using XpertSphere.CommunicationService.Exceptions;
using XpertSphere.CommunicationService.Models;
using XpertSphere.CommunicationService.Options;
using XpertSphere.CommunicationService.Services.Interfaces;

namespace XpertSphere.CommunicationService.Services;

public class EmailService(
    ITemplateService templateService,
    IOptions<SmtpOptions> smtpOptions,
    ILogger<EmailService> logger) : IEmailService
{
    private readonly SmtpOptions _smtpOptions = smtpOptions.Value;

    public async Task<string> SendTemplatedEmailAsync(
        string templateName,
        string to,
        Dictionary<string, string> templateData,
        string language = "fr-FR",
        CancellationToken cancellationToken = default)
    {
        var rendered = await templateService.RenderTemplateAsync(templateName, templateData, language, cancellationToken);

        var message = new EmailMessage
        {
            To = to,
            Subject = rendered.Subject,
            Body = rendered.Body,
            IsHtml = rendered.IsHtml,
            TemplateName = templateName,
            TemplateData = templateData
        };

        await SendEmailAsync(message, cancellationToken);

        return message.MessageId;
    }

    public async Task<bool> SendEmailAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var mimeMessage = new MimeMessage();
        mimeMessage.From.Add(MailboxAddress.Parse(_smtpOptions.From));
        mimeMessage.To.Add(MailboxAddress.Parse(message.To));
        mimeMessage.Subject = message.Subject;

        var bodyBuilder = new BodyBuilder();
        if (message.IsHtml)
        {
            bodyBuilder.HtmlBody = message.Body;
        }
        else
        {
            bodyBuilder.TextBody = message.Body;
        }

        mimeMessage.Body = bodyBuilder.ToMessageBody();

        try
        {
            using var client = new MailKit.Net.Smtp.SmtpClient();
            var secureOptions = _smtpOptions.EnableSsl
                ? MailKit.Security.SecureSocketOptions.StartTls
                : MailKit.Security.SecureSocketOptions.None;

            await client.ConnectAsync(_smtpOptions.Host, _smtpOptions.Port, secureOptions, cancellationToken);

            if (!string.IsNullOrEmpty(_smtpOptions.Username))
            {
                await client.AuthenticateAsync(_smtpOptions.Username, _smtpOptions.Password ?? string.Empty, cancellationToken);
            }

            await client.SendAsync(mimeMessage, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email {MessageId} to {To}", message.MessageId, message.To);
            throw new EmailSendException("Failed to send email via SMTP", ex);
        }

        return true;
    }
}
