using FluentAssertions;
using XpertSphere.MonolithApi.Enums;
using XpertSphere.MonolithApi.Extensions;
using XpertSphere.MonolithApi.Utils;

namespace XpertSphere.MonolithApi.Tests.Extensions;

/// <summary>
/// Pure dataset invariant tests for the demo data defined in
/// `Extensions/DatabaseExtensions.DemoData.cs` (specification
/// `seed-demo-organizations-users-joboffers.md`). These do not touch any database: they only
/// assert on the static in-memory collections themselves, so they run instantly and catch data
/// entry mistakes (typos in a referenced title/email, wrong counts, invalid salary ranges, etc.)
/// independently of the persistence/idempotence behavior covered by
/// <see cref="DatabaseExtensionsSeedDemoDataTests"/>.
/// </summary>
public class DatabaseExtensionsDemoDataTests
{
    [Fact]
    public void DemoOrganizations_HasExactlyThreeOrganizations_WithExpectedCodes()
    {
        DatabaseExtensions.DemoOrganizations.Should().HaveCount(3);
        DatabaseExtensions.DemoOrganizations.Select(o => o.Code)
            .Should().BeEquivalentTo(["MEILLEURTAUX", "EXPERTIME", "DYNAMINQS"]);
        DatabaseExtensions.DemoOrganizations.Select(o => o.Code).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void DemoOrganizationUsers_HasFiveUsersPerOrganization_WithExpectedRoleDistribution()
    {
        DatabaseExtensions.DemoOrganizationUsers.Should().HaveCount(15);

        foreach (var org in DatabaseExtensions.DemoOrganizations)
        {
            var usersForOrg = DatabaseExtensions.DemoOrganizationUsers
                .Where(u => u.OrgCode == org.Code)
                .ToList();

            usersForOrg.Should().HaveCount(5, $"organization {org.Code} should have exactly 5 demo users");

            usersForOrg.Count(u => u.RoleName == Roles.OrganizationAdmin.Name).Should().Be(1);
            usersForOrg.Count(u => u.RoleName == Roles.Manager.Name).Should().Be(1);
            usersForOrg.Count(u => u.RoleName == Roles.Recruiter.Name).Should().Be(2);
            usersForOrg.Count(u => u.RoleName == Roles.TechnicalEvaluator.Name).Should().Be(1);
        }
    }

    [Fact]
    public void DemoOrganizationUsers_AllEmailsAreUnique()
    {
        DatabaseExtensions.DemoOrganizationUsers.Select(u => u.Email)
            .Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void DemoJobOffers_HasExactlyThirtyOffers_TenPerOrganization()
    {
        DatabaseExtensions.DemoJobOffers.Should().HaveCount(30);

        foreach (var org in DatabaseExtensions.DemoOrganizations)
        {
            DatabaseExtensions.DemoJobOffers.Count(jo => jo.OrgCode == org.Code).Should().Be(10);
        }
    }

    [Fact]
    public void DemoJobOffers_NoDuplicateTitleWithinTheSameOrganization()
    {
        var duplicates = DatabaseExtensions.DemoJobOffers
            .GroupBy(jo => (jo.OrgCode, jo.Title))
            .Where(g => g.Count() > 1)
            .ToList();

        duplicates.Should().BeEmpty("JobOffer idempotence relies on (Title, OrganizationId) being unique per dataset");
    }

    [Fact]
    public void DemoJobOffers_SalaryMinNeverExceedsSalaryMax()
    {
        foreach (var offer in DatabaseExtensions.DemoJobOffers)
        {
            offer.SalaryMin.Should().BeLessThanOrEqualTo(offer.SalaryMax,
                $"offer '{offer.Title}' ({offer.OrgCode}) must satisfy SalaryMin <= SalaryMax");
        }
    }

    [Fact]
    public void DemoJobOffers_LocationIsSetUnlessFullRemote()
    {
        foreach (var offer in DatabaseExtensions.DemoJobOffers)
        {
            if (offer.WorkMode == WorkMode.FullRemote)
            {
                offer.Location.Should().BeNull(
                    $"offer '{offer.Title}' ({offer.OrgCode}) is FullRemote and should have no Location");
            }
            else
            {
                offer.Location.Should().NotBeNullOrWhiteSpace(
                    $"offer '{offer.Title}' ({offer.OrgCode}) requires a Location (RequiresLocation rule)");
            }
        }
    }

    [Fact]
    public void DemoJobOffers_DescriptionRequirementsBenefitsAreNeverEmpty()
    {
        foreach (var offer in DatabaseExtensions.DemoJobOffers)
        {
            offer.Description.Should().NotBeNullOrWhiteSpace();
            offer.Requirements.Should().NotBeNullOrWhiteSpace();
            offer.Benefits.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void DemoJobOffers_SalaryCurrencyMatchesOrganizationConvention()
    {
        var expectedCurrencyByOrg = new Dictionary<string, string>
        {
            ["MEILLEURTAUX"] = "EUR",
            ["EXPERTIME"] = "EUR",
            ["DYNAMINQS"] = "XOF"
        };

        foreach (var offer in DatabaseExtensions.DemoJobOffers)
        {
            offer.SalaryCurrency.Should().Be(expectedCurrencyByOrg[offer.OrgCode]);
        }
    }

    [Fact]
    public void DemoCandidates_HasExactlyFourCandidates_WithUniqueEmails()
    {
        DatabaseExtensions.DemoCandidates.Should().HaveCount(4);
        DatabaseExtensions.DemoCandidates.Select(c => c.Email).Should().OnlyHaveUniqueItems();
        DatabaseExtensions.DemoCandidates.Select(c => c.Email)
            .Should().OnlyContain(email => email.EndsWith("@candidat-demo.fr"));
    }

    [Fact]
    public void DemoApplications_HasExactlyElevenApplications_WithNoDuplicatePair()
    {
        DatabaseExtensions.DemoApplications.Should().HaveCount(11);

        DatabaseExtensions.DemoApplications
            .Select(a => (a.CandidateEmail, a.OrgCode, a.JobOfferTitle))
            .Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void DemoApplications_ReferenceOnlyExistingCandidatesAndJobOffers()
    {
        var candidateEmails = DatabaseExtensions.DemoCandidates.Select(c => c.Email).ToHashSet();
        var jobOfferKeys = DatabaseExtensions.DemoJobOffers
            .Select(jo => (jo.OrgCode, jo.Title))
            .ToHashSet();

        foreach (var application in DatabaseExtensions.DemoApplications)
        {
            candidateEmails.Should().Contain(application.CandidateEmail,
                $"application references candidate '{application.CandidateEmail}' which must exist in DemoCandidates");

            jobOfferKeys.Should().Contain((application.OrgCode, application.JobOfferTitle),
                $"application references job offer '{application.JobOfferTitle}' ({application.OrgCode}) which must exist in DemoJobOffers");
        }
    }

    [Fact]
    public void DemoApplications_CoverAtLeastThreeDistinctOrganizationsAcrossAllCandidates()
    {
        // Sanity check on the spirit of the dataset (§Candidats de démonstration): candidates are
        // spread across several of the 3 organizations, not siloed one-candidate-per-organization.
        var organizationsCovered = DatabaseExtensions.DemoApplications
            .Select(a => a.OrgCode)
            .Distinct()
            .ToList();

        organizationsCovered.Should().HaveCount(3);
    }
}
