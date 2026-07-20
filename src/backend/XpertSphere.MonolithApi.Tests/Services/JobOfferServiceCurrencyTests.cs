using AutoMapper;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using XpertSphere.MonolithApi.Data;
using XpertSphere.MonolithApi.DTOs.JobOffer;
using XpertSphere.MonolithApi.Enums;
using XpertSphere.MonolithApi.Interfaces;
using XpertSphere.MonolithApi.Mappings;
using XpertSphere.MonolithApi.Models;
using XpertSphere.MonolithApi.Services;
using XpertSphere.MonolithApi.Tests.Helpers;

namespace XpertSphere.MonolithApi.Tests.Services;

/// <summary>
/// Covers .claude/specifications/configurable-salary-currency.md: <see cref="JobOfferService.CreateJobOfferAsync"/>
/// must stamp <c>JobOffer.SalaryCurrency</c> from <c>Organization.Currency</c> at creation time (a snapshot,
/// never recomputed afterwards), falling back silently to <see cref="Currency.XOF"/> when the organization has
/// not configured a currency yet.
///
/// Uses a real <see cref="MapperConfiguration"/> (production profiles) rather than the mocked
/// <see cref="AutoMapperHelper"/>, which has no CreateJobOfferDto -&gt; JobOffer mapping configured.
/// </summary>
public class JobOfferServiceCurrencyTests : IDisposable
{
    private readonly XpertSphereDbContext _context;
    private readonly IMapper _mapper;
    private readonly Mock<IValidator<CreateJobOfferDto>> _mockCreateValidator;
    private readonly Mock<IValidator<UpdateJobOfferDto>> _mockUpdateValidator;
    private readonly Mock<IValidator<JobOfferFilterDto>> _mockFilterValidator;
    private readonly Mock<ILogger<JobOfferService>> _mockLogger;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;

    public JobOfferServiceCurrencyTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext(Guid.NewGuid().ToString());
        _mapper = new MapperConfiguration(cfg => cfg.AddMaps(typeof(JobOfferMappingProfile).Assembly),
            NullLoggerFactory.Instance).CreateMapper();

        _mockCreateValidator = new Mock<IValidator<CreateJobOfferDto>>();
        _mockCreateValidator.Setup(x => x.ValidateAsync(It.IsAny<CreateJobOfferDto>(), default))
            .ReturnsAsync(new ValidationResult());

        _mockUpdateValidator = new Mock<IValidator<UpdateJobOfferDto>>();
        _mockFilterValidator = new Mock<IValidator<JobOfferFilterDto>>();
        _mockLogger = new Mock<ILogger<JobOfferService>>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
    }

    private static CreateJobOfferDto BuildValidCreateDto() => new()
    {
        Title = "Développeur backend",
        Description = "Description de l'offre",
        Requirements = "Exigences de l'offre",
        Benefits = "Avantages",
        Location = "Paris",
        WorkMode = WorkMode.OnSite,
        ContractType = ContractType.FullTime
    };

    private async Task<(Organization Organization, User User)> SeedOrganizationAndUserAsync(Currency? organizationCurrency)
    {
        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Test Organization",
            Code = "TESTORG",
            Currency = organizationCurrency
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Jane",
            LastName = "Doe",
            Email = "jane.doe@testorg.fr",
            UserName = "jane.doe@testorg.fr",
            OrganizationId = organization.Id
        };

        _context.Organizations.Add(organization);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return (organization, user);
    }

    [Fact]
    public async Task CreateJobOfferAsync_WithOrganizationCurrencyConfigured_ShouldStampThatCurrency()
    {
        // Arrange
        var (organization, user) = await SeedOrganizationAndUserAsync(Currency.XOF);
        var service = CreateService();

        // Act
        var result = await service.CreateJobOfferAsync(BuildValidCreateDto(), user.Id, organization.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.SalaryCurrency.Should().Be(Currency.XOF);
    }

    [Fact]
    public async Task CreateJobOfferAsync_WithoutOrganizationCurrencyConfigured_ShouldFallBackToXof()
    {
        // Arrange - organization has never configured its currency (decision 3: silent fallback, no blocking)
        var (organization, user) = await SeedOrganizationAndUserAsync(organizationCurrency: null);
        var service = CreateService();

        // Act
        var result = await service.CreateJobOfferAsync(BuildValidCreateDto(), user.Id, organization.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.SalaryCurrency.Should().Be(Currency.XOF);
    }

    [Fact]
    public async Task CreateJobOfferAsync_IgnoresAnySalaryCurrencySentByTheClient()
    {
        // Arrange - CreateJobOfferDto no longer exposes SalaryCurrency at all: even if a legacy
        // client sent one in raw JSON, model binding would silently drop it before it reaches the
        // service. The organization's configured currency (EUR here) always wins.
        var (organization, user) = await SeedOrganizationAndUserAsync(Currency.EUR);
        var service = CreateService();

        // Act
        var result = await service.CreateJobOfferAsync(BuildValidCreateDto(), user.Id, organization.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.SalaryCurrency.Should().Be(Currency.EUR);
    }

    [Fact]
    public async Task UpdateJobOfferAsync_AfterOrganizationCurrencyChanges_ShouldNotChangeExistingOfferCurrency()
    {
        // Arrange - offer created while the organization was EUR
        var (organization, user) = await SeedOrganizationAndUserAsync(Currency.EUR);
        var service = CreateService();
        var createResult = await service.CreateJobOfferAsync(BuildValidCreateDto(), user.Id, organization.Id);
        createResult.IsSuccess.Should().BeTrue();

        // Organization currency changes afterwards
        organization.Currency = Currency.XOF;
        await _context.SaveChangesAsync();

        _mockUpdateValidator.Setup(x => x.ValidateAsync(It.IsAny<UpdateJobOfferDto>(), default))
            .ReturnsAsync(new ValidationResult());

        // Act - updating an unrelated field must not recompute SalaryCurrency
        var updateResult = await service.UpdateJobOfferAsync(createResult.Data!.Id,
            new UpdateJobOfferDto { Title = "Développeur backend senior" }, user.Id);

        // Assert
        updateResult.IsSuccess.Should().BeTrue();
        updateResult.Data!.SalaryCurrency.Should().Be(Currency.EUR);
    }

    private JobOfferService CreateService()
    {
        return new JobOfferService(
            _context,
            _mapper,
            _mockCreateValidator.Object,
            _mockUpdateValidator.Object,
            _mockFilterValidator.Object,
            _mockLogger.Object,
            _mockCurrentUserService.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
