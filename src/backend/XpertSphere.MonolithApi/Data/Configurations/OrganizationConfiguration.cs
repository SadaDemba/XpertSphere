using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using XpertSphere.MonolithApi.Data.Configurations.Base;
using XpertSphere.MonolithApi.Enums;
using XpertSphere.MonolithApi.Extensions;
using XpertSphere.MonolithApi.Models;

namespace XpertSphere.MonolithApi.Data.Configurations;

public class OrganizationConfiguration : AuditableEntityConfiguration<Organization>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("Organizations");

        // Configure enum as string
        builder.Property(o => o.Size)
            .HasConversion(
                v => v.HasValue ? v.Value.ToStringValue() : null,
                v => !string.IsNullOrEmpty(v) ? v.ToOrganizationSize() : (OrganizationSize?)null)
            .HasMaxLength(50);

        // Configure Address as owned entity (ComplexType)
        builder.OwnsOne(
            o => o.Address,
            address =>
            {
                address.Property(a => a.StreetNumber).HasColumnName("Address_StreetNumber");
                address.Property(a => a.StreetName).HasColumnName("Address_StreetName");
                address.Property(a => a.City).HasColumnName("Address_City");
                address.Property(a => a.PostalCode).HasColumnName("Address_PostalCode");
                address.Property(a => a.Region).HasColumnName("Address_Region");
                address
                    .Property(a => a.Country)
                    .HasColumnName("Address_Country")
                    .HasDefaultValue("France");
                address.Property(a => a.AddressLine2).HasColumnName("Address_AddressLine2");

                // Ignore computed properties
                address.Ignore(a => a.FullAddress);
                address.Ignore(a => a.IsEmpty);
                address.Ignore(a => a.MultiLineAddress);
            }
        );

        // Indexes
        builder.HasIndex(o => o.Code).IsUnique().HasDatabaseName("IX_Organizations_Code");
        builder.HasIndex(o => o.Name).HasDatabaseName("IX_Organizations_Name");
        builder.HasIndex(o => o.Industry).HasDatabaseName("IX_Organizations_Industry");
        builder.HasIndex(o => o.Size).HasDatabaseName("IX_Organizations_Size");
        builder.HasIndex(o => o.IsActive).HasDatabaseName("IX_Organizations_IsActive");

        // Relationships
        builder
            .HasMany(o => o.Users)
            .WithOne(u => u.Organization)
            .HasForeignKey(u => u.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
