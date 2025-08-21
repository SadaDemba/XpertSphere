using Microsoft.EntityFrameworkCore;
using XpertSphere.MonolithApi.Data;

namespace XpertSphere.MonolithApi.Tests.Helpers;

public static class TestDbContextFactory
{
    public static XpertSphereDbContext CreateInMemoryContext(string databaseName = "TestDatabase")
    {
        var options = new DbContextOptionsBuilder<XpertSphereDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName)
            .Options;

        var context = new XpertSphereDbContext(options);
        
        // Ensure the database is created
        context.Database.EnsureCreated();
        
        return context;
    }
}