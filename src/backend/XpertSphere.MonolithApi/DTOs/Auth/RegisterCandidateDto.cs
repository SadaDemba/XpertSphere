using System.ComponentModel.DataAnnotations;
using XpertSphere.MonolithApi.Models.Base;

namespace XpertSphere.MonolithApi.DTOs.Auth;

public record RegisterCandidateDto
{
    [Required]
    [EmailAddress]
    public required string Email { get; init; }

    [Required]
    [MinLength(6)]
    public required string Password { get; init; }

    [Required]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match")]
    public required string ConfirmPassword { get; init; }

    [Required]
    [MaxLength(100)]
    public required string FirstName { get; init; }

    [Required]
    [MaxLength(100)]
    public required string LastName { get; init; }

    [Phone]
    public string? PhoneNumber { get; init; }

    // Address Information (matching Address model)
    public string? StreetNumber { get; init; }
    public string? Street { get; init; }
    public string? City { get; init; }
    public string? PostalCode { get; init; }
    public string? Region { get; init; }
    public string? Country { get; init; } = "France";
    public string? AddressLine2 { get; init; }

    // Professional Information
    public string? Skills { get; init; }
    public int? YearsOfExperience { get; init; }
    public decimal? DesiredSalary { get; init; }
    public DateTime? Availability { get; init; }
    public string? LinkedInProfile { get; init; }

    // Training and Experience
    public List<Training>? Trainings { get; init; }
    public List<Experience>? Experiences { get; init; }

    // Communication Preferences
    public bool EmailNotificationsEnabled { get; init; } = true;
    public bool SmsNotificationsEnabled { get; init; } = false;
    public string PreferredLanguage { get; init; } = "fr";
    public string TimeZone { get; init; } = "UTC";

    // Legal
    [Required]
    public bool AcceptTerms { get; init; } = false;

    [Required]
    public bool AcceptPrivacyPolicy { get; init; } = false;

    public DateTime? ConsentGivenAt { get; init; }

    // Navigation
    public string? ReturnUrl { get; init; }
    
    // Entra ID specific fields
    public bool ForceLocalRegistration { get; init; } = false;
    public string? OrganizationDomain { get; init; }
    public string? EntraIdToken { get; init; }
    public string? ExternalId { get; init; }
    public bool LinkToEntraId { get; init; } = false;
    public Dictionary<string, string>? EntraIdMetadata { get; init; }
}