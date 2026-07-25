using FluentAssertions;
using XpertSphere.CommunicationService.Exceptions;
using XpertSphere.CommunicationService.Services;

namespace XpertSphere.CommunicationService.Tests.Services;

public class TemplateServiceTests
{
    private readonly TemplateService _sut = new();

    [Fact]
    public async Task GetTemplateAsync_WithSeededTemplate_ReturnsTemplate()
    {
        var template = await _sut.GetTemplateAsync("AccountActivation", "fr-FR");

        template.Should().NotBeNull();
        template!.Subject.Should().Be("Activez votre compte XpertSphere");
    }

    [Fact]
    public async Task GetTemplateAsync_WithUnknownTemplate_ReturnsNull()
    {
        var template = await _sut.GetTemplateAsync("DoesNotExist", "fr-FR");

        template.Should().BeNull();
    }

    [Fact]
    public async Task RenderTemplateAsync_WithAllVariables_SubstitutesPlaceholders()
    {
        var data = new Dictionary<string, string>
        {
            ["ActivationLink"] = "https://example.com/activate/abc123"
        };

        var rendered = await _sut.RenderTemplateAsync("AccountActivation", data, "fr-FR");

        rendered.Subject.Should().Be("Activez votre compte XpertSphere");
        rendered.Body.Should().Contain("https://example.com/activate/abc123");
        rendered.Body.Should().NotContain("{{ActivationLink}}");
        rendered.IsHtml.Should().BeTrue();
    }

    [Fact]
    public async Task RenderTemplateAsync_WithUnknownTemplate_ThrowsTemplateNotFoundException()
    {
        var data = new Dictionary<string, string>();

        var act = async () => await _sut.RenderTemplateAsync("DoesNotExist", data, "fr-FR");

        await act.Should().ThrowAsync<TemplateNotFoundException>();
    }

    [Fact]
    public async Task RenderTemplateAsync_WithMissingVariable_ThrowsMissingTemplateVariableException()
    {
        var data = new Dictionary<string, string>();

        var act = async () => await _sut.RenderTemplateAsync("AccountActivation", data, "fr-FR");

        await act.Should().ThrowAsync<MissingTemplateVariableException>()
            .WithMessage("*ActivationLink*");
    }
}
