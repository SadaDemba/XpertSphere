using System.ComponentModel.DataAnnotations;
using XpertSphere.MonolithApi.DTOs.ExperienceDtos;
using XpertSphere.MonolithApi.DTOs.TrainingDtos;
using XpertSphere.MonolithApi.Enums;

namespace XpertSphere.MonolithApi.DTOs.Auth;

public record RegisterCandidateDto
{
    [Required(ErrorMessage = "L'email est obligatoire")]
    [EmailAddress(ErrorMessage = "Format d'email invalide")]
    public required string Email { get; init; }

    [Required(ErrorMessage = "Le mot de passe est obligatoire")]
    [MinLength(6, ErrorMessage = "Le mot de passe doit contenir au moins 6 caractères")]
    public required string Password { get; init; }

    [Required(ErrorMessage = "La confirmation du mot de passe est obligatoire")]
    [Compare(nameof(Password), ErrorMessage = "Les mots de passe ne correspondent pas")]
    public required string ConfirmPassword { get; init; }

    [Required(ErrorMessage = "Le prénom est obligatoire")]
    [MaxLength(100, ErrorMessage = "Le prénom ne peut pas dépasser 100 caractères")]
    public required string FirstName { get; init; }

    [Required(ErrorMessage = "Le nom est obligatoire")]
    [MaxLength(100, ErrorMessage = "Le nom ne peut pas dépasser 100 caractères")]
    public required string LastName { get; init; }

    [Phone(ErrorMessage = "Format de numéro de téléphone invalide")]
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
    public Currency? DesiredSalaryCurrency { get; init; }
    public DateTime? Availability { get; init; }
    public string? LinkedInProfile { get; init; }

    // Training and Experience
    public List<CreateTrainingDto>? Trainings { get; init; }
    public List<CreateExperienceDto>? Experiences { get; init; }

    // Communication Preferences
    public bool EmailNotificationsEnabled { get; init; } = true;
    public bool SmsNotificationsEnabled { get; init; } = false;
    public string PreferredLanguage { get; init; } = "fr";
    public string TimeZone { get; init; } = "UTC";

    // Legal
    public bool AcceptTerms { get; init; } = false;

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