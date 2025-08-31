using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using XpertSphere.MonolithApi.Data.Configurations.Base;
using XpertSphere.MonolithApi.Enums;
using XpertSphere.MonolithApi.Extensions;
using XpertSphere.MonolithApi.Models;

namespace XpertSphere.MonolithApi.Data.Configurations;

public class ApplicationConfiguration : AuditableEntityConfiguration<Application>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Application> builder)
    {
        builder.ToTable("Applications", t =>
        {
            t.HasCheckConstraint(
                "CK_Application_Rating",
                "[Rating] IS NULL OR ([Rating] >= 1 AND [Rating] <= 5)");

            t.HasCheckConstraint(
                "CK_Application_AppliedAt",
                "[AppliedAt] <= GETUTCDATE()");

            t.HasCheckConstraint(
                "CK_Application_LastUpdatedAt",
                "[LastUpdatedAt] IS NULL OR [LastUpdatedAt] >= [AppliedAt]");
        });

        // Configure enum as string
        builder.Property(a => a.CurrentStatus)
            .HasConversion(
                v => v.ToStringValue(),
                v => v.ToApplicationStatus())
            .HasMaxLength(50);

        // Configure computed properties to be ignored
        builder.Ignore(a => a.IsActive);
        builder.Ignore(a => a.IsCompleted);
        builder.Ignore(a => a.IsInProgress);
        builder.Ignore(a => a.StatusDisplayName);

        // Indexes for performance
        builder.HasIndex(a => a.CurrentStatus).HasDatabaseName("IX_Applications_CurrentStatus");
        builder.HasIndex(a => a.Rating).HasDatabaseName("IX_Applications_Rating");
        builder.HasIndex(a => a.AppliedAt).HasDatabaseName("IX_Applications_AppliedAt");
        builder.HasIndex(a => a.LastUpdatedAt).HasDatabaseName("IX_Applications_LastUpdatedAt");
        builder.HasIndex(a => a.JobOfferId).HasDatabaseName("IX_Applications_JobOfferId");
        builder.HasIndex(a => a.CandidateId).HasDatabaseName("IX_Applications_CandidateId");

        // Composite indexes for common queries
        builder.HasIndex(a => new { a.JobOfferId, a.CurrentStatus })
            .HasDatabaseName("IX_Applications_JobOfferId_Status");
        
        builder.HasIndex(a => new { a.CandidateId, a.CurrentStatus })
            .HasDatabaseName("IX_Applications_CandidateId_Status");

        builder.HasIndex(a => new { a.JobOfferId, a.CandidateId })
            .IsUnique()
            .HasDatabaseName("IX_Applications_JobOfferId_CandidateId");

        // Relationships
        builder
            .HasOne(a => a.JobOffer)
            .WithMany()
            .HasForeignKey(a => a.JobOfferId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(a => a.Candidate)
            .WithMany()
            .HasForeignKey(a => a.CandidateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(a => a.AssignedTechnicalEvaluator)
            .WithMany()
            .HasForeignKey(a => a.AssignedTechnicalEvaluatorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(a => a.AssignedManager)
            .WithMany()
            .HasForeignKey(a => a.AssignedManagerId)
            .OnDelete(DeleteBehavior.Restrict);

        // One-to-many with ApplicationStatusHistory
        builder
            .HasMany(a => a.StatusHistory)
            .WithOne(h => h.Application)
            .HasForeignKey(h => h.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade); // When application is deleted, cascade delete history
    }
}