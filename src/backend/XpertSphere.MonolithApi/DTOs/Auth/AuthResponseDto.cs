using XpertSphere.MonolithApi.DTOs.User;

namespace XpertSphere.MonolithApi.DTOs.Auth;

public record AuthResponseDto
{
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; init; }
    public DateTime? TokenExpiry { get; init; }
    public UserDto? User { get; init; }
    public string? EmailConfirmationToken { get; init; }
    public string? RedirectUrl { get; set; }
    public List<string> Errors { get; init; } = [];
    
    // Entra ID specific fields
    public bool RequiresEntraId { get; init; } = false;
    public string? EntraIdAuthUrl { get; init; }
    public string? AuthType { get; init; } // "B2B", "B2C", "Local"
    public string? EntraIdState { get; init; }
    public bool IsExternalAuth { get; init; } = false;
    public string? ExternalId { get; init; }
    public Dictionary<string, object>? EntraIdClaims { get; init; }
}