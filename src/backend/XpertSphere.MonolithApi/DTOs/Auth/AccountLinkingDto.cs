using System.ComponentModel.DataAnnotations;

namespace XpertSphere.MonolithApi.DTOs.Auth;

public record AccountLinkingDto
{
    [Required]
    [EmailAddress]
    public required string Email { get; init; }
    
    [Required]
    public required string EntraIdToken { get; init; }
    
    public string? ExternalId { get; init; }
    
    public string? AuthType { get; init; } // "B2B" or "B2C"
    
    public bool OverwriteExisting { get; init; } = false;
}