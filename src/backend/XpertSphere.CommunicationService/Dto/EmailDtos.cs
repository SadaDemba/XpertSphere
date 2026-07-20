using System.ComponentModel.DataAnnotations;

namespace XpertSphere.CommunicationService.Dto;

public class SendTemplatedEmailRequestDto
{
    [Required]
    public string TemplateName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string To { get; set; } = string.Empty;

    public Dictionary<string, string> TemplateData { get; set; } = new();

    public string Language { get; set; } = "fr-FR";
}

public class SendEmailResponseDto
{
    public string MessageId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
