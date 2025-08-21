using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using XpertSphere.MonolithApi.DTOs.Organization;
using XpertSphere.MonolithApi.Enums;
using XpertSphere.MonolithApi.Models;
using XpertSphere.MonolithApi.Models.Base;
using XpertSphere.MonolithApi.Services;
using XpertSphere.MonolithApi.Tests.Helpers;

namespace XpertSphere.MonolithApi.Tests.Services;

public class OrganizationServiceTests : IDisposable
{
    private readonly Mock<IValidator<CreateOrganizationDto>> _mockCreateValidator;
    private readonly Mock<IValidator<UpdateOrganizationDto>> _mockUpdateValidator;
    private readonly Mock<IValidator<OrganizationFilterDto>> _mockFilterValidator;
    private readonly Mock<ILogger<OrganizationService>> _mockLogger;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly XpertSphere.MonolithApi.Data.XpertSphereDbContext _context;

    public OrganizationServiceTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _mockCreateValidator = new Mock<IValidator<CreateOrganizationDto>>();
        _mockUpdateValidator = new Mock<IValidator<UpdateOrganizationDto>>();
        _mockFilterValidator = new Mock<IValidator<OrganizationFilterDto>>();
        _mockLogger = MockHelper.CreateMockLogger<OrganizationService>();
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
    }

    [Fact]
    public async Task CreateAsync_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var createOrganizationDto = new CreateOrganizationDto
        {
            Name = "Test Organization",
            Code = "TESTORG",
            Description = "A test organization",
            Industry = "Technology",
            Size = OrganizationSize.Medium,
            Address = new Address { Street = "123 Test St", City = "Test City" },
            ContactEmail = "contact@testorg.com",
            ContactPhone = "+1234567890",
            Website = "https://testorg.com"
        };

        _mockCreateValidator.Setup(x => x.ValidateAsync(createOrganizationDto, default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        var mapper = AutoMapperHelper.CreateMapper();
        var organizationService = CreateOrganizationService(mapper);

        // Act
        var result = await organizationService.CreateAsync(createOrganizationDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Name.Should().Be("Test Organization");
        result.Message.Should().Contain("Organization created successfully");
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateName_ShouldReturnConflict()
    {
        // Arrange
        var existingOrganization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Existing Organization",
            Code = "EXISTING",
            Size = OrganizationSize.Small,
            Address = new Address(),
            IsActive = true
        };

        _context.Organizations.Add(existingOrganization);
        await _context.SaveChangesAsync();

        var createOrganizationDto = new CreateOrganizationDto
        {
            Name = "Existing Organization",
            Code = "NEWCODE",
            Size = OrganizationSize.Medium,
            Address = new Address()
        };

        _mockCreateValidator.Setup(x => x.ValidateAsync(createOrganizationDto, default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        var mapper = AutoMapperHelper.CreateMapper();
        var organizationService = CreateOrganizationService(mapper);

        // Act
        var result = await organizationService.CreateAsync(createOrganizationDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("already exists"));
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingOrganization_ShouldReturnSuccess()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var organization = new Organization
        {
            Id = organizationId,
            Name = "Test Organization",
            Code = "TESTORG",
            Size = OrganizationSize.Medium,
            Address = new Address(),
            IsActive = true
        };

        _context.Organizations.Add(organization);
        await _context.SaveChangesAsync();

        var mapper = AutoMapperHelper.CreateMapper();
        var organizationService = CreateOrganizationService(mapper);

        // Act
        var result = await organizationService.GetByIdAsync(organizationId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(organizationId);
        result.Data.Name.Should().Be("Test Organization");
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingOrganization_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();
        var mapper = AutoMapperHelper.CreateMapper();
        var organizationService = CreateOrganizationService(mapper);

        // Act
        var result = await organizationService.GetByIdAsync(nonExistingId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain($"Organization with ID {nonExistingId} not found");
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistingOrganization_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();
        var mapper = AutoMapperHelper.CreateMapper();
        var organizationService = CreateOrganizationService(mapper);

        // Act
        var result = await organizationService.DeleteAsync(nonExistingId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain($"Organization with ID {nonExistingId} not found");
    }

    [Fact]
    public async Task DeleteAsync_WithOrganizationHavingActiveUsers_ShouldReturnFailure()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var organization = new Organization
        {
            Id = organizationId,
            Name = "Test Organization",
            Code = "TESTORG",
            Size = OrganizationSize.Medium,
            Address = new Address(),
            IsActive = true
        };

        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            OrganizationId = organizationId,
            IsActive = true
        };

        _context.Organizations.Add(organization);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var mapper = AutoMapperHelper.CreateMapper();
        var organizationService = CreateOrganizationService(mapper);

        // Act
        var result = await organizationService.DeleteAsync(organizationId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Cannot delete organization that has active users");
    }

    [Fact]
    public async Task GetAllWithoutPaginationAsync_ShouldReturnSuccess()
    {
        // Arrange
        var organization1 = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Organization 1",
            Code = "ORG1",
            Size = OrganizationSize.Small,
            Address = new Address(),
            IsActive = true
        };

        var organization2 = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Organization 2",
            Code = "ORG2",
            Size = OrganizationSize.Large,
            Address = new Address(),
            IsActive = true
        };

        _context.Organizations.AddRange(organization1, organization2);
        await _context.SaveChangesAsync();

        var mapper = AutoMapperHelper.CreateMapper();
        var organizationService = CreateOrganizationService(mapper);

        // Act
        var result = await organizationService.GetAllWithoutPaginationAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Count().Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidData_ShouldReturnValidationError()
    {
        // Arrange
        var createOrganizationDto = new CreateOrganizationDto
        {
            Name = "", // Invalid empty name
            Code = "",
            Size = OrganizationSize.Small,
            Address = new Address()
        };

        var validationErrors = new FluentValidation.Results.ValidationResult(
        [
            new FluentValidation.Results.ValidationFailure("Name", "Name is required"),
            new FluentValidation.Results.ValidationFailure("Code", "Code is required")
        ]);

        _mockCreateValidator.Setup(x => x.ValidateAsync(createOrganizationDto, default))
            .ReturnsAsync(validationErrors);

        var mapper = AutoMapperHelper.CreateMapper();
        var organizationService = CreateOrganizationService(mapper);

        // Act
        var result = await organizationService.CreateAsync(createOrganizationDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Name is required");
        result.Errors.Should().Contain("Code is required");
    }

    [Fact]
    public async Task DeleteAsync_WithOrganizationWithoutActiveUsers_ShouldReturnSuccess()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var organization = new Organization
        {
            Id = organizationId,
            Name = "Test Organization",
            Code = "TESTORG",
            Size = OrganizationSize.Medium,
            Address = new Address(),
            IsActive = true
        };

        _context.Organizations.Add(organization);
        await _context.SaveChangesAsync();

        var mapper = AutoMapperHelper.CreateMapper();
        var organizationService = CreateOrganizationService(mapper);

        // Act
        var result = await organizationService.DeleteAsync(organizationId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Message.Should().Contain("Organization deleted successfully");
    }

    private OrganizationService CreateOrganizationService(AutoMapper.IMapper mapper)
    {
        return new OrganizationService(
            _context,
            mapper,
            _mockCreateValidator.Object,
            _mockUpdateValidator.Object,
            _mockFilterValidator.Object,
            _mockLogger.Object,
            _mockHttpContextAccessor.Object
        );
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}