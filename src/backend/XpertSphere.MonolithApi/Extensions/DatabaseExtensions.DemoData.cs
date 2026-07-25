using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using XpertSphere.MonolithApi.Data;
using XpertSphere.MonolithApi.Enums;
using XpertSphere.MonolithApi.Models;
using XpertSphere.MonolithApi.Models.Base;
using XpertSphere.MonolithApi.Utils;

namespace XpertSphere.MonolithApi.Extensions;

/// <summary>
/// Demo dataset seeded only in Development (see <c>SeedDatabaseAsync</c> in DatabaseExtensions.cs),
/// as described by specification `seed-demo-organizations-users-joboffers.md`: 3 demo client
/// organizations, their user roster, 30 job offers, 4 candidates and their applications.
///
/// Kept in its own partial class file, separate from the structural seed
/// (DatabaseExtensions.cs), purely because of the volume of inline data (job offer texts).
/// </summary>
public static partial class DatabaseExtensions
{
    internal const string DemoPassword = "Azerty123*";

    // ---------------------------------------------------------------------
    // Orchestration
    // ---------------------------------------------------------------------

    // internal (rather than private): exercised directly by
    // XpertSphere.MonolithApi.Tests (InternalsVisibleTo) to verify idempotence and entity counts
    // without booting the full application/SQL Server.
    internal static async Task SeedDemoDataAsync(XpertSphereDbContext context, UserManager<User> userManager)
    {
        // Step 1: organizations, flushed immediately so that the roles/XpertSphere organization
        // added earlier in SeedDatabaseAsync (and still only tracked in memory on a first boot)
        // are actually persisted before the queries below run against the database.
        var organizationsByCode = await SeedDemoOrganizationsAsync(context);

        var roleNames = new[]
        {
            Roles.OrganizationAdmin.Name,
            Roles.Manager.Name,
            Roles.Recruiter.Name,
            Roles.TechnicalEvaluator.Name,
            Roles.Candidate.Name
        };
        var rolesByName = await context.Roles
            .Where(r => roleNames.Contains(r.Name))
            .ToDictionaryAsync(r => r.Name);

        // Step 2: organization users (+ role assignment).
        var organizationUsersByEmail =
            await SeedDemoOrganizationUsersAsync(context, userManager, organizationsByCode, rolesByName);

        // Step 3: job offers.
        var jobOffersByKey = await SeedDemoJobOffersAsync(context, organizationsByCode, organizationUsersByEmail);

        // Step 4: candidates (+ role assignment).
        var candidatesByEmail = await SeedDemoCandidatesAsync(context, userManager, rolesByName);

        // Step 5: applications.
        await SeedDemoApplicationsAsync(context, jobOffersByKey, candidatesByEmail);
    }

    // ---------------------------------------------------------------------
    // Step 1: Organizations
    // ---------------------------------------------------------------------

    private static async Task<Dictionary<string, Organization>> SeedDemoOrganizationsAsync(
        XpertSphereDbContext context)
    {
        var organizationsByCode = new Dictionary<string, Organization>();

        foreach (var seed in DemoOrganizations)
        {
            var organization = await context.Organizations.FirstOrDefaultAsync(o => o.Code == seed.Code);

            if (organization == null)
            {
                organization = new Organization
                {
                    Id = Guid.NewGuid(),
                    Name = seed.Name,
                    Code = seed.Code,
                    Industry = seed.Industry,
                    Size = seed.Size,
                    Website = seed.Website,
                    ContactEmail = seed.ContactEmail,
                    ContactPhone = seed.ContactPhone,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    Address = new Address
                    {
                        City = seed.City,
                        Country = seed.Country
                    }
                };

                context.Organizations.Add(organization);
                Console.WriteLine($"Demo organization '{seed.Code}' seeded successfully");
            }
            else
            {
                Console.WriteLine($"Demo organization '{seed.Code}' already exists");
            }

            organizationsByCode[seed.Code] = organization;
        }

        // Explicit flush required here: on a first boot against an empty database, the 7 roles
        // and the XpertSphere organization added earlier in SeedDatabaseAsync are only tracked in
        // memory at this point (the only other SaveChangesAsync happens at the very end). Without
        // this flush, the role lookups performed right after this method returns
        // (SeedDemoOrganizationUsersAsync, SeedDemoCandidatesAsync) would query the database and
        // find nothing, exactly like the latent SeedPlatformSuperAdminAsync bug on a first boot.
        // See specification `seed-demo-organizations-users-joboffers.md`, §Point d'attention.
        await context.SaveChangesAsync();

        return organizationsByCode;
    }

    // ---------------------------------------------------------------------
    // Step 2: Organization users + role assignment
    // ---------------------------------------------------------------------

    private static async Task<Dictionary<string, User>> SeedDemoOrganizationUsersAsync(
        XpertSphereDbContext context,
        UserManager<User> userManager,
        IReadOnlyDictionary<string, Organization> organizationsByCode,
        IReadOnlyDictionary<string, Role> rolesByName)
    {
        var usersByEmail = new Dictionary<string, User>(StringComparer.OrdinalIgnoreCase);

        foreach (var seed in DemoOrganizationUsers)
        {
            var organization = organizationsByCode[seed.OrgCode];
            var role = rolesByName[seed.RoleName];

            var user = await userManager.FindByEmailAsync(seed.Email);

            if (user == null)
            {
                user = new User
                {
                    Id = Guid.NewGuid(),
                    FirstName = seed.FirstName,
                    LastName = seed.LastName,
                    Email = seed.Email,
                    UserName = seed.Email,
                    EmailConfirmed = true,
                    OrganizationId = organization.Id,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    ConsentGivenAt = DateTime.UtcNow
                    // EmployeeId intentionally left null, consistent with the existing
                    // PlatformSuperAdmin seed: no IValidator<TDto> is invoked by this seed.
                };

                var result = await userManager.CreateAsync(user, DemoPassword);

                if (!result.Succeeded)
                {
                    Console.WriteLine(
                        $"Failed to create demo organization user '{seed.Email}': " +
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                    continue;
                }

                Console.WriteLine($"Demo organization user '{seed.Email}' seeded successfully");
            }
            else
            {
                Console.WriteLine($"Demo organization user '{seed.Email}' already exists");
            }

            usersByEmail[seed.Email] = user;

            var existingUserRole = await context.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id);

            if (existingUserRole == null)
            {
                context.UserRoles.Add(new UserRole
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    RoleId = role.Id,
                    IsActive = true,
                    AssignedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                });
                Console.WriteLine($"Role '{seed.RoleName}' assigned to demo user '{seed.Email}'");
            }
        }

