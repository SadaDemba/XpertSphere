using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using XpertSphere.MonolithApi.Models;
using XpertSphere.MonolithApi.Data.Configurations.Base;

namespace XpertSphere.MonolithApi.Data.Configurations;

public class RoleConfiguration : AuditableEntityConfiguration<Role>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");

        builder.Property(r => r.Id)
            .HasDefaultValueSql("NEWID()");

        // Indexes
        builder.HasIndex(r => r.Name)
            .IsUnique()
            .HasDatabaseName("IX_Roles_Name");

        builder.HasIndex(r => r.IsActive)
            .HasDatabaseName("IX_Roles_IsActive");

        // Relationships - Use Restrict to avoid cascade cycles
        builder.HasMany(r => r.UserRoles)
            .WithOne(ur => ur.Role)
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(r => r.RolePermissions)
            .WithOne(rp => rp.Role)
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
