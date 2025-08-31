using System.ComponentModel.DataAnnotations;

namespace XpertSphere.MonolithApi.DTOs.Auth;

public record EntraIdCallbackDto
{
    [Required]
    public required string Code { get; init; }
    
    [Required]
    public required string State { get; init; }
    
    public string? Error { get; init; }
    
    public string? ErrorDescription { get; init; }
    
    public string? AuthFlow { get; init; } // "B2B" or "B2C"
}