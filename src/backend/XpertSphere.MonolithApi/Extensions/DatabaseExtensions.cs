using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using XpertSphere.MonolithApi.Data;
using XpertSphere.MonolithApi.Enums;
using XpertSphere.MonolithApi.Models;
using XpertSphere.MonolithApi.Models.Base;
using XpertSphere.MonolithApi.Utils;

namespace XpertSphere.MonolithApi.Extensions;

public static class DatabaseExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services,
        IConfiguration configuration, IWebHostEnvironment environment)
    {
        var connectionString = GetConnectionString(configuration,  environment);

        services.AddDbContext<XpertSphereDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                // Connection resilience
                sqlOptions.EnableRetryOnFailure(
                    3,
                    TimeSpan.FromSeconds(10),
                    null);

                // Migration assembly
                sqlOptions.MigrationsAssembly("XpertSphere.MonolithApi");
            });

            // Development configurations
            if (environment.IsDevelopment())
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }

            // Production or Staging optimizations
            else if(!environment.IsDevelopment())
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
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        try
        {
            // Apply pending migrations
            await context.Database.MigrateAsync();

            // Seed initial data if needed
            await SeedDatabaseAsync(context, userManager, configuration);
        }
        catch (Exception ex)
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while migrating the database");
            throw;
        }

        return app;
    }

    private static string GetConnectionString(IConfiguration configuration,  IWebHostEnvironment environment)
    {
        var connectionString = environment.IsDevelopment()
            ? Environment.GetEnvironmentVariable("DEFAULTCONNECTIONSTRING")
            : Environment.GetEnvironmentVariable("DEFAULTCONNECTIONSTRING");
        //TODO: reset after the test prod
            //Environment.GetEnvironmentVariable("AZURESQLCONNECTIONSTRING");
        
        if (string.IsNullOrEmpty(connectionString)) throw new InvalidOperationException("No connection string found");

        return connectionString;
    }

    private static async Task SeedDatabaseAsync(XpertSphereDbContext context, UserManager<User> userManager, IConfiguration configuration)
    {
        await SeedXpertSphereOrganizationAsync(context, configuration);
        await SeedDefaultRolesAsync(context);
        await SeedPlatformSuperAdminAsync(context, userManager, configuration);

        await context.SaveChangesAsync();
    }

    private static async Task SeedXpertSphereOrganizationAsync(XpertSphereDbContext context, IConfiguration configuration)
    {
        // Check if XpertSphere organization already exists
        var xpertSphereOrg = await context.Organizations
            .FirstOrDefaultAsync(o => o.Name == Constants.XPERTSPHERE);

        if (xpertSphereOrg == null)
        {
            var orgConfig = configuration.GetSection("Seeding:Organization");
            
            xpertSphereOrg = new Organization
            {
                Id = Guid.NewGuid(),
                Name = orgConfig["Name"] ?? Constants.XPERTSPHERE,
                Code = orgConfig["Code"] ?? throw new InvalidOperationException("No organization id found"),
                ContactEmail = orgConfig["ContactEmail"],
                ContactPhone = orgConfig["ContactPhone"],
                Size = OrganizationSize.Large,
                IsActive = true,
                Industry = orgConfig["Industry"],
                Website = orgConfig["Website"],
                CreatedAt = DateTime.UtcNow,
                Address = new Address
                {
                    Street = orgConfig["Address:Street"],
                    City = orgConfig["Address:City"],
                    PostalCode = orgConfig["Address:PostalCode"],
                    Country = orgConfig["Address:Country"],
                    Region = orgConfig["Address:Region"]
                }
            };

            context.Organizations.Add(xpertSphereOrg);
            Console.WriteLine("XpertSphere organization seeded successfully");
        }
        else
        {
            Console.WriteLine("XpertSphere organization already exists");
        }
    }

    private static async Task SeedDefaultRolesAsync(XpertSphereDbContext context)
    {
        var rolesToSeed = new List<RoleDefinition>
        {
            new(Roles.PlatformSuperAdmin.Name, Roles.PlatformSuperAdmin.Description,
                Roles.PlatformSuperAdmin.DisplayName),
            new(Roles.PlatformAdmin.Name, Roles.PlatformAdmin.Description, Roles.PlatformAdmin.DisplayName),
            new(Roles.OrganizationAdmin.Name, Roles.OrganizationAdmin.Description, Roles.OrganizationAdmin.DisplayName),
            new(Roles.Manager.Name, Roles.Manager.Description, Roles.Manager.DisplayName),
            new(Roles.Recruiter.Name, Roles.Recruiter.Description, Roles.Recruiter.DisplayName),
            new(Roles.TechnicalEvaluator.Name, Roles.TechnicalEvaluator.Description,
                Roles.TechnicalEvaluator.DisplayName),
            new(Roles.Candidate.Name, Roles.Candidate.Description, Roles.Candidate.DisplayName)
        };

        foreach (var roleDef in rolesToSeed)
        {
            var existingRole = await context.Roles
                .FirstOrDefaultAsync(r => r.Name == roleDef.Name);

            if (existingRole == null)
            {
                var role = new Role
                {
                    Id = Guid.NewGuid(),
                    Name = roleDef.Name,
                    DisplayName = roleDef.DisplayName,
                    Description = roleDef.Description,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                context.Roles.Add(role);
                Console.WriteLine($"Role '{roleDef.Name}' seeded successfully");
            }
            else
            {
                Console.WriteLine($"ℹRole '{roleDef.Name}' already exists");
            }
        }
    }

    private static async Task SeedPlatformSuperAdminAsync(XpertSphereDbContext context, UserManager<User> userManager, IConfiguration configuration)
    {
        var adminConfig = configuration.GetSection("Seeding:PlatformSuperAdmin");
        
        var adminEmail = configuration["Admin:Email"] ?? throw new InvalidOperationException("No admin email found");
        var adminPassword = configuration["Admin:Password"] ?? throw new InvalidOperationException("No admin password found");
        var adminFirstName = adminConfig["FirstName"] ?? throw new InvalidOperationException("No First Name found");
        var adminLastName = adminConfig["LastName"] ?? throw new InvalidOperationException("No Last Name found");

        // Check if super admin already exists
        var existingSuperAdmin = await userManager.FindByEmailAsync(adminEmail);
        if (existingSuperAdmin != null)
        {
            Console.WriteLine($"PlatformSuperAdmin already exists");
            return;
        }

        // Get XpertSphere organization
        var xpertSphereOrg = await context.Organizations
            .FirstOrDefaultAsync(o => o.Name == Constants.XPERTSPHERE);
        
        if (xpertSphereOrg == null)
        {
            Console.WriteLine("XpertSphere organization not found. Cannot create PlatformSuperAdmin");
            return;
        }

        // Get PlatformSuperAdmin role
        var superAdminRole = await context.Roles
            .FirstOrDefaultAsync(r => r.Name == Roles.PlatformSuperAdmin.Name);
        
        if (superAdminRole == null)
        {
            Console.WriteLine("PlatformSuperAdmin role not found. Cannot create PlatformSuperAdmin user");
            return;
        }

        // Create PlatformSuperAdmin user
        var superAdminUser = new User
        {
            Id = Guid.NewGuid(),
            FirstName = adminFirstName,
            LastName = adminLastName,
            Email = adminEmail,
            UserName = adminEmail,
            EmailConfirmed = true,
            OrganizationId = xpertSphereOrg.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            ConsentGivenAt = DateTime.UtcNow
        };

        // Create user with UserManager (handles password hashing)
        var result = await userManager.CreateAsync(superAdminUser, adminPassword);
        
        if (!result.Succeeded)
        {
            Console.WriteLine($"Failed to create PlatformSuperAdmin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            return;
        }

        // Assign the role SA to user
        var userRole = new UserRole
        {
            Id = Guid.NewGuid(),
            UserId = superAdminUser.Id,
            RoleId = superAdminRole.Id,
            IsActive = true,
            AssignedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        context.UserRoles.Add(userRole);
        
        Console.WriteLine($"PlatformSuperAdmin user created successfully");
        Console.WriteLine($"⚠Default password used. Please change it after first login.");
    }
}