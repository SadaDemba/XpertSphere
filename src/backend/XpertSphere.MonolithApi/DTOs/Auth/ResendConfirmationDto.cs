using System.ComponentModel.DataAnnotations;

namespace XpertSphere.MonolithApi.DTOs.Auth;

public record ResendConfirmationDto
{
    [Required] [EmailAddress] public required string Email { get; init; }
}
