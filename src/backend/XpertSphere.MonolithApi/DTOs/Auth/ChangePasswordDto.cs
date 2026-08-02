using System.ComponentModel.DataAnnotations;

namespace XpertSphere.MonolithApi.DTOs.Auth;

public record ChangePasswordDto
{
    [Required(ErrorMessage = "Le mot de passe actuel est obligatoire")]
    public required string CurrentPassword { get; init; }

    [Required(ErrorMessage = "Le nouveau mot de passe est obligatoire")]
    [MinLength(6, ErrorMessage = "Le mot de passe doit contenir au moins 6 caractères")]
    public required string NewPassword { get; init; }

    [Required(ErrorMessage = "La confirmation du mot de passe est obligatoire")]
    [Compare(nameof(NewPassword), ErrorMessage = "Les mots de passe ne correspondent pas")]
    public required string ConfirmPassword { get; init; }
}