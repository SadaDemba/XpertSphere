using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using XpertSphere.MonolithApi.Models;
using XpertSphere.MonolithApi.Enums;
using XpertSphere.MonolithApi.Extensions;
using XpertSphere.MonolithApi.Data.Configurations.Base;

namespace XpertSphere.MonolithApi.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users", t =>
        {
            t.HasCheckConstraint(
                "CK_User_Internal_OrganizationRequired",
                "([UserType] = 'RECRUITER' AND [OrganizationId] IS NOT NULL) OR" +
                "([UserType] = 'MANAGER' AND [OrganizationId] IS NOT NULL) OR" +
                "([UserType] = 'COLLABORATOR' AND [OrganizationId] IS NOT NULL)" +
                "OR ([UserType] = 'CANDIDATE' AND [OrganizationId] IS NULL)");

            t.HasCheckConstraint(
                "CK_User_Experience",
                "[Experience] IS NULL OR [Experience] >= 0");

            t.HasCheckConstraint(
                "CK_User_DesiredSalary",
                "[DesiredSalary] IS NULL OR [DesiredSalary] > 0");
        });


        // Configure enum as string
        builder.Property(u => u.UserType)
            .HasConversion(
                v => v.ToStringValue(),
                v => v.ToUserType())
            .HasMaxLength(50);

        // Configure Address as owned entity (ComplexType)
        builder.OwnsOne(
            u => u.Address,
            address =>
            {
                address.Property(a => a.StreetNumber).HasColumnName("Address_StreetNumber");
                address.Property(a => a.Street).HasColumnName("Address_StreetName");
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
        builder.HasIndex(u => u.Email).IsUnique().HasDatabaseName("IX_Users_Email");

        builder
            .HasIndex(u => new { u.OrganizationId, u.EmployeeId })
            .IsUnique()
            .HasDatabaseName("IX_Users_OrganizationId_EmployeeId")
            .HasFilter("[OrganizationId] IS NOT NULL AND [EmployeeId] IS NOT NULL");

        builder.HasIndex(u => u.UserType).HasDatabaseName("IX_Users_UserType");

        builder.HasIndex(u => u.LastLoginAt).HasDatabaseName("IX_Users_LastLoginAt");

        // Relationships - Use Restrict to avoid cascade cycles
        builder
            .HasOne(u => u.Organization)
            .WithMany(o => o.Users)
            .HasForeignKey(u => u.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasMany(u => u.UserRoles)
            .WithOne(ur => ur.User)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        
        ConfigureAuditProperties(builder);

    }

    private static void ConfigureAuditProperties(EntityTypeBuilder<User> builder)
    {
        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(e => e.CreatedByUser)
            .WithMany()
            .HasForeignKey(e => e.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.UpdatedByUser)
            .WithMany()
            .HasForeignKey(e => e.UpdatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.CreatedAt)
            .HasDatabaseName("IX_Users_CreatedAt");
        builder.HasIndex(e => e.CreatedBy)
            .HasDatabaseName("IX_Users_CreatedBy");
    }
}
