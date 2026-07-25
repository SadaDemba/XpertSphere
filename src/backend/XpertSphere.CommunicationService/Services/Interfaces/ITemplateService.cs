using XpertSphere.CommunicationService.Models;

namespace XpertSphere.CommunicationService.Services.Interfaces;

public interface ITemplateService
{
    Task<MessageTemplate?> GetTemplateAsync(string templateName, string language = "fr-FR", CancellationToken cancellationToken = default);

    // Valide l'existence du template et la présence des variables requises (template.Variables),
    // puis retourne le sujet/corps rendus (placeholders substitués).
    Task<RenderedTemplate> RenderTemplateAsync(string templateName, Dictionary<string, string> data, string language = "fr-FR", CancellationToken cancellationToken = default);
}

public class RenderedTemplate
{
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsHtml { get; set; } = true;
}
