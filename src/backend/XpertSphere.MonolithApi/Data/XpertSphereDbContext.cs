using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using XpertSphere.MonolithApi.Interfaces;
using XpertSphere.MonolithApi.Models;
using XpertSphere.MonolithApi.Models.Base;

namespace XpertSphere.MonolithApi.Data;

public class XpertSphereDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
{
    private readonly ICurrentUserService? _currentUserService;

    public XpertSphereDbContext(DbContextOptions<XpertSphereDbContext> options) : base(options)
    {
    }

    public XpertSphereDbContext(DbContextOptions<XpertSphereDbContext> options, ICurrentUserService currentUserService) : base(options)
    {
        _currentUserService = currentUserService;
    }

    public DbSet<Organization> Organizations { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<Training> Trainings { get; set; }
    public DbSet<Experience> Experiences { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<JobOffer> JobOffers { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Apply configurations
        builder.ApplyConfigurationsFromAssembly(typeof(XpertSphereDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService?.UserId;

        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    if (currentUserId.HasValue)
                    {
                        entry.Entity.CreatedBy = currentUserId.Value;
                    }
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    if (currentUserId.HasValue)
                    {
                        entry.Entity.UpdatedBy = currentUserId.Value;
                    }
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
