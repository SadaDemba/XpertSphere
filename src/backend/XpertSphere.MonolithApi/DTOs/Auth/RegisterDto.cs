using System.ComponentModel.DataAnnotations;
using XpertSphere.MonolithApi.Models.Base;

namespace XpertSphere.MonolithApi.DTOs.Auth;

public record RegisterDto
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
    
    [Required]
    public required List<Training>? Trainings { get; init; }
    
    [Required]
    public required List<Experience>? Experiences { get; init; }

    public bool AcceptTerms { get; init; } = false;

    public bool AcceptPrivacyPolicy { get; init; } = false;
    
    public string? ReturnUrl { get; init; }
    
    // Entra ID specific fields
    public bool ForceLocalRegistration { get; init; } = false;
    public string? OrganizationDomain { get; init; }
    public string? EntraIdToken { get; init; }
    public string? ExternalId { get; init; }
    public bool LinkToEntraId { get; init; } = false;
    public Dictionary<string, string>? EntraIdMetadata { get; init; }
}