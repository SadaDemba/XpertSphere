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

    /// <summary>
    /// True si le compte n'a pas encore confirmé son email (activation stricte,
    /// RequireConfirmedEmail = true). Positionné automatiquement par
    /// <see cref="XpertSphere.MonolithApi.Mappings.AuthMappingProfile"/> à partir de
    /// <c>User.EmailConfirmed</c> pour toute projection User -> AuthResponseDto.
    /// </summary>
    public bool RequiresEmailConfirmation { get; init; } = false;

    // Entra ID specific fields
    public bool RequiresEntraId { get; init; } = false;
    public string? EntraIdAuthUrl { get; init; }
    public string? AuthType { get; init; } // "B2B", "B2C", "Local"
    public string? EntraIdState { get; init; }
    public bool IsExternalAuth { get; init; } = false;
    public string? ExternalId { get; init; }
    public Dictionary<string, object>? EntraIdClaims { get; init; }
}