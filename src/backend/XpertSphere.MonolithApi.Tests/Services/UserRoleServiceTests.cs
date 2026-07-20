using System.Security.Claims;
using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Moq;
using XpertSphere.MonolithApi.DTOs.UserRole;
using XpertSphere.MonolithApi.Interfaces;
using XpertSphere.MonolithApi.Models;
using XpertSphere.MonolithApi.Services;
using XpertSphere.MonolithApi.Tests.Helpers;
using XpertSphere.MonolithApi.Utils;

namespace XpertSphere.MonolithApi.Tests.Services;

public class UserRoleServiceTests : IDisposable
{
    private readonly Mock<IValidator<AssignRoleDto>> _mockAssignRoleValidator;
    private readonly Mock<ILogger<UserRoleService>> _mockLogger;
    private readonly XpertSphere.MonolithApi.Data.XpertSphereDbContext _context;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;

    public UserRoleServiceTests()
    {
        // Base de données InMemory dédiée à l'instance de test (même raison que RoleServiceTests) :
        // les nouveaux tests de scoping par organisation ci-dessous ont besoin d'un jeu de données
        // déterministe, non partagé avec d'autres classes de tests utilisant le nom par défaut.
        _context = TestDbContextFactory.CreateInMemoryContext(Guid.NewGuid().ToString());
        _mockAssignRoleValidator = new Mock<IValidator<AssignRoleDto>>();
        _mockLogger = MockHelper.CreateMockLogger<UserRoleService>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
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

    [Fact]
    public async Task GetRoleUsersAsync_AsOrganizationAdmin_ShouldScopeToOwnOrganization()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var organizationAId = Guid.NewGuid();
        var organizationBId = Guid.NewGuid();

        var role = new Role
        {
            Id = roleId,
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
            new UserRole { Id = Guid.NewGuid(), UserId = userInOrgA.Id, RoleId = roleId, IsActive = true },
            new UserRole { Id = Guid.NewGuid(), UserId = userInOrgB.Id, RoleId = roleId, IsActive = true }
        );
        await _context.SaveChangesAsync();

        _mockCurrentUserService.Setup(x => x.User)
            .Returns(CreateClaimsPrincipal(Roles.OrganizationAdmin.Name));
        _mockCurrentUserService.Setup(x => x.OrganizationId).Returns(organizationAId);

        var mapper = AutoMapperHelper.CreateMapper();
        var userRoleService = CreateUserRoleService(mapper);

        // Act
        var result = await userRoleService.GetRoleUsersAsync(roleId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().ContainSingle().Which.UserId.Should().Be(userInOrgA.Id);
    }

    [Fact]
    public async Task GetRoleUsersAsync_AsPlatformAdmin_ShouldReturnAllOrganizations()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var organizationAId = Guid.NewGuid();
        var organizationBId = Guid.NewGuid();

        var role = new Role
        {
            Id = roleId,
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
            new UserRole { Id = Guid.NewGuid(), UserId = userInOrgA.Id, RoleId = roleId, IsActive = true },
            new UserRole { Id = Guid.NewGuid(), UserId = userInOrgB.Id, RoleId = roleId, IsActive = true }
        );
        await _context.SaveChangesAsync();

        _mockCurrentUserService.Setup(x => x.User)
            .Returns(CreateClaimsPrincipal(Roles.PlatformAdmin.Name));

        var mapper = AutoMapperHelper.CreateMapper();
        var userRoleService = CreateUserRoleService(mapper);

        // Act
        var result = await userRoleService.GetRoleUsersAsync(roleId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(2);
    }

    private static ClaimsPrincipal CreateClaimsPrincipal(string roleName)
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, roleName)],
            authenticationType: "TestAuth");
        return new ClaimsPrincipal(identity);
    }

    private UserRoleService CreateUserRoleService(AutoMapper.IMapper mapper)
    {
        return new UserRoleService(
            _context,
            mapper,
            _mockAssignRoleValidator.Object,
            _mockLogger.Object,
            _mockCurrentUserService.Object
        );
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}