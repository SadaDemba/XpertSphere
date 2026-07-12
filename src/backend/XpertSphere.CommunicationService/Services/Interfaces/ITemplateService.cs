using XpertSphere.CommunicationService.Models;

namespace XpertSphere.CommunicationService.Services.Interfaces;

public interface ITemplateService
{
    Task<MessageTemplate?> GetTemplateAsync(string templateName, string language = "fr-FR", CancellationToken cancellationToken = default);
    Task<MessageTemplate> CreateTemplateAsync(MessageTemplate template, CancellationToken cancellationToken = default);
    Task<MessageTemplate?> UpdateTemplateAsync(string templateId, MessageTemplate template, CancellationToken cancellationToken = default);
    Task<bool> DeleteTemplateAsync(string templateId, CancellationToken cancellationToken = default);
    Task<List<MessageTemplate>> GetAllTemplatesAsync(bool activeOnly = true, CancellationToken cancellationToken = default);
    Task<string> RenderTemplateAsync(string templateName, Dictionary<string, string> data, string language = "fr-FR", CancellationToken cancellationToken = default);
}
