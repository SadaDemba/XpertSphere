using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using XpertSphere.MonolithApi.Enums;
using XpertSphere.MonolithApi.Extensions;
using XpertSphere.MonolithApi.Models;

namespace XpertSphere.MonolithApi.Data.Configurations;

public class ApplicationStatusHistoryConfiguration : IEntityTypeConfiguration<ApplicationStatusHistory>
{
    public void Configure(EntityTypeBuilder<ApplicationStatusHistory> builder)
    {
        builder.ToTable("ApplicationStatusHistories", t =>
        {
            t.HasCheckConstraint(
                "CK_ApplicationStatusHistory_Rating",
                "[Rating] IS NULL OR ([Rating] >= 1 AND [Rating] <= 5)");

            t.HasCheckConstraint(
                "CK_ApplicationStatusHistory_UpdatedAt",
                "[UpdatedAt] <= DATEADD(minute, 5, GETUTCDATE())"); // Allow 5 minutes tolerance for clock skew
        });

        // Configure enum as string
        builder.Property(h => h.Status)
            .HasConversion(
                v => v.ToStringValue(),
                v => v.ToApplicationStatus())
            .HasMaxLength(50);

        // Set default value for UpdatedAt
        builder.Property(h => h.UpdatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        // Configure computed properties to be ignored
        builder.Ignore(h => h.StatusDisplayName);
        builder.Ignore(h => h.HasRating);
        builder.Ignore(h => h.RatingDescription);

        // Indexes for performance
        builder.HasIndex(h => h.ApplicationId).HasDatabaseName("IX_ApplicationStatusHistories_ApplicationId");
        builder.HasIndex(h => h.Status).HasDatabaseName("IX_ApplicationStatusHistories_Status");
        builder.HasIndex(h => h.UpdatedAt).HasDatabaseName("IX_ApplicationStatusHistories_UpdatedAt");
        builder.HasIndex(h => h.UpdatedByUserId).HasDatabaseName("IX_ApplicationStatusHistories_UpdatedByUserId");
        builder.HasIndex(h => h.Rating).HasDatabaseName("IX_ApplicationStatusHistories_Rating");

        // Composite indexes for common queries
        builder.HasIndex(h => new { h.ApplicationId, h.UpdatedAt })
            .HasDatabaseName("IX_ApplicationStatusHistories_ApplicationId_UpdatedAt");

        builder.HasIndex(h => new { h.ApplicationId, h.Status })
            .HasDatabaseName("IX_ApplicationStatusHistories_ApplicationId_Status");

        // Relationships
        builder
            .HasOne(h => h.Application)
            .WithMany(a => a.StatusHistory)
            .HasForeignKey(h => h.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(h => h.UpdatedByUser)
            .WithMany()
            .HasForeignKey(h => h.UpdatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}