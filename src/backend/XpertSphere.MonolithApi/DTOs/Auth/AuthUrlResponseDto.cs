namespace XpertSphere.MonolithApi.DTOs.Auth;

public record AuthUrlResponseDto
{
    public bool UseLocalAuth { get; init; }
    public string? EntraIdUrl { get; init; }
    public string? AuthType { get; init; }
    public string? LocalEndpoint { get; init; }
    public string Message { get; init; } = string.Empty;
}