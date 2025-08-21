using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Moq;
using XpertSphere.MonolithApi.DTOs.Role;
using XpertSphere.MonolithApi.Models;
using XpertSphere.MonolithApi.Services;
using XpertSphere.MonolithApi.Tests.Helpers;

namespace XpertSphere.MonolithApi.Tests.Services;

public class RoleServiceTests : IDisposable
{
    private readonly Mock<IValidator<CreateRoleDto>> _mockCreateRoleValidator;
    private readonly Mock<IValidator<UpdateRoleDto>> _mockUpdateRoleValidator;
    private readonly Mock<IValidator<RoleFilterDto>> _mockFilterValidator;
    private readonly Mock<ILogger<RoleService>> _mockLogger;
    private readonly XpertSphere.MonolithApi.Data.XpertSphereDbContext _context;

    public RoleServiceTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _mockCreateRoleValidator = new Mock<IValidator<CreateRoleDto>>();
        _mockUpdateRoleValidator = new Mock<IValidator<UpdateRoleDto>>();
        _mockFilterValidator = new Mock<IValidator<RoleFilterDto>>();
        _mockLogger = MockHelper.CreateMockLogger<RoleService>();
    }

    [Fact]
    public async Task CreateRoleAsync_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var createRoleDto = new CreateRoleDto
        {
            Name = "TestRole",
            DisplayName = "Test Role",
            Description = "A test role"
        };

        _mockCreateRoleValidator.Setup(x => x.ValidateAsync(createRoleDto, default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        var mapper = AutoMapperHelper.CreateMapper();
        var roleService = CreateRoleService(mapper);

        // Act
        var result = await roleService.CreateRoleAsync(createRoleDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("A role with name 'TestRole' already exists");
    }

    [Fact]
    public async Task CreateRoleAsync_WithDuplicateName_ShouldReturnConflict()
    {
        // Arrange
        var existingRole = new Role
        {
            Id = Guid.NewGuid(),
            Name = "ExistingRole",
            DisplayName = "Existing Role",
            IsActive = true
        };

        _context.Roles.Add(existingRole);
        await _context.SaveChangesAsync();

        var createRoleDto = new CreateRoleDto
        {
            Name = "ExistingRole",
            DisplayName = "New Role",
            Description = "A duplicate role"
        };

        _mockCreateRoleValidator.Setup(x => x.ValidateAsync(createRoleDto, default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        var mapper = AutoMapperHelper.CreateMapper();
        var roleService = CreateRoleService(mapper);

        // Act
        var result = await roleService.CreateRoleAsync(createRoleDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("A role with name 'ExistingRole' already exists");
    }

    [Fact]
    public async Task GetRoleByIdAsync_WithExistingRole_ShouldReturnSuccess()
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

        var mapper = AutoMapperHelper.CreateMapper();
        var roleService = CreateRoleService(mapper);

        // Act
        var result = await roleService.GetRoleByIdAsync(roleId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(roleId);
        result.Data.Name.Should().Be("TestRole");
    }

    [Fact]
    public async Task GetRoleByIdAsync_WithNonExistingRole_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();
        var mapper = AutoMapperHelper.CreateMapper();
        var roleService = CreateRoleService(mapper);

        // Act
        var result = await roleService.GetRoleByIdAsync(nonExistingId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain($"Role with ID {nonExistingId} not found");
    }

    [Fact]
    public async Task GetRoleByNameAsync_WithExistingRole_ShouldReturnSuccess()
    {
        // Arrange
        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = "AdminRole",
            DisplayName = "Administrator Role",
            IsActive = true
        };

        _context.Roles.Add(role);
        await _context.SaveChangesAsync();

        var mapper = AutoMapperHelper.CreateMapper();
        var roleService = CreateRoleService(mapper);

        // Act
        var result = await roleService.GetRoleByNameAsync("AdminRole");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Name.Should().Be("AdminRole");
    }

    [Fact]
    public async Task UpdateRoleAsync_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var role = new Role
        {
            Id = roleId,
            Name = "OriginalRole",
            DisplayName = "Original Role",
            IsActive = true
        };

        _context.Roles.Add(role);
        await _context.SaveChangesAsync();

        var updateRoleDto = new UpdateRoleDto
        {
            DisplayName = "Updated Role",
            Description = "Updated description",
            IsActive = true
        };

        _mockUpdateRoleValidator.Setup(x => x.ValidateAsync(updateRoleDto, default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        var mapper = AutoMapperHelper.CreateMapper();
        var roleService = CreateRoleService(mapper);

        // Act
        var result = await roleService.UpdateRoleAsync(roleId, updateRoleDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Message.Should().Contain("Role updated successfully");
    }

    [Fact]
    public async Task DeleteRoleAsync_WithExistingRole_ShouldReturnSuccess()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var role = new Role
        {
            Id = roleId,
            Name = "RoleToDelete",
            DisplayName = "Role To Delete",
            IsActive = true
        };

        _context.Roles.Add(role);
        await _context.SaveChangesAsync();

        var mapper = AutoMapperHelper.CreateMapper();
        var roleService = CreateRoleService(mapper);

        // Act
        var result = await roleService.DeleteRoleAsync(roleId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Message.Should().Contain("Role deleted successfully");
    }

    [Fact]
    public async Task ActivateRoleAsync_WithInactiveRole_ShouldReturnSuccess()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var role = new Role
        {
            Id = roleId,
            Name = "InactiveRole",
            DisplayName = "Inactive Role",
            IsActive = false
        };

        _context.Roles.Add(role);
        await _context.SaveChangesAsync();

        var mapper = AutoMapperHelper.CreateMapper();
        var roleService = CreateRoleService(mapper);

        // Act
        var result = await roleService.ActivateRoleAsync(roleId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Message.Should().Contain("Role activated successfully");
    }

    [Fact]
    public async Task DeactivateRoleAsync_WithActiveRole_ShouldReturnSuccess()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var role = new Role
        {
            Id = roleId,
            Name = "ActiveRole",
            DisplayName = "Active Role",
            IsActive = true
        };

        _context.Roles.Add(role);
        await _context.SaveChangesAsync();

        var mapper = AutoMapperHelper.CreateMapper();
        var roleService = CreateRoleService(mapper);

        // Act
        var result = await roleService.DeactivateRoleAsync(roleId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Message.Should().Contain("Role deactivated successfully");
    }

    [Fact]
    public async Task RoleExistsAsync_WithExistingRole_ShouldReturnTrue()
    {
        // Arrange
        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = "ExistingRole",
            DisplayName = "Existing Role",
            IsActive = true
        };

        _context.Roles.Add(role);
        await _context.SaveChangesAsync();

        var mapper = AutoMapperHelper.CreateMapper();
        var roleService = CreateRoleService(mapper);

        // Act
        var result = await roleService.RoleExistsAsync("ExistingRole");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();
    }

    private RoleService CreateRoleService(AutoMapper.IMapper mapper)
    {
        return new RoleService(
            _context,
            mapper,
            _mockCreateRoleValidator.Object,
            _mockUpdateRoleValidator.Object,
            _mockFilterValidator.Object,
            _mockLogger.Object
        );
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}