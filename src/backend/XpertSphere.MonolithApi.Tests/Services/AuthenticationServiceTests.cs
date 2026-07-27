using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using XpertSphere.MonolithApi.DTOs.Auth;
using XpertSphere.MonolithApi.DTOs.ExperienceDtos;
using XpertSphere.MonolithApi.Enums;
using XpertSphere.MonolithApi.Interfaces;
using XpertSphere.MonolithApi.Models;
using XpertSphere.MonolithApi.Services;
using XpertSphere.MonolithApi.Tests.Helpers;
using XpertSphere.MonolithApi.Utils.Results;
using XpertSphere.MonolithApi.Data;
using XpertSphere.MonolithApi.Models.Base;
using XpertSphere.MonolithApi.Extensions;
using XpertSphere.MonolithApi.Config;

namespace XpertSphere.MonolithApi.Tests.Services;

public class AuthenticationServiceTests : IDisposable
{
    private readonly Mock<UserManager<User>> _mockUserManager;
    private readonly Mock<SignInManager<User>> _mockSignInManager;
    private readonly Mock<ILogger<AuthenticationService>> _mockLogger;
    private readonly Mock<IValidator<RegisterDto>> _mockRegisterValidator;
    private readonly Mock<IValidator<LoginDto>> _mockLoginValidator;
    private readonly Mock<IValidator<RefreshTokenDto>> _mockRefreshTokenValidator;
    private readonly Mock<IValidator<ResetPasswordDto>> _mockResetPasswordValidator;
    private readonly Mock<IValidator<ConfirmEmailDto>> _mockConfirmEmailValidator;
    private readonly Mock<IValidator<ForgotPasswordDto>> _mockForgotPasswordValidator;
    private readonly Mock<IValidator<AdminResetPasswordDto>> _mockAdminResetPasswordValidator;
    private readonly Mock<IValidator<ResendConfirmationDto>> _mockResendConfirmationValidator;
    private readonly Mock<IWebHostEnvironment> _mockEnvironment;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<IExperienceService> _mockExperienceService;
    private readonly Mock<ITrainingService> _mockTrainingService;
    private readonly Mock<IResumeService> _mockResumeService;
    private readonly Mock<IEmailNotificationService> _mockEmailNotificationService;
    private readonly XpertSphereDbContext _context;

    public AuthenticationServiceTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _mockUserManager = MockHelper.CreateMockUserManager();
        _mockSignInManager = MockHelper.CreateMockSignInManager(_mockUserManager);
        _mockLogger = MockHelper.CreateMockLogger<AuthenticationService>();
        _mockRegisterValidator = new Mock<IValidator<RegisterDto>>();
        _mockLoginValidator = new Mock<IValidator<LoginDto>>();
        _mockRefreshTokenValidator = new Mock<IValidator<RefreshTokenDto>>();
        _mockResetPasswordValidator = new Mock<IValidator<ResetPasswordDto>>();
        _mockConfirmEmailValidator = new Mock<IValidator<ConfirmEmailDto>>();
        _mockForgotPasswordValidator = new Mock<IValidator<ForgotPasswordDto>>();
        _mockAdminResetPasswordValidator = new Mock<IValidator<AdminResetPasswordDto>>();
        _mockResendConfirmationValidator = new Mock<IValidator<ResendConfirmationDto>>();
        _mockEnvironment = new Mock<IWebHostEnvironment>();
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _mockExperienceService = new Mock<IExperienceService>();
        _mockTrainingService = new Mock<ITrainingService>();
        _mockUserService = new Mock<IUserService>();
        _mockResumeService = new Mock<IResumeService>();
        _mockEmailNotificationService = new Mock<IEmailNotificationService>();

