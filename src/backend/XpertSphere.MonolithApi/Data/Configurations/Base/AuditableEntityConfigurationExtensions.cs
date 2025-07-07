using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using XpertSphere.MonolithApi.Models.Base;

namespace XpertSphere.MonolithApi.Data.Configurations.Base;

public static class AuditableEntityConfigurationExtensions
{
    public static void ConfigureAuditableEntity<T>(this EntityTypeBuilder<T> builder) 
        where T : class, new()
    {
        // Configure default values at database level
        builder.Property("Id")
            .HasDefaultValueSql("NEWID()");

        builder.Property("CreatedAt")
            .HasDefaultValueSql("GETUTCDATE()");

        // Configure audit relationships
        builder.HasOne("XpertSphere.MonolithApi.Models.User", "CreatedByUser")
            .WithMany()
            .HasForeignKey("CreatedBy")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne("XpertSphere.MonolithApi.Models.User", "UpdatedByUser")
            .WithMany()
            .HasForeignKey("UpdatedBy")
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes for performance
        builder.HasIndex("CreatedAt");
        builder.HasIndex("CreatedBy");
    }
}
