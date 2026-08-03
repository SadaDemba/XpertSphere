using System.ComponentModel.DataAnnotations;

namespace XpertSphere.MonolithApi.DTOs.Auth;

/// <summary>
/// DTO for admin-initiated password reset
/// </summary>
public class AdminResetPasswordDto
{
    /// <summary>
    /// Email of the user whose password should be reset
    /// </summary>
    [Required(ErrorMessage = "L'email est obligatoire")]
    [EmailAddress(ErrorMessage = "Format d'email invalide")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// New password for the user
    /// </summary>
    [Required(ErrorMessage = "Le nouveau mot de passe est obligatoire")]
    [MinLength(6, ErrorMessage = "Le mot de passe doit contenir au moins 6 caractères")]
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// Confirmation of the new password
    /// </summary>
    [Required(ErrorMessage = "La confirmation du mot de passe est obligatoire")]
    public string ConfirmPassword { get; set; } = string.Empty;
}