        // Setup environment to be Development (to avoid Entra ID logic)
        _mockEnvironment.Setup(x => x.EnvironmentName).Returns("Development");
    }

    [Fact]
    public async Task RegisterAsync_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Email = "test@example.com",
            Password = "Test123!",
            ConfirmPassword = "Test123!",
            FirstName = "Test",
            LastName = "User",
            Trainings = new List<Training>(),
            Experiences = new List<Experience>()
        };

        _mockRegisterValidator.Setup(x => x.ValidateAsync(registerDto, default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _mockUserManager.Setup(x => x.FindByEmailAsync(registerDto.Email))
            .ReturnsAsync((User?)null);

        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<User>(), registerDto.Password))
            .ReturnsAsync(IdentityResult.Success);

        _mockUserManager.Setup(x => x.GenerateEmailConfirmationTokenAsync(It.IsAny<User>()))
            .ReturnsAsync("test-token");

        // Create minimal service without complex dependencies
        var mapper = AutoMapperHelper.CreateMapper();
        var authService = CreateAuthenticationService(mapper);

        // Act
        var result = await authService.RegisterAsync(registerDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Message.Should().Contain("Inscription réussie");
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ShouldReturnFailure()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Email = "existing@example.com",
            Password = "Test123!",
            ConfirmPassword = "Test123!",
            FirstName = "Test",
            LastName = "User",
            Trainings = new List<Training>(),
            Experiences = new List<Experience>()
        };

        var existingUser = new User
        {
            Email = registerDto.Email,
            FirstName = "Existing",
            LastName = "User"
        };

        _mockRegisterValidator.Setup(x => x.ValidateAsync(registerDto, default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _mockUserManager.Setup(x => x.FindByEmailAsync(registerDto.Email))
            .ReturnsAsync(existingUser);

        var mapper = AutoMapperHelper.CreateMapper();
        var authService = CreateAuthenticationService(mapper);

        // Act
        var result = await authService.RegisterAsync(registerDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Un utilisateur avec cet email existe déjà");
    }

    [Fact]
    public async Task RegisterAsync_WithInvalidData_ShouldReturnValidationError()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Email = "invalid-email", // Invalid email
            Password = "123", // Weak password
            ConfirmPassword = "456", // Different from password
            FirstName = "",
            LastName = "",
            Trainings = [],
            Experiences = []
        };

        var validationErrors = new FluentValidation.Results.ValidationResult(
        [
            new FluentValidation.Results.ValidationFailure("Email", "Invalid email format"),
            new FluentValidation.Results.ValidationFailure("Password", "Password too weak")
        ]);

        _mockRegisterValidator.Setup(x => x.ValidateAsync(registerDto, CancellationToken.None))
            .ReturnsAsync(validationErrors);

        var mapper = AutoMapperHelper.CreateMapper();
        var authService = CreateAuthenticationService(mapper);

        // Act
        var result = await authService.RegisterAsync(registerDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Invalid email format");
    }


    [Fact]
    public async Task LoginAsync_WithInvalidCredentials_ShouldReturnFailure()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "test@example.com",
            Password = "WrongPassword"
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = loginDto.Email,
            EmailConfirmed = true,
            FirstName = "Test",
            LastName = "User"
        };

        _mockLoginValidator.Setup(x => x.ValidateAsync(loginDto, default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _mockUserManager.Setup(x => x.FindByEmailAsync(loginDto.Email))
            .ReturnsAsync(user);

        _mockSignInManager.Setup(x => x.CheckPasswordSignInAsync(user, loginDto.Password, false))
            .ReturnsAsync(SignInResult.Failed);

        var mapper = AutoMapperHelper.CreateMapper();
        var authService = CreateAuthenticationService(mapper);

        // Act
        var result = await authService.LoginAsync(loginDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Une erreur est survenue lors de la connexion");
    }

    [Fact]
    public async Task LoginAsync_WithUnconfirmedEmail_ShouldReturnFailure()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "test@example.com",
            Password = "Test123!"
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = loginDto.Email,
            Email = loginDto.Email,
            EmailConfirmed = false,
            FirstName = "Test",
            LastName = "User"
        };

        _mockLoginValidator.Setup(x => x.ValidateAsync(loginDto, default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        // LoginAsync loads the user via `_userManager.Users.Include(...).FirstOrDefaultAsync(...)`,
        // not `FindByEmailAsync` - mocking FindByEmailAsync alone (as a previous version of this
        // test did) leaves `Users` returning Moq's default LINQ-to-Objects queryable, whose
        // provider is not IAsyncQueryProvider: FirstOrDefaultAsync throws, and that exception -
        // not the intended IsNotAllowed branch - is what the generic catch below actually turns
        // into the (coincidentally matching) generic error message. Backing `Users` with a real EF
        // Core InMemory DbSet (same technique as AuthenticationServiceProfileCompletenessTests)
        // makes the async LINQ chain actually work.
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        _mockUserManager.Setup(x => x.Users).Returns(_context.Users);

        // Exercise the real SignInResult.NotAllowed branch (activation stricte,
        // RequireConfirmedEmail = true) rather than relying on Moq's default `null` return for an
        // unconfigured setup - a previous version of this test omitted this Setup entirely, which
        // made CheckPasswordSignInAsync return null, throw a NullReferenceException caught by the
        // generic try/catch, and pass "by coincidence" on the generic error message instead of
        // actually covering the unconfirmed-email path (see candidate-account-activation-email.md,
        // Constat point 9).
        _mockSignInManager
            .Setup(x => x.CheckPasswordSignInAsync(It.IsAny<User>(), loginDto.Password, true))
            .ReturnsAsync(SignInResult.NotAllowed);

        var mapper = AutoMapperHelper.CreateMapper();
        // AutoMapperHelper.CreateMapper() has no User -> AuthResponseDto mapping configured
        // (see AuthenticationServiceProfileCompletenessTests' doc comment) - add it explicitly for
        // this test so LoginAsync's IsNotAllowed branch can map RequiresEmailConfirmation.
        Mock.Get(mapper)
            .Setup(m => m.Map<AuthResponseDto>(It.IsAny<User>()))
            .Returns((User u) => new AuthResponseDto { RequiresEmailConfirmation = !u.EmailConfirmed });

        var authService = CreateAuthenticationService(mapper);

        // Act
        var result = await authService.LoginAsync(loginDto);

        // Assert
        result.IsSuccess.Should().BeFalse(because: result.Message + string.Join(",", result.Errors));
        result.Errors.Should().Contain(
            "Votre compte n'est pas encore activé. Veuillez consulter l'email de confirmation envoyé lors de votre inscription, ou demandez un nouvel envoi.");
        result.Data.Should().NotBeNull();
        result.Data!.RequiresEmailConfirmation.Should().BeTrue();
    }

    [Fact]
    public async Task ConfirmEmailAsync_WithValidToken_ShouldReturnSuccess()
    {
        // Arrange
        var confirmEmailDto = new ConfirmEmailDto
        {
            Email = "test@gmail.com",
            Token = "valid-token"
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            EmailConfirmed = false,
            FirstName = "Test",
            LastName = "User"
        };

        _mockConfirmEmailValidator.Setup(x => x.ValidateAsync(confirmEmailDto, default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _mockUserManager.Setup(x => x.FindByEmailAsync(confirmEmailDto.Email))
            .ReturnsAsync(user);

        _mockUserManager.Setup(x => x.ConfirmEmailAsync(user, confirmEmailDto.Token))
            .ReturnsAsync(IdentityResult.Success);

        var mapper = AutoMapperHelper.CreateMapper();
        var authService = CreateAuthenticationService(mapper);

        // Act
        var result = await authService.ConfirmEmailAsync(confirmEmailDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Message.Should().Contain("Email confirmé avec succès");
    }

    [Fact]
    public async Task ConfirmEmailAsync_WithInvalidToken_ShouldReturnFailure()
    {
        // Arrange
        var confirmEmailDto = new ConfirmEmailDto
        {
            Email = "test@gmail.com",
            Token = "valid-token"
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            EmailConfirmed = false,
            FirstName = "Test",
            LastName = "User"
        };

        _mockConfirmEmailValidator.Setup(x => x.ValidateAsync(confirmEmailDto, default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _mockUserManager.Setup(x => x.FindByEmailAsync(confirmEmailDto.Email))
            .ReturnsAsync(user);

        var identityErrors = new List<IdentityError>
        {
            new() { Code = "InvalidToken", Description = "Invalid token" }
        };

        _mockUserManager.Setup(x => x.ConfirmEmailAsync(user, confirmEmailDto.Token))
            .ReturnsAsync(IdentityResult.Failed(identityErrors.ToArray()));

        var mapper = AutoMapperHelper.CreateMapper();
        var authService = CreateAuthenticationService(mapper);

        // Act
        var result = await authService.ConfirmEmailAsync(confirmEmailDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Échec de la confirmation de l'email : Invalid token");
    }

    [Fact]
    public async Task ForgotPasswordAsync_WithValidEmail_ShouldReturnSuccess()
    {
        // Arrange
        var forgotPasswordDto = new ForgotPasswordDto
        {
            Email = "test@example.com"
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = forgotPasswordDto.Email,
            EmailConfirmed = true,
            FirstName = "Test",
            LastName = "User"
        };

        _mockForgotPasswordValidator.Setup(x => x.ValidateAsync(forgotPasswordDto, default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _mockUserManager.Setup(x => x.FindByEmailAsync(forgotPasswordDto.Email))
            .ReturnsAsync(user);

        _mockUserManager.Setup(x => x.GeneratePasswordResetTokenAsync(user))
            .ReturnsAsync("reset-token");

        var mapper = AutoMapperHelper.CreateMapper();
        var authService = CreateAuthenticationService(mapper);

        // Act
        var result = await authService.ForgotPasswordAsync(forgotPasswordDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Message.Should().Contain("réinitialisation");
    }

    private const string ResendConfirmationGenericMessage =
        "Si un compte existe pour cet email et n'est pas encore confirmé, un nouvel email d'activation vient d'être envoyé.";

    [Fact]
    public async Task ResendConfirmationEmailAsync_WithUnconfirmedAccount_ShouldSendEmailAndReturnGenericMessage()
    {
        // Arrange
        var dto = new ResendConfirmationDto { Email = $"unconfirmed-{Guid.NewGuid()}@example.com" };
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = dto.Email,
            EmailConfirmed = false,
            FirstName = "Test",
            LastName = "User"
        };

        _mockResendConfirmationValidator.Setup(x => x.ValidateAsync(dto, default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());
        _mockUserManager.Setup(x => x.FindByEmailAsync(dto.Email)).ReturnsAsync(user);
        _mockUserManager.Setup(x => x.GenerateEmailConfirmationTokenAsync(user)).ReturnsAsync("some-token");
        _mockEmailNotificationService
            .Setup(x => x.SendAccountActivationEmailAsync(dto.Email, It.IsAny<string>()))
            .ReturnsAsync(true);

        var mapper = AutoMapperHelper.CreateMapper();
        var authService = CreateAuthenticationService(mapper);

        // Act
        var result = await authService.ResendConfirmationEmailAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Message.Should().Be(ResendConfirmationGenericMessage);
        _mockEmailNotificationService.Verify(
            x => x.SendAccountActivationEmailAsync(dto.Email, It.IsAny<string>()), Times.Once);
    }

    [Theory]
    [InlineData(false)] // compte inexistant
    [InlineData(true)] // compte déjà confirmé
    public async Task ResendConfirmationEmailAsync_EnumerationSafety_ShouldNeverSendEmail(bool accountExistsAndConfirmed)
    {
        // Arrange
        var dto = new ResendConfirmationDto { Email = "irrelevant@example.com" };

        _mockResendConfirmationValidator.Setup(x => x.ValidateAsync(dto, default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        User? user = accountExistsAndConfirmed
            ? new User { Id = Guid.NewGuid(), Email = dto.Email, EmailConfirmed = true, FirstName = "T", LastName = "U" }
            : null;
        _mockUserManager.Setup(x => x.FindByEmailAsync(dto.Email)).ReturnsAsync(user);

        var mapper = AutoMapperHelper.CreateMapper();
        var authService = CreateAuthenticationService(mapper);

        // Act
        var result = await authService.ResendConfirmationEmailAsync(dto);

        // Assert - same generic message regardless of the real underlying reason (enumeration-safety)
        result.IsSuccess.Should().BeTrue();
        result.Message.Should().Be(ResendConfirmationGenericMessage);
        _mockEmailNotificationService.Verify(
            x => x.SendAccountActivationEmailAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ResendConfirmationEmailAsync_WithInvalidEmail_ShouldReturnValidationError()
    {
        // Arrange
        var dto = new ResendConfirmationDto { Email = "not-an-email" };
        var validationErrors = new FluentValidation.Results.ValidationResult(
        [
            new FluentValidation.Results.ValidationFailure("Email", "Format d'email invalide")
        ]);
        _mockResendConfirmationValidator.Setup(x => x.ValidateAsync(dto, default))
            .ReturnsAsync(validationErrors);

        var mapper = AutoMapperHelper.CreateMapper();
        var authService = CreateAuthenticationService(mapper);

        // Act
        var result = await authService.ResendConfirmationEmailAsync(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Format d'email invalide");
    }

    [Fact]
    public async Task ResetPasswordAsync_WithValidToken_ShouldReturnSuccess()
    {
        // Arrange
        var resetPasswordDto = new ResetPasswordDto
        {
            Email = "test@example.com",
            Token = "valid-reset-token",
            NewPassword = "NewPassword123!",
            ConfirmPassword = "NewPassword123!"
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = resetPasswordDto.Email,
            FirstName = "Test",
            LastName = "User"
        };

        _mockResetPasswordValidator.Setup(x => x.ValidateAsync(resetPasswordDto, default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _mockUserManager.Setup(x => x.FindByEmailAsync(resetPasswordDto.Email))
            .ReturnsAsync(user);

        _mockUserManager.Setup(x => x.ResetPasswordAsync(user, resetPasswordDto.Token, resetPasswordDto.NewPassword))
            .ReturnsAsync(IdentityResult.Success);

        var mapper = AutoMapperHelper.CreateMapper();
        var authService = CreateAuthenticationService(mapper);

        // Act
        var result = await authService.ResetPasswordAsync(resetPasswordDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Message.Should().Contain("Réinitialisation du mot de passe réussie");
    }

    [Fact]
    public async Task RegisterCandidateAsync_WithExperienceMissingDescription_ShouldReturnValidationError()
    {
        // Arrange
        var registerDto = CreateValidRegisterCandidateDto(
        [
            new CreateExperienceDto
            {
                Title = "Backend Developer",
                Company = "Acme",
                Location = "Paris",
                Date = "2020-2022",
                Description = ""
            }
        ]);

        var mapper = AutoMapperHelper.CreateMapper();
        var authService = CreateAuthenticationService(mapper);

        // Act
        var result = await authService.RegisterCandidateAsync(registerDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(422);
        result.Errors.Should().Contain("L'expérience n°1 (« Backend Developer ») n'a pas de description.");
    }

    [Fact]
    public async Task RegisterCandidateAsync_WithExperienceWhitespaceDescription_ShouldReturnValidationError()
    {
        // Arrange
        var registerDto = CreateValidRegisterCandidateDto(
        [
            new CreateExperienceDto
            {
                Title = "Backend Developer",
                Company = "Acme",
                Location = "Paris",
                Date = "2020-2022",
                Description = "   "
            }
        ]);

        var mapper = AutoMapperHelper.CreateMapper();
        var authService = CreateAuthenticationService(mapper);

        // Act
        var result = await authService.RegisterCandidateAsync(registerDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(422);
        result.Errors.Should().Contain("L'expérience n°1 (« Backend Developer ») n'a pas de description.");
    }

    [Fact]
    public async Task RegisterCandidateAsync_WithExperienceMissingTitleAndDescription_ShouldReturnValidationErrorWithoutTitle()
    {
        // Arrange
        var registerDto = CreateValidRegisterCandidateDto(
        [
            new CreateExperienceDto
            {
                Title = "",
                Company = "Acme",
                Location = "Paris",
                Date = "2020-2022",
                Description = ""
            }
        ]);

        var mapper = AutoMapperHelper.CreateMapper();
        var authService = CreateAuthenticationService(mapper);

        // Act
        var result = await authService.RegisterCandidateAsync(registerDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(422);
        result.Errors.Should().Contain("L'expérience n°1 n'a pas de description.");
    }

    [Fact]
    public async Task RegisterCandidateAsync_WithTwoExperiencesMissingDescription_ShouldReturnBothErrors()
    {
        // Arrange
        var registerDto = CreateValidRegisterCandidateDto(
        [
            new CreateExperienceDto
            {
                Title = "Backend Developer",
                Company = "Acme",
                Location = "Paris",
                Date = "2020-2022",
                Description = ""
            },
            new CreateExperienceDto
            {
                Title = "Frontend Developer",
                Company = "Beta",
                Location = "Lyon",
                Date = "2018-2020",
                Description = "   "
            }
        ]);

        var mapper = AutoMapperHelper.CreateMapper();
        var authService = CreateAuthenticationService(mapper);

        // Act
        var result = await authService.RegisterCandidateAsync(registerDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(422);
        result.Errors.Should().Contain("L'expérience n°1 (« Backend Developer ») n'a pas de description.");
        result.Errors.Should().Contain("L'expérience n°2 (« Frontend Developer ») n'a pas de description.");
    }

    [Fact]
    public async Task RegisterCandidateAsync_WithOneValidAndOneInvalidExperience_ShouldReturnOnlyInvalidOneError()
    {
        // Arrange
        var registerDto = CreateValidRegisterCandidateDto(
        [
            new CreateExperienceDto
            {
                Title = "Backend Developer",
                Company = "Acme",
                Location = "Paris",
                Date = "2020-2022",
                Description = "Built and maintained backend services."
            },
            new CreateExperienceDto
            {
                Title = "Frontend Developer",
                Company = "Beta",
                Location = "Lyon",
                Date = "2018-2020",
                Description = ""
            }
        ]);

        var mapper = AutoMapperHelper.CreateMapper();
        var authService = CreateAuthenticationService(mapper);

        // Act
        var result = await authService.RegisterCandidateAsync(registerDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(422);
        result.Errors.Should().ContainSingle();
        result.Errors.Should().Contain("L'expérience n°2 (« Frontend Developer ») n'a pas de description.");
    }

    [Fact]
    public async Task RegisterCandidateAsync_WithNoExperiences_ShouldReturnSuccess()
    {
        // Arrange - empty experiences list must remain a non-regression: the step stays optional overall
        var registerDto = CreateValidRegisterCandidateDto();

        _mockUserManager.Setup(x => x.FindByEmailAsync(registerDto.Email))
            .ReturnsAsync((User?)null);
        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<User>(), registerDto.Password))
            .ReturnsAsync(IdentityResult.Success);
        _mockUserManager.Setup(x => x.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Success);
        _mockUserManager.Setup(x => x.GenerateEmailConfirmationTokenAsync(It.IsAny<User>()))
            .ReturnsAsync("test-token");

        var mapper = AutoMapperHelper.CreateMapper();
        var authService = CreateAuthenticationService(mapper);

        // Act
        var result = await authService.RegisterCandidateAsync(registerDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Message.Should().Contain("Inscription réussie");
    }

    [Fact]
    public async Task RegisterCandidateAsync_WithDesiredSalaryAndNoCurrency_ShouldFallBackToXof()
    {
        // Arrange - covers configurable-salary-currency.md: a direct API call (Swagger, Postman, or
        // a stale client) omitting DesiredSalaryCurrency while still sending a DesiredSalary must not
        // break account creation; it falls back to Currency.XOF, consistent with the backfill applied
        // to already-existing candidate accounts.
        var registerDto = CreateValidRegisterCandidateDto();
        registerDto = registerDto with { DesiredSalary = 35000m, DesiredSalaryCurrency = null };

        User? capturedUser = null;
        _mockUserManager.Setup(x => x.FindByEmailAsync(registerDto.Email))
            .ReturnsAsync((User?)null);
        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<User>(), registerDto.Password))
            .Callback<User, string>((user, _) => capturedUser = user)
            .ReturnsAsync(IdentityResult.Success);
        _mockUserManager.Setup(x => x.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Success);
        _mockUserManager.Setup(x => x.GenerateEmailConfirmationTokenAsync(It.IsAny<User>()))
            .ReturnsAsync("test-token");

        var mapper = AutoMapperHelper.CreateMapper();
        var authService = CreateAuthenticationService(mapper);

        // Act
        var result = await authService.RegisterCandidateAsync(registerDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedUser.Should().NotBeNull();
        capturedUser!.DesiredSalaryCurrency.Should().Be(Currency.XOF);
    }

    [Fact]
    public async Task RegisterCandidateAsync_WithDesiredSalaryCurrencyProvided_ShouldUseProvidedValue()
    {
        // Arrange - the registration form always sends a pre-selected currency; it must be honored
        // rather than overridden by the XOF fallback.
        var registerDto = CreateValidRegisterCandidateDto();
        registerDto = registerDto with { DesiredSalary = 35000m, DesiredSalaryCurrency = Currency.EUR };

        User? capturedUser = null;
        _mockUserManager.Setup(x => x.FindByEmailAsync(registerDto.Email))
            .ReturnsAsync((User?)null);
        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<User>(), registerDto.Password))
            .Callback<User, string>((user, _) => capturedUser = user)
            .ReturnsAsync(IdentityResult.Success);
        _mockUserManager.Setup(x => x.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Success);
        _mockUserManager.Setup(x => x.GenerateEmailConfirmationTokenAsync(It.IsAny<User>()))
            .ReturnsAsync("test-token");

        var mapper = AutoMapperHelper.CreateMapper();
        var authService = CreateAuthenticationService(mapper);

        // Act
        var result = await authService.RegisterCandidateAsync(registerDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedUser.Should().NotBeNull();
        capturedUser!.DesiredSalaryCurrency.Should().Be(Currency.EUR);
    }

    [Fact]
    public async Task RegisterCandidateAsync_WithoutDesiredSalary_ShouldLeaveCurrencyNull()
    {
        // Arrange - no salary at all: the currency fallback must not invent a value out of nowhere.
        var registerDto = CreateValidRegisterCandidateDto();
        registerDto = registerDto with { DesiredSalary = null, DesiredSalaryCurrency = null };

        User? capturedUser = null;
        _mockUserManager.Setup(x => x.FindByEmailAsync(registerDto.Email))
            .ReturnsAsync((User?)null);
        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<User>(), registerDto.Password))
            .Callback<User, string>((user, _) => capturedUser = user)
            .ReturnsAsync(IdentityResult.Success);
        _mockUserManager.Setup(x => x.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Success);
        _mockUserManager.Setup(x => x.GenerateEmailConfirmationTokenAsync(It.IsAny<User>()))
            .ReturnsAsync("test-token");

        var mapper = AutoMapperHelper.CreateMapper();
        var authService = CreateAuthenticationService(mapper);

        // Act
        var result = await authService.RegisterCandidateAsync(registerDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedUser.Should().NotBeNull();
        capturedUser!.DesiredSalaryCurrency.Should().BeNull();
    }

    private static RegisterCandidateDto CreateValidRegisterCandidateDto(List<CreateExperienceDto>? experiences = null)
    {
        return new RegisterCandidateDto
        {
            Email = "candidate@example.com",
            Password = "Test123!",
            ConfirmPassword = "Test123!",
            FirstName = "Test",
            LastName = "Candidate",
            AcceptTerms = true,
            AcceptPrivacyPolicy = true,
            Experiences = experiences
        };
    }

    private AuthenticationService CreateAuthenticationService(AutoMapper.IMapper mapper)
    {
        // Create real config objects
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

        var jwtOptions = MockHelper.CreateMockOptions(jwtSettings);
        var entraIdOptions = MockHelper.CreateMockOptions(entraIdSettings);
        var frontendOptions = MockHelper.CreateMockOptions(new FrontendSettings
        {
            CandidateAppBaseUrl = "http://localhost:3000"
        });

        return new AuthenticationService(
            _mockUserManager.Object,
            _mockSignInManager.Object,
            jwtOptions,
            entraIdOptions,
            mapper,
            _mockLogger.Object,
            _mockRegisterValidator.Object,
            _mockLoginValidator.Object,
            _mockRefreshTokenValidator.Object,
            _mockResetPasswordValidator.Object,
            _mockConfirmEmailValidator.Object,
            _mockForgotPasswordValidator.Object,
            _mockAdminResetPasswordValidator.Object,
            _mockResendConfirmationValidator.Object,
            _mockEnvironment.Object,
            _mockHttpContextAccessor.Object,
            _mockUserService.Object,
            _mockResumeService.Object,
            _mockTrainingService.Object,
            _mockExperienceService.Object,
            _mockEmailNotificationService.Object,
            frontendOptions,
            _context
        );
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}