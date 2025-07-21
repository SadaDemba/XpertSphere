using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using XpertSphere.MonolithApi.Models.Base;
using XpertSphere.MonolithApi.Enums;
using Microsoft.AspNetCore.Identity;

namespace XpertSphere.MonolithApi.Models;

public class User : IdentityUser<Guid>, IAuditableEntity
{
    [Required]
    [MaxLength(100)]
    public required string FirstName { get; set; }

    [Required]
    [MaxLength(100)]
    public required string LastName { get; set; }

    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    public Address Address { get; set; } = new();

    [Required]
    public UserType UserType { get; set; }

    public Guid? OrganizationId { get; set; }

    [MaxLength(50)]
    public string? EmployeeId { get; set; }

    [MaxLength(100)]
    public string? Department { get; set; }

    public DateTime? HireDate { get; set; }

    [MaxLength(255)]
    public string? LinkedInProfile { get; set; }

    [MaxLength(500)]
    public string? CvPath { get; set; }

    public string? Skills { get; set; }

    public int? Experience { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? DesiredSalary { get; set; }

    public DateTime? Availability { get; set; }

    // Authentication & Security - NEW
    [MaxLength(500)]
    public string? RefreshToken { get; set; }

    public DateTime? RefreshTokenExpiry { get; set; }

    [MaxLength(100)]
    public string? PasswordResetToken { get; set; }

    public DateTime? PasswordResetTokenExpiry { get; set; }

    [MaxLength(100)]
    public string? EmailConfirmationToken { get; set; }

    public DateTime? EmailConfirmationTokenExpiry { get; set; }

    // Account Status & Security - NEW
    public DateTime? LastPasswordChangeAt { get; set; }

    public int FailedLoginAttempts { get; set; } = 0;

    public DateTime? AccountLockedUntil { get; set; }

    // GDPR & Privacy - NEW
    public DateTime? ConsentGivenAt { get; set; }

    public DateTime? ConsentWithdrawnAt { get; set; }

    public DateTime? DataRetentionUntil { get; set; }

    public bool IsAnonymized { get; set; } = false;

    // Profile completeness tracking - NEW
    public int ProfileCompletionPercentage { get; set; } = 0;

    public DateTime? ProfileLastUpdatedAt { get; set; }

    // Communication preferences - NEW
    public bool EmailNotificationsEnabled { get; set; } = true;

    public bool SmsNotificationsEnabled { get; set; } = false;

    [MaxLength(20)]
    public string? PreferredLanguage { get; set; } = "en";

    [MaxLength(50)]
    public string? TimeZone { get; set; } = "UTC";

    // Common properties
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }

    // Navigation properties
    [ForeignKey("CreatedBy")]
    public virtual User? CreatedByUser { get; set; }

    [ForeignKey("UpdatedBy")]
    public virtual User? UpdatedByUser { get; set; }

    [ForeignKey("OrganizationId")]
    public virtual Organization? Organization { get; set; }

    public virtual ICollection<UserRole> UserRoles { get; set; } = [];

    // Computed properties
    [NotMapped]
    public string FullName => $"{FirstName} {LastName}";

    [NotMapped]
    public bool IsAccountLocked => AccountLockedUntil.HasValue && AccountLockedUntil > DateTime.UtcNow;

    [NotMapped]
    public bool IsTokenValid => RefreshTokenExpiry.HasValue && RefreshTokenExpiry > DateTime.UtcNow;

    [NotMapped]
    public bool HasValidConsent => ConsentGivenAt.HasValue && !ConsentWithdrawnAt.HasValue;

    // Methods for token management
    public void SetRefreshToken(string token, TimeSpan expiry)
    {
        RefreshToken = token;
        RefreshTokenExpiry = DateTime.UtcNow.Add(expiry);
    }

    public void ClearRefreshToken()
    {
        RefreshToken = null;
        RefreshTokenExpiry = null;
    }

    public void SetPasswordResetToken(string token, TimeSpan expiry)
    {
        PasswordResetToken = token;
        PasswordResetTokenExpiry = DateTime.UtcNow.Add(expiry);
    }

    public void ClearPasswordResetToken()
    {
        PasswordResetToken = null;
        PasswordResetTokenExpiry = null;
    }

    public void IncrementFailedLogin()
    {
        FailedLoginAttempts++;
        if (FailedLoginAttempts >= 5)
        {
            AccountLockedUntil = DateTime.UtcNow.AddMinutes(10);
        }
    }

    public void ResetFailedLogins()
    {
        FailedLoginAttempts = 0;
        AccountLockedUntil = null;
    }

    public void UpdateLastLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        ResetFailedLogins();
    }

    public void CalculateProfileCompletion()
    {
        var totalFields = 10; // Adjust based on required fields
        var completedFields = 0;

        if (!string.IsNullOrEmpty(FirstName)) completedFields++;
        if (!string.IsNullOrEmpty(LastName)) completedFields++;
        if (!string.IsNullOrEmpty(Email)) completedFields++;
        if (!string.IsNullOrEmpty(PhoneNumber)) completedFields++;
        if (Address != null && !string.IsNullOrEmpty(Address.Street)) completedFields++;
        if (!string.IsNullOrEmpty(Skills)) completedFields++;
        if (Experience.HasValue) completedFields++;
        if (!string.IsNullOrEmpty(CvPath)) completedFields++;
        if (!string.IsNullOrEmpty(LinkedInProfile)) completedFields++;
        if (Availability.HasValue) completedFields++;

        ProfileCompletionPercentage = (completedFields * 100) / totalFields;
        ProfileLastUpdatedAt = DateTime.UtcNow;
    }
}