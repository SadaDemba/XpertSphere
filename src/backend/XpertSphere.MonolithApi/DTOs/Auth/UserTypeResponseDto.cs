namespace XpertSphere.MonolithApi.DTOs.Auth;

public record UserTypeResponseDto
{
    public string Email { get; init; } = string.Empty;
    public string UserType { get; init; } = string.Empty;
    public string RecommendedAuth { get; init; } = string.Empty;
    public string AuthEndpoint { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}