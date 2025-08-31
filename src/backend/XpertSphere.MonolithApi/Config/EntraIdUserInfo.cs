namespace XpertSphere.MonolithApi.Config;

public class EntraIdUserInfo
{
    public string ExternalId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public List<string> Groups { get; set; } = [];
    public List<string> Roles { get; set; } = [];
    public string AuthType { get; set; } = string.Empty;
}