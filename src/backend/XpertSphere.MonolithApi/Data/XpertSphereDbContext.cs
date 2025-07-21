using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using XpertSphere.MonolithApi.Models;
using XpertSphere.MonolithApi.Models.Base;

namespace XpertSphere.MonolithApi.Data;

public class XpertSphereDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
{
    public XpertSphereDbContext(DbContextOptions<XpertSphereDbContext> options) : base(options)
    {
    }

    public DbSet<Organization> Organizations { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Apply configurations
        builder.ApplyConfigurationsFromAssembly(typeof(XpertSphereDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    /*
                    if (entry.Entity.Id == Guid.Empty)
                    {
                        entry.Entity.Id = Guid.NewGuid();
                    }
                    */
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    // TODO: entry.Entity.CreatedBy = GetCurrentUserId();
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    // TODO: entry.Entity.UpdatedBy = GetCurrentUserId();
                    break;
            }
        }


        return await base.SaveChangesAsync(cancellationToken);
    }
}
