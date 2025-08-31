using System.ComponentModel.DataAnnotations;

namespace XpertSphere.MonolithApi.DTOs.Auth;

public record LinkAccountDto
{
    [Required]
    public required string EntraIdToken { get; init; }
}