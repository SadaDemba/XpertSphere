using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using XpertSphere.MonolithApi.Models;
using XpertSphere.MonolithApi.Enums;
using XpertSphere.MonolithApi.Extensions;
using XpertSphere.MonolithApi.Data.Configurations.Base;

namespace XpertSphere.MonolithApi.Data.Configurations;

public class UserConfiguration : AuditableEntityConfiguration<User>
{
    protected override void ConfigureEntity(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

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

        // Check constraints
        builder.HasCheckConstraint(
            "CK_User_Internal_OrganizationRequired",
            "([UserType] = 'INTERNAL') OR ([UserType] = 'EXTERNAL' AND [OrganizationId] IS NULL)"
        );
        builder.HasCheckConstraint(
            "CK_User_Experience",
            "[Experience] IS NULL OR [Experience] >= 0"
        );
        builder.HasCheckConstraint(
            "CK_User_DesiredSalary",
            "[DesiredSalary] IS NULL OR [DesiredSalary] > 0"
        );
    }
}
