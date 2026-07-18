using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Moq;
using XpertSphere.MonolithApi.Data;
using XpertSphere.MonolithApi.Extensions;
using XpertSphere.MonolithApi.Models;
using XpertSphere.MonolithApi.Tests.Helpers;

namespace XpertSphere.MonolithApi.Tests.Extensions;

/// <summary>
/// Verifies the Development-only gate on the demo dataset (specification
/// `seed-demo-organizations-users-joboffers.md`, §Garde-fou d'environnement): the demo
/// organizations/users/job offers/candidates/applications must only be seeded when
/// <see cref="IWebHostEnvironment.IsDevelopment"/> is true, never in Staging or Production,
/// while the pre-existing structural seed (XpertSphere organization, roles, PlatformSuperAdmin)
/// must keep running regardless of environment (no regression -- criterion 7 and 8).
/// </summary>
public class DatabaseExtensionsEnvironmentGateTests : IDisposable
{
    private readonly XpertSphereDbContext _context;
    private readonly Mock<UserManager<User>> _mockUserManager;
    private readonly IConfiguration _configuration;

    public DatabaseExtensionsEnvironmentGateTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext(Guid.NewGuid().ToString());
        _mockUserManager = MockHelper.CreateMockUserManager();

        _mockUserManager
            .Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        _mockUserManager
            .Setup(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        var configValues = new Dictionary<string, string?>
        {
            ["Seeding:Organization:Name"] = "XpertSphere",
            ["Seeding:Organization:Code"] = "XPERTSPHERE",
            ["Seeding:PlatformSuperAdmin:FirstName"] = "Super",
            ["Seeding:PlatformSuperAdmin:LastName"] = "Admin",
            ["Admin:Email"] = "superadmin@xpertsphere.com",
            ["Admin:Password"] = "Azerty123*Admin"
        };

        _configuration = new ConfigurationBuilder().AddInMemoryCollection(configValues).Build();
    }

    private static Mock<IWebHostEnvironment> CreateMockEnvironment(string environmentName)
    {
        var mockEnvironment = new Mock<IWebHostEnvironment>();
        mockEnvironment.Setup(e => e.EnvironmentName).Returns(environmentName);
        return mockEnvironment;
    }

    [Fact]
    public async Task SeedDatabaseAsync_InDevelopment_SeedsDemoOrganizations()
    {
        var mockEnvironment = CreateMockEnvironment(Environments.Development);

        await DatabaseExtensions.SeedDatabaseAsync(
            _context, _mockUserManager.Object, _configuration, mockEnvironment.Object);

        _context.Organizations
            .Count(o => o.Code == "MEILLEURTAUX" || o.Code == "EXPERTIME" || o.Code == "DYNAMINQS")
            .Should().Be(3);
        _context.JobOffers.Count().Should().Be(30);
    }

    [Theory]
    [InlineData("Staging")]
    [InlineData("Production")]
    public async Task SeedDatabaseAsync_OutsideDevelopment_NeverSeedsDemoOrganizations(string environmentName)
    {
        var mockEnvironment = CreateMockEnvironment(environmentName);

        await DatabaseExtensions.SeedDatabaseAsync(
            _context, _mockUserManager.Object, _configuration, mockEnvironment.Object);

        _context.Organizations
            .Count(o => o.Code == "MEILLEURTAUX" || o.Code == "EXPERTIME" || o.Code == "DYNAMINQS")
            .Should().Be(0);
        _context.JobOffers.Count().Should().Be(0);
        _context.Applications.Count().Should().Be(0);
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Staging")]
    [InlineData("Production")]
    public async Task SeedDatabaseAsync_AnyEnvironment_StillSeedsXpertSphereOrganizationAndRoles(
        string environmentName)
    {
        // No regression on the pre-existing structural seed (criterion 8): it must keep running
        // identically regardless of environment, demo dataset or not.
        var mockEnvironment = CreateMockEnvironment(environmentName);

        await DatabaseExtensions.SeedDatabaseAsync(
            _context, _mockUserManager.Object, _configuration, mockEnvironment.Object);

        _context.Organizations.Count(o => o.Name == "XpertSphere").Should().Be(1);
        _context.Roles.Count().Should().Be(7);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
