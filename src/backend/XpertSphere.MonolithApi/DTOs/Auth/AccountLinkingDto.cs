using System.ComponentModel.DataAnnotations;

namespace XpertSphere.MonolithApi.DTOs.Auth;

public record AccountLinkingDto
{
    [Required(ErrorMessage = "L'email est obligatoire")]
    [EmailAddress(ErrorMessage = "Format d'email invalide")]
    public required string Email { get; init; }

    [Required(ErrorMessage = "Le jeton Entra ID est obligatoire")]
    public required string EntraIdToken { get; init; }

    public string? ExternalId { get; init; }

    public string? AuthType { get; init; } // "B2B" or "B2C"

    public bool OverwriteExisting { get; init; } = false;
}