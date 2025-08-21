using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Moq;
using XpertSphere.MonolithApi.DTOs.UserRole;
using XpertSphere.MonolithApi.Models;
using XpertSphere.MonolithApi.Services;
using XpertSphere.MonolithApi.Tests.Helpers;

namespace XpertSphere.MonolithApi.Tests.Services;

public class UserRoleServiceTests : IDisposable
{
    private readonly Mock<IValidator<AssignRoleDto>> _mockAssignRoleValidator;
    private readonly Mock<ILogger<UserRoleService>> _mockLogger;
    private readonly XpertSphere.MonolithApi.Data.XpertSphereDbContext _context;

    public UserRoleServiceTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _mockAssignRoleValidator = new Mock<IValidator<AssignRoleDto>>();
        _mockLogger = MockHelper.CreateMockLogger<UserRoleService>();
    }

    [Fact]
    public async Task GetUserRolesAsync_WithExistingUser_ShouldReturnSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            IsActive = true
        };

        var role = new Role
        {
            Id = roleId,
            Name = "TestRole",
            DisplayName = "Test Role",
            IsActive = true
        };

        var userRole = new UserRole
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RoleId = roleId,
            IsActive = true,
            AssignedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        _context.Roles.Add(role);
        _context.UserRoles.Add(userRole);
        await _context.SaveChangesAsync();

        var mapper = AutoMapperHelper.CreateMapper();
        var userRoleService = CreateUserRoleService(mapper);

        // Act
        var result = await userRoleService.GetUserRolesAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task GetRoleUsersAsync_WithExistingRole_ShouldReturnSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            IsActive = true
        };

        var role = new Role
        {
            Id = roleId,
            Name = "TestRole",
            DisplayName = "Test Role",
            IsActive = true
        };

        var userRole = new UserRole
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RoleId = roleId,
            IsActive = true,
            AssignedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        _context.Roles.Add(role);
        _context.UserRoles.Add(userRole);
        await _context.SaveChangesAsync();

        var mapper = AutoMapperHelper.CreateMapper();
        var userRoleService = CreateUserRoleService(mapper);

        // Act
        var result = await userRoleService.GetRoleUsersAsync(roleId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task AssignRoleToUserAsync_WithNonExistingUser_ShouldReturnNotFound()
    {
        // Arrange
        var assignRoleDto = new AssignRoleDto
        {
            UserId = Guid.NewGuid(),
            RoleId = Guid.NewGuid()
        };

        _mockAssignRoleValidator.Setup(x => x.ValidateAsync(assignRoleDto, default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        var mapper = AutoMapperHelper.CreateMapper();
        var userRoleService = CreateUserRoleService(mapper);

        // Act
        var result = await userRoleService.AssignRoleToUserAsync(assignRoleDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain($"User with ID {assignRoleDto.UserId} not found");
    }

    [Fact]
    public async Task AssignRoleToUserAsync_WithNonExistingRole_ShouldReturnNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var assignRoleDto = new AssignRoleDto
        {
            UserId = userId,
            RoleId = Guid.NewGuid()
        };

        _mockAssignRoleValidator.Setup(x => x.ValidateAsync(assignRoleDto, default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        var mapper = AutoMapperHelper.CreateMapper();
        var userRoleService = CreateUserRoleService(mapper);

        // Act
        var result = await userRoleService.AssignRoleToUserAsync(assignRoleDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain($"Role with ID {assignRoleDto.RoleId} not found");
    }

    [Fact]
    public async Task RemoveRoleFromUserAsync_WithNonExistingUserRole_ShouldReturnNotFound()
    {
        // Arrange
        var userRoleId = Guid.NewGuid();
        var mapper = AutoMapperHelper.CreateMapper();
        var userRoleService = CreateUserRoleService(mapper);

        // Act
        var result = await userRoleService.RemoveRoleFromUserAsync(userRoleId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain($"User role assignment with ID {userRoleId} not found");
    }

    [Fact]
    public async Task UpdateUserRoleStatusAsync_WithNonExistingUserRole_ShouldReturnNotFound()
    {
        // Arrange
        var userRoleId = Guid.NewGuid();
        var mapper = AutoMapperHelper.CreateMapper();
        var userRoleService = CreateUserRoleService(mapper);

        // Act
        var result = await userRoleService.UpdateUserRoleStatusAsync(userRoleId, true);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain($"User role assignment with ID {userRoleId} not found");
    }

    [Fact]
    public async Task ExtendUserRoleAsync_WithNonExistingUserRole_ShouldReturnNotFound()
    {
        // Arrange
        var userRoleId = Guid.NewGuid();
        var newExpiryDate = DateTime.UtcNow.AddDays(30);
        var mapper = AutoMapperHelper.CreateMapper();
        var userRoleService = CreateUserRoleService(mapper);

        // Act
        var result = await userRoleService.ExtendUserRoleAsync(userRoleId, newExpiryDate);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain($"User role assignment with ID {userRoleId} not found");
    }

    [Fact]
    public async Task UserHasRoleAsync_WithNonExistingRole_ShouldReturnFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleName = "NonExistingRole";
        var mapper = AutoMapperHelper.CreateMapper();
        var userRoleService = CreateUserRoleService(mapper);

        // Act
        var result = await userRoleService.UserHasRoleAsync(userId, roleName);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeFalse();
    }

    [Fact]
    public async Task UserHasActiveRoleAsync_WithNonExistingRole_ShouldReturnFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleName = "NonExistingRole";
        var mapper = AutoMapperHelper.CreateMapper();
        var userRoleService = CreateUserRoleService(mapper);

        // Act
        var result = await userRoleService.UserHasActiveRoleAsync(userId, roleName);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeFalse();
    }

    [Fact]
    public async Task GetUserRoleNamesAsync_WithUserWithoutRoles_ShouldReturnEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var mapper = AutoMapperHelper.CreateMapper();
        var userRoleService = CreateUserRoleService(mapper);

        // Act
        var result = await userRoleService.GetUserRoleNamesAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Should().BeEmpty();
    }

    private UserRoleService CreateUserRoleService(AutoMapper.IMapper mapper)
    {
        return new UserRoleService(
            _context,
            mapper,
            _mockAssignRoleValidator.Object,
            _mockLogger.Object
        );
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}