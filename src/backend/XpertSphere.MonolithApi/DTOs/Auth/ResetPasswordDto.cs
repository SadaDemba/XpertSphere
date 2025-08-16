using System.ComponentModel.DataAnnotations;

namespace XpertSphere.MonolithApi.DTOs.Auth;

public record ResetPasswordDto
{
    [Required]
    [EmailAddress]
    public required string Email { get; init; }

    [Required]
    public required string Token { get; init; }

    [Required]
    [MinLength(6)]
    public required string NewPassword { get; init; }

    [Required]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match")]
    public required string ConfirmPassword { get; init; }
}