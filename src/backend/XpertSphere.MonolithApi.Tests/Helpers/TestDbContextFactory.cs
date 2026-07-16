using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using XpertSphere.MonolithApi.Data;

namespace XpertSphere.MonolithApi.Tests.Helpers;

public static class TestDbContextFactory
{
    public static XpertSphereDbContext CreateInMemoryContext(string databaseName = "TestDatabase")
    {
        var options = new DbContextOptionsBuilder<XpertSphereDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName)
            // The in-memory provider does not support real transactions; some services
            // (e.g. AuthenticationService.RegisterCandidateAsync) call BeginTransactionAsync
            // as part of a normal execution strategy. Without this, EF Core raises
            // TransactionIgnoredWarning as an error instead of silently no-op'ing it.
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var context = new XpertSphereDbContext(options);

        // Ensure the database is created
        context.Database.EnsureCreated();

        return context;
    }
}