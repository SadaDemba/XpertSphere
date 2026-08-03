using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit.Abstractions;

namespace XpertSphere.MonolithApi.Tests.Integration;

/// <summary>
/// End-to-end (real HTTP, real ASP.NET Core pipeline, real Identity, no mocked
/// UserManager/DbContext) verification of critères d'acceptation 3, 4, 5 and 9 of
/// .claude/specifications/localize-identity-error-messages.md: closes the gap flagged by the
/// validator on PR #132, where these criteria required a non-mocked HTTP-level check that native
/// ASP.NET Core Identity error messages and DataAnnotations validation messages are returned in
/// French in the HTTP response body.
///
/// Uses <see cref="CustomWebApplicationFactory"/> (WebApplicationFactory&lt;Program&gt; + EF Core
/// InMemory provider) rather than a unit test with a mocked UserManager, precisely because a
/// mocked UserManager never goes through the real FrenchIdentityErrorDescriber nor through the
/// real ASP.NET Core model-binding/validation pipeline that produces ValidationProblemDetails.
/// </summary>
[Trait("Category", "Integration")]
public class IdentityErrorLocalizationIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly ITestOutputHelper _output;

    public IdentityErrorLocalizationIntegrationTests(CustomWebApplicationFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
    }

    /// <summary>
    /// Critère d'acceptation 4 : lien de confirmation d'email invalide/expiré
    /// (<c>ConfirmEmailAsync</c>) -&gt; message entièrement français, plus de "Invalid token" en
    /// anglais concaténé. Uses the PlatformSuperAdmin account seeded by
    /// SeedPlatformSuperAdminAsync (see CustomWebApplicationFactory) - its EmailConfirmed state is
    /// irrelevant here since UserManager.ConfirmEmailAsync fails on token verification before
    /// ever looking at the current confirmation state.
    /// </summary>
    [Fact]
    public async Task ConfirmEmail_WithInvalidToken_ReturnsFrenchMessage_NotEnglish()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/Auth/confirm-email", new
        {
            Email = CustomWebApplicationFactory.SeededAdminEmail,
            Token = "this-is-not-a-valid-token"
        });

        var body = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Status: {(int)response.StatusCode}");
        _output.WriteLine($"Body: {body}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        body.Should().Contain("Jeton invalide");
        body.Should().NotContain("Invalid token");
    }

    /// <summary>
    /// Critère d'acceptation 9 (volet DataAnnotations) : <c>POST /api/Auth/register/candidate</c>
    /// avec <c>Email</c> manquant renvoie un message entièrement français dans le corps de la
    /// réponse HTTP (<c>ValidationProblemDetails</c> produit par le ModelState binding
    /// automatique d'<c>[ApiController]</c>, pas par le contrôleur ni par
    /// AuthenticationService).
    /// </summary>
    [Fact]
    public async Task RegisterCandidate_WithMissingEmail_ReturnsFrenchValidationProblemDetails()
    {
        using var client = _factory.CreateClient();

        using var form = new MultipartFormDataContent
        {
            { new StringContent("Abcdefgh1!"), "Password" },
            { new StringContent("Abcdefgh1!"), "ConfirmPassword" },
            { new StringContent("Jean"), "FirstName" },
            { new StringContent("Dupont"), "LastName" },
            { new StringContent("true"), "AcceptTerms" },
            { new StringContent("true"), "AcceptPrivacyPolicy" }
        };

        var response = await client.PostAsync("/api/Auth/register/candidate", form);

        var body = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Status: {(int)response.StatusCode}");
        _output.WriteLine($"Body: {body}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        body.Should().Contain("obligatoire");

        using var problemDetails = JsonDocument.Parse(body);
        var emailErrors = problemDetails.RootElement.GetProperty("errors").GetProperty("Email");
        var emailErrorMessages = emailErrors.EnumerateArray().Select(e => e.GetString()).ToList();
        emailErrorMessages.Should().ContainSingle(m => m != null && m.Contains("obligatoire"));
    }

    /// <summary>
    /// Critère d'acceptation 3 : <c>POST /api/Auth/register/candidate</c> avec un mot de passe
    /// respectant la longueur (8 caractères, valide dans les deux environnements) mais sans
    /// caractère non-alphanumérique -&gt; message entièrement français
    /// (<c>PasswordRequiresNonAlphanumeric</c> via <see cref="Utils.FrenchIdentityErrorDescriber"/>),
    /// sans aucun fragment anglais résiduel.
    /// </summary>
    [Fact]
    public async Task RegisterCandidate_WithNonAlphanumericMissingPassword_ReturnsFrenchMessage_NotEnglish()
    {
        using var client = _factory.CreateClient();

        var uniqueEmail = $"candidate-{Guid.NewGuid():N}@example.com";
        using var form = new MultipartFormDataContent
        {
            { new StringContent(uniqueEmail), "Email" },
            { new StringContent("Abcdefgh1"), "Password" },
            { new StringContent("Abcdefgh1"), "ConfirmPassword" },
            { new StringContent("Jean"), "FirstName" },
            { new StringContent("Dupont"), "LastName" },
            { new StringContent("true"), "AcceptTerms" },
            { new StringContent("true"), "AcceptPrivacyPolicy" }
        };

        var response = await client.PostAsync("/api/Auth/register/candidate", form);

        var body = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Status: {(int)response.StatusCode}");
        _output.WriteLine($"Body: {body}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        body.Should().Contain("caract");
        (body.Contains("spécial") || body.Contains("sp\\u00E9cial")).Should().BeTrue();
        body.Should().NotContain("alphanumeric");
        body.Should().NotContain("Passwords must");
    }

    /// <summary>
    /// Critère d'acceptation 5 : réinitialisation de mot de passe admin
    /// (<c>AdminResetPasswordAsync</c>, flux <c>recruiter-app/UsersPage.vue</c>) avec un mot de
    /// passe ne respectant pas la complexité (règle client ne vérifiant que la longueur) ->
    /// message entièrement français. Logs in as the seeded PlatformSuperAdmin (real JWT, real
    /// CanResetPasswords policy evaluation) then targets its own account to avoid any
    /// cross-organization authorization branch.
    /// </summary>
    [Fact]
    public async Task AdminResetPassword_WithNonAlphanumericMissingPassword_ReturnsFrenchMessage_NotEnglish()
    {
        using var client = _factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/Auth/login", new
        {
            Email = CustomWebApplicationFactory.SeededAdminEmail,
            Password = CustomWebApplicationFactory.SeededAdminPassword
        });

        var loginBody = await loginResponse.Content.ReadAsStringAsync();
        _output.WriteLine($"Login status: {(int)loginResponse.StatusCode}");
        _output.WriteLine($"Login body: {loginBody}");
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var loginDocument = JsonDocument.Parse(loginBody);
        var accessToken = loginDocument.RootElement.GetProperty("data").GetProperty("accessToken").GetString();
        accessToken.Should().NotBeNullOrEmpty();

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var resetResponse = await client.PostAsJsonAsync("/api/Auth/admin-reset-password", new
        {
            Email = CustomWebApplicationFactory.SeededAdminEmail,
            NewPassword = "Abcdefg1",
            ConfirmPassword = "Abcdefg1"
        });

        var resetBody = await resetResponse.Content.ReadAsStringAsync();
        _output.WriteLine($"Reset status: {(int)resetResponse.StatusCode}");
        _output.WriteLine($"Reset body: {resetBody}");

        resetResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        resetBody.Should().Contain("caract");
        resetBody.Should().NotContain("alphanumeric");
        resetBody.Should().NotContain("Passwords must");
    }
}
