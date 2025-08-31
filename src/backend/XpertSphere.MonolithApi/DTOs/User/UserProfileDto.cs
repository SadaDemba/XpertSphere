using XpertSphere.MonolithApi.Enums;

namespace XpertSphere.MonolithApi.DTOs.User;

/// <summary>
/// DTO for detailed user profile information, primarily used for candidate profiles
/// </summary>
public record UserProfileDto
{
    public Guid Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    // Profile completion
    public int ProfileCompletionPercentage { get; set; }
    public DateTime? ProfileLastUpdatedAt { get; set; }

    // Address information
    public AddressDto? Address { get; set; }

    // Professional information
    public string? LinkedInProfile { get; set; }
    public string? Skills { get; set; }
    
    public int? Experience { get; set; }
    public decimal? DesiredSalary { get; set; }
    public DateTime? Availability { get; set; }
    public string? CvPath { get; set; }

    // Organization info (for internal users)
    public Guid? OrganizationId { get; set; }
    public string? OrganizationName { get; set; }
    public string? EmployeeId { get; set; }
    public string? Department { get; set; }
    public DateTime? HireDate { get; set; }

    // Communication preferences
    public bool EmailNotificationsEnabled { get; set; }
    public bool SmsNotificationsEnabled { get; set; }
    public string? PreferredLanguage { get; set; }
    public string? TimeZone { get; set; }

    // Computed properties
    public string FullName => $"{FirstName} {LastName}";
    public bool IsProfileComplete => ProfileCompletionPercentage >= 80;
    public bool IsAvailable => Availability.HasValue && Availability <= DateTime.UtcNow.AddMonths(1);

    // Roles and permissions
    public List<string> Roles { get; set; } = [];

    // GDPR compliance
    public DateTime? ConsentGivenAt { get; set; }
    public bool HasValidConsent => ConsentGivenAt.HasValue;
}
