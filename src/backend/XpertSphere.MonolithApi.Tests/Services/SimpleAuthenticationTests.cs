using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using XpertSphere.MonolithApi.DTOs.Auth;
using XpertSphere.MonolithApi.Models;
using XpertSphere.MonolithApi.Tests.Helpers;
using XpertSphere.MonolithApi.Models.Base;

namespace XpertSphere.MonolithApi.Tests.Services;

public class SimpleAuthenticationTests
{
    private readonly Mock<UserManager<User>> _mockUserManager;
    private readonly Mock<SignInManager<User>> _mockSignInManager;
    private readonly Mock<ILogger<object>> _mockLogger;

    public SimpleAuthenticationTests()
    {
        _mockUserManager = MockHelper.CreateMockUserManager();
        _mockSignInManager = MockHelper.CreateMockSignInManager(_mockUserManager);
        _mockLogger = MockHelper.CreateMockLogger<object>();
    }

    [Fact]
    public void UserManager_ShouldBeConfiguredCorrectly()
    {
        // Arrange & Act
        var userManager = _mockUserManager.Object;

        // Assert
        userManager.Should().NotBeNull();
    }

    [Fact]
    public void SignInManager_ShouldBeConfiguredCorrectly()
    {
        // Arrange & Act
        var signInManager = _mockSignInManager.Object;

        // Assert
        signInManager.Should().NotBeNull();
    }

    [Fact]
    public async Task UserManager_FindByEmailAsync_ShouldReturnUser()
    {
        // Arrange
        var testEmail = "test@example.com";
        var testUser = new User
        {
            Id = Guid.NewGuid(),
            Email = testEmail,
            UserName = testEmail,
            FirstName = "Test",
            LastName = "User"
        };

        _mockUserManager.Setup(x => x.FindByEmailAsync(testEmail))
            .ReturnsAsync(testUser);

        // Act
        var result = await _mockUserManager.Object.FindByEmailAsync(testEmail);

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be(testEmail);
        result.FirstName.Should().Be("Test");
    }

    [Fact]
    public async Task UserManager_CreateAsync_ShouldReturnSuccess()
    {
        // Arrange
        var testUser = new User
        {
            Email = "new@example.com",
            UserName = "new@example.com",
            FirstName = "New",
            LastName = "User"
        };

        _mockUserManager.Setup(x => x.CreateAsync(testUser, It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _mockUserManager.Object.CreateAsync(testUser, "Password123!");

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task SignInManager_CheckPasswordSignInAsync_ShouldReturnSuccess()
    {
        // Arrange
        var testUser = new User
        {
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User"
        };

        _mockSignInManager.Setup(x => x.CheckPasswordSignInAsync(testUser, "CorrectPassword", false))
            .ReturnsAsync(SignInResult.Success);

        // Act
        var result = await _mockSignInManager.Object.CheckPasswordSignInAsync(testUser, "CorrectPassword", false);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task SignInManager_CheckPasswordSignInAsync_WithWrongPassword_ShouldReturnFailure()
    {
        // Arrange
        var testUser = new User
        {
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User"
        };

        _mockSignInManager.Setup(x => x.CheckPasswordSignInAsync(testUser, "WrongPassword", false))
            .ReturnsAsync(SignInResult.Failed);

        // Act
        var result = await _mockSignInManager.Object.CheckPasswordSignInAsync(testUser, "WrongPassword", false);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public void RegisterDto_ShouldHaveRequiredProperties()
    {
        // Arrange & Act
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

        // Assert
        registerDto.Email.Should().Be("test@example.com");
        registerDto.FirstName.Should().Be("Test");
        registerDto.LastName.Should().Be("User");
        registerDto.Trainings.Should().NotBeNull();
        registerDto.Experiences.Should().NotBeNull();
    }

    [Fact]
    public void LoginDto_ShouldHaveRequiredProperties()
    {
        // Arrange & Act
        var loginDto = new LoginDto
        {
            Email = "test@example.com",
            Password = "Test123!"
        };

        // Assert
        loginDto.Email.Should().Be("test@example.com");
        loginDto.Password.Should().Be("Test123!");
    }

    [Fact]
    public void ConfirmEmailDto_ShouldHaveRequiredProperties()
    {
        // Arrange & Act
        var confirmEmailDto = new ConfirmEmailDto
        {
            Email = "test@example.com",
            Token = "test-token"
        };

        // Assert
        confirmEmailDto.Email.Should().Be("test@example.com");
        confirmEmailDto.Token.Should().Be("test-token");
    }

    [Fact]
    public void ForgotPasswordDto_ShouldHaveRequiredProperties()
    {
        // Arrange & Act
        var forgotPasswordDto = new ForgotPasswordDto
        {
            Email = "test@example.com"
        };

        // Assert
        forgotPasswordDto.Email.Should().Be("test@example.com");
    }

    [Fact]
    public void ResetPasswordDto_ShouldHaveRequiredProperties()
    {
        // Arrange & Act
        var resetPasswordDto = new ResetPasswordDto
        {
            Email = "test@example.com",
            Token = "reset-token",
            NewPassword = "NewPassword123!",
            ConfirmPassword = "NewPassword123!"
        };

        // Assert
        resetPasswordDto.Email.Should().Be("test@example.com");
        resetPasswordDto.Token.Should().Be("reset-token");
        resetPasswordDto.NewPassword.Should().Be("NewPassword123!");
        resetPasswordDto.ConfirmPassword.Should().Be("NewPassword123!");
    }
}