using System.ComponentModel.DataAnnotations;

namespace XpertSphere.MonolithApi.DTOs.Auth;

public record ForgotPasswordDto
{
    [Required]
    [EmailAddress]
    public required string Email { get; init; }
}