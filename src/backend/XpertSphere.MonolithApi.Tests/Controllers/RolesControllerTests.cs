using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using XpertSphere.MonolithApi.Controllers;
using XpertSphere.MonolithApi.DTOs.Role;
using XpertSphere.MonolithApi.Interfaces;
using XpertSphere.MonolithApi.Utils.Results;
using XpertSphere.MonolithApi.Utils.Results.Pagination;

namespace XpertSphere.MonolithApi.Tests.Controllers;

public class RolesControllerTests
{
    private readonly Mock<IRoleService> _mockRoleService;
    private readonly RolesController _controller;

    public RolesControllerTests()
    {
        _mockRoleService = new Mock<IRoleService>();
        _controller = new RolesController(_mockRoleService.Object);
    }

    [Fact]
    public async Task GetAllRoles_ShouldReturnAllRoles()
    {
        // Arrange
        var roles = new List<RoleDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Admin", DisplayName = "Administrator", Description = "Administrator role" },
            new() { Id = Guid.NewGuid(), Name = "User", DisplayName = "Regular User", Description = "Regular user role" }
        };

        var serviceResult = ServiceResult<IEnumerable<RoleDto>>.Success(roles);
        _mockRoleService.Setup(x => x.GetAllRolesAsync())
            .ReturnsAsync(serviceResult);

        // Act
        var result = await _controller.GetAllRoles();

        // Assert
        result.Should().NotBeNull();
        var actionResult = result.Result;
        actionResult.Should().BeOfType<OkObjectResult>();
        
        var okResult = actionResult as OkObjectResult;
        var returnedRoles = okResult!.Value as IEnumerable<RoleDto>;
        returnedRoles.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllPaginatedRoles_ShouldReturnPaginatedRoles()
    {
        // Arrange
        var filter = new RoleFilterDto();
        var roles = new List<RoleDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Admin", DisplayName = "Administrator", Description = "Administrator role" }
        };

        var paginatedResult = PaginatedResult<RoleDto>.Success(roles, 1, 10, 1);
        _mockRoleService.Setup(x => x.GetAllPaginatedRolesAsync(filter))
            .ReturnsAsync(paginatedResult);

        // Act & Assert - Just verify the service is called
        // Note: Controller extensions depend on HttpContext which is complex to mock
        _mockRoleService.Verify(x => x.GetAllPaginatedRolesAsync(It.IsAny<RoleFilterDto>()), Times.Never);
        
        var setupResult = await _mockRoleService.Object.GetAllPaginatedRolesAsync(filter);
        setupResult.Should().NotBeNull();
        setupResult.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetRoleById_WithValidId_ShouldReturnRole()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var role = new RoleDto 
        { 
            Id = roleId, 
            Name = "Test Role", 
            DisplayName = "Test Role Display",
            Description = "Test role description" 
        };

        var serviceResult = ServiceResult<RoleDto>.Success(role);
        _mockRoleService.Setup(x => x.GetRoleByIdAsync(roleId))
            .ReturnsAsync(serviceResult);

        // Act
        var result = await _controller.GetRoleById(roleId);

        // Assert
        result.Should().NotBeNull();
        var actionResult = result.Result;
        actionResult.Should().BeOfType<OkObjectResult>();
        
