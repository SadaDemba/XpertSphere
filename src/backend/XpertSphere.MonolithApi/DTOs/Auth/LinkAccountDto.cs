using System.ComponentModel.DataAnnotations;

namespace XpertSphere.MonolithApi.DTOs.Auth;

public record LinkAccountDto
{
    [Required(ErrorMessage = "Le jeton Entra ID est obligatoire")]
    public required string EntraIdToken { get; init; }
}