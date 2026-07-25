using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using XpertSphere.CommunicationService.Middleware;

namespace XpertSphere.CommunicationService.Tests.Middleware;

public class ApiKeyMiddlewareTests
{
    private static IConfiguration BuildConfiguration(string apiKey) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ApiKey"] = apiKey })
            .Build();

    private static DefaultHttpContext BuildContext(string path, string? apiKeyHeader)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        if (apiKeyHeader is not null)
        {
            context.Request.Headers["X-Api-Key"] = apiKeyHeader;
        }

        return context;
    }

    [Fact]
    public async Task InvokeAsync_ForNonApiPath_SkipsValidationAndCallsNext()
    {
        var nextCalled = false;
        var middleware = new ApiKeyMiddleware(_ => { nextCalled = true; return Task.CompletedTask; }, BuildConfiguration("expected-key"));
        var context = BuildContext("/health", apiKeyHeader: null);

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task InvokeAsync_ForApiPathWithValidKey_CallsNext()
    {
        var nextCalled = false;
        var middleware = new ApiKeyMiddleware(_ => { nextCalled = true; return Task.CompletedTask; }, BuildConfiguration("expected-key"));
        var context = BuildContext("/api/emails/send", apiKeyHeader: "expected-key");

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_ForApiPathWithMissingKey_ReturnsUnauthorizedWithoutCallingNext()
    {
        var nextCalled = false;
        var middleware = new ApiKeyMiddleware(_ => { nextCalled = true; return Task.CompletedTask; }, BuildConfiguration("expected-key"));
        var context = BuildContext("/api/emails/send", apiKeyHeader: null);

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public async Task InvokeAsync_ForApiPathWithInvalidKey_ReturnsUnauthorizedWithoutCallingNext()
    {
        var nextCalled = false;
        var middleware = new ApiKeyMiddleware(_ => { nextCalled = true; return Task.CompletedTask; }, BuildConfiguration("expected-key"));
        var context = BuildContext("/api/emails/send", apiKeyHeader: "wrong-key");

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
        var body = await reader.ReadToEndAsync();
        body.Should().Contain("Missing or invalid API key");
    }
}
