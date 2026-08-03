using System.ComponentModel.DataAnnotations;

namespace XpertSphere.MonolithApi.DTOs.Auth;

public record ResetPasswordDto
{
    [Required(ErrorMessage = "L'email est obligatoire")]
    [EmailAddress(ErrorMessage = "Format d'email invalide")]
    public required string Email { get; init; }

    [Required(ErrorMessage = "Le jeton est obligatoire")]
    public required string Token { get; init; }

    [Required(ErrorMessage = "Le mot de passe est obligatoire")]
    [MinLength(6, ErrorMessage = "Le mot de passe doit contenir au moins 6 caractères")]
    public required string NewPassword { get; init; }

    [Required(ErrorMessage = "La confirmation du mot de passe est obligatoire")]
    [Compare(nameof(NewPassword), ErrorMessage = "Les mots de passe ne correspondent pas")]
    public required string ConfirmPassword { get; init; }
}