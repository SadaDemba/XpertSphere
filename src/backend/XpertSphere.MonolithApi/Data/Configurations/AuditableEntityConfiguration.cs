using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using XpertSphere.MonolithApi.Models.Base;

namespace XpertSphere.MonolithApi.Data.Configurations;

public class AuditableEntityConfiguration : IEntityTypeConfiguration<AuditableEntity>
{
    public void Configure(EntityTypeBuilder<AuditableEntity> builder)
    {
        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        // Configure audit relationships (pas possible avec DataAnnotations)
        builder.HasOne(e => e.CreatedByUser)
            .WithMany()
            .HasForeignKey(e => e.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.UpdatedByUser)
            .WithMany()
            .HasForeignKey(e => e.UpdatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes for performance (pas possible avec DataAnnotations)
        builder.HasIndex(e => e.CreatedAt);
        builder.HasIndex(e => e.CreatedBy);
    }
}
