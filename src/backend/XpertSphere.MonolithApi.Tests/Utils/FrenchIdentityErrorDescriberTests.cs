using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using XpertSphere.MonolithApi.Utils;

namespace XpertSphere.MonolithApi.Tests.Utils;

/// <summary>
/// Covers .claude/specifications/localize-identity-error-messages.md, critère d'acceptation 1 :
/// <see cref="FrenchIdentityErrorDescriber"/> surcharge exhaustivement toutes les méthodes
/// virtuelles publiques exposées par <see cref="IdentityErrorDescriber"/> pour la version du SDK
/// réellement utilisée par le projet (vérifié par réflexion, pas par une liste figée à l'avance :
/// une méthode ajoutée/retirée entre versions du SDK est détectée automatiquement par ce test).
/// </summary>
public class FrenchIdentityErrorDescriberTests
{
    [Fact]
    public void AllVirtualMethods_ShouldBeOverriddenByFrenchDescriber()
    {
        var baseType = typeof(IdentityErrorDescriber);
        var derivedType = typeof(FrenchIdentityErrorDescriber);

        var virtualMethods = baseType
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.DeclaringType == baseType && m.IsVirtual)
            .ToList();

        // Garde-fou : si le SDK change ce nombre, ce test doit être relu (pas juste ajusté à l'aveugle).
        virtualMethods.Should().HaveCount(22,
            "la spec a été rédigée sur la base de 22 méthodes virtuelles pour le SDK utilisé (9.0.7) ; " +
            "un écart signifie que la version du SDK a changé et que la liste doit être revérifiée");

        var notOverridden = virtualMethods
            .Where(baseMethod =>
            {
                var overridden = derivedType.GetMethod(
                    baseMethod.Name,
                    BindingFlags.Public | BindingFlags.Instance,
                    binder: null,
                    types: baseMethod.GetParameters().Select(p => p.ParameterType).ToArray(),
                    modifiers: null);

                return overridden is null || overridden.DeclaringType != derivedType;
            })
            .Select(m => m.Name)
            .ToList();

        notOverridden.Should().BeEmpty(
            "toutes les méthodes virtuelles de IdentityErrorDescriber doivent être surchargées par FrenchIdentityErrorDescriber");
    }

    [Fact]
    public void PasswordTooShort_ShouldInterpolateLength()
    {
        var describer = new FrenchIdentityErrorDescriber();

        var error = describer.PasswordTooShort(8);

        error.Description.Should().Be("Le mot de passe doit contenir au moins 8 caractères.");
        error.Code.Should().Be("PasswordTooShort");
    }

    [Fact]
    public void PasswordRequiresNonAlphanumeric_ShouldReturnFrenchMessage()
    {
        var describer = new FrenchIdentityErrorDescriber();

        var error = describer.PasswordRequiresNonAlphanumeric();

        error.Description.Should().Be("Le mot de passe doit contenir au moins un caractère spécial (non alphanumérique).");
        error.Code.Should().Be("PasswordRequiresNonAlphanumeric");
    }

    [Fact]
    public void InvalidToken_ShouldReturnFrenchMessage()
    {
        var describer = new FrenchIdentityErrorDescriber();

        var error = describer.InvalidToken();

        error.Description.Should().Be("Jeton invalide.");
        error.Code.Should().Be("InvalidToken");
    }

    [Fact]
    public void DuplicateEmail_ShouldPreservePlaceholder()
    {
        var describer = new FrenchIdentityErrorDescriber();

        var error = describer.DuplicateEmail("test@example.com");

        error.Description.Should().Be("L'email « test@example.com » est déjà utilisé.");
    }

    [Fact]
    public async Task PasswordValidator_WithRealDescriber_ShouldReturnFrenchNonAlphanumericMessage()
    {
        // Exerce le vrai chemin runtime (PasswordValidator<TUser> d'Identity + le descripteur réel),
        // pas seulement un IdentityError construit à la main : un mot de passe respectant longueur/
        // majuscule/minuscule/chiffre mais sans caractère spécial doit produire le message français.
        var describer = new FrenchIdentityErrorDescriber();
        var validator = new PasswordValidator<IdentityUser>(describer);

        var options = new IdentityOptions
        {
            Password =
            {
                RequireDigit = true,
                RequireLowercase = true,
                RequireUppercase = true,
                RequireNonAlphanumeric = true,
                RequiredLength = 8,
                RequiredUniqueChars = 1
            }
        };
        var identityOptions = Options.Create(options);

        var store = new Mock<IUserStore<IdentityUser>>();
        var userManager = new UserManager<IdentityUser>(
            store.Object, identityOptions, new PasswordHasher<IdentityUser>(),
            new List<IUserValidator<IdentityUser>>(), new List<IPasswordValidator<IdentityUser>> { validator },
            new UpperInvariantLookupNormalizer(), describer,
            new Mock<IServiceProvider>().Object,
            new Mock<ILogger<UserManager<IdentityUser>>>().Object);

        var result = await validator.ValidateAsync(userManager, new IdentityUser(), "Abcdefg1");

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.Description == "Le mot de passe doit contenir au moins un caractère spécial (non alphanumérique).");
    }

    [Fact]
    public void AddErrorDescriber_ShouldFlowThroughToPasswordValidator_ViaDependencyInjection()
    {
        // PasswordValidator<TUser>.Describer vient de son propre paramètre de constructeur
        // (optionnel, résolu par le conteneur DI), pas de UserManager.ErrorDescriber (vérifié par
        // décompilation de Microsoft.Extensions.Identity.Core 9.0.7 : le constructeur fait
        // `Describer = errors ?? new IdentityErrorDescriber();`). Ce test reproduit donc le
        // graphe DI réel (même mécanisme que AddIdentity<...>().AddErrorDescriber<...>() dans
        // SecurityExtensions.AddSecurity) pour prouver que .AddErrorDescriber<FrenchIdentityErrorDescriber>()
        // atteint bien PasswordValidator via injection, et pas seulement UserManager.
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(new Mock<IUserStore<IdentityUser>>().Object);
        services.AddIdentityCore<IdentityUser>()
            .AddErrorDescriber<FrenchIdentityErrorDescriber>();

        using var provider = services.BuildServiceProvider();

        var passwordValidator = provider.GetServices<IPasswordValidator<IdentityUser>>()
            .OfType<PasswordValidator<IdentityUser>>()
            .Should().ContainSingle().Subject;

        passwordValidator.Describer.Should().BeOfType<FrenchIdentityErrorDescriber>();

        var userManager = provider.GetRequiredService<UserManager<IdentityUser>>();
        userManager.ErrorDescriber.Should().BeOfType<FrenchIdentityErrorDescriber>();
    }
}
