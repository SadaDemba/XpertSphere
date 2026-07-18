using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using XpertSphere.MonolithApi.Data;
using XpertSphere.MonolithApi.Enums;
using XpertSphere.MonolithApi.Extensions;
using XpertSphere.MonolithApi.Models;
using XpertSphere.MonolithApi.Tests.Helpers;
using XpertSphere.MonolithApi.Utils;

namespace XpertSphere.MonolithApi.Tests.Extensions;

/// <summary>
/// Behavior/idempotence tests for <c>DatabaseExtensions.SeedDemoDataAsync</c> (specification
/// `seed-demo-organizations-users-joboffers.md`), using EF Core InMemory and a stateful
/// <see cref="UserManager{TUser}"/> mock (backed by a plain dictionary, standing in for Identity's
/// real user store) rather than a real SQL Server instance.
///
/// Note: this InMemory setup does not, and cannot, exercise the exact same flush-ordering trap a
/// real relational provider would (queries against added-but-unsaved entities behave differently
/// between providers) -- see specification §Point d'attention. The correctness of the explicit
/// `SaveChangesAsync()` flush after organizations, and the offer/application wiring via in-memory
/// object references rather than requeries, was additionally verified against a real SQL Server
/// instance (first boot then second boot) as part of this implementation; see final report.
/// </summary>
public class DatabaseExtensionsSeedDemoDataTests : IDisposable
{
    private readonly XpertSphereDbContext _context;
    private readonly Mock<UserManager<User>> _mockUserManager;
    private readonly Dictionary<string, User> _usersByEmail = new(StringComparer.OrdinalIgnoreCase);

    public DatabaseExtensionsSeedDemoDataTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext(Guid.NewGuid().ToString());
        _mockUserManager = MockHelper.CreateMockUserManager();

        _mockUserManager
            .Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((string email) => _usersByEmail.GetValueOrDefault(email));

        _mockUserManager
            .Setup(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync((User user, string _) =>
            {
                _usersByEmail[user.Email!] = user;
                return IdentityResult.Success;
            });

        SeedRequiredRoles();
    }

    // SeedDemoDataAsync resolves roles via `context.Roles` queries: seed the 5 roles it needs
    // directly (bypassing SeedDefaultRolesAsync, which is private and not under test here).
    private void SeedRequiredRoles()
    {
        var roleDefinitions = new[]
        {
            Roles.OrganizationAdmin,
            Roles.Manager,
            Roles.Recruiter,
            Roles.TechnicalEvaluator,
            Roles.Candidate
        };

        foreach (var roleDef in roleDefinitions)
        {
            _context.Roles.Add(new Role
            {
                Id = Guid.NewGuid(),
                Name = roleDef.Name,
                DisplayName = roleDef.DisplayName,
                Description = roleDef.Description,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        _context.SaveChanges();
    }

    [Fact]
    public async Task SeedDemoDataAsync_FirstRun_CreatesExpectedCounts()
    {
        await DatabaseExtensions.SeedDemoDataAsync(_context, _mockUserManager.Object);
        await _context.SaveChangesAsync();

        _context.Organizations
            .Count(o => o.Code == "MEILLEURTAUX" || o.Code == "EXPERTIME" || o.Code == "DYNAMINQS")
            .Should().Be(3);

        _usersByEmail.Should().HaveCount(19); // 15 organization users + 4 candidates

        _context.JobOffers.Count().Should().Be(30);
        _context.JobOffers.Count(jo => jo.Status == JobOfferStatus.Published && jo.PublishedAt != null)
            .Should().Be(30);

        _context.Applications.Count().Should().Be(11);
        _context.UserRoles.Count().Should().Be(19);
    }

    [Fact]
    public async Task SeedDemoDataAsync_CalledTwice_IsIdempotent()
    {
        await DatabaseExtensions.SeedDemoDataAsync(_context, _mockUserManager.Object);
        await _context.SaveChangesAsync();

        await DatabaseExtensions.SeedDemoDataAsync(_context, _mockUserManager.Object);
        await _context.SaveChangesAsync();

        _context.Organizations
            .Count(o => o.Code == "MEILLEURTAUX" || o.Code == "EXPERTIME" || o.Code == "DYNAMINQS")
            .Should().Be(3);
        _usersByEmail.Should().HaveCount(19);
        _context.JobOffers.Count().Should().Be(30);
        _context.Applications.Count().Should().Be(11);
        _context.UserRoles.Count().Should().Be(19);
    }

    [Fact]
    public async Task SeedDemoDataAsync_JobOffers_CreatedByUserBelongsToSameOrganization()
    {
        await DatabaseExtensions.SeedDemoDataAsync(_context, _mockUserManager.Object);
        await _context.SaveChangesAsync();

        var jobOffers = await _context.JobOffers.ToListAsync();

        foreach (var offer in jobOffers)
        {
            var creator = _usersByEmail.Values.Single(u => u.Id == offer.CreatedByUserId);
            creator.OrganizationId.Should().Be(offer.OrganizationId,
                $"offer '{offer.Title}' must be created by a user of its own organization, never another organization's user or a candidate");
        }
    }

    [Fact]
    public async Task SeedDemoDataAsync_Applications_HaveNoDuplicateJobOfferCandidatePair()
    {
        await DatabaseExtensions.SeedDemoDataAsync(_context, _mockUserManager.Object);
        await _context.SaveChangesAsync();

        var applications = await _context.Applications.ToListAsync();

        applications.Select(a => (a.JobOfferId, a.CandidateId)).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task SeedDemoDataAsync_Candidates_HaveNoOrganization()
    {
        await DatabaseExtensions.SeedDemoDataAsync(_context, _mockUserManager.Object);
        await _context.SaveChangesAsync();

        foreach (var candidateSeed in DatabaseExtensions.DemoCandidates)
        {
            var candidate = _usersByEmail[candidateSeed.Email];
            candidate.OrganizationId.Should().BeNull();
        }
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
