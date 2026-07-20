using System.Security.Claims;
using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Moq;
using XpertSphere.MonolithApi.DTOs.Role;
using XpertSphere.MonolithApi.Interfaces;
using XpertSphere.MonolithApi.Models;
using XpertSphere.MonolithApi.Services;
using XpertSphere.MonolithApi.Tests.Helpers;
using XpertSphere.MonolithApi.Utils;

namespace XpertSphere.MonolithApi.Tests.Services;

public class RoleServiceTests : IDisposable
{
    private readonly Mock<IValidator<CreateRoleDto>> _mockCreateRoleValidator;
    private readonly Mock<IValidator<UpdateRoleDto>> _mockUpdateRoleValidator;
    private readonly Mock<IValidator<RoleFilterDto>> _mockFilterValidator;
    private readonly Mock<ILogger<RoleService>> _mockLogger;
    private readonly Data.XpertSphereDbContext _context;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;

    public RoleServiceTests()
    {
        // Base de données InMemory dédiée à l'instance de test (au lieu du nom par défaut partagé
        // entre plusieurs classes de tests) : nécessaire pour les nouveaux tests de scoping par
        // organisation ci-dessous, qui listent des rôles paginés et doivent pouvoir compter sur un
        // jeu de données déterministe, non pollué par d'autres classes de tests utilisant la même
        // XpertSphereDbContext par défaut.
        _context = TestDbContextFactory.CreateInMemoryContext(Guid.NewGuid().ToString());
        _mockCreateRoleValidator = new Mock<IValidator<CreateRoleDto>>();
        _mockUpdateRoleValidator = new Mock<IValidator<UpdateRoleDto>>();
        _mockFilterValidator = new Mock<IValidator<RoleFilterDto>>();
        _mockLogger = MockHelper.CreateMockLogger<RoleService>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
    }

    [Fact]
    public async Task CreateRoleAsync_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var createRoleDto = new CreateRoleDto
        {
            Name = "TestRole2",
            DisplayName = "Test Role2",
            Description = "A test role2"
        };

