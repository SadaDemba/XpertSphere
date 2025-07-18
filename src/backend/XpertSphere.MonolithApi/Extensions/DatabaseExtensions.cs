using Microsoft.EntityFrameworkCore;
using XpertSphere.MonolithApi.Data;

namespace XpertSphere.MonolithApi.Extensions;

public static class DatabaseExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services,
        IConfiguration configuration, IWebHostEnvironment environment)
    {
        var connectionString = GetConnectionString(configuration);

        services.AddDbContext<XpertSphereDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                // Connection resilience
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null);

                // Migration assembly
                sqlOptions.MigrationsAssembly("XpertSphere.MonolithApi");
            });

            // Development configurations
            if (environment.IsDevelopment())
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }

            // Production optimizations
            if (environment.IsProduction())
            {
                options.EnableServiceProviderCaching();
                options.EnableSensitiveDataLogging(false);
            }
        });

        return services;
    }

    public static async Task<WebApplication> UseDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<XpertSphereDbContext>();

        try
        {
            // Apply pending migrations
            await context.Database.MigrateAsync();

            // Seed initial data if needed
            //await SeedDatabaseAsync(context);
        }
        catch (Exception ex)
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while migrating the database");
            throw;
        }

        return app;
    }

    private static string GetConnectionString(IConfiguration configuration)
    {
        // Try environment variable first (for Azure deployment)
        var azureConnectionString = Environment.GetEnvironmentVariable("AZURE_SQL_CONNECTIONSTRING");

        Console.WriteLine("Azure Connection string  is " + azureConnectionString);
        if (!string.IsNullOrEmpty(azureConnectionString))
        {
            return azureConnectionString;
        }

        Console.WriteLine("Local Connection string  is " + configuration.GetConnectionString("DefaultConnection"));

        // Fallback to configuration
        return configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("No connection string found");
    }

    private static async Task SeedDatabaseAsync(XpertSphereDbContext context)
    {
        // Add any initial data seeding here
        // Example: Default roles, admin user, etc.

        if (!await context.Roles.AnyAsync())
        {
            // Seed default roles if none exist
            // This would be implemented based on your needs
        }

        await context.SaveChangesAsync();
    }
}