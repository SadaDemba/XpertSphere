using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using XpertSphere.MonolithApi.Models;
using XpertSphere.MonolithApi.Data.Configurations.Base;

namespace XpertSphere.MonolithApi.Data.Configurations;

public class RolePermissionConfiguration : AuditableEntityConfiguration<RolePermission>
{
    protected override void ConfigureEntity(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions");

        builder.Property(rp => rp.Id)
            .HasDefaultValueSql("NEWID()");

        // Indexes
        builder.HasIndex(rp => new { rp.RoleId, rp.PermissionId })
            .IsUnique()
            .HasDatabaseName("IX_RolePermissions_RoleId_PermissionId");

        builder.HasIndex(rp => rp.RoleId)
            .HasDatabaseName("IX_RolePermissions_RoleId");

        builder.HasIndex(rp => rp.PermissionId)
            .HasDatabaseName("IX_RolePermissions_PermissionId");

        // Relationships - Use Restrict to avoid cascade cycles
        builder.HasOne(rp => rp.Role)
            .WithMany(r => r.RolePermissions)
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(rp => rp.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(rp => rp.PermissionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
