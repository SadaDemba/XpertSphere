using System.ComponentModel.DataAnnotations;

namespace XpertSphere.MonolithApi.DTOs.Auth;

public record RefreshTokenDto
{
    [Required]
    [EmailAddress]
    public required string Email { get; init; }

    [Required]
    public required string RefreshToken { get; init; }
}