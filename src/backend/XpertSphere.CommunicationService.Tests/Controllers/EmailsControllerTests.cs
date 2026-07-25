using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using XpertSphere.CommunicationService.Controllers;
using XpertSphere.CommunicationService.Dto;
using XpertSphere.CommunicationService.Exceptions;
using XpertSphere.CommunicationService.Services.Interfaces;

namespace XpertSphere.CommunicationService.Tests.Controllers;

public class EmailsControllerTests
{
    private readonly Mock<IEmailService> _emailServiceMock = new();
    private readonly EmailsController _sut;

    public EmailsControllerTests()
    {
        _sut = new EmailsController(_emailServiceMock.Object, Mock.Of<ILogger<EmailsController>>());
    }

    private static SendTemplatedEmailRequestDto BuildRequest() => new()
    {
        TemplateName = "AccountActivation",
        To = "candidate@example.com",
        TemplateData = new Dictionary<string, string> { ["ActivationLink"] = "https://example.com/activate/abc123" },
        Language = "fr-FR"
    };

    [Fact]
    public async Task Send_WhenEmailServiceSucceeds_ReturnsOkWithMessageId()
    {
        _emailServiceMock
            .Setup(s => s.SendTemplatedEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("message-id-123");

        var result = await _sut.Send(BuildRequest(), CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<SendEmailResponseDto>().Subject;
        response.MessageId.Should().Be("message-id-123");
        response.Status.Should().Be("Sent");
    }

    [Fact]
    public async Task Send_WhenTemplateNotFound_ReturnsBadRequest()
    {
        _emailServiceMock
            .Setup(s => s.SendTemplatedEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TemplateNotFoundException("DoesNotExist", "fr-FR"));

        var result = await _sut.Send(BuildRequest(), CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Send_WhenTemplateVariableMissing_ReturnsBadRequest()
    {
        _emailServiceMock
            .Setup(s => s.SendTemplatedEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new MissingTemplateVariableException("AccountActivation", ["ActivationLink"]));

        var result = await _sut.Send(BuildRequest(), CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Send_WhenSmtpSendFails_ReturnsStatus502()
    {
        _emailServiceMock
            .Setup(s => s.SendTemplatedEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new EmailSendException("Failed to send email via SMTP", new InvalidOperationException("boom")));

        var result = await _sut.Send(BuildRequest(), CancellationToken.None);

        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(502);
    }
}
