using System.Net.Http.Json;
using XpertSphere.MonolithApi.DTOs.Communication;
using XpertSphere.MonolithApi.Interfaces;

namespace XpertSphere.MonolithApi.Services;

/// <summary>
/// Implémentation HTTP de <see cref="IEmailNotificationService"/>, consommant
/// <c>POST /api/emails/send</c> de XpertSphere.CommunicationService (voir
/// email-sending-foundation.md, ticket A, déjà mergé). Enregistré via
/// <c>AddHttpClient&lt;IEmailNotificationService, EmailNotificationService&gt;</c> dans
/// <c>Program.cs</c> (BaseAddress/X-Api-Key/timeout configurés au moment de l'enregistrement).
/// </summary>
public class EmailNotificationService : IEmailNotificationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<EmailNotificationService> _logger;

    public EmailNotificationService(HttpClient httpClient, ILogger<EmailNotificationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> SendAccountActivationEmailAsync(string email, string activationLink)
    {
        var request = new SendTemplatedEmailRequestDto
        {
            TemplateName = "AccountActivation",
            To = email,
            TemplateData = new Dictionary<string, string> { ["ActivationLink"] = activationLink },
            Language = "fr-FR"
        };

        try
        {
            using var response = await _httpClient.PostAsJsonAsync("api/emails/send", request);
            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            var body = await response.Content.ReadAsStringAsync();
            _logger.LogError(
                "CommunicationService returned {StatusCode} while sending AccountActivation email to {Email}: {Body}",
                (int)response.StatusCode, email, body);
            return false;
        }
        catch (Exception ex)
        {
            // Ne jamais laisser remonter d'exception : un échec d'envoi (service injoignable,
            // timeout, DNS, etc.) ne doit jamais faire échouer/rollback l'appelant (voir §11 de
            // candidate-account-activation-email.md).
            _logger.LogError(ex, "Failed to call CommunicationService to send AccountActivation email to {Email}", email);
            return false;
        }
    }
}
