using System.ComponentModel.DataAnnotations;

namespace XpertSphere.MonolithApi.DTOs.Auth;

public record LoginDto
{
    [Required(ErrorMessage = "L'email est obligatoire")]
    [EmailAddress(ErrorMessage = "Format d'email invalide")]
    public required string Email { get; init; }

    [Required(ErrorMessage = "Le mot de passe est obligatoire")]
    [MinLength(6, ErrorMessage = "Le mot de passe doit contenir au moins 6 caractères")]
    public required string Password { get; init; }

    public bool RememberMe { get; init; } = false;

    public string? ReturnUrl { get; init; }

    // Entra ID specific fields
    public bool ForceLocalAuth { get; init; } = false;
    public string? PreferredAuthType { get; init; }
    public bool SkipEntraIdRedirect { get; init; } = false;
}