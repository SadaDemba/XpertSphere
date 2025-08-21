using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Moq;
using XpertSphere.MonolithApi.DTOs.Permission;
using XpertSphere.MonolithApi.Enums;
using XpertSphere.MonolithApi.Models;
using XpertSphere.MonolithApi.Services;
using XpertSphere.MonolithApi.Tests.Helpers;

namespace XpertSphere.MonolithApi.Tests.Services;

public class PermissionServiceTests : IDisposable
{
    private readonly Mock<IValidator<CreatePermissionDto>> _mockCreateValidator;
    private readonly Mock<IValidator<PermissionFilterDto>> _mockFilterValidator;
    private readonly Mock<ILogger<PermissionService>> _mockLogger;
    private readonly XpertSphere.MonolithApi.Data.XpertSphereDbContext _context;

    public PermissionServiceTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _mockCreateValidator = new Mock<IValidator<CreatePermissionDto>>();
        _mockFilterValidator = new Mock<IValidator<PermissionFilterDto>>();
        _mockLogger = MockHelper.CreateMockLogger<PermissionService>();
    }

    [Fact]
    public async Task GetAllPermissionsAsync_ShouldReturnSuccess()
    {
        // Arrange
        var permission = new Permission
        {
            Id = Guid.NewGuid(),
            Name = "TestPermission",
            Resource = "User",
            Action = PermissionAction.Read,
            Category = "Admin"
        };

        _context.Permissions.Add(permission);
        await _context.SaveChangesAsync();

        var mapper = AutoMapperHelper.CreateMapper();
        var permissionService = CreatePermissionService(mapper);

        // Act
        var result = await permissionService.GetAllPermissionsAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPermissionByIdAsync_WithExistingPermission_ShouldReturnSuccess()
    {
        // Arrange
        var permissionId = Guid.NewGuid();
        var permission = new Permission
        {
            Id = permissionId,
            Name = "TestPermission",
            Resource = "User",
            Action = PermissionAction.Read,
            Category = "Admin"
        };

        _context.Permissions.Add(permission);
        await _context.SaveChangesAsync();

        var mapper = AutoMapperHelper.CreateMapper();
        var permissionService = CreatePermissionService(mapper);

        // Act
        var result = await permissionService.GetPermissionByIdAsync(permissionId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(permissionId);
    }

    [Fact]
    public async Task GetPermissionByIdAsync_WithNonExistingPermission_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();
        var mapper = AutoMapperHelper.CreateMapper();
        var permissionService = CreatePermissionService(mapper);

        // Act
        var result = await permissionService.GetPermissionByIdAsync(nonExistingId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain($"Permission with ID {nonExistingId} not found");
    }

    [Fact]
    public async Task GetPermissionsByResourceAsync_WithExistingResource_ShouldReturnSuccess()
    {
        // Arrange
        var permission = new Permission
        {
            Id = Guid.NewGuid(),
            Name = "UserReadPermission",
            Resource = "User",
            Action = PermissionAction.Read,
            Category = "Admin"
        };

        _context.Permissions.Add(permission);
        await _context.SaveChangesAsync();

        var mapper = AutoMapperHelper.CreateMapper();
        var permissionService = CreatePermissionService(mapper);

        // Act
        var result = await permissionService.GetPermissionsByResourceAsync("User");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPermissionsByCategoryAsync_WithExistingCategory_ShouldReturnSuccess()
    {
        // Arrange
        var permission = new Permission
        {
            Id = Guid.NewGuid(),
            Name = "AdminPermission",
            Resource = "User",
            Action = PermissionAction.Read,
            Category = "Admin"
        };

        _context.Permissions.Add(permission);
        await _context.SaveChangesAsync();

        var mapper = AutoMapperHelper.CreateMapper();
        var permissionService = CreatePermissionService(mapper);

        // Act
        var result = await permissionService.GetPermissionsByCategoryAsync("Admin");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task CreatePermissionAsync_WithDuplicateName_ShouldReturnConflict()
    {
        // Arrange
        var existingPermission = new Permission
        {
            Id = Guid.NewGuid(),
            Name = "ExistingPermission",
            Resource = "User",
            Action = PermissionAction.Read,
            Category = "Admin"
        };

        _context.Permissions.Add(existingPermission);
        await _context.SaveChangesAsync();

        var createPermissionDto = new CreatePermissionDto
        {
            Name = "ExistingPermission",
            Resource = "Role",
            Action = PermissionAction.Create,
            Category = "Admin"
        };

        _mockCreateValidator.Setup(x => x.ValidateAsync(createPermissionDto, default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        var mapper = AutoMapperHelper.CreateMapper();
        var permissionService = CreatePermissionService(mapper);

        // Act
        var result = await permissionService.CreatePermissionAsync(createPermissionDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("A permission with name 'ExistingPermission' already exists");
    }

    [Fact]
    public async Task DeletePermissionAsync_WithNonExistingPermission_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();
        var mapper = AutoMapperHelper.CreateMapper();
        var permissionService = CreatePermissionService(mapper);

        // Act
        var result = await permissionService.DeletePermissionAsync(nonExistingId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain($"Permission with ID {nonExistingId} not found");
    }

    [Fact]
    public async Task DeletePermissionAsync_WithPermissionAssignedToRoles_ShouldReturnFailure()
    {
        // Arrange
        var permissionId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        var permission = new Permission
        {
            Id = permissionId,
            Name = "AssignedPermission",
            Resource = "User",
            Action = PermissionAction.Read,
            Category = "Admin"
        };

        var role = new Role
        {
            Id = roleId,
            Name = "TestRole",
            DisplayName = "Test Role",
            IsActive = true
        };

        var rolePermission = new RolePermission
        {
            Id = Guid.NewGuid(),
            RoleId = roleId,
            PermissionId = permissionId
        };

        _context.Permissions.Add(permission);
        _context.Roles.Add(role);
        _context.RolePermissions.Add(rolePermission);
        await _context.SaveChangesAsync();

        var mapper = AutoMapperHelper.CreateMapper();
        var permissionService = CreatePermissionService(mapper);

        // Act
        var result = await permissionService.DeletePermissionAsync(permissionId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Cannot delete permission that is assigned to roles");
    }

    [Fact]
    public async Task PermissionExistsAsync_WithExistingPermission_ShouldReturnTrue()
    {
        // Arrange
        var permission = new Permission
        {
            Id = Guid.NewGuid(),
            Name = "ExistingPermission",
            Resource = "User",
            Action = PermissionAction.Read,
            Category = "Admin"
        };

        _context.Permissions.Add(permission);
        await _context.SaveChangesAsync();

        var mapper = AutoMapperHelper.CreateMapper();
        var permissionService = CreatePermissionService(mapper);

        // Act
        var result = await permissionService.PermissionExistsAsync("ExistingPermission");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();
    }

    [Fact]
    public async Task CanDeletePermissionAsync_WithUnassignedPermission_ShouldReturnTrue()
    {
        // Arrange
        var permissionId = Guid.NewGuid();
        var permission = new Permission
        {
            Id = permissionId,
            Name = "UnassignedPermission",
            Resource = "User",
            Action = PermissionAction.Read,
            Category = "Admin"
        };

        _context.Permissions.Add(permission);
        await _context.SaveChangesAsync();

        var mapper = AutoMapperHelper.CreateMapper();
        var permissionService = CreatePermissionService(mapper);

        // Act
        var result = await permissionService.CanDeletePermissionAsync(permissionId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();
    }

    private PermissionService CreatePermissionService(AutoMapper.IMapper mapper)
    {
        return new PermissionService(
            _context,
            mapper,
            _mockCreateValidator.Object,
            _mockFilterValidator.Object,
            _mockLogger.Object
        );
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}