        _mockCreateRoleValidator.Setup(x => x.ValidateAsync(createRoleDto, default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        var mapper = AutoMapperHelper.CreateMapper();
        var roleService = CreateRoleService(mapper);

        // Act
        var result = await roleService.CreateRoleAsync(createRoleDto);
        
        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Errors.Should().BeEmpty();
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

    [Fact]
    public async Task GetAllPaginatedRolesAsync_AsOrganizationAdmin_ShouldScopeUsersCountToOrganization()
    {
        // Arrange
        var organizationAId = Guid.NewGuid();
        var organizationBId = Guid.NewGuid();

        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = "Organization.Recruiter",
            DisplayName = "Recruiter",
            IsActive = true
        };

        var userInOrgA = new User
        {
            Id = Guid.NewGuid(),
            Email = "recruiter-a@example.com",
            FirstName = "Recruiter",
            LastName = "OrgA",
            IsActive = true,
            OrganizationId = organizationAId
        };

        var userInOrgB = new User
        {
            Id = Guid.NewGuid(),
            Email = "recruiter-b@example.com",
            FirstName = "Recruiter",
            LastName = "OrgB",
            IsActive = true,
            OrganizationId = organizationBId
        };

        _context.Roles.Add(role);
        _context.Users.AddRange(userInOrgA, userInOrgB);
        _context.UserRoles.AddRange(
            new UserRole { Id = Guid.NewGuid(), UserId = userInOrgA.Id, RoleId = role.Id, IsActive = true },
            new UserRole { Id = Guid.NewGuid(), UserId = userInOrgB.Id, RoleId = role.Id, IsActive = true }
        );
        await _context.SaveChangesAsync();

        _mockFilterValidator.Setup(x => x.ValidateAsync(It.IsAny<RoleFilterDto>(), default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _mockCurrentUserService.Setup(x => x.User)
            .Returns(CreateClaimsPrincipal(Roles.OrganizationAdmin.Name));
        _mockCurrentUserService.Setup(x => x.OrganizationId).Returns(organizationAId);

        var mapper = AutoMapperHelper.CreateMapper();
        var roleService = CreateRoleService(mapper);

        // Act
        var result = await roleService.GetAllPaginatedRolesAsync(new RoleFilterDto());

        // Assert
        result.IsSuccess.Should().BeTrue();
        var roleDto = result.Data.Should().ContainSingle(r => r.Id == role.Id).Subject;
        roleDto.UsersCount.Should().Be(1);
    }

    [Fact]
    public async Task GetAllPaginatedRolesAsync_AsPlatformAdmin_ShouldCountAllOrganizations()
    {
        // Arrange
        var organizationAId = Guid.NewGuid();
        var organizationBId = Guid.NewGuid();

        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = "Organization.Recruiter",
            DisplayName = "Recruiter",
            IsActive = true
        };

        var userInOrgA = new User
        {
            Id = Guid.NewGuid(),
            Email = "recruiter-a2@example.com",
            FirstName = "Recruiter",
            LastName = "OrgA",
            IsActive = true,
            OrganizationId = organizationAId
        };

        var userInOrgB = new User
        {
            Id = Guid.NewGuid(),
            Email = "recruiter-b2@example.com",
            FirstName = "Recruiter",
            LastName = "OrgB",
            IsActive = true,
            OrganizationId = organizationBId
        };

        _context.Roles.Add(role);
        _context.Users.AddRange(userInOrgA, userInOrgB);
        _context.UserRoles.AddRange(
            new UserRole { Id = Guid.NewGuid(), UserId = userInOrgA.Id, RoleId = role.Id, IsActive = true },
            new UserRole { Id = Guid.NewGuid(), UserId = userInOrgB.Id, RoleId = role.Id, IsActive = true }
        );
        await _context.SaveChangesAsync();

        _mockFilterValidator.Setup(x => x.ValidateAsync(It.IsAny<RoleFilterDto>(), default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        // PlatformAdmin : comportement inchangé, total toutes organisations confondues,
        // indépendamment de la valeur d'OrganizationId (ici non configurée, comme en production
        // pour un compte plateforme qui ne porte pas nécessairement ce claim).
        _mockCurrentUserService.Setup(x => x.User)
            .Returns(CreateClaimsPrincipal(Roles.PlatformAdmin.Name));

        var mapper = AutoMapperHelper.CreateMapper();
        var roleService = CreateRoleService(mapper);

        // Act
        var result = await roleService.GetAllPaginatedRolesAsync(new RoleFilterDto());

        // Assert
        result.IsSuccess.Should().BeTrue();
        var roleDto = result.Data.Should().ContainSingle(r => r.Id == role.Id).Subject;
        roleDto.UsersCount.Should().Be(2);
    }

    [Fact]
    public async Task GetAllPaginatedRolesAsync_AsOrganizationAdmin_WithNoUsersInOwnOrganization_ShouldReturnZero()
    {
        // Arrange
        var organizationAId = Guid.NewGuid();
        var organizationBId = Guid.NewGuid();

        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = "Organization.Recruiter",
            DisplayName = "Recruiter",
            IsActive = true
        };

        var userInOrgB = new User
        {
            Id = Guid.NewGuid(),
            Email = "recruiter-b3@example.com",
            FirstName = "Recruiter",
            LastName = "OrgB",
            IsActive = true,
            OrganizationId = organizationBId
        };

        _context.Roles.Add(role);
        _context.Users.Add(userInOrgB);
        _context.UserRoles.Add(
            new UserRole { Id = Guid.NewGuid(), UserId = userInOrgB.Id, RoleId = role.Id, IsActive = true });
        await _context.SaveChangesAsync();

        _mockFilterValidator.Setup(x => x.ValidateAsync(It.IsAny<RoleFilterDto>(), default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _mockCurrentUserService.Setup(x => x.User)
            .Returns(CreateClaimsPrincipal(Roles.OrganizationAdmin.Name));
        _mockCurrentUserService.Setup(x => x.OrganizationId).Returns(organizationAId);

        var mapper = AutoMapperHelper.CreateMapper();
        var roleService = CreateRoleService(mapper);

        // Act
        var result = await roleService.GetAllPaginatedRolesAsync(new RoleFilterDto());

        // Assert
        result.IsSuccess.Should().BeTrue();
        var roleDto = result.Data.Should().ContainSingle(r => r.Id == role.Id).Subject;
        roleDto.UsersCount.Should().Be(0);
    }

    private static ClaimsPrincipal CreateClaimsPrincipal(string roleName)
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, roleName)],
            authenticationType: "TestAuth");
        return new ClaimsPrincipal(identity);
    }

    private RoleService CreateRoleService(AutoMapper.IMapper mapper)
    {
        return new RoleService(
            _context,
            mapper,
            _mockCreateRoleValidator.Object,
            _mockUpdateRoleValidator.Object,
            _mockFilterValidator.Object,
            _mockLogger.Object,
            _mockCurrentUserService.Object
        );
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}