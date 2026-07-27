using AutoMapper;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using XpertSphere.MonolithApi.Config;
using XpertSphere.MonolithApi.Data;
using XpertSphere.MonolithApi.DTOs.Auth;
using XpertSphere.MonolithApi.DTOs.ExperienceDtos;
using XpertSphere.MonolithApi.DTOs.TrainingDtos;
using XpertSphere.MonolithApi.Extensions;
using XpertSphere.MonolithApi.Interfaces;
using XpertSphere.MonolithApi.Mappings;
using XpertSphere.MonolithApi.Models;
using XpertSphere.MonolithApi.Models.Base;
using XpertSphere.MonolithApi.Services;
using XpertSphere.MonolithApi.Tests.Helpers;

namespace XpertSphere.MonolithApi.Tests.Services;

/// <summary>
/// Covers .claude/specifications/login-response-missing-experiences-trainings.md:
/// <see cref="AuthenticationService.LoginAsync"/> and <see cref="AuthenticationService.RefreshTokenAsync"/>
/// must return a <c>UserDto</c> with <c>Address</c>/<c>Experiences</c>/<c>Trainings</c> populated, exactly
/// like <see cref="AuthenticationService.GetCurrentUserAsync"/> already does.
///
/// Unlike <see cref="AuthenticationServiceTests"/> (which uses a mock <see cref="IMapper"/> that has no
/// mapping configured for <c>User -&gt; AuthResponseDto</c>, so its Login/Refresh tests only ever exercise
/// the generic catch-block), these tests use a real <see cref="MapperConfiguration"/> built from the
/// production mapping profiles and a real EF Core InMemory-backed <c>Users</c> queryable, so the
/// <c>.Include(...)</c> chains under test actually run against tracked navigation data.
/// </summary>
public class AuthenticationServiceProfileCompletenessTests : IDisposable
{
    private readonly XpertSphereDbContext _context;
    private readonly Mock<UserManager<User>> _mockUserManager;
    private readonly Mock<SignInManager<User>> _mockSignInManager;
    private readonly Mock<IWebHostEnvironment> _mockEnvironment;
    private readonly IMapper _mapper;

    public AuthenticationServiceProfileCompletenessTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext(Guid.NewGuid().ToString());
        _mockUserManager = MockHelper.CreateMockUserManager();
        _mockSignInManager = MockHelper.CreateMockSignInManager(_mockUserManager);
        _mockEnvironment = new Mock<IWebHostEnvironment>();

        // Development environment => ShouldUseEntraId is false, so LoginAsync/RegisterCandidateAsync
        // take the local JWT path (no redirect to Entra ID) - same setup as AuthenticationServiceTests.
        _mockEnvironment.Setup(x => x.EnvironmentName).Returns("Development");

