using System.ComponentModel.DataAnnotations;
using XpertSphere.MonolithApi.Models;

namespace XpertSphere.MonolithApi.DTOs.Auth;

public record RegisterDto
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

    [Required(ErrorMessage = "La liste des formations est obligatoire")]
    public required List<Training>? Trainings { get; init; }

    [Required(ErrorMessage = "La liste des expériences est obligatoire")]
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