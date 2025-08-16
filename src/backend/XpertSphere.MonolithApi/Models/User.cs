using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using XpertSphere.MonolithApi.Models.Base;
using XpertSphere.MonolithApi.Utils;

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
    
    public int? YearsOfExperience { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? DesiredSalary { get; set; }

    public DateTime? Availability { get; set; }

    // Authentication & Security
    [MaxLength(255)]
    public string? ExternalId { get; set; }

    [MaxLength(500)]
    public string? RefreshToken { get; set; }

    public DateTime? RefreshTokenExpiry { get; set; }

    [MaxLength(100)]
    public string? PasswordResetToken { get; set; }

    public DateTime? PasswordResetTokenExpiry { get; set; }

    [MaxLength(100)]
    public string? EmailConfirmationToken { get; set; }

    public DateTime? EmailConfirmationTokenExpiry { get; set; }

    // Account Status & Security
    public DateTime? LastPasswordChangeAt { get; set; }

    public int FailedLoginAttempts { get; set; }

    public DateTime? AccountLockedUntil { get; set; }

    // GDPR & Privacy
    public DateTime? ConsentGivenAt { get; set; }

    public DateTime? ConsentWithdrawnAt { get; set; }

    public DateTime? DataRetentionUntil { get; set; }

    public bool IsAnonymized { get; set; } = false;

    // Profile completeness tracking
    public int ProfileCompletionPercentage { get; set; }

    public DateTime? ProfileLastUpdatedAt { get; set; }

    // Communication preferences
    public bool EmailNotificationsEnabled { get; set; } = true;

    public bool SmsNotificationsEnabled { get; set; } = false;

    [MaxLength(20)]
    public string? PreferredLanguage { get; set; } = "fr";

    [MaxLength(50)]
    public string? TimeZone { get; set; } = "UTC";

    // Common properties
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }

    // Navigation properties
    [ForeignKey("CreatedBy")]
    [JsonIgnore]
    public virtual User? CreatedByUser { get; set; }

    [ForeignKey("UpdatedBy")]
    [JsonIgnore]
    public virtual User? UpdatedByUser { get; set; }

    [ForeignKey("OrganizationId")]
    [JsonIgnore]
    public virtual Organization? Organization { get; set; }

    [JsonIgnore]
    public virtual ICollection<UserRole> UserRoles { get; set; } = [];
    
    [JsonIgnore]
    public virtual ICollection<Training> Trainings { get; set; } = [];

    [JsonIgnore]
    public virtual ICollection<Experience> Experiences { get; set; } = [];

    // Computed properties
    [NotMapped]
    public string FullName => $"{FirstName} {LastName}";

    [NotMapped]
    public bool IsAccountLocked => AccountLockedUntil.HasValue && AccountLockedUntil > DateTime.UtcNow;

    [NotMapped]
    public bool IsTokenValid => RefreshTokenExpiry.HasValue && RefreshTokenExpiry > DateTime.UtcNow;

    [NotMapped]
    public bool HasValidConsent => ConsentGivenAt.HasValue && !ConsentWithdrawnAt.HasValue;
    
    [NotMapped]
    public bool IsCandidate => !OrganizationId.HasValue;

    [NotMapped]
    public bool IsOrganizationalUser => OrganizationId.HasValue;

    [NotMapped]
    public bool IsXpertSphereUser => Organization?.Name == Constants.XPERTSPHERE;

    [NotMapped]
    public bool IsClientUser => OrganizationId.HasValue && Organization?.Name != Constants.XPERTSPHERE;

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
        
        if (!IsCandidate) return;
        const int totalFields = 12;
        var completedFields = 0;

        if (!string.IsNullOrEmpty(FirstName)) completedFields++;
        if (!string.IsNullOrEmpty(LastName)) completedFields++;
        if (!string.IsNullOrEmpty(Email)) completedFields++;
        if (!string.IsNullOrEmpty(PhoneNumber)) completedFields++;
        if (Address != null && !string.IsNullOrEmpty(Address.Street)) completedFields++;
        if (!string.IsNullOrEmpty(Skills)) completedFields++;
        if (YearsOfExperience.HasValue) completedFields++;
        if (!string.IsNullOrEmpty(CvPath)) completedFields++;
        if (!string.IsNullOrEmpty(LinkedInProfile)) completedFields++;
        if (Experiences is { Count: > 0 }) completedFields++;
        if (Trainings?.Count > 0) completedFields++;
        if (Availability.HasValue) completedFields++;

        ProfileCompletionPercentage = (completedFields * 100) / totalFields;
        ProfileLastUpdatedAt = DateTime.UtcNow;
    }
}