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

    // -----------------------------------------------------------------
    // Candidate profile enrichment (specification `enrich-seed-candidate-profiles.md`)
    // -----------------------------------------------------------------

    [Fact]
    public async Task SeedDemoDataAsync_FirstRun_EnrichesCandidateProfiles_MatchingSpecificationTables()
    {
        // AC1: on a fresh database (first boot), the 4 demo candidates get their scalar fields,
        // address, experiences/trainings and profile completion enriched as described by the
        // specification tables.
        await DatabaseExtensions.SeedDemoDataAsync(_context, _mockUserManager.Object);
        await _context.SaveChangesAsync();

        _context.Experiences.Count().Should().Be(8); // 2 per candidate x 4 candidates
        _context.Trainings.Count().Should().Be(4); // 1 per candidate x 4 candidates

        foreach (var seed in DatabaseExtensions.DemoCandidates)
        {
            var candidate = _usersByEmail[seed.Email];

            candidate.PhoneNumber.Should().Be(seed.PhoneNumber);
            candidate.LinkedInProfile.Should().Be(seed.LinkedInProfile);
            candidate.Skills.Should().Be(seed.Skills);
            candidate.YearsOfExperience.Should().Be(seed.YearsOfExperience);
            candidate.DesiredSalary.Should().Be(seed.DesiredSalary);
            candidate.DesiredSalaryCurrency.Should().Be(seed.DesiredSalaryCurrency);
            candidate.Availability.Should().NotBeNull();
            candidate.Address.IsEmpty.Should().BeFalse();
            candidate.Address.StreetNumber.Should().Be(seed.Address.StreetNumber);
            candidate.Address.StreetName.Should().Be(seed.Address.StreetName);
            candidate.Address.City.Should().Be(seed.Address.City);
            candidate.Address.PostalCode.Should().Be(seed.Address.PostalCode);
            candidate.Address.Country.Should().Be(seed.Address.Country);
            candidate.CvPath.Should().BeNull(); // AC5: never touched by this seed
            candidate.ProfileCompletionPercentage.Should().Be(91);

            var experiences = _context.Experiences.Where(e => e.UserId == candidate.Id).ToList();
            var trainings = _context.Trainings.Where(t => t.UserId == candidate.Id).ToList();
            experiences.Should().HaveCount(2);
            trainings.Should().HaveCount(1);
        }
    }

    [Fact]
    public async Task SeedDemoDataAsync_CalledTwice_DoesNotDuplicateExperiencesOrTrainings_AndKeepsProfileCompletionAt91()
    {
        // AC3: a second consecutive boot must not add any extra Experience/Training row, and
        // ProfileCompletionPercentage must not regress below 91 (the exact silent regression
        // warned about by specification §Point d'attention: unloaded navigation collections would
        // make CalculateProfileCompletion() see empty Experiences/Trainings on the 2nd boot).
        await DatabaseExtensions.SeedDemoDataAsync(_context, _mockUserManager.Object);
        await _context.SaveChangesAsync();

        await DatabaseExtensions.SeedDemoDataAsync(_context, _mockUserManager.Object);
        await _context.SaveChangesAsync();

        _context.Experiences.Count().Should().Be(8);
        _context.Trainings.Count().Should().Be(4);

        foreach (var seed in DatabaseExtensions.DemoCandidates)
        {
            var candidate = _usersByEmail[seed.Email];
            candidate.ProfileCompletionPercentage.Should().Be(91);

            _context.Experiences.Count(e => e.UserId == candidate.Id).Should().Be(2);
            _context.Trainings.Count(t => t.UserId == candidate.Id).Should().Be(1);
        }
    }

    [Fact]
    public async Task SeedDemoDataAsync_RetroactivelyEnrichesAPreexistingCandidateWithAnEmptyProfile()
    {
        // AC2: a candidate created by a prior version of the seed (before this correctif), with
        // all these fields still empty, gets retroactively enriched on the next API restart --
        // not only on a brand-new database.
        var preexistingCandidate = DatabaseExtensions.DemoCandidates[0];
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = preexistingCandidate.FirstName,
            LastName = preexistingCandidate.LastName,
            Email = preexistingCandidate.Email,
            UserName = preexistingCandidate.Email,
            EmailConfirmed = true,
            OrganizationId = null,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            ConsentGivenAt = DateTime.UtcNow
        };
        _usersByEmail[preexistingCandidate.Email] = user;

        await DatabaseExtensions.SeedDemoDataAsync(_context, _mockUserManager.Object);
        await _context.SaveChangesAsync();

        var enrichedCandidate = _usersByEmail[preexistingCandidate.Email];
        enrichedCandidate.PhoneNumber.Should().Be(preexistingCandidate.PhoneNumber);
        enrichedCandidate.Address.IsEmpty.Should().BeFalse();
        enrichedCandidate.ProfileCompletionPercentage.Should().Be(91);
        _context.Experiences.Count(e => e.UserId == enrichedCandidate.Id).Should().Be(2);
        _context.Trainings.Count(t => t.UserId == enrichedCandidate.Id).Should().Be(1);

        // The pre-existing candidate count assertion from SeedDemoDataAsync_FirstRun_CreatesExpectedCounts
        // must still hold: pre-inserting this one candidate does not change the total.
        _usersByEmail.Should().HaveCount(19);
    }

    [Fact]
    public async Task SeedDemoDataAsync_NeverOverwritesAManuallyEditedScalarField()
    {
        // AC4: a field already filled in (by this seed on a previous boot, or manually by the
        // candidate via candidate-app's EditProfileDialog.vue) must never be overwritten by a
        // subsequent boot.
        var seed = DatabaseExtensions.DemoCandidates[2]; // Léa Dupont (EUR candidate)
        var manuallyEditedSkills = "Compétence modifiée manuellement par le candidat";
        var manuallyEditedSalary = 99999m;

        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = seed.FirstName,
            LastName = seed.LastName,
            Email = seed.Email,
            UserName = seed.Email,
            EmailConfirmed = true,
            OrganizationId = null,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            ConsentGivenAt = DateTime.UtcNow,
            Skills = manuallyEditedSkills,
            DesiredSalary = manuallyEditedSalary,
            DesiredSalaryCurrency = Currency.XOF // deliberately not the seed's EUR either
        };
        _usersByEmail[seed.Email] = user;

        await DatabaseExtensions.SeedDemoDataAsync(_context, _mockUserManager.Object);
        await _context.SaveChangesAsync();

        var candidate = _usersByEmail[seed.Email];
        candidate.Skills.Should().Be(manuallyEditedSkills);
        candidate.DesiredSalary.Should().Be(manuallyEditedSalary);
        candidate.DesiredSalaryCurrency.Should().Be(Currency.XOF);
    }

    [Fact]
    public async Task SeedDemoDataAsync_NeverTouchesCvPath()
    {
        // AC5: CvPath stays null for all 4 candidates after this correctif (upload of CV is
        // explicitly out of scope).
        await DatabaseExtensions.SeedDemoDataAsync(_context, _mockUserManager.Object);
        await _context.SaveChangesAsync();

        foreach (var seed in DatabaseExtensions.DemoCandidates)
        {
            _usersByEmail[seed.Email].CvPath.Should().BeNull();
        }
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
