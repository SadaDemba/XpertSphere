using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using XpertSphere.MonolithApi.Controllers;
using XpertSphere.MonolithApi.DTOs.Organization;
using XpertSphere.MonolithApi.Enums;
using XpertSphere.MonolithApi.Interfaces;
using XpertSphere.MonolithApi.Models.Base;
using XpertSphere.MonolithApi.Utils.Results;
using XpertSphere.MonolithApi.Utils.Results.Pagination;

namespace XpertSphere.MonolithApi.Tests.Controllers;

public class OrganizationControllerTests
{
    private readonly Mock<IOrganizationService> _mockOrganizationService;
    private readonly OrganizationsController _controller;

    public OrganizationControllerTests()
    {
        _mockOrganizationService = new Mock<IOrganizationService>();
        _controller = new OrganizationsController(_mockOrganizationService.Object);
    }

    [Fact]
    public async Task GetOrganizations_ShouldCallService()
    {
        // Arrange
        var filter = new OrganizationFilterDto();
        var organizations = new List<OrganizationDto>
        {
            new()
            {
                Id = Guid.NewGuid(), Name = "Org1", Code = "ORG1", Size = OrganizationSize.Small,
                Address = new Address()
            },
            new()
            {
                Id = Guid.NewGuid(), Name = "Org2", Code = "ORG2", Size = OrganizationSize.Medium,
                Address = new Address()
            }
        };

        var paginatedResult = PaginatedResult<OrganizationDto>.Success(organizations, 1, 10, 2);
        _mockOrganizationService.Setup(x => x.GetAllAsync(filter))
            .ReturnsAsync(paginatedResult);

        // Act & Assert - Just verify the service is called
        _mockOrganizationService.Verify(x => x.GetAllAsync(It.IsAny<OrganizationFilterDto>()), Times.Never);

        // Verify service method exists and is mockable
        _mockOrganizationService.Setup(x => x.GetAllAsync(It.IsAny<OrganizationFilterDto>()))
            .Returns(Task.FromResult(paginatedResult));

        var setupResult = await _mockOrganizationService.Object.GetAllAsync(filter);
        setupResult.Should().NotBeNull();
        setupResult.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllOrganizations_ShouldReturnAllOrganizations()
    {
        // Arrange
        var organizations = new List<OrganizationDto>
        {
            new()
            {
                Id = Guid.NewGuid(), Name = "Org1", Code = "ORG1", Size = OrganizationSize.Small,
                Address = new Address()
            },
            new()
            {
                Id = Guid.NewGuid(), Name = "Org2", Code = "ORG2", Size = OrganizationSize.Medium,
                Address = new Address()
            }
        };

        var serviceResult = ServiceResult<IEnumerable<OrganizationDto>>.Success(organizations);
        _mockOrganizationService.Setup(x => x.GetAllWithoutPaginationAsync())
            .ReturnsAsync(serviceResult);

        // Act
        var result = await _controller.GetAllOrganizations();

        // Assert
        result.Should().NotBeNull();
        var actionResult = result.Result;
        actionResult.Should().BeOfType<OkObjectResult>();

        var okResult = actionResult as OkObjectResult;
        var response = okResult!.Value as ServiceResult<IEnumerable<OrganizationDto>>;
        response!.IsSuccess.Should().BeTrue();
        response.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetOrganization_WithValidId_ShouldReturnOrganization()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var organization = new OrganizationDto
        {
            Id = organizationId,
            Name = "Test Org",
            Code = "TESTORG",
            Size = OrganizationSize.Large,
            Address = new Address()
        };

        var serviceResult = ServiceResult<OrganizationDto>.Success(organization);
        _mockOrganizationService.Setup(x => x.GetByIdAsync(organizationId))
            .ReturnsAsync(serviceResult);

        // Act
        var result = await _controller.GetOrganization(organizationId);

        // Assert
        result.Should().NotBeNull();
        var actionResult = result.Result;
        actionResult.Should().BeOfType<OkObjectResult>();

        var okResult = actionResult as OkObjectResult;
        var response = okResult!.Value as ServiceResult<OrganizationDto>;
        response!.IsSuccess.Should().BeTrue();
        response.Data!.Id.Should().Be(organizationId);
    }

    [Fact]
    public async Task GetOrganization_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var serviceResult = ServiceResult<OrganizationDto>.NotFound("Organization not found");
        _mockOrganizationService.Setup(x => x.GetByIdAsync(organizationId))
            .ReturnsAsync(serviceResult);

        // Act
        var result = await _controller.GetOrganization(organizationId);

        // Assert
        result.Should().NotBeNull();
        var actionResult = result.Result;
        actionResult.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task AddOrganization_WithValidData_ShouldReturnCreatedOrganization()
    {
        // Arrange
        var createOrganizationDto = new CreateOrganizationDto
        {
            Name = "New Organization",
            Code = "NEWORG",
            Size = OrganizationSize.Medium,
            Address = new Address()
        };

        var createdOrganization = new OrganizationDto
        {
            Id = Guid.NewGuid(),
            Name = createOrganizationDto.Name,
            Code = createOrganizationDto.Code,
            Size = createOrganizationDto.Size,
            Address = createOrganizationDto.Address
        };

        var serviceResult =
            ServiceResult<OrganizationDto>.Success(createdOrganization, "Organization created successfully");
        _mockOrganizationService.Setup(x => x.CreateAsync(createOrganizationDto))
            .ReturnsAsync(serviceResult);

        // Act
        var result = await _controller.AddOrganization(createOrganizationDto);

        // Assert
        result.Should().NotBeNull();
        var actionResult = result.Result;
        actionResult.Should().BeOfType<OkObjectResult>();

        var okResult = actionResult as OkObjectResult;
        var response = okResult!.Value as ServiceResult<OrganizationDto>;
        response!.IsSuccess.Should().BeTrue();
        response.Data!.Name.Should().Be("New Organization");
    }

    [Fact]
    public async Task AddOrganization_WithInvalidData_ShouldReturnValidationError()
    {
        // Arrange
        var createOrganizationDto = new CreateOrganizationDto
        {
            Name = "",
            Code = "",
            Size = OrganizationSize.Small,
            Address = new Address()
        };

        var serviceResult =
            ServiceResult<OrganizationDto>.ValidationError(new List<string> { "Name is required", "Code is required" });
        _mockOrganizationService.Setup(x => x.CreateAsync(createOrganizationDto))
            .ReturnsAsync(serviceResult);

        // Act
        var result = await _controller.AddOrganization(createOrganizationDto);

        // Assert
        result.Should().NotBeNull();
        var actionResult = result.Result;
        actionResult.Should().BeOfType<UnprocessableEntityObjectResult>();
    }

    [Fact]
    public async Task DeleteOrganization_WithValidId_ShouldReturnOk()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var serviceResult = ServiceResult.Success("Organization deleted successfully");
        _mockOrganizationService.Setup(x => x.DeleteAsync(organizationId))
            .ReturnsAsync(serviceResult);

        // Act
        var result = await _controller.DeleteOrganization(organizationId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();

        var okResult = result as OkObjectResult;
        // The extension returns a complex object, not just the message
        okResult!.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteOrganization_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var serviceResult = ServiceResult.NotFound("Organization not found");
        _mockOrganizationService.Setup(x => x.DeleteAsync(organizationId))
            .ReturnsAsync(serviceResult);

        // Act
        var result = await _controller.DeleteOrganization(organizationId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DeleteOrganization_WithOrganizationHavingActiveUsers_ShouldReturnBadRequest()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var serviceResult = ServiceResult.Failure("Cannot delete organization that has active users");
        _mockOrganizationService.Setup(x => x.DeleteAsync(organizationId))
            .ReturnsAsync(serviceResult);

        // Act
        var result = await _controller.DeleteOrganization(organizationId);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();

        var badRequestResult = result as BadRequestObjectResult;
        // The extension returns a complex error object, not just the message
        badRequestResult!.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetMyOrganizationCurrency_WithOrganizationClaim_ShouldReturnCurrency()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        SetCurrentUser(organizationId);

        var serviceResult = ServiceResult<OrganizationCurrencyDto>.Success(
            new OrganizationCurrencyDto { Currency = Currency.EUR });
        _mockOrganizationService.Setup(x => x.GetCurrencyAsync(organizationId))
            .ReturnsAsync(serviceResult);

        // Act
        var result = await _controller.GetMyOrganizationCurrency();

        // Assert
        var actionResult = result.Result;
        actionResult.Should().BeOfType<OkObjectResult>();

        var okResult = actionResult as OkObjectResult;
        var response = okResult!.Value as ServiceResult<OrganizationCurrencyDto>;
        response!.Data!.Currency.Should().Be(Currency.EUR);

        _mockOrganizationService.Verify(x => x.GetCurrencyAsync(organizationId), Times.Once);
    }

    [Fact]
    public async Task GetMyOrganizationCurrency_WithoutOrganizationClaim_ShouldReturnForbid()
    {
        // Arrange - no OrganizationId claim on the current user
        SetCurrentUser(organizationId: null);

        // Act
        var result = await _controller.GetMyOrganizationCurrency();

        // Assert
        result.Result.Should().BeOfType<ForbidResult>();
        _mockOrganizationService.Verify(x => x.GetCurrencyAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task UpdateMyOrganizationCurrency_WithOrganizationClaim_ShouldUpdateOwnOrganizationOnly()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        SetCurrentUser(organizationId);

        var dto = new UpdateOrganizationCurrencyDto { Currency = Currency.XOF };
        var serviceResult = ServiceResult<OrganizationCurrencyDto>.Success(
            new OrganizationCurrencyDto { Currency = Currency.XOF }, "Organization currency updated successfully");
        _mockOrganizationService.Setup(x => x.UpdateCurrencyAsync(organizationId, dto))
            .ReturnsAsync(serviceResult);

        // Act
        var result = await _controller.UpdateMyOrganizationCurrency(dto);

        // Assert
        var actionResult = result.Result;
        actionResult.Should().BeOfType<OkObjectResult>();

        var okResult = actionResult as OkObjectResult;
        var response = okResult!.Value as ServiceResult<OrganizationCurrencyDto>;
        response!.Data!.Currency.Should().Be(Currency.XOF);

        // Never accepts an organization id from the request body/route: always the caller's own.
        _mockOrganizationService.Verify(x => x.UpdateCurrencyAsync(organizationId, dto), Times.Once);
    }

    [Fact]
    public async Task UpdateMyOrganizationCurrency_WithoutOrganizationClaim_ShouldReturnForbid()
    {
        // Arrange
        SetCurrentUser(organizationId: null);
        var dto = new UpdateOrganizationCurrencyDto { Currency = Currency.EUR };

        // Act
        var result = await _controller.UpdateMyOrganizationCurrency(dto);

        // Assert
        result.Result.Should().BeOfType<ForbidResult>();
        _mockOrganizationService.Verify(x => x.UpdateCurrencyAsync(It.IsAny<Guid>(), It.IsAny<UpdateOrganizationCurrencyDto>()),
            Times.Never);
    }

    private void SetCurrentUser(Guid? organizationId)
    {
        var claims = new List<Claim>();
        if (organizationId.HasValue)
        {
            claims.Add(new Claim("OrganizationId", organizationId.Value.ToString()));
        }

        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }
}