        return usersByEmail;
    }

    // ---------------------------------------------------------------------
    // Step 3: Job offers
    // ---------------------------------------------------------------------

    private static async Task<Dictionary<(string OrgCode, string Title), JobOffer>> SeedDemoJobOffersAsync(
        XpertSphereDbContext context,
        IReadOnlyDictionary<string, Organization> organizationsByCode,
        IReadOnlyDictionary<string, User> organizationUsersByEmail)
    {
        var jobOffersByKey = new Dictionary<(string, string), JobOffer>();

        // Round-robin between the two Organization.Recruiter of each organization: offers 1, 3, 5,
        // 7, 9 (within an organization) are created by the first recruiter listed for that
        // organization in DemoOrganizationUsers, offers 2, 4, 6, 8, 10 by the second.
        var recruiterEmailsByOrg = DemoOrganizationUsers
            .Where(u => u.RoleName == Roles.Recruiter.Name)
            .GroupBy(u => u.OrgCode)
            .ToDictionary(g => g.Key, g => g.Select(u => u.Email).ToArray());

        var offerIndexPerOrg = new Dictionary<string, int>();

        foreach (var seed in DemoJobOffers)
        {
            var organization = organizationsByCode[seed.OrgCode];

            var offerIndex = offerIndexPerOrg.GetValueOrDefault(seed.OrgCode, 0);
            var recruiterEmails = recruiterEmailsByOrg[seed.OrgCode];
            var creatorEmail = recruiterEmails[offerIndex % recruiterEmails.Length];
            offerIndexPerOrg[seed.OrgCode] = offerIndex + 1;

            var creator = organizationUsersByEmail[creatorEmail];

            var jobOffer = await context.JobOffers
                .FirstOrDefaultAsync(jo => jo.Title == seed.Title && jo.OrganizationId == organization.Id);

            if (jobOffer == null)
            {
                jobOffer = new JobOffer
                {
                    Id = Guid.NewGuid(),
                    Title = seed.Title,
                    Description = seed.Description,
                    Requirements = seed.Requirements,
                    Benefits = seed.Benefits,
                    Location = seed.Location,
                    WorkMode = seed.WorkMode,
                    ContractType = seed.ContractType,
                    SalaryMin = seed.SalaryMin,
                    SalaryMax = seed.SalaryMax,
                    SalaryCurrency = seed.SalaryCurrency,
                    Status = JobOfferStatus.Published,
                    PublishedAt = DateTime.UtcNow,
                    ExpiresAt = null,
                    OrganizationId = organization.Id,
                    CreatedByUserId = creator.Id,
                    CreatedAt = DateTime.UtcNow
                };

                context.JobOffers.Add(jobOffer);
                Console.WriteLine($"Demo job offer '{seed.Title}' ({seed.OrgCode}) seeded successfully");
            }
            else
            {
                Console.WriteLine($"Demo job offer '{seed.Title}' ({seed.OrgCode}) already exists");
            }

            jobOffersByKey[(seed.OrgCode, seed.Title)] = jobOffer;
        }

        return jobOffersByKey;
    }

    // ---------------------------------------------------------------------
    // Step 4: Candidates + role assignment
    // ---------------------------------------------------------------------

    private static async Task<Dictionary<string, User>> SeedDemoCandidatesAsync(
        XpertSphereDbContext context,
        UserManager<User> userManager,
        IReadOnlyDictionary<string, Role> rolesByName)
    {
        var candidatesByEmail = new Dictionary<string, User>(StringComparer.OrdinalIgnoreCase);
        var candidateRole = rolesByName[Roles.Candidate.Name];

        foreach (var seed in DemoCandidates)
        {
            var user = await userManager.FindByEmailAsync(seed.Email);

            if (user == null)
            {
                user = new User
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
                    ConsentGivenAt = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(user, DemoPassword);

                if (!result.Succeeded)
                {
                    Console.WriteLine(
                        $"Failed to create demo candidate '{seed.Email}': " +
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                    continue;
                }

                Console.WriteLine($"Demo candidate '{seed.Email}' seeded successfully");
            }
            else
            {
                Console.WriteLine($"Demo candidate '{seed.Email}' already exists");
            }

            candidatesByEmail[seed.Email] = user;

            var existingUserRole = await context.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == user.Id && ur.RoleId == candidateRole.Id);

            if (existingUserRole == null)
            {
                context.UserRoles.Add(new UserRole
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    RoleId = candidateRole.Id,
                    IsActive = true,
                    AssignedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                });
                Console.WriteLine($"Role '{Roles.Candidate.Name}' assigned to demo candidate '{seed.Email}'");
            }
        }

        return candidatesByEmail;
    }

    // ---------------------------------------------------------------------
    // Step 5: Applications
    // ---------------------------------------------------------------------

    private static async Task SeedDemoApplicationsAsync(
        XpertSphereDbContext context,
        IReadOnlyDictionary<(string OrgCode, string Title), JobOffer> jobOffersByKey,
        IReadOnlyDictionary<string, User> candidatesByEmail)
    {
        foreach (var seed in DemoApplications)
        {
            if (!jobOffersByKey.TryGetValue((seed.OrgCode, seed.JobOfferTitle), out var jobOffer))
            {
                Console.WriteLine(
                    $"Demo job offer '{seed.JobOfferTitle}' ({seed.OrgCode}) not found. " +
                    $"Skipping demo application for '{seed.CandidateEmail}'");
                continue;
            }

            if (!candidatesByEmail.TryGetValue(seed.CandidateEmail, out var candidate))
            {
                Console.WriteLine(
                    $"Demo candidate '{seed.CandidateEmail}' not found. Skipping demo application");
                continue;
            }

            var existingApplication = await context.Applications
                .FirstOrDefaultAsync(a => a.JobOfferId == jobOffer.Id && a.CandidateId == candidate.Id);

            if (existingApplication == null)
            {
                context.Applications.Add(new Application
                {
                    Id = Guid.NewGuid(),
                    JobOfferId = jobOffer.Id,
                    CandidateId = candidate.Id,
                    CurrentStatus = ApplicationStatus.Applied,
                    AppliedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                });
                Console.WriteLine(
                    $"Demo application seeded: '{seed.CandidateEmail}' -> '{seed.JobOfferTitle}' ({seed.OrgCode})");
            }
            else
            {
                Console.WriteLine(
                    $"Demo application already exists: '{seed.CandidateEmail}' -> '{seed.JobOfferTitle}' ({seed.OrgCode})");
            }
        }
    }

    // ---------------------------------------------------------------------
    // Data: Organizations
    // ---------------------------------------------------------------------

    internal sealed record DemoOrganizationSeed(
        string Code,
        string Name,
        string Industry,
        OrganizationSize Size,
        string Website,
        string ContactEmail,
        string ContactPhone,
        string City,
        string Country);

    internal static readonly DemoOrganizationSeed[] DemoOrganizations =
    [
        new DemoOrganizationSeed(
            "MEILLEURTAUX",
            "Meilleurtaux",
            "Courtage en crédits et assurances",
            OrganizationSize.Large,
            "https://www.meilleurtaux.com",
            "contact@meilleurtaux-demo.fr",
            "+33140506070",
            "Paris",
            "France"),
        new DemoOrganizationSeed(
            "EXPERTIME",
            "Expertime",
            "Conseil en transformation digitale",
            OrganizationSize.Medium,
            "https://expertime.com",
            "contact@expertime-demo.fr",
            "+33140506080",
            "Paris",
            "France"),
        new DemoOrganizationSeed(
            "DYNAMINQS",
            "Dynaminqs",
            "Conseil et intégration Microsoft Dynamics 365 / Power Platform",
            OrganizationSize.Small,
            "https://www.dynaminqs.com",
            "contact@dynaminqs-demo.fr",
            "+221338000000",
            "Dakar",
            "Sénégal")
    ];

    // ---------------------------------------------------------------------
    // Data: Organization users (5 per organization, 15 total)
    // ---------------------------------------------------------------------

    internal sealed record DemoOrgUserSeed(string OrgCode, string FirstName, string LastName, string Email, string RoleName);

    internal static readonly DemoOrgUserSeed[] DemoOrganizationUsers =
    [
        // Meilleurtaux
        new DemoOrgUserSeed("MEILLEURTAUX", "Sophie", "Lambert", "sophie.lambert@meilleurtaux-demo.fr", Roles.OrganizationAdmin.Name),
        new DemoOrgUserSeed("MEILLEURTAUX", "Julien", "Moreau", "julien.moreau@meilleurtaux-demo.fr", Roles.Manager.Name),
        new DemoOrgUserSeed("MEILLEURTAUX", "Camille", "Girard", "camille.girard@meilleurtaux-demo.fr", Roles.Recruiter.Name),
        new DemoOrgUserSeed("MEILLEURTAUX", "Nicolas", "Petit", "nicolas.petit@meilleurtaux-demo.fr", Roles.Recruiter.Name),
        new DemoOrgUserSeed("MEILLEURTAUX", "Aurélie", "Dubois", "aurelie.dubois@meilleurtaux-demo.fr", Roles.TechnicalEvaluator.Name),

        // Expertime
        new DemoOrgUserSeed("EXPERTIME", "Thomas", "Bernard", "thomas.bernard@expertime-demo.fr", Roles.OrganizationAdmin.Name),
        new DemoOrgUserSeed("EXPERTIME", "Claire", "Rousseau", "claire.rousseau@expertime-demo.fr", Roles.Manager.Name),
        new DemoOrgUserSeed("EXPERTIME", "Mehdi", "Benali", "mehdi.benali@expertime-demo.fr", Roles.Recruiter.Name),
        new DemoOrgUserSeed("EXPERTIME", "Laura", "Fontaine", "laura.fontaine@expertime-demo.fr", Roles.Recruiter.Name),
        new DemoOrgUserSeed("EXPERTIME", "Antoine", "Leroy", "antoine.leroy@expertime-demo.fr", Roles.TechnicalEvaluator.Name),

        // Dynaminqs
        new DemoOrgUserSeed("DYNAMINQS", "Fatou", "Ndiaye", "fatou.ndiaye@dynaminqs-demo.fr", Roles.OrganizationAdmin.Name),
        new DemoOrgUserSeed("DYNAMINQS", "Moussa", "Diop", "moussa.diop@dynaminqs-demo.fr", Roles.Manager.Name),
        new DemoOrgUserSeed("DYNAMINQS", "Awa", "Sarr", "awa.sarr@dynaminqs-demo.fr", Roles.Recruiter.Name),
        new DemoOrgUserSeed("DYNAMINQS", "Ibrahima", "Fall", "ibrahima.fall@dynaminqs-demo.fr", Roles.Recruiter.Name),
        new DemoOrgUserSeed("DYNAMINQS", "Khadija", "Sy", "khadija.sy@dynaminqs-demo.fr", Roles.TechnicalEvaluator.Name)
    ];

    // ---------------------------------------------------------------------
    // Data: Job offers (10 per organization, 30 total)
    // ---------------------------------------------------------------------

    internal sealed record DemoJobOfferSeed(
        string OrgCode,
        string Title,
        string Description,
        string Requirements,
        string Benefits,
        ContractType ContractType,
        WorkMode WorkMode,
        string? Location,
        decimal SalaryMin,
        decimal SalaryMax,
        Currency SalaryCurrency);

    internal static readonly DemoJobOfferSeed[] DemoJobOffers =
    [
        // ------------------------------------------------------------
        // Meilleurtaux (EUR)
        // ------------------------------------------------------------
        new DemoJobOfferSeed(
            "MEILLEURTAUX",
            "Conseiller(ère) en crédit immobilier",
            "Au sein de notre agence de Nantes, vous accompagnez une clientèle de particuliers dans leurs projets d'acquisition immobilière. Vous analysez leur situation financière, comparez les offres de nos partenaires bancaires et négociez les meilleures conditions de taux et d'assurance emprunteur.",
            "Formation Bac+2/3 en banque, finance ou commerce. Une première expérience en courtage ou en agence bancaire est appréciée mais non exigée pour les profils juniors motivés. Aisance relationnelle et goût du conseil client indispensables.",
            "Rémunération fixe + variable sur objectifs, mutuelle d'entreprise, tickets restaurant, parcours de formation interne aux produits de crédit et d'assurance.",
            ContractType.FullTime, WorkMode.OnSite, "Nantes", 28000m, 35000m, Currency.EUR),
        new DemoJobOfferSeed(
            "MEILLEURTAUX",
            "Courtier(ère) en assurance",
            "Basé(e) à Lyon, vous développez un portefeuille de clients particuliers et professionnels à qui vous proposez des solutions d'assurance habitation, auto, santé et emprunteur. Vous analysez leurs besoins, comparez les offres de nos partenaires assureurs et les accompagnez jusqu'à la souscription.",
            "Formation Bac+2/3 en assurance, banque ou commerce, idéalement complétée par une expérience en courtage ou en agence. Bonne connaissance des produits d'assurance et sens aigu de la négociation. Autonomie et rigueur dans le suivi des dossiers.",
            "Rémunération fixe + variable attractive, télétravail partiel (2 jours/semaine), mutuelle d'entreprise, formation continue aux produits d'assurance.",
            ContractType.FullTime, WorkMode.Hybrid, "Lyon", 30000m, 40000m, Currency.EUR),
        new DemoJobOfferSeed(
            "MEILLEURTAUX",
            "Analyste financier(ère)",
            "Rattaché(e) à la direction financière à Paris, vous analysez les indicateurs de performance de l'activité de courtage, produisez les reportings mensuels et participez à l'élaboration des prévisions budgétaires. Vous travaillez en lien étroit avec les équipes commerciales et la direction générale.",
            "Diplôme Bac+5 en finance, école de commerce ou université. Maîtrise d'Excel et des outils de reporting financier ; une première expérience en analyse financière ou contrôle de gestion est appréciée. Rigueur analytique et esprit de synthèse.",
            "Rémunération selon profil, intéressement, tickets restaurant, mutuelle d'entreprise, perspectives d'évolution vers des fonctions de contrôle de gestion.",
            ContractType.FullTime, WorkMode.OnSite, "Paris", 35000m, 45000m, Currency.EUR),
        new DemoJobOfferSeed(
            "MEILLEURTAUX",
            "Chargé(e) de clientèle crédit consommation",
            "Au sein de notre agence de Bordeaux, vous accueillez et conseillez une clientèle de particuliers sur leurs projets de crédit à la consommation (auto, travaux, projets personnels). Vous étudiez leur dossier, sélectionnez l'offre la plus adaptée parmi nos partenaires bancaires et assurez le suivi jusqu'au déblocage des fonds.",
            "Formation Bac+2 en banque, commerce ou finance. Aisance relationnelle, sens du service client et goût pour les objectifs commerciaux. Débutant(e) accepté(e) avec une forte motivation.",
            "Fixe + primes sur objectifs, mutuelle d'entreprise, tickets restaurant, formation initiale aux produits de crédit.",
            ContractType.FullTime, WorkMode.OnSite, "Bordeaux", 26000m, 32000m, Currency.EUR),
        new DemoJobOfferSeed(
            "MEILLEURTAUX",
            "Responsable d'agence",
            "Vous prenez la responsabilité de notre agence de Marseille : management d'une équipe de conseillers, animation commerciale, suivi des objectifs de production et développement du portefeuille clients locaux. Vous représentez également l'agence auprès des partenaires bancaires et assureurs de la région.",
            "Expérience confirmée (5 ans minimum) dans le courtage, la banque ou l'assurance, dont une expérience en management d'équipe. Leadership, sens commercial et capacité à fédérer autour des objectifs. Formation Bac+3/5 souhaitée.",
            "Rémunération fixe + variable sur la performance de l'agence, véhicule de fonction, mutuelle d'entreprise, forte autonomie de gestion.",
            ContractType.FullTime, WorkMode.OnSite, "Marseille", 40000m, 55000m, Currency.EUR),
        new DemoJobOfferSeed(
            "MEILLEURTAUX",
            "Conseiller(ère) en gestion de patrimoine",
            "Basé(e) à Paris, vous accompagnez une clientèle patrimoniale dans l'optimisation de leurs placements (assurance-vie, immobilier, défiscalisation). Vous réalisez des bilans patrimoniaux complets et proposez des stratégies adaptées à leurs objectifs de vie.",
            "Formation Bac+4/5 en gestion de patrimoine, finance ou droit. Expérience de 2 ans minimum en conseil patrimonial ou en banque privée. Excellent relationnel et rigueur juridique/fiscale.",
            "Rémunération fixe + commissions attractives, télétravail 2 jours/semaine, mutuelle d'entreprise, accès à des formations certifiantes (CIF).",
            ContractType.FullTime, WorkMode.Hybrid, "Paris", 35000m, 50000m, Currency.EUR),
        new DemoJobOfferSeed(
            "MEILLEURTAUX",
            "Chargé(e) de conformité",
            "Au sein de la direction des risques à Paris, vous veillez au respect des obligations réglementaires liées au courtage (LCB-FT, DDA, RGPD) et menez des contrôles de conformité sur les dossiers clients et les pratiques commerciales des agences. Vous rédigez les procédures internes et sensibilisez les équipes terrain.",
            "Formation Bac+5 en droit, conformité ou finance. Une expérience de 2 ans minimum en conformité dans le secteur bancaire ou assurantiel est requise. Rigueur, discrétion et bonne connaissance de la réglementation ACPR.",
            "Rémunération selon expérience, intéressement, mutuelle d'entreprise, tickets restaurant, environnement de travail structuré et formateur.",
            ContractType.FullTime, WorkMode.OnSite, "Paris", 38000m, 48000m, Currency.EUR),
        new DemoJobOfferSeed(
            "MEILLEURTAUX",
            "Data analyst risques crédit",
            "En full remote, vous rejoignez l'équipe data pour développer des modèles de scoring et analyser les indicateurs de risque de crédit sur l'ensemble du portefeuille clients. Vous produisez des tableaux de bord et des analyses prédictives pour améliorer la qualité d'octroi des crédits.",
            "Formation Bac+5 en data science, statistiques ou ingénierie. Maîtrise de Python/SQL et de Power BI ou équivalent. Une première expérience en analyse de risque bancaire ou crédit est un plus.",
            "Full remote, matériel informatique fourni, prime de télétravail, mutuelle d'entreprise, budget formation annuel.",
            ContractType.FullTime, WorkMode.FullRemote, null, 36000m, 46000m, Currency.EUR),
        new DemoJobOfferSeed(
            "MEILLEURTAUX",
            "Alternant(e) conseiller crédit",
            "Au sein de notre agence de Toulouse, vous êtes formé(e) au métier de conseiller en crédit : accueil client, montage de dossiers de financement, comparaison des offres bancaires partenaires, sous la supervision d'un conseiller confirmé. Cette alternance vous permet de découvrir l'ensemble de la chaîne de courtage en crédit.",
            "Préparation d'un Bac+2/3 en banque, finance ou commerce en alternance. Bon relationnel, sens de l'écoute et envie d'apprendre le métier de conseiller en crédit. Aucune expérience préalable exigée.",
            "Rémunération légale d'alternance, tickets restaurant, tutorat individualisé, forte probabilité d'embauche à l'issue du contrat.",
            ContractType.Internship, WorkMode.OnSite, "Toulouse", 18000m, 20000m, Currency.EUR),
        new DemoJobOfferSeed(
            "MEILLEURTAUX",
            "Responsable marketing digital",
            "Basé(e) à Paris, vous pilotez la stratégie marketing digital du groupe : acquisition SEA/SEO, campagnes emailing, animation des réseaux sociaux et optimisation du parcours client sur le site meilleurtaux.com. Vous encadrez une équipe de chargés de marketing digital et pilotez les prestataires externes.",
            "Formation Bac+5 en marketing digital ou école de commerce. Expérience de 4 ans minimum en marketing digital, idéalement dans le secteur financier ou e-commerce. Maîtrise des outils d'analytics (GA4) et de gestion de campagnes.",
            "Rémunération selon profil, télétravail 2-3 jours/semaine, mutuelle d'entreprise, intéressement, environnement dynamique et data-driven.",
            ContractType.FullTime, WorkMode.Hybrid, "Paris", 40000m, 52000m, Currency.EUR),

        // ------------------------------------------------------------
        // Expertime (EUR)
        // ------------------------------------------------------------
        new DemoJobOfferSeed(
            "EXPERTIME",
            "Consultant(e) Data & Analytics",
            "Au sein du pôle Data & Analytics à Paris, vous accompagnez nos clients grands comptes dans la conception de leurs plateformes data (data warehouse, dashboards, gouvernance de la donnée). Vous intervenez de la phase de cadrage jusqu'à la mise en production des solutions analytiques.",
            "Formation Bac+5 en école d'ingénieur, de commerce ou université, spécialisation data. Expérience de 2 à 5 ans en conseil ou en cabinet data, maîtrise de SQL et d'un outil de BI (Power BI, Tableau). Bon relationnel client indispensable.",
            "Rémunération selon profil, intéressement, télétravail hybride, plan de formation certifiant, mobilité possible vers les autres implantations du groupe.",
            ContractType.FullTime, WorkMode.Hybrid, "Paris", 40000m, 55000m, Currency.EUR),
        new DemoJobOfferSeed(
            "EXPERTIME",
            "Développeur(se) Low-Code (Power Apps/OutSystems)",
            "Basé(e) à Nantes, vous concevez et développez des applications métier sur mesure pour nos clients à l'aide de plateformes low-code (Power Apps, OutSystems). Vous participez aux ateliers de cadrage fonctionnel et assurez la maintenance évolutive des applications livrées.",
            "Formation Bac+3/5 en informatique. Expérience de 1 à 3 ans en développement low-code ou développement applicatif classique. Curiosité technique et capacité à dialoguer avec des utilisateurs métier non techniques.",
            "Télétravail hybride, certifications Microsoft/OutSystems financées, mutuelle d'entreprise, tickets restaurant.",
            ContractType.FullTime, WorkMode.Hybrid, "Nantes", 38000m, 48000m, Currency.EUR),
        new DemoJobOfferSeed(
            "EXPERTIME",
            "Chef(fe) de projet transformation digitale",
            "Depuis notre implantation de Lyon, vous pilotez des projets de transformation digitale pour des clients grands comptes : cadrage, planification, coordination des équipes techniques et fonctionnelles, suivi budgétaire. Vous êtes l'interlocuteur(trice) privilégié(e) du client tout au long du projet.",
            "Formation Bac+5 école d'ingénieur ou de commerce. Expérience de 5 ans minimum en gestion de projet informatique ou conseil, idéalement avec une certification (Prince2, Scrum). Leadership et excellente communication.",
            "Rémunération selon expérience, véhicule ou forfait mobilité, intéressement, forte autonomie dans la gestion de portefeuille client.",
            ContractType.FullTime, WorkMode.OnSite, "Lyon", 45000m, 60000m, Currency.EUR),
        new DemoJobOfferSeed(
            "EXPERTIME",
            "Développeur(se) Full-Stack e-commerce",
            "En full remote, vous développez et maintenez des plateformes e-commerce pour nos clients (front-end et back-end), en méthodologie agile. Vous intervenez sur des projets de refonte ou de création de sites marchands à fort trafic.",
            "Formation Bac+3/5 en informatique. Maîtrise de JavaScript/TypeScript (Vue ou React) et d'un langage back-end (Node.js, .NET ou PHP). Expérience de 2 ans minimum sur des projets e-commerce appréciée.",
            "Full remote total, matériel au choix, prime de télétravail, budget formation, séminaires d'équipe trimestriels.",
            ContractType.FullTime, WorkMode.FullRemote, null, 40000m, 52000m, Currency.EUR),
        new DemoJobOfferSeed(
            "EXPERTIME",
            "Consultant(e) conduite du changement",
            "Depuis notre bureau d'Aix-en-Provence, vous accompagnez les équipes de nos clients dans l'adoption de nouveaux outils et process digitaux : diagnostic des impacts, plans de communication, formation des utilisateurs. Vous mesurez l'adhésion et ajustez les plans d'action en continu.",
            "Formation Bac+5 en sciences humaines, management ou école de commerce. Expérience de 2 ans minimum en conduite du changement ou en formation. Excellentes qualités pédagogiques et relationnelles.",
            "Télétravail partiel, mutuelle d'entreprise, plan de formation individualisé, ambiance de travail collaborative.",
            ContractType.FullTime, WorkMode.Hybrid, "Aix-en-Provence", 38000m, 48000m, Currency.EUR),
        new DemoJobOfferSeed(
            "EXPERTIME",
            "Architecte solutions cloud",
            "Basé(e) à Paris, vous concevez les architectures cloud (Azure principalement) des projets de nos clients grands comptes : sécurité, scalabilité, coûts. Vous encadrez techniquement les équipes de développement et validez les choix d'architecture en comité technique.",
            "Formation Bac+5 école d'ingénieur. Expérience de 6 ans minimum en architecture logicielle ou cloud, certification Azure Solutions Architect appréciée. Excellente capacité à vulgariser des choix techniques complexes.",
            "Rémunération élevée selon expertise, télétravail hybride, budget certifications, participation aux conférences techniques.",
            ContractType.FullTime, WorkMode.Hybrid, "Paris", 55000m, 70000m, Currency.EUR),
        new DemoJobOfferSeed(
            "EXPERTIME",
            "UX/UI Designer espaces de travail modernes",
            "Au sein de l'équipe expériences de travail modernes à Nantes, vous concevez des interfaces intuitives pour les intranets, portails collaboratifs et applications métier de nos clients. Vous réalisez des ateliers de recherche utilisateur, des wireframes et des prototypes interactifs.",
            "Formation Bac+3/5 en design UX/UI. Expérience de 2 ans minimum avec Figma et une bonne culture des design systems. Sensibilité à l'accessibilité numérique (WCAG).",
            "Télétravail hybride, matériel de design haut de gamme, budget formation, environnement créatif et collaboratif.",
            ContractType.FullTime, WorkMode.Hybrid, "Nantes", 35000m, 45000m, Currency.EUR),
        new DemoJobOfferSeed(
            "EXPERTIME",
            "Consultant(e) GreenOps / RSE numérique",
            "Basé(e) à Paris, vous accompagnez nos clients dans la mise en œuvre de démarches d'écoconception numérique et de sobriété IT (méthodologie GreenOps). Vous réalisez des audits d'impact environnemental des systèmes d'information et proposez des plans d'action concrets.",
            "Formation Bac+5 en environnement, informatique ou RSE. Une première expérience en conseil RSE ou green IT est appréciée. Sensibilité forte aux enjeux de transformation durable, esprit de synthèse.",
            "Contribution directe à la stratégie RSE du groupe (certifié EcoVadis Platinum), télétravail hybride, formations spécialisées green IT, mutuelle d'entreprise.",
            ContractType.FullTime, WorkMode.Hybrid, "Paris", 42000m, 52000m, Currency.EUR),
        new DemoJobOfferSeed(
            "EXPERTIME",
            "Business Analyst applications métier",
            "Depuis Lyon, vous recueillez et formalisez les besoins métier de nos clients pour la conception d'applications sur mesure. Vous rédigez les spécifications fonctionnelles et assurez le lien entre les équipes métier et les équipes de développement tout au long du projet.",
            "Formation Bac+4/5 en informatique de gestion ou école de commerce. Expérience de 2 ans minimum en analyse fonctionnelle ou business analysis. Rigueur rédactionnelle et sens de l'écoute client.",
            "Rémunération selon profil, mutuelle d'entreprise, tickets restaurant, parcours de montée en compétences vers un rôle de chef de projet.",
            ContractType.FullTime, WorkMode.OnSite, "Lyon", 36000m, 46000m, Currency.EUR),
        new DemoJobOfferSeed(
            "EXPERTIME",
            "Stagiaire consultant(e) data (6 mois)",
            "Pour un stage de 6 mois au sein du pôle Data & Analytics à Paris, vous participez aux missions de conseil data pour nos clients : préparation de données, construction de dashboards, support aux consultants seniors sur les livrables. Une belle opportunité de découvrir le métier de consultant en environnement réel.",
            "Étudiant(e) en Bac+4/5 école d'ingénieur, de commerce ou université, spécialisation data ou informatique. Bases en SQL et intérêt marqué pour la data. Curiosité, rigueur et envie d'apprendre.",
            "Gratification de stage, tickets restaurant, encadrement par un consultant senior, forte possibilité de poursuite en alternance ou en CDI.",
            ContractType.Internship, WorkMode.OnSite, "Paris", 12000m, 15000m, Currency.EUR),

        // ------------------------------------------------------------
        // Dynaminqs (XOF)
        // ------------------------------------------------------------
        new DemoJobOfferSeed(
            "DYNAMINQS",
            "Consultant(e) fonctionnel(le) Dynamics 365 F&O",
            "Basé(e) à Dakar, vous accompagnez nos clients dans le paramétrage et le déploiement du module Finance & Operations de Microsoft Dynamics 365. Vous animez les ateliers de cadrage fonctionnel, formez les utilisateurs clés et assurez le support post-déploiement.",
            "Formation Bac+4/5 en gestion, finance ou informatique de gestion. Expérience de 2 ans minimum sur Dynamics 365 F&O ou un ERP équivalent (SAP, Sage). Bon relationnel client et capacité à vulgariser des concepts fonctionnels.",
            "Rémunération attractive selon profil, prise en charge des déplacements clients, formation continue aux certifications Microsoft, mutuelle santé.",
            ContractType.FullTime, WorkMode.OnSite, "Dakar", 6000000m, 9000000m, Currency.XOF),
        new DemoJobOfferSeed(
            "DYNAMINQS",
            "Développeur(se) Power Platform (Power Apps/Power Automate)",
            "Depuis Dakar, vous concevez des applications métier et des automatisations de processus pour nos clients à l'aide de Power Apps et Power Automate. Vous participez aux phases de conception, développement et recette avec les équipes fonctionnelles.",
            "Formation Bac+3/5 en informatique. Expérience de 1 à 3 ans en développement Power Platform ou développement applicatif. Autonomie et goût pour l'apprentissage de nouvelles technologies Microsoft.",
            "Télétravail partiel, certifications Microsoft financées, mutuelle santé, prime de performance.",
            ContractType.FullTime, WorkMode.Hybrid, "Dakar", 5500000m, 8000000m, Currency.XOF),
        new DemoJobOfferSeed(
            "DYNAMINQS",
            "Architecte solution Microsoft Dynamics 365",
            "Basé(e) à Dakar, vous définissez les architectures techniques et fonctionnelles des projets Dynamics 365 (F&O et/ou CE) pour nos clients régionaux. Vous encadrez techniquement les équipes de consultants et développeurs et validez les choix d'intégration avec le système d'information existant.",
            "Formation Bac+5 en informatique. Expérience de 6 ans minimum sur l'écosystème Microsoft Dynamics 365, certifications d'architecte souhaitées. Excellente capacité de communication avec les décideurs clients.",
            "Rémunération élevée selon expertise, véhicule de fonction, mutuelle santé familiale, missions régionales valorisantes.",
            ContractType.FullTime, WorkMode.OnSite, "Dakar", 9000000m, 13000000m, Currency.XOF),
        new DemoJobOfferSeed(
            "DYNAMINQS",
            "Chef(fe) de projet intégration Dynamics 365",
            "Depuis Dakar, vous pilotez des projets d'intégration Microsoft Dynamics 365 de bout en bout : cadrage, planification, coordination des équipes techniques et fonctionnelles, suivi budgétaire et reporting client. Vous êtes garant(e) du respect des délais et de la qualité de la livraison.",
            "Formation Bac+5 en gestion de projet ou informatique. Expérience de 4 ans minimum en gestion de projet ERP/CRM, certification en gestion de projet (Prince2, PMP) appréciée. Leadership et rigueur organisationnelle.",
            "Rémunération selon expérience, prime sur objectifs projet, mutuelle santé, perspectives d'évolution vers un poste de direction de projets.",
            ContractType.FullTime, WorkMode.OnSite, "Dakar", 7000000m, 10000000m, Currency.XOF),
        new DemoJobOfferSeed(
            "DYNAMINQS",
            "Consultant(e) Power BI / Data Analyst",
            "Basé(e) à Dakar, vous concevez des tableaux de bord Power BI pour nos clients afin de piloter leur performance opérationnelle et financière. Vous modélisez les données issues de Dynamics 365 et d'autres sources pour produire des analyses fiables et exploitables.",
            "Formation Bac+4/5 en data, informatique de gestion ou finance. Maîtrise de Power BI et du langage DAX ; une première expérience avec Dynamics 365 est un plus. Rigueur analytique et sens de la présentation.",
            "Télétravail partiel, formations certifiantes Power BI, mutuelle santé, environnement de travail international.",
            ContractType.FullTime, WorkMode.Hybrid, "Dakar", 5000000m, 7500000m, Currency.XOF),
        new DemoJobOfferSeed(
            "DYNAMINQS",
            "Développeur(se) .NET / Dynamics 365 CE",
            "En full remote, vous développez des extensions et intégrations personnalisées sur Microsoft Dynamics 365 Customer Engagement (plugins, workflows, API .NET). Vous collaborez avec les consultants fonctionnels pour traduire les besoins clients en solutions techniques robustes.",
            "Formation Bac+3/5 en informatique. Maîtrise de C#/.NET et une expérience de 2 ans minimum sur Dynamics 365 CE ou CRM équivalent. Autonomie forte requise pour le travail en full remote.",
            "Full remote total, matériel informatique fourni, prime de connexion internet, formations certifiantes Microsoft.",
            ContractType.FullTime, WorkMode.FullRemote, null, 6000000m, 9000000m, Currency.XOF),
        new DemoJobOfferSeed(
            "DYNAMINQS",
            "Business Analyst transformation digitale",
            "Depuis Dakar, vous accompagnez nos clients dans l'analyse de leurs processus métier en vue de leur transformation digitale via l'écosystème Microsoft. Vous rédigez les cahiers des charges fonctionnels et participez aux phases de recette avant mise en production.",
            "Formation Bac+4/5 en gestion, informatique de gestion ou commerce. Expérience de 2 ans minimum en analyse fonctionnelle ou conseil en transformation digitale. Bonnes capacités rédactionnelles et esprit d'analyse.",
            "Rémunération selon profil, mutuelle santé, tickets restaurant, montée en compétences sur l'écosystème Microsoft Dynamics.",
            ContractType.FullTime, WorkMode.OnSite, "Dakar", 4500000m, 7000000m, Currency.XOF),
        new DemoJobOfferSeed(
            "DYNAMINQS",
            "Administrateur(trice) systèmes Dynamics 365",
            "Basé(e) à Dakar, vous assurez l'administration technique des environnements Dynamics 365 de nos clients : gestion des accès, sauvegardes, montées de version et supervision des performances. Vous intervenez également en support de niveau 2 sur les incidents remontés par les utilisateurs.",
            "Formation Bac+2/3 en informatique. Expérience de 2 ans minimum en administration système ou support technique, idéalement sur environnement cloud Microsoft (Azure, Dynamics 365). Sens du service et réactivité.",
            "Rémunération selon profil, mutuelle santé, formation continue aux outils Microsoft, environnement de travail structuré.",
            ContractType.FullTime, WorkMode.OnSite, "Dakar", 4000000m, 6000000m, Currency.XOF),
        new DemoJobOfferSeed(
            "DYNAMINQS",
            "Consultant(e) conduite du changement",
            "Depuis Dakar, vous accompagnez les collaborateurs de nos clients dans l'appropriation des nouveaux outils Microsoft Dynamics 365 déployés : ateliers de formation, supports pédagogiques, mesure de l'adoption. Vous identifiez les freins au changement et proposez des plans d'action adaptés.",
            "Formation Bac+4/5 en sciences humaines, management ou gestion de projet. Expérience de 2 ans minimum en conduite du changement ou en formation. Excellentes qualités pédagogiques et sens de l'écoute.",
            "Télétravail partiel, mutuelle santé, plan de formation individualisé, missions variées auprès de clients de différents secteurs.",
            ContractType.FullTime, WorkMode.Hybrid, "Dakar", 4500000m, 6500000m, Currency.XOF),
        new DemoJobOfferSeed(
            "DYNAMINQS",
            "Stagiaire développeur(se) Power Platform",
            "Pour un stage au sein de l'équipe technique à Dakar, vous participez au développement d'applications Power Apps et d'automatisations Power Automate pour nos clients, sous la supervision d'un développeur senior. Une belle opportunité de découvrir l'écosystème Microsoft Power Platform en conditions réelles.",
            "Étudiant(e) en Bac+3/5 informatique. Bases en programmation et intérêt marqué pour les technologies low-code Microsoft. Curiosité, rigueur et envie d'apprendre.",
            "Gratification de stage, encadrement par un développeur senior, formation aux outils Power Platform, forte possibilité de poursuite en alternance ou en CDI.",
            ContractType.Internship, WorkMode.OnSite, "Dakar", 1500000m, 2000000m, Currency.XOF)
    ];

    // ---------------------------------------------------------------------
    // Data: Candidates (4 total, not tied to any single organization)
    // ---------------------------------------------------------------------

    internal sealed record DemoCandidateSeed(string FirstName, string LastName, string Email);

    internal static readonly DemoCandidateSeed[] DemoCandidates =
    [
        new DemoCandidateSeed("Aïssatou", "Ba", "aissatou.ba@candidat-demo.fr"),
        new DemoCandidateSeed("Ousmane", "Kane", "ousmane.kane@candidat-demo.fr"),
        new DemoCandidateSeed("Léa", "Dupont", "lea.dupont@candidat-demo.fr"),
        new DemoCandidateSeed("Maxime", "Renard", "maxime.renard@candidat-demo.fr")
    ];

    // ---------------------------------------------------------------------
    // Data: Applications (11 total)
    // ---------------------------------------------------------------------

    internal sealed record DemoApplicationSeed(string CandidateEmail, string OrgCode, string JobOfferTitle);

    internal static readonly DemoApplicationSeed[] DemoApplications =
    [
        // Aïssatou Ba: Meilleurtaux #1, Expertime #1, Dynaminqs #1
        new DemoApplicationSeed("aissatou.ba@candidat-demo.fr", "MEILLEURTAUX", "Conseiller(ère) en crédit immobilier"),
        new DemoApplicationSeed("aissatou.ba@candidat-demo.fr", "EXPERTIME", "Consultant(e) Data & Analytics"),
        new DemoApplicationSeed("aissatou.ba@candidat-demo.fr", "DYNAMINQS", "Consultant(e) fonctionnel(le) Dynamics 365 F&O"),

        // Ousmane Kane: Expertime #2, Dynaminqs #2, Dynaminqs #6
        new DemoApplicationSeed("ousmane.kane@candidat-demo.fr", "EXPERTIME", "Développeur(se) Low-Code (Power Apps/OutSystems)"),
        new DemoApplicationSeed("ousmane.kane@candidat-demo.fr", "DYNAMINQS", "Développeur(se) Power Platform (Power Apps/Power Automate)"),
        new DemoApplicationSeed("ousmane.kane@candidat-demo.fr", "DYNAMINQS", "Développeur(se) .NET / Dynamics 365 CE"),

        // Léa Dupont: Meilleurtaux #5, Expertime #3
        new DemoApplicationSeed("lea.dupont@candidat-demo.fr", "MEILLEURTAUX", "Responsable d'agence"),
        new DemoApplicationSeed("lea.dupont@candidat-demo.fr", "EXPERTIME", "Chef(fe) de projet transformation digitale"),

        // Maxime Renard: Meilleurtaux #2, Meilleurtaux #6, Dynaminqs #5
        new DemoApplicationSeed("maxime.renard@candidat-demo.fr", "MEILLEURTAUX", "Courtier(ère) en assurance"),
        new DemoApplicationSeed("maxime.renard@candidat-demo.fr", "MEILLEURTAUX", "Conseiller(ère) en gestion de patrimoine"),
        new DemoApplicationSeed("maxime.renard@candidat-demo.fr", "DYNAMINQS", "Consultant(e) Power BI / Data Analyst")
    ];
}
