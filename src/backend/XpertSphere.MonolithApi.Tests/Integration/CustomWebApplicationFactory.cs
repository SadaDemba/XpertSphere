using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using XpertSphere.MonolithApi.Data;

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
                ["Admin:Password"] = SeededAdminPassword,
                // SeedPlatformSuperAdminAsync looks up the seeded organization by comparing
                // Organization.Name to the hardcoded Utils.Constants.XPERTSPHERE ("XPERTSPHERE",
                // all caps), while appsettings.json's Seeding:Organization:Name is "XpertSphere"
                // (mixed case). This comparison relies on SQL Server's default case-insensitive
                // collation in real deployments; the EF Core InMemory provider used by this
                // harness performs ordinal (case-sensitive) comparisons, so the two would never
                // match without this override. Not a production bug - a provider-behavior
                // difference specific to this test harness - so fixed here at the configuration
                // level rather than in DatabaseExtensions.cs.
                ["Seeding:Organization:Name"] = "XPERTSPHERE"
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
}
