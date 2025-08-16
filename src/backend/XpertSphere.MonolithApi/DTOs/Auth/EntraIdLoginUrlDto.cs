using System.ComponentModel.DataAnnotations;

namespace XpertSphere.MonolithApi.DTOs.Auth;

public record EntraIdLoginUrlDto
{
    [EmailAddress]
    public string? Email { get; init; }
    
    public string? ReturnUrl { get; init; } = "/dashboard";
    
    public string? AuthType { get; init; } // "B2B" or "B2C"
    
    public bool ForceLocal { get; init; } = false;
}