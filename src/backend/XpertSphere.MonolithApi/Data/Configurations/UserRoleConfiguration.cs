using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using XpertSphere.MonolithApi.Data.Configurations.Base;
using XpertSphere.MonolithApi.Models;

namespace XpertSphere.MonolithApi.Data.Configurations;

public class UserRoleConfiguration : AuditableEntityConfiguration<UserRole>
{
    protected override void ConfigureEntity(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("UserRoles", ur => 
            ur.HasCheckConstraint("CK_UserRole_ExpiresAt",
                "[ExpiresAt] IS NULL OR [ExpiresAt] > [AssignedAt]"));

        builder.Property(ur => ur.Id)
            .HasDefaultValueSql("NEWID()");

        // Indexes
        builder
            .HasIndex(ur => new { ur.UserId, ur.RoleId })
            .IsUnique()
            .HasDatabaseName("IX_UserRoles_UserId_RoleId");

        builder.HasIndex(ur => ur.AssignedAt).HasDatabaseName("IX_UserRoles_AssignedAt");
        builder.HasIndex(ur => ur.ExpiresAt).HasDatabaseName("IX_UserRoles_ExpiresAt");
        builder.HasIndex(ur => ur.IsActive).HasDatabaseName("IX_UserRoles_IsActive");

        // Relationships - Use Restrict to avoid cascade cycles
        builder
            .HasOne(ur => ur.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(ur => ur.AssignedByUser)
            .WithMany()
            .HasForeignKey(ur => ur.AssignedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
