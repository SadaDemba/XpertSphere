namespace XpertSphere.MonolithApi.DTOs.Communication;

/// <summary>
/// Payload envoyé à <c>POST /api/emails/send</c> de XpertSphere.CommunicationService
/// (voir email-sending-foundation.md, ticket A, déjà mergé dans develop).
/// </summary>
public class SendTemplatedEmailRequestDto
{
    public string TemplateName { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public Dictionary<string, string> TemplateData { get; set; } = new();
    public string Language { get; set; } = "fr-FR";
}

/// <summary>
/// Réponse renvoyée par <c>POST /api/emails/send</c> en cas de succès.
/// </summary>
public class SendEmailResponseDto
{
    public string MessageId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
