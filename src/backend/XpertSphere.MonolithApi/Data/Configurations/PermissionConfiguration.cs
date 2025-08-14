using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using XpertSphere.MonolithApi.Data.Configurations.Base;
using XpertSphere.MonolithApi.Enums;
using XpertSphere.MonolithApi.Extensions;
using XpertSphere.MonolithApi.Models;

namespace XpertSphere.MonolithApi.Data.Configurations;

public class PermissionConfiguration : AuditableEntityConfiguration<Permission>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions");

        // Configure enums as strings
        builder.Property(p => p.Action)
            .HasConversion(
                v => v.ToStringValue(),
                v => v.ToPermissionAction())
            .HasMaxLength(50);

        builder.Property(p => p.Scope)
            .HasConversion(
                v => v.HasValue ? v.Value.ToStringValue() : null,
                v => !string.IsNullOrEmpty(v) ? v.ToPermissionScope() : (PermissionScope?)null)
            .HasMaxLength(100);

        // Indexes
        builder.HasIndex(p => new { p.Resource, p.Action, p.Scope })
            .IsUnique()
            .HasDatabaseName("IX_Permissions_Resource_Action_Scope");

        builder.HasIndex(p => p.Resource)
            .HasDatabaseName("IX_Permissions_Resource");

        builder.HasIndex(p => p.Category)
            .HasDatabaseName("IX_Permissions_Category");

        // Relationships - Use Restrict to avoid cascade cycles
        builder.HasMany(p => p.RolePermissions)
            .WithOne(rp => rp.Permission)
            .HasForeignKey(rp => rp.PermissionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
