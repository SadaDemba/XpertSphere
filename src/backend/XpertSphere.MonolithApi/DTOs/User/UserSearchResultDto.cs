using XpertSphere.MonolithApi.Enums;

namespace XpertSphere.MonolithApi.DTOs.User;

/// <summary>
/// Lightweight DTO for user search results and lists
/// </summary>
public record UserSearchResultDto
{
    public Guid Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    // Organization info (for context)
    public Guid? OrganizationId { get; set; }
    public string? OrganizationName { get; set; }
    public string? Department { get; set; }

    // Candidate summary info
    public string? Skills { get; set; }
    public int? Experience { get; set; }
    public decimal? DesiredSalary { get; set; }
    public DateTime? Availability { get; set; }

    // Profile metrics
    public int ProfileCompletionPercentage { get; set; }

    // Location summary
    public string? City { get; set; }
    public string? Country { get; set; }

    // Computed properties
    public string FullName => $"{FirstName} {LastName}";
    public bool IsAvailable => Availability.HasValue && Availability <= DateTime.UtcNow.AddMonths(1);
    public string ExperienceDisplay => Experience.HasValue ? $"{Experience} years" : "Not specified";
}
