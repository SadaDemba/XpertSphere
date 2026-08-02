using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using XpertSphere.MonolithApi.Data;
using XpertSphere.MonolithApi.Models;
using XpertSphere.MonolithApi.Utils;

namespace XpertSphere.MonolithApi.Tests.Integration;

/// <summary>
/// Boots the real ASP.NET Core pipeline (Program.cs) end-to-end - real Identity, real
/// FrenchIdentityErrorDescriber, real DataAnnotations model binding/validation, real
/// authentication/authorization - against an EF Core InMemory database instead of SQL Server, so
/// that the HTTP-level verification required by
/// .claude/specifications/localize-identity-error-messages.md (critères d'acceptation 3, 4, 5, 9)
/// does not depend on a running SQL Server instance.
///
/// The "Development" environment is used deliberately (not a dedicated "Testing" environment):
/// KeyVaultExtensions.AddKeyVaultConfiguration and the Application Insights wiring in Program.cs
/// both throw/require real Azure configuration for any environment where
/// IWebHostEnvironment.IsDevelopment() is false. Using "Development" here is the smallest way to
/// avoid touching that unrelated startup code while still exercising the real pipeline; it does
/// mean the full demo dataset (SeedDemoDataAsync) also runs once per factory instance, which is a
/// few extra seconds, not a correctness concern (see DatabaseExtensions.DemoData.cs - no raw SQL,
/// no transactions, no blob storage calls, confirmed compatible with the InMemory provider).
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    // Shared by every DbContext instance resolved from this factory's service provider, so that
    // data seeded during startup (PlatformSuperAdmin, roles, etc.) is visible to every request
    // handled by the same factory/client.
    private readonly string _databaseName = $"integration-tests-{Guid.NewGuid()}";

    /// <summary>
    /// Credentials of the PlatformSuperAdmin account seeded by SeedPlatformSuperAdminAsync for
    /// this factory instance (see Admin:Email / Admin:Password below) - usable by integration
    /// tests that need an authenticated organization/platform-scoped call (e.g. criterion 5,
    /// admin-reset-password).
    /// </summary>
    public const string SeededAdminEmail = "integration-test-admin@xpertsphere.local";

    public const string SeededAdminPassword = "IntegrationTestAdmin1!";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            // Values that a real local run would source from a .env file (via DotNetEnv) or from
            // Azure Key Vault (Staging/Production) - none of which exist in this test
            // environment. Every key below is read via IConfiguration["..."] or
            // Environment.GetEnvironmentVariable("...") somewhere in Program.cs/SecurityExtensions
            // /DatabaseExtensions and throws InvalidOperationException at startup if missing.
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "integration-tests-signing-key-at-least-256-bits-long-0123456789",
                ["Admin:Email"] = SeededAdminEmail,
                ["Admin:Password"] = SeededAdminPassword
                // Seeding:Organization:Name is deliberately left at its appsettings.json default
                // ("XpertSphere") rather than overridden here, so that this harness stays
                // production-faithful on a security-relevant value: SecurityExtensions.cs policies
                // (CanCreateUsers, OrganizationIsolation, CanResetPasswords, ...) compare an
                // "OrganizationName" claim against the literal "XpertSphere" with ordinal,
                // case-sensitive C# `==`. See EnsureSeededAdminExistsAsync below for why
                // SeedPlatformSuperAdminAsync itself still needs a helping hand under this
                // specific combination of provider and organization name casing.
            });
        });

        builder.ConfigureServices(services =>
        {
            // Replace the SQL Server DbContextOptions registered by AddDatabase(...) in
            // Program.cs with the EF Core InMemory provider - the only change needed to run this
            // application without a real SQL Server instance. AddDbContext appends an
            // IDbContextOptionsConfiguration<XpertSphereDbContext> descriptor per call rather than
            // replacing the previous one, so both the SQL Server and InMemory configurations would
            // otherwise be applied to the same options ("Only a single database provider can be
            // registered" at startup) - remove every existing registration for this context before
            // adding ours.
            services.RemoveAll<DbContextOptions<XpertSphereDbContext>>();
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<XpertSphereDbContext>));

            services.AddDbContext<XpertSphereDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
                // AuthenticationService wraps several operations (e.g. RegisterCandidateAsync) in
                // an explicit Database.BeginTransactionAsync(); the InMemory provider does not
                // support real transactions and otherwise logs/throws a
                // TransactionIgnoredWarning - silence it here since it is expected and harmless
                // for this test harness.
                options.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
            });
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        // DatabaseExtensions.SeedPlatformSuperAdminAsync (run as part of the real startup
        // sequence above) looks up the just-seeded XpertSphere organization via
        // context.Organizations.FirstOrDefaultAsync(o => o.Name == Constants.XPERTSPHERE)
        // ("XPERTSPHERE", all caps) while the organization's actual Name is "XpertSphere" (mixed
        // case, from Seeding:Organization:Name - deliberately left at its production value, see
        // ConfigureWebHost above). That comparison matches under SQL Server's default
        // case-insensitive collation in real deployments, but never matches under the EF Core
        // InMemory provider's ordinal comparison, so the built-in seeding silently skips creating
        // the PlatformSuperAdmin account here. Rather than change the organization's name (which
        // would make this harness diverge from production on a claim value used by
        // ordinal-comparison authorization policies in SecurityExtensions.cs), seed the same
        // account directly against the already-persisted, production-faithful "XpertSphere"
        // organization.
        using var scope = host.Services.CreateScope();
        EnsureSeededAdminExistsAsync(scope.ServiceProvider).GetAwaiter().GetResult();

        return host;
    }

    private static async Task EnsureSeededAdminExistsAsync(IServiceProvider services)
    {
        var userManager = services.GetRequiredService<UserManager<User>>();
        if (await userManager.FindByEmailAsync(SeededAdminEmail) != null)
        {
            // Already created by the real startup seeding (e.g. organization name casing
            // happened to match) - nothing to do.
            return;
        }

        var context = services.GetRequiredService<XpertSphereDbContext>();
        var organization = await context.Organizations.FirstAsync(o => o.Name == "XpertSphere");
        var superAdminRole = await context.Roles.FirstAsync(r => r.Name == Roles.PlatformSuperAdmin.Name);

        var admin = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Super",
            LastName = "Admin",
            Email = SeededAdminEmail,
            UserName = SeededAdminEmail,
            EmailConfirmed = true,
            OrganizationId = organization.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            ConsentGivenAt = DateTime.UtcNow
        };

        var createResult = await userManager.CreateAsync(admin, SeededAdminPassword);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(
                "Failed to seed the integration test PlatformSuperAdmin account: " +
                string.Join(", ", createResult.Errors.Select(e => e.Description)));
        }

        context.UserRoles.Add(new UserRole
        {
            Id = Guid.NewGuid(),
            UserId = admin.Id,
            RoleId = superAdminRole.Id,
            IsActive = true,
            AssignedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();
    }
}
