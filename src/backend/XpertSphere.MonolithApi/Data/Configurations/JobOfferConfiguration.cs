using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using XpertSphere.MonolithApi.Data.Configurations.Base;
using XpertSphere.MonolithApi.Enums;
using XpertSphere.MonolithApi.Extensions;
using XpertSphere.MonolithApi.Models;

namespace XpertSphere.MonolithApi.Data.Configurations;

public class JobOfferConfiguration : AuditableEntityConfiguration<JobOffer>
{
    protected override void ConfigureEntity(EntityTypeBuilder<JobOffer> builder)
    {
        builder.ToTable("JobOffers", t =>
        {
            t.HasCheckConstraint(
                "CK_JobOffer_SalaryRange",
                "[SalaryMin] IS NULL OR [SalaryMax] IS NULL OR [SalaryMin] <= [SalaryMax]");

            t.HasCheckConstraint(
                "CK_JobOffer_ExpiresAt",
                "[ExpiresAt] IS NULL OR [ExpiresAt] > [CreatedAt]");
        });

        // Configure enums as strings
        builder.Property(jo => jo.WorkMode)
            .HasConversion(
                v => v.ToStringValue(),
                v => v.ToWorkMode())
            .HasMaxLength(50);

        builder.Property(jo => jo.ContractType)
            .HasConversion(
                v => v.ToStringValue(),
                v => v.ToContractType())
            .HasMaxLength(50);

        builder.Property(jo => jo.Status)
            .HasConversion(
                v => v.ToStringValue(),
                v => v.ToJobOfferStatus())
            .HasMaxLength(50);

        // Configure computed properties to be ignored
        builder.Ignore(jo => jo.IsActive);
        builder.Ignore(jo => jo.IsExpired);
        builder.Ignore(jo => jo.RequiresLocation);

        // Indexes for performance
        builder.HasIndex(jo => jo.Title).HasDatabaseName("IX_JobOffers_Title");
        builder.HasIndex(jo => jo.Location).HasDatabaseName("IX_JobOffers_Location");
        builder.HasIndex(jo => jo.WorkMode).HasDatabaseName("IX_JobOffers_WorkMode");
        builder.HasIndex(jo => jo.ContractType).HasDatabaseName("IX_JobOffers_ContractType");
        builder.HasIndex(jo => jo.Status).HasDatabaseName("IX_JobOffers_Status");
        builder.HasIndex(jo => jo.PublishedAt).HasDatabaseName("IX_JobOffers_PublishedAt");
        builder.HasIndex(jo => jo.ExpiresAt).HasDatabaseName("IX_JobOffers_ExpiresAt");
        builder.HasIndex(jo => jo.OrganizationId).HasDatabaseName("IX_JobOffers_OrganizationId");
        builder.HasIndex(jo => jo.CreatedByUserId).HasDatabaseName("IX_JobOffers_CreatedByUserId");

        // Composite indexes for common queries
        builder.HasIndex(jo => new { jo.Status, jo.PublishedAt })
            .HasDatabaseName("IX_JobOffers_Status_PublishedAt");
        
        builder.HasIndex(jo => new { jo.OrganizationId, jo.Status })
            .HasDatabaseName("IX_JobOffers_OrganizationId_Status");

        // Relationships
        builder
            .HasOne(jo => jo.Organization)
            .WithMany()
            .HasForeignKey(jo => jo.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(jo => jo.CreatedByUserNavigation)
            .WithMany()
            .HasForeignKey(jo => jo.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // One-to-many with Applications
        builder
            .HasMany<Application>()
            .WithOne(a => a.JobOffer)
            .HasForeignKey(a => a.JobOfferId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}