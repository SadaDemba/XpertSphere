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
    private readonly Mock<IWebHostEnvironment> _mockEnvironment;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<IResumeService> _mockResumeService;
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
        _mockEnvironment = new Mock<IWebHostEnvironment>();
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _mockUserService = new Mock<IUserService>();
        _mockResumeService = new Mock<IResumeService>();

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
        result.Data.Should().NotBeNull();
        result.Data!.Message.Should().Contain("Registration successful");
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
        result.Errors.Should().Contain("User with this email already exists");
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
            Trainings =[],
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
        result.Errors.Should().Contain("An error occurred during login");
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
            Email = loginDto.Email,
            EmailConfirmed = false,
            FirstName = "Test",
            LastName = "User"
        };

        _mockLoginValidator.Setup(x => x.ValidateAsync(loginDto, default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _mockUserManager.Setup(x => x.FindByEmailAsync(loginDto.Email))
            .ReturnsAsync(user);

        var mapper = AutoMapperHelper.CreateMapper();
        var authService = CreateAuthenticationService(mapper);

        // Act
        var result = await authService.LoginAsync(loginDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("An error occurred during login");
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
        result.Data!.Message.Should().Contain("Email confirmed successfully");
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
        result.Errors.Should().Contain("Email confirmation failed: Invalid token");
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
        result.Data.Should().NotBeNull();
        result.Data!.Message.Should().Contain("Password reset");
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
        result.Data!.Message.Should().Contain("Password reset successful");
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
            _mockEnvironment.Object,
            _mockHttpContextAccessor.Object,
            _mockUserService.Object,
            _mockResumeService.Object,
            _context
        );
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}