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
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// New password for the user
    /// </summary>
    [Required(ErrorMessage = "New password is required")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters long")]
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// Confirmation of the new password
    /// </summary>
    [Required(ErrorMessage = "Password confirmation is required")]
    public string ConfirmPassword { get; set; } = string.Empty;
}