        // Real AutoMapper configuration (all production profiles), needed because
        // AutoMapperHelper.CreateMapper() (used by AuthenticationServiceTests) is a mock IMapper with no
        // User -> AuthResponseDto/UserDto mapping configured: it would return null and this would only
        // exercise the generic catch-block, hiding the very bug/fix under test here.
        _mapper = new MapperConfiguration(cfg => cfg.AddMaps(typeof(UserMappingProfile).Assembly), NullLoggerFactory.Instance)
            .CreateMapper();
    }

    private static XpertSphereDbContext CreateContextIgnoringTransactionWarnings()
    {
        var options = new DbContextOptionsBuilder<XpertSphereDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var context = new XpertSphereDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private AuthenticationService CreateAuthenticationService(
        IExperienceService? experienceService = null,
        ITrainingService? trainingService = null,
        XpertSphereDbContext? context = null)
    {
        var jwtSettings = new JwtSettings
        {
            Key = "ThisIsATestSecretKeyThatIsLongEnoughForHS256Algorithm",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            AccessTokenExpirationMinutes = 60,
            RefreshTokenExpirationDays = 7
        };

        var entraIdSettings = new EntraIdSettings
        {
            TenantId = "test-tenant-id",
            ClientId = "test-client-id",
            ClientSecret = "test-client-secret"
        };

        var frontendOptions = MockHelper.CreateMockOptions(new FrontendSettings
        {
            CandidateAppBaseUrl = "http://localhost:3000"
        });

        return new AuthenticationService(
            _mockUserManager.Object,
            _mockSignInManager.Object,
            MockHelper.CreateMockOptions(jwtSettings),
            MockHelper.CreateMockOptions(entraIdSettings),
            _mapper,
            MockHelper.CreateMockLogger<AuthenticationService>().Object,
            new Mock<IValidator<RegisterDto>>().Object,
            CreatePassthroughValidator<LoginDto>(),
            CreatePassthroughValidator<RefreshTokenDto>(),
            new Mock<IValidator<ResetPasswordDto>>().Object,
            new Mock<IValidator<ConfirmEmailDto>>().Object,
            new Mock<IValidator<ForgotPasswordDto>>().Object,
            new Mock<IValidator<AdminResetPasswordDto>>().Object,
            new Mock<IValidator<ResendConfirmationDto>>().Object,
            _mockEnvironment.Object,
            new Mock<IHttpContextAccessor>().Object,
            new Mock<IUserService>().Object,
            new Mock<IResumeService>().Object,
            trainingService ?? new Mock<ITrainingService>().Object,
            experienceService ?? new Mock<IExperienceService>().Object,
            new Mock<IEmailNotificationService>().Object,
            frontendOptions,
            context ?? _context);
    }

    private static IValidator<T> CreatePassthroughValidator<T>()
    {
        var mock = new Mock<IValidator<T>>();
        mock.Setup(v => v.ValidateAsync(It.IsAny<T>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        return mock.Object;
    }

    private static User BuildCandidateWithProfile(string email, out Experience experience, out Training training)
    {
        var userId = Guid.NewGuid();

        experience = new Experience
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = "Software Engineer",
            Company = "Acme",
            Location = "Paris",
            Date = "2020-2022",
            Description = "Backend development"
        };

        training = new Training
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            School = "Sorbonne",
            Field = "Computer Science",
            Level = "Master",
            Period = "2018-2020"
        };

        return new User
        {
            Id = userId,
            Email = email,
            UserName = email,
            NormalizedEmail = email.ToUpperInvariant(),
            NormalizedUserName = email.ToUpperInvariant(),
            EmailConfirmed = true,
            IsActive = true,
            FirstName = "Jane",
            LastName = "Doe",
            Address = new Address { City = "Paris", Country = "France" },
            Experiences = [experience],
            Trainings = [training]
        };
    }

    private static User BuildCandidateWithoutProfile(string email)
    {
        var userId = Guid.NewGuid();
        return new User
        {
            Id = userId,
            Email = email,
            UserName = email,
            NormalizedEmail = email.ToUpperInvariant(),
            NormalizedUserName = email.ToUpperInvariant(),
            EmailConfirmed = true,
            IsActive = true,
            FirstName = "John",
            LastName = "Smith"
        };
    }

    // --- Acceptance criterion 1: POST /Auth/login returns Address/Experiences/Trainings ---

    [Fact]
    public async Task LoginAsync_ForCandidateWithProfile_ReturnsAddressExperiencesAndTrainings()
    {
        // Arrange
        const string email = "jane.doe@example.com";
        const string password = "Password1!";
        var user = BuildCandidateWithProfile(email, out var experience, out var training);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        // Detach the just-saved entity: otherwise the mocked UserManager.Users query below would
        // return the still-tracked `user` instance (whose Experiences/Trainings/Address are already
        // populated from construction) regardless of the .Include() chain under test, making these
        // assertions pass even without the fix. Clearing forces a real re-materialization from the
        // InMemory store, so the .Include(...) calls in LoginAsync/RefreshTokenAsync actually matter.
        _context.ChangeTracker.Clear();

        _mockUserManager.Setup(x => x.Users).Returns(_context.Users);
        _mockUserManager.Setup(x => x.UpdateAsync(It.IsAny<User>())).ReturnsAsync(IdentityResult.Success);
        _mockSignInManager
            .Setup(x => x.CheckPasswordSignInAsync(It.IsAny<User>(), password, true))
            .ReturnsAsync(SignInResult.Success);

        var authService = CreateAuthenticationService();

        // Act
        var result = await authService.LoginAsync(new LoginDto { Email = email, Password = password });

        // Assert
        result.IsSuccess.Should().BeTrue(because: result.Message + string.Join(",", result.Errors));
        result.Data!.User.Should().NotBeNull();
        result.Data!.User!.Address.Should().NotBeNull();
        result.Data!.User!.Address!.City.Should().Be("Paris");
        result.Data!.User!.Experiences.Should().NotBeNull();
        result.Data!.User!.Experiences!.Should().ContainSingle(e => e.Id == experience.Id && e.Title == "Software Engineer");
        result.Data!.User!.Trainings.Should().NotBeNull();
        result.Data!.User!.Trainings!.Should().ContainSingle(t => t.Id == training.Id && t.School == "Sorbonne");
    }

    [Fact]
    public async Task LoginAsync_ForCandidateWithoutProfile_ReturnsEmptyListsNotNull()
    {
        // Arrange - non-regression: acceptance criterion 4 (no experience/training -> [] not null/error)
        const string email = "john.smith@example.com";
        const string password = "Password1!";
        var user = BuildCandidateWithoutProfile(email);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        // Detach the just-saved entity: otherwise the mocked UserManager.Users query below would
        // return the still-tracked `user` instance (whose Experiences/Trainings/Address are already
        // populated from construction) regardless of the .Include() chain under test, making these
        // assertions pass even without the fix. Clearing forces a real re-materialization from the
        // InMemory store, so the .Include(...) calls in LoginAsync/RefreshTokenAsync actually matter.
        _context.ChangeTracker.Clear();

        _mockUserManager.Setup(x => x.Users).Returns(_context.Users);
        _mockUserManager.Setup(x => x.UpdateAsync(It.IsAny<User>())).ReturnsAsync(IdentityResult.Success);
        _mockSignInManager
            .Setup(x => x.CheckPasswordSignInAsync(It.IsAny<User>(), password, true))
            .ReturnsAsync(SignInResult.Success);

        var authService = CreateAuthenticationService();

        // Act
        var result = await authService.LoginAsync(new LoginDto { Email = email, Password = password });

        // Assert
        result.IsSuccess.Should().BeTrue(because: result.Message + string.Join(",", result.Errors));
        result.Data!.User!.Experiences.Should().NotBeNull().And.BeEmpty();
        result.Data!.User!.Trainings.Should().NotBeNull().And.BeEmpty();
    }

    // --- Acceptance criterion 5: POST /api/auth/refresh returns Address/Experiences/Trainings ---

    [Fact]
    public async Task RefreshTokenAsync_ForCandidateWithProfile_ReturnsAddressExperiencesAndTrainings()
    {
        // Arrange
        const string email = "jane.doe@example.com";
        var user = BuildCandidateWithProfile(email, out var experience, out var training);
        user.SetRefreshToken("valid-refresh-token", TimeSpan.FromDays(7));

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        // Detach the just-saved entity: otherwise the mocked UserManager.Users query below would
        // return the still-tracked `user` instance (whose Experiences/Trainings/Address are already
        // populated from construction) regardless of the .Include() chain under test, making these
        // assertions pass even without the fix. Clearing forces a real re-materialization from the
        // InMemory store, so the .Include(...) calls in LoginAsync/RefreshTokenAsync actually matter.
        _context.ChangeTracker.Clear();

        _mockUserManager.Setup(x => x.Users).Returns(_context.Users);
        _mockUserManager.Setup(x => x.UpdateAsync(It.IsAny<User>())).ReturnsAsync(IdentityResult.Success);

        var authService = CreateAuthenticationService();

        // Act
        var result = await authService.RefreshTokenAsync(new RefreshTokenDto
        {
            Email = email,
            RefreshToken = "valid-refresh-token"
        });

        // Assert
        result.IsSuccess.Should().BeTrue(because: result.Message + string.Join(",", result.Errors));
        result.Data!.User!.Address.Should().NotBeNull();
        result.Data!.User!.Address!.City.Should().Be("Paris");
        result.Data!.User!.Experiences.Should().NotBeNull();
        result.Data!.User!.Experiences!.Should().ContainSingle(e => e.Id == experience.Id);
        result.Data!.User!.Trainings.Should().NotBeNull();
        result.Data!.User!.Trainings!.Should().ContainSingle(t => t.Id == training.Id);
    }

    // --- Empirical verification requested by the spec: does RegisterCandidateAsync need the same fix? ---

    [Fact]
    public async Task RegisterCandidateAsync_WithExperienceAndTraining_ReturnsPopulatedCollections()
    {
        // This test exists to answer empirically (per the spec) whether EF Core's relationship fixup
        // populates `user.Experiences`/`user.Trainings` in memory after RegisterCandidateAsync's separate
        // calls to ExperienceService.CreateExperienceAsync/TrainingService.CreateTrainingAsync on the same
        // DbContext-tracked `user`, WITHOUT any explicit .Include() (there is nothing to Include: the user
        // was just created in this same call, before any SaveChanges/reload).
        //
        // To be faithful to production behaviour, UserManager.CreateAsync/UpdateAsync are mocked but their
        // callbacks perform the same Add+SaveChanges a real EF Core Identity UserStore would perform against
        // this DbContext, so `user` becomes genuinely tracked - and the real ExperienceService/TrainingService
        // (not mocks) are used, writing Experience/Training rows to that same tracked DbContext.

        // Arrange
        const string email = "new.candidate@example.com";
        var mapper = new MapperConfiguration(cfg => cfg.AddMaps(typeof(UserMappingProfile).Assembly), NullLoggerFactory.Instance)
            .CreateMapper();

        // RegisterCandidateAsync wraps its work in a Database.BeginTransactionAsync(). The EF Core InMemory
        // provider does not support transactions and turns that into an error-level warning by default; it
        // is otherwise a harmless no-op for this provider. Use a dedicated context (not TestDbContextFactory,
        // which other tests in this suite/other files rely on staying untouched) that just ignores that one
        // warning, so the transaction plumbing already present in production code doesn't get in the way of
        // observing the actual behaviour under test (EF Core relationship fixup).
        using var registerContext = CreateContextIgnoringTransactionWarnings();

        var experienceService = new ExperienceService(
            registerContext,
            mapper,
            MockHelper.CreateMockLogger<ExperienceService>().Object);

        var trainingService = new TrainingService(
            registerContext,
            mapper,
            MockHelper.CreateMockLogger<TrainingService>().Object);

        _mockUserManager.Setup(x => x.FindByEmailAsync(email)).ReturnsAsync((User?)null);
        _mockUserManager
            .Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .Callback<User, string>((user, _) =>
            {
                // Mirrors what the real EF Core Identity UserStore does on CreateAsync: track + persist.
                registerContext.Users.Add(user);
                registerContext.SaveChanges();
            })
            .ReturnsAsync(IdentityResult.Success);
        _mockUserManager
            .Setup(x => x.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Success);
        _mockUserManager
            .Setup(x => x.GenerateEmailConfirmationTokenAsync(It.IsAny<User>()))
            .ReturnsAsync("test-token");

        var authService = CreateAuthenticationService(experienceService, trainingService, registerContext);

        var registerDto = new RegisterCandidateDto
        {
            Email = email,
            Password = "Password1!",
            ConfirmPassword = "Password1!",
            FirstName = "New",
            LastName = "Candidate",
            AcceptTerms = true,
            AcceptPrivacyPolicy = true,
            Experiences =
            [
                new CreateExperienceDto
                {
                    Title = "Backend Developer",
                    Company = "Acme",
                    Location = "Lyon",
                    Date = "2021-2023",
                    Description = "Backend development"
                }
            ],
            Trainings =
            [
                new CreateTrainingDto
                {
                    School = "Sorbonne",
                    Field = "Computer Science",
                    Level = "Master",
                    Period = "2018-2020"
                }
            ]
        };

        // Act
        var result = await authService.RegisterCandidateAsync(registerDto);

        // Assert
        result.IsSuccess.Should().BeTrue(because: result.Message + string.Join(",", result.Errors));

        // *** This is the empirical check requested by the spec. Whatever it shows must be documented in
        // the commit/PR description: if it fails (collections empty), RegisterCandidateAsync needs the same
        // defensive reload as LoginAsync/RefreshTokenAsync; if it passes, no code change is needed there. ***
        result.Data!.User!.Experiences.Should().NotBeNull();
        result.Data!.User!.Experiences!.Should().ContainSingle(e => e.Title == "Backend Developer");
        result.Data!.User!.Trainings.Should().NotBeNull();
        result.Data!.User!.Trainings!.Should().ContainSingle(t => t.School == "Sorbonne");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
