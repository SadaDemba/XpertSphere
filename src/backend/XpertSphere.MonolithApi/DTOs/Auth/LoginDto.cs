using System.ComponentModel.DataAnnotations;

namespace XpertSphere.MonolithApi.DTOs.Auth;

public record LoginDto
{
    [Required]
    [EmailAddress]
    public required string Email { get; init; }

    [Required]
    [MinLength(6)]
    public required string Password { get; init; }

    public bool RememberMe { get; init; } = false;
    
    public string? ReturnUrl { get; init; }
    
    // Entra ID specific fields
    public bool ForceLocalAuth { get; init; } = false;
    public string? PreferredAuthType { get; init; }
    public bool SkipEntraIdRedirect { get; init; } = false;
}