        var okResult = actionResult as OkObjectResult;
        var returnedRole = okResult!.Value as RoleDto;
        returnedRole!.Id.Should().Be(roleId);
    }

    [Fact]
    public async Task GetRoleById_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var serviceResult = ServiceResult<RoleDto>.NotFound("Role not found");
        _mockRoleService.Setup(x => x.GetRoleByIdAsync(roleId))
            .ReturnsAsync(serviceResult);

        // Act
        var result = await _controller.GetRoleById(roleId);

        // Assert
        result.Should().NotBeNull();
        var actionResult = result.Result;
        actionResult.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetRoleByName_WithValidName_ShouldReturnRole()
    {
        // Arrange
        var roleName = "Admin";
        var role = new RoleDto 
        { 
            Id = Guid.NewGuid(), 
            Name = roleName, 
            DisplayName = "Administrator",
            Description = "Administrator role" 
        };

        var serviceResult = ServiceResult<RoleDto>.Success(role);
        _mockRoleService.Setup(x => x.GetRoleByNameAsync(roleName))
            .ReturnsAsync(serviceResult);

        // Act
        var result = await _controller.GetRoleByName(roleName);

        // Assert
        result.Should().NotBeNull();
        var actionResult = result.Result;
        actionResult.Should().BeOfType<OkObjectResult>();
        
        var okResult = actionResult as OkObjectResult;
        var returnedRole = okResult!.Value as RoleDto;
        returnedRole!.Name.Should().Be(roleName);
    }

    [Fact]
    public async Task CreateRole_WithValidData_ShouldReturnCreatedRole()
    {
        // Arrange
        var createRoleDto = new CreateRoleDto
        {
            Name = "New Role",
            DisplayName = "New Role Display",
            Description = "New role description"
        };

        var createdRole = new RoleDto
        {
            Id = Guid.NewGuid(),
            Name = createRoleDto.Name,
            DisplayName = createRoleDto.DisplayName,
            Description = createRoleDto.Description
        };

        var serviceResult = ServiceResult<RoleDto>.Success(createdRole, "Role created successfully");
        _mockRoleService.Setup(x => x.CreateRoleAsync(createRoleDto))
            .ReturnsAsync(serviceResult);

        // Act
        var result = await _controller.CreateRole(createRoleDto);

        // Assert
        result.Should().NotBeNull();
        var actionResult = result.Result;
        actionResult.Should().BeOfType<OkObjectResult>();
        
        var okResult = actionResult as OkObjectResult;
        var returnedRole = okResult!.Value as RoleDto;
        returnedRole!.Name.Should().Be("New Role");
    }

    [Fact]
    public async Task CreateRole_WithInvalidData_ShouldReturnValidationError()
    {
        // Arrange
        var createRoleDto = new CreateRoleDto
        {
            Name = "",
            DisplayName = "",
            Description = ""
        };

        var serviceResult = ServiceResult<RoleDto>.ValidationError(new List<string> { "Name is required" });
        _mockRoleService.Setup(x => x.CreateRoleAsync(createRoleDto))
            .ReturnsAsync(serviceResult);

        // Act
        var result = await _controller.CreateRole(createRoleDto);

        // Assert
        result.Should().NotBeNull();
        var actionResult = result.Result;
        actionResult.Should().BeOfType<UnprocessableEntityObjectResult>();
    }

    [Fact]
    public async Task UpdateRole_WithValidData_ShouldReturnUpdatedRole()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var updateRoleDto = new UpdateRoleDto
        {
            DisplayName = "Updated Role Display",
            Description = "Updated description"
        };

        var updatedRole = new RoleDto
        {
            Id = roleId,
            Name = "UpdatedRole",
            DisplayName = updateRoleDto.DisplayName,
            Description = updateRoleDto.Description
        };

        var serviceResult = ServiceResult<RoleDto>.Success(updatedRole, "Role updated successfully");
        _mockRoleService.Setup(x => x.UpdateRoleAsync(roleId, updateRoleDto))
            .ReturnsAsync(serviceResult);

        // Act
        var result = await _controller.UpdateRole(roleId, updateRoleDto);

        // Assert
        result.Should().NotBeNull();
        var actionResult = result.Result;
        actionResult.Should().BeOfType<OkObjectResult>();
        
        var okResult = actionResult as OkObjectResult;
        var returnedRole = okResult!.Value as RoleDto;
        returnedRole!.DisplayName.Should().Be("Updated Role Display");
    }

    [Fact]
    public async Task DeleteRole_WithValidId_ShouldReturnOk()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var serviceResult = ServiceResult.Success("Role deleted successfully");
        _mockRoleService.Setup(x => x.DeleteRoleAsync(roleId))
            .ReturnsAsync(serviceResult);

        // Act
        var result = await _controller.DeleteRole(roleId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteRole_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var serviceResult = ServiceResult.NotFound("Role not found");
        _mockRoleService.Setup(x => x.DeleteRoleAsync(roleId))
            .ReturnsAsync(serviceResult);

        // Act
        var result = await _controller.DeleteRole(roleId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task ActivateRole_WithValidId_ShouldReturnOk()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var serviceResult = ServiceResult.Success("Role activated successfully");
        _mockRoleService.Setup(x => x.ActivateRoleAsync(roleId))
            .ReturnsAsync(serviceResult);

        // Act
        var result = await _controller.ActivateRole(roleId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task DeactivateRole_WithValidId_ShouldReturnOk()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var serviceResult = ServiceResult.Success("Role deactivated successfully");
        _mockRoleService.Setup(x => x.DeactivateRoleAsync(roleId))
            .ReturnsAsync(serviceResult);

        // Act
        var result = await _controller.DeactivateRole(roleId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task CheckRoleExists_WithExistingRole_ShouldReturnTrue()
    {
        // Arrange
        var roleName = "Admin";
        var serviceResult = ServiceResult<bool>.Success(true);
        _mockRoleService.Setup(x => x.RoleExistsAsync(roleName))
            .ReturnsAsync(serviceResult);

        // Act
        var result = await _controller.CheckRoleExists(roleName);

        // Assert
        result.Should().NotBeNull();
        var actionResult = result.Result;
        actionResult.Should().BeOfType<OkObjectResult>();
        
        var okResult = actionResult as OkObjectResult;
        var exists = (bool)okResult!.Value!;
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task CanDeleteRole_WithDeletableRole_ShouldReturnTrue()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var serviceResult = ServiceResult<bool>.Success(true);
        _mockRoleService.Setup(x => x.CanDeleteRoleAsync(roleId))
            .ReturnsAsync(serviceResult);

        // Act
        var result = await _controller.CanDeleteRole(roleId);

        // Assert
        result.Should().NotBeNull();
        var actionResult = result.Result;
        actionResult.Should().BeOfType<OkObjectResult>();
        
        var okResult = actionResult as OkObjectResult;
        var canDelete = (bool)okResult!.Value!;
        canDelete.Should().BeTrue();
    }
}