using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Moq;
using XpertSphere.MonolithApi.DTOs.RolePermission;
using XpertSphere.MonolithApi.Enums;
using XpertSphere.MonolithApi.Models;
using XpertSphere.MonolithApi.Services;
using XpertSphere.MonolithApi.Tests.Helpers;

namespace XpertSphere.MonolithApi.Tests.Services;

public class RolePermissionServiceTests : IDisposable
{
    private readonly Mock<IValidator<AssignPermissionDto>> _mockAssignPermissionValidator;
    private readonly Mock<ILogger<RolePermissionService>> _mockLogger;
    private readonly XpertSphere.MonolithApi.Data.XpertSphereDbContext _context;

    public RolePermissionServiceTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _mockAssignPermissionValidator = new Mock<IValidator<AssignPermissionDto>>();
        _mockLogger = MockHelper.CreateMockLogger<RolePermissionService>();
    }

    [Fact]
    public async Task GetRolePermissionsAsync_WithExistingRole_ShouldReturnSuccess()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();

        var role = new Role
        {
            Id = roleId,
            Name = "TestRole",
            DisplayName = "Test Role",
            IsActive = true
        };

        var permission = new Permission
        {
            Id = permissionId,
            Name = "TestPermission",
            Resource = "User",
            Action = PermissionAction.Read,
            Category = "Admin"
        };

        var rolePermission = new RolePermission
        {
            Id = Guid.NewGuid(),
            RoleId = roleId,
            PermissionId = permissionId
        };

        _context.Roles.Add(role);
        _context.Permissions.Add(permission);
        _context.RolePermissions.Add(rolePermission);
        await _context.SaveChangesAsync();

        var mapper = AutoMapperHelper.CreateMapper();
        var rolePermissionService = CreateRolePermissionService(mapper);

        // Act
        var result = await rolePermissionService.GetRolePermissionsAsync(roleId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPermissionRolesAsync_WithExistingPermission_ShouldReturnSuccess()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();

        var role = new Role
        {
            Id = roleId,
            Name = "TestRole",
            DisplayName = "Test Role",
            IsActive = true
        };

        var permission = new Permission
        {
            Id = permissionId,
            Name = "TestPermission",
            Resource = "User",
            Action = PermissionAction.Read,
            Category = "Admin"
        };

        var rolePermission = new RolePermission
        {
            Id = Guid.NewGuid(),
            RoleId = roleId,
            PermissionId = permissionId
        };

        _context.Roles.Add(role);
        _context.Permissions.Add(permission);
        _context.RolePermissions.Add(rolePermission);
        await _context.SaveChangesAsync();

        var mapper = AutoMapperHelper.CreateMapper();
        var rolePermissionService = CreateRolePermissionService(mapper);

        // Act
        var result = await rolePermissionService.GetPermissionRolesAsync(permissionId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task AssignPermissionToRoleAsync_WithNonExistingRole_ShouldReturnNotFound()
    {
        // Arrange
        var assignPermissionDto = new AssignPermissionDto
        {
            RoleId = Guid.NewGuid(),
            PermissionId = Guid.NewGuid()
        };

        _mockAssignPermissionValidator.Setup(x => x.ValidateAsync(assignPermissionDto, default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        var mapper = AutoMapperHelper.CreateMapper();
        var rolePermissionService = CreateRolePermissionService(mapper);

        // Act
        var result = await rolePermissionService.AssignPermissionToRoleAsync(assignPermissionDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain($"Role with ID {assignPermissionDto.RoleId} not found");
    }

    [Fact]
    public async Task AssignPermissionToRoleAsync_WithNonExistingPermission_ShouldReturnNotFound()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var role = new Role
        {
            Id = roleId,
            Name = "TestRole",
            DisplayName = "Test Role",
            IsActive = true
        };

        _context.Roles.Add(role);
        await _context.SaveChangesAsync();

        var assignPermissionDto = new AssignPermissionDto
        {
            RoleId = roleId,
            PermissionId = Guid.NewGuid()
        };

        _mockAssignPermissionValidator.Setup(x => x.ValidateAsync(assignPermissionDto, default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        var mapper = AutoMapperHelper.CreateMapper();
        var rolePermissionService = CreateRolePermissionService(mapper);

        // Act
        var result = await rolePermissionService.AssignPermissionToRoleAsync(assignPermissionDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain($"Permission with ID {assignPermissionDto.PermissionId} not found");
    }

    [Fact]
    public async Task AssignPermissionToRoleAsync_WithExistingAssignment_ShouldReturnConflict()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();

        var role = new Role
        {
            Id = roleId,
            Name = "TestRole",
            DisplayName = "Test Role",
            IsActive = true
        };

        var permission = new Permission
        {
            Id = permissionId,
            Name = "TestPermission",
            Resource = "User",
            Action = PermissionAction.Read,
            Category = "Admin"
        };

        var existingRolePermission = new RolePermission
        {
            Id = Guid.NewGuid(),
            RoleId = roleId,
            PermissionId = permissionId
        };

        _context.Roles.Add(role);
        _context.Permissions.Add(permission);
        _context.RolePermissions.Add(existingRolePermission);
        await _context.SaveChangesAsync();

        var assignPermissionDto = new AssignPermissionDto
        {
            RoleId = roleId,
            PermissionId = permissionId
        };

        _mockAssignPermissionValidator.Setup(x => x.ValidateAsync(assignPermissionDto, default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        var mapper = AutoMapperHelper.CreateMapper();
        var rolePermissionService = CreateRolePermissionService(mapper);

        // Act
        var result = await rolePermissionService.AssignPermissionToRoleAsync(assignPermissionDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Role already has this permission assigned");
    }

    [Fact]
    public async Task RemovePermissionFromRoleAsync_WithNonExistingRolePermission_ShouldReturnNotFound()
    {
        // Arrange
        var rolePermissionId = Guid.NewGuid();
        var mapper = AutoMapperHelper.CreateMapper();
        var rolePermissionService = CreateRolePermissionService(mapper);

        // Act
        var result = await rolePermissionService.RemovePermissionFromRoleAsync(rolePermissionId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain($"Role permission assignment with ID {rolePermissionId} not found");
    }

    [Fact]
    public async Task RoleHasPermissionAsync_WithNonExistingPermission_ShouldReturnFalse()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var permissionName = "NonExistingPermission";
        var mapper = AutoMapperHelper.CreateMapper();
        var rolePermissionService = CreateRolePermissionService(mapper);

        // Act
        var result = await rolePermissionService.RoleHasPermissionAsync(roleId, permissionName);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeFalse();
    }

    [Fact]
    public async Task RoleHasPermissionAsync_WithExistingPermission_ShouldReturnTrue()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();

        var role = new Role
        {
            Id = roleId,
            Name = "TestRole",
            DisplayName = "Test Role",
            IsActive = true
        };

        var permission = new Permission
        {
            Id = permissionId,
            Name = "ExistingPermission",
            Resource = "User",
            Action = PermissionAction.Read,
            Category = "Admin"
        };

        var rolePermission = new RolePermission
        {
            Id = Guid.NewGuid(),
            RoleId = roleId,
            PermissionId = permissionId
        };

        _context.Roles.Add(role);
        _context.Permissions.Add(permission);
        _context.RolePermissions.Add(rolePermission);
        await _context.SaveChangesAsync();

        var mapper = AutoMapperHelper.CreateMapper();
        var rolePermissionService = CreateRolePermissionService(mapper);

        // Act
        var result = await rolePermissionService.RoleHasPermissionAsync(roleId, "ExistingPermission");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();
    }

    [Fact]
    public async Task GetRolePermissionNamesAsync_WithRoleWithoutPermissions_ShouldReturnEmptyList()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var mapper = AutoMapperHelper.CreateMapper();
        var rolePermissionService = CreateRolePermissionService(mapper);

        // Act
        var result = await rolePermissionService.GetRolePermissionNamesAsync(roleId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRolePermissionNamesAsync_WithRoleWithPermissions_ShouldReturnPermissionNames()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();

        var role = new Role
        {
            Id = roleId,
            Name = "TestRole",
            DisplayName = "Test Role",
            IsActive = true
        };

        var permission = new Permission
        {
            Id = permissionId,
            Name = "TestPermission",
            Resource = "User",
            Action = PermissionAction.Read,
            Category = "Admin"
        };

        var rolePermission = new RolePermission
        {
            Id = Guid.NewGuid(),
            RoleId = roleId,
            PermissionId = permissionId
        };

        _context.Roles.Add(role);
        _context.Permissions.Add(permission);
        _context.RolePermissions.Add(rolePermission);
        await _context.SaveChangesAsync();

        var mapper = AutoMapperHelper.CreateMapper();
        var rolePermissionService = CreateRolePermissionService(mapper);

        // Act
        var result = await rolePermissionService.GetRolePermissionNamesAsync(roleId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Should().Contain("TestPermission");
    }

    private RolePermissionService CreateRolePermissionService(AutoMapper.IMapper mapper)
    {
        return new RolePermissionService(
            _context,
            mapper,
            _mockAssignPermissionValidator.Object,
            _mockLogger.Object
        );
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}