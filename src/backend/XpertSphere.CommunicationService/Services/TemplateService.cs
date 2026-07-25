using XpertSphere.CommunicationService.Exceptions;
using XpertSphere.CommunicationService.Models;
using XpertSphere.CommunicationService.Services.Interfaces;

namespace XpertSphere.CommunicationService.Services;

public class TemplateService : ITemplateService
{
    private static readonly Dictionary<(string Name, string Language), MessageTemplate> Templates = new()
    {
        [("AccountActivation", "fr-FR")] = new MessageTemplate
        {
            TemplateId = "AccountActivation-fr-FR",
            Name = "AccountActivation",
            Language = "fr-FR",
            Type = TemplateType.AccountActivation,
            Subject = "Activez votre compte XpertSphere",
            Body = "<p>Bonjour,</p><p>Merci de votre inscription sur XpertSphere. Pour activer votre compte, cliquez sur le lien ci-dessous :</p><p><a href=\"{{ActivationLink}}\">Activer mon compte</a></p><p>Si vous n'êtes pas à l'origine de cette demande, ignorez cet email.</p>",
            Variables = new Dictionary<string, string>
            {
                ["ActivationLink"] = "Lien absolu vers la page d'activation du compte"
            },
            IsActive = true,
            Category = "Account"
        }
    };

    public Task<MessageTemplate?> GetTemplateAsync(string templateName, string language = "fr-FR", CancellationToken cancellationToken = default)
    {
        var template = Templates.GetValueOrDefault((templateName, language));
        return Task.FromResult(template);
    }

    public async Task<RenderedTemplate> RenderTemplateAsync(string templateName, Dictionary<string, string> data, string language = "fr-FR", CancellationToken cancellationToken = default)
    {
        var template = await GetTemplateAsync(templateName, language, cancellationToken);
        if (template is null)
        {
            throw new TemplateNotFoundException(templateName, language);
        }

        var missingVariables = (template.Variables?.Keys ?? Enumerable.Empty<string>())
            .Where(key => !data.ContainsKey(key))
            .ToList();

        if (missingVariables.Count > 0)
        {
            throw new MissingTemplateVariableException(templateName, missingVariables);
        }

        var subject = template.Subject;
        var body = template.Body;

        foreach (var (key, value) in data)
        {
            var placeholder = "{{" + key + "}}";
            subject = subject.Replace(placeholder, value);
            body = body.Replace(placeholder, value);
        }

        return new RenderedTemplate
        {
            Subject = subject,
            Body = body,
            IsHtml = true
        };
    